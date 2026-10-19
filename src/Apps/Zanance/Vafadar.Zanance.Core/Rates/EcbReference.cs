using System.Globalization;
using System.Text.Json;

namespace Vafadar.Zanance.Core.Rates;

/// <summary>Date-precision source evidence; no fixing timestamp is inferred from document metadata or retrieval.</summary>
public sealed record EcbDailyEvidence(int Version, string SourceSha256, DateTimeOffset FetchedAt,
    DateTimeOffset? DocumentModifiedAt, decimal BasePerEuro, decimal QuotePerEuro, string Calculation);

/// <summary>Approved daily ECB FX terms and reproducible calculation, independent of financial writers.</summary>
public static class EcbReference
{
    /// <summary>Fixed approved public XML, without account, user parameters or credentials.</summary>
    public const string FeedUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    /// <summary>Original freely accessible reference description.</summary>
    public const string SourceUrl = "https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html";
    /// <summary>Reviewed attribution, modification and free-original disclosure terms.</summary>
    public const string LicenceUrl = "https://www.ecb.europa.eu/services/using-our-site/disclaimer/html/index.en.html";
    /// <summary>Exact immutable source policy identity.</summary>
    public const string Revision = "daily-fx-2026-10-19";
    /// <summary>Explicit calculation semantics; decimal division uses the managed decimal representation, without additional rounding.</summary>
    public const string Division = "quote-per-EUR/base-per-EUR;decimal-division;no-additional-rounding";
    /// <summary>Exact original direct publication, without a calculated inversion.</summary>
    public const string Direct = "direct-ECB-quote-per-EUR";
    private static readonly string[] currencies = ["EUR", "USD", "GBP", "CHF", "CAD", "AUD"];
    /// <summary>Locally registered metadata only; enumeration never fetches the source.</summary>
    public static MarketCatalog Catalog => new(1, "ecb", Revision, "European Central Bank", SourceUrl, LicenceUrl,
        "Source: ECB. Original reference data is freely available. Informational only; not a transaction price. Calculated inverse/cross rates are modifications; no ECB endorsement.",
        "Europe/Berlin", 300, 604800, true, new(2026, 10, 19), false,
        currencies.SelectMany(from => currencies.Where(to => to != from).Select(to =>
            new MarketSymbol(from + "-" + to, from + " / " + to, from, to, MarketUnit.Currency))).ToArray());

    /// <summary>Reproduces a positive finite source pair using the same dated original legs.</summary>
    public static decimal Calculate(decimal basePerEuro, decimal quotePerEuro)
    {
        if (basePerEuro is <= 0 or > 1_000_000_000m || quotePerEuro is <= 0 or > 1_000_000_000m)
            throw new ArgumentException("Invalid daily reference leg.");
        return quotePerEuro / basePerEuro;
    }

    /// <summary>Checks version-two provenance while old source packets retain their original version-one semantics.</summary>
    public static void Validate(MarketQuote quote)
    {
        var evidence = quote.Observation.Daily ?? throw new ArgumentException("Daily precision evidence required.");
        var original = Catalog;
        if (evidence.Version != 1 || JsonSerializer.Serialize(quote.Catalog, MarketJson.Default.MarketCatalog) !=
            JsonSerializer.Serialize(original with { Symbols = [quote.Symbol] }, MarketJson.Default.MarketCatalog) ||
            !original.Symbols.Contains(quote.Symbol) || quote.Observation.ObservedAt is not null ||
            evidence.SourceSha256 is not { Length: 64 } || evidence.SourceSha256.Any(c => !char.IsAsciiHexDigitLower(c)) ||
            evidence.FetchedAt.Offset != TimeSpan.Zero || evidence.FetchedAt > quote.RetrievedAt ||
            quote.Observation.Date > DateOnly.FromDateTime(evidence.FetchedAt.UtcDateTime) ||
            quote.Observation.Date < DateOnly.FromDateTime(evidence.FetchedAt.UtcDateTime).AddDays(-7) ||
            evidence.DocumentModifiedAt is { } modified && (modified.Offset != TimeSpan.Zero || modified > evidence.FetchedAt.AddMinutes(5)) ||
            quote.Symbol.Base == "EUR" && evidence.BasePerEuro != 1m || quote.Symbol.Currency == "EUR" && evidence.QuotePerEuro != 1m ||
            evidence.Calculation != (quote.Symbol.Base == "EUR" ? Direct : Division) ||
            quote.Observation.Value != Calculate(evidence.BasePerEuro, evidence.QuotePerEuro) ||
            quote.Observation.QuoteId != QuoteId(quote.Observation.Date, quote.Symbol, evidence.SourceSha256))
            throw new ArgumentException("Daily reference provenance differs from the original source.");
    }

    /// <summary>Stable source identity includes actual date, pair and complete original-byte digest.</summary>
    public static string QuoteId(DateOnly date, MarketSymbol symbol, string digest) =>
        "ecb:" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ":" + symbol.Id + ":" + digest;
}
