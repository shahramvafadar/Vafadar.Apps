using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Coherent original inputs and exact-file access for forecast computation and retained saved data.</summary>
public sealed class ForecastReadSnapshot
{
    private readonly CommercialFileAccess _access;
    /// <summary>Retains the original configured file for validation and reviewed writer ownership.</summary>
    internal readonly string DatabasePath;
    /// <summary>Retains original rows without filtering balances, rewriting rules or granting paid access.</summary>
    internal ForecastReadSnapshot(string path, CommercialFileAccess access, ZananceSettings settings,
        List<Account> accounts, List<LedgerEntry> entries, List<Schedule> schedules, List<OccurrenceState> states,
        List<ForecastSnapshot> snapshots)
    {
        DatabasePath = path; _access = access; Settings = settings; Accounts = accounts; Entries = entries;
        Schedules = schedules; States = states; Snapshots = snapshots;
    }
    /// <summary>Gets the original display/financial month settings, with unsaved defaults when absent.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets complete original accounts, including retained read-only history.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets complete original ledger inputs.</summary>
    public IReadOnlyList<LedgerEntry> Entries { get; }
    /// <summary>Gets complete original plans for the established forecast calculator.</summary>
    public IReadOnlyList<Schedule> Schedules { get; }
    /// <summary>Gets original occurrence overrides and settlement relationships.</summary>
    public IReadOnlyList<OccurrenceState> States { get; }
    /// <summary>Gets retained immutable saved forecasts, newest first, irrespective of analytical access.</summary>
    public IReadOnlyList<ForecastSnapshot> Snapshots { get; }
    /// <summary>Gets the approved original operation permission; inactive test builds remain unrestricted.</summary>
    public FeaturePermission Permission(CommercialFeature feature) => _access.Context is { } context
        ? PlanPolicy.Check(feature, context) : FeaturePermission.Allowed;
    /// <summary>Rejects retired cached access before publication, including inactive-to-active changes.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Checks the actual requested computation without changing the selected horizon or scenario.</summary>
    public void Demand(CommercialFeature feature) { EnsureCurrent(); _access.DemandFeature(feature); }
    /// <summary>Calls the original calculator; Free retains exactly the named calendar's financial-month-end horizon.</summary>
    public IReadOnlyList<CurrencyForecast> Compute(int horizonIndex, ForecastScenario scenario, PeriodCalendar calendar, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (horizonIndex is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(horizonIndex));
        Demand(horizonIndex == 0 && scenario.IsEmpty ? CommercialFeature.BasicForecast : CommercialFeature.AdvancedForecast);
        var horizon = Horizon(horizonIndex, calendar, day);
        var result = ForecastCalculator.Compute(Accounts, Entries, Schedules, States, day, horizon, scenario: scenario);
        EnsureCurrent(); return result;
    }
    /// <summary>Returns the original actual horizon, independent of UI language and commercial presentation mode.</summary>
    public DateOnly Horizon(int index, PeriodCalendar calendar, DateOnly day)
    {
        var (year, month) = PeriodMath.MonthOf(day, calendar, Settings.MonthStartDay);
        return index switch { 0 => PeriodMath.MonthRange(year, month, calendar, Settings.MonthStartDay).Last,
            1 => day.AddDays(30), 2 => day.AddDays(90), _ => throw new ArgumentOutOfRangeException(nameof(index)) };
    }
    /// <summary>Computes new analytical comparison only with access; retained saved values remain independently readable.</summary>
    public SnapshotComparison Compare(ForecastSnapshot snapshot, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(snapshot); Demand(CommercialFeature.AdvancedForecast);
        if (!Snapshots.Any(item => item.Id == snapshot.Id)) throw new InvalidOperationException("The saved forecast belongs to another read.");
        var result = SnapshotComparer.Compare(snapshot, Accounts, Entries, day); EnsureCurrent(); return result;
    }
    /// <summary>Rejects an original reviewed computation targeting a different current writer file.</summary>
    internal void DemandWriterFile(ZananceDbContext db, CommercialFeature feature = CommercialFeature.AdvancedForecast)
    {
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The forecast review belongs to another file.");
        Demand(feature);
    }
}

/// <summary>Reads original forecast inputs in one deferred SQLite reader, without settings creation or a write lock.</summary>
public sealed class ForecastWorkStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source)
{
    /// <summary>Captures original file/access before awaits and publishes only a complete coherent history packet.</summary>
    public async Task<ForecastReadSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(source, path);
        access.DemandFeature(CommercialFeature.History);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
        var settings = await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken) ?? new ZananceSettings();
        var accounts = await db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var entries = await db.Entries.AsNoTracking().ToListAsync(cancellationToken);
        var schedules = await db.Schedules.AsNoTracking().OrderBy(item => item.State).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        var states = await db.OccurrenceStates.AsNoTracking().ToListAsync(cancellationToken);
        var snapshots = await db.ForecastSnapshots.AsNoTracking().OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        access.EnsureSnapshotCurrent(); return new(path, access, settings, accounts, entries, schedules, states, snapshots);
    }
    /// <summary>Rechecks the actual current factory file and captured access before a native prompt result is used.</summary>
    public async Task ValidateAsync(ForecastReadSnapshot reviewed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (reviewed.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original forecast file changed.");
        reviewed.EnsureCurrent();
    }
}
