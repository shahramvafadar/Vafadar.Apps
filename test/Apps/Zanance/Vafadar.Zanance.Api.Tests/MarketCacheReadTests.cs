using System.Text.Json;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Rates;
using Vafadar.Zanance.Data.Sync;
using Microsoft.Extensions.Time.Testing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual typed device-cache reader with explicit native storage/access effects, without network or financial writes.</summary>
[Trait("AT", "AT-299")]
public sealed class MarketCacheReadTests
{
    private static readonly DateTimeOffset Instant = new(2026, 10, 17, 23, 50, 0, TimeSpan.Zero);
    private sealed class Storage : IMarketPrivateCache
    {
        internal readonly Dictionary<string, string> Values = [];
        internal string? LastScope;
        internal Action? AfterRead;
        internal int Writes;
        public Task<string?> ReadAsync(string scope, CancellationToken ct)
        { LastScope = scope; var value = Values.GetValueOrDefault(scope); AfterRead?.Invoke(); return Task.FromResult(value); }
        public Task WriteAsync(string scope, string value, CancellationToken ct) { Writes++; Values[scope] = value; return Task.CompletedTask; }
    }
    private static MarketQuote Quote()
    {
        var symbol = new MarketSymbol("EURUSD", "Fictitious source EUR/USD", "EUR", "USD", MarketUnit.Currency);
        var catalog = new MarketCatalog(1, "fictitious-cache", "fixture-v1", "Fictitious market source",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious local qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), false, [symbol]);
        return new(1, new(catalog.Provider, catalog.Revision, symbol.Id, new(2026, 10, 17)), catalog, symbol,
            new("fictitious-cache-observation", new(2026, 10, 16), Instant.AddHours(-1), 1.234567890123m), Instant, Instant.AddSeconds(60), Instant.AddHours(1));
    }
    private static MarketQuoteClient Client(Storage storage, TimeProvider time, string file = "first.sqlite", Action? access = null, string origin = "https://localhost:15498/") =>
        new(storage, new LocalSyncPeer(origin, new string('f', 64)), Path.Combine(Path.GetTempPath(), "zanance-market-cache-d299", file), time, access ?? (() => { }));
    private static async Task Seed(Storage storage, MarketQuoteClient client, string raw)
    { Assert.Empty(await client.ReadAsync(TestContext.Current.CancellationToken)); storage.Values[storage.LastScope!] = raw; }
    private static string Encoded(MarketQuote quote) => JsonSerializer.Serialize(new MarketCacheState(1, [quote]), MarketCacheJson.Default.MarketCacheState);

    [Fact]
    public async Task Exact_quote_stays_readable_stale_after_midnight_without_any_native_write_or_new_lookup()
    {
        var storage = new Storage(); var clock = new FakeTimeProvider(Instant); var client = Client(storage, clock); var original = Quote();
        await Seed(storage, client, Encoded(original)); clock.Advance(TimeSpan.FromMinutes(20)); var result = Assert.Single(await client.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(original.Observation, result.Observation); Assert.True(MarketReference.IsStale(result, clock.GetUtcNow())); Assert.Equal(0, storage.Writes);
        Assert.DoesNotContain("AccountId", Encoded(original)); Assert.DoesNotContain("Quantity", Encoded(original));
    }
    [Fact]
    public async Task Another_original_profile_or_pinned_origin_cannot_read_the_first_scope_cache()
    {
        var storage = new Storage(); var clock = new FakeTimeProvider(Instant); var first = Client(storage, clock);
        await Seed(storage, first, Encoded(Quote())); var original = storage.LastScope;
        Assert.Single(await first.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await Client(storage, clock, "other.sqlite").ReadAsync(TestContext.Current.CancellationToken)); Assert.NotEqual(original, storage.LastScope);
        Assert.Empty(await Client(storage, clock, origin: "https://localhost:15499/").ReadAsync(TestContext.Current.CancellationToken)); Assert.NotEqual(original, storage.LastScope);
        Assert.Equal(0, storage.Writes);
    }
    [Fact]
    public async Task Retention_expiry_is_not_a_zero_value_or_an_automatic_cache_reset()
    {
        var storage = new Storage(); var clock = new FakeTimeProvider(Instant); var client = Client(storage, clock); var raw = Encoded(Quote());
        await Seed(storage, client, raw); clock.Advance(TimeSpan.FromHours(2)); Assert.Empty(await client.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(raw, Assert.Single(storage.Values).Value); Assert.Equal(0, storage.Writes);
    }
    [Theory][InlineData("null")][InlineData("{\"Version\":2,\"Quotes\":[]}")][InlineData("{\"Version\":1,\"Quotes\":[],\"AccountId\":\"fictitious-unrequested\"}")]
    public async Task Unsupported_or_extra_cache_fields_remain_a_visible_refusal_without_repair(string raw)
    {
        var storage = new Storage(); var client = Client(storage, new FakeTimeProvider(Instant)); await Seed(storage, client, raw);
        await Assert.ThrowsAnyAsync<Exception>(() => client.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(raw, Assert.Single(storage.Values).Value); Assert.Equal(0, storage.Writes);
    }
    [Fact]
    public async Task A_cover_or_retired_file_during_private_read_cannot_publish_its_original_cached_value()
    {
        var storage = new Storage(); var clock = new FakeTimeProvider(Instant); var allowed = true;
        var client = Client(storage, clock, access: () => { if (!allowed) throw new OperationCanceledException(); }); await Seed(storage, client, Encoded(Quote()));
        storage.AfterRead = () => allowed = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReadAsync(TestContext.Current.CancellationToken)); Assert.Equal(0, storage.Writes);
    }
    [Fact]
    public async Task Oversized_native_cache_is_refused_before_parsing_or_replacement()
    {
        var storage = new Storage(); var client = Client(storage, new FakeTimeProvider(Instant)); await Seed(storage, client, new string('x', 65537));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.ReadAsync(TestContext.Current.CancellationToken)); Assert.Equal(0, storage.Writes);
    }
}
