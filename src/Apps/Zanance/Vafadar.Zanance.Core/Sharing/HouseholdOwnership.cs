using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Explicit disposition of the former owning identity, never an inferred financial ownership share.</summary>
public enum HouseholdFormerOwnerRole
{
    /// <summary>Remove the former identity and all its devices from the next signed roster.</summary>
    Leave,
    /// <summary>Retain the former identity with explicit read-only access and no root delegation.</summary>
    Viewer,
    /// <summary>Retain explicit financial editor access, without any ownership root delegation.</summary>
    Editor,
}
/// <summary>Durable public handshake stages; none before Published changes the original signed membership.</summary>
public enum HouseholdOwnershipStage
{
    /// <summary>No participant operation was observed.</summary>
    None,
    /// <summary>The original source owner signed its exact target/device/disposition consent.</summary>
    Offered,
    /// <summary>The original target retained its independent private candidates and public recipient inventory.</summary>
    Prepared,
    /// <summary>The original source signed the target's exact membership statement.</summary>
    Authorized,
    /// <summary>The target accepted that literal source signature and fixed root-recipient inventory.</summary>
    Accepted,
    /// <summary>The exact accepted membership was committed atomically by the source.</summary>
    Published,
    /// <summary>The original operation has an authenticated explicit cancellation receipt.</summary>
    Canceled,
    /// <summary>The service observed the original exclusive deadline without publishing the operation.</summary>
    Expired,
}

/// <summary>Original owner-root consent to one exact existing target device, checkpoint and former-owner role.</summary>
public sealed record HouseholdOwnershipOffer(int Format, Guid SpaceId, Guid OperationId, Guid OwnerDeviceId, string OwnerId,
    Guid TargetDeviceId, string TargetId, HouseholdFormerOwnerRole FormerRole, long ExpectedVersion, string ExpectedHead,
    long PublicationCursor, string PublicationHead, long ExpiresUnixSeconds);
/// <summary>One current target-device proof over exact public candidate/final bytes, not a service identity directory entry.</summary>
public sealed record HouseholdOwnershipProof(int Format, Guid SpaceId, Guid OperationId, Guid DeviceId, string IdentityId,
    HouseholdOwnershipStage Stage, string PayloadDigest);
/// <summary>Public next roster and standard opaque recipient grants; private root/epoch bytes never enter this object.</summary>
public sealed record HouseholdOwnershipPreparation(HouseholdMembershipStatement Statement, HouseholdEncryptedGrant[] Grants, string SignedDeviceProof);
/// <summary>Target acceptance of the exact old-root-signed statement plus standard private-root delegations to only its existing approved devices.</summary>
public sealed record HouseholdOwnershipAcceptance(string SignedAcceptance, HouseholdOwnerRootGrant[] OwnerRootGrants, string SignedDeviceProof);
/// <summary>Finite participant-scoped original handshake observation; public metadata/ciphertext only.</summary>
public sealed record HouseholdOwnershipObservation(Guid? OperationId, HouseholdOwnershipStage Stage, string? SignedOffer,
    HouseholdOwnershipPreparation? Preparation, string? SignedStatement, HouseholdOwnershipAcceptance? Acceptance, HouseholdMembershipProof? PublishedProof, long AnchorVersion = 0);
/// <summary>Original source request; actual HTTP identity/session/device are independently required.</summary>
public sealed record HouseholdOpenOwnership(Guid DeviceId, string SignedOffer);
/// <summary>Target's original candidate, bound to its actual approved device.</summary>
public sealed record HouseholdPrepareOwnership(Guid DeviceId, HouseholdOwnershipPreparation Preparation);
/// <summary>Exact old-root signature, persisted before submission and never regenerated for a lost acknowledgement.</summary>
public sealed record HouseholdAuthorizeOwnership(Guid DeviceId, string SignedStatement);
/// <summary>Final target private-key acceptance, without publishing or changing financial data.</summary>
public sealed record HouseholdAcceptOwnership(Guid DeviceId, HouseholdOwnershipAcceptance Acceptance);
/// <summary>Actual-device operation metadata; it cannot authorize new grants or a financial write.</summary>
public sealed record HouseholdObserveOwnership(Guid DeviceId, string SignedPossession);
/// <summary>Explicit cancellation/decline; current source owner or original target can stop only the original pending operation.</summary>
public sealed record HouseholdCancelOwnership(Guid DeviceId);

/// <summary>Fixed-purpose ownership framing and independent finite public-byte commitments; maintained JOSE supplies signatures/encryption.</summary>
public static class HouseholdOwnershipProtocol
{
    /// <summary>Original old-root offer purpose.</summary>
    public const string OfferType = "zanance-household-ownership-offer+jwt";
    /// <summary>Current target device's exact original candidate/acceptance purpose.</summary>
    public const string DeviceProofType = "zanance-household-ownership-device+jwt";
    /// <summary>Participant metadata observation only, including after the original owner leaves.</summary>
    public const string ObservationType = "zanance-household-ownership-observe+jwt";
    /// <summary>Finite standard encrypted inventory for at most six identities/eight devices each, not an unbounded financial attachment.</summary>
    public const int MaximumBytes = 2 * 1024 * 1024;

    /// <summary>Canonical typed public candidate digest; no target signature participates in its own commitment.</summary>
    public static string PreparationDigest(HouseholdOwnershipPreparation value) => Hash(JsonSerializer.SerializeToUtf8Bytes(value with { SignedDeviceProof = "" },
        HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation));
    /// <summary>Canonical exact complete target acceptance digest, excluding only its own device proof.</summary>
    public static string AcceptanceDigest(HouseholdOwnershipAcceptance value) => Hash(JsonSerializer.SerializeToUtf8Bytes(value with { SignedDeviceProof = "" },
        HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance));
    /// <summary>Original exact typed roster commitment, without signature nondeterminism.</summary>
    public static string StatementDigest(HouseholdMembershipStatement value) => Hash(JsonSerializer.SerializeToUtf8Bytes(value, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement));
    /// <summary>Bounds the public commitment before hashing; temporary serialized bytes remain caller-owned here.</summary>
    private static string Hash(byte[] value)
    {
        try { if (value.Length > MaximumBytes) throw new InvalidDataException("Household ownership metadata bound exceeded."); return Convert.ToHexStringLower(SHA256.HashData(value)); }
        finally { CryptographicOperations.ZeroMemory(value); }
    }

    /// <summary>Requires the offer's exact old/target device and role effect in the already validated current signed roster.</summary>
    public static bool Matches(HouseholdOwnershipOffer offer, HouseholdMembershipStatement before, HouseholdMembershipStatement next)
    {
        if (offer.Format != 1 || offer.SpaceId != before.SpaceId || offer.OperationId == Guid.Empty || !Enum.IsDefined(offer.FormerRole) ||
            offer.OwnerId != before.OwnerId || offer.TargetId == offer.OwnerId || offer.ExpectedVersion != before.Version ||
            next.OperationId != offer.OperationId || next.SpaceId != offer.SpaceId || next.Change != HouseholdMembershipChange.TransferOwner ||
            next.ExpectedVersion != offer.ExpectedVersion || next.PreviousHash != offer.ExpectedHead || next.PreviousOwnerId != offer.OwnerId || next.OwnerId != offer.TargetId ||
            next.PublicationCursor != offer.PublicationCursor || next.PublicationHead != offer.PublicationHead || next.ValidUntilUnixSeconds != offer.ExpiresUnixSeconds ||
            !before.Members.Any(member => member.IdentityId == offer.OwnerId && member.Devices.Any(device => device.Id == offer.OwnerDeviceId)) ||
            !before.Members.Any(member => member.IdentityId == offer.TargetId && member.Role is HouseholdRole.Editor or HouseholdRole.Viewer && member.Devices.Any(device => device.Id == offer.TargetDeviceId))) return false;
        var old = next.Members.SingleOrDefault(member => member.IdentityId == offer.OwnerId);
        return offer.FormerRole switch
        {
            HouseholdFormerOwnerRole.Leave => old is null,
            HouseholdFormerOwnerRole.Editor => old?.Role == HouseholdRole.Editor,
            HouseholdFormerOwnerRole.Viewer => old?.Role == HouseholdRole.Viewer,
            _ => false,
        };
    }
}
