using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Narrow original file/access authority passed into a goal editor without retaining a parent's ledger graph.</summary>
public sealed class GoalEditorOrigin
{
    private readonly CommercialFileAccess _access;
    /// <summary>Captures an actual parent reader's file and cached access without its financial rows.</summary>
    internal GoalEditorOrigin(string path, CommercialFileAccess access) { DatabasePath = path; _access = access; }
    /// <summary>Gets the actual original file, never a later selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Refuses retired original cached rights before opening the editor's context.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
}

/// <summary>Complete original non-goal references used by the existing editor and its unchanged preview algorithms.</summary>
public sealed record GoalEditorInputs(ZananceSettings? Settings, List<Account> Accounts, List<LedgerEntry> Entries,
    List<AssetType> AssetTypes, List<AssetEvent> AssetEvents, List<AssetLocation> Locations, List<Category> Categories);

/// <summary>One immutable-guarded original goal form source captured from one actual file and worker transaction.</summary>
public sealed class GoalEditorSnapshot
{
    private readonly string _stamp;
    private readonly string? _defaultStamp;
    /// <summary>Binds complete detached inputs to the actual original goal/work read and local preview day.</summary>
    internal GoalEditorSnapshot(Guid? requestedId, DateOnly day, GoalWorkSnapshot goals, GoalEditorInputs inputs)
    {
        RequestedId = requestedId; Day = day; Goals = goals; Inputs = inputs;
        Settings = inputs.Settings ?? new ZananceSettings(); _stamp = Capture(inputs);
        _defaultStamp = inputs.Settings is null ? JsonSerializer.Serialize(Settings, GoalEditorJson.Default.ZananceSettings) : null;
    }
    /// <summary>Gets the requested original edit identity; null belongs only to an explicit new goal.</summary>
    public Guid? RequestedId { get; }
    /// <summary>Gets the original local day used by current progress and contribution preview.</summary>
    public DateOnly Day { get; }
    /// <summary>Gets the original actual file identity.</summary>
    public string DatabasePath => Goals.DatabasePath;
    /// <summary>Gets original goals/rules/allocations and separate new-work rights.</summary>
    public GoalWorkSnapshot Goals { get; }
    /// <summary>Gets complete detached facts; mutating them invalidates later publication and writes.</summary>
    public GoalEditorInputs Inputs { get; }
    /// <summary>Gets original preferences or nonpersisted defaults; a read never creates storage.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Checks immutable detached inputs and cached original rights without opening another connection.</summary>
    public void EnsureCurrent()
    {
        var access = Goals.Access ?? throw new InvalidOperationException("Original goal editor access is unavailable.");
        access.EnsureSnapshotCurrent();
        RequireUnchangedInputs();
    }
    private void RequireUnchangedInputs()
    {
        if (_stamp != Capture(Inputs) || Goals.InputStamp != GoalWorkSnapshot.CaptureInputs(Goals.Goals, Goals.ContributionPlans, Goals.Allocations)
            || _defaultStamp is not null && _defaultStamp != JsonSerializer.Serialize(Settings, GoalEditorJson.Default.ZananceSettings))
            throw new InvalidOperationException("Original goal editor inputs changed in memory.");
    }
    /// <summary>Checks only original cached rights for repeated synchronous native publication guards.</summary>
    public void EnsureAccessCurrent() => (Goals.Access ?? throw new InvalidOperationException("Original goal editor access is unavailable.")).EnsureSnapshotCurrent();
    /// <summary>Compares every original source field when explicit reload merges retained drafts.</summary>
    public bool HasSameInputs(GoalEditorSnapshot previous)
    {
        // A fresh explicit review can replace expired old rights, but never altered original input facts.
        ArgumentNullException.ThrowIfNull(previous); EnsureCurrent(); previous.RequireUnchangedInputs();
        return RequestedId == previous.RequestedId && Day == previous.Day && DatabasePath == previous.DatabasePath
            && _stamp == previous._stamp && Goals.HasSameOriginalInputs(previous.Goals);
    }
    /// <summary>Requires the actual original file/access without relying on the later global profile selection.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        (Goals.Access ?? throw new InvalidOperationException("Original goal editor access is unavailable."))
            .RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
    }
    /// <summary>Rejects changed stored facts and detached original data before publication or a reviewed writer.</summary>
    internal void Compare(GoalEditorSnapshot fresh)
    {
        EnsureCurrent();
        if (RequestedId != fresh.RequestedId || Day != fresh.Day || DatabasePath != fresh.DatabasePath
            || _stamp != fresh._stamp || !Goals.HasSameOriginalInputs(fresh.Goals))
            throw new InvalidOperationException("The complete original goal editor source changed.");
    }
    private static string Capture(GoalEditorInputs inputs)
    {
        // D-640: stream the complete original ledger packet instead of allocating a second full JSON buffer.
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, GoalEditorJson.Default.GoalEditorInputs); output.FlushFinalBlock();
        return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Explicit AOT metadata for the complete original goal editor references and preferences.</summary>
[JsonSerializable(typeof(GoalEditorInputs))]
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class GoalEditorJson : JsonSerializerContext;

public sealed partial class GoalStore
{
    /// <summary>Reads original goal fields and every preview reference through one deferred worker transaction.</summary>
    public async Task<GoalEditorSnapshot> ReadEditorSnapshotAsync(Guid? requestedId, DateOnly day,
        Func<string, bool>? acceptsFile = null, CancellationToken cancellationToken = default)
    {
        if (requestedId == Guid.Empty) throw new InvalidOperationException("The requested original goal is unavailable.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = ProfileFilePath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original goal editor file changed.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            return await ReadEditorSnapshotAsync(db, requestedId, day, cancellationToken);
        }, cancellationToken);
    }
    /// <summary>Revalidates complete original detached and stored facts before publication or a reviewed writer.</summary>
    public async Task ValidateEditorSnapshotAsync(GoalEditorSnapshot source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); source.EnsureAccessCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            source.RequireFile(db); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            await ValidateOriginalEditorAsync(source, db, cancellationToken);
        }, cancellationToken);
    }
    private async Task ValidateOriginalEditorAsync(GoalEditorSnapshot source, ZananceDbContext db, CancellationToken ct)
    {
        source.RequireFile(db); source.EnsureCurrent();
        source.Compare(await ReadEditorSnapshotAsync(db, source.RequestedId, source.Day, ct));
        ct.ThrowIfCancellationRequested(); source.RequireFile(db);
    }
    private async Task<GoalEditorSnapshot> ReadEditorSnapshotAsync(ZananceDbContext db, Guid? id, DateOnly day, CancellationToken ct)
    {
        var goals = await GetWorkSnapshotAsync(db, ct);
        if (id is not null && !goals.Goals.Any(row => row.Id == id))
            throw new InvalidOperationException("The requested original goal is unavailable.");
        var settings = await SettingsRowReader.ReadAsync(db, ct);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(row => row.IsArchived).ThenBy(row => row.SortOrder).ThenBy(row => row.Name).ToListAsync(ct);
        var entries = await ZananceStore.ReadCompleteHomeLedgerAsync(db, ct);
        var types = await db.AssetTypes.AsNoTracking().OrderBy(row => row.IsArchived).ThenBy(row => row.SortOrder).ThenBy(row => row.Name).ToListAsync(ct);
        var events = (await db.AssetEvents.AsNoTracking().ToListAsync(ct)).OrderBy(row => row.Date).ThenBy(row => row.CreatedAt).ToList();
        var locations = await db.AssetLocations.AsNoTracking().OrderBy(row => row.IsArchived).ThenBy(row => row.SortOrder).ThenBy(row => row.Name).ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().OrderBy(row => row.Kind).ThenBy(row => row.SortOrder).ToListAsync(ct);
        var result = new GoalEditorSnapshot(id, day, goals, new(settings, accounts, entries, types, events, locations, categories));
        ct.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
    }
}
