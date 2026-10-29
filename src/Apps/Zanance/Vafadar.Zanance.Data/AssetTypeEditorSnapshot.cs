using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Narrow actual-file and cached-access authority for an original type editor, without its parent graph.</summary>
public sealed class AssetTypeEditorOrigin
{
    private readonly CommercialFileAccess _access;
    /// <summary>Retains only the original file and cached rights captured by the actual parent reader.</summary>
    internal AssetTypeEditorOrigin(string path, CommercialFileAccess access) { DatabasePath = path; _access = access; }
    /// <summary>Gets the original actual file, never a subsequently selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Refuses retired parent access before the original form acquires its source.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
}

/// <summary>Original complete type/history and optional preferences acquired together in one worker transaction.</summary>
public sealed class AssetTypeEditorSnapshot
{
    private readonly string _settingsStamp;
    private readonly string? _defaultStamp;
    /// <summary>Guards the original type review, stored preferences and nonpersisted defaults before publication.</summary>
    internal AssetTypeEditorSnapshot(HoldingTypeReview review, ZananceSettings? settings)
    {
        Review = review; StoredSettings = settings; Settings = settings ?? new ZananceSettings();
        _settingsStamp = SettingsPacket(settings); _defaultStamp = settings is null ? SettingsPacket(Settings) : null;
    }
    /// <summary>Gets the single-use original type and its complete measurement/valuation history.</summary>
    public HoldingTypeReview Review { get; }
    /// <summary>Gets optional stored preferences without creating a Settings row during a read.</summary>
    public ZananceSettings? StoredSettings { get; }
    /// <summary>Gets the stored preferences or guarded nonpersisted defaults used by this original form.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets the actual captured file.</summary>
    public string DatabasePath => Review.DatabasePath;
    /// <summary>Checks only original cached rights for repeated synchronous native publication guards.</summary>
    public void EnsureAccessCurrent() => Review.EnsureAccessCurrent();
    /// <summary>Refuses a different configured file before its connection is opened.</summary>
    internal void RequireFile(ZananceDbContext db) => Review.RequireFile(db);
    /// <summary>Refuses mutated detached inputs, expired rights or altered fallback defaults.</summary>
    public void EnsureCurrent()
    {
        Review.EnsureCurrent(); RequireUnchangedSettings();
    }
    private void RequireUnchangedSettings()
    {
        if (_settingsStamp != SettingsPacket(StoredSettings)
            || _defaultStamp is not null && _defaultStamp != SettingsPacket(Settings))
            throw new HoldingEventReviewChangedException("Original type editor preferences changed in memory.");
    }
    /// <summary>Compares original stored inputs during explicit draft-preserving reload, without reusing expired grants.</summary>
    public bool HasSameInputs(AssetTypeEditorSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(previous); EnsureCurrent(); previous.RequireUnchangedSettings();
        return _settingsStamp == previous._settingsStamp && Review.HasSameInputs(previous.Review);
    }
    /// <summary>Revalidates the complete original source in an already captured reader or writer context.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        EnsureCurrent(); await Review.ValidateAsync(db, ct);
        if (_settingsStamp != SettingsPacket(await SettingsRowReader.ReadAsync(db, ct)))
            throw new HoldingEventReviewChangedException("Original type editor preferences changed; reload explicitly.");
        ct.ThrowIfCancellationRequested(); EnsureCurrent();
    }
    private static string SettingsPacket(ZananceSettings? value) => JsonSerializer.Serialize(value, AssetTypeEditorJson.Default.ZananceSettings);
}

/// <summary>Explicit AOT metadata for the original optional preferences and guarded default values.</summary>
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class AssetTypeEditorJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Captures the actual file before reading complete original type/history/preferences on one worker.</summary>
    public async Task<AssetTypeEditorSnapshot> ReadTypeEditorSnapshotAsync(Guid id, bool editing,
        Func<string, bool>? acceptsFile = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Original type identity required.", nameof(id));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new HoldingEventReviewChangedException("The original type editor file changed.");
        return await Task.Run(async () =>
        {
            var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var review = new HoldingTypeReview(id, editing, path, access, await HoldingTypeReview.ReadAsync(db, id, editing, ct));
            var result = new AssetTypeEditorSnapshot(review, await SettingsRowReader.ReadAsync(db, ct));
            result.EnsureCurrent(); ct.ThrowIfCancellationRequested(); return result;
        }, ct);
    }
    /// <summary>Revalidates the exact original file, type/history and preferences before uncovering its native form.</summary>
    public async Task ValidateTypeEditorSnapshotAsync(AssetTypeEditorSnapshot source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source); source.EnsureAccessCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            source.RequireFile(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await source.ValidateAsync(db, ct);
        }, ct);
    }
}
