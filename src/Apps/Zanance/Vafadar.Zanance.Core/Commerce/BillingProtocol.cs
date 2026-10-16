using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Explicit signed operation; status and restore never imply a new purchase.</summary>
public enum BillingOperation
{
    /// <summary>Fresh signed complete service state; it is not a new provider verification.</summary>
    Observe,
    /// <summary>Provider verification of an original native purchase result.</summary>
    VerifyPurchase,
    /// <summary>Explicit restoration from the actual store's current owned-purchase query.</summary>
    RestorePurchase,
    /// <summary>Requery already bound purchases, without replacing their ownership.</summary>
    Refresh,
    /// <summary>Explicitly associate this installation with its actual authenticated identity.</summary>
    LinkIdentity,
    /// <summary>Retire only this device's billing principal, not a purchase or identity.</summary>
    RetireDevice,
}

/// <summary>Original key-possession declaration; idempotent registration never generates replacement keys.</summary>
public sealed record BillingRegistration(Guid Id, BillingIssuer Distribution, BillingEnvironment Environment,
    string PublicIdentity, string? IdentityHash);

/// <summary>Original bounded command retained securely before HTTP; raw provider references must never enter logs.</summary>
public sealed record BillingCommand(Guid CustomerId, Guid OperationId, BillingOperation Operation, long ExpectedRevision,
    string? IdentityHash, BillingProduct? ProductHint, string? ProviderReference, DateTimeOffset Deadline, Guid? NativePaymentId = null,
    string? ExpectedReplacementId = null)
{
    /// <summary>Never expose a purchase token through automatic diagnostic formatting.</summary>
    public override string ToString() => "Protected billing command";
}

/// <summary>Only a fresh current-state exchange can replace the entitlement cache; old operation replies remain receipts.</summary>
public enum BillingSnapshotKind
{
    /// <summary>Immutable empty initial registration response.</summary>
    Registration,
    /// <summary>Original operation completion; may be replayed after a later refund and is not current cache authority.</summary>
    Operation,
    /// <summary>Fresh nonce-bound complete current service observation.</summary>
    Current,
}

/// <summary>
/// Device-bound signed complete state. Revision tracks principal changes, not other devices' purchase updates;
/// cache replacement additionally requires its exact fresh current-request nonce and nondecreasing signed server time.
/// </summary>
public sealed record BillingSnapshot(int Version, BillingSnapshotKind Kind, Guid CustomerId, Guid RequestId, string DeviceFingerprint,
    BillingIssuer Distribution, BillingEnvironment Environment, string? IdentityHash, Guid AccountToken, long Revision,
    DateTimeOffset ServerTime, IReadOnlyList<BillingCachedGrant> Grants, bool Retired, IReadOnlyList<BillingProductMapping>? Catalog = null);

/// <summary>One selected store's configured logical mapping; it never invents a live store price.</summary>
public sealed record BillingProductMapping(BillingProduct Product, string StoreProductId, string? BasePlanId, string TermsVersion, bool AvailableForPurchase = true, IReadOnlyList<BillingOfferDefinition>? Offers = null);

/// <summary>Explicit trusted offer classification; actual prices and eligibility must still come from the store.</summary>
public sealed record BillingOfferDefinition(string StoreOfferId, BillingOfferKind Kind, string? Country = null,
    DateTimeOffset? StartsAt = null, DateTimeOffset? EndsAt = null);

/// <summary>Public registration result, authenticated only through the configured HTTPS peer and retained signing anchor.</summary>
public sealed record BillingRegistrationResult(Guid CustomerId, Guid AccountToken, string Authority,
    string SignedSnapshot, IReadOnlyList<BillingProductMapping> Catalog)
{
    /// <summary>Signed state and account bindings never enter incidental diagnostic formatting.</summary>
    public override string ToString() => "Billing registration result";
}

/// <summary>Public nonce-bound server time, obtained through the configured HTTPS peer, never a device wall clock.</summary>
public sealed record BillingTimeProof(Guid RequestId, DateTimeOffset ServerTime, DateTimeOffset Deadline);

/// <summary>The retained signing anchor and its nonce-bound authenticated time proof.</summary>
public sealed record BillingTimeResult(string Authority, string SignedTime)
{
    /// <summary>No signed token in incidental diagnostics.</summary>
    public override string ToString() => "Billing time result";
}

/// <summary>Content-free operation outcome with a current signed snapshot and explicit native finish permission.</summary>
public sealed record BillingCommandResult(Guid OperationId, string SignedSnapshot, bool MayFinish,
    BillingPurchaseState? State, string? SignedReceipt = null)
{
    /// <summary>Original signed operation replies are private data, not log messages.</summary>
    public override string ToString() => "Billing operation result";
}

/// <summary>Original signed native completion permission, distinct from current paid-state authority.</summary>
public sealed record BillingOperationReceipt(Guid CustomerId, Guid OperationId, string CommandHash, string DeviceFingerprint,
    BillingIssuer Distribution, BillingEnvironment Environment, string? IdentityHash, bool MayFinish,
    BillingPurchaseState? State, DateTimeOffset ServerTime, Guid? NativePaymentId = null, BillingProduct? CompletedProduct = null);

/// <summary>Transport body with no automatic raw-token formatting.</summary>
public sealed record BillingSignedRequest(string Signed)
{
    /// <summary>Do not turn an opaque signed purchase request into a diagnostic string.</summary>
    public override string ToString() => "Signed billing request";
}

/// <summary>Self-owned public registration key; it creates no identity or paid capability before verification.</summary>
public sealed record BillingRegistrationEnvelope(string PublicIdentity, string Signed)
{
    /// <summary>Keep signed payloads out of incidental diagnostics.</summary>
    public override string ToString() => "Billing registration proof";
}

/// <summary>Reflection-free strict protocol metadata for server and trimmed native callers.</summary>
[JsonSerializable(typeof(BillingProductMapping[]))]
[JsonSerializable(typeof(BillingRegistration))]
[JsonSerializable(typeof(BillingCommand))]
[JsonSerializable(typeof(BillingSnapshot))]
[JsonSerializable(typeof(BillingRegistrationResult))]
[JsonSerializable(typeof(BillingTimeProof))]
[JsonSerializable(typeof(BillingTimeResult))]
[JsonSerializable(typeof(BillingCommandResult))]
[JsonSerializable(typeof(BillingOperationReceipt))]
[JsonSerializable(typeof(BillingSignedRequest))]
[JsonSerializable(typeof(BillingRegistrationEnvelope))]
[JsonSerializable(typeof(BillingPurchaseFacts))]
[JsonSerializable(typeof(BillingScheduledChange))]
[JsonSerializable(typeof(BillingCachedGrant[]))]
public partial class BillingJson : JsonSerializerContext;
