using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Rates;
using Vafadar.Finance.Core.Settings;

namespace Vafadar.Finance.Data;

/// <summary>The result of an import.</summary>
/// <param name="BatchId">The batch that can be undone; <see langword="null"/> when nothing was imported.</param>
/// <param name="Imported">Entries saved.</param>
/// <param name="Skipped">Entries skipped because they exist already.</param>
/// <param name="Errors">Invalid entries by id; when not empty, nothing was saved.</param>
public sealed record ImportResult(Guid? BatchId, int Imported, int Skipped, IReadOnlyDictionary<Guid, IReadOnlyList<LedgerError>> Errors);

/// <summary>A past import that can be undone.</summary>
public sealed record ImportBatchInfo(Guid BatchId, int Count, DateTimeOffset ImportedAt);

/// <summary>The result of saving an entry.</summary>
/// <param name="Errors">Validation problems; empty when saved.</param>
public sealed record SaveResult(IReadOnlyList<LedgerError> Errors)
{
    /// <summary>Gets a value indicating whether the entry was saved.</summary>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>A successful result.</summary>
    public static SaveResult Success { get; } = new([]);
}

/// <summary>
/// Data access for the Finance app. Every operation uses a short-lived context from the factory.
/// </summary>
public sealed class FinanceStore(IDbContextFactory<FinanceDbContext> contextFactory)
{
    /// <summary>Raised after data was written, e.g. to refresh reminders.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Returns the settings synchronously – for startup code on the UI thread, where blocking on async code could
    /// deadlock. Returns defaults (not saved) when no settings exist yet.
    /// </summary>
    public FinanceSettings GetSettings()
    {
        using var db = contextFactory.CreateDbContext();
        return db.Settings.AsNoTracking().FirstOrDefault() ?? new FinanceSettings();
    }

    /// <summary>Returns the settings row, creating it on first use.</summary>
    public async Task<FinanceSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new FinanceSettings();
        db.Settings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
        return settings;
    }

    /// <summary>Saves the settings.</summary>
    public async Task SaveSettingsAsync(FinanceSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existingId = await db.Settings.Select(s => (Guid?)s.Id).FirstOrDefaultAsync(cancellationToken);
        if (existingId is null)
        {
            db.Settings.Add(settings);
        }
        else if (existingId == settings.Id)
        {
            db.Settings.Update(settings);
        }
        else
        {
            throw new InvalidOperationException("A different settings row already exists; load it with GetSettingsAsync first.");
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Returns all accounts ordered for display.</summary>
    public async Task<List<Account>> GetAccountsAsync(bool includeArchived = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Accounts.AsNoTracking()
            .Where(a => includeArchived || !a.IsArchived)
            .OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Inserts or updates an account. The currency of an account with entries cannot change (ACC-07).</summary>
    /// <returns><see langword="false"/> when the currency change was refused.</returns>
    public async Task<bool> SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);
        if (existing is null)
        {
            db.Accounts.Add(account);
        }
        else
        {
            if (!string.Equals(existing.CurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                && await db.Entries.AnyAsync(e => e.AccountId == account.Id || e.ToAccountId == account.Id, cancellationToken))
            {
                return false;
            }

            db.Accounts.Update(account);
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
        return true;
    }

    /// <summary>Returns whether an account has any entries.</summary>
    public async Task<bool> HasEntriesAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AnyAsync(e => e.AccountId == accountId || e.ToAccountId == accountId, cancellationToken);
    }

    /// <summary>Returns all categories.</summary>
    public async Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.SortOrder).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the default categories (CAT-01). Defaults added in later versions are created on the next start;
    /// existing ones – renamed or archived by the user – are never touched.
    /// </summary>
    public async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = (await db.Categories.Where(c => c.SystemKey != null).Select(c => new { c.Kind, c.SystemKey }).ToListAsync(cancellationToken))
            .Select(c => (c.Kind, c.SystemKey!)).ToHashSet();

        var order = 0;
        foreach (var (kind, key, icon, color) in DefaultCategories.All)
        {
            if (!existing.Contains((kind, key)))
            {
                db.Categories.Add(new Category { Kind = kind, SystemKey = key, Icon = icon, Color = color, SortOrder = order });
            }

            order++;
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Inserts or updates a category.</summary>
    public async Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Categories.AnyAsync(c => c.Id == category.Id, cancellationToken))
        {
            db.Categories.Update(category);
        }
        else
        {
            db.Categories.Add(category);
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Returns the budget of a month in a calendar and currency, if one exists.</summary>
    public async Task<Budget?> GetBudgetAsync(int year, int month, PeriodCalendar calendar, string currencyCode, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Year == year && b.Month == month && b.Calendar == calendar && b.CurrencyCode == currencyCode, cancellationToken);
    }

    /// <summary>Returns all budgets, newest period first.</summary>
    public async Task<List<Budget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking().OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns what the previous consecutive months pass on to <paramref name="budget"/> (§10.3). Only budgets of the same
    /// calendar and currency count; a month without a budget ends the chain. Spending includes unreviewed entries.
    /// </summary>
    public async Task<BudgetCarry> GetBudgetCarryAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (budget.Rollover == BudgetRollover.None)
        {
            return BudgetCarry.None;
        }

        var budgets = (await GetBudgetsAsync(cancellationToken))
            .Where(b => b.Calendar == budget.Calendar && string.Equals(b.CurrencyCode, budget.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(b => (b.Year, b.Month));
        var chain = new List<Budget>();
        var (year, month) = PeriodMath.Previous(budget.Year, budget.Month);
        while (chain.Count < BudgetRolloverCalculator.MaxMonths && budgets.TryGetValue((year, month), out var previous))
        {
            chain.Insert(0, previous);
            (year, month) = PeriodMath.Previous(year, month);
        }

        if (chain.Count == 0)
        {
            return BudgetCarry.None;
        }

        var from = PeriodMath.MonthRange(chain[0].Year, chain[0].Month, budget.Calendar).First;
        var to = PeriodMath.MonthRange(chain[^1].Year, chain[^1].Month, budget.Calendar).Last;
        var accounts = await GetAccountsAsync(cancellationToken: cancellationToken);
        var entries = await GetEntriesAsync(from, to, cancellationToken);
        var categories = await GetCategoriesAsync(cancellationToken);
        var months = chain.Select(b =>
        {
            var (start, end) = PeriodMath.MonthRange(b.Year, b.Month, b.Calendar);
            var scope = b.AccountIds.Count > 0 ? b.AccountIds : null;
            return new BudgetMonth(
                b.Rollover,
                b.TotalLimit,
                BudgetCalculator.NetExpense(accounts, entries, start, end, b.CurrencyCode, scope),
                b.CategoryLimits.ToDictionary(l => l.CategoryId, l => l.Limit),
                b.CategoryLimits.ToDictionary(l => l.CategoryId, l => BudgetCalculator.NetExpense(accounts, entries, start, end, b.CurrencyCode, scope, [l.CategoryId], categories)));
        }).ToList();
        return BudgetRolloverCalculator.CarryInto(months, budget.Rollover);
    }

    /// <summary>Inserts or updates a budget including its category limits.</summary>
    public async Task SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budget.Id, cancellationToken);
        if (existing is null)
        {
            db.Budgets.Add(budget);
        }
        else
        {
            // Owned limits are replaced as a whole; the tracked instance keeps EF's owned-entity bookkeeping right.
            existing.TotalLimit = budget.TotalLimit;
            existing.AccountIds = [.. budget.AccountIds];
            existing.AlertsEnabled = budget.AlertsEnabled;
            existing.Rollover = budget.Rollover;
            existing.CategoryLimits.Clear();
            existing.CategoryLimits.AddRange(budget.CategoryLimits.Select(l => new BudgetCategoryLimit { CategoryId = l.CategoryId, Limit = l.Limit }));
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Deletes a budget; entries are not affected.</summary>
    public async Task DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken) is { } budget)
        {
            db.Budgets.Remove(budget);
            await db.SaveChangesAsync(cancellationToken);
            OnChanged();
        }
    }

    /// <summary>
    /// Imports entries as one batch in a single transaction (IO-11, AT-54): either all valid entries are saved or none.
    /// Entries whose id exists already are skipped (IO-10). Invalid entries are reported and nothing is saved.
    /// </summary>
    public async Task<ImportResult> ImportAsync(IReadOnlyList<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var ids = entries.Select(e => e.Id).ToList();
        var existing = (await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(cancellationToken)).ToHashSet();

        var batchId = Guid.CreateVersion7();
        var errors = new Dictionary<Guid, IReadOnlyList<LedgerError>>();
        var toAdd = new List<LedgerEntry>();
        foreach (var entry in entries.Where(e => !existing.Contains(e.Id)))
        {
            var problems = LedgerValidator.Validate(entry, accounts, categories);
            if (problems.Count > 0)
            {
                errors[entry.Id] = problems;
                continue;
            }

            entry.ImportBatchId = batchId;
            entry.Source = EntrySource.Import;
            toAdd.Add(entry);
        }

        if (errors.Count > 0 || toAdd.Count == 0)
        {
            return new ImportResult(null, 0, entries.Count - toAdd.Count - errors.Count, errors);
        }

        db.Entries.AddRange(toAdd);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
        return new ImportResult(batchId, toAdd.Count, existing.Count, errors);
    }

    /// <summary>Returns the import batches, newest first.</summary>
    public async Task<List<ImportBatchInfo>> GetImportBatchesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Entries.AsNoTracking().Where(e => e.ImportBatchId != null)
            .Select(e => new { e.ImportBatchId, e.CreatedAt }).ToListAsync(cancellationToken);
        return [.. rows.GroupBy(r => r.ImportBatchId!.Value)
            .Select(g => new ImportBatchInfo(g.Key, g.Count(), g.Min(r => r.CreatedAt)))
            .OrderByDescending(b => b.ImportedAt)];
    }

    /// <summary>Removes the entries of one import batch only; data that existed before is untouched (IO-11).</summary>
    public async Task<int> UndoImportAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var removed = await db.Entries.Where(e => e.ImportBatchId == batchId).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
        return removed;
    }

    /// <summary>Returns all manual exchange rates, newest first.</summary>
    public async Task<List<ExchangeRate>> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ExchangeRates.AsNoTracking().OrderByDescending(r => r.Date).ThenBy(r => r.FromCurrencyCode).ToListAsync(cancellationToken);
    }

    /// <summary>Saves a rate; a rate for the same date and currency pair is replaced.</summary>
    public async Task SaveRateAsync(ExchangeRate rate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rate);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.ExchangeRates.FirstOrDefaultAsync(
            r => r.Id == rate.Id || (r.Date == rate.Date && r.FromCurrencyCode == rate.FromCurrencyCode && r.ToCurrencyCode == rate.ToCurrencyCode),
            cancellationToken);
        if (existing is null)
        {
            db.ExchangeRates.Add(rate);
        }
        else
        {
            existing.Date = rate.Date;
            existing.FromCurrencyCode = rate.FromCurrencyCode;
            existing.ToCurrencyCode = rate.ToCurrencyCode;
            existing.Rate = rate.Rate;
            existing.IsEstimate = rate.IsEstimate;
        }

        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Returns the quick entry templates in their order (TX-04).</summary>
    public async Task<List<EntryTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Templates.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(cancellationToken);
    }

    /// <summary>Adds a template at the end, or updates an existing one.</summary>
    public async Task SaveTemplateAsync(EntryTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Templates.AnyAsync(t => t.Id == template.Id, cancellationToken))
        {
            db.Templates.Update(template);
        }
        else
        {
            template.SortOrder = await db.Templates.Select(t => (int?)t.SortOrder).MaxAsync(cancellationToken) + 1 ?? 0;
            db.Templates.Add(template);
        }

        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a template; entries created from it are not affected.</summary>
    public async Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Templates.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a rate; recorded amounts are never affected (FX-04).</summary>
    public async Task DeleteRateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.ExchangeRates.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>
    /// Merges <paramref name="sourceId"/> into <paramref name="targetId"/> (CAT-02): entries, plans, budget limits and
    /// sub-categories move to the target, then the source is archived. No entry is deleted.
    /// </summary>
    public async Task MergeCategoryAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        if (sourceId == targetId)
        {
            return;
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var source = await db.Categories.FirstAsync(c => c.Id == sourceId, cancellationToken);
        var target = await db.Categories.FirstAsync(c => c.Id == targetId, cancellationToken);
        if (source.Kind != target.Kind)
        {
            throw new InvalidOperationException("Only categories of the same kind can be merged.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Entries.Where(e => e.CategoryId == sourceId).ExecuteUpdateAsync(e => e.SetProperty(x => x.CategoryId, targetId), cancellationToken);
        await db.Schedules.Where(s => s.CategoryId == sourceId).ExecuteUpdateAsync(s => s.SetProperty(x => x.CategoryId, targetId), cancellationToken);
        await db.Templates.Where(t => t.CategoryId == sourceId).ExecuteUpdateAsync(t => t.SetProperty(x => x.CategoryId, targetId), cancellationToken);

        // One level only: children of the source go under the target's main category.
        var newParent = target.ParentId ?? target.Id;
        await db.Categories.Where(c => c.ParentId == sourceId && c.Id != targetId).ExecuteUpdateAsync(c => c.SetProperty(x => x.ParentId, newParent), cancellationToken);

        foreach (var budget in await db.Budgets.ToListAsync(cancellationToken))
        {
            var limit = budget.CategoryLimits.FirstOrDefault(l => l.CategoryId == sourceId);
            if (limit is null)
            {
                continue;
            }

            budget.CategoryLimits.Remove(limit);
            if (budget.CategoryLimits.FirstOrDefault(l => l.CategoryId == targetId) is { } existing)
            {
                existing.Limit += limit.Limit;
            }
            else
            {
                budget.CategoryLimits.Add(new BudgetCategoryLimit { CategoryId = targetId, Limit = limit.Limit });
            }
        }

        source.IsArchived = true;
        source.ParentId = null;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Moves a category one place earlier (<paramref name="step"/> = -1) or later (+1) among its siblings.</summary>
    public async Task MoveCategoryAsync(Guid id, int step, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.FirstAsync(c => c.Id == id, cancellationToken);
        var siblings = await db.Categories
            .Where(c => c.Kind == category.Kind && c.ParentId == category.ParentId && !c.IsArchived)
            .OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
        var index = siblings.FindIndex(c => c.Id == id);
        var other = index + step;
        if (index < 0 || other < 0 || other >= siblings.Count)
        {
            return;
        }

        // Renumber so that equal sort orders never make the move a no-op.
        (siblings[index], siblings[other]) = (siblings[other], siblings[index]);
        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = siblings.Min(s => s.SortOrder) + i;
        }

        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>
    /// Deletes all Finance data on this device (SEC-05, BAK-15): entries, plans, budgets, rates, categories, accounts
    /// and settings. Backup files and copies outside the database are not touched.
    /// </summary>
    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Entries.ExecuteDeleteAsync(cancellationToken);
        await db.Templates.ExecuteDeleteAsync(cancellationToken);
        await db.GoalAllocations.ExecuteDeleteAsync(cancellationToken);
        await db.Goals.ExecuteDeleteAsync(cancellationToken);
        await db.OccurrenceStates.ExecuteDeleteAsync(cancellationToken);
        await db.Schedules.ExecuteDeleteAsync(cancellationToken);
        db.Budgets.RemoveRange(await db.Budgets.ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        await db.ExchangeRates.ExecuteDeleteAsync(cancellationToken);
        await db.Categories.Where(c => c.ParentId != null).ExecuteDeleteAsync(cancellationToken);
        await db.Categories.ExecuteDeleteAsync(cancellationToken);
        await db.Accounts.ExecuteDeleteAsync(cancellationToken);
        await db.Settings.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Returns entries between two dates (inclusive), newest first.</summary>
    public async Task<List<LedgerEntry>> GetEntriesAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Entries.AsNoTracking();
        if (from is { } f)
        {
            query = query.Where(e => e.Date >= f);
        }

        if (to is { } t)
        {
            query = query.Where(e => e.Date <= t);
        }

        return await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);
    }

    /// <summary>Returns one entry.</summary>
    public async Task<LedgerEntry?> GetEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>Returns the refunds linked to a purchase.</summary>
    public async Task<List<LedgerEntry>> GetRefundsAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().Where(e => e.RefundOfId == purchaseId).OrderBy(e => e.Date).ToListAsync(cancellationToken);
    }

    /// <summary>Returns the entries sharing a group id (e.g. a transfer and its fee).</summary>
    public async Task<List<LedgerEntry>> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().Where(e => e.GroupId == groupId).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Validates and saves an entry. Saving the same entry id twice updates it instead of creating a second entry, so
    /// repeated taps on Save cannot duplicate it (TX-06, AT-03).
    /// </summary>
    public Task<SaveResult> SaveEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default) =>
        SaveEntriesAsync([entry], [], cancellationToken);

    /// <summary>
    /// Validates and saves related entries in one transaction – e.g. a transfer and its fee – and deletes
    /// <paramref name="deleteIds"/> (a removed fee). Nothing is saved when any entry is invalid.
    /// </summary>
    public async Task<SaveResult> SaveEntriesAsync(
        IReadOnlyList<LedgerEntry> entries,
        IReadOnlyCollection<Guid> deleteIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(deleteIds);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var ids = entries.Select(e => e.Id).ToList();
        var existing = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);

        foreach (var entry in entries)
        {
            LedgerEntry? original = null;
            long otherRefunds = 0;
            if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } originalId)
            {
                original = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == originalId, cancellationToken);
                otherRefunds = await db.Entries.Where(e => e.RefundOfId == originalId && e.Id != entry.Id).SumAsync(e => e.Amount, cancellationToken);
            }

            var errors = LedgerValidator.Validate(entry, accounts, categories, original, otherRefunds, isNew: !existing.ContainsKey(entry.Id));
            if (errors.Count > 0)
            {
                return new SaveResult(errors);
            }
        }

        foreach (var entry in entries)
        {
            if (entry.Kind != EntryKind.Transfer)
            {
                entry.ToAccountId = null;
                entry.ToAmount = null;
            }

            if (existing.TryGetValue(entry.Id, out var previous))
            {
                entry.CreatedAt = previous.CreatedAt;
                db.Entries.Update(entry);
            }
            else
            {
                db.Entries.Add(entry);
            }
        }

        if (deleteIds.Count > 0)
        {
            db.Entries.RemoveRange(await db.Entries.Where(e => deleteIds.Contains(e.Id) && !ids.Contains(e.Id)).ToListAsync(cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
        return SaveResult.Success;
    }

    /// <summary>
    /// Deletes an entry together with the entries of its group (a transfer and its fee) and returns them, so that
    /// <see cref="RestoreEntriesAsync"/> can undo the deletion. Refunds linked to a deleted purchase are kept.
    /// </summary>
    public async Task<IReadOnlyList<LedgerEntry>> DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return [];
        }

        List<LedgerEntry> deleted = entry.GroupId is { } group
            ? await db.Entries.Where(e => e.GroupId == group).ToListAsync(cancellationToken)
            : [entry];

        db.Entries.RemoveRange(deleted);

        // A deleted settlement reopens its occurrence; automatic posting must not bring it back (REC-18, AT-31).
        foreach (var settled in deleted.Where(e => e.ScheduleId is not null))
        {
            var state = await db.OccurrenceStates.FirstOrDefaultAsync(
                s => s.ScheduleId == settled.ScheduleId && s.OriginalDate == settled.OccurrenceDate, cancellationToken);
            if (state is not null)
            {
                state.Status = OccurrenceStatus.Open;
                state.EntryId = null;
                state.AutoPostSuppressed = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
        return deleted;
    }

    /// <summary>Re-inserts deleted entries unchanged (undo of <see cref="DeleteEntryAsync"/>).</summary>
    public async Task RestoreEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var entry in entries)
        {
            if (await db.Entries.AnyAsync(e => e.Id == entry.Id, cancellationToken))
            {
                continue;
            }

            db.Entries.Add(entry);
            if (entry.ScheduleId is { } scheduleId && entry.OccurrenceDate is { } original)
            {
                var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.OriginalDate == original, cancellationToken);
                if (state is null)
                {
                    state = new OccurrenceState { ScheduleId = scheduleId, OriginalDate = original };
                    db.OccurrenceStates.Add(state);
                }

                state.Status = OccurrenceStatus.Settled;
                state.EntryId = entry.Id;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
