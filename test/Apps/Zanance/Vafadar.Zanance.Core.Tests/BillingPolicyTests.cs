using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Actual billing policy boundaries; provider/native verification is qualified separately.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly BillingMonotonicInstant Native = new("fictitious-boot-1", 1000);

    [Fact]
    public void Anonymous_and_identity_Google_account_bindings_cannot_alias_the_same_public_UUID()
    {
        var token = Guid.Parse("dbf41b08-e532-42a3-9c29-532ba23bd612");
        Assert.NotEqual(BillingAccountBinding.Google(token, false), BillingAccountBinding.Google(token, true));
        Assert.Equal(64, BillingAccountBinding.Google(token, false).Length);
    }

    [Theory]
    [InlineData(BillingPurchaseState.Pending)]
    [InlineData(BillingPurchaseState.Hold)]
    [InlineData(BillingPurchaseState.Paused)]
    [InlineData(BillingPurchaseState.Expired)]
    [InlineData(BillingPurchaseState.Revoked)]
    public void Non_access_states_never_gain_service_outage_time(BillingPurchaseState state)
    {
        var facts = Subscription(state, Now.AddDays(1));
        Assert.False(facts.HasAccessAt(Now));
        var result = Evaluate([Cached(facts)], new(Native.BootIdentity, 2000), true);
        Assert.Equal(ProductPlan.Free, result.Plan); Assert.False(result.UsingOutageAllowance);
    }

    [Fact]
    public void Disabling_renewal_preserves_the_exact_paid_period_but_not_its_exclusive_end()
    {
        var facts = Subscription(BillingPurchaseState.Active, Now.AddDays(1)) with { AutoRenews = false };
        Assert.True(facts.HasAccessAt(Now)); Assert.False(facts.HasAccessAt(Now.AddDays(1)));
        Assert.Equal(ProductPlan.Pro, facts.ToEntitlement(BillingIssuer.GooglePlay, Now)!.Plan);
        Assert.Null(facts.ToEntitlement(BillingIssuer.GooglePlay, Now.AddDays(1)));
    }

    [Fact]
    public void Google_effective_grace_end_does_not_invent_an_earlier_paid_end_or_add_outage_grace()
    {
        var facts = Subscription(BillingPurchaseState.Grace, null) with { GraceUntil = Now.AddDays(3) };
        Assert.True(facts.HasAccessAt(Now)); Assert.False(facts.HasAccessAt(Now.AddDays(3)));
        var result = Evaluate([Cached(facts)], new(Native.BootIdentity, 1000 + (long)TimeSpan.FromDays(4).TotalMilliseconds), true);
        Assert.Equal(ProductPlan.Free, result.Plan); Assert.False(result.UsingOutageAllowance);
    }

    [Fact]
    public void Verified_lifetime_base_survives_service_outage_and_unknown_boot_without_extending_Pro()
    {
        var grants = new[] { Cached(Lifetime()), Cached(Subscription(BillingPurchaseState.Active, Now.AddDays(1))) };
        var result = Evaluate(grants, new("fictitious-new-boot", 50000), true);
        Assert.Equal(ProductPlan.Plus, result.Plan); Assert.True(result.NeedsVerification);
        Assert.False(result.UsingOutageAllowance);
    }

    [Fact]
    public void Normal_subscription_outage_is_bounded_to_seven_days_after_the_verified_end()
    {
        var facts = Subscription(BillingPurchaseState.Active, Now.AddDays(1));
        var before = Evaluate([Cached(facts)], new(Native.BootIdentity, 1000 + (long)TimeSpan.FromDays(7).TotalMilliseconds), true);
        var atEnd = Evaluate([Cached(facts)], new(Native.BootIdentity, 1000 + (long)TimeSpan.FromDays(8).TotalMilliseconds), true);
        Assert.Equal(ProductPlan.Pro, before.Plan); Assert.True(before.UsingOutageAllowance);
        Assert.Equal(ProductPlan.Free, atEnd.Plan); Assert.False(atEnd.UsingOutageAllowance);
        Assert.Equal(ProductPlan.Free, Evaluate([Cached(facts)], new(Native.BootIdentity,
            1000 + (long)TimeSpan.FromDays(7).TotalMilliseconds), false).Plan);
    }

    [Fact]
    public void Trial_never_becomes_a_free_paid_period_during_an_outage()
    {
        var trial = Subscription(BillingPurchaseState.Active, Now.AddDays(14)) with { Trial = true };
        var result = Evaluate([Cached(trial)], new(Native.BootIdentity, 1000 + (long)TimeSpan.FromDays(14).TotalMilliseconds), true);
        Assert.Equal(ProductPlan.Free, result.Plan); Assert.False(result.UsingOutageAllowance);
    }

    [Fact]
    public void Monotonic_regression_never_creates_trusted_subscription_time()
    {
        Assert.Null(new BillingClockAnchor(Now, Native).Read(new(Native.BootIdentity, 999)));
        var result = Evaluate([Cached(Subscription(BillingPurchaseState.Active, Now.AddDays(1)))], new(Native.BootIdentity, 999), true);
        Assert.Equal(ProductPlan.Free, result.Plan); Assert.True(result.NeedsVerification);
    }

    [Fact]
    public void Store_local_Plus_and_identity_Pro_have_independent_distribution_scope()
    {
        Assert.False(BillingProducts.RequiresIdentity(BillingProduct.PlusLifetime, BillingIssuer.GooglePlay));
        Assert.False(BillingProducts.AppliesTo(BillingProduct.PlusLifetime, BillingIssuer.GooglePlay, BillingIssuer.AppStore, true));
        Assert.True(BillingProducts.AppliesTo(BillingProduct.ProYearly, BillingIssuer.GooglePlay, BillingIssuer.AppStore, true));
        Assert.False(BillingProducts.AppliesTo(BillingProduct.ProYearly, BillingIssuer.GooglePlay, BillingIssuer.AppStore, false));
        Assert.True(BillingProducts.AppliesTo(BillingProduct.PlusLifetime, BillingIssuer.Web, BillingIssuer.AppStore, true));
        Assert.False(BillingProducts.AppliesTo(BillingProduct.PlusLifetime, BillingIssuer.Web, BillingIssuer.AppStore, false));
    }

    [Fact]
    public void Sandbox_purchase_facts_never_enter_production_cache()
    {
        var result = BillingCachePolicy.Evaluate([Cached(Lifetime())], BillingIssuer.GooglePlay, BillingEnvironment.Production,
            new(Now, Native), new(Native.BootIdentity, 1001), true);
        Assert.Equal(ProductPlan.Free, result.Plan);
    }

    [Fact]
    public void Lifetime_is_not_a_subscription_or_a_future_dated_grant()
    {
        Assert.Throws<ArgumentException>(() => (Lifetime() with { PaidUntil = Now.AddDays(1) }).Validate());
        Assert.Throws<ArgumentException>(() => (Lifetime() with { AutoRenews = true }).Validate());
        Assert.Throws<ArgumentException>(() => Evaluate([Cached(Lifetime() with { StartsAt = Now.AddDays(1) })], Native, true));
    }

    [Fact]
    public void Lifetime_discount_requires_verified_ownership_one_annual_period_and_actual_twenty_five_percent_price()
    {
        var offer = AnnualOffer();
        Assert.True(BillingOffers.IsEligible(offer, true, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer, false, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer, true, false, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer with { Phases = [offer.Phases[0] with { PriceMicros = 39_590_000 }, offer.Phases[1]] }, true, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer with { Phases = [offer.Phases[0] with { Cycles = 2 }, offer.Phases[1]] }, true, true, "DE", Now, 2));
    }

    [Fact]
    public void Native_trial_eligibility_and_fourteen_day_terms_remain_separate_from_Lifetime_discount()
    {
        var offer = AnnualOffer() with { Kind = BillingOfferKind.ProTrial,
            Phases = [new(0, "EUR", "Fictitious free trial", "P14D", 1, false), AnnualOffer().Phases[1]] };
        Assert.True(BillingOffers.IsEligible(offer, true, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer with { StoreEligible = false }, true, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer with { Phases = [offer.Phases[0] with { Period = "P1M" }, offer.Phases[1]] }, true, true, "DE", Now, 2));
        Assert.False(BillingOffers.IsEligible(offer with { Phases = [offer.Phases[0], AnnualOffer().Phases[0], offer.Phases[1]] }, true, true, "DE", Now, 2));
    }

    /// <summary>Explicit fictitious provider facts, not a stub of the policy under test.</summary>
    private static BillingPurchaseFacts Subscription(BillingPurchaseState state, DateTimeOffset? until)
        => new(BillingProduct.ProYearly, state, Now.AddDays(-1), until, null, true, false, true, "fictitious-terms-v1");
    /// <summary>Explicit non-consumable facts without an invented expiry.</summary>
    private static BillingPurchaseFacts Lifetime() => new(BillingProduct.PlusLifetime, BillingPurchaseState.Active,
        Now.AddDays(-30), null, null, false, false, true, "fictitious-terms-v1");
    /// <summary>Already verified-input test packet; these unit cases do not exercise cryptographic verification.</summary>
    private static BillingCachedGrant Cached(BillingPurchaseFacts facts)
        => new(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, facts, true, Now);
    /// <summary>Calls the original policy with explicit trusted and native clocks, without a device wall clock.</summary>
    private static BillingCacheDecision Evaluate(IReadOnlyList<BillingCachedGrant> facts, BillingMonotonicInstant native, bool outage)
        => BillingCachePolicy.Evaluate(facts, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, new(Now, Native), native, outage);
    /// <summary>Original-price/discount phases are explicit fictitious store values, never a production charge.</summary>
    private static BillingStoreOffer AnnualOffer() => new("fictitious-lifetime-offer", BillingProduct.ProYearly,
        BillingOfferKind.LifetimeAnnual, [new(29_990_000, "EUR", "Fictitious first year", "P1Y", 1, false),
            new(39_990_000, "EUR", "Fictitious renewal", "P1Y", 0, true)], true);
}
