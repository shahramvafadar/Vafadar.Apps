using Microsoft.Extensions.Configuration;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Public store restoration requires signed possession, while internal refresh alone may use the already authorized canonical id.</summary>
[Trait("AT", "AT-295")]
public sealed class ApplePurchasePossessionTests
{
    /// <summary>A numeric id, fabricated JWS or oversized string cannot cause an operator credential read or provider request.</summary>
    [Theory]
    [InlineData("numeric")][InlineData("unsigned")][InlineData("oversized")]
    public async Task Public_apple_restore_refuses_identifiers_without_signed_store_possession(string scenario)
    {
        var credentials = new Credentials(); using var handler = new Handler(); using var http = new BillingProviderHttp(handler);
        var provider = new ApplePurchaseProvider(http, new(new ConfigurationBuilder().Build()), credentials,
            new(TimeProvider.System), TimeProvider.System);
        var reference = scenario switch { "numeric" => "2000000123456789", "unsigned" => "eyJhbGciOiJub25lIn0.e30.ZmFrZQ", _ => new string('x', 16385) };
        var error = await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync(reference, BillingProduct.PlusLifetime,
            BillingEnvironment.Sandbox, TestContext.Current.CancellationToken));
        Assert.Equal(422, error.Status); Assert.Equal(0, credentials.Reads); Assert.Equal(0, handler.Requests);
        Assert.DoesNotContain(reference, error.ToString());
    }

    /// <summary>The server-only already-authorized path still queries the actual fixed Apple API rather than pretending an id is a paid grant.</summary>
    [Fact]
    public async Task Internal_apple_refresh_is_separate_and_still_requires_actual_provider_verification()
    {
        var credentials = new Credentials(); using var handler = new Handler(); using var http = new BillingProviderHttp(handler);
        IBillingProvider provider = new ApplePurchaseProvider(http, new(new ConfigurationBuilder().Build()), credentials,
            new(TimeProvider.System), TimeProvider.System);
        var error = await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyExistingAsync("2000000123456789", BillingProduct.PlusLifetime,
            BillingEnvironment.Sandbox, TestContext.Current.CancellationToken));
        Assert.Equal(503, error.Status); Assert.Equal(1, credentials.Reads); Assert.Equal(1, handler.Requests);
    }
    private sealed class Credentials : IAppleBillingCredentials
    {
        internal int Reads;
        public Task<string> AccessAsync(CancellationToken ct) { Reads++; return Task.FromResult("fictional-proof-fixture-only"); }
    }
    private sealed class Handler : HttpMessageHandler
    {
        internal int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++; Assert.Equal("api.storekit-sandbox.apple.com", request.RequestUri!.Host);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") });
        }
    }
}
