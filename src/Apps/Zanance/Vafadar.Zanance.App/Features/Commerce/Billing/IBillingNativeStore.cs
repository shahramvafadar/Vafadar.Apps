using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Actual native catalog choice; the handle is local and never an externally supplied payment URL.</summary>
public sealed record NativeBillingOffer(string Handle, string Title, string Description, BillingStoreOffer Offer, string StoreIdentity,
    string? StoreProductId = null, BillingReplacement? Replacement = null, bool ExistingSubscription = false);

/// <summary>Actual native-owned purchase reference, confined to private verification and never diagnostic output.</summary>
public sealed record NativeBillingPurchase(string Reference, BillingProduct Hint, bool Pending, string? StoreProductId = null)
{
    /// <inheritdoc />
    public override string ToString() => "Native purchase awaiting verification";
}

/// <summary>Transient exact SDK replacement input; the raw original token never enters a view model or diagnostics.</summary>
public sealed record NativeBillingReplacement(string OriginalReference, string OriginalStoreProductId, BillingReplacementTiming Timing)
{
    /// <inheritdoc />
    public override string ToString() => "Private native subscription replacement";
}

/// <summary>Content-free native outcome; cancellation and unavailable service never imply a financial refund.</summary>
internal sealed class NativeBillingRefusal(string code) : Exception(code);

/// <summary>Native store effects only; no callback alone can grant paid capabilities.</summary>
public interface IBillingNativeStore
{
    /// <summary>Actual platform distribution, separate from cross-store identity-bound purchase authority.</summary>
    BillingIssuer Distribution { get; }
    /// <summary>Actual native store country, independent of language and financial display choices.</summary>
    Task<string> CountryAsync(CancellationToken ct);
    /// <summary>Reads current store products and phases joined only to the trusted issuer catalog.</summary>
    Task<IReadOnlyList<NativeBillingOffer>> CatalogAsync(IReadOnlyList<BillingProductMapping> catalog, CancellationToken ct);
    /// <summary>Queries the actual store's owned/pending purchases; it never starts another payment.</summary>
    Task<IReadOnlyList<NativeBillingPurchase>> OwnedAsync(IReadOnlyList<BillingProductMapping> catalog, CancellationToken ct);
    /// <summary>Launches one actual user-selected native flow using the verified service's account binding.</summary>
    Task<IReadOnlyList<NativeBillingPurchase>> PurchaseAsync(Guid intent, string handle, string accountBinding, CancellationToken ct,
        NativeBillingReplacement? replacement = null);
    /// <summary>Observes the same still-retained SDK task after native foreground returns, without launching another flow.</summary>
    Task<IReadOnlyList<NativeBillingPurchase>?> ContinuePaymentAsync(Guid intent, CancellationToken ct);
    /// <summary>Finishes only an original service-verified purchase; Play acknowledgement remains durable server work.</summary>
    Task FinishAsync(NativeBillingPurchase purchase, BillingCommandResult verified, CancellationToken ct);
    /// <summary>Signals actual store updates; a coordinator still verifies each original reference with the service.</summary>
    event EventHandler? PurchasesChanged;
}
