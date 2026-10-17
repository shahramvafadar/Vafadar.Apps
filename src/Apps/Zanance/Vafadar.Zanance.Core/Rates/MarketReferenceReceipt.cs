using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Vafadar.Core.Domain;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Rates;

/// <summary>Existing financial subjects to which an independently reviewed reference document may attach.</summary>
public enum MarketReferenceTarget
{
    /// <summary>A dated user-approved exchange-rate row; not an actual payment or transaction conversion.</summary>
    ExchangeRate = 0,
    /// <summary>A dated holding price; no holding quantity or purchase cost changes.</summary>
    AssetPrice = 1,
}

/// <summary>Immutable profile-owned source document, not a cache entry, provider signature, paid grant or financial wire row.</summary>
public sealed class MarketReferenceReceipt : Entity, IAuditableEntity
{
    /// <summary>Creates one original reviewed receipt identity.</summary>
    public MarketReferenceReceipt() { }
    /// <summary>Reconstructs one literal original receipt, never a replacement id during continuation.</summary>
    public MarketReferenceReceipt(Guid id) : base(id) { }
    /// <summary>Existing financial target kind; source metadata cannot create a new kind of financial write.</summary>
    public MarketReferenceTarget Target { get; set; }
    /// <summary>Original local financial row identity, never sent in a public-symbol provider request.</summary>
    public Guid TargetId { get; set; }
    /// <summary>Standard digest of exact numeric/date/currency/definition facts; a later edit cannot inherit stale attribution.</summary>
    public required string TargetFingerprint { get; set; }
    /// <summary>Standard digest of the exact original source document, independent from any current source catalog.</summary>
    public required string QuoteDigest { get; set; }
    /// <summary>Complete bounded original reference/source/unit/time/licence snapshot; not a provider credential.</summary>
    public required string QuoteJson { get; set; }
    /// <summary>Explicit reviewed conversion description, without an inferred coin premium or quantity change.</summary>
    public required string MappingJson { get; set; }
    /// <summary>Original user's confirmation instant, distinct from market observation and retrieval.</summary>
    public DateTimeOffset AcceptedAt { get; set; }
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }
    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Exact original target binding and bounded conversion; no source metadata or generated advice is write authority.</summary>
public static class MarketApplication
{
    /// <summary>Fingerprints the original financial rate; audit timestamps do not change its attribution.</summary>
    public static string Fingerprint(ExchangeRate rate) => Digest("rate", rate.Id.ToString("N"), rate.Date.ToString("O", CultureInfo.InvariantCulture),
        rate.FromCurrencyCode, rate.ToCurrencyCode, rate.Rate.ToString("G29", CultureInfo.InvariantCulture), rate.IsEstimate ? "1" : "0");
    /// <summary>Fingerprints an explicit target definition, including the identity/caption the user actually reviewed.</summary>
    public static string Definition(AssetType type) => Digest("definition", type.Id.ToString("N"), type.Name, ((int)type.Kind).ToString(CultureInfo.InvariantCulture),
        ((int)type.Dimension).ToString(CultureInfo.InvariantCulture), ((int)type.Metal).ToString(CultureInfo.InvariantCulture),
        type.PurityPer10000?.ToString(CultureInfo.InvariantCulture) ?? "", type.UnitWeightMg?.ToString(CultureInfo.InvariantCulture) ?? "",
        type.CountUnitName ?? "", type.PriceCurrencyCode, type.Divisible ? "1" : "0", type.IsArchived ? "1" : "0");
    /// <summary>Fingerprints the exact saved price and original definition, never current holding quantities.</summary>
    public static string Fingerprint(AssetValuation value, AssetType type) => Digest("price", value.Id.ToString("N"), value.AssetTypeId.ToString("N"),
        value.Date.ToString("O", CultureInfo.InvariantCulture), value.CurrencyCode, value.PricePerUnitMilli.ToString(CultureInfo.InvariantCulture),
        ((int)value.Source).ToString(CultureInfo.InvariantCulture), Definition(type));

    /// <summary>Converts only an explicitly matched gram/piece source into existing minor-unit thousandths; never derives a coin price from metal.</summary>
    public static long PricePerUnitMilli(MarketQuote quote, AssetType target, bool exactItemAcknowledged, DateTimeOffset now)
    {
        MarketReference.Validate(quote, now); ArgumentNullException.ThrowIfNull(target);
        if (target.IsArchived || quote.Symbol.Currency != target.PriceCurrencyCode || quote.Symbol.Unit == MarketUnit.Currency)
            throw new ArgumentException("Original reference/asset currency or unit differs.");
        decimal price = quote.Observation.Value;
        switch (quote.Symbol.Unit)
        {
            case MarketUnit.FineTroyOunce:
                // Standard troy ounce =31.1034768g; D-299 records the original unit and explicit conversion in the source document.
                price /= 31.1034768m;
                goto case MarketUnit.FineGram;
            case MarketUnit.FineGram:
                // Fine-metal references do not include jewellery workmanship, spreads or retail premiums.
                if (target.Dimension != AssetDimension.Mass || target.PurityPer10000 is not (> 0 and <= 10000) ||
                    quote.Symbol.FineMetal is null || target.Metal != quote.Symbol.FineMetal)
                    throw new ArgumentException("Explicit matching metal and known purity required.");
                price = checked(price * target.PurityPer10000.Value / 10000m);
                break;
            case MarketUnit.Gram:
                if (!exactItemAcknowledged || target.Dimension != AssetDimension.Mass) throw new ArgumentException("Explicit matching gram reference required.");
                break;
            case MarketUnit.Piece:
                if (!exactItemAcknowledged || target.Dimension != AssetDimension.Count) throw new ArgumentException("Explicit exact item reference required.");
                break;
            default: throw new ArgumentException("Unsupported reference unit.");
        }
        if (!Currencies.TryGet(target.PriceCurrencyCode, out var currency)) throw new ArgumentException("Supported original currency required.");
        var factor = 1m; for (var digit = 0; digit < currency.MinorDigits; digit++) factor *= 10;
        var scaled = checked(price * factor * 1000m);
        var result = checked((long)decimal.Round(scaled, 0, MidpointRounding.AwayFromZero));
        if (result <= 0) throw new ArgumentException("Reference price cannot be represented in the original unit.");
        return result;
    }
    private static string Digest(params string[] fields)
    {
        // Length-prefixed standard UTF-8 avoids delimiter ambiguity; this digest is comparison, never authentication.
        var value = string.Concat(fields.Select(field => field.Length.ToString(CultureInfo.InvariantCulture) + ":" + field));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
