using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One original detail read, including absence and existing settings, bound to its actual opened file.</summary>
public sealed class HoldingDetailSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _settings;

    /// <summary>Retains the exact original file, optional row and detached settings before publication.</summary>
    internal HoldingDetailSnapshot(string path, CommercialFileAccess access, Guid id,
        HoldingPriceReview? review, ZananceSettings? settings)
    {
        DatabasePath = path; _access = access; TypeId = id; Review = review; Settings = settings;
        _settings = SettingsPacket(settings);
    }

    /// <summary>Gets the captured actual file, independently of later selected-profile metadata.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the original requested type identity, including an original missing row.</summary>
    public Guid TypeId { get; }
    /// <summary>Gets the original coherent writer review, or null for a verified missing type.</summary>
    public HoldingPriceReview? Review { get; }
    /// <summary>Gets detached existing display settings; a missing row is never initialized by this read.</summary>
    public ZananceSettings? Settings { get; }
    /// <summary>Refuses retired cached access before synchronous native publication.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Refuses a foreign actual file, retired access or caller-mutated display settings.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db) || _settings != SettingsPacket(Settings))
            throw new HoldingEventReviewChangedException("Original holding detail file or settings changed.");
    }
    /// <summary>Rechecks the complete original facts under the caller's coherent read transaction.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        RequireFile(db);
        if (_settings != SettingsPacket(await SettingsRowReader.ReadAsync(db, ct)))
            throw new HoldingEventReviewChangedException("Original holding detail settings changed; refresh explicitly.");
        if (Review is { } review) await review.ValidateAsync(db, ct);
        else if (await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Id == TypeId, ct))
            throw new HoldingEventReviewChangedException("Original missing holding changed; read it again.");
        ct.ThrowIfCancellationRequested(); RequireFile(db);
    }
    private static string SettingsPacket(ZananceSettings? value) =>
        JsonSerializer.Serialize(value, HoldingDetailJson.Default.ZananceSettings);
}

/// <summary>Explicit settings metadata for the trimmed/AOT detail snapshot.</summary>
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class HoldingDetailJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Captures one actual-file detail/settings read on a worker, without type/settings initialization.</summary>
    public async Task<HoldingDetailSnapshot> ReadDetailSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            // D-419: absence, settings and financial facts share the original deferred SQLite read transaction.
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var exists = await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Id == id, ct);
            var review = exists ? new HoldingPriceReview(path, access, await HoldingPriceReview.ReadAsync(db, id, ct)) : null;
            var result = new HoldingDetailSnapshot(path, access, id, review, await SettingsRowReader.ReadAsync(db, ct));
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent(); return result;
        }, ct);
    }

    /// <summary>Rechecks all original detail facts/settings in one coherent actual-file transaction before publication.</summary>
    public async Task ValidateDetailSnapshotAsync(HoldingDetailSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            snapshot.RequireFile(db); await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await snapshot.ValidateAsync(db, ct);
        }, ct);
    }

    /// <summary>Checks a retained original detail target without opening/keying another data read.</summary>
    public async Task ValidateDetailAccessAsync(HoldingDetailSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        ct.ThrowIfCancellationRequested(); snapshot.RequireFile(db);
    }
}
