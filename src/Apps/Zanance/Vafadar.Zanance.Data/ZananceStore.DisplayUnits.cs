using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Immutable original file/settings/unit facts for one explicitly reviewed display-unit replacement.</summary>
public sealed class DisplayUnitsReview
{
    private readonly string _path;
    private readonly Guid _id;
    private readonly string? _original;
    /// <summary>Captures immutable original display-unit facts independently of mutable presentation data.</summary>
    internal DisplayUnitsReview(string path, ZananceSettings settings)
    { _path = path; _id = settings.Id; _original = settings.DisplayUnits; DefaultCurrencyCode = settings.DefaultCurrencyCode; }
    /// <summary>Gets the original profile default used only to initialize the unit editor's selection.</summary>
    public string DefaultCurrencyCode { get; }
    /// <summary>Gets the original profile serialization; null preserves the separate legacy preference migration contract.</summary>
    public string? SerializedUnits => _original;
    /// <summary>Refuses a writer redirected to another actual profile file.</summary>
    internal void RequireFile(ZananceDbContext db)
    { if (_path != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original display-unit file changed."); }
    /// <summary>Refuses replacement of newer stored facts or a restored settings identity.</summary>
    internal void RequireOriginal(ZananceSettings current)
    { if (current.Id != _id || current.DisplayUnits != _original) throw new InvalidOperationException("The original display units changed; refresh before replacing them."); }
}

public sealed partial class ZananceStore
{
    /// <summary>Captures original unit settings and file through the existing single settings read.</summary>
    public async Task<DisplayUnitsReview> ReadDisplayUnitsAsync(CancellationToken ct = default)
    {
        DisplayUnitsReview? result = null;
        await GetSettingsCoreAsync(null, ct, (db, settings) => result = new(DatabaseFileIdentity.ConfiguredPath(db), settings));
        return result ?? throw new InvalidOperationException("Original display-unit settings were not captured.");
    }
    /// <summary>Writes only the original unit property and retains known acceptance before fallible notifications.</summary>
    public async Task<DisplayUnitsReview> SaveDisplayUnitsAsync(DisplayUnitsReview original, string serialized, Func<bool> isCurrent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(serialized); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        { original.RequireFile(db); if (!isCurrent()) throw new InvalidOperationException("The original display-unit owner retired."); }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        Current();
        var settings = await SettingsRowReader.ReadAsync(db, ct) ?? throw new InvalidOperationException("Original display-unit settings are unavailable.");
        original.RequireOriginal(settings); settings.DisplayUnits = serialized; db.Attach(settings);
        db.Entry(settings).Property(row => row.DisplayUnits).IsModified = true;
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct); Current(); await write.CommitAsync(ct);
        return CommittedWriteNotification.Publish(OnChanged, new DisplayUnitsReview(DatabaseFileIdentity.ConfiguredPath(db), settings));
    }
}
