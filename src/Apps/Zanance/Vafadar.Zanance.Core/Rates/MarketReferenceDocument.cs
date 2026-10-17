using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Rates;

/// <summary>Original explicit target definition needed to explain a saved conversion later, never sent to a market provider.</summary>
public sealed record MarketAssetDefinition(Guid Id, string Name, AssetKind Kind, AssetDimension Dimension, Metal Metal,
    int? PurityPer10000, long? UnitWeightMg, string? CountUnitName, bool Divisible, string Currency)
{
    /// <inheritdoc />
    public override string ToString() => nameof(MarketAssetDefinition);
}
/// <summary>Exact user-approved effective date/value and explicit unit conversion; it never changes a quantity or payment.</summary>
public sealed record MarketAppliedMapping(int Version, MarketReferenceTarget Target, DateOnly Date, string Currency,
    string? FromCurrency, decimal? Rate, long? PricePerUnitMilli, MarketAssetDefinition? Asset, bool ExactItemAcknowledged)
{
    /// <inheritdoc />
    public override string ToString() => nameof(MarketAppliedMapping);
}

/// <summary>One immutable original accepted source document; its target fingerprint is comparison, never financial authority.</summary>
public sealed record MarketReferenceDocument(int Version, Guid Id, MarketReferenceTarget Target, Guid TargetId,
    string TargetFingerprint, string QuoteDigest, string QuoteJson, string MappingJson, DateTimeOffset AcceptedAt)
{
    /// <inheritdoc />
    public override string ToString() => nameof(MarketReferenceDocument);
    /// <summary>Captures only exact immutable source content; local audit timestamps are not transfer authority.</summary>
    public static MarketReferenceDocument From(MarketReferenceReceipt receipt) => new(1, receipt.Id, receipt.Target, receipt.TargetId,
        receipt.TargetFingerprint, receipt.QuoteDigest, receipt.QuoteJson, receipt.MappingJson, receipt.AcceptedAt);
    /// <summary>Reconstructs source metadata only, never creating or modifying its referenced financial row.</summary>
    public MarketReferenceReceipt ToReceipt() => new(Id) { Target = Target, TargetId = TargetId, TargetFingerprint = TargetFingerprint,
        QuoteDigest = QuoteDigest, QuoteJson = QuoteJson, MappingJson = MappingJson, AcceptedAt = AcceptedAt };
}

/// <summary>Shared bounded original-source validation for local writers, portable data and authenticated companion transfer.</summary>
public static class MarketReferenceDocuments
{
    /// <summary>Finite canonical original companion document bound, independent from commercial quotas.</summary>
    public const int MaximumEncodedBytes = 65536;
    /// <summary>Canonical standard comparison digest of one validated original document, never a signature.</summary>
    public static string Digest(MarketReferenceDocument document, DateTimeOffset now)
    {
        Validate(document, now); var bytes = JsonSerializer.SerializeToUtf8Bytes(document, MarketSourceJson.Default.MarketReferenceDocument);
        try
        {
            if (bytes.Length > MaximumEncodedBytes) throw new InvalidDataException("Original source document bound exceeded.");
            return Convert.ToHexStringLower(SHA256.HashData(bytes));
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    /// <summary>Checks exact original quote, conversion and target binding without trusting or writing any financial authority.</summary>
    public static void Validate(MarketReferenceDocument row, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Version != 1 || row.TargetFingerprint is not { Length: 64 } || row.QuoteDigest is not { Length: 64 } ||
            row.AcceptedAt.Offset != TimeSpan.Zero || row.AcceptedAt > now.AddMinutes(5) ||
            row.QuoteJson is null || row.MappingJson is null)
            throw new InvalidDataException("Original reference document is invalid.");
        if (Encoding.UTF8.GetByteCount(row.QuoteJson) > MarketReference.MaximumQuoteBytes || Encoding.UTF8.GetByteCount(row.MappingJson) > 4096)
            throw new InvalidDataException("Original reference document bound exceeded.");
        var quote = JsonSerializer.Deserialize(row.QuoteJson, MarketJson.Default.MarketQuote) ?? throw new InvalidDataException("Original reference document unavailable.");
        if (MarketReference.Digest(quote, now, requireRetained: false) != row.QuoteDigest)
            throw new InvalidDataException("Original reference document differs.");
        var mapping = JsonSerializer.Deserialize(row.MappingJson, MarketMappingJson.Default.MarketAppliedMapping) ?? throw new InvalidDataException("Original reference mapping unavailable.");
        if (row.Id == Guid.Empty || row.TargetId == Guid.Empty || mapping.Version != 1 || mapping.Target != row.Target ||
            mapping.Date != quote.Request.OnOrBefore || mapping.Currency != quote.Symbol.Currency || row.AcceptedAt < quote.RetrievedAt ||
            row.AcceptedAt > quote.RetainUntil)
            throw new InvalidDataException("Original reference mapping differs.");
        string expected;
        if (mapping.Target == MarketReferenceTarget.ExchangeRate)
        {
            if (quote.Symbol.Unit != MarketUnit.Currency || mapping.FromCurrency != quote.Symbol.Base || mapping.Rate != quote.Observation.Value ||
                mapping.Asset is not null || mapping.PricePerUnitMilli is not null || mapping.ExactItemAcknowledged)
                throw new InvalidDataException("Original currency mapping differs.");
            expected = MarketApplication.Fingerprint(new ExchangeRate(row.TargetId) { Date = mapping.Date,
                FromCurrencyCode = mapping.FromCurrency, ToCurrencyCode = mapping.Currency, Rate = mapping.Rate.Value, IsEstimate = true });
        }
        else if (mapping.Target == MarketReferenceTarget.AssetPrice && mapping.Asset is { } asset)
        {
            if (mapping.FromCurrency is not null || mapping.Rate is not null || asset.Currency != mapping.Currency ||
                asset.Id == Guid.Empty || string.IsNullOrWhiteSpace(asset.Name) || asset.Name.Length > 100 || asset.CountUnitName?.Length > 40 ||
                !Enum.IsDefined(asset.Kind) || !Enum.IsDefined(asset.Dimension) || !Enum.IsDefined(asset.Metal) ||
                asset.PurityPer10000 is < 1 or > 10000 || asset.UnitWeightMg is < 1)
                throw new InvalidDataException("Original asset mapping differs.");
            var type = new AssetType(asset.Id) { Name = asset.Name, Kind = asset.Kind, Dimension = asset.Dimension, Metal = asset.Metal,
                PurityPer10000 = asset.PurityPer10000, UnitWeightMg = asset.UnitWeightMg, CountUnitName = asset.CountUnitName,
                Divisible = asset.Divisible, PriceCurrencyCode = asset.Currency };
            // Historical documents keep their original retention policy, but do not expire the user's accepted audit history.
            var price = MarketApplication.PricePerUnitMilli(quote, type, mapping.ExactItemAcknowledged, row.AcceptedAt);
            if (mapping.PricePerUnitMilli != price) throw new InvalidDataException("Original converted price differs.");
            expected = MarketApplication.Fingerprint(new AssetValuation(row.TargetId) { AssetTypeId = asset.Id, Date = mapping.Date,
                CurrencyCode = mapping.Currency, PricePerUnitMilli = price, Source = ValuationSource.Manual }, type);
        }
        else throw new InvalidDataException("Original reference target is unsupported.");
        if (expected != row.TargetFingerprint) throw new InvalidDataException("Original reference target binding differs.");
    }
}

/// <summary>Explicit AOT metadata for local reviewed mappings; these private target definitions never enter provider request DTOs.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MarketAppliedMapping))]
public sealed partial class MarketMappingJson : JsonSerializerContext;

/// <summary>A bounded exact original personal-profile companion upload; device authority comes from the authenticated session.</summary>
public sealed record MarketSourceUpload(int Version, Guid DeviceId, MarketReferenceDocument[] Documents);
/// <summary>One immutable source document in its profile's monotonic companion feed, separate from financial cursors.</summary>
public sealed record MarketSourceEvent(long Sequence, MarketReferenceDocument Document);
/// <summary>One bounded source feed page; an empty page never authorizes deleting locally retained history.</summary>
public sealed record MarketSourcePage(int Version, long Through, bool More, MarketSourceEvent[] Events);
/// <summary>Original immutable document identities acknowledged by an already authenticated source companion writer.</summary>
public sealed record MarketSourceAcknowledgment(int Version, Guid[] DocumentIds);

/// <summary>Explicit source-companion AOT metadata; unknown document fields cannot silently become financial writes.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MarketReferenceDocument))]
[JsonSerializable(typeof(MarketSourceUpload))]
[JsonSerializable(typeof(MarketSourcePage))]
[JsonSerializable(typeof(MarketSourceAcknowledgment))]
public sealed partial class MarketSourceJson : JsonSerializerContext;
