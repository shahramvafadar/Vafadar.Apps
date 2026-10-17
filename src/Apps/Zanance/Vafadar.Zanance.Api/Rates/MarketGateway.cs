using System.Text.Json;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Bounded original-source public cache; one live provider lookup per exact request, independent of caller cancellation.</summary>
internal sealed class MarketGateway(IMarketProvider provider, TimeProvider time)
{
    private sealed record Cached(string Policy, string Quote, DateTimeOffset FreshUntil, DateTimeOffset RetainUntil);
    private readonly object gate = new();
    private readonly Dictionary<MarketRequest, Cached> cache = [];
    private readonly Dictionary<MarketRequest, Task<MarketQuote>> pending = [];
    private const int MaximumCached = 128;
    private const int MaximumPending = 8;
    /// <summary>Only original approved metadata, never an implicit quote request or commercial entitlement.</summary>
    internal MarketAvailability Availability() => new(Catalog());

    /// <summary>Reads a valid cache or joins one original lookup; no money/quantity/history is written.</summary>
    internal async Task<MarketQuote> FetchAsync(MarketRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        var catalog = Catalog() ?? throw new MarketRefusal(503, "MarketProviderUnavailable");
        var policy = JsonSerializer.Serialize(catalog, MarketJson.Default.MarketCatalog);
        Task<MarketQuote> original;
        lock (gate)
        {
            var now = time.GetUtcNow();
            foreach (var expired in cache.Where(pair => pair.Value.RetainUntil < now).Select(pair => pair.Key).ToArray()) cache.Remove(expired);
            if (cache.TryGetValue(request, out var saved) && saved.Policy == policy && saved.FreshUntil >= now)
            {
                var value = Decode(saved.Quote); MarketReference.Validate(value, now); return value;
            }
            if (!pending.TryGetValue(request, out original!))
            {
                MarketReference.Validate(catalog, request, now);
                if (pending.Count >= MaximumPending) throw new MarketRefusal(429, "MarketLookupBound");
                // Schedule after releasing the monitor. Even a synchronous adapter cannot call back into the cache while it is half-published.
                original = Task.Run(() => LookupAsync(request, catalog, policy)); pending.Add(request, original);
                _ = RetireAsync(request, original);
            }
        }
        // A client abort or observation deadline does not retire the original live task and manufacture a parallel paid lookup.
        try { return Decode(JsonSerializer.Serialize(await original.WaitAsync(TimeSpan.FromSeconds(20), time, ct), MarketJson.Default.MarketQuote)); }
        catch (TimeoutException) { throw new MarketRefusal(504, "MarketLookupPending"); }
    }

    /// <summary>Reads the last retained original source without HTTP, explicitly including stale values only when requested.</summary>
    internal MarketQuote? CachedReference(MarketRequest request, bool includeStale)
    {
        var catalog = Catalog(); if (catalog is null) return null;
        MarketReference.ResolveSymbol(catalog, request); var policy = JsonSerializer.Serialize(catalog, MarketJson.Default.MarketCatalog);
        lock (gate)
        {
            if (!cache.TryGetValue(request, out var value) || value.Policy != policy) return null;
            var now = time.GetUtcNow();
            if (value.RetainUntil < now) { cache.Remove(request); return null; }
            if (!includeStale && value.FreshUntil < now) return null;
            var quote = Decode(value.Quote); MarketReference.Validate(quote, now); return quote;
        }
    }

    private async Task<MarketQuote> LookupAsync(MarketRequest request, MarketCatalog catalog, string policy)
    {
        // Actual adapters must honor this cancellation. If one does not, its task remains joined/bounded until actually terminal.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15), time);
        try
        {
            var before = Catalog();
            if (before is null || JsonSerializer.Serialize(before, MarketJson.Default.MarketCatalog) != policy)
                throw new MarketRefusal(409, "MarketOriginalSourceChanged");
            var observation = await provider.FetchAsync(request, deadline.Token);
            var now = time.GetUtcNow(); var symbol = catalog.Symbols.Single(s => s.Id == request.Symbol);
            var quote = new MarketQuote(1, request, catalog with { Symbols = [symbol] }, symbol, observation, now,
                now.AddSeconds(catalog.FreshSeconds), now.AddSeconds(catalog.RetainSeconds));
            MarketReference.Validate(quote, now);
            var active = Catalog();
            if (active is null || JsonSerializer.Serialize(active, MarketJson.Default.MarketCatalog) != policy)
                throw new MarketRefusal(409, "MarketOriginalSourceChanged");
            var encoded = JsonSerializer.Serialize(quote, MarketJson.Default.MarketQuote);
            lock (gate)
            {
                if (cache.Count >= MaximumCached && !cache.ContainsKey(request))
                    cache.Remove(cache.MinBy(pair => pair.Value.RetainUntil).Key);
                cache[request] = new(policy, encoded, quote.FreshUntil, quote.RetainUntil);
            }
            return quote;
        }
        catch (MarketRefusal) { throw; }
        catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
        { throw new MarketRefusal(503, "MarketSourceUnavailable"); }
        catch (Exception error) when (error is ArgumentException or JsonException or OverflowException)
        { throw new MarketRefusal(502, "MarketSourceResponseRefused"); }
    }
    private async Task RetireAsync(MarketRequest request, Task<MarketQuote> original)
    {
        try { await original; } catch (Exception error) when (error is not OutOfMemoryException) { }
        lock (gate) if (pending.TryGetValue(request, out var actual) && ReferenceEquals(actual, original)) pending.Remove(request);
    }
    private MarketCatalog? Catalog()
    {
        var current = provider.Catalog; if (current is null) return null;
        // Freeze actual locally registered terms; mutable adapter lists cannot replace an original in-flight policy.
        MarketReference.Validate(current);
        return JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(current, MarketJson.Default.MarketCatalog), MarketJson.Default.MarketCatalog)!;
    }
    private static MarketQuote Decode(string encoded) => JsonSerializer.Deserialize(encoded, MarketJson.Default.MarketQuote)!;
}
