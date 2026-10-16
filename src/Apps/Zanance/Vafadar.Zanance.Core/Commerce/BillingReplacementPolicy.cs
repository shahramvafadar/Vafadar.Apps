namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Native store timing; the provider alone determines amounts, credits and the final payable price.</summary>
public enum BillingReplacementTiming
{
    /// <summary>Same-product cadence change keeps the next billing date, without an application-computed charge.</summary>
    KeepBillingDate,
    /// <summary>A higher tier starts now with provider-calculated time credit.</summary>
    TimeCredit,
    /// <summary>A cross-product lower tier or cadence starts only after the already paid period.</summary>
    Deferred,
}

/// <summary>Public reviewed replacement context; it contains no raw store token or provider credential.</summary>
public sealed record BillingReplacement(BillingProduct FromProduct, string PurchaseId, string StoreProductId,
    BillingReplacementTiming Timing, DateTimeOffset CurrentPaidUntil);

/// <summary>Plans only an explicit single existing store subscription, never a second parallel subscription or guessed credit.</summary>
public static class BillingReplacementPolicy
{
    /// <summary>Returns a supported reviewed replacement, or refuses an ambiguous, inactive, already scheduled or non-subscription source.</summary>
    public static BillingReplacement? Create(BillingCachedGrant current, BillingProduct target, string oldStoreProduct,
        string newStoreProduct, DateTimeOffset trustedNow)
    {
        current.Facts.Validate();
        if (current.Issuer != BillingIssuer.GooglePlay || current.PurchaseId is not { Length: 64 } id || !id.All(char.IsAsciiHexDigit) ||
            current.Facts.State != BillingPurchaseState.Active || !current.Facts.HasAccessAt(trustedNow) || current.Facts.ScheduledChange is not null ||
            current.Facts.PaidUntil is not { } end || BillingProducts.Period(current.Facts.Product) == BillingPeriod.None ||
            BillingProducts.Period(target) == BillingPeriod.None || current.Facts.Product == target ||
            string.IsNullOrEmpty(oldStoreProduct) || string.IsNullOrEmpty(newStoreProduct)) return null;
        var oldPlan = BillingProducts.Plan(current.Facts.Product); var nextPlan = BillingProducts.Plan(target);
        if (oldStoreProduct == newStoreProduct && oldPlan != nextPlan) return null; // A trusted store SKU cannot change its capability family.
        var timing = oldStoreProduct == newStoreProduct ? BillingReplacementTiming.KeepBillingDate
            : nextPlan > oldPlan ? BillingReplacementTiming.TimeCredit : BillingReplacementTiming.Deferred;
        return new(current.Facts.Product, id, oldStoreProduct, timing, end);
    }
}
