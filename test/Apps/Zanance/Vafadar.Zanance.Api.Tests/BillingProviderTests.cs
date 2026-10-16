using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Official-provider wire boundaries, sold terms and explicit legacy restoration without real credentials or purchases.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Turning renewal off retains the actual paid interval; actual store phases and issuer facts determine the result.</summary>
    [Fact]
    public async Task Canceled_subscription_keeps_paid_end_without_renewal()
    {
        var row = Subscription(); row["subscriptionState"] = "SUBSCRIPTION_STATE_CANCELED";
        row["lineItems"]![0]!["autoRenewingPlan"]!["autoRenewEnabled"] = false;
        using var wire = new Wire(row.ToJsonString()); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        var result = await provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken);
        Assert.Equal(BillingPurchaseState.Active, result.Facts.State); Assert.False(result.Facts.AutoRenews);
        Assert.True(result.Facts.HasAccessAt(Now)); Assert.Equal(Now.AddDays(20), result.Facts.PaidUntil);
        Assert.Equal("androidpublisher.googleapis.com", wire.LastUri!.Host);
        Assert.StartsWith("/androidpublisher/v3/applications/pro.vafadar.zanance/purchases/subscriptionsv2/", wire.LastUri.AbsolutePath);
        Assert.Equal(HttpMethod.Get, wire.LastMethod);
    }

    /// <summary>A sandbox response cannot be promoted by a client-selected production environment.</summary>
    [Fact]
    public async Task Sandbox_cannot_become_production()
    {
        using var wire = new Wire(Subscription().ToJsonString()); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        var error = await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Production, TestContext.Current.CancellationToken));
        Assert.Equal("StoreEnvironmentMismatch", error.Message);
    }

    /// <summary>Unknown paid states/phases, extra current items and mismatched provider products refuse a grant.</summary>
    [Theory]
    [InlineData("state")]
    [InlineData("phase")]
    [InlineData("duplicate-current-item")]
    [InlineData("product")]
    public async Task Ambiguous_provider_authority_is_refused(string corruption)
    {
        var row = Subscription();
        if (corruption == "state") row["subscriptionState"] = "SUBSCRIPTION_STATE_UNKNOWN_FUTURE";
        if (corruption == "phase") row["lineItems"]![0]!["offerPhase"] = JsonNode.Parse("""{"unknownFuturePhase":{}}""");
        if (corruption == "duplicate-current-item") ((JsonArray)row["lineItems"]!).Add(row["lineItems"]![0]!.DeepClone());
        if (corruption == "product") row["lineItems"]![0]!["productId"] = "other_application_product";
        using var wire = new Wire(row.ToJsonString()); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync("fictional-only-reference", BillingProduct.ProYearly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken));
    }

    /// <summary>A provider outage or redirected endpoint does not turn into a refund or a success response.</summary>
    [Theory]
    [InlineData(503)]
    [InlineData(302)]
    [InlineData(401)]
    public async Task Provider_failures_are_content_free_and_not_paid_facts(int status)
    {
        using var wire = new Wire("private-provider-body-must-not-escape", (HttpStatusCode)status);
        using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        var error = await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken));
        Assert.Equal(503, error.Status); Assert.DoesNotContain("private-provider-body", error.ToString());
        Assert.Equal(1, wire.Calls);
    }

    /// <summary>The strict HTTP parser refuses duplicated lifecycle properties rather than accepting a later value.</summary>
    [Fact]
    public async Task Duplicate_provider_fields_are_refused()
    {
        var json = Subscription().ToJsonString().Replace("\"testPurchase\":{}", "\"testPurchase\":{},\"testPurchase\":null", StringComparison.Ordinal);
        using var wire = new Wire(json); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        await Assert.ThrowsAsync<BillingRefusal>(() => provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken));
    }

    /// <summary>A historical SKU restores under its explicit original terms but is never offered for a new payment.</summary>
    [Fact]
    public async Task Legacy_purchase_acknowledges_its_actual_sku()
    {
        var row = Subscription(); row["lineItems"]![0]!["productId"] = "fictional.plus.legacy";
        using var wire = new Wire(row.ToJsonString()); using var http = new BillingProviderHttp(wire);
        var catalog = Catalog(); var provider = new GooglePlayPurchaseProvider(http, catalog, new Credentials(), new Clock());
        var result = await provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken);
        Assert.Equal("original-v1", result.Facts.TermsVersion);
        Assert.False(catalog.Read(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox).Single(p => p.StoreProductId == result.StoreProductId).AvailableForPurchase);
        wire.Response = "";
        await provider.AcknowledgeAsync(result, TestContext.Current.CancellationToken);
        Assert.Equal(HttpMethod.Post, wire.LastMethod);
        Assert.Contains("/subscriptions/fictional.plus.legacy/tokens/", wire.LastUri!.AbsolutePath);
    }

    /// <summary>Changing current catalog wording does not replace accepted Lifetime capabilities.</summary>
    [Fact]
    public void Same_product_preserves_accepted_terms_and_refuses_lifetime_substitution()
    {
        var old = new BillingPurchaseFacts(BillingProduct.PlusLifetime, BillingPurchaseState.Active, Now.AddDays(-10), null, null, false, false, true, "sold-v1");
        var purchase = new BillingPurchaseRow { Verified = true, Product = old.Product,
            FactsJson = JsonSerializer.Serialize(old, BillingJson.Default.BillingPurchaseFacts) };
        Assert.Equal("sold-v1", BillingAcceptedTerms.Preserve(purchase, old with { TermsVersion = "later-v9" }).TermsVersion);
        var subscription = new BillingPurchaseFacts(BillingProduct.PlusMonthly, BillingPurchaseState.Active, Now.AddDays(-1), Now.AddDays(20), null, true, false, true, "later-v9");
        Assert.Throws<BillingRefusal>(() => BillingAcceptedTerms.Preserve(purchase, subscription));
    }

    /// <summary>Fresh review is due by the paid end; terminal states remain reconcilable without granting outage access.</summary>
    [Fact]
    public void Reconciliation_does_not_sleep_past_paid_end()
    {
        var facts = new BillingPurchaseFacts(BillingProduct.PlusMonthly, BillingPurchaseState.Active, Now.AddDays(-1), Now.AddMinutes(10), null, true, false, true, "v1");
        Assert.Equal(Now.AddMinutes(10), BillingReconciliation.NextCheck(Now, facts));
        Assert.Equal(Now.AddDays(1), BillingReconciliation.NextCheck(Now, facts with { State = BillingPurchaseState.Revoked }));
    }

    /// <summary>Omitted protobuf false and optional trial phase do not imply renewal or an earned outage extension.</summary>
    [Fact]
    public async Task Optional_provider_fields_do_not_invent_trial_knowledge_or_renewal()
    {
        var row = Subscription(); ((JsonObject)row["lineItems"]![0]!["autoRenewingPlan"]!).Remove("autoRenewEnabled");
        ((JsonObject)row["lineItems"]![0]!).Remove("offerPhase");
        using var wire = new Wire(row.ToJsonString()); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        var purchase = await provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken);
        Assert.False(purchase.Facts.AutoRenews); Assert.False(purchase.Facts.TrialKnown);
        var later = purchase.Facts.PaidUntil!.Value.AddDays(1);
        var cache = BillingCachePolicy.Evaluate([new(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, purchase.Facts, false, Now)],
            BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, new(Now, new("fictional-boot", 0)),
            new("fictional-boot", checked((long)(later - Now).TotalMilliseconds)), true);
        Assert.Equal(ProductPlan.Free, cache.Plan); Assert.False(cache.UsingOutageAllowance);
    }

    /// <summary>Cancellation before a first payment is a terminal non-grant, not indefinitely pending native work.</summary>
    [Fact]
    public async Task Canceled_pending_subscription_without_paid_dates_is_terminal()
    {
        var row = Subscription(); row["subscriptionState"] = "SUBSCRIPTION_STATE_PENDING_PURCHASE_CANCELED";
        row.Remove("startTime"); var line = (JsonObject)row["lineItems"]![0]!;
        foreach (var key in new[] { "expiryTime", "latestSuccessfulOrderId", "offerPhase", "autoRenewingPlan" }) line.Remove(key);
        using var wire = new Wire(row.ToJsonString()); using var http = new BillingProviderHttp(wire);
        var provider = new GooglePlayPurchaseProvider(http, Catalog(), new Credentials(), new Clock());
        var purchase = await provider.VerifyAsync("fictional-only-reference", BillingProduct.PlusMonthly, BillingEnvironment.Sandbox, TestContext.Current.CancellationToken);
        Assert.Equal(BillingPurchaseState.Revoked, purchase.Facts.State); Assert.False(purchase.Facts.HasAccessAt(Now));
        Assert.Null(purchase.Facts.PaidUntil); Assert.False(purchase.Facts.AutoRenews);
    }

    private static JsonObject Subscription() => (JsonObject)JsonNode.Parse("""
        {"testPurchase":{},"subscriptionState":"SUBSCRIPTION_STATE_ACTIVE","startTime":"2026-10-01T12:00:00Z",
         "acknowledgementState":"ACKNOWLEDGEMENT_STATE_PENDING","externalAccountIdentifiers":{"obfuscatedExternalAccountId":"fictional-binding"},
         "lineItems":[{"productId":"fictional.plus","latestSuccessfulOrderId":"fictional-not-an-identity",
          "expiryTime":"2026-11-05T12:00:00Z","offerDetails":{"basePlanId":"monthly"},
          "autoRenewingPlan":{"autoRenewEnabled":true},"offerPhase":{"basePrice":{}}}]}
        """)!;

    private static BillingCatalog Catalog() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:ProductId"] = "fictional.plus",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:BasePlanId"] = "monthly",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:TermsVersion"] = "current-v2",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:Legacy:0:ProductId"] = "fictional.plus.legacy",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:Legacy:0:BasePlanId"] = "monthly",
        ["Billing:Catalog:GooglePlay:Sandbox:PlusMonthly:Legacy:0:TermsVersion"] = "original-v1",
    }).Build());

    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Credentials : IGoogleBillingCredentials
    { public Task<string> AccessAsync(CancellationToken ct) => Task.FromResult("fictional-only-not-a-credential"); }
    private sealed class Wire(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal string Response { get; set; } = response;
        internal Uri? LastUri { get; private set; }
        internal HttpMethod? LastMethod { get; private set; }
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++; LastUri = request.RequestUri; LastMethod = request.Method;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(Response, Encoding.UTF8, "application/json") });
        }
    }
}
