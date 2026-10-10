using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The result of an import.</summary>
/// <param name="BatchId">The batch that can be undone; <see langword="null"/> when nothing was imported.</param>
/// <param name="Imported">Accepted new rows, including an aggregate fully replaced by details.</param>
/// <param name="Skipped">Entries skipped because they exist already.</param>
/// <param name="Errors">Invalid entries by id; when not empty, nothing was saved.</param>
public sealed record ImportResult(Guid? BatchId, int Imported, int Skipped, IReadOnlyDictionary<Guid, IReadOnlyList<LedgerError>> Errors)
{
    /// <summary>Gets a conflicting overlap choice; when present, nothing was saved.</summary>
    public ImportOverlapConflict? Conflict { get; init; }
}

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
/// Data access for the Zanance app. Every operation uses a short-lived context from the factory.
/// </summary>
public sealed partial class ZananceStore(IDbContextFactory<ZananceDbContext> contextFactory, TimeProvider time,
    ICommercialWriteAccessSource commercialAccess)
{
    // Refund id → purchase id of refunds unlinked by a purchase deletion, until that deletion is undone (this session).
    private readonly Dictionary<Guid, Guid> _refundLinks = [];
    private readonly SemaphoreSlim _settingsGate = new(1, 1);

    // One settings change at a time: every change reads the row, changes it and writes the whole row back.
    private readonly SemaphoreSlim _settingsUpdate = new(1, 1);

    /// <summary>Raised after data was written, e.g. to refresh reminders.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Completes first run after restoring a profile with accounts, preserving its preferences and creating no
    /// account (D-62). An empty backup still needs onboarding.
    /// </summary>
    public async Task CompleteRestoredOnboardingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var hasAccounts = await db.Accounts.AnyAsync(cancellationToken);
        await UpdateSettingsAsync(settings => settings.OnboardingCompleted = hasAccounts, cancellationToken);
    }

    /// <summary>
    /// Returns the settings synchronously – for startup code on the UI thread, where blocking on async code could
    /// deadlock. Returns defaults (not saved) when no settings exist yet.
    /// </summary>
    public ZananceSettings GetSettings()
    {
        using var db = contextFactory.CreateDbContext();
        return db.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstOrDefault() ?? new ZananceSettings();
    }

    /// <summary>
    /// Saves the settings synchronously – for startup code on the UI thread (see <see cref="GetSettings"/>). Creates the
    /// row when none exists yet.
    /// </summary>
    public void SaveSettings(ZananceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        using var db = contextFactory.CreateDbContext();
        var existingId = db.Settings.OrderBy(s => s.CreatedAt).Select(s => (Guid?)s.Id).FirstOrDefault();
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
            throw new InvalidOperationException("A different settings row already exists; load it with GetSettings first.");
        }

        db.SaveChanges();
        OnChanged();
    }

    /// <summary>Returns the settings row, creating it on first use.</summary>
    public async Task<ZananceSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        // The first calls at start come from several places at once; only one of them creates the row.
        await _settingsGate.WaitAsync(cancellationToken);
        try
        {
            settings = await db.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (settings is not null)
            {
                return settings;
            }

            settings = new ZananceSettings();
            db.Settings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _settingsGate.Release();
        }

        OnChanged();
        return settings;
    }

    /// <summary>
    /// Reads the settings, applies <paramref name="change"/> and saves them – one change after the other. Two changes at the
    /// same moment (e.g. two switches on the settings page) each read the row and wrote it back whole, so the later one
    /// restored the old value of the earlier one.
    /// </summary>
    public async Task UpdateSettingsAsync(Action<ZananceSettings> change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        await _settingsUpdate.WaitAsync(cancellationToken);
        try
        {
            var settings = await GetSettingsAsync(cancellationToken);
            change(settings);
            await SaveSettingsAsync(settings, cancellationToken);
        }
        finally
        {
            _settingsUpdate.Release();
        }
    }

    /// <summary>Saves the settings.</summary>
    public async Task SaveSettingsAsync(ZananceSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existingId = await db.Settings.OrderBy(s => s.CreatedAt).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(cancellationToken);
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

    /// <summary>
    /// Inserts or updates an account. The currency cannot change while any stored amount is in it – entries, plans,
    /// templates, earmarks, budgets or a loan installment (ACC-07, ZEX-S0105).
    /// </summary>
    /// <returns><see langword="false"/> when the currency change was refused.</returns>
    public async Task<bool> SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);

        var existing = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);
        write.DemandFeature(existing is null ? CommercialFeature.FinancialAccounts : CommercialFeature.Corrections);
        if (write.Enforced && !account.IsArchived && (existing is null || existing.IsArchived))
        {
            // D-118: creation and unarchive consume the same slot. Existing corrections and archival stay possible
            // above quota; no chosen account, money, visibility or historical status is changed automatically.
            if (write.DemandCapacity(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts) is { } maximum)
            {
                var current = await db.Accounts.CountAsync(a => !a.IsArchived, cancellationToken);
                write.DemandCount(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts, maximum, current);
            }
        }
        if (existing is null)
        {
            db.Accounts.Add(account);
        }
        else
        {
            if (!string.Equals(existing.CurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                && (await CurrencyLockAsync(db, existing, cancellationToken)).IsLocked)
            {
                return false;
            }

            db.Accounts.Update(account);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

        OnChanged();
        return true;
    }

    /// <summary>Returns what keeps the currency of an account from changing (ZEX-S0105); <see cref="AccountCurrencyLock.None"/> for a new account.</summary>
    public async Task<AccountCurrencyLock> GetCurrencyLockAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        return account is null ? AccountCurrencyLock.None : await CurrencyLockAsync(db, account, cancellationToken);
    }

    private static async Task<AccountCurrencyLock> CurrencyLockAsync(ZananceDbContext db, Account account, CancellationToken cancellationToken)
    {
        var id = account.Id;
        var entries = await db.Entries.CountAsync(e => e.AccountId == id || e.ToAccountId == id, cancellationToken);
        var plans = await db.Schedules.CountAsync(s => s.AccountId == id || s.ToAccountId == id, cancellationToken);
        var templates = await db.Templates.CountAsync(t => t.AccountId == id || t.ToAccountId == id, cancellationToken);
        var earmarks = await db.GoalAllocations.CountAsync(a => a.AccountId == id, cancellationToken);

        // The account list of a budget is a stored collection, checked after loading (budgets are few).
        var budgets = (await db.Budgets.AsNoTracking().ToListAsync(cancellationToken)).Count(b => b.AccountIds.Contains(id));
        return new AccountCurrencyLock(entries, plans, templates, earmarks, budgets, account.Installment is not null);
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
        var added = 0;
        foreach (var (kind, key, icon, color) in DefaultCategories.All)
        {
            if (!existing.Contains((kind, key)))
            {
                db.Categories.Add(new Category
                {
                    Kind = kind, SystemKey = key, Icon = icon, Color = color, SortOrder = order,
                    SpendingType = kind == CategoryKind.Expense ? DefaultCategories.SpendingTypeOf(key) : SpendingType.Flexible,
                    IsEssential = kind == CategoryKind.Expense && DefaultCategories.IsEssential(key),
                });
                added++;
            }

            order++;
        }

        // Listeners (e.g. the reminders) are told only when something was really added.
        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            OnChanged();
        }
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
            .FirstOrDefaultAsync(b => b.Period == BudgetPeriod.Month && b.Year == year && b.Month == month && b.Calendar == calendar && b.CurrencyCode == currencyCode, cancellationToken);
    }

    /// <summary>
    /// Returns the weekly or two-week budget in a currency whose period contains <paramref name="date"/>, if one exists.
    /// A budget made before the week start or the two-week start day was changed is still found.
    /// </summary>
    public async Task<Budget?> GetBudgetAsync(BudgetPeriod period, DateOnly date, string currencyCode, CancellationToken cancellationToken = default)
    {
        var earliest = date.AddDays(1 - BudgetPeriods.Days(period));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking()
            .Where(b => b.Period == period && b.CurrencyCode == currencyCode && b.PeriodStart <= date && b.PeriodStart >= earliest)
            .OrderByDescending(b => b.PeriodStart)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Returns all budgets, newest period first.</summary>
    public async Task<List<Budget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking().OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns what the previous consecutive periods pass on to <paramref name="budget"/> (§10.3). Only budgets of the same
    /// period length, calendar and currency count; a period without a budget ends the chain. Spending includes unreviewed
    /// entries.
    /// </summary>
    public async Task<BudgetCarry> GetBudgetCarryAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (budget.Rollover == BudgetRollover.None)
        {
            return BudgetCarry.None;
        }

        var sameKind = (await GetBudgetsAsync(cancellationToken))
            .Where(b => b.Period == budget.Period && string.Equals(b.CurrencyCode, budget.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var chain = new List<Budget>();
        if (budget.Period == BudgetPeriod.Month)
        {
            var budgets = sameKind.Where(b => b.Calendar == budget.Calendar).ToDictionary(b => (b.Year, b.Month));
            var (year, month) = PeriodMath.Previous(budget.Year, budget.Month);
            while (chain.Count < BudgetRolloverCalculator.MaxMonths && budgets.TryGetValue((year, month), out var previous))
            {
                chain.Insert(0, previous);
                (year, month) = PeriodMath.Previous(year, month);
            }
        }
        else
        {
            // Weeks follow each other without a gap: the previous period ends the day before this one starts.
            var byStart = sameKind.GroupBy(b => b.PeriodStart).ToDictionary(g => g.Key, g => g.First());
            var start = BudgetPeriods.Step(budget.Period, budget.PeriodStart, -1);
            while (chain.Count < BudgetRolloverCalculator.MaxMonths && byStart.TryGetValue(start, out var previous))
            {
                chain.Insert(0, previous);
                start = BudgetPeriods.Step(budget.Period, start, -1);
            }
        }

        if (chain.Count == 0)
        {
            return BudgetCarry.None;
        }

        // Months are financial months: the same start day as the budget page (§10.3).
        var startDay = (await GetSettingsAsync(cancellationToken)).MonthStartDay;
        var from = BudgetPeriods.Range(chain[0], startDay).First;
        var to = BudgetPeriods.Range(chain[^1], startDay).Last;
        var accounts = await GetAccountsAsync(cancellationToken: cancellationToken);
        var entries = await GetEntriesAsync(from, to, cancellationToken);
        var categories = await GetCategoriesAsync(cancellationToken);
        var months = chain.Select(b =>
        {
            var (start, end) = BudgetPeriods.Range(b, startDay);
            var scope = b.AccountIds.Count > 0 ? b.AccountIds : null;
            return new BudgetMonth(
                b.Rollover,
                b.TotalLimit,
                b.Method == BudgetMethod.Flex
                    ? FlexCalculator.FlexibleSpent(accounts, entries, categories, start, end, b.CurrencyCode, scope)
                    : BudgetCalculator.NetExpense(accounts, entries, start, end, b.CurrencyCode, scope),
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
            existing.Method = budget.Method;
            existing.CategoryLimits.Clear();
            existing.CategoryLimits.AddRange(budget.CategoryLimits.Select(l => new BudgetCategoryLimit { CategoryId = l.CategoryId, Limit = l.Limit }));
        }

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>
    /// Saves <paramref name="budget"/> in place of the budget <paramref name="replacedId"/> in one transaction (copy to the
    /// next period over an existing budget, BUD-07): if the new budget cannot be saved, the old one stays.
    /// </summary>
    public async Task ReplaceBudgetAsync(Guid replacedId, Budget budget, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.Budgets.FirstOrDefaultAsync(b => b.Id == replacedId, cancellationToken) is { } replaced)
        {
            // Removed first in its own step, so the new budget of the same period never meets the old one.
            db.Budgets.Remove(replaced);
            await db.SaveChangesAsync(cancellationToken);
        }

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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

    /// <summary>Returns the attachments of an entry without their content (for lists), oldest first.</summary>
    public async Task<List<AttachmentInfo>> GetAttachmentsAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var list = await db.Attachments.AsNoTracking().Where(a => a.EntryId == entryId)
            .Select(a => new AttachmentInfo(a.Id, a.EntryId, a.FileName, a.ContentType, a.Data.Length, a.CreatedAt))
            .ToListAsync(cancellationToken);
        return [.. list.OrderBy(a => a.CreatedAt)];
    }

    /// <summary>
    /// Removes attachments whose entry no longer exists. Deleting an entry keeps them so undo restores them; the app
    /// runs this when it starts (undo does not survive a restart), so receipts of deleted entries do not stay on the device.
    /// </summary>
    public async Task<int> PurgeOrphanAttachmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // A consumed aggregate's attachments must survive a restart until its durable import Undo restores it.
        var protectedIds = (await ReadImportLinksAsync(db, cancellationToken)).SelectMany(b => b.State.Adjustments)
            .Where(a => a.Before is not null && a.After is null).Select(a => a.Before!.Id).ToList();
        return await db.Attachments.Where(a => !db.Entries.Any(e => e.Id == a.EntryId) && !protectedIds.Contains(a.EntryId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Returns one attachment with its content.</summary>
    public async Task<EntryAttachment?> GetAttachmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>Adds an attachment to an existing entry (F2-TX-04).</summary>
    public async Task AddAttachmentAsync(EntryAttachment attachment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        if (attachment.Data.Length is 0 or > EntryAttachment.MaxBytes)
        {
            throw new ArgumentException("An attachment must not be empty or larger than the limit.", nameof(attachment));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Entries.AnyAsync(e => e.Id == attachment.EntryId, cancellationToken))
        {
            throw new InvalidOperationException("The entry does not exist.");
        }

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes an attachment.</summary>
    public async Task DeleteAttachmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Attachments.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>
    /// Moves the attachments of <paramref name="fromEntryIds"/> to another entry, e.g. when split parts are joined or
    /// fewer parts remain, so no receipt is lost with a removed part.
    /// </summary>
    public async Task MoveAttachmentsAsync(IReadOnlyCollection<Guid> fromEntryIds, Guid toEntryId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fromEntryIds);
        if (fromEntryIds.Count == 0)
        {
            return;
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Attachments.Where(a => fromEntryIds.Contains(a.EntryId)).ExecuteUpdateAsync(a => a.SetProperty(x => x.EntryId, toEntryId), cancellationToken);
    }

    /// <summary>Returns the categorization rules sorted by their text (F2-TX-04).</summary>
    public async Task<List<CategoryRule>> GetCategoryRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rules = await db.CategoryRules.AsNoTracking().ToListAsync(cancellationToken);
        return [.. rules.OrderBy(r => r.Match, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>Saves a rule; a rule with the same text and kind is replaced, so a text always has one category.</summary>
    public async Task SaveCategoryRuleAsync(CategoryRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        rule.Match = rule.Match.Trim();
        if (rule.Match.Length < Core.Categories.CategoryRules.MinLength)
        {
            throw new ArgumentException("The text of a rule is too short.", nameof(rule));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var same = (await db.CategoryRules.Where(r => r.Kind == rule.Kind && r.Id != rule.Id).ToListAsync(cancellationToken))
            .Where(r => string.Equals(r.Match, rule.Match, StringComparison.CurrentCultureIgnoreCase));
        db.CategoryRules.RemoveRange(same);
        if (await db.CategoryRules.AnyAsync(r => r.Id == rule.Id, cancellationToken))
        {
            db.CategoryRules.Update(rule);
        }
        else
        {
            db.CategoryRules.Add(rule);
        }

        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a rule; saved entries keep their categories.</summary>
    public async Task DeleteCategoryRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.CategoryRules.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
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
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var existing = await db.Templates.AnyAsync(t => t.Id == template.Id, cancellationToken);
        write.DemandFeature(existing ? CommercialFeature.Corrections : CommercialFeature.QuickTemplates);
        if (existing)
        {
            db.Templates.Update(template);
        }
        else
        {
            // D-119: every template-producing caller uses this count under the same SQLite writer. Edits reuse a
            // slot; reject a new template before changing its sort order or any stored/audited fields.
            if (write.Enforced && write.DemandCapacity(CommercialFeature.QuickTemplates, QuotaKind.QuickTemplates) is { } maximum)
            {
                var current = await db.Templates.CountAsync(cancellationToken);
                write.DemandCount(CommercialFeature.QuickTemplates, QuotaKind.QuickTemplates, maximum, current);
            }
            template.SortOrder = await db.Templates.Select(t => (int?)t.SortOrder).MaxAsync(cancellationToken) + 1 ?? 0;
            db.Templates.Add(template);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a template; entries created from it are not affected.</summary>
    public async Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Templates.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Returns the saved transaction list filters in their order (REP-08).</summary>
    public async Task<List<SavedFilter>> GetSavedFiltersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedFilters.AsNoTracking().OrderBy(f => f.SortOrder).ThenBy(f => f.Name).ToListAsync(cancellationToken);
    }

    /// <summary>Adds a filter at the end, or replaces the one with the same id or the same name.</summary>
    public async Task SaveSavedFilterAsync(SavedFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.Name = filter.Name.Trim();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var all = await db.SavedFilters.ToListAsync(cancellationToken);
        var sameId = all.FirstOrDefault(f => f.Id == filter.Id);

        // Another filter with the same name is replaced (the user confirmed it); the filter itself is updated in place.
        // One SaveChanges, so nothing is removed unless the whole change is saved.
        var sameName = all.Where(f => f.Id != filter.Id && string.Equals(f.Name, filter.Name, StringComparison.CurrentCultureIgnoreCase)).ToList();
        var addsSlot = sameId is null && sameName.Count == 0;
        write.DemandFeature(addsSlot ? CommercialFeature.SavedFilters : CommercialFeature.Corrections);
        // D-119: an explicitly confirmed same-name replacement consumes the original slot, even with a new row id.
        // Quota rejection precedes removal/sort assignment so every original query and its metadata remain intact.
        if (addsSlot && write.Enforced && write.DemandCapacity(CommercialFeature.SavedFilters, QuotaKind.SavedFilters) is { } maximum)
        {
            write.DemandCount(CommercialFeature.SavedFilters, QuotaKind.SavedFilters, maximum, all.Count);
        }
        filter.SortOrder = sameId?.SortOrder ?? sameName.FirstOrDefault()?.SortOrder ?? (all.Count == 0 ? 0 : all.Max(f => f.SortOrder) + 1);
        db.SavedFilters.RemoveRange(sameName);
        if (sameId is not null)
        {
            var created = sameId.CreatedAt;
            db.Entry(sameId).CurrentValues.SetValues(filter);
            sameId.CreatedAt = created;
        }
        else
        {
            db.SavedFilters.Add(filter);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a saved filter; entries are never affected.</summary>
    public async Task DeleteSavedFilterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.SavedFilters.Where(f => f.Id == id).ExecuteDeleteAsync(cancellationToken);
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
        await db.CategoryRules.Where(r => r.CategoryId == sourceId).ExecuteUpdateAsync(r => r.SetProperty(x => x.CategoryId, targetId), cancellationToken);

        // Category lists move too: the spending cut of a goal plan and the categories of a saved filter.
        static List<Guid> Moved(List<Guid> ids, Guid from, Guid to) => [.. ids.Select(id => id == from ? to : id).Distinct()];
        foreach (var plan in (await db.ContributionPlans.ToListAsync(cancellationToken)).Where(p => p.CategoryIds.Contains(sourceId)))
        {
            plan.CategoryIds = Moved(plan.CategoryIds, sourceId, targetId);
        }

        foreach (var filter in (await db.SavedFilters.ToListAsync(cancellationToken)).Where(f => f.CategoryIds.Contains(sourceId)))
        {
            filter.CategoryIds = Moved(filter.CategoryIds, sourceId, targetId);
        }

        // One level only: children of the source go under the target's main category. Merging a main category into one
        // of its own children makes that child the main category, so nothing stays under the archived source.
        if (target.ParentId == sourceId)
        {
            target.ParentId = null;
        }

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
        var first = siblings.Min(s => s.SortOrder);
        (siblings[index], siblings[other]) = (siblings[other], siblings[index]);
        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = first + i;
        }

        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>
    /// Deletes all Zanance data on this device (SEC-05, BAK-15): entries, plans, budgets, rates, categories, accounts
    /// and settings. Backup files and copies outside the database are not touched.
    /// </summary>
    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.ImportLinks.ExecuteDeleteAsync(cancellationToken);
        await db.AssetEvents.ExecuteDeleteAsync(cancellationToken);
        await db.AssetValuations.ExecuteDeleteAsync(cancellationToken);
        await db.ForecastSnapshots.ExecuteDeleteAsync(cancellationToken);
        await db.AssetTypes.ExecuteDeleteAsync(cancellationToken);
        await db.AssetLocations.ExecuteDeleteAsync(cancellationToken);
        await db.Entries.ExecuteDeleteAsync(cancellationToken);
        await db.Templates.ExecuteDeleteAsync(cancellationToken);
        await db.SavedFilters.ExecuteDeleteAsync(cancellationToken);
        await db.CategoryRules.ExecuteDeleteAsync(cancellationToken);
        await db.Attachments.ExecuteDeleteAsync(cancellationToken);
        await db.GoalAllocations.ExecuteDeleteAsync(cancellationToken);
        await db.ContributionPlans.ExecuteDeleteAsync(cancellationToken);
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
        // Microsoft.Data.Sqlite's async I/O executes synchronously. Materialize large snapshots off the UI thread
        // (D-75; https://learn.microsoft.com/dotnet/standard/data/sqlite/async). Capture the context first, so a queued
        // read stays in the profile that requested it. One worker owns the query; disposal follows its completion.
        return await Task.Run(async () =>
        {
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
        }, cancellationToken);
    }

    /// <summary>Returns one entry.</summary>
    public async Task<LedgerEntry?> GetEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>Returns how many entries wait for review (REC-13), counted in the database instead of loading every entry.</summary>
    public async Task<int> CountUnreviewedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.CountAsync(e => e.Review == ReviewState.Unreviewed, cancellationToken);
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
    public Task<SaveResult> SaveEntriesAsync(
        IReadOnlyList<LedgerEntry> entries,
        IReadOnlyCollection<Guid> deleteIds,
        CancellationToken cancellationToken = default) =>
        SaveEntriesAsync(entries, deleteIds, null, cancellationToken);

    /// <summary>
    /// Saves entries like <see cref="SaveEntriesAsync(IReadOnlyList{LedgerEntry}, IReadOnlyCollection{Guid}, CancellationToken)"/>
    /// and moves the attachments of the deleted entries to <paramref name="moveAttachmentsTo"/> in the same transaction,
    /// so no receipt is left without an entry (split parts that are joined or removed).
    /// </summary>
    public async Task<SaveResult> SaveEntriesAsync(
        IReadOnlyList<LedgerEntry> entries,
        IReadOnlyCollection<Guid> deleteIds,
        Guid? moveAttachmentsTo,
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

        var removed = deleteIds.Count > 0
            ? await db.Entries.Where(e => deleteIds.Contains(e.Id) && !ids.Contains(e.Id)).ToListAsync(cancellationToken)
            : [];
        db.Entries.RemoveRange(removed);

        // Entries and the paid amounts of their occurrences change together or not at all.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, entries.Concat(existing.Values).Concat(removed), cancellationToken);
        if (moveAttachmentsTo is { } target && deleteIds.Count > 0)
        {
            await db.Attachments.Where(a => deleteIds.Contains(a.EntryId)).ExecuteUpdateAsync(a => a.SetProperty(x => x.EntryId, target), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        OnChanged();
        return SaveResult.Success;
    }

    /// <summary>
    /// Deletes an entry together with the entries of its group (a transfer and its fee) and returns them, so that
    /// <see cref="RestoreEntriesAsync"/> can undo the deletion. Refunds linked to a deleted purchase are kept. The money of a
    /// holding purchase or sale is deleted only with its holding event (<see cref="HoldingStore.DeleteEventAsync"/>), so
    /// nothing is deleted here for it.
    /// </summary>
    public Task<IReadOnlyList<LedgerEntry>> DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteEntriesAsync([id], cancellationToken);

    /// <summary>
    /// Deletes several entries with the same rules as <see cref="DeleteEntryAsync"/> – groups go together, holding money
    /// stays – in one transaction, and raises <see cref="Changed"/> once (bulk delete, F2-TX-04). Returns every deleted
    /// entry for undo.
    /// </summary>
    public async Task<IReadOnlyList<LedgerEntry>> DeleteEntriesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var wanted = ids.Distinct().ToList();
        var entries = await db.Entries.Where(e => wanted.Contains(e.Id)).ToListAsync(cancellationToken);
        var groups = entries.Where(e => e.GroupId is not null).Select(e => e.GroupId!.Value).Distinct().ToList();
        var holdingGroups = groups.Count == 0
            ? []
            : await db.AssetEvents.Where(e => e.GroupId != null && groups.Contains(e.GroupId.Value)).Select(e => e.GroupId!.Value).Distinct().ToListAsync(cancellationToken);
        var otherGroups = groups.Except(holdingGroups).ToList();
        var grouped = otherGroups.Count == 0
            ? []
            : await db.Entries.Where(e => e.GroupId != null && otherGroups.Contains(e.GroupId.Value)).ToListAsync(cancellationToken);
        List<LedgerEntry> deleted = [.. entries.Where(e => e.GroupId is null).Concat(grouped).DistinctBy(e => e.Id)];
        if (deleted.Count == 0)
        {
            return [];
        }

        // Refunds of a deleted purchase stay, and the database unlinks them; remember the links so undo restores them.
        var deletedIds = deleted.Select(e => e.Id).ToList();
        var refundLinks = await db.Entries.Where(e => e.RefundOfId != null && deletedIds.Contains(e.RefundOfId.Value) && !deletedIds.Contains(e.Id))
            .Select(e => new { e.Id, Purchase = e.RefundOfId!.Value }).ToListAsync(cancellationToken);
        lock (_refundLinks)
        {
            foreach (var link in refundLinks)
            {
                _refundLinks[link.Id] = link.Purchase;
            }
        }

        db.Entries.RemoveRange(deleted);

        // A deleted settlement reopens its occurrence; automatic posting must not bring it back (REC-18, AT-31). A deleted
        // partial payment only lowers the paid amount (F2-TX-02).
        foreach (var settled in deleted.Where(e => e.ScheduleId is not null && !e.IsPartialPayment))
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

        // The deletion, the reopened occurrences and the paid amounts change together or not at all.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, deleted, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        OnChanged();
        return deleted;
    }

    /// <summary>Re-inserts deleted entries unchanged (undo of <see cref="DeleteEntryAsync"/>).</summary>
    public async Task RestoreEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var list = entries.ToList();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var entry in list)
        {
            if (await db.Entries.AnyAsync(e => e.Id == entry.Id, cancellationToken))
            {
                continue;
            }

            db.Entries.Add(entry);
            if (!entry.IsPartialPayment && entry.ScheduleId is { } scheduleId && entry.OccurrenceDate is { } original)
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

        // Refunds that lost their purchase with the deletion are linked again.
        var restoredIds = list.Select(e => e.Id).ToHashSet();
        List<KeyValuePair<Guid, Guid>> relink;
        lock (_refundLinks)
        {
            relink = [.. _refundLinks.Where(l => restoredIds.Contains(l.Value))];
            foreach (var link in relink)
            {
                _refundLinks.Remove(link.Key);
            }
        }

        foreach (var (refundId, purchaseId) in relink)
        {
            if (await db.Entries.FirstOrDefaultAsync(e => e.Id == refundId, cancellationToken) is { RefundOfId: null } refund)
            {
                refund.RefundOfId = purchaseId;
            }
        }

        // The entries, their occurrences, the refund links and the paid amounts come back together.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, list, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Forgets what the store keeps in memory for the current session (refunds to relink on undo), e.g. when another
    /// local profile is opened, and tells listeners that all data changed.
    /// </summary>
    public void ResetSession()
    {
        lock (_refundLinks)
        {
            _refundLinks.Clear();
        }

        OnChanged();
    }

    // Keeps OccurrenceState.PaidAmount equal to the sum of the partial payments of every touched occurrence (F2-TX-02).
    private static async Task UpdatePaidAmountsAsync(ZananceDbContext db, IEnumerable<LedgerEntry> touched, CancellationToken cancellationToken)
    {
        var keys = touched
            .Where(e => e.IsPartialPayment && e.ScheduleId is not null && e.OccurrenceDate is not null)
            .Select(e => (ScheduleId: e.ScheduleId!.Value, Date: e.OccurrenceDate!.Value))
            .Distinct()
            .ToList();
        if (keys.Count == 0)
        {
            return;
        }

        foreach (var (scheduleId, date) in keys)
        {
            var paid = await db.Entries
                .Where(e => e.ScheduleId == scheduleId && e.OccurrenceDate == date && e.IsPartialPayment)
                .SumAsync(e => e.Amount, cancellationToken);
            var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.OriginalDate == date, cancellationToken);
            if (state is null)
            {
                state = new OccurrenceState { ScheduleId = scheduleId, OriginalDate = date };
                db.OccurrenceStates.Add(state);
            }

            state.PaidAmount = paid;

            // Automatic posting of the full amount would pay twice once part is paid (REC-21).
            state.AutoPostSuppressed |= paid > 0;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns the saved forecast snapshots, newest first (ZEX-S0803).</summary>
    public async Task<List<Core.Forecasts.ForecastSnapshot>> GetForecastSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return [.. (await db.ForecastSnapshots.AsNoTracking().ToListAsync(cancellationToken)).OrderByDescending(s => s.CreatedAt)];
    }

    /// <summary>Saves a new forecast snapshot; snapshots are read-only, so an existing one is never changed (ZEX-AT40).</summary>
    public async Task SaveForecastSnapshotAsync(Core.Forecasts.ForecastSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ForecastSnapshots.AnyAsync(s => s.Id == snapshot.Id, cancellationToken))
        {
            throw new InvalidOperationException("A forecast snapshot cannot be changed.");
        }

        db.ForecastSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a forecast snapshot.</summary>
    public async Task DeleteForecastSnapshotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.ForecastSnapshots.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken);
        OnChanged();
    }
}
