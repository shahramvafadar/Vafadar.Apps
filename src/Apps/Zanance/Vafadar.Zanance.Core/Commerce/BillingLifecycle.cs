namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Provider-confirmed future subscription item; it never grants its target capabilities before fresh effective verification.</summary>
public sealed record BillingScheduledChange(BillingProduct Product, DateTimeOffset EffectiveAt, string TermsVersion);

/// <summary>Provider-verified payment lifecycle, distinct from notification type and app operation status.</summary>
public enum BillingPurchaseState
{
    /// <summary>No grant while payment is pending.</summary>
    Pending,
    /// <summary>Verified current payment/non-consumable.</summary>
    Active,
    /// <summary>Provider-confirmed grace with an explicit end.</summary>
    Grace,
    /// <summary>Payment retry without access.</summary>
    Hold,
    /// <summary>Provider pause without access.</summary>
    Paused,
    /// <summary>Paid validity ended.</summary>
    Expired,
    /// <summary>Explicit provider revocation/refund withdrawal.</summary>
    Revoked,
}

/// <summary>One verifier's current facts; constructing them is not store verification or purchase ownership.</summary>
public sealed record BillingPurchaseFacts(BillingProduct Product, BillingPurchaseState State,
    DateTimeOffset StartsAt, DateTimeOffset? PaidUntil, DateTimeOffset? GraceUntil, bool AutoRenews,
    bool Trial, bool Acknowledged, string TermsVersion, bool TrialKnown = true, BillingScheduledChange? ScheduledChange = null)
{
    /// <summary>Rejects contradictory, incomplete or unsupported provider observations before persistence.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(State) || string.IsNullOrWhiteSpace(TermsVersion) || TermsVersion.Length > 64)
            throw new ArgumentException("Invalid purchase facts.");
        var period = BillingProducts.Period(Product);
        if (ScheduledChange is { } change && (period == BillingPeriod.None || change.Product == Product ||
            BillingProducts.Period(change.Product) == BillingPeriod.None || State != BillingPurchaseState.Active ||
            AutoRenews || PaidUntil != change.EffectiveAt || change.EffectiveAt <= StartsAt ||
            string.IsNullOrWhiteSpace(change.TermsVersion) || change.TermsVersion.Length > 64))
            throw new ArgumentException("A scheduled store change is not a current future-product grant.");
        if (period == BillingPeriod.None)
        {
            if (PaidUntil is not null || GraceUntil is not null || AutoRenews || Trial ||
                State is BillingPurchaseState.Grace or BillingPurchaseState.Hold or BillingPurchaseState.Paused or BillingPurchaseState.Expired)
                throw new ArgumentException("A non-consumable has no subscription lifecycle.");
        }
        else if (PaidUntil is null && State is not (BillingPurchaseState.Pending or BillingPurchaseState.Grace or BillingPurchaseState.Revoked) || PaidUntil is { } end && end <= StartsAt)
            throw new ArgumentException("A paid subscription requires a consistent explicit period.");
        // Google reports an effective grace expiry without a distinct paid-period end; never invent that earlier date.
        if (State == BillingPurchaseState.Grace && (GraceUntil is null || GraceUntil <= StartsAt || PaidUntil is not null && GraceUntil < PaidUntil) ||
            State != BillingPurchaseState.Grace && GraceUntil is not null)
            throw new ArgumentException("Store grace requires its own verified end.");
    }

    /// <summary>Actual store validity only; turning auto-renew off never discards the already paid period.</summary>
    public bool HasAccessAt(DateTimeOffset trustedNow)
    {
        Validate();
        if (trustedNow < StartsAt) return false;
        return State switch
        {
            BillingPurchaseState.Active => PaidUntil is null || trustedNow < PaidUntil,
            BillingPurchaseState.Grace => trustedNow < GraceUntil,
            _ => false,
        };
    }

    /// <summary>Converts only an accepted current verifier result to the existing pure local capability policy.</summary>
    public Entitlement? ToEntitlement(BillingIssuer issuer, DateTimeOffset trustedNow)
    {
        if (!HasAccessAt(trustedNow)) return null;
        var source = issuer switch
        {
            BillingIssuer.GooglePlay => EntitlementSource.GooglePlay,
            BillingIssuer.AppStore => EntitlementSource.AppStore,
            BillingIssuer.Web => EntitlementSource.Web,
            _ => throw new ArgumentOutOfRangeException(nameof(issuer)),
        };
        return new(BillingProducts.Plan(Product), Product == BillingProduct.PlusLifetime ? PurchaseKind.Lifetime : PurchaseKind.Subscription,
            BillingProducts.Period(Product), source, StartsAt, State == BillingPurchaseState.Grace ? GraceUntil : PaidUntil, TermsVersion);
    }
}
