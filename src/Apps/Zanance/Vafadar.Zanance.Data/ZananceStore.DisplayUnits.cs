using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Immutable original file/settings/unit facts for one explicitly reviewed display-unit replacement.</summary>
public sealed class DisplayUnitsReview
{
    private readonly string _path;
    private readonly CommercialFileAccess? _access;
    private readonly Guid _id;
    private readonly string? _original;
    /// <summary>Captures immutable original display-unit facts independently of mutable presentation data.</summary>
    internal DisplayUnitsReview(string path, ZananceSettings settings, CommercialFileAccess? access = null)
    { _path = path; _access = access; _id = settings.Id; _original = settings.DisplayUnits; DefaultCurrencyCode = settings.DefaultCurrencyCode; }
    /// <summary>Gets the original profile default used only to initialize the unit editor's selection.</summary>
    public string DefaultCurrencyCode { get; }
    /// <summary>Gets the original profile serialization; null preserves the separate legacy preference migration contract.</summary>
    public string? SerializedUnits => _original;
    /// <summary>Refuses retired cached original file authority without requesting a permission.</summary>
    public void EnsureCurrent() => _access?.EnsureSnapshotCurrent();
    /// <summary>Compares only original writer/draft identity; unrelated newer preferences stay independent.</summary>
    public bool OwnsSameVersion(DisplayUnitsReview other) => other is not null && _path == other._path && _id == other._id && _original == other._original;
    /// <summary>Validates the initial currency hint as well as original unit definitions before publication.</summary>
    internal void RequireInputs(ZananceSettings current)
    { RequireOriginal(current); if (current.DefaultCurrencyCode != DefaultCurrencyCode) throw new InvalidOperationException("The original unit default changed."); }
    /// <summary>Records the known committed value with original authority and no fallible fresh access capture.</summary>
    internal DisplayUnitsReview WithAccepted(ZananceSettings saved) => new(_path, saved, _access);
    /// <summary>Refuses a writer redirected to another actual profile file.</summary>
    internal void RequireFile(ZananceDbContext db)
    { EnsureCurrent(); if (_path != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original display-unit file changed."); }
    /// <summary>Refuses replacement of newer stored facts or a restored settings identity.</summary>
    internal void RequireOriginal(ZananceSettings current)
    { if (current.Id != _id || current.DisplayUnits != _original) throw new InvalidOperationException("The original display units changed; refresh before replacing them."); }
}

public sealed partial class ZananceStore
{
    /// <summary>Captures original unit settings and file through the existing single settings read.</summary>
    public async Task<DisplayUnitsReview> ReadDisplayUnitsAsync(CancellationToken ct = default, Func<string, bool>? acceptsFile = null)
    {
        DisplayUnitsReview? result = null;
        await GetSettingsCoreAsync(null, ct, (db, settings) => result = new(DatabaseFileIdentity.ConfiguredPath(db), settings, new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db))), acceptsFile);
        return result ?? throw new InvalidOperationException("Original display-unit settings were not captured.");
    }
    /// <summary>Validates original actual unit/default inputs without initializing a deleted or foreign settings row.</summary>
    public async Task ValidateDisplayUnitsAsync(DisplayUnitsReview original, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        await using var db = await contextFactory.CreateDbContextAsync(ct); original.RequireFile(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var read = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(read, ct);
            var current = await SettingsRowReader.ReadAsync(db, ct) ?? throw new InvalidOperationException("The original unit settings were removed.");
            original.RequireInputs(current); ct.ThrowIfCancellationRequested(); original.RequireFile(db);
        }, ct);
    }
    /// <summary>Writes only the original unit property and retains known acceptance before fallible notifications.</summary>
    public async Task<DisplayUnitsReview> SaveDisplayUnitsAsync(DisplayUnitsReview original, string serialized, Func<bool> isCurrent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(serialized); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        { ct.ThrowIfCancellationRequested(); original.RequireFile(db); if (!isCurrent()) throw new InvalidOperationException("The original display-unit owner retired."); }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        Current();
        var settings = await SettingsRowReader.ReadAsync(db, ct) ?? throw new InvalidOperationException("Original display-unit settings are unavailable.");
        original.RequireOriginal(settings); settings.DisplayUnits = serialized; db.Attach(settings);
        db.Entry(settings).Property(row => row.DisplayUnits).IsModified = true;
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct); Current(); await write.CommitAsync(ct);
        return CommittedWriteNotification.Publish(OnChanged, original.WithAccepted(settings));
    }
}
