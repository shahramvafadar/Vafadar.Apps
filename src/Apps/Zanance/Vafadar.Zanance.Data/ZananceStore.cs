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
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budget.Id, cancellationToken);
        // D-121: updates retain stored period/currency identity. Never count a caller's ignored changed date/code
        // while the actual saved row still belongs to the current financial period.
        if (write.Enforced)
        {
            var effective = existing is null ? budget : new Budget
            {
                Period = existing.Period, PeriodStart = existing.PeriodStart, Year = existing.Year, Month = existing.Month,
                Calendar = existing.Calendar, CurrencyCode = existing.CurrencyCode, AccountIds = [.. budget.AccountIds],
                Method = budget.Method, Rollover = budget.Rollover,
            };
            await CheckCommercialBudgetSaveAsync(db, write, effective, existing, cancellationToken);
        }
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

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

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
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var replaced = await db.Budgets.FirstOrDefaultAsync(b => b.Id == replacedId, cancellationToken);
        await CheckCommercialBudgetSaveAsync(db, write, budget, replaced, cancellationToken);
        if (replaced is not null)
        {
            // Removed first in its own step, so the new budget of the same period never meets the old one.
            db.Budgets.Remove(replaced);
            await db.SaveChangesAsync(cancellationToken);
        }

        db.Budgets.Add(budget);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        write.EnsureCurrent();
        await transaction.CommitAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Checks actual current-period definitions and newly used methods before any budget/limit mutation.</summary>
    private async Task CheckCommercialBudgetSaveAsync(ZananceDbContext db, CommercialWriteTransaction write,
        Budget target, Budget? original, CancellationToken cancellationToken)
    {
        if (!write.Enforced) return;
        if (!Enum.IsDefined(target.Period) || !Enum.IsDefined(target.Calendar)
            || !Enum.IsDefined(target.Method) || !Enum.IsDefined(target.Rollover))
            throw new ArgumentException("A budget requires known period, calendar, method and rollover values.", nameof(target));
        // Read the existing preference row without creating/updating Settings inside a financial write.
        var startDay = await db.Settings.Select(s => (int?)s.MonthStartDay).FirstOrDefaultAsync(cancellationToken) ?? 1;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        bool Current(Budget b)
        {
            var (first, last) = BudgetPeriods.Range(b, startDay);
            return today >= first && today <= last;
        }
        var targetCurrent = Current(target);
        var scope = write.FinancialScope;
        var key = BudgetDefinitionKey.From(scope, target);
        var sameDefinition = original is not null && BudgetDefinitionKey.From(scope, original) == key;
        var newUse = original is null || !sameDefinition || (targetCurrent && !Current(original));
        var addsAdvanced = (newUse || targetCurrent) &&
            ((target.Method != BudgetMethod.Limits && (newUse || original!.Method == BudgetMethod.Limits))
                || (target.Rollover != BudgetRollover.None && (newUse || original!.Rollover == BudgetRollover.None))
                || (target.Period != BudgetPeriod.Month && (newUse || original!.Period == BudgetPeriod.Month)));
        write.DemandFeature(addsAdvanced ? CommercialFeature.AdvancedBudgets
            : newUse ? CommercialFeature.BasicBudgets : CommercialFeature.Corrections);
        if (!targetCurrent || write.GetMaximum(QuotaKind.BudgetDefinitions) is not { } maximum) return;
        var all = await db.Budgets.AsNoTracking().ToListAsync(cancellationToken);
        var current = all.Where(Current).Select(b => BudgetDefinitionKey.From(scope, b)).ToHashSet();
        var after = all.Where(b => b.Id != original?.Id).Where(Current).Select(b => BudgetDefinitionKey.From(scope, b)).ToHashSet();
        after.Add(key);
        // D-121: period row ids/calendars and copied months are not new definitions. Existing corrections and
        // slot-neutral replacement remain available above quota; historical/future rows do not count as current.
        // Explicit active/read-only selection and activation as a future period arrives are later ENT-02/03 work.
        if (after.Count <= current.Count) return;
        write.DemandCount(CommercialFeature.BasicBudgets, QuotaKind.BudgetDefinitions, maximum, current.Count, after.Count - current.Count);
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
        var match = rule.Match.Trim();
        if (match.Length < Core.Categories.CategoryRules.MinLength)
        {
            throw new ArgumentException("The text of a rule is too short.", nameof(rule));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // D-125: matching and replacement share the actual writer even in unrestricted test builds. Separate
        // providers cannot both read an absent pattern and insert duplicates before the implicit Save transaction.
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        var existing = await db.CategoryRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken);
        var same = (await db.CategoryRules.Where(r => r.Kind == rule.Kind && r.Id != rule.Id).ToListAsync(cancellationToken))
            .Where(r => string.Equals(r.Match, match, StringComparison.CurrentCultureIgnoreCase)).ToList();
        var retained = same.Count > 0 || existing is not null && existing.Kind == rule.Kind
            && string.Equals(existing.Match, match, StringComparison.CurrentCultureIgnoreCase);
        // Changing the category of a retained pattern is a correction; a new pattern/kind configures new automation.
        write.DemandFeature(retained ? CommercialFeature.Corrections : CommercialFeature.CategorizationRules);
        rule.Match = match;
        db.CategoryRules.RemoveRange(same);
        if (existing is not null)
        {
            rule.CreatedAt = existing.CreatedAt;
            db.CategoryRules.Update(rule);
        }
        else
        {
            db.CategoryRules.Add(rule);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        OnChanged();
    }

    /// <summary>Deletes a rule; saved entries keep their categories.</summary>
    public async Task DeleteCategoryRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        write.DemandFeature(CommercialFeature.DeleteData);
        await db.CategoryRules.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
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
        // D-126: classify/validate against the same actual writer snapshot that commits the complete batch.
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);

        return await SaveEntriesUnderWriterAsync(db, write, entries, deleteIds, moveAttachmentsTo, new HashSet<Guid>(), cancellationToken);
    }

    /// <summary>Recomputes an owned advance bill under the same writer that saves its correction entries.</summary>
    public async Task<SaveResult> SaveAdvanceSettlementAsync(Schedule reviewedPlan, DateOnly from, DateOnly to,
        SettlementResult reviewed, string title, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewedPlan);
        ArgumentNullException.ThrowIfNull(reviewed);
        if (from > to || reviewed.Actual < 0) throw new ArgumentException("The settlement requires an ordered period and non-negative actual bill.");
        ArgumentNullException.ThrowIfNull(title);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var plan = await db.Schedules.AsNoTracking().SingleAsync(p => p.Id == reviewedPlan.Id, cancellationToken);
        if (plan.Kind != EntryKind.Expense) throw new InvalidOperationException("Only an expense plan has advance bills.");
        var history = await db.Entries.AsNoTracking().ToListAsync(cancellationToken);
        var result = AdvanceSettlement.Compute(plan, history, from, to, reviewed.Actual);
        // A reviewed bill must not silently acquire changed amounts, refund links or a different account.
        if (System.Text.Json.JsonSerializer.Serialize(plan) != System.Text.Json.JsonSerializer.Serialize(reviewedPlan)
            || result.Paid != reviewed.Paid
            || System.Text.Json.JsonSerializer.Serialize(result.Advances.OrderBy(e => e.Id))
                != System.Text.Json.JsonSerializer.Serialize(reviewed.Advances.OrderBy(e => e.Id)))
            throw new InvalidOperationException("The reviewed advances or plan changed; review the bill again.");
        if (result.Advances.Count == 0) throw new InvalidOperationException("There are no retained advances in this period.");
        // Caller-supplied entry flags cannot grant correction rights; these entries come from reviewed stored advances.
        var created = AdvanceSettlement.CreateEntries(plan, result, history, DateOnly.FromDateTime(time.GetLocalNow().DateTime), title);
        if (created.Count == 0) return SaveResult.Success;
        return await SaveEntriesUnderWriterAsync(db, write, created, [], null, created.Select(e => e.Id).ToHashSet(), cancellationToken);
    }

    /// <summary>Validates the complete ledger batch within its caller's writer; correction grants are private identities.</summary>
    private async Task<SaveResult> SaveEntriesUnderWriterAsync(ZananceDbContext db, CommercialWriteTransaction write,
        IReadOnlyList<LedgerEntry> entries, IReadOnlyCollection<Guid> deleteIds, Guid? moveAttachmentsTo,
        IReadOnlySet<Guid> retainedCorrections, CancellationToken cancellationToken, OccurrenceState? settlement = null)
    {
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var ids = entries.Select(e => e.Id).ToList();
        var existing = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);

        await DemandLedgerSaveAsync(db, write, entries, deleteIds, existing, accounts, categories, retainedCorrections, cancellationToken);

        // D-126: validation sees the final batch, not old refunds that will be edited/deleted or missing peers.
        var purchaseIds = entries.Where(e => e.RefundOfId is not null).Select(e => e.RefundOfId!.Value).Concat(ids).Distinct().ToList();
        var storedRefunds = await db.Entries.AsNoTracking().Where(e => e.Kind == EntryKind.Refund && e.RefundOfId != null
            && purchaseIds.Contains(e.RefundOfId.Value) && !ids.Contains(e.Id) && !deleteIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var finalRefunds = storedRefunds.Concat(entries.Where(e => e.Kind == EntryKind.Refund)).ToList();
        foreach (var entry in entries)
        {
            LedgerEntry? original = null;
            long otherRefunds = 0;
            if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } originalId)
            {
                original = entries.FirstOrDefault(e => e.Id == originalId);
                if (original is null && !deleteIds.Contains(originalId))
                    original = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == originalId, cancellationToken);
                if (original is null) return new SaveResult([LedgerError.RefundOriginalMissing]);
                var otherTotal = finalRefunds.Where(e => e.RefundOfId == originalId && e.Id != entry.Id).Sum(e => (decimal)e.Amount);
                if (otherTotal > long.MaxValue) return new SaveResult([LedgerError.RefundExceedsPurchase]);
                otherRefunds = (long)otherTotal;
            }

            var errors = LedgerValidator.Validate(entry, accounts, categories, original, otherRefunds, isNew: !existing.ContainsKey(entry.Id));
            if (errors.Count > 0)
            {
                return new SaveResult(errors);
            }

            var linked = finalRefunds.Where(e => e.RefundOfId == entry.Id).ToList();
            if (linked.Count > 0)
            {
                if (entry.Kind != EntryKind.Expense) return new SaveResult([LedgerError.RefundOriginalNotExpense]);
                if (linked.Sum(e => (decimal)e.Amount) > entry.Amount) return new SaveResult([LedgerError.RefundExceedsPurchase]);
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
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, entries.Concat(existing.Values).Concat(removed), cancellationToken);
        if (moveAttachmentsTo is { } target && deleteIds.Count > 0)
        {
            await db.Attachments.Where(a => deleteIds.Contains(a.EntryId)).ExecuteUpdateAsync(a => a.SetProperty(x => x.EntryId, target), cancellationToken);
        }

        if (settlement is not null)
        {
            // D-134: the final state shares the ledger writer, including actual partial totals and failure rollback.
            if (db.Entry(settlement).State == EntityState.Detached) db.OccurrenceStates.Add(settlement);
            write.EnsureCurrent();
            await db.SaveChangesAsync(cancellationToken);
        }
        await write.CommitAsync(cancellationToken);

        OnChanged();
        return SaveResult.Success;
    }

    /// <summary>Checks new financial work and complete resulting groups under the caller's actual-file writer.</summary>
    private async Task DemandLedgerSaveAsync(ZananceDbContext db, CommercialWriteTransaction write,
        IReadOnlyList<LedgerEntry> entries, IReadOnlyCollection<Guid> deleteIds,
        IReadOnlyDictionary<Guid, LedgerEntry> existing, IReadOnlyDictionary<Guid, Account> accounts,
        IReadOnlyDictionary<Guid, Category> categories, IReadOnlySet<Guid> retainedCorrections, CancellationToken cancellationToken)
    {
        if (!write.Enforced) return;
        var groups = entries.Where(e => e.GroupId is not null).Select(e => e.GroupId!.Value).Distinct().ToList();
        var savedIds = entries.Select(e => e.Id).ToHashSet();
        var stored = groups.Count == 0 ? []
            : await db.Entries.AsNoTracking().Where(e => e.GroupId != null && groups.Contains(e.GroupId.Value)).ToListAsync(cancellationToken);
        var holdingGroups = groups.Count == 0 ? []
            : await db.AssetEvents.Where(e => e.GroupId != null && groups.Contains(e.GroupId.Value)).Select(e => e.GroupId!.Value).Distinct().ToListAsync(cancellationToken);
        var retainedFees = new HashSet<Guid>();
        foreach (var group in groups)
        {
            var before = stored.Where(e => e.GroupId == group).ToList();
            var incoming = entries.Where(e => e.GroupId == group).ToList();
            var after = before.Where(e => !savedIds.Contains(e.Id) && !deleteIds.Contains(e.Id)).Concat(incoming).ToList();
            var beforeIds = before.Select(e => e.Id).ToHashSet();
            if (holdingGroups.Contains(group))
            {
                // Holding payment/fee groups are not category splits. New work still needs their holding right.
                if (incoming.Any(e => !existing.ContainsKey(e.Id))) write.DemandFeature(CommercialFeature.ManageHoldings);
                continue;
            }

            if (EntryActions.IsSplit(after) && (!EntryActions.IsSplit(before) || after.Any(e => !beforeIds.Contains(e.Id))))
                write.DemandFeature(CommercialFeature.SplitTransactions);
            foreach (var transfer in after.Where(e => e.Kind == EntryKind.Transfer && before.Any(old => old.Id == e.Id && old.Kind == EntryKind.Transfer)))
            {
                // Adding/correcting fees belongs to the retained transfer, rather than a new split or money event.
                foreach (var fee in incoming.Where(e => e.Kind == EntryKind.Expense
                    && (e.AccountId == transfer.AccountId || e.AccountId == transfer.ToAccountId))) retainedFees.Add(fee.Id);
            }
        }

        var newWork = new List<Guid>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var closing = await RetainedDebtClosingAsync(db, entries, deleteIds, existing, accounts, categories,
            stored, holdingGroups, today, cancellationToken);
        foreach (var entry in entries.Where(e => !existing.ContainsKey(e.Id) && !retainedFees.Contains(e.Id)
            && !retainedCorrections.Contains(e.Id) && e.Kind is not (EntryKind.Adjustment or EntryKind.Refund or EntryKind.IncomeReversal)))
        {
            if (await IsRetainedOverduePaymentAsync(db, entry, today, cancellationToken)) continue;
            var requested = entry.Kind == EntryKind.Transfer && entry.ToAccountId is { } destination
                ? new[] { entry.AccountId, destination } : new[] { entry.AccountId };
            newWork.AddRange(requested.Where(id => !closing.TryGetValue(entry.Id, out var allowed) || !allowed.Contains(id)));
        }
        if (newWork.Count > 0)
            write.DemandSelectedAccounts(CommercialFeature.Transactions, accounts,
                newWork);
    }

    /// <summary>Checks original rules, state and paid rows rather than trusting a draft's schedule markers.</summary>
    private static async Task<bool> IsRetainedOverduePaymentAsync(ZananceDbContext db, LedgerEntry entry,
        DateOnly today, CancellationToken cancellationToken)
    {
        if (entry.Source != EntrySource.Schedule || entry.Review != ReviewState.Confirmed
            || entry.ScheduleId is not { } scheduleId || entry.OccurrenceDate is not { } originalDate) return false;
        var plan = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(p => p.Id == scheduleId, cancellationToken);
        if (plan is null || !plan.Owns(originalDate) || plan.Kind != entry.Kind || plan.AccountId != entry.AccountId
            || (entry.Kind == EntryKind.Transfer && plan.ToAccountId != entry.ToAccountId)
            || !Recurrence.Between(plan.Rule, originalDate, originalDate).Any()) return false;
        var state = await db.OccurrenceStates.AsNoTracking().FirstOrDefaultAsync(s => s.ScheduleId == scheduleId
            && s.OriginalDate == originalDate, cancellationToken);
        if ((state is not null && state.Status != OccurrenceStatus.Open)
            || (state?.DueDate ?? plan.Rule.ApplyWeekend(originalDate)) >= today) return false;
        // A missing/obsolete state must not turn a completed settlement into another retained payment.
        return !await db.Entries.AnyAsync(e => e.ScheduleId == scheduleId && e.OccurrenceDate == originalDate
            && !e.IsPartialPayment, cancellationToken);
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
        var file = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.DeleteData);
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

        // D-127: return exact refund links with the file-bound deletion; receipts retain their existing orphan policy.
        var deletedIds = deleted.Select(e => e.Id).ToList();
        var refunds = await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && deletedIds.Contains(e.RefundOfId.Value)
            && !deletedIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var refundLinks = refunds.Select(refund =>
        {
            var expected = refund.Copy();
            expected.RefundOfId = null; // SQLite's delete action unlinks the retained row; all its other metadata must remain.
            return new DeletedRefundLink(refund.Id, refund.RefundOfId!.Value, expected);
        }).ToList();
        var accountIds = deleted.Concat(refunds).SelectMany(e => e.ToAccountId is { } destination
            ? new[] { e.AccountId, destination } : new[] { e.AccountId }).Distinct().ToList();
        var currencies = await db.Accounts.AsNoTracking().Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.CurrencyCode, cancellationToken);
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
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, deleted, cancellationToken);
        await write.CommitAsync(cancellationToken);

        OnChanged();
        return new DeletedLedgerEntries(file, deleted, refundLinks, currencies);
    }

    /// <summary>Re-inserts deleted entries unchanged (undo of <see cref="DeleteEntryAsync"/>).</summary>
    public async Task RestoreEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var file = Path.GetFullPath(db.Database.GetDbConnection().DataSource);
        var deleted = entries as DeletedLedgerEntries;
        if (deleted is not null && !string.Equals(deleted.File, file, StringComparison.Ordinal))
            throw new InvalidOperationException("The deleted entries belong to another database.");
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var list = entries.DistinctBy(e => e.Id).ToList();
        var ids = list.Select(e => e.Id).ToList();
        var existing = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);
        var missing = list.Where(e => !existing.ContainsKey(e.Id)).ToList();
        if (missing.Count == 0) return;
        if (deleted is not null) await ValidateDeletionSnapshotAsync(db, deleted, list, existing, cancellationToken);
        var restoredIds = missing.Select(e => e.Id).ToHashSet();
        foreach (var entry in missing)
        {
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
        var relink = deleted?.RefundLinks.Where(l => restoredIds.Contains(l.PurchaseId)).ToList() ?? [];
        foreach (var link in relink)
        {
            if (await db.Entries.FirstOrDefaultAsync(e => e.Id == link.RefundId, cancellationToken) is { RefundOfId: null } refund)
                refund.RefundOfId = link.PurchaseId;
        }

        // Entries, occurrences, explicit refund links and paid amounts come back together; receipt rows stay intact.
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await UpdatePaidAmountsAsync(db, list, cancellationToken);
        await write.CommitAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Rejects an obsolete short Undo before inserting rows or reapplying its explicit refund relationships.</summary>
    private static async Task ValidateDeletionSnapshotAsync(ZananceDbContext db, DeletedLedgerEntries deleted,
        IReadOnlyList<LedgerEntry> entries, IReadOnlyDictionary<Guid, LedgerEntry> existing, CancellationToken cancellationToken)
    {
        // D-129: partial recovery must not combine old missing rows with a newly edited sibling.
        if (entries.Any(e => existing.TryGetValue(e.Id, out var current) && !SameDeletedEntry(current, e)))
            throw new InvalidOperationException("The deleted group changed after deletion; Undo requires review.");

        var accountIds = deleted.Currencies.Keys.ToList();
        var currencies = await db.Accounts.AsNoTracking().Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.CurrencyCode, cancellationToken);
        if (deleted.Currencies.Any(pair => !currencies.TryGetValue(pair.Key, out var currency)
            || !string.Equals(currency, pair.Value, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("An account currency changed after deletion; Undo requires review.");

        var refundIds = deleted.RefundLinks.Where(link => !existing.ContainsKey(link.PurchaseId)).Select(link => link.RefundId).ToList();
        var refunds = await db.Entries.AsNoTracking().Where(e => refundIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);
        foreach (var link in deleted.RefundLinks.Where(link => !existing.ContainsKey(link.PurchaseId)))
        {
            if (!refunds.TryGetValue(link.RefundId, out var current) || !SameDeletedEntry(current, link.Expected))
                throw new InvalidOperationException("A retained refund changed after deletion; Undo requires review.");
        }
    }

    /// <summary>Compares complete semantic metadata and creation identity, allowing only subsequent audit updates.</summary>
    private static bool SameDeletedEntry(LedgerEntry current, LedgerEntry expected) =>
        current.CreatedAt == expected.CreatedAt && SameEntry(current, expected);

    /// <summary>An explicit stored refund relationship captured by a committed deletion, never guessed from amounts.</summary>
    /// <param name="RefundId">The retained refund entry.</param>
    /// <param name="PurchaseId">The deleted purchase it referenced.</param>
    /// <param name="Expected">Complete expected unlinked refund metadata for rejecting later edits.</param>
    private sealed record DeletedRefundLink(Guid RefundId, Guid PurchaseId, LedgerEntry Expected);

    /// <summary>Retains the original file, entries and explicit refund links for the application's short Undo offer.</summary>
    private sealed class DeletedLedgerEntries(string file, IReadOnlyList<LedgerEntry> entries,
        IReadOnlyList<DeletedRefundLink> refundLinks, IReadOnlyDictionary<Guid, string> currencies) : IReadOnlyList<LedgerEntry>
    {
        private readonly IReadOnlyList<LedgerEntry> _entries = entries.Select(entry => entry.Copy()).ToArray();

        /// <summary>Gets the original currencies; an empty account cannot relabel old minor units during Undo.</summary>
        public IReadOnlyDictionary<Guid, string> Currencies { get; } = currencies;

        /// <summary>Gets the actual database file that committed the deletion.</summary>
        public string File { get; } = file;

        /// <summary>Gets explicit retained refund links for retry without session-global mutable state.</summary>
        public IReadOnlyList<DeletedRefundLink> RefundLinks { get; } = refundLinks;

        /// <summary>Gets the number of deleted entries.</summary>
        public int Count => _entries.Count;

        /// <summary>Gets a deleted entry in its original order.</summary>
        public LedgerEntry this[int index] => _entries[index].Copy();

        /// <summary>Enumerates the unchanged deleted entries.</summary>
        public IEnumerator<LedgerEntry> GetEnumerator() => _entries.Select(entry => entry.Copy()).GetEnumerator();

        /// <summary>Enumerates the unchanged deleted entries.</summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Tells listeners that the selected local profile changed. The application dismisses its short Undo offers;
    /// any retained deletion snapshot remains bound to its original database rather than session-global state.
    /// </summary>
    public void ResetSession()
    {
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
