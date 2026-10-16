using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Fixed standard JWS purposes; an incoming header never selects a different verification algorithm or authority.</summary>
public static class HouseholdRecoveryProofTypes
{
    /// <summary>Current-root public enrollment certificate.</summary>
    public const string Enrollment = "zanance-household-recovery-enrollment+jwt";
    /// <summary>Current-owner finite original archive preparation.</summary>
    public const string Seed = "zanance-household-recovery-seed+jwt";
    /// <summary>Independent off-device kit consent to one exact owner/device/root recovery.</summary>
    public const string Consent = "zanance-household-recovery-consent+jwt";
    /// <summary>Fresh exact recovering device possession, separate from ordinary enrollment/session rebinding.</summary>
    public const string DevicePossession = "zanance-household-device-recovery+jwt";
}

/// <summary>Independent technical recovery bounds; these values never create paid allocation or financial write authority.</summary>
public static class HouseholdRecoveryBounds
{
    /// <summary>Finite page size for staged uploads and original encrypted epoch reads.</summary>
    public const int PageSize = 128;
    /// <summary>Maximum standard recipient-encrypted single epoch frame.</summary>
    public const int EpochCiphertextCharacters = 8192;
    /// <summary>Maximum recipient-encrypted owner-root package.</summary>
    public const int RootCiphertextCharacters = 65536;
    /// <summary>Maximum owner-signed public certificate/declaration.</summary>
    public const int MetadataCharacters = 16384;
    /// <summary>Finite cardinality independent of ciphertext byte bounds; epochs remain streamed in pages.</summary>
    public const long MaximumEpochs = 1_000_000;
    /// <summary>Maximum current plus staged opaque recovery bytes for one room, separate from financial live/history limits.</summary>
    public const long MaximumStoredBytes = 256L * 1024 * 1024;
    /// <summary>Preparation may resume within48 hours; final membership consent retains its original much shorter publication window.</summary>
    public static TimeSpan SeedLifetime { get; } = TimeSpan.FromHours(48);
}

/// <summary>Owner-signed public enrollment; its exact root generation must be independently verified, never trusted from a service directory.</summary>
public sealed record HouseholdRecoveryCertificate([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid EnrollmentId,
    [property: JsonRequired] string OwnerId, [property: JsonRequired] HouseholdPublicKeys OwnerKeys,
    [property: JsonRequired] HouseholdPublicKeys RecoveryKeys, [property: JsonRequired] long MembershipVersion,
    [property: JsonRequired] string MembershipHead, [property: JsonRequired] string RootCiphertextDigest);

/// <summary>Public content-free root/device recovery consent, signed by the separately held enrolled kit.</summary>
public sealed record HouseholdRecoveryConsent([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid EnrollmentId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] string IdentityId,
    [property: JsonRequired] Guid SessionId, [property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] string MembershipStatementHash, [property: JsonRequired] string GrantsHash);

/// <summary>Public retained evidence for recipients of an explicit owner recovery; it contains neither kit nor root private keys.</summary>
public sealed record HouseholdRecoveryEvidence([property: JsonRequired] string SignedEnrollment,
    [property: JsonRequired] string SignedConsent);

/// <summary>Explicit current enrollment mutation; kit replacement/removal requires a real owner-root rotation.</summary>
public enum HouseholdRecoveryMutationKind
{
    /// <summary>Enroll a new independent kit after the owner confirms its protected export.</summary>
    Enroll = 1,
    /// <summary>Append the new epoch or refresh the same enrolled kit's encrypted owner root after recovery.</summary>
    Refresh = 2,
    /// <summary>Remove recovery and rotate away from all private owner material the old kit could expose.</summary>
    Revoke = 3,
}

/// <summary>One bounded recipient-encrypted original epoch, identified by its verified granting root generation.</summary>
public sealed record HouseholdRecoveryEpochGrant([property: JsonRequired] long Epoch,
    [property: JsonRequired] long MembershipVersion, [property: JsonRequired] string Ciphertext,
    [property: JsonRequired] string Digest);

/// <summary>Atomic public certificate/opaque key-archive maintenance alongside one exact signed membership publication.</summary>
public sealed record HouseholdRecoveryMutation([property: JsonRequired] HouseholdRecoveryMutationKind Kind,
    [property: JsonRequired] Guid? ExpectedEnrollmentId, [property: JsonRequired] string? SignedEnrollment,
    [property: JsonRequired] string? RootCiphertext, [property: JsonRequired] string? RootDigest,
    [property: JsonRequired] HouseholdRecoveryEpochGrant[] Epochs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ArchiveOperationId = null);

/// <summary>Current-owner signed finite archive preparation; it reserves no financial publication and cannot change membership.</summary>
public sealed record HouseholdRecoverySeedDeclaration([property: JsonRequired] int Format,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] Guid DeviceId, [property: JsonRequired] Guid EnrollmentId,
    [property: JsonRequired] string OwnerId, [property: JsonRequired] long ExpectedVersion,
    [property: JsonRequired] string ExpectedHead, [property: JsonRequired] long TargetEpoch,
    [property: JsonRequired] HouseholdPublicKeys TargetOwnerKeys, [property: JsonRequired] HouseholdPublicKeys RecoveryKeys,
    [property: JsonRequired] string RootCiphertextDigest, [property: JsonRequired] long ValidUntilUnixSeconds);

/// <summary>Opaque current-owner archive staging header, kept separate from financial rounds and their cursors.</summary>
public sealed record HouseholdOpenRecoverySeed([property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] string SignedDeclaration, [property: JsonRequired] string RootCiphertext);

/// <summary>Finite exact upload prefix; old original key frames never need to be resealed after an unobserved successful response.</summary>
public sealed record HouseholdRecoverySeedProgress(Guid OperationId, long ThroughEpoch, long UploadedThroughEpoch,
    bool Complete, long StoredBytes);

/// <summary>One bounded native archive page, not an unbounded all-history request.</summary>
public sealed record HouseholdUploadRecoveryEpochs([property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] HouseholdRecoveryEpochGrant[] Epochs);

/// <summary>Opaque recovery discovery for the actual signed-in owner; possession of this response never unlocks a household.</summary>
public sealed record HouseholdRecoveryObservation(Guid EnrollmentId, string SignedEnrollment,
    long MembershipVersion, string RootCiphertext, string RootDigest, long ThroughEpoch,
    long PublicationCursor = 0, string PublicationHead = "0000000000000000000000000000000000000000000000000000000000000000");

/// <summary>Finite original encrypted recovery-key page; clients verify each grant against their pinned complete membership history.</summary>
public sealed record HouseholdRecoveryEpochPage(Guid EnrollmentId, long ThroughEpoch,
    HouseholdRecoveryEpochGrant[] Epochs, long NextEpoch, bool Complete);

/// <summary>Owner recovery also proves possession of the original pending device bound to the actual authenticated session.</summary>
public sealed record HouseholdRecoverOwner([property: JsonRequired] HouseholdPublishMembership Publication,
    [property: JsonRequired] string SignedDevicePossession);

/// <summary>Separate current-root delegation, never a financial epoch grant and never delivered to an editor/viewer identity.</summary>
public sealed record HouseholdOwnerRootGrant([property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] long MembershipVersion, [property: JsonRequired] string RootFingerprint,
    [property: JsonRequired] string Ciphertext, [property: JsonRequired] string Digest);

/// <summary>Original public kit anchor retained locally after recovery; it carries neither private kit material nor current service authority.</summary>
public sealed record HouseholdRecoveryAnchor([property: JsonRequired] int Format, [property: JsonRequired] string Kind,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid EnrollmentId, [property: JsonRequired] string OwnerId,
    [property: JsonRequired] HouseholdPublicKeys KitKeys, [property: JsonRequired] HouseholdPublicKeys InitialOwnerRoot);
