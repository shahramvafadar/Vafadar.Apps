using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual HTTP source routes and version-two serializer; only authentication/upstream effects are fictitious.</summary>
[Trait("AT", "AT-299")]
public sealed class EcbMarketRouteTests
{
    [Theory][InlineData(null, false)][InlineData("", false)][InlineData("ecb", true)]
    public void Source_registration_stays_closed_unless_the_approved_adapter_is_explicitly_selected(string? source, bool enabled)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System);
        MarketModule.Add(services, source); using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IMarketProvider>().Catalog;
        if (enabled) Assert.Equal(30, catalog!.Symbols.Count); else Assert.Null(catalog);
    }
    [Fact]
    public void An_unreviewed_source_or_arbitrary_url_is_not_a_configuration_escape()
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System);
        Assert.Throws<InvalidOperationException>(() => MarketModule.Add(services, "https://foreign.invalid/feed"));
    }
    private sealed class Identity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(Request.Headers.ContainsKey("Fictitious-Identity")
            ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "fictitious-ecb-review")], "fixture")), "fixture"))
            : AuthenticateResult.NoResult());
    }
    private sealed class Source : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            var day = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(
                "<g:Envelope xmlns:g=\"http://www.gesmes.org/xml/2002-08-01\" xmlns=\"http://www.ecb.int/vocabulary/2002-08-01/eurofxref\"><g:subject>Reference rates</g:subject><g:Sender><g:name>European Central Bank</g:name></g:Sender><Cube><Cube time=\"" + day +
                "\"><Cube currency=\"USD\" rate=\"1.25\"/><Cube currency=\"GBP\" rate=\"0.8\"/><Cube currency=\"CHF\" rate=\"0.9\"/><Cube currency=\"CAD\" rate=\"1.5\"/><Cube currency=\"AUD\" rate=\"1.6\"/></Cube></Cube></g:Envelope>", System.Text.Encoding.UTF8, "text/xml") });
        }
    }
    [Fact]
    public async Task Approved_source_routes_keep_authentication_and_https_and_publish_explicit_daily_precision_only_on_fetch()
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
        builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, Identity>("fixture", _ => { });
        builder.Services.AddAuthorization(); builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddSingleton(new ServiceStorage { Root = Path.GetTempPath(), LocalMailbox = false, ApprovedEcbReference = true });
        MarketModule.Add(builder.Services, "ecb"); using var source = new Source(); using var http = new HttpClient(source);
        builder.Services.AddSingleton<IMarketProvider>(new EcbMarketProvider(http, TimeProvider.System));
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); MarketModule.Map(app); await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient(); client.BaseAddress = new("https://localhost");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/market/availability", TestContext.Current.CancellationToken)).StatusCode);
        client.DefaultRequestHeaders.Add("Fictitious-Identity", "yes");
        var availability = await client.GetFromJsonAsync("/market/availability", MarketJson.Default.MarketAvailability, TestContext.Current.CancellationToken);
        Assert.Equal(30, availability!.Catalog!.Symbols.Count); Assert.Equal(0, source.Calls);
        var request = new MarketRequest("ecb", EcbReference.Revision, "USD-CHF", DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime));
        using var response = await client.PostAsJsonAsync("/market/quote", request, MarketJson.Default.MarketRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode(); var quote = await response.Content.ReadFromJsonAsync(MarketJson.Default.MarketQuote, TestContext.Current.CancellationToken);
        Assert.Equal(2, quote!.Version); Assert.Null(quote.Observation.ObservedAt); Assert.Equal(0.72m, quote.Observation.Value);
        Assert.Equal(EcbReference.Division, quote.Observation.Daily!.Calculation); MarketReference.Validate(quote, DateTimeOffset.UtcNow);
        Assert.Equal(426, (int)(await client.GetAsync("http://localhost/market/availability", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(1, source.Calls);
    }
}
