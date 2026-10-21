using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The original settings read and immutable estimate facts, without an additional settings query.</summary>
public sealed class EssentialEstimateReview
{
    private readonly string _path;
    private readonly Guid _id;
    private readonly long? _amount;
    private readonly EstimatePeriod _period;
    private readonly string? _currency;
    private readonly string _effectiveCurrency;
    /// <summary>Captures immutable original essential-estimate facts independently of mutable presentation data.</summary>
    internal EssentialEstimateReview(string path, ZananceSettings settings)
    {
        _path = path; Settings = settings; _id = settings.Id; _amount = settings.EssentialEstimate;
        _period = settings.EssentialEstimatePeriod; _currency = settings.EssentialEstimateCurrency;
        _effectiveCurrency = _currency ?? settings.DefaultCurrencyCode;
    }
    /// <summary>Gets the actual complete settings row used by the owning covered publication.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets the original immutable settings identity independently of mutable presentation data.</summary>
    public Guid SettingsId => _id;
    /// <summary>Refuses a writer redirected to another actual profile file.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        if (DatabaseFileIdentity.ConfiguredPath(db) != _path)
            throw new InvalidOperationException("The original essential estimate file changed.");
    }
    /// <summary>Refuses replacement of newer stored facts or a restored settings identity.</summary>
    internal void RequireOriginal(ZananceSettings current)
    {
        if (current.Id != _id || current.EssentialEstimate != _amount || current.EssentialEstimatePeriod != _period
            || current.EssentialEstimateCurrency != _currency || (current.EssentialEstimateCurrency ?? current.DefaultCurrencyCode) != _effectiveCurrency)
            throw new InvalidOperationException("The original essential estimate changed; refresh before replacing it.");
    }
}

public sealed partial class ZananceStore
{
    /// <summary>Captures settings and the original estimate file using the existing single settings-read path.</summary>
    public async Task<EssentialEstimateReview> ReadEssentialEstimateAsync(CancellationToken ct = default)
    {
        EssentialEstimateReview? review = null;
        await GetSettingsCoreAsync(null, ct, (db, settings) => review = new(DatabaseFileIdentity.ConfiguredPath(db), settings));
        return review ?? throw new InvalidOperationException("Original estimate settings were not captured.");
    }

    /// <summary>Changes only the original estimate fields, preserving unrelated current preferences and known acceptance.</summary>
    public async Task<EssentialEstimateReview> SaveEssentialEstimateAsync(EssentialEstimateReview original, long? amount, EstimatePeriod period,
        string currency, Func<bool> isCurrent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(isCurrent);
        if (amount is <= 0 || !Enum.IsDefined(period) || !Currencies.TryGet(currency, out _))
            throw new ArgumentException("A positive original estimate or explicit clearing is required.");
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current()
        {
            original.RequireFile(db);
            if (!isCurrent()) throw new InvalidOperationException("The original essential estimate form retired.");
        }
        Current();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        Current();
        var settings = await SettingsRowReader.ReadAsync(db, ct)
            ?? throw new InvalidOperationException("Original estimate settings are unavailable.");
        original.RequireOriginal(settings);
        settings.EssentialEstimate = amount; settings.EssentialEstimatePeriod = period; settings.EssentialEstimateCurrency = currency;
        db.Attach(settings);
        db.Entry(settings).Property(row => row.EssentialEstimate).IsModified = true;
        db.Entry(settings).Property(row => row.EssentialEstimatePeriod).IsModified = true;
        db.Entry(settings).Property(row => row.EssentialEstimateCurrency).IsModified = true;
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct);
        Current(); await write.CommitAsync(ct);
        var accepted = new EssentialEstimateReview(DatabaseFileIdentity.ConfiguredPath(db), settings);
        return CommittedWriteNotification.Publish(OnChanged, accepted);
    }
}
