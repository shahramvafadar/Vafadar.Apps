using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ForecastSourceFacts _facts;
    private readonly string _packet, _displaySettings;
    private readonly Dictionary<Guid, string> _savedPackets;
    /// <summary>Retains the original configured file for validation and reviewed writer ownership.</summary>
    public string DatabasePath { get; }
    /// <summary>Retains original rows without filtering balances, rewriting rules or granting paid access.</summary>
    internal ForecastReadSnapshot(string path, CommercialFileAccess access, ZananceSettings? settings,
        List<Account> accounts, List<LedgerEntry> entries, List<Schedule> schedules, List<OccurrenceState> states,
        List<ForecastSnapshot> snapshots)
    {
        DatabasePath = path; _access = access; Settings = settings ?? new ZananceSettings(); Accounts = accounts; Entries = entries;
        Schedules = schedules; States = states; Snapshots = snapshots;
        _facts = new(settings, accounts, entries, schedules, states, snapshots); _packet = Packet(_facts);
        _displaySettings = JsonSerializer.Serialize(Settings, ForecastSourceJson.Default.ZananceSettings);
        _savedPackets = snapshots.ToDictionary(item => item.Id, SnapshotPacket);
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
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != Packet(_facts) || _displaySettings != JsonSerializer.Serialize(Settings, ForecastSourceJson.Default.ZananceSettings))
            throw new InvalidOperationException("Original forecast source was mutated; read again explicitly.");
    }
    /// <summary>Checks only cached access while publishing already detached prepared native rows.</summary>
    public void EnsurePublicationAccess() => _access.EnsureSnapshotCurrent();
    /// <summary>Rejects changed original rows/settings under the actual current reader or writer transaction.</summary>
    internal void RequireFacts(ForecastSourceFacts facts)
    {
        EnsureCurrent();
        if (_packet != Packet(facts)) throw new InvalidOperationException("Original forecast inputs changed; read again explicitly.");
    }
    /// <summary>Requires the exact originally displayed saved identity and content before any reviewed deletion.</summary>
    internal async Task ValidateDeletionAsync(ZananceDbContext db, Guid id, CancellationToken ct)
    {
        DemandWriterFile(db, CommercialFeature.DeleteData);
        if (!_savedPackets.TryGetValue(id, out var expected)) throw new InvalidOperationException("Saved forecast is absent from the original review.");
        var actual = await db.ForecastSnapshots.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        if (actual is null || expected != SnapshotPacket(actual)) throw new InvalidOperationException("Originally confirmed saved forecast changed.");
        RequireFacts(await ForecastWorkStore.ReadFactsAsync(db, ct));
    }
    private static string Packet(ForecastSourceFacts facts) => JsonSerializer.Serialize(facts, ForecastSourceJson.Default.ForecastSourceFacts);
    private static string SnapshotPacket(ForecastSnapshot snapshot) => JsonSerializer.Serialize(snapshot, ForecastSourceJson.Default.ForecastSnapshot);
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
    public async Task<ForecastReadSnapshot> ReadAsync(CancellationToken cancellationToken = default, Func<string, bool>? acceptFile = null)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(source, path);
        access.DemandFeature(CommercialFeature.History);
        if (acceptFile?.Invoke(path) == false) throw new InvalidOperationException("The original forecast source cannot follow another file.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            var facts = await ReadFactsAsync(db, cancellationToken); access.EnsureSnapshotCurrent(); cancellationToken.ThrowIfCancellationRequested();
            return new ForecastReadSnapshot(path, access, facts.Settings, facts.Accounts, facts.Entries, facts.Schedules, facts.States, facts.Snapshots);
        }, cancellationToken);
    }
    /// <summary>Reads every original fact in the caller's existing transaction, preserving original enumeration/order.</summary>
    internal static async Task<ForecastSourceFacts> ReadFactsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(ct),
        await db.Accounts.AsNoTracking().ToListAsync(ct),
        await db.Entries.AsNoTracking().ToListAsync(ct),
        await db.Schedules.AsNoTracking().OrderBy(item => item.State).ThenBy(item => item.Name).ToListAsync(ct),
        await db.OccurrenceStates.AsNoTracking().ToListAsync(ct),
        await db.ForecastSnapshots.AsNoTracking().OrderByDescending(item => item.CreatedAt).ToListAsync(ct));
    /// <summary>Rechecks the actual current factory file and captured access before a native prompt result is used.</summary>
    public async Task ValidateAsync(ForecastReadSnapshot reviewed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (reviewed.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original forecast file changed.");
        await Task.Run(async () =>
        {
            reviewed.EnsureCurrent(); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            reviewed.RequireFacts(await ReadFactsAsync(db, cancellationToken)); reviewed.EnsurePublicationAccess();
        }, cancellationToken);
    }
}

/// <summary>Complete original forecast facts; the frozen packet detects caller mutation without changing stored rows.</summary>
internal sealed record ForecastSourceFacts(ZananceSettings? Settings, List<Account> Accounts, List<LedgerEntry> Entries,
    List<Schedule> Schedules, List<OccurrenceState> States, List<ForecastSnapshot> Snapshots);

/// <summary>Explicit complete source metadata for trimmed native forecast readers and writers.</summary>
[JsonSerializable(typeof(ForecastSourceFacts))]
[JsonSerializable(typeof(ForecastSnapshot))]
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class ForecastSourceJson : JsonSerializerContext;
