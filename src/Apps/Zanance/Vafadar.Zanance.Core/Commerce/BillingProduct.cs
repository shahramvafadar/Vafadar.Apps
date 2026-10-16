namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Actual purchase issuer, independent of the store that distributed this application.</summary>
public enum BillingIssuer
{
    /// <summary>Google Play's verified purchase.</summary>
    GooglePlay,
    /// <summary>Apple's verified purchase.</summary>
    AppStore,
    /// <summary>Reserved identity-bound verified web issuer; no checkout or issuer is activated.</summary>
    Web,
}

/// <summary>Separate provider data and verification authority; sandbox never creates a production grant.</summary>
public enum BillingEnvironment
{
    /// <summary>Explicit provider test environment.</summary>
    Sandbox,
    /// <summary>Actual provider production environment.</summary>
    Production,
}

/// <summary>Approved logical products; external store identifiers are separately configured and verified.</summary>
public enum BillingProduct
{
    /// <summary>Local Plus, monthly subscription.</summary>
    PlusMonthly,
    /// <summary>Local Plus, yearly subscription.</summary>
    PlusYearly,
    /// <summary>Local Plus, non-consumable without an invented expiry.</summary>
    PlusLifetime,
    /// <summary>Identity-bound Pro, monthly subscription.</summary>
    ProMonthly,
    /// <summary>Identity-bound Pro, yearly subscription.</summary>
    ProYearly,
}

/// <summary>Pure catalog invariants, never evidence that a store product exists or a payment occurred.</summary>
public static class BillingProducts
{
    /// <summary>Gets the capability tier for a supported logical product.</summary>
    public static ProductPlan Plan(BillingProduct product) => product switch
    {
        BillingProduct.PlusMonthly or BillingProduct.PlusYearly or BillingProduct.PlusLifetime => ProductPlan.Plus,
        BillingProduct.ProMonthly or BillingProduct.ProYearly => ProductPlan.Pro,
        _ => throw new ArgumentOutOfRangeException(nameof(product)),
    };

    /// <summary>Gets the actual product cadence; Lifetime has no renewal.</summary>
    public static BillingPeriod Period(BillingProduct product) => product switch
    {
        BillingProduct.PlusMonthly or BillingProduct.ProMonthly => BillingPeriod.Month,
        BillingProduct.PlusYearly or BillingProduct.ProYearly => BillingPeriod.Year,
        BillingProduct.PlusLifetime => BillingPeriod.None,
        _ => throw new ArgumentOutOfRangeException(nameof(product)),
    };

    /// <summary>Online Pro and future web purchases require identity; store-local Plus does not.</summary>
    public static bool RequiresIdentity(BillingProduct product, BillingIssuer issuer)
    {
        if (!Enum.IsDefined(issuer)) throw new ArgumentOutOfRangeException(nameof(issuer));
        return Plan(product) == ProductPlan.Pro || issuer == BillingIssuer.Web;
    }

    /// <summary>Store Plus is distribution-scoped; Pro and future web grants follow identity across supported stores.</summary>
    public static bool AppliesTo(BillingProduct product, BillingIssuer issuer, BillingIssuer distribution,
        bool matchingIdentity)
    {
        if (!Enum.IsDefined(issuer) || distribution is not (BillingIssuer.GooglePlay or BillingIssuer.AppStore)) return false;
        return RequiresIdentity(product, issuer) ? matchingIdentity : issuer == distribution;
    }
}
