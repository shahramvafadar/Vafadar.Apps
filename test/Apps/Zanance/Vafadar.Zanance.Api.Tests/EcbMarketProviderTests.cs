using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual approved adapter with synthetic public-feed bytes; no upstream account or financial data.</summary>
[Trait("AT", "AT-299")]
public sealed class EcbMarketProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 13, 0, 0, TimeSpan.Zero);
    private const string Xml = """
        <g:Envelope xmlns:g="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
        <g:subject>Reference rates</g:subject><g:Sender><g:name>European Central Bank</g:name></g:Sender>
        <Cube><Cube time="2026-10-16"><Cube currency="USD" rate="1.25"/><Cube currency="GBP" rate="0.8"/>
        <Cube currency="CHF" rate="0.9"/><Cube currency="CAD" rate="1.5"/><Cube currency="AUD" rate="1.6"/></Cube></Cube></g:Envelope>
        """;
    private sealed class Source : HttpMessageHandler
    {
        internal int Calls;
        internal readonly TaskCompletionSource Called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond = (request, _) => Task.FromResult(Response(request, Xml));
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls); Called.TrySetResult();
            Assert.Equal(EcbReference.FeedUrl, request.RequestUri!.AbsoluteUri); Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content); Assert.Null(request.Headers.Authorization); Assert.False(request.Headers.Contains("Cookie"));
            return Respond(request, ct);
        }
    }
    private static HttpResponseMessage Response(HttpRequestMessage request, string xml) => new(HttpStatusCode.OK)
    { RequestMessage = request, Content = new StringContent(xml, Encoding.UTF8, "text/xml") };
    private static MarketRequest Request(string pair = "EUR-USD") => new("ecb", EcbReference.Revision, pair, new(2026, 10, 19));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact(Explicit = true)]
    public async Task Actual_fixed_public_feed_one_request_qualifies_all_pairs_without_accounts_or_financial_writes()
    {
        using var client = EcbMarketProvider.CreateClient(); using var provider = new EcbMarketProvider(client, TimeProvider.System);
        var gateway = new MarketGateway(provider, TimeProvider.System); var quotes = new List<MarketQuote>();
        var day = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        foreach (var symbol in EcbReference.Catalog.Symbols)
            quotes.Add(await gateway.FetchAsync(new("ecb", EcbReference.Revision, symbol.Id, day), Ct));
        Assert.Equal(30, quotes.Count); Assert.Single(quotes.Select(value => value.Observation.Daily!.SourceSha256).Distinct());
        Assert.Single(quotes.Select(value => value.Observation.Daily!.FetchedAt).Distinct());
        var root = Path.GetFullPath(Path.Combine("artifacts", "ecb-integration-d389")); Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "actual-adapter-public-result.json"), JsonSerializer.Serialize(quotes, new JsonSerializerOptions { WriteIndented = true }), Ct);
    }

    [Fact]
    public async Task Another_pair_does_not_extend_original_feed_freshness()
    {
        using var source = new Source(); using var http = new HttpClient(source); var clock = new FakeTimeProvider(Now);
        var gateway = new MarketGateway(new EcbMarketProvider(http, clock), clock);
        var first = await gateway.FetchAsync(Request(), Ct); clock.Advance(TimeSpan.FromMinutes(4));
        var second = await gateway.FetchAsync(Request("USD-CHF"), Ct);
        Assert.Equal(first.RetrievedAt, second.RetrievedAt); Assert.Equal(first.FreshUntil, second.FreshUntil); Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task All_thirty_pairs_share_one_dated_feed_and_preserve_direct_or_calculated_original_legs()
    {
        using var source = new Source(); using var http = new HttpClient(source); var clock = new FakeTimeProvider(Now);
        var gateway = new MarketGateway(new EcbMarketProvider(http, clock), clock);
        Assert.Equal(30, gateway.Availability().Catalog!.Symbols.Count); Assert.Equal(0, source.Calls);
        var quotes = new List<MarketQuote>();
        foreach (var symbol in EcbReference.Catalog.Symbols)
        {
            var quote = await gateway.FetchAsync(Request(symbol.Id), Ct); quotes.Add(quote);
            Assert.Equal(2, quote.Version); Assert.Equal(new DateOnly(2026, 10, 16), quote.Observation.Date);
            Assert.Null(quote.Observation.ObservedAt);
            var daily = Assert.IsType<EcbDailyEvidence>(quote.Observation.Daily);
            Assert.Equal(Now, daily.FetchedAt); Assert.Null(daily.DocumentModifiedAt);
            Assert.Equal(daily.QuotePerEuro / daily.BasePerEuro, quote.Observation.Value);
            Assert.Equal(symbol.Base == "EUR" ? EcbReference.Direct : EcbReference.Division, daily.Calculation);
            var encoded = JsonSerializer.Serialize(quote, MarketJson.Default.MarketQuote);
            Assert.Equal(MarketReference.Digest(quote, Now), MarketReference.Digest(JsonSerializer.Deserialize(encoded, MarketJson.Default.MarketQuote)!, Now));
        }
        Assert.Equal(1, source.Calls); Assert.Single(quotes.Select(q => q.Observation.Daily!.SourceSha256).Distinct());
        Assert.Equal(5, quotes.Count(q => q.Observation.Daily!.Calculation == EcbReference.Direct));
        Assert.Equal(25, quotes.Count(q => q.Observation.Daily!.Calculation == EcbReference.Division));
    }

    [Fact]
    public async Task Different_pairs_join_original_feed_after_first_caller_cancellation()
    {
        using var source = new Source(); using var http = new HttpClient(source); var clock = new FakeTimeProvider(Now);
        var completion = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpRequestMessage? original = null;
        source.Respond = (request, _) => { original = request; return completion.Task; };
        var provider = new EcbMarketProvider(http, clock); using var cancel = new CancellationTokenSource();
        var first = provider.FetchAsync(Request(), cancel.Token); await source.Called.Task.WaitAsync(Ct); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var second = provider.FetchAsync(Request("USD-CHF"), Ct); Assert.Equal(1, source.Calls);
        completion.SetResult(Response(original!, Xml)); Assert.Equal(0.72m, (await second).Value); Assert.Equal(1, source.Calls);
    }

    [Theory]
    [InlineData("future")][InlineData("old")][InlineData("duplicate")][InlineData("missing")]
    [InlineData("zero")][InlineData("negative")][InlineData("exponent")][InlineData("namespace")]
    [InlineData("dtd")][InlineData("depth")][InlineData("oversize")][InlineData("unknown")]
    public async Task Invalid_refresh_never_erases_last_good_source_or_calls_it_fresh(string failure)
    {
        using var source = new Source(); using var http = new HttpClient(source); var clock = new FakeTimeProvider(Now);
        var gateway = new MarketGateway(new EcbMarketProvider(http, clock), clock);
        var good = await gateway.FetchAsync(Request(), Ct); clock.Advance(TimeSpan.FromMinutes(6));
        var broken = failure switch
        {
            "future" => Xml.Replace("2026-10-16", "2026-10-21", StringComparison.Ordinal),
            "old" => Xml.Replace("2026-10-16", "2026-10-01", StringComparison.Ordinal),
            "duplicate" => Xml.Replace("CAD", "USD", StringComparison.Ordinal),
            "missing" => Xml.Replace("AUD", "JPY", StringComparison.Ordinal),
            "zero" => Xml.Replace("1.25", "0", StringComparison.Ordinal),
            "negative" => Xml.Replace("1.25", "-1.25", StringComparison.Ordinal),
            "exponent" => Xml.Replace("1.25", "1e2", StringComparison.Ordinal),
            "namespace" => Xml.Replace("www.ecb.int", "foreign.invalid", StringComparison.Ordinal),
            "dtd" => "<!DOCTYPE Envelope [<!ENTITY x SYSTEM 'file:///unread'>]>" + Xml,
            "depth" => Xml.Replace("rate=\"1.25\"/>", "rate=\"1.25\"><Cube><Cube/></Cube></Cube>", StringComparison.Ordinal),
            "oversize" => Xml + new string(' ', EcbMarketProvider.MaximumBytes),
            _ => Xml.Replace("rate=\"1.25\"", "rate=\"1.25\" extra=\"1\"", StringComparison.Ordinal),
        };
        source.Respond = (request, _) => Task.FromResult(Response(request, broken));
        Assert.Equal(502, (await Assert.ThrowsAsync<MarketRefusal>(() => gateway.FetchAsync(Request(), Ct))).Status);
        Assert.Null(gateway.CachedReference(Request(), false));
        Assert.Equal(MarketReference.Digest(good, clock.GetUtcNow()), MarketReference.Digest(gateway.CachedReference(Request(), true)!, clock.GetUtcNow()));
        Assert.Equal(2, source.Calls);
    }

    [Theory][InlineData("redirect")][InlineData("compressed")][InlineData("content")][InlineData("modified")][InlineData("regressed")]
    public async Task Unexpected_transport_or_regressed_publication_is_refused(string failure)
    {
        using var source = new Source(); using var http = new HttpClient(source); var clock = new FakeTimeProvider(Now);
        var provider = new EcbMarketProvider(http, clock);
        await provider.FetchAsync(Request(), Ct); clock.Advance(TimeSpan.FromMinutes(6));
        source.Respond = (request, _) =>
        {
            var response = Response(request, failure == "regressed" ? Xml.Replace("2026-10-16", "2026-10-15", StringComparison.Ordinal) : Xml);
            switch (failure)
            {
                case "redirect": response.StatusCode = HttpStatusCode.Redirect; break;
                case "compressed": response.Content.Headers.ContentEncoding.Add("gzip"); break;
                case "content": response.Content.Headers.ContentType!.MediaType = "text/html"; break;
                case "modified": response.Content.Headers.LastModified = clock.GetUtcNow().AddHours(1); break;
            }
            return Task.FromResult(response);
        };
        await Assert.ThrowsAsync<MarketRefusal>(() => provider.FetchAsync(Request(), Ct));
    }

    [Fact]
    public async Task Unknown_symbol_and_historical_request_never_dispatch_upstream()
    {
        using var source = new Source(); using var http = new HttpClient(source); var provider = new EcbMarketProvider(http, new FakeTimeProvider(Now));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.FetchAsync(Request("XAU-EUR"), Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.FetchAsync(Request() with { OnOrBefore = new(2026, 10, 18) }, Ct));
        Assert.Equal(0, source.Calls);
    }
}
