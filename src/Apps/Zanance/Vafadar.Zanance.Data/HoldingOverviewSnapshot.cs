using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The complete original holding overview inputs, including optional existing display settings.</summary>
public sealed record HoldingOverviewInputs(ZananceSettings? Settings, List<AssetType> Types,
    List<AssetEvent> Events, List<AssetValuation> Valuations, List<AssetLocation> Locations);

/// <summary>One coherent original holding overview, bound to the actual captured database and read authority.</summary>
public sealed class HoldingOverviewSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;

    /// <summary>Retains original file/access and complete immutable comparison before the read is published.</summary>
    internal HoldingOverviewSnapshot(string path, CommercialFileAccess access, HoldingOverviewInputs inputs)
    { DatabasePath = path; _access = access; Inputs = inputs; _stamp = Capture(inputs); }

    /// <summary>Gets the actual original file; a retry cannot follow a later profile selection.</summary>
    public string DatabasePath { get; }

    /// <summary>Gets the detached original inputs; modifying them invalidates publication validation.</summary>
    public HoldingOverviewInputs Inputs { get; }

    /// <summary>Checks original cached file/access before synchronous native publication or navigation.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();

    /// <summary>Rejects a different opened file even while test-build commercial enforcement is inactive.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original holding overview belongs to another opened file.");
    }

    /// <summary>Refuses changed file, retired rights and caller-mutated input before classifying source supersession.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        RequireFile(db);
        if (_stamp != Capture(Inputs))
            throw new InvalidOperationException("The original holding overview file or input changed.");
    }

    /// <summary>Distinguishes a same-file source edit from other failures without authorizing another profile.</summary>
    internal void Compare(HoldingOverviewInputs current)
    {
        EnsureCurrent();
        if (_stamp != Capture(current)) throw new HoldingOverviewSupersededException();
    }

    private static string Capture(HoldingOverviewInputs inputs) =>
        JsonSerializer.Serialize(inputs, HoldingOverviewJson.Default.HoldingOverviewInputs);
}

/// <summary>Only a coherent original overview's persisted same-file source has changed.</summary>
public sealed class HoldingOverviewSupersededException() : InvalidOperationException("The original holding overview source changed; read it again.");

/// <summary>Explicit serialization metadata for the original complete holding overview on trimmed/AOT hosts.</summary>
[JsonSerializable(typeof(HoldingOverviewInputs))]
internal sealed partial class HoldingOverviewJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Captures the factory lease before queuing a read of all holding facts in one deferred transaction.</summary>
    public async Task<HoldingOverviewSnapshot> ReadOverviewSnapshotAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            // D-418: SQLite async APIs may materialize synchronously. Keep key derivation and every query off UI.
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var result = new HoldingOverviewSnapshot(path, access, await ReadOverviewInputsAsync(db, ct));
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent(); return result;
        }, ct);
    }

    /// <summary>Checks a retained native target against the currently opened actual file without opening/keying a second read.</summary>
    public async Task ValidateOverviewAccessAsync(HoldingOverviewSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        ct.ThrowIfCancellationRequested(); snapshot.RequireFile(db);
    }

    /// <summary>Rechecks the original packet and actual file before any overview becomes visible or actionable.</summary>
    public async Task ValidateOverviewSnapshotAsync(HoldingOverviewSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var current = await ReadOverviewInputsAsync(db, ct);
            snapshot.RequireOriginal(db); ct.ThrowIfCancellationRequested(); snapshot.Compare(current);
        }, ct);
    }

    /// <summary>Reads display-ordered detached facts in the caller's already joined original transaction, without initialization writes.</summary>
    private static async Task<HoldingOverviewInputs> ReadOverviewInputsAsync(ZananceDbContext db, CancellationToken ct) => new(
        await SettingsRowReader.ReadAsync(db, ct),
        await db.AssetTypes.AsNoTracking().OrderBy(t => t.IsArchived).ThenBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct),
        (await db.AssetEvents.AsNoTracking().ToListAsync(ct)).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList(),
        (await db.AssetValuations.AsNoTracking().ToListAsync(ct)).OrderByDescending(v => v.Date).ThenByDescending(v => v.CreatedAt).ToList(),
        await db.AssetLocations.AsNoTracking().OrderBy(l => l.IsArchived).ThenBy(l => l.SortOrder).ThenBy(l => l.Name).ToListAsync(ct));
}
