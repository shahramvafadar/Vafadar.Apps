using Microsoft.Extensions.Time.Testing;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Original provider policy/cache/in-flight behavior with an explicit fictional adapter; no licence or real-source acceptance.</summary>
[Trait("AT", "AT-299")]
public sealed class MarketGatewayTests
{
    private static readonly DateTimeOffset Instant = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private static MarketCatalog Catalog() => new(1, "fictitious-market", "fixture-v1", "Fictitious local source",
        "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious local qualification only",
        "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true,
        [new("EURUSD", "Fictitious EUR to USD", "EUR", "USD", MarketUnit.Currency), new("XAU-EUR", "Fictitious fine gold reference", "XAU", "EUR", MarketUnit.FineGram, Metal.Gold)]);
    private static MarketRequest Request() => new("fictitious-market", "fixture-v1", "EURUSD", new(2026, 10, 17));
    private static MarketObservation Observation() => new("fictitious-observation-1", new(2026, 10, 16), Instant.AddHours(-1), 1.123456789012m);
    private sealed class Provider : IMarketProvider
    {
        internal MarketCatalog Terms = MarketGatewayTests.Catalog();
        internal Func<MarketRequest, CancellationToken, Task<MarketObservation>> Handler = (_, _) => Task.FromResult(Observation());
        internal int Calls;
        internal readonly TaskCompletionSource Called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public MarketCatalog? Catalog => Terms;
        public Task<MarketObservation> FetchAsync(MarketRequest request, CancellationToken ct)
        { Interlocked.Increment(ref Calls); Called.TrySetResult(); return Handler(request, ct); }
    }
    [Fact]
    public async Task Default_provider_stays_unavailable_without_an_implicit_source_or_entitlement()
    {
        var gateway = new MarketGateway(new UnconfiguredMarketProvider(), new FakeTimeProvider(Instant));
        Assert.Null(gateway.Availability().Catalog);
        Assert.Equal(503, (await Assert.ThrowsAsync<MarketRefusal>(() => gateway.FetchAsync(Request(), TestContext.Current.CancellationToken))).Status);
    }
    [Fact]
    public async Task Fresh_cache_preserves_the_exact_date_unit_precision_source_and_only_selected_symbol()
    {
        var provider = new Provider(); var clock = new FakeTimeProvider(Instant); var gateway = new MarketGateway(provider, clock);
        var first = await gateway.FetchAsync(Request(), TestContext.Current.CancellationToken);
        Assert.Equal(Observation(), first.Observation); Assert.Equal(MarketUnit.Currency, first.Symbol.Unit); Assert.Single(first.Catalog.Symbols);
        Assert.Equal(Instant, first.RetrievedAt); Assert.False(MarketReference.IsStale(first, clock.GetUtcNow()));
        Assert.Equal(MarketReference.Digest(first, clock.GetUtcNow()), MarketReference.Digest(await gateway.FetchAsync(Request(), TestContext.Current.CancellationToken), clock.GetUtcNow()));
        Assert.Equal(1, provider.Calls);
        Assert.IsAssignableFrom<IList<MarketSymbol>>(first.Catalog.Symbols).Clear();
        Assert.Single(gateway.CachedReference(Request(), false)!.Catalog.Symbols); Assert.Equal(1, provider.Calls);
    }
    [Fact]
    public async Task Caller_cancellation_joins_the_same_actual_live_lookup_without_a_parallel_dispatch()
    {
        var source = new TaskCompletionSource<MarketObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider { Handler = (_, _) => source.Task }; var gateway = new MarketGateway(provider, new FakeTimeProvider(Instant));
        using var caller = new CancellationTokenSource(); var first = gateway.FetchAsync(Request(), caller.Token);
        await provider.Called.Task.WaitAsync(TestContext.Current.CancellationToken); caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var second = gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); Assert.Equal(1, provider.Calls);
        source.SetResult(Observation()); Assert.Equal(Observation(), (await second).Observation); Assert.Equal(1, provider.Calls);
    }
    [Fact]
    public async Task A_timed_out_observer_cannot_retire_a_provider_that_ignored_cancellation_and_start_another_call()
    {
        var source = new TaskCompletionSource<MarketObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider { Handler = (_, _) => source.Task }; var clock = new FakeTimeProvider(Instant); var gateway = new MarketGateway(provider, clock);
        var first = gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); await provider.Called.Task.WaitAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(21)); Assert.Equal(504, (await Assert.ThrowsAsync<MarketRefusal>(() => first)).Status);
        var second = gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); Assert.Equal(1, provider.Calls);
        source.SetResult(Observation()); Assert.Equal(Observation(), (await second).Observation); Assert.Equal(1, provider.Calls);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task A_failed_or_invalid_refresh_preserves_last_good_stale_data_without_calling_it_fresh(bool invalid)
    {
        var provider = new Provider(); var clock = new FakeTimeProvider(Instant); var gateway = new MarketGateway(provider, clock);
        var original = await gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); clock.Advance(TimeSpan.FromSeconds(61));
        provider.Handler = invalid ? (_, _) => Task.FromResult(Observation() with { Date = new(2026, 10, 18) }) : (_, _) => throw new IOException("Fictitious unavailable source.");
        var failure = await Assert.ThrowsAsync<MarketRefusal>(() => gateway.FetchAsync(Request(), TestContext.Current.CancellationToken)); Assert.Equal(invalid ? 502 : 503, failure.Status);
        Assert.Null(gateway.CachedReference(Request(), false)); var stale = gateway.CachedReference(Request(), true); Assert.NotNull(stale);
        Assert.True(MarketReference.IsStale(stale, clock.GetUtcNow())); Assert.Equal(original.Observation, stale.Observation); Assert.Equal(2, provider.Calls);
        clock.Advance(TimeSpan.FromHours(1)); Assert.Null(gateway.CachedReference(Request(), true)); Assert.Equal(2, provider.Calls);
    }
    [Fact]
    public async Task Changed_original_source_terms_during_a_lookup_are_never_cached_under_the_old_consent()
    {
        var source = new TaskCompletionSource<MarketObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider { Handler = (_, _) => source.Task }; var gateway = new MarketGateway(provider, new FakeTimeProvider(Instant));
        var pending = gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); await provider.Called.Task.WaitAsync(TestContext.Current.CancellationToken);
        provider.Terms = provider.Terms with { Revision = "fixture-v2" }; source.SetResult(Observation());
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketRefusal>(() => pending)).Status);
        Assert.Equal(1, provider.Calls); Assert.Throws<ArgumentException>(() => gateway.CachedReference(Request(), true));
    }
    [Fact]
    public async Task Unsupported_historical_lookup_is_refused_before_any_provider_request()
    {
        var provider = new Provider { Terms = Catalog() with { SupportsHistoricalRequests = false } };
        var gateway = new MarketGateway(provider, new FakeTimeProvider(Instant));
        await Assert.ThrowsAsync<ArgumentException>(() => gateway.FetchAsync(Request() with { OnOrBefore = new(2026, 10, 16) }, TestContext.Current.CancellationToken));
        Assert.Equal(0, provider.Calls);
    }
    [Fact]
    public async Task Latest_only_source_cache_remains_readable_after_midnight_without_authorizing_an_unsupported_historical_fetch()
    {
        var provider = new Provider { Terms = Catalog() with { SupportsHistoricalRequests = false } };
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 17, 23, 50, 0, TimeSpan.Zero)); var gateway = new MarketGateway(provider, clock);
        var original = await gateway.FetchAsync(Request(), TestContext.Current.CancellationToken); clock.Advance(TimeSpan.FromMinutes(20));
        var cached = gateway.CachedReference(Request(), true); Assert.NotNull(cached); Assert.True(MarketReference.IsStale(cached, clock.GetUtcNow()));
        Assert.Equal(original.Observation, cached.Observation);
        await Assert.ThrowsAsync<ArgumentException>(() => gateway.FetchAsync(Request(), TestContext.Current.CancellationToken)); Assert.Equal(1, provider.Calls);
    }
    [Theory][InlineData("https://user:secret@example.invalid/reference")][InlineData("https://source.example.invalid/?key=not-a-real-key")][InlineData("http://source.example.invalid/reference")]
    public void Source_metadata_never_embeds_a_credential_query_or_unencrypted_link(string unsafeUrl)
        => Assert.Throws<ArgumentException>(() => MarketReference.Validate(Catalog() with { SourceUrl = unsafeUrl }));
}
