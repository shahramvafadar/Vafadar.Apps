using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Actual provider parser/acknowledgement over the documented two-item deferred response; HTTP is an explicit fixture port.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingDeferredProviderTests
{
    /// <summary>The new token retains current Pro until expiry, exposes scheduled Plus separately and acknowledges the actual current SKU.</summary>
    [Fact]
    public async Task Deferred_item_keeps_only_current_paid_rights_and_actual_acknowledgement_product()
    {
        var ct = TestContext.Current.CancellationToken; using var handler = new Handler("valid"); using var http = new BillingProviderHttp(handler);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new FakeTimeProvider(new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero)));
        var verified = await provider.VerifyAsync("fictional-new-deferred-token", BillingProduct.PlusYearly, BillingEnvironment.Sandbox, ct);
        Assert.Equal(BillingProduct.ProMonthly, verified.Facts.Product); Assert.Equal("fictional-old-pro-token", verified.ReplacedReference);
        Assert.Equal(BillingProduct.PlusYearly, verified.Facts.ScheduledChange!.Product);
        Assert.Equal(verified.Facts.PaidUntil, verified.Facts.ScheduledChange.EffectiveAt);
        Assert.Equal(ProductPlan.Pro, verified.Facts.ToEntitlement(BillingIssuer.GooglePlay, new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero))!.Plan);
        Assert.Null(verified.Facts.ToEntitlement(BillingIssuer.GooglePlay, verified.Facts.PaidUntil!.Value));
        await provider.AcknowledgeAsync(verified, ct); Assert.Equal(1, handler.Acknowledgements);
    }

    /// <summary>Missing/ambiguous future product evidence and installment plans never fabricate a supported scheduled target.</summary>
    [Theory]
    [InlineData("missing-offer")][InlineData("different-future-product")][InlineData("installment")]
    public async Task Unsupported_future_item_is_refused_before_a_native_finish(string scenario)
    {
        var ct = TestContext.Current.CancellationToken; using var handler = new Handler(scenario); using var http = new BillingProviderHttp(handler);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new FakeTimeProvider(new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero)));
        var error = await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync("fictional-new-deferred-token", BillingProduct.PlusYearly, BillingEnvironment.Sandbox, ct));
        Assert.Equal(422, error.Status); Assert.Equal(0, handler.Acknowledgements);
    }

    private static BillingCatalog Catalog() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Billing:Catalog:GooglePlay:Sandbox:ProMonthly:ProductId"] = "pro.monthly",
        ["Billing:Catalog:GooglePlay:Sandbox:ProMonthly:BasePlanId"] = "monthly",
        ["Billing:Catalog:GooglePlay:Sandbox:ProMonthly:TermsVersion"] = "original-pro",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusYearly:ProductId"] = "plus.yearly",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusYearly:BasePlanId"] = "yearly",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusYearly:TermsVersion"] = "accepted-next-plus",
    }).Build());
    private sealed class Credentials : IGoogleBillingCredentials
    { public Task<string> AccessAsync(CancellationToken ct) => Task.FromResult("fictional-provider-fixture-only"); }
    private sealed class Handler(string scenario) : HttpMessageHandler
    {
        internal int Acknowledgements;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal("androidpublisher.googleapis.com", request.RequestUri!.Host);
            if (request.Method == HttpMethod.Post)
            {
                Assert.Contains("/subscriptions/pro.monthly/tokens/", request.RequestUri.AbsolutePath); Acknowledgements++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
            }
            var old = new Dictionary<string, object?>
            {
                ["productId"] = "pro.monthly", ["expiryTime"] = "2026-11-01T00:00:00Z", ["latestSuccessfulOrderId"] = "fictional-successful-order",
                ["autoRenewingPlan"] = scenario == "installment" ? new { autoRenewEnabled = false, installmentDetails = new { } } : (object)new { autoRenewEnabled = false },
                ["offerDetails"] = new { basePlanId = "monthly" }, ["offerPhase"] = new { basePrice = new { } },
                ["deferredItemReplacement"] = new { productId = "plus.yearly" },
            };
            var next = new Dictionary<string, object?> { ["productId"] = scenario == "different-future-product" ? "unrelated.sku" : "plus.yearly" };
            if (scenario != "missing-offer") next["offerDetails"] = new { basePlanId = "yearly" };
            var json = JsonSerializer.Serialize(new
            {
                subscriptionState = "SUBSCRIPTION_STATE_ACTIVE", startTime = "2026-10-16T00:00:00Z", testPurchase = new { },
                acknowledgementState = "ACKNOWLEDGEMENT_STATE_PENDING", linkedPurchaseToken = "fictional-old-pro-token",
                externalAccountIdentifiers = new { obfuscatedExternalAccountId = new string('a', 64) }, lineItems = new[] { old, next },
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
