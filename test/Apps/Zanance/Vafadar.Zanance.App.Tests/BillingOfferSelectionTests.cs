using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual source-linked selection joins private native ownership with signed current purchase facts, never a second guessed subscription.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingOfferSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
    private const string Reference = "fictional-selection-owned-token";
    private static readonly string Id = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, Reference);
    private static readonly BillingProductMapping[] Catalog =
    [new(BillingProduct.PlusMonthly, "plus", "monthly", "sold-v1"), new(BillingProduct.PlusYearly, "plus", "yearly", "sold-v1"),
        new(BillingProduct.ProMonthly, "pro", "monthly", "sold-v1"), new(BillingProduct.PlusLifetime, "lifetime", null, "sold-v1")];

    /// <summary>The original paid Plus cadence can only become a reviewed same-SKU replacement; an account is not forced.</summary>
    [Fact]
    public void Existing_local_plus_uses_one_exact_replacement_and_keeps_lifetime_warning()
    {
        var result = Select(View(Grant()), null, [Owned()], [Offer(BillingProduct.PlusMonthly), Offer(BillingProduct.PlusYearly), Offer(BillingProduct.PlusLifetime)], false);
        Assert.Equal(ProductBillingStatus.Ready, result.Status); Assert.Equal(2, result.Offers.Count);
        var yearly = Assert.Single(result.Offers, offer => offer.Offer.Product == BillingProduct.PlusYearly);
        Assert.Equal(Id, yearly.Replacement!.PurchaseId); Assert.Equal(BillingReplacementTiming.KeepBillingDate, yearly.Replacement.Timing);
        Assert.True(Assert.Single(result.Offers, offer => offer.Offer.Product == BillingProduct.PlusLifetime).ExistingSubscription);
        Assert.DoesNotContain(Reference, yearly.ToString()); Assert.False(result.HasIdentity);
    }

    /// <summary>Pro purchase needs an explicitly associated original Plus, not just a signed-in identity or a native token.</summary>
    [Fact]
    public void Plus_to_pro_requires_original_identity_association_before_time_credit()
    {
        var local = View(Grant()); var offered = new[] { Offer(BillingProduct.ProMonthly) };
        var refused = Select(local, View(), [Owned()], offered, true);
        Assert.Empty(refused.Offers); Assert.True(refused.ProRestoreRequired);
        var admitted = Select(local, View(Grant() with { MatchingIdentity = true }), [Owned()], offered, true);
        Assert.Equal(BillingReplacementTiming.TimeCredit, Assert.Single(admitted.Offers).Replacement!.Timing);
        Assert.False(admitted.ProRestoreRequired);
    }

    /// <summary>Pending, unverified, hidden owned and already scheduled subscriptions never expose another charge.</summary>
    [Theory]
    [InlineData("pending", ProductBillingStatus.Pending)]
    [InlineData("unverified", ProductBillingStatus.RecoveryRequired)]
    [InlineData("different-account", ProductBillingStatus.ManageExisting)]
    [InlineData("scheduled", ProductBillingStatus.ManageExisting)]
    public void Unsafe_ownership_blocks_new_subscription(string scenario, ProductBillingStatus expected)
    {
        var grant = Grant();
        if (scenario == "scheduled") grant = grant with { Facts = grant.Facts with { AutoRenews = false,
            ScheduledChange = new(BillingProduct.PlusYearly, Now.AddDays(10), "sold-v1") } };
        var result = Select(scenario == "unverified" ? View() : View(grant), null,
            scenario == "different-account" ? [] : [Owned() with { Pending = scenario == "pending" }], [Offer(BillingProduct.PlusYearly)], false);
        Assert.Equal(expected, result.Status); Assert.Empty(result.Offers);
    }

    /// <summary>A verified subscription from another supported store cannot be charged in parallel here.</summary>
    [Fact]
    public void Other_store_identity_subscription_keeps_management_instead_of_parallel_buy()
    {
        var other = Grant() with { Issuer = BillingIssuer.AppStore, MatchingIdentity = true, Facts = Grant().Facts with { Product = BillingProduct.ProMonthly } };
        var result = Select(View(), View(other), [], [Offer(BillingProduct.ProMonthly)], true);
        Assert.Equal(ProductBillingStatus.ManageExisting, result.Status); Assert.Empty(result.Offers);
    }

    /// <summary>Cold deferred continuation can use its original old/new SKU, never an unrelated Lifetime receipt from the same owned-query result.</summary>
    [Theory]
    [InlineData("pro", true)][InlineData("plus", true)][InlineData("lifetime", false)][InlineData("unrelated", false)][InlineData(null, false)]
    public void Cold_replacement_routes_only_exact_original_subscription_skus(string? storeProduct, bool matches)
    {
        var current = Grant() with { Facts = Grant().Facts with { Product = BillingProduct.ProMonthly } };
        var view = View(current) with { NativePaymentId = Guid.NewGuid(), NativeProduct = BillingProduct.PlusYearly,
            HasUnresolvedNativePayment = true, NativeReplacementId = Id };
        var resolved = BillingOfferSelection.OriginalNativeProduct(view,
            new("fictional-cold-owned-reference", storeProduct == "pro" ? BillingProduct.ProMonthly : BillingProduct.PlusLifetime, false, storeProduct), Catalog);
        if (matches) Assert.Equal(BillingProduct.PlusYearly, resolved); else Assert.Null(resolved);
    }

    /// <summary>All six actual translations expose the original replacement timing/date and separate Lifetime warning before native Cancel, without writes.</summary>
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Replacement_terms_are_complete_before_cancel_and_changed_offer_requires_new_review(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var effect = new ReviewStore();
        var dates = new DateFormatter(f.Localization);
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, effect, dates);
        foreach (var timing in Enum.GetValues<BillingReplacementTiming>())
        {
            effect.State = new(ProductBillingStatus.Ready, null, null,
                [Offer(BillingProduct.PlusYearly) with { ExistingSubscription = true, Replacement = new(BillingProduct.PlusMonthly, Id, "plus", timing, Now.AddDays(10)) }],
                true, true, ProRestoreRequired: true, OfferChanged: true);
            await vm.LoadAsync(); var row = Assert.Single(vm.BillingOffers);
            Assert.Contains(dates.Format(Now.AddDays(10)), row.Terms);
            Assert.Contains(f.Translator.Format("Billing_Replacement_" + timing, "Plus", dates.Format(Now.AddDays(10))), row.Terms);
            Assert.Equal(f.Translator["Billing_OfferChanged"], vm.BillingStatusText);
            Assert.Equal(f.Translator["Billing_ProRestoreRequired"], vm.BillingIdentityText);
            Assert.DoesNotContain("Billing_", row.Terms);
            await vm.BuyBillingCommand.ExecuteAsync(row);
            Assert.Contains(row.Terms, f.Platform.ConfirmationDialogs.Last().Message);
        }
        effect.State = new(ProductBillingStatus.Ready, null, null, [Offer(BillingProduct.PlusLifetime) with { ExistingSubscription = true }], true, false);
        await vm.LoadAsync(); Assert.Contains(f.Translator["Billing_LifetimeKeepsSubscription"], Assert.Single(vm.BillingOffers).Terms);
        effect.State = new(ProductBillingStatus.ManageExisting, null, null, [], true, false);
        await vm.LoadAsync(); Assert.False(vm.CanBuyBilling); Assert.False(vm.CanContinueBilling); Assert.True(vm.CanManageBilling);
        Assert.Equal(f.Translator["Billing_Status_ManageExisting"], vm.BillingStatusText);
        Assert.Equal(0, effect.Purchases); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); vm.DeactivateBilling();
    }

    private sealed class ReviewStore : IProductBillingEnvironment
    {
        public bool Available => true;
        internal ProductBillingPresentation State = new(ProductBillingStatus.NotConfigured, null, null, [], false, false);
        internal int Purchases;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) => Task.FromResult(State);
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct) { Purchases++; throw new InvalidOperationException("Cancel must prevent payment."); }
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) => throw new InvalidOperationException("No restore in terms review.");
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => throw new InvalidOperationException("No pending work in terms review.");
        public Task ManageAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static ProductBillingPresentation Select(BillingClientView local, BillingClientView? identity, IReadOnlyList<NativeBillingPurchase> owned,
        IReadOnlyList<NativeBillingOffer> offers, bool hasIdentity) => BillingOfferSelection.Select(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
            local, identity, Catalog, owned, offers, "DE", hasIdentity);
    private static BillingCachedGrant Grant() => new(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
        new(BillingProduct.PlusMonthly, BillingPurchaseState.Active, Now.AddDays(-10), Now.AddDays(10), null, true, false, true, "sold-v1"),
        false, Now, Id);
    private static BillingClientView View(params BillingCachedGrant[] grants) => new(
        new(1, BillingSnapshotKind.Current, Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), BillingIssuer.GooglePlay,
            BillingEnvironment.Sandbox, null, Guid.NewGuid(), 0, Now, grants, false, Catalog), new(ProductPlan.Plus, false, false), false, null, false);
    private static NativeBillingPurchase Owned() => new(Reference, BillingProduct.PlusMonthly, false, "plus");
    private static NativeBillingOffer Offer(BillingProduct product)
    {
        var mapping = Catalog.Single(item => item.Product == product); var lifetime = product == BillingProduct.PlusLifetime;
        return new(product.ToString(), "Fictional selection price", "Fictional only",
            new(product.ToString(), product, BillingOfferKind.Standard, [new(10000000, "EUR", "10.00 EUR", lifetime ? "" :
                product == BillingProduct.PlusYearly ? "P1Y" : "P1M", lifetime ? 1 : 0, !lifetime)], true), product.ToString(), mapping.StoreProductId);
    }
}
