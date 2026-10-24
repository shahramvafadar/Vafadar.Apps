using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    List<AssetType> AssetTypes, List<AssetEvent> AssetEvents, Budget? MonthBudget, BudgetCarry Carry, int PeriodIndex)
{
    /// <summary>Gets the transactionally captured local revision of Home's financial source tables.</summary>
    internal string FinancialRevision { get; init; } = string.Empty;

    /// <summary>Gets the captured actual file so one covered refresh cannot move to another selected profile.</summary>
    public string DatabasePath => QuickResources.TemplateChoice.DatabasePath;
}

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
            // D-286: retain the operation-local 32 MiB cache for the complete captured read. D-303 reads
            // ledger pages sequentially instead of revisiting them through the date index.
            // This is SQLite's cache target, not a process-memory cap or a persistent database setting.
            // Ordinary pooled callers retain their original allowance so it cannot leak to later operations.
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            if (!new SqliteConnectionStringBuilder(connection.ConnectionString).Pooling)
                await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -32768", cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var financialRevision = await ReadHomeFinancialRevisionAsync(db, cancellationToken);
            var quick = await GetQuickResourceReviewAsync(db, null, cancellationToken);
            var plans = await new PlanStore(contextFactory, this, commercialAccess).GetWorkSnapshotAsync(db, cancellationToken);
            var goals = await new GoalStore(contextFactory, commercialAccess).GetWorkSnapshotAsync(db, cancellationToken);
            var entries = await ReadCompleteHomeLedgerAsync(db, cancellationToken);
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
            return new HomeReadSnapshot(quick, plans, goals, entries, budgets, rates, types, events, budget, carry, periodIndex)
            { FinancialRevision = financialRevision };
        }, cancellationToken);
    }

    /// <summary>Reads each ledger page in physical order, then retains the original newest-first financial packet.</summary>
    internal static async Task<List<LedgerEntry>> ReadCompleteHomeLedgerAsync(ZananceDbContext db, CancellationToken ct)
    {
        // The date index visits full encrypted rows out of page order. A reverse table scan avoids repeated
        // decryption, and supplies the same descending row identity used by the original reverse date scan
        // when both visible sort keys tie. Stable managed ordering needs no SQLite temporary sort file.
        var entries = await CompleteHomeLedgerReader.ReadAsync(db, ct);
        ct.ThrowIfCancellationRequested();
        var ordered = entries.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt).ToList();
        ct.ThrowIfCancellationRequested();
        return ordered;
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
            (snapshot.Plans.Access ?? throw new InvalidOperationException("Original Home plan authority is missing.")).EnsureSnapshotCurrent();
            (snapshot.Goals.Access ?? throw new InvalidOperationException("Original Home goal authority is missing.")).EnsureSnapshotCurrent();
            void RequireOriginalInputs()
            {
                var quick = snapshot.QuickResources;
                var path = DatabaseFileIdentity.ConfiguredPath(db);
                if (snapshot.Plans.DatabasePath != path || snapshot.Goals.DatabasePath != path
                    || quick.TemplateChoice.DatabasePath != path || quick.FilterChoice.DatabasePath != path)
                    throw new InvalidOperationException("The original Home refresh belongs to another opened file.");
                snapshot.Plans.Access.DemandFeature(Core.Commerce.CommercialFeature.History);
                snapshot.Goals.Access.DemandFeature(Core.Commerce.CommercialFeature.History);
                snapshot.Plans.Access.EnsureSnapshotCurrent(); snapshot.Goals.Access.EnsureSnapshotCurrent();
                quick.TemplateChoice.Access.EnsureSnapshotCurrent(); quick.FilterChoice.Access.EnsureSnapshotCurrent();
                if (PlanWorkSnapshot.CaptureInputs(snapshot.Plans.Schedules, snapshot.Plans.States) != snapshot.Plans.InputStamp
                    || GoalWorkSnapshot.CaptureInputs(snapshot.Goals.Goals, snapshot.Goals.ContributionPlans, snapshot.Goals.Allocations) != snapshot.Goals.InputStamp
                    || QuickResourceReview.Capture(quick.Templates, quick.Filters, quick.Accounts, quick.Categories, quick.Settings, quick.SourceEntry) != quick.InputStamp)
                    throw new InvalidOperationException("The original Home inputs changed in memory; reload them.");
            }
            // D-398: no persisted-source change may hide a different-file, retired-access or altered-input refusal.
            RequireOriginalInputs();
            // D-509: definitions alone cannot detect a ledger/budget/rate/holding write after the read.
            // A local trigger revision changes in the writer's transaction, without reading all ledger rows twice.
            if (await ReadHomeFinancialRevisionAsync(db, cancellationToken) != snapshot.FinancialRevision)
                throw new HomeSnapshotSupersededException();
            try
            {
                await PlanStore.ValidateWorkSnapshotAsync(snapshot.Plans, db, cancellationToken, homeRefresh: true);
                await GoalStore.ValidateWorkSnapshotAsync(snapshot.Goals, db, cancellationToken, homeRefresh: true);
                await ValidateQuickResourceReviewAsync(snapshot.QuickResources, db, cancellationToken, homeRefresh: true);
            }
            catch (HomeSnapshotSupersededException)
            {
                RequireOriginalInputs();
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>Reads local-only SQLite bookkeeping under the original complete-read/validation transaction.</summary>
    private static async Task<string> ReadHomeFinancialRevisionAsync(ZananceDbContext db, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT Revision FROM HomeFinancialRevision WHERE Id = 1";
        var revision = await command.ExecuteScalarAsync(ct) as string;
        if (!Guid.TryParseExact(revision, "N", out _))
            throw new InvalidOperationException("The original Home financial source revision is unavailable.");
        return revision;
    }
}
