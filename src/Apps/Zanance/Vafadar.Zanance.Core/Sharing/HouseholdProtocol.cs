using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Public-only standard SPKI identities; these fields never contain device/owner/epoch private keys.</summary>
public sealed record HouseholdPublicKeys([property: JsonRequired] string SigningKey, [property: JsonRequired] string WrappingKey);

/// <summary>One owner-approved public device inside a signed membership generation.</summary>
public sealed record HouseholdDeviceDefinition([property: JsonRequired] Guid Id, [property: JsonRequired] HouseholdPublicKeys Keys);

/// <summary>One accepted identity and its exact role/devices; knowing this metadata never supplies a decryption grant.</summary>
public sealed record HouseholdMemberDefinition([property: JsonRequired] string IdentityId,
    [property: JsonRequired] HouseholdRole Role, [property: JsonRequired] string AcceptanceHash,
    [property: JsonRequired] HouseholdDeviceDefinition[] Devices);

/// <summary>Explicit owner-root transition type, never inferred from an altered server key directory.</summary>
public enum HouseholdMembershipChange
{
    /// <summary>Original locally pinned owner root and device.</summary>
    Genesis = 1,
    /// <summary>An explicitly accepted invitation is finally approved.</summary>
    AcceptMember = 2,
    /// <summary>Approve another key after independent fingerprint verification.</summary>
    ApproveDevice = 3,
    /// <summary>Revoke a member and rotate to a key it never receives.</summary>
    RevokeMember = 4,
    /// <summary>Revoke a device and rotate away from its known epoch.</summary>
    RevokeDevice = 5,
    /// <summary>Confirm a previously requested departure and finish required rotation.</summary>
    ConfirmDeparture = 6,
    /// <summary>Explicit owner/editor/viewer role change, with fresh encryption generation.</summary>
    ChangeRole = 7,
    /// <summary>Old owner consent and new owner acceptance, with a new owner-root private key.</summary>
    TransferOwner = 8,
    /// <summary>Recovery with current owner-root proof and a separately held recovery kit; lost keys are retired.</summary>
    RecoverOwner = 9,
    /// <summary>Explicitly rotate an unchanged owner's root, including removal/replacement of a separately held recovery kit.</summary>
    RotateOwnerRoot = 10,
}

/// <summary>Complete standard-signed public authority; financial labels, values and all decryption keys stay encrypted.</summary>
public sealed record HouseholdMembershipStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] HouseholdMembershipChange Change, [property: JsonRequired] Guid? InvitationId,
    [property: JsonRequired] long ExpectedVersion, [property: JsonRequired] long Version,
    [property: JsonRequired] string PreviousHash, [property: JsonRequired] string PreviousOwnerId,
    [property: JsonRequired] string OwnerId, [property: JsonRequired] HouseholdPublicKeys OwnerKeys,
    [property: JsonRequired] long KeyEpoch, [property: JsonRequired] string KeyCommitment,
    [property: JsonRequired] string AddressKeyCommitment, [property: JsonRequired] long ValidUntilUnixSeconds,
    [property: JsonRequired] HouseholdMemberDefinition[] Members,
    [property: JsonRequired] long PublicationCursor = 0,
    [property: JsonRequired] string PublicationHead = "0000000000000000000000000000000000000000000000000000000000000000");

/// <summary>One encrypted current/historical epoch grant to one explicit approved device.</summary>
public sealed record HouseholdEncryptedGrant([property: JsonRequired] long Epoch,
    [property: JsonRequired] Guid DeviceId, [property: JsonRequired] string Ciphertext, [property: JsonRequired] string Digest);

/// <summary>Complete atomic membership publication; a transfer also requires exact new-root acceptance.</summary>
public sealed record HouseholdPublishMembership([property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] string SignedStatement, [property: JsonRequired] string? TransferAcceptance,
    [property: JsonRequired] string? SignedAcceptanceClaim,
    [property: JsonRequired] HouseholdEncryptedGrant[] Grants,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdRecoveryMutation? Recovery = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdRecoveryEvidence? RecoveryEvidence = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdOwnerRootGrant[]? OwnerRootGrants = null);

/// <summary>Independent target-root consent to the exact signed transition and complete encrypted grant inventory.</summary>
public sealed record HouseholdOwnerAcceptanceStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] string MembershipStatementHash, [property: JsonRequired] string GrantsHash);

/// <summary>Complete public membership evidence; recipients independently pin/verify roots and never trust the service's signing-key directory.</summary>
public sealed record HouseholdMembershipProof(string SignedStatement, string? TransferAcceptance, string? SignedAcceptanceClaim,
    string GrantsHash, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdRecoveryEvidence? RecoveryEvidence = null);

/// <summary>Idempotent room reservation before financial data; server chooses the room/device resource ids.</summary>
public sealed record HouseholdReserveSpace([property: JsonRequired] Guid OperationId,
    [property: JsonRequired] string SchemaHash, [property: JsonRequired] HouseholdPublicKeys OwnerKeys,
    [property: JsonRequired] HouseholdPublicKeys DeviceKeys);

/// <summary>Original reservation state retained across a process exit before genesis completion.</summary>
public sealed record HouseholdSpaceReservation(Guid SpaceId, Guid DeviceId, string OwnerId, bool Initialized);

/// <summary>Actual authenticated session binding for private-key enrollment/rebind proof, never an authentication credential.</summary>
public sealed record HouseholdSessionIdentity(string IdentityId, Guid SessionId);

/// <summary>Possession proof binds a new/persistent public device key to the actual current identity/session and bounded operation.</summary>
public sealed record HouseholdDeviceSessionStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid DeviceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] string IdentityId, [property: JsonRequired] Guid SessionId,
    [property: JsonRequired] HouseholdPublicKeys DeviceKeys, [property: JsonRequired] long ValidUntilUnixSeconds);

/// <summary>New device enrollment remains pending until a separately signed owner approval and epoch rotation.</summary>
public sealed record HouseholdEnrollDevice([property: JsonRequired] HouseholdPublicKeys DeviceKeys,
    [property: JsonRequired] string SignedPossession);

/// <summary>Rebinding an already approved key requires its private signature plus an actual new authenticated session.</summary>
public sealed record HouseholdRebindDevice([property: JsonRequired] string SignedPossession);

/// <summary>Room-scoped public device observation; a new login cannot infer approval from this response.</summary>
public sealed record HouseholdDeviceObservation(Guid DeviceId, string IdentityId, string Fingerprint, bool Approved, bool Revoked);

/// <summary>Owner-scoped original self-enrollment of an already accepted member; it grants no data access by itself.</summary>
public sealed record HouseholdPendingDevice(Guid DeviceId, string IdentityId, HouseholdPublicKeys DeviceKeys, string SignedPossession);

/// <summary>One-use invitation metadata, signed by the pinned owner root and delivered outside the untrusted key directory.</summary>
public sealed record HouseholdInvitationStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid InvitationId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] long MembershipVersion,
    [property: JsonRequired] string MembershipHead, [property: JsonRequired] HouseholdRole Role,
    [property: JsonRequired] string CapabilityHash, [property: JsonRequired] long ExpiresUnixSeconds);

/// <summary>Creates only a reserved seat; no data/key access exists before explicit acceptance and owner approval.</summary>
public sealed record HouseholdCreateInvitation([property: JsonRequired] Guid DeviceId, [property: JsonRequired] string SignedInvitation);

/// <summary>One public invitation resource reference, never its independent private capability.</summary>
public sealed record HouseholdInvitationReservation(Guid InvitationId);

/// <summary>Explicitly shared out-of-band invitation; its independent capability is private and must never enter logs or a URL.</summary>
public sealed record HouseholdInvitationPackage(int Format, HouseholdPublicKeys OwnerKeys, string SignedInvitation, string Capability)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdInvitationPackage);
}

/// <summary>Owner-only pending recipient proof; the client verifies both original signatures and confirms the joint device fingerprint.</summary>
public sealed record HouseholdPendingClaim(Guid InvitationId, string SignedInvitation, string SignedClaim,
    string IdentityId, Guid DeviceId, HouseholdPublicKeys DeviceKeys);

/// <summary>Exact device-protected original setup bytes for lost-acknowledgement retry; never a portable credential.</summary>
public sealed record HouseholdSetupIntent(Guid OperationId, string SignedBody, string? Capability)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdSetupIntent);
}

/// <summary>Exact authenticated recipient grant response; no API database entity or private key is part of the wire contract.</summary>
public sealed record HouseholdDeviceGrant(long Epoch, Guid DeviceId, long MembershipVersion, string Ciphertext, string Digest);

/// <summary>Possession proof binds the exact claiming identity and new public device key to an invitation/checkpoint.</summary>
public sealed record HouseholdInvitationClaimStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid InvitationId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] string IdentityId,
    [property: JsonRequired] string InvitationHash, [property: JsonRequired] string OwnerFingerprint,
    [property: JsonRequired] HouseholdPublicKeys DeviceKeys);

/// <summary>Private HTTPS claim body; never a URL query parameter, log entry or authorization to decrypt.</summary>
public sealed record HouseholdClaimInvitation([property: JsonRequired] string Capability,
    [property: JsonRequired] string SignedClaim, [property: JsonRequired] HouseholdPublicKeys DeviceKeys);

/// <summary>Content-free pending state; the owner must still confirm the claimed fingerprint independently.</summary>
public sealed record HouseholdInvitationClaimResult(Guid InvitationId, Guid DeviceId, string IdentityId,
    string Fingerprint, HouseholdRole ProposedRole, bool Accepted);

/// <summary>Opens exactly one staged opaque publication against current observed authority.</summary>
public sealed record HouseholdOpenRound([property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] Guid AttemptId, [property: JsonRequired] long ExpectedCursor,
    [property: JsonRequired] long MembershipVersion, [property: JsonRequired] long KeyEpoch,
    [property: JsonRequired] string SchemaHash);

/// <summary>Bounded encrypted record/tombstone; the service never receives its table/key, values or blob plaintext.</summary>
public sealed record HouseholdOpaquePart([property: JsonRequired] int Index,
    [property: JsonRequired] string RecordId, [property: JsonRequired] long BaseVersion,
    [property: JsonRequired] string Ciphertext, [property: JsonRequired] string Digest);

/// <summary>Author-signed complete ciphertext transaction metadata, linked to its authenticated predecessor.</summary>
public sealed record HouseholdCommitStatement([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] Guid AttemptId, [property: JsonRequired] long ExpectedCursor,
    [property: JsonRequired] long Cursor, [property: JsonRequired] long MembershipVersion,
    [property: JsonRequired] long KeyEpoch, [property: JsonRequired] string PreviousHead,
    [property: JsonRequired] int Parts, [property: JsonRequired] string ContentHash);

/// <summary>Only a complete signed ciphertext digest can publish the staged round atomically.</summary>
public sealed record HouseholdCommitRound([property: JsonRequired] Guid DeviceId, [property: JsonRequired] string SignedCommit);

/// <summary>Server-created staging identity plus retained immutable completion, independent of transport observation timeouts.</summary>
public sealed record HouseholdRoundHandle(Guid Id, int Parts, long Bytes, long? CommittedCursor,
    bool Abandoned, bool Acknowledged, string? SignedCommit);

/// <summary>One immutable encrypted event carrying only bounded public routing/authority metadata.</summary>
public sealed record HouseholdOpaqueEvent(long Cursor, Guid RoundId, string RecordId, long KeyEpoch, long MembershipVersion,
    Guid DeviceId, Guid OperationId, Guid AttemptId, int Index, long BaseVersion, string Ciphertext, string Digest);

/// <summary>Public ordered ciphertext commitment; no financial table, key, value or attachment plaintext.</summary>
public sealed record HouseholdPartCommitment(int Index, string RecordId, long BaseVersion, string Digest);

/// <summary>Full original author-signed completion and exact ordered manifest, verified before applying any part.</summary>
public sealed record HouseholdPublicationReceipt(Guid RoundId, string SignedCommit, HouseholdPartCommitment[] Parts);

/// <summary>One bounded immutable ordered manifest page; consumers join only the same original signed completion and exact contiguous offsets.</summary>
public sealed record HouseholdReceiptPage(Guid RoundId, string SignedCommit, int Offset, int Total, HouseholdPartCommitment[] Parts, bool Complete);

/// <summary>Exact immutable uploaded-part observations, not a highest-index or count substitute for matched original ciphertext.</summary>
public sealed record HouseholdUploadedParts(Guid RoundId, int Offset, int Total, HouseholdPartCommitment[] Parts, bool Complete);

/// <summary>Finite original ciphertext upload batch; the service still validates every part and commits staging atomically.</summary>
public sealed record HouseholdUploadParts([property: JsonRequired] Guid DeviceId, [property: JsonRequired] HouseholdOpaquePart[] Parts);

/// <summary>Exact ordered position inside an atomic multi-record cursor; a count alone never proves a prefix.</summary>
public sealed record HouseholdFeedPosition(long Cursor, string RecordId);

/// <summary>Bounded feed page frozen at one declared cursor; a client cannot publish an incomplete local financial graph.</summary>
public sealed record HouseholdFeedPage(long ThroughCursor, HouseholdOpaqueEvent[] Events,
    HouseholdFeedPosition Next, bool Complete);

/// <summary>Public checkpoint; clients still verify the owner-signed chain and their independently pinned root.</summary>
public sealed record HouseholdCheckpoint(Guid SpaceId, string OwnerId, long MembershipVersion,
    string MembershipHead, long KeyEpoch, long Cursor, string PublicationHead, bool RotationRequired,
    HouseholdRetentionState Retention, long ObservedAtUnixSeconds = 0, long? HostExpiresAtUnixSeconds = null);

/// <summary>Explicit metadata JSON, without dynamic CLR types, private key handles or case-insensitive wire reinterpretation.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(HouseholdMembershipStatement))]
[JsonSerializable(typeof(HouseholdPublishMembership))]
[JsonSerializable(typeof(HouseholdOwnerAcceptanceStatement))]
[JsonSerializable(typeof(HouseholdMembershipProof[]))]
[JsonSerializable(typeof(HouseholdReserveSpace))]
[JsonSerializable(typeof(HouseholdSpaceReservation))]
[JsonSerializable(typeof(HouseholdSessionIdentity))]
[JsonSerializable(typeof(HouseholdDeviceSessionStatement))]
[JsonSerializable(typeof(HouseholdEnrollDevice))]
[JsonSerializable(typeof(HouseholdRebindDevice))]
[JsonSerializable(typeof(HouseholdDeviceObservation))]
[JsonSerializable(typeof(HouseholdInvitationStatement))]
[JsonSerializable(typeof(HouseholdCreateInvitation))]
[JsonSerializable(typeof(HouseholdInvitationReservation))]
[JsonSerializable(typeof(HouseholdInvitationPackage))]
[JsonSerializable(typeof(HouseholdPendingClaim[]))]
[JsonSerializable(typeof(HouseholdPendingDevice[]))]
[JsonSerializable(typeof(HouseholdSetupIntent))]
[JsonSerializable(typeof(HouseholdDeviceGrant))]
[JsonSerializable(typeof(HouseholdInvitationClaimStatement))]
[JsonSerializable(typeof(HouseholdClaimInvitation))]
[JsonSerializable(typeof(HouseholdInvitationClaimResult))]
[JsonSerializable(typeof(HouseholdOpenRound))]
[JsonSerializable(typeof(HouseholdOpaquePart))]
[JsonSerializable(typeof(HouseholdCommitStatement))]
[JsonSerializable(typeof(HouseholdCommitRound))]
[JsonSerializable(typeof(HouseholdRoundHandle))]
[JsonSerializable(typeof(HouseholdFeedPage))]
[JsonSerializable(typeof(HouseholdCheckpoint))]
[JsonSerializable(typeof(HouseholdPublicationReceipt))]
[JsonSerializable(typeof(HouseholdReceiptPage))]
[JsonSerializable(typeof(HouseholdUploadedParts))]
[JsonSerializable(typeof(HouseholdUploadParts))]
[JsonSerializable(typeof(HouseholdRecoveryCertificate))]
[JsonSerializable(typeof(HouseholdRecoveryConsent))]
[JsonSerializable(typeof(HouseholdRecoveryEvidence))]
[JsonSerializable(typeof(HouseholdRecoveryMutation))]
[JsonSerializable(typeof(HouseholdRecoveryObservation))]
[JsonSerializable(typeof(HouseholdRecoveryEpochPage))]
[JsonSerializable(typeof(HouseholdRecoverOwner))]
[JsonSerializable(typeof(HouseholdRecoverySeedDeclaration))]
[JsonSerializable(typeof(HouseholdOpenRecoverySeed))]
[JsonSerializable(typeof(HouseholdRecoverySeedProgress))]
[JsonSerializable(typeof(HouseholdUploadRecoveryEpochs))]
[JsonSerializable(typeof(HouseholdOwnerRootGrant))]
[JsonSerializable(typeof(HouseholdRecoveryAnchor))]
[JsonSerializable(typeof(HouseholdOwnershipOffer))]
[JsonSerializable(typeof(HouseholdDeleteStatement))]
[JsonSerializable(typeof(HouseholdDeleteRequest))]
[JsonSerializable(typeof(HouseholdDeletionCheckpoint))]
[JsonSerializable(typeof(HouseholdDeletionReview))]
[JsonSerializable(typeof(HouseholdDeletionIntent))]
[JsonSerializable(typeof(HouseholdDeletionReceipt))]
[JsonSerializable(typeof(HouseholdLifecycleStatus))]
[JsonSerializable(typeof(HouseholdAuditEvidence))]
[JsonSerializable(typeof(HouseholdAuditItem))]
[JsonSerializable(typeof(HouseholdAuditPage))]
[JsonSerializable(typeof(HouseholdOwnershipProof))]
[JsonSerializable(typeof(HouseholdOwnershipPreparation))]
[JsonSerializable(typeof(HouseholdOwnershipAcceptance))]
[JsonSerializable(typeof(HouseholdOwnershipObservation))]
[JsonSerializable(typeof(HouseholdOpenOwnership))]
[JsonSerializable(typeof(HouseholdPrepareOwnership))]
[JsonSerializable(typeof(HouseholdAuthorizeOwnership))]
[JsonSerializable(typeof(HouseholdAcceptOwnership))]
[JsonSerializable(typeof(HouseholdObserveOwnership))]
[JsonSerializable(typeof(HouseholdCancelOwnership))]
[JsonSerializable(typeof(HouseholdDeviceAccessPackage))]
[JsonSerializable(typeof(HouseholdDeviceAccessStatement))]
public partial class HouseholdMetadataJsonContext : JsonSerializerContext;
