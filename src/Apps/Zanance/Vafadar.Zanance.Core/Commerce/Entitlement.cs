namespace Vafadar.Zanance.Core.Commerce;

/// <summary>The capability tier; a purchase duration is not another tier (D-117).</summary>
public enum ProductPlan
{
    /// <summary>Everyday local capabilities.</summary>
    Free,
    /// <summary>All local capabilities.</summary>
    Plus,
    /// <summary>Plus and separately authorized online services.</summary>
    Pro,
}

/// <summary>How a paid entitlement was acquired.</summary>
public enum PurchaseKind
{
    /// <summary>A verified subscription with an explicit validity end.</summary>
    Subscription,
    /// <summary>A non-consumable Plus base right without an invented expiry.</summary>
    Lifetime,
}

/// <summary>The billing cadence, independent of the capability tier.</summary>
public enum BillingPeriod
{
    /// <summary>No renewal for a Lifetime purchase.</summary>
    None,
    /// <summary>Monthly subscription.</summary>
    Month,
    /// <summary>Yearly subscription.</summary>
    Year,
}

/// <summary>Provenance supplied by a future purchase-verification adapter; not a verification mechanism.</summary>
public enum EntitlementSource
{
    /// <summary>Google Play verification.</summary>
    GooglePlay,
    /// <summary>Apple App Store verification.</summary>
    AppStore,
    /// <summary>Windows store verification, if that distribution channel is approved.</summary>
    WindowsStore,
}

/// <summary>
/// Immutable verified-input facts for the pure policy. Constructing this type does not verify a purchase.
/// Never hydrate paid facts from a financial backup. Store adapters, secure caching and clock trust are later slices.
/// </summary>
public sealed record Entitlement
{
    /// <summary>Creates a paid grant with a consistent product, cadence and half-open validity window.</summary>
    public Entitlement(ProductPlan plan, PurchaseKind kind, BillingPeriod period, EntitlementSource source,
        DateTimeOffset validFrom, DateTimeOffset? validUntil, string termsVersion, bool revoked = false)
    {
        if (!Enum.IsDefined(plan) || plan == ProductPlan.Free) throw new ArgumentOutOfRangeException(nameof(plan));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(period)) throw new ArgumentOutOfRangeException(nameof(period));
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(termsVersion);
        if (kind == PurchaseKind.Lifetime && (plan != ProductPlan.Plus || period != BillingPeriod.None || validUntil is not null))
            throw new ArgumentException("Lifetime grants only the non-expiring local Plus base right.");
        if (kind == PurchaseKind.Subscription && (period == BillingPeriod.None || validUntil is null || validUntil <= validFrom))
            throw new ArgumentException("A subscription requires a cadence and a validity end after its start.");
        Plan = plan;
        Kind = kind;
        Period = period;
        Source = source;
        ValidFrom = validFrom.ToUniversalTime();
        ValidUntil = validUntil?.ToUniversalTime();
        TermsVersion = termsVersion;
        Revoked = revoked;
    }

    /// <summary>Gets the capability tier.</summary>
    public ProductPlan Plan { get; }
    /// <summary>Gets the purchase kind.</summary>
    public PurchaseKind Kind { get; }
    /// <summary>Gets the renewal cadence.</summary>
    public BillingPeriod Period { get; }
    /// <summary>Gets the verified provenance.</summary>
    public EntitlementSource Source { get; }
    /// <summary>Gets the inclusive validity start in UTC.</summary>
    public DateTimeOffset ValidFrom { get; }
    /// <summary>Gets the exclusive validity end, absent only for Lifetime.</summary>
    public DateTimeOffset? ValidUntil { get; }
    /// <summary>Gets the recorded purchase terms version.</summary>
    public string TermsVersion { get; }
    /// <summary>Gets whether the verifier revoked this grant.</summary>
    public bool Revoked { get; }

    /// <summary>Evaluates supplied validity facts at an explicit instant, without inventing offline grace.</summary>
    public bool IsValidAt(DateTimeOffset instant) => !Revoked && instant >= ValidFrom && (ValidUntil is null || instant < ValidUntil);
}

/// <summary>Resolves overlapping verified rights without removing an enduring Plus Lifetime base.</summary>
public static class EntitlementResolver
{
    /// <summary>Returns the highest currently valid tier; empty, future, expired or revoked rights yield Free.</summary>
    public static ProductPlan Resolve(IEnumerable<Entitlement> grants, DateTimeOffset instant)
    {
        ArgumentNullException.ThrowIfNull(grants);
        var result = ProductPlan.Free;
        foreach (var grant in grants)
        {
            ArgumentNullException.ThrowIfNull(grant);
            if (grant.IsValidAt(instant) && grant.Plan > result) result = grant.Plan;
        }
        return result;
    }
}
