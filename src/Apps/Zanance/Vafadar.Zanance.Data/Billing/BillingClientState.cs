using System.Text.Json.Serialization;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Billing;

/// <summary>Original device state inside one platform-protected slot, never a financial database entity or backup preference.</summary>
internal sealed record BillingClientState(int Version, string Peer, BillingIssuer Distribution, BillingEnvironment Environment,
    string? IdentityHash, Guid Customer, string PrivateIdentity, string PublicIdentity, string Registration,
    string? Authority = null, string? PendingCommand = null, string? Cache = null, Guid CacheRequest = default,
    BillingClockAnchor? Anchor = null, long Revision = 0, DateTimeOffset MinimumServerTime = default,
    BillingPurchaseState? LastPurchaseState = null, Guid? NativePayment = null, string? LastOperation = null, bool DeviceRetired = false,
    BillingProduct? NativeProduct = null, string[]? NativePriorReceipts = null, string? NativeReceipt = null,
    Guid PrivateGeneration = default, bool RequiresStoreRestore = false, string? NativeReplacementId = null)
{
    /// <inheritdoc />
    public override string ToString() => "Protected original billing device state";
}

/// <summary>Protected routing metadata only; original private key slots are never overwritten by restoration.</summary>
internal sealed record BillingPrivateRoute(Guid Current, Guid? Candidate = null);

/// <summary>Reflection-free native private state and public key parser for trimmed callers.</summary>
[JsonSerializable(typeof(BillingClientState))]
[JsonSerializable(typeof(BillingPrivateRoute))]
[JsonSerializable(typeof(PublicSigningRecipientIdentity))]
internal partial class BillingClientJson : JsonSerializerContext;
