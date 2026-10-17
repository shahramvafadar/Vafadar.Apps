using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Rates;

/// <summary>Device-private bounded public-reference cache, outside portable preferences and financial authority.</summary>
public interface IMarketPrivateCache
{
    /// <summary>Reads only the exact original peer/file scope; cryptographic/storage failure is not an empty valid cache.</summary>
    Task<string?> ReadAsync(string scope, CancellationToken ct);
    /// <summary>Publishes at most64KiB of verified cache state; never stores a provider key or financial account.</summary>
    Task WriteAsync(string scope, string value, CancellationToken ct);
}

/// <summary>Device-only bounded original quote collection; accepted financial provenance is a separately reviewed persistence concern.</summary>
public sealed record MarketCacheState(int Version, IReadOnlyList<MarketQuote> Quotes);

/// <summary>Original file/peer reference cache and one explicit typed fetch; no ledger/rate/valuation writer is reachable here.</summary>
public sealed class MarketQuoteClient(IMarketPrivateCache storage, LocalSyncPeer peer, string originalFile, TimeProvider time, Action verifyAccess)
{
    private const int MaximumCacheBytes = 65536;
    private const int MaximumQuotes = 12;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Operations = new(StringComparer.Ordinal);
    private string Scope => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.market.cache.v1\n" +
        peer.BindingKey("public-market-reference") + "\n" + Path.GetFullPath(originalFile))));
    /// <summary>Reads still-retained original references, with their own staleness/source; no sign-in, HTTP or financial mutation.</summary>
    public async Task<IReadOnlyList<MarketQuote>> ReadAsync(CancellationToken ct = default)
    {
        verifyAccess(); var raw = await storage.ReadAsync(Scope, ct); verifyAccess();
        if (raw is null) return [];
        if (Encoding.UTF8.GetByteCount(raw) > MaximumCacheBytes) throw new InvalidDataException("Market cache bound exceeded.");
        var stored = JsonSerializer.Deserialize(raw, MarketCacheJson.Default.MarketCacheState) ?? throw new InvalidDataException("Original market cache unavailable.");
        if (stored.Version != 1 || stored.Quotes is null || stored.Quotes.Count > MaximumQuotes || stored.Quotes.Any(q => q is null) ||
            stored.Quotes.Select(q => q.Request).Distinct().Count() != stored.Quotes.Count)
            throw new InvalidDataException("Original market cache differs.");
        var now = time.GetUtcNow();
        foreach (var quote in stored.Quotes) MarketReference.Validate(quote, now, requireRetained: false);
        return stored.Quotes.Where(q => q.RetainUntil >= now).OrderByDescending(q => q.RetrievedAt).ToArray();
    }
    /// <summary>Checks actual original metadata explicitly; configured market rights are not inferred from a subscription.</summary>
    public async Task<MarketAvailability> AvailabilityAsync(PersonalSyncTransport transport, string expectedUser, CancellationToken ct = default)
    {
        OriginalPeer(transport); verifyAccess(); var result = await transport.MarketAvailabilityAsync(expectedUser, ct); verifyAccess();
        if (result.Catalog is not null) MarketReference.Validate(result.Catalog); return result;
    }
    /// <summary>Fetches only an explicit original public request; failure keeps the previous good cache and never retries automatically.</summary>
    public async Task<MarketQuote> FetchAsync(PersonalSyncTransport transport, string expectedUser, MarketCatalog original,
        MarketRequest request, CancellationToken ct = default)
    {
        OriginalPeer(transport); verifyAccess(); MarketReference.Validate(original, request, time.GetUtcNow());
        var policy = JsonSerializer.Serialize(original, MarketJson.Default.MarketCatalog);
        var frozen = JsonSerializer.Deserialize(policy, MarketJson.Default.MarketCatalog)!;
        var selected = MarketReference.Validate(frozen, request, time.GetUtcNow());
        var quote = await transport.MarketQuoteAsync(expectedUser, request, ct); verifyAccess(); MarketReference.Validate(quote, time.GetUtcNow());
        // A compact result retains only its selected public symbol, while every original source/licence/cache term must match.
        var expected = frozen with { Symbols = [selected] };
        if (quote.Request != request || quote.Symbol != selected || JsonSerializer.Serialize(quote.Catalog, MarketJson.Default.MarketCatalog) !=
            JsonSerializer.Serialize(expected, MarketJson.Default.MarketCatalog)) throw new InvalidDataException("Original market source terms changed.");
        var semaphore = Operations.GetOrAdd(Scope, _ => new SemaphoreSlim(1, 1)); await semaphore.WaitAsync(ct);
        try
        {
            var prior = await ReadAsync(ct); verifyAccess();
            if (prior.Any(q => q.Request == request && q.RetrievedAt > quote.RetrievedAt))
                return quote; // Keep a newer valid cache when another original window's older response arrives late.
            var quotes = prior.Where(q => q.Request != request).Prepend(quote).Take(MaximumQuotes).ToList();
            string encoded;
            while (true)
            {
                encoded = JsonSerializer.Serialize(new MarketCacheState(1, quotes), MarketCacheJson.Default.MarketCacheState);
                if (Encoding.UTF8.GetByteCount(encoded) <= MaximumCacheBytes) break;
                if (quotes.Count <= 1) throw new InvalidDataException("Original market reference cannot fit its cache bound.");
                quotes.RemoveAt(quotes.Count - 1); // Only dispensable reference cache, never accepted prices/history/provenance.
            }
            verifyAccess(); await storage.WriteAsync(Scope, encoded, ct); verifyAccess(); return quote;
        }
        finally { semaphore.Release(); }
    }
    private void OriginalPeer(PersonalSyncTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        if (transport.Peer.BindingKey("public-market-reference") != peer.BindingKey("public-market-reference"))
            throw new InvalidOperationException("Original market service changed.");
    }
}

/// <summary>Explicit trimmed/AOT private cache metadata; no reflection fallback or silent unknown-field repair.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowDuplicateProperties = false)]
[JsonSerializable(typeof(MarketCacheState))]
public sealed partial class MarketCacheJson : JsonSerializerContext;
