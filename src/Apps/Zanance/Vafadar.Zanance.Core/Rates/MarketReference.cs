using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Rates;

/// <summary>Explicit source unit; a fine gram is never silently a jewellery gram or a coin.</summary>
public enum MarketUnit
{
    /// <summary>One major unit of the named base currency.</summary>
    Currency = 0,
    /// <summary>One gram of the exact source-described material.</summary>
    Gram = 1,
    /// <summary>One gram of fine metal; any target purity conversion needs separate explicit review.</summary>
    FineGram = 2,
    /// <summary>One exact catalogued item; its price never follows from a similarly named holding.</summary>
    Piece = 3,
    /// <summary>One troy ounce of fine metal, retained as the original source unit rather than silently normalized.</summary>
    FineTroyOunce = 4,
}

/// <summary>One approved public catalog symbol, not a user's local holding/account identity or free text.</summary>
public sealed record MarketSymbol(string Id, string Label, string Base, string Currency, MarketUnit Unit, Metal? FineMetal = null);

/// <summary>Original registered source policy; live selection/rights must be approved before an adapter supplies it.</summary>
public sealed record MarketCatalog(int Version, string Provider, string Revision, string Name, string SourceUrl,
    string LicenceUrl, string Attribution, string MarketTimeZone, int FreshSeconds, int RetainSeconds,
    bool AllowsReviewedHistory, DateOnly EarliestRequestDate, bool SupportsHistoricalRequests, IReadOnlyList<MarketSymbol> Symbols);

/// <summary>Only an exact approved public symbol/date; no financial amounts, ids, names, arbitrary URL or API key.</summary>
public sealed record MarketRequest(string Provider, string Revision, string Symbol, DateOnly OnOrBefore);

/// <summary>Original provider observation; retrieval/cache policy is added by the trusted gateway, not this response.</summary>
public sealed record MarketObservation(string QuoteId, DateOnly Date, DateTimeOffset ObservedAt, decimal Value);

/// <summary>Immutable source policy with only its selected catalog symbol; numerical use still needs independent native consent.</summary>
public sealed record MarketQuote(int Version, MarketRequest Request, MarketCatalog Catalog, MarketSymbol Symbol,
    MarketObservation Observation, DateTimeOffset RetrievedAt, DateTimeOffset FreshUntil, DateTimeOffset RetainUntil);

/// <summary>Metadata-only availability; the unconfigured default cannot imply a live source or price entitlement.</summary>
public sealed record MarketAvailability(MarketCatalog? Catalog);

/// <summary>Strict provider-neutral boundaries and standard digests, independent from all financial writers.</summary>
public static class MarketReference
{
    /// <summary>Finite original source-response bound, not a commercial storage allocation.</summary>
    public const int MaximumQuoteBytes = 32768;
    /// <summary>Gets whether a source identifier is finite and cannot be a URL, path or private free-form label.</summary>
    public static bool Identifier(string? value) => value is { Length: > 0 and <= 128 } &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or ':');

    /// <summary>Refuses incomplete/unsafe source terms; no validation can itself approve a licence or provider.</summary>
    public static void Validate(MarketCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.Version != 1 || !Identifier(catalog.Provider) || !Identifier(catalog.Revision) ||
            !Text(catalog.Name, 128) || !Text(catalog.Attribution, 1024) || !Text(catalog.MarketTimeZone, 128) ||
            !PublicUrl(catalog.SourceUrl) || !PublicUrl(catalog.LicenceUrl) || !catalog.AllowsReviewedHistory || catalog.EarliestRequestDate == DateOnly.MinValue ||
            catalog.FreshSeconds is < 0 or > 604800 || catalog.RetainSeconds < Math.Max(1, catalog.FreshSeconds) || catalog.RetainSeconds > 31622400 ||
            catalog.Symbols is null || catalog.Symbols.Count is < 1 or > 128 || catalog.Symbols.Any(s => s is null))
            throw new ArgumentException("Original market source policy is unavailable.");
        if (catalog.Symbols.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Symbols.Count)
            throw new ArgumentException("Unique market catalog symbols required.");
        foreach (var symbol in catalog.Symbols)
        {
            if (!Identifier(symbol.Id) || !Text(symbol.Label, 128) || !Identifier(symbol.Base) || !Currency(symbol.Currency) || !Enum.IsDefined(symbol.Unit) ||
                symbol.Unit == MarketUnit.Currency && (!Currency(symbol.Base) || symbol.Base == symbol.Currency) ||
                symbol.Unit is MarketUnit.FineGram or MarketUnit.FineTroyOunce && symbol.FineMetal is not (Metal.Gold or Metal.Silver or Metal.Platinum or Metal.Palladium) ||
                symbol.Unit is not (MarketUnit.FineGram or MarketUnit.FineTroyOunce) && symbol.FineMetal is not null)
                throw new ArgumentException("Explicit supported market symbol/unit required.");
        }
        if (JsonSerializer.SerializeToUtf8Bytes(catalog, MarketJson.Default.MarketCatalog).Length > 24576)
            throw new ArgumentException("Market source policy exceeds its technical bound.");
    }

    /// <summary>Returns only the original catalog symbol; caller-selected URLs, inferred aliases and future requests are refused.</summary>
    public static MarketSymbol Validate(MarketCatalog catalog, MarketRequest request, DateTimeOffset now)
    {
        var symbol = ResolveSymbol(catalog, request);
        if (request.OnOrBefore > DateOnly.FromDateTime(now.UtcDateTime).AddDays(1) ||
            !catalog.SupportsHistoricalRequests && request.OnOrBefore < DateOnly.FromDateTime(now.UtcDateTime))
            throw new ArgumentException("Original market request/source differs.");
        return symbol;
    }

    /// <summary>Checks complete original packet/date/unit/policy without making it an actual payment rate or write authority.</summary>
    public static void Validate(MarketQuote quote, DateTimeOffset now, bool requireRetained = true)
    {
        ArgumentNullException.ThrowIfNull(quote);
        // Historical-fetch coverage restricts new HTTP, not an already obtained reference read after midnight.
        var symbol = ResolveSymbol(quote.Catalog, quote.Request);
        if (quote.Version != 1 || quote.Catalog.Symbols.Count != 1 || quote.Symbol != symbol || quote.Observation is not { } observation || !Identifier(observation.QuoteId) ||
            quote.Request.OnOrBefore > DateOnly.FromDateTime(quote.RetrievedAt.UtcDateTime).AddDays(1) ||
            observation.Date == DateOnly.MinValue || observation.Date > quote.Request.OnOrBefore || observation.ObservedAt.Offset != TimeSpan.Zero ||
            observation.Value is <= 0 or > 1_000_000_000_000m || quote.RetrievedAt.Offset != TimeSpan.Zero ||
            observation.ObservedAt > quote.RetrievedAt.AddMinutes(5) || quote.RetrievedAt > now.AddMinutes(5) ||
            quote.FreshUntil != quote.RetrievedAt.AddSeconds(quote.Catalog.FreshSeconds) ||
            quote.RetainUntil != quote.RetrievedAt.AddSeconds(quote.Catalog.RetainSeconds) || requireRetained && now > quote.RetainUntil)
            throw new ArgumentException("Original market reference is invalid or no longer retained.");
        if (JsonSerializer.SerializeToUtf8Bytes(quote, MarketJson.Default.MarketQuote).Length > MaximumQuoteBytes)
            throw new ArgumentException("Market reference exceeds its technical bound.");
    }

    /// <summary>Gets refresh staleness separately from the source's older market date; neither silently authorizes a replacement.</summary>
    public static bool IsStale(MarketQuote quote, DateTimeOffset now) { Validate(quote, now); return now > quote.FreshUntil; }
    /// <summary>Hashes exactly one complete frozen source/quote packet for subsequent original-consent checks.</summary>
    public static string Digest(MarketQuote quote, DateTimeOffset now, bool requireRetained = true)
    { Validate(quote, now, requireRetained); return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(quote, MarketJson.Default.MarketQuote))); }

    /// <summary>Resolves original metadata only; a new HTTP lookup still requires current-date coverage validation.</summary>
    public static MarketSymbol ResolveSymbol(MarketCatalog catalog, MarketRequest request)
    {
        Validate(catalog); ArgumentNullException.ThrowIfNull(request);
        if (request.Provider != catalog.Provider || request.Revision != catalog.Revision || !Identifier(request.Symbol) || request.OnOrBefore < catalog.EarliestRequestDate)
            throw new ArgumentException("Original market request/source differs.");
        return catalog.Symbols.SingleOrDefault(s => s.Id == request.Symbol) ?? throw new ArgumentException("Market symbol is not supported by this source.");
    }

    private static bool Currency(string? code) => code is { Length: 3 } && code.All(char.IsAsciiLetterUpper) && Currencies.TryGet(code, out _);
    private static bool Text(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum &&
        !value.Any(c => char.IsControl(c) || c is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069');
    private static bool PublicUrl(string? value) => value is { Length: > 0 and <= 512 } && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        !uri.IsLoopback && !System.Net.IPAddress.TryParse(uri.DnsSafeHost, out _) && uri.DnsSafeHost.Contains('.', StringComparison.Ordinal);
}

/// <summary>Explicit AOT/trim metadata; unknown request fields cannot become a hidden data disclosure or source policy.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MarketCatalog))]
[JsonSerializable(typeof(MarketRequest))]
[JsonSerializable(typeof(MarketObservation))]
[JsonSerializable(typeof(MarketQuote))]
[JsonSerializable(typeof(MarketAvailability))]
public sealed partial class MarketJson : JsonSerializerContext;
