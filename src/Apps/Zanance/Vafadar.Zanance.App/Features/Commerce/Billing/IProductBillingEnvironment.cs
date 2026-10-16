using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit store-screen outcome, separate from the original comparison and actual app quota registration.</summary>
public enum ProductBillingStatus
{
    /// <summary>No native payment adapter in this selected edition.</summary>
    Unavailable,
    /// <summary>Current service and actual store choices are available.</summary>
    Ready,
    /// <summary>No actual configured products; no static prices or fabricated checkout.</summary>
    NotConfigured,
    /// <summary>Current verification unavailable; any displayed cached purchase is labeled separately.</summary>
    VerificationUnavailable,
    /// <summary>Original payment or provider operation needs continuation, never another charge.</summary>
    Pending,
    /// <summary>Explicit store restoration is required before a separate new private context may start another payment.</summary>
    RecoveryRequired,
    /// <summary>An existing suspended, ambiguous or already scheduled subscription needs its original store management.</summary>
    ManageExisting,
}

/// <summary>Verified current/cache metadata and actual catalog choices; no provider reference is presented.</summary>
public sealed record ProductBillingPresentation(ProductBillingStatus Status, BillingClientView? Local,
    BillingClientView? Identity, IReadOnlyList<NativeBillingOffer> Offers, bool CanRestore, bool HasIdentity,
    bool ProRestoreRequired = false, bool OfferChanged = false)
{
    /// <summary>Best verified cached plan, independent of currently inactive commercial quotas.</summary>
    public ProductPlan Plan => (Local?.Decision.Plan ?? ProductPlan.Free) > (Identity?.Decision.Plan ?? ProductPlan.Free)
        ? Local!.Decision.Plan : Identity?.Decision.Plan ?? ProductPlan.Free;
}

/// <summary>Actual original store flow behind the comparison screen; effects remain explicit and testable.</summary>
public interface IProductBillingEnvironment
{
    /// <summary>Whether this edition has a native billing adapter.</summary>
    bool Available { get; }
    /// <summary>Loads original verified current state and actual store offers without starting payment.</summary>
    Task<ProductBillingPresentation> LoadAsync(CancellationToken ct);
    /// <summary>Rechecks the reviewed exact native terms, retains intent, then opens one actual native confirmation.</summary>
    Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct);
    /// <summary>Explicit actual-store restoration; Pro uses the current actual identity, Plus remains account-free.</summary>
    Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct);
    /// <summary>Continues original literal requests before separately querying owned purchases; it never launches payment.</summary>
    Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct);
    /// <summary>Opens only the fixed official platform subscription-management surface.</summary>
    Task ManageAsync(CancellationToken ct);
}
