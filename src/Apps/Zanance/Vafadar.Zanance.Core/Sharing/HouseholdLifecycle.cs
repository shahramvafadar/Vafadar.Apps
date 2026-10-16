namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Fixed public lifecycle purposes; none authorizes a financial change or a different room.</summary>
public static class HouseholdLifecycleProtocol
{
    /// <summary>Current owner-root consent to permanently stop exactly the reviewed remote room.</summary>
    public const string DeleteType = "zanance-household-delete+jwt";
    /// <summary>Finite public owner declaration, without financial content or private key material.</summary>
    public const int DeclarationBytes = 8192;
}

/// <summary>Explicit original owner deletion bound to the reviewed signed membership and financial checkpoint.</summary>
public sealed record HouseholdDeleteStatement(int Format, Guid SpaceId, Guid OperationId, string OwnerId, Guid DeviceId,
    long MembershipVersion, string MembershipHead, long PublicationCursor, string PublicationHead, long ValidUntilUnixSeconds);
/// <summary>Actual approved owner device plus the exact original private-root declaration.</summary>
public sealed record HouseholdDeleteRequest(Guid DeviceId, string SignedDeclaration);
/// <summary>Owner-only exact privacy review checkpoint, available during retained time without any financial record, epoch grant or new write authority.</summary>
public sealed record HouseholdDeletionCheckpoint(Guid SpaceId, Guid DeviceId, string OwnerId, string RootFingerprint,
    long MembershipVersion, string MembershipHead, long PublicationCursor, string PublicationHead, long ObservedAtUnixSeconds);
/// <summary>Exact original request review under the room lock; only an unaccepted expired request may be explicitly withdrawn locally.</summary>
public sealed record HouseholdDeletionReview(Guid SpaceId, Guid OperationId, string RequestHash, bool Rejected, HouseholdDeletionReceipt? Receipt);
/// <summary>Protected device-local original declaration and its authenticated service outcome; no financial data or private key frame.</summary>
public sealed record HouseholdDeletionIntent(HouseholdDeleteRequest Request, long AnchorVersion, HouseholdDeletionReceipt? Receipt = null, bool Rejected = false);
/// <summary>Why future service access permanently stopped; no reason recalls previously downloaded copies.</summary>
public enum HouseholdDeletionReason
{
    /// <summary>The actual current owner explicitly deleted the remote room.</summary>
    Owner = 1,
    /// <summary>The approved total120-day host-expiry retention interval ended.</summary>
    RetentionEnded = 2,
}
/// <summary>Owner-only minimal original-operation receipt; physical database retirement is distinct from access revocation.</summary>
public sealed record HouseholdDeletionReceipt(Guid SpaceId, Guid OperationId, string RequestHash, HouseholdDeletionReason Reason,
    long DeclaredAtUnixSeconds, bool StorageRetired);
/// <summary>Content-free service deadlines, independently of financial read/export authority.</summary>
public sealed record HouseholdLifecycleStatus(Guid SpaceId, HouseholdRetentionState Retention, long ObservedAtUnixSeconds,
    long? HostExpiresAtUnixSeconds, long? ReadUntilUnixSeconds, long? DeleteAfterUnixSeconds, bool StorageRetired);

/// <summary>Content-free operational event classification; signed membership/publication evidence is identified separately from service-reported time.</summary>
public enum HouseholdAuditAction
{
    /// <summary>Verified root-signed membership/key/role change.</summary>
    MembershipPublished = 1,
    /// <summary>An approved author committed one encrypted financial packet.</summary>
    EncryptedUpdate = 2,
    /// <summary>The authenticated member explicitly left, requiring a later owner key rotation.</summary>
    MemberLeft = 3,
    /// <summary>An already approved private key was rebound to an actual new session.</summary>
    DeviceRebound = 4,
}
/// <summary>Only public standard signatures, never root/key recipient ciphertext or financial rows.</summary>
public sealed record HouseholdAuditEvidence(HouseholdMembershipProof? Membership = null, string? SignedPublication = null);
/// <summary>One room-scoped audit item; its service-reported operational instant is not a cryptographically proven financial date.</summary>
public sealed record HouseholdAuditItem(long Sequence, HouseholdAuditAction Action, string ActorId, Guid DeviceId, Guid OperationId,
    long OccurredAtUnixSeconds, long MembershipVersion, string MembershipHead, long PublicationCursor, string PublicationHead, HouseholdAuditEvidence Evidence);
/// <summary>A finite original audit prefix; new events do not extend a previously reviewed pagination horizon.</summary>
public sealed record HouseholdAuditPage(Guid SpaceId, long Through, HouseholdAuditItem[] Items, long NextAfter, bool Complete);
