using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>One original financial input packet for report projection and portable PDF export.</summary>
public sealed record ReportInputs
{
    /// <summary>Gets the stored settings row; absence uses nonpersisted defaults and never creates a row.</summary>
    public ZananceSettings? Settings { get; init; }
    /// <summary>Gets complete original accounts, never filtered by new-work quotas.</summary>
    public List<Account> Accounts { get; init; } = [];
    /// <summary>Gets complete original entries, never filtered by new-work quotas.</summary>
    public List<LedgerEntry> Entries { get; init; } = [];
    /// <summary>Gets complete original categories, never filtered by new-work quotas.</summary>
    public List<Category> Categories { get; init; } = [];
    /// <summary>Gets complete original schedules, never filtered by new-work quotas.</summary>
    public List<Schedule> Schedules { get; init; } = [];
    /// <summary>Gets complete original occurrencestates, never filtered by new-work quotas.</summary>
    public List<OccurrenceState> OccurrenceStates { get; init; } = [];
    /// <summary>Gets complete original budgets, never filtered by new-work quotas.</summary>
    public List<Budget> Budgets { get; init; } = [];
    /// <summary>Gets complete original exchangerates, never filtered by new-work quotas.</summary>
    public List<ExchangeRate> ExchangeRates { get; init; } = [];
    /// <summary>Gets complete original goals, never filtered by new-work quotas.</summary>
    public List<Goal> Goals { get; init; } = [];
    /// <summary>Gets complete original goalallocations, never filtered by new-work quotas.</summary>
    public List<GoalAllocation> GoalAllocations { get; init; } = [];
    /// <summary>Gets complete original contributionplans, never filtered by new-work quotas.</summary>
    public List<ContributionPlan> ContributionPlans { get; init; } = [];
    /// <summary>Gets complete original assettypes, never filtered by new-work quotas.</summary>
    public List<AssetType> AssetTypes { get; init; } = [];
    /// <summary>Gets complete original assetevents, never filtered by new-work quotas.</summary>
    public List<AssetEvent> AssetEvents { get; init; } = [];
    /// <summary>Gets complete original assetvaluations, never filtered by new-work quotas.</summary>
    public List<AssetValuation> AssetValuations { get; init; } = [];
    /// <summary>Reads every original report source inside the caller's single deferred reader.</summary>
    internal static async Task<ReportInputs> ReadAsync(ZananceDbContext db, CancellationToken cancellationToken) => new()
    {
        Settings = await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken),
            Accounts = await db.Accounts.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            Entries = await db.Entries.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            Categories = await db.Categories.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            Schedules = await db.Schedules.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            OccurrenceStates = await db.OccurrenceStates.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            Budgets = await db.Budgets.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            ExchangeRates = await db.ExchangeRates.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            Goals = await db.Goals.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            GoalAllocations = await db.GoalAllocations.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            ContributionPlans = await db.ContributionPlans.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            AssetTypes = await db.AssetTypes.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            AssetEvents = await db.AssetEvents.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken),
            AssetValuations = await db.AssetValuations.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken)
    };
}

/// <summary>Original report inputs, the reviewed day and cached rights of the actual opened file.</summary>
public sealed class ReportReadReview
{
    private readonly CommercialFileAccess _access;
    private readonly TimeProvider _time;
    private readonly string _packet;
    /// <summary>Retains the exact opened file for native and original contribution-writer validation.</summary>
    internal readonly string DatabasePath;
    /// <summary>Captures original financial inputs before any analytical or native export await.</summary>
    internal ReportReadReview(string path, CommercialFileAccess access, TimeProvider time, DateOnly day, ReportInputs inputs)
    { DatabasePath = path; _access = access; _time = time; Day = day; Inputs = inputs; _packet = JsonSerializer.Serialize(inputs); }
    /// <summary>Gets the unfiltered original financial packet.</summary>
    public ReportInputs Inputs { get; }
    /// <summary>Gets the local day used by the original calculations and PDF creation label.</summary>
    public DateOnly Day { get; }
    /// <summary>Gets cached feature permission independently of Simple/Advanced presentation mode.</summary>
    public FeaturePermission Permission(CommercialFeature feature) => _access.Context is { } context ? PlanPolicy.Check(feature, context) : FeaturePermission.Allowed;
    /// <summary>Rejects a changed packet, day or access snapshot before publishing analytical work.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (DateOnly.FromDateTime(_time.GetLocalNow().DateTime) != Day || _packet != JsonSerializer.Serialize(Inputs))
            throw new InvalidOperationException("The reviewed report inputs changed.");
    }
    /// <summary>Demands the actual operation before its original calculators run.</summary>
    public void Demand(CommercialFeature feature) { EnsureCurrent(); _access.DemandFeature(feature); }
    /// <summary>Guards original package projection; base totals and essential data status remain available.</summary>
    public T Project<T>(int package, Func<ReportInputs, T> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        if (package is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(package));
        Demand(package == 5 ? CommercialFeature.Warnings : package == 0 ? CommercialFeature.BasicReports : CommercialFeature.AdvancedReports);
        var result = projection(Inputs); EnsureCurrent(); return result;
    }
    /// <summary>Checks export scope before authentication or rendering; basic owned-data PDF remains available.</summary>
    public void DemandPdf(bool advanced)
    {
        Demand(CommercialFeature.BasicReports);
        if (advanced) { Demand(CommercialFeature.AdvancedReports); Demand(CommercialFeature.AdvancedPdf); }
    }
    /// <summary>Computes the original budget carry from this coherent packet without a second database read.</summary>
    public BudgetCarry BudgetCarry(Budget budget)
    {
        Demand(CommercialFeature.BasicReports);
        return ZananceStore.CalculateBudgetCarry(budget, ZananceStore.BudgetCarryChain(budget, Inputs.Budgets),
            Inputs.Settings?.MonthStartDay ?? 1, Inputs.Accounts, Inputs.Entries, Inputs.Categories);
    }
    /// <summary>Validates original analytical inputs under the actual contribution writer, excluding only its own accepted rule after Save.</summary>
    internal async Task ValidateContributionWriterAsync(ZananceDbContext db, Guid? acceptedGoal, CancellationToken cancellationToken)
    {
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The report suggestion belongs to another file.");
        Demand(CommercialFeature.AdvancedReports);
        var current = await ReportInputs.ReadAsync(db, cancellationToken);
        if (acceptedGoal is null) Match(current);
        else
        {
            var original = Inputs with { ContributionPlans = Inputs.ContributionPlans.Where(item => item.GoalId != acceptedGoal).ToList() };
            var remaining = current with { ContributionPlans = current.ContributionPlans.Where(item => item.GoalId != acceptedGoal).ToList() };
            if (JsonSerializer.Serialize(original) != JsonSerializer.Serialize(remaining)) throw new InvalidOperationException("The report suggestion inputs changed during Save.");
        }
        EnsureCurrent();
    }
    /// <summary>Matches current database data to the immutable original packet.</summary>
    internal void Match(ReportInputs current)
    { EnsureCurrent(); if (_packet != JsonSerializer.Serialize(current)) throw new InvalidOperationException("The original report data changed; review it again."); }
}

/// <summary>Captures and revalidates complete report sources without writes, profile switching or feature activation.</summary>
public sealed class ReportWorkStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source, TimeProvider time)
{
    /// <summary>Allows explicit fresh reads only in the original displayed file, without retaining old rights.</summary>
    public async Task ValidateFileAsync(ReportReadReview review, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (review.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original report file changed.");
    }
    /// <summary>Captures all original report inputs and exact-file cached rights in one SQLite reader.</summary>
    public async Task<ReportReadReview> ReadAsync(CancellationToken cancellationToken = default, Func<string, bool>? acceptFile = null)
    {
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(source, path);
        access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.BasicReports);
        if (acceptFile?.Invoke(path) == false) throw new InvalidOperationException("The original report cannot follow another file.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var review = new ReportReadReview(path, access, time, day, await ReportInputs.ReadAsync(db, cancellationToken));
            review.EnsureCurrent(); return review;
        }, cancellationToken);
    }
    /// <summary>Rejects retired actual-file data or access after native authentication, rendering and cache awaits.</summary>
    public async Task ValidateAsync(ReportReadReview review, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.EnsureCurrent();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (review.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The reviewed report file changed.");
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
        review.Match(await ReportInputs.ReadAsync(db, cancellationToken));
    }
}
