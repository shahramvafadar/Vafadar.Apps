using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Billing;

/// <summary>Local protected purchase state is missing or corrupt; explicit store restoration may establish a separate new context.</summary>
public sealed class BillingPrivateStateUnavailableException : IOException
{
    /// <summary>Exposes no key, receipt, private storage value or platform exception text.</summary>
    public BillingPrivateStateUnavailableException() : base("Original private purchase state requires explicit store restoration.") { }
}

/// <summary>One platform-protected atomic private slot; it never enters financial or portable storage.</summary>
public interface IBillingPrivateStorage
{
    /// <summary>Reads the original bounded protected slot without replacing corrupt or inaccessible state.</summary>
    Task<string?> ReadAsync(string scope, CancellationToken ct);
    /// <summary>Publishes one complete slot; keys, literal requests and signed floors change together.</summary>
    Task WriteAsync(string scope, string value, CancellationToken ct);
}

/// <summary>Fixed owned service transport, with separately validated current optional identity.</summary>
public interface IBillingServiceTransport
{
    /// <summary>Exact configured peer/certificate context, independent of financial profiles.</summary>
    string PeerBinding { get; }
    /// <summary>Current private service-confirmed subject; no credential or unverified bearer decoding is exposed.</summary>
    Task<string?> CurrentUserAsync(CancellationToken ct);
    /// <summary>Nonce-bound signed time obtained through the exact owned HTTPS peer.</summary>
    Task<BillingTimeResult> TimeAsync(Guid nonce, CancellationToken ct);
    /// <summary>Original self-owned registration; an account is optional for local store Plus.</summary>
    Task<BillingRegistrationResult> RegisterAsync(string? user, BillingRegistrationEnvelope input, CancellationToken ct);
    /// <summary>Exact retained original signed operation under the same actual optional session.</summary>
    Task<BillingCommandResult> CommandAsync(string? user, Guid customer, BillingSignedRequest input, CancellationToken ct);
}

/// <summary>Current native lifetime and monotonic time; no device wall clock is an entitlement authority.</summary>
public interface IBillingNativeAccess
{
    /// <summary>Refuses retired profile/navigation, background and locked publication.</summary>
    void Validate();
    /// <summary>Reads the actual native boot/session and elapsed time, or explicit unavailable evidence.</summary>
    BillingMonotonicInstant ReadMonotonic();
}

/// <summary>Presentation-only verified billing state; it cannot activate commercial quotas or a payment.</summary>
public sealed record BillingClientView(BillingSnapshot? Snapshot, BillingCacheDecision Decision, bool HasPendingCommand,
    BillingPurchaseState? LastPurchaseState, bool HasUnresolvedNativePayment, Guid? NativePaymentId = null,
    BillingProduct? NativeProduct = null, bool RequiresStoreRestore = false, string? NativeReplacementId = null);
