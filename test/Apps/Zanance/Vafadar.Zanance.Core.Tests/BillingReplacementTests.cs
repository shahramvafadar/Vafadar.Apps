using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Current paid tiers, deferred items and native replacement timing stay separate from charges and future grants.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingReplacementTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddMonths(1);

    /// <summary>A known deferred downgrade keeps Pro only until the actual paid end and never invents Plus or extra outage Pro.</summary>
    [Fact]
    public void Deferred_change_never_grants_the_future_tier_or_extends_the_replaced_tier()
    {
        var facts = new BillingPurchaseFacts(BillingProduct.ProMonthly, BillingPurchaseState.Active, Start, End, null,
            false, false, true, "sold-pro-v1", ScheduledChange: new(BillingProduct.PlusYearly, End, "sold-plus-v2"));
        var grant = new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, facts, true, Start);
        var native = new BillingMonotonicInstant("original-boot", 1000); var anchor = new BillingClockAnchor(Start, native);
        Assert.Equal(ProductPlan.Pro, BillingCachePolicy.Evaluate([grant], BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
            anchor, native with { ElapsedMilliseconds = 1000 + (long)(End - Start).TotalMilliseconds - 1 }, false).Plan);
        var after = BillingCachePolicy.Evaluate([grant], BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, anchor,
            native with { ElapsedMilliseconds = 1000 + (long)(End - Start).TotalMilliseconds + 1 }, true);
        Assert.Equal(ProductPlan.Free, after.Plan); Assert.True(after.NeedsVerification); Assert.False(after.UsingOutageAllowance);
        Assert.Null(facts.ToEntitlement(BillingIssuer.GooglePlay, End));
    }

    /// <summary>Only explicit supported transitions select a provider timing, without calculating a monetary credit.</summary>
    [Theory]
    [InlineData(BillingProduct.PlusMonthly, BillingProduct.PlusYearly, true, BillingReplacementTiming.KeepBillingDate)]
    [InlineData(BillingProduct.PlusMonthly, BillingProduct.ProMonthly, false, BillingReplacementTiming.TimeCredit)]
    [InlineData(BillingProduct.ProYearly, BillingProduct.PlusMonthly, false, BillingReplacementTiming.Deferred)]
    [InlineData(BillingProduct.PlusMonthly, BillingProduct.PlusYearly, false, BillingReplacementTiming.Deferred)]
    public void Verified_existing_subscription_selects_only_its_supported_native_timing(BillingProduct from, BillingProduct target,
        bool sameStoreProduct, BillingReplacementTiming expected)
    {
        var facts = new BillingPurchaseFacts(from, BillingPurchaseState.Active, Start, End, null, true, false, true, "original-v1");
        var id = BillingAccountBinding.PurchaseId(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, "fictional-original-subscription");
        var grant = new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, facts, true, Start, id);
        var change = BillingReplacementPolicy.Create(grant, target, "original.sku", sameStoreProduct ? "original.sku" : "new.sku", Start.AddDays(1));
        Assert.NotNull(change); Assert.Equal(expected, change.Timing); Assert.Equal(id, change.PurchaseId); Assert.Equal(End, change.CurrentPaidUntil);
        Assert.Null(BillingReplacementPolicy.Create(grant, from, "original.sku", "original.sku", Start.AddDays(1)));
        Assert.Null(BillingReplacementPolicy.Create(grant, BillingProduct.PlusLifetime, "original.sku", "lifetime.sku", Start.AddDays(1)));
    }

    /// <summary>A pending store replacement cannot become another guessed change.</summary>
    [Fact]
    public void Already_scheduled_or_unverified_source_requires_management_instead_of_another_replacement()
    {
        var facts = new BillingPurchaseFacts(BillingProduct.ProMonthly, BillingPurchaseState.Active, Start, End, null,
            false, false, true, "original", ScheduledChange: new(BillingProduct.PlusMonthly, End, "next"));
        var grant = new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, facts, true, Start, new string('a', 64));
        Assert.Null(BillingReplacementPolicy.Create(grant, BillingProduct.ProYearly, "old.sku", "next.sku", Start.AddDays(1)));
        Assert.Null(BillingReplacementPolicy.Create(grant with { PurchaseId = null }, BillingProduct.PlusMonthly, "old.sku", "next.sku", Start.AddDays(1)));
    }
}
