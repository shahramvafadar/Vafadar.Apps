using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Data;

/// <summary>One complete Home read, retaining original resource reviews and unfiltered financial history.</summary>
/// <param name="QuickResources">Original templates, choices, settings, accounts and categories.</param>
/// <param name="Plans">Original complete plan rules, occurrence states and exact-file work rights.</param>
/// <param name="Goals">Original complete goals, allocations, contributions and exact-file work rights.</param>
/// <param name="Entries">Complete ledger history in its original newest-first order.</param>
/// <param name="Budgets">All original budgets, including the consecutive rollover history.</param>
/// <param name="Rates">Complete newest-first rates only when Home uses multi-currency valuation.</param>
/// <param name="AssetTypes">All original holding types in their display order.</param>
/// <param name="AssetEvents">Complete original holding history, oldest first.</param>
/// <param name="MonthBudget">The same original monthly budget query used by Home.</param>
/// <param name="Carry">The original carry calculation from the same complete financial snapshot.</param>
/// <param name="PeriodIndex">The original current or previous period requested by the page.</param>
public sealed record HomeReadSnapshot(QuickResourceReview QuickResources, PlanWorkSnapshot Plans,
    GoalWorkSnapshot Goals, List<LedgerEntry> Entries, List<Budget> Budgets, List<ExchangeRate> Rates,
    List<AssetType> AssetTypes, List<AssetEvent> AssetEvents, Budget? MonthBudget, BudgetCarry Carry, int PeriodIndex);

public sealed partial class ZananceStore
{
    /// <summary>Captures Home's complete original sources with one keyed connection and one deferred SQLite reader.</summary>
    public async Task<HomeReadSnapshot> GetHomeSnapshotAsync(DateOnly today, int periodIndex,
        CancellationToken cancellationToken = default)
    {
        if (periodIndex is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(periodIndex));
        // Capture the factory lease before queuing work, so profile maintenance cannot redirect this read.
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            // D-283: SQLite async APIs execute synchronously. Both key derivation and materialization belong
            // off the UI thread; keep the unchanged KDF and non-pooled handle scoped to this operation only.
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var quick = await GetQuickResourceReviewAsync(db, null, cancellationToken);
            var plans = await new PlanStore(contextFactory, this, commercialAccess).GetWorkSnapshotAsync(db, cancellationToken);
            var goals = await new GoalStore(contextFactory, commercialAccess).GetWorkSnapshotAsync(db, cancellationToken);
            var entries = await db.Entries.AsNoTracking().OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);
            var budgets = await db.Budgets.AsNoTracking().OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ToListAsync(cancellationToken);
            var settings = quick.Settings ?? new Core.Settings.ZananceSettings();
            var currencies = LedgerCalculator.InScope(quick.Accounts, null).Where(account => !account.IsArchived)
                .Select(account => account.CurrencyCode).Distinct(StringComparer.Ordinal).Count();
            // Match the original Home valuation boundary; unrelated rate history is not a dashboard input.
            var rates = settings.ValuationCurrencyEnabled && currencies > 1
                ? await db.ExchangeRates.AsNoTracking().OrderByDescending(r => r.Date).ToListAsync(cancellationToken)
                : new List<ExchangeRate>();
            var types = await db.AssetTypes.AsNoTracking().OrderBy(t => t.IsArchived).ThenBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(cancellationToken);
            var events = (await db.AssetEvents.AsNoTracking().ToListAsync(cancellationToken)).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();
            var (year, month) = PeriodMath.MonthOf(today, settings.BudgetCalendar, settings.MonthStartDay);
            if (periodIndex == 1) (year, month) = PeriodMath.Previous(year, month);
            var currency = BudgetCurrency.Resolve(settings, quick.Accounts);
            // Keep the original SQL selection, including its behavior with several account-set definitions.
            var budget = await db.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Period == BudgetPeriod.Month
                && b.Year == year && b.Month == month && b.Calendar == settings.BudgetCalendar && b.CurrencyCode == currency, cancellationToken);
            var carry = budget is null ? BudgetCarry.None : CalculateBudgetCarry(budget, BudgetCarryChain(budget, budgets),
                settings.MonthStartDay, quick.Accounts.ToList(), entries, quick.Categories.ToList());
            await ValidateQuickResourceReviewAsync(quick, db, cancellationToken);
            // Both original readers must retain their actual-file authority; absence cannot authorize publication.
            var planAccess = plans.Access ?? throw new InvalidOperationException("Original Home plan authority is missing.");
            var goalAccess = goals.Access ?? throw new InvalidOperationException("Original Home goal authority is missing.");
            planAccess.EnsureSnapshotCurrent(); goalAccess.EnsureSnapshotCurrent();
            return new HomeReadSnapshot(quick, plans, goals, entries, budgets, rates, types, events, budget, carry, periodIndex);
        }, cancellationToken);
    }

    /// <summary>Revalidates original file, definitions and work rights before the covered Home becomes actionable.</summary>
    public async Task ValidateHomeSnapshotAsync(HomeReadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateQuickResourceReviewAsync(snapshot.QuickResources, db, cancellationToken);
            await PlanStore.ValidateWorkSnapshotAsync(snapshot.Plans, db, cancellationToken);
            await GoalStore.ValidateWorkSnapshotAsync(snapshot.Goals, db, cancellationToken);
        }, cancellationToken);
    }
}
