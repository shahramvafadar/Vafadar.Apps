namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Native boot/session identity and monotonic elapsed time; wall-clock time is deliberately absent.</summary>
public sealed record BillingMonotonicInstant(string BootIdentity, long ElapsedMilliseconds)
{
    /// <summary>Rejects absent native clock evidence rather than guessing from a device date.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(BootIdentity) && BootIdentity.Length <= 128 && ElapsedMilliseconds >= 0;
}

/// <summary>Server-authenticated time anchored to the same device's native monotonic clock.</summary>
public sealed record BillingClockAnchor(DateTimeOffset ServerTime, BillingMonotonicInstant Native)
{
    /// <summary>Gets elapsed trusted time only within the same native boot; a reboot requires renewed time evidence.</summary>
    public DateTimeOffset? Read(BillingMonotonicInstant now)
    {
        if (!Native.IsValid || !now.IsValid || Native.BootIdentity != now.BootIdentity || now.ElapsedMilliseconds < Native.ElapsedMilliseconds)
            return null;
        try { return ServerTime.AddMilliseconds(checked(now.ElapsedMilliseconds - Native.ElapsedMilliseconds)); }
        catch (ArgumentOutOfRangeException) { return null; }
        catch (OverflowException) { return null; }
    }
}

/// <summary>A provider fact inside an already verified device-bound service snapshot; never portable backup data.</summary>
public sealed record BillingCachedGrant(BillingIssuer Issuer, BillingEnvironment Environment, BillingPurchaseFacts Facts,
    bool MatchingIdentity, DateTimeOffset VerifiedAt, string? PurchaseId = null);

/// <summary>Offline result is distinct from provider revocation and never hides financial records.</summary>
public sealed record BillingCacheDecision(ProductPlan Plan, bool NeedsVerification, bool UsingOutageAllowance);

/// <summary>Pure evaluation after signature/device/revision validation; it cannot validate an untrusted receipt.</summary>
public static class BillingCachePolicy
{
    /// <summary>Bounded service-outage allowance, separate from the store's billing grace.</summary>
    public static readonly TimeSpan MaximumOutageAllowance = TimeSpan.FromDays(7);

    /// <summary>Resolves verified Lifetime bases and current subscriptions without consulting or extending a wall clock.</summary>
    public static BillingCacheDecision Evaluate(IReadOnlyList<BillingCachedGrant> grants, BillingIssuer distribution,
        BillingEnvironment environment, BillingClockAnchor anchor, BillingMonotonicInstant nativeNow, bool allowOutage)
    {
        if (grants.Count > 32 || !Enum.IsDefined(environment)) throw new ArgumentException("Bounded verified billing snapshot required.");
        var trusted = anchor.Read(nativeNow); var plan = ProductPlan.Free; var needsVerification = false; var outage = false;
        foreach (var grant in grants)
        {
            grant.Facts.Validate();
            if (grant.VerifiedAt > anchor.ServerTime || grant.Facts.Product == BillingProduct.PlusLifetime && grant.Facts.StartsAt > grant.VerifiedAt)
                throw new ArgumentException("Verified billing time differs from its authenticated anchor.");
            if (grant.Environment != environment ||
                !BillingProducts.AppliesTo(grant.Facts.Product, grant.Issuer, distribution, grant.MatchingIdentity)) continue;
            // A verified non-consumable does not expire when the service, device clock or monotonic anchor is unavailable.
            if (grant.Facts.Product == BillingProduct.PlusLifetime)
            {
                if (grant.Facts.State == BillingPurchaseState.Active) plan = Max(plan, ProductPlan.Plus);
                continue;
            }
            if (trusted is null) { needsVerification = true; continue; }
            if (grant.VerifiedAt > trusted || grant.Facts.StartsAt > trusted) { needsVerification = true; continue; }
            if (grant.Facts.HasAccessAt(trusted.Value)) { plan = Max(plan, BillingProducts.Plan(grant.Facts.Product)); continue; }
            if (grant.Facts.ScheduledChange is { } scheduled && trusted >= scheduled.EffectiveAt)
            { needsVerification = true; continue; } // Never prolong the old tier or invent the future tier after a known replacement date.
            // Pending, hold, pause, revoked and expired facts never acquire time from an outage flag.
            // A trial is not lengthened into a free paid period, nor is store grace stacked with another grace.
            if (allowOutage && grant.Facts.ScheduledChange is null && grant.Facts.TrialKnown && !grant.Facts.Trial && grant.Facts.State == BillingPurchaseState.Active &&
                grant.Facts.PaidUntil is { } paid && trusted < AddBounded(paid, MaximumOutageAllowance))
            {
                plan = Max(plan, BillingProducts.Plan(grant.Facts.Product)); outage = true; needsVerification = true;
            }
        }
        return new(plan, needsVerification, outage);
    }

    /// <summary>Compares only the existing ordered capability tiers.</summary>
    private static ProductPlan Max(ProductPlan left, ProductPlan right) => left > right ? left : right;
    /// <summary>Provider endpoints cannot overflow the finite expiry calculation into perpetual access.</summary>
    private static DateTimeOffset AddBounded(DateTimeOffset value, TimeSpan interval)
    {
        try { return value.Add(interval); }
        catch (ArgumentOutOfRangeException) { return value; }
    }
}
