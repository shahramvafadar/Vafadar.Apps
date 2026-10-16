using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Independent shared-space authority; financial bodies and device/epoch private keys are never service columns.</summary>
public sealed partial class HouseholdSpaceRow
{
    /// <summary>Server-created random resource scope.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual current service identity owner, independent of device count.</summary>
    public string OwnerId { get; set; } = "";
    /// <summary>Exact negotiated financial contract.</summary>
    public string SchemaHash { get; set; } = "";
    /// <summary>Idempotent creation identity, scoped to the actual owner.</summary>
    public Guid CreationOperationId { get; set; }
    /// <summary>Public owner-root signing SPKI, confirmed through client continuity/invitation authority.</summary>
    public string OwnerSigningKey { get; set; } = "";
    /// <summary>Public owner-root recipient SPKI; its joint fingerprint binds the public identity.</summary>
    public string OwnerWrappingKey { get; set; } = "";
    /// <summary>Current signed membership sequence.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Digest of the immutable current signed membership event.</summary>
    public string MembershipHead { get; set; } = "";
    /// <summary>Current encryption generation, incremented for removals and ownership changes.</summary>
    public long KeyEpoch { get; set; }
    /// <summary>Current atomic publication cursor, unrelated to financial dates.</summary>
    public long Cursor { get; set; }
    /// <summary>Digest of the last author-signed publication receipt, preventing silent local checkpoint rollback.</summary>
    public string PublicationHead { get; set; } = "";
    /// <summary>Verified server subscription expiry; clients cannot set or extend it through sharing requests.</summary>
    public DateTimeOffset? HostExpiresAt { get; set; }
    /// <summary>Immediate permanent access revocation before durable storage retirement.</summary>
    public bool Deleted { get; set; }
    /// <summary>Completed physical retirement; a pending deletion is resumed, never resurrected.</summary>
    public bool StorageRetired { get; set; }
    /// <summary>Explicit security freeze until a required signed epoch rotation completes.</summary>
    public bool RotationRequired { get; set; }
    /// <summary>Original creation instant used only for operations.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Accepted identity role, separate from pending invitations and cryptographic device approval.</summary>
public sealed class HouseholdMemberRow
{
    /// <summary>Exact shared scope.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Actual registered identity, never request-body email authority.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Current mirrored owner-signed role.</summary>
    public HouseholdRole Role { get; set; }
    /// <summary>One-use invitation acceptance, never inferred from knowing a space id.</summary>
    public bool Accepted { get; set; }
    /// <summary>Security revocation overrides paid rights and every retention state.</summary>
    public bool Revoked { get; set; }
    /// <summary>Original signed membership sequence at acceptance.</summary>
    public long JoinedVersion { get; set; }
}

/// <summary>Room-scoped public device identity; a successful login alone never approves a new key.</summary>
public sealed class HouseholdDeviceRow
{
    /// <summary>Server-created room-scoped device resource.</summary>
    public Guid Id { get; set; }
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Actual accepted member identity.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Current authenticated session; rebind requires proof with this approved private key.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Joint public signing/recipient fingerprint.</summary>
    public string Fingerprint { get; set; } = "";
    /// <summary>Public ES256 SPKI.</summary>
    public string SigningKey { get; set; } = "";
    /// <summary>Public RSA3072 SPKI.</summary>
    public string WrappingKey { get; set; } = "";
    /// <summary>Explicit owner-signed approval, not an identity-server key-directory assertion.</summary>
    public bool Approved { get; set; }
    /// <summary>Immediate device revocation.</summary>
    public bool Revoked { get; set; }
    /// <summary>Public last-use instant, never a financial timestamp.</summary>
    public DateTimeOffset LastSeenAt { get; set; }
    /// <summary>Original self-signed pending-device enrollment proof; it never supplies owner approval.</summary>
    public string? AdmissionProof { get; set; }
    /// <summary>Original enrollment operation for exact lost-acknowledgement retry.</summary>
    public Guid? AdmissionOperationId { get; set; }
    /// <summary>Most recent approved-key session-possession proof, independent of an account password reset.</summary>
    public string? SessionProof { get; set; }
    /// <summary>Most recent exact approved-key rebind operation.</summary>
    public Guid? SessionOperationId { get; set; }
}

/// <summary>Single-use expiring reservation; its possession cannot itself decrypt or read financial content.</summary>
public sealed class HouseholdInvitationRow
{
    /// <summary>Random public reference, not the secret invitation capability.</summary>
    public Guid Id { get; set; }
    /// <summary>Exact shared scope.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Owner-selected editor/viewer role; invitations cannot silently create a second owner.</summary>
    public HouseholdRole Role { get; set; }
    /// <summary>Digest only of an independent high-entropy one-use invitation capability.</summary>
    public string CapabilityHash { get; set; } = "";
    /// <summary>Exclusive expiry from injected operational time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Identity that explicitly claimed the invitation; changing it requires a new invitation.</summary>
    public string? ClaimedUserId { get; set; }
    /// <summary>Joint public device fingerprint to be independently confirmed by the owner.</summary>
    public string? ClaimedFingerprint { get; set; }
    /// <summary>True only after explicit two-sided acceptance and signed membership/key publication.</summary>
    public bool Accepted { get; set; }
    /// <summary>Withdrawn/declined invitation no longer reserves a seat or accepts a claim.</summary>
    public bool Revoked { get; set; }
    /// <summary>Idempotent owner request.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Immutable root-signed invitation; the actual claim binds its complete digest.</summary>
    public string SignedInvitation { get; set; } = "";
    /// <summary>Immutable claimant-signed consent, never inferred from owner metadata or knowledge of an email.</summary>
    public string? SignedClaim { get; set; }
    /// <summary>Original claim operation, preserving a lost-acknowledgement retry without accepting a second claimant.</summary>
    public Guid? ClaimOperationId { get; set; }
    /// <summary>Exact server-created pending device, approved only by the later signed membership transition.</summary>
    public Guid? ClaimedDeviceId { get; set; }
}

/// <summary>Immutable public membership/key-continuity event; contains no household financial plaintext.</summary>
public sealed partial class HouseholdMembershipEventRow
{
    /// <summary>Exact space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Strictly increasing signed generation.</summary>
    public long Version { get; set; }
    /// <summary>Digest of the preceding signed event, or the explicit genesis marker.</summary>
    public string PreviousHash { get; set; } = "";
    /// <summary>Current event digest, retained for exact replay/checkpoint comparison.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Standard ES256 owner-root-signed membership metadata.</summary>
    public string SignedEvent { get; set; } = "";
    /// <summary>Exact public root that verified this event, retained across a legitimate owner-root transfer.</summary>
    public string SignerSigningKey { get; set; } = "";
    /// <summary>Joint public identity paired with the original signing root; never the current directory's replacement.</summary>
    public string SignerWrappingKey { get; set; } = "";
    /// <summary>Complete membership-request digest, including exact encrypted grant inventory, for altered-retry refusal.</summary>
    public string RequestHash { get; set; } = "";
    /// <summary>Complete public encrypted-grant inventory digest independently bound by transfer acceptance.</summary>
    public string GrantsHash { get; set; } = "";
    /// <summary>Target-root signature for an explicitly accepted ownership transfer, otherwise absent.</summary>
    public string? TransferAcceptance { get; set; }
    /// <summary>New member's exact private-key consent proof bound by the root-signed member acceptance hash.</summary>
    public string? SignedAcceptanceClaim { get; set; }
    /// <summary>Actual initiating identity, not the event's untrusted display labels.</summary>
    public string ActorId { get; set; } = "";
    /// <summary>Idempotent operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Operational audit time.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>One encrypted epoch grant to an approved recipient; no server-readable group/address/private key material.</summary>
public sealed class HouseholdKeyGrantRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Encryption generation.</summary>
    public long Epoch { get; set; }
    /// <summary>Exact approved recipient device.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Owner-signed generation authorizing this grant.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Standard recipient-encrypted and owner-signed key package.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Complete encrypted-byte integrity identity for altered-retry refusal.</summary>
    public string Digest { get; set; } = "";
}

/// <summary>Durable unpublished/committed opaque transaction; a request never contains financial SQL or clear rows.</summary>
public sealed class HouseholdRoundRow
{
    /// <summary>Server-created staging resource.</summary>
    public Guid Id { get; set; }
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Actual approved author.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Idempotent client operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Distinct immutable staging attempt; CAS retries keep the logical operation but use a fresh attempt.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Complete observed predecessor cursor; publication is compare-and-swap, never last-writer-wins.</summary>
    public long ExpectedCursor { get; set; }
    /// <summary>Observed signed membership, rechecked at publication.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Observed encryption generation, rechecked at publication.</summary>
    public long KeyEpoch { get; set; }
    /// <summary>Complete encrypted-part digest declared and verified at commit.</summary>
    public string ContentHash { get; set; } = "";
    /// <summary>Actual committed cursor; null while unpublished.</summary>
    public long? CommittedCursor { get; set; }
    /// <summary>Immutable author-signed publication metadata, without clear financial values.</summary>
    public string? SignedCommit { get; set; }
    /// <summary>Explicit abandoned staging work; its ciphertext may be reclaimed independently of live state.</summary>
    public bool Abandoned { get; set; }
    /// <summary>Client durably applied the acknowledged feed, permitting bounded historical reclamation.</summary>
    public bool Acknowledged { get; set; }
    /// <summary>Creation instant for bounded staging maintenance.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Last original authenticated use, not proof that financial work completed.</summary>
    public DateTimeOffset LastSeenAt { get; set; }
}

/// <summary>One encrypted immutable staging record; its address is keyed on the client, hiding table/key guesses.</summary>
public sealed class HouseholdPartRow
{
    /// <summary>Actual staging resource.</summary>
    public Guid RoundId { get; set; }
    /// <summary>Original canonical ordered part number.</summary>
    public int Index { get; set; }
    /// <summary>Client-keyed opaque address, never a server-visible financial table/key.</summary>
    public string RecordId { get; set; } = "";
    /// <summary>Actual prior version of this opaque record.</summary>
    public long BaseVersion { get; set; }
    /// <summary>Standard encrypted signed row/tombstone/blob-piece payload.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Complete encrypted-byte digest for idempotency and storage-integrity refusal.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Bounded actual encoded length, counted inside the owner/space transaction.</summary>
    public int Bytes { get; set; }
}

/// <summary>Current opaque record; a client verifies complete decrypted financial semantics before local application.</summary>
public sealed class HouseholdRecordRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Client-keyed opaque identity.</summary>
    public string RecordId { get; set; } = "";
    /// <summary>Atomic global publication version.</summary>
    public long Version { get; set; }
    /// <summary>Encryption generation.</summary>
    public long KeyEpoch { get; set; }
    /// <summary>Signed membership generation at publication.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Actual authorized signing device.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Original signed operation identity.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Encrypted complete content/tombstone.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Complete encrypted-byte digest.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Actual bounded encoded length.</summary>
    public int Bytes { get; set; }
}

/// <summary>Immutable opaque feed history for actual clients resuming original acknowledged work.</summary>
public sealed class HouseholdEventRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Atomic publication cursor.</summary>
    public long Cursor { get; set; }
    /// <summary>Client-keyed record identity, ordered inside this cursor.</summary>
    public string RecordId { get; set; } = "";
    /// <summary>The original staging authority retaining author/epoch and signed commit.</summary>
    public Guid RoundId { get; set; }
    /// <summary>Original ordered position bound by the signed completion manifest.</summary>
    public int Index { get; set; }
    /// <summary>Original opaque predecessor version, also bound by the signed manifest.</summary>
    public long BaseVersion { get; set; }
    /// <summary>Original encrypted complete content.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Exact encrypted-byte digest.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Actual encoded length for independent history bounds.</summary>
    public int Bytes { get; set; }
}

/// <summary>Optional owner recovery encrypted for a separately held recovery recipient, never a password or clear private key.</summary>
public sealed partial class HouseholdRecoveryRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Current explicit recovery enrollment identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Current owner, checked independently of an incoming device-key claim.</summary>
    public string OwnerId { get; set; } = "";
    /// <summary>Public recovery recipient signing identity, never its private kit/key.</summary>
    public string SigningKey { get; set; } = "";
    /// <summary>Public recovery RSA recipient, allowing encrypted updates without remembering a recovery secret.</summary>
    public string WrappingKey { get; set; } = "";
    /// <summary>Current owner-signed generation authorizing this encrypted package.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Recipient-encrypted package; only the owner-held recovery kit can open it.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Complete encrypted-byte digest for retry/integrity checks.</summary>
    public string Digest { get; set; } = "";
}

/// <summary>Maps finite service metadata/ciphertext without interpreting financial content or storing client decryption keys.</summary>
internal static partial class HouseholdServiceModel
{
    /// <summary>Extends the independent PostgreSQL service only; migrations/compiled model follow the complete batch model.</summary>
    internal static void Configure(ModelBuilder model)
    {
        var spaces = model.Entity<HouseholdSpaceRow>(); spaces.ToTable("HouseholdSpaces"); spaces.HasKey(row => row.Id);
        spaces.Property(row => row.OwnerId).HasMaxLength(450); spaces.Property(row => row.SchemaHash).HasMaxLength(64);
        spaces.Property(row => row.OwnerSigningKey).HasMaxLength(512); spaces.Property(row => row.OwnerWrappingKey).HasMaxLength(2048);
        spaces.Property(row => row.MembershipHead).HasMaxLength(64); spaces.Property(row => row.PublicationHead).HasMaxLength(64);
        spaces.HasOne<ServiceUser>().WithMany().HasForeignKey(row => row.OwnerId).OnDelete(DeleteBehavior.Restrict);
        spaces.HasIndex(row => new { row.OwnerId, row.CreationOperationId }).IsUnique();
        // Cross-room ownership transfer cannot take another identity lock in reverse order. This
        // immediate database constraint closes a concurrent reserve/transfer race for the same new owner.
        spaces.HasIndex(row => row.OwnerId).IsUnique().HasFilter("NOT \"Deleted\"");
        var members = model.Entity<HouseholdMemberRow>(); members.ToTable("HouseholdMembers"); members.HasKey(row => new { row.SpaceId, row.UserId });
        members.Property(row => row.UserId).HasMaxLength(450);
        members.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        members.HasOne<ServiceUser>().WithMany().HasForeignKey(row => row.UserId).OnDelete(DeleteBehavior.Restrict);
        members.HasIndex(row => new { row.SpaceId, row.Role }).IsUnique().HasFilter("\"Role\" = 3 AND NOT \"Revoked\"");
        var devices = model.Entity<HouseholdDeviceRow>(); devices.ToTable("HouseholdDevices"); devices.HasKey(row => row.Id);
        devices.Property(row => row.UserId).HasMaxLength(450); devices.Property(row => row.Fingerprint).HasMaxLength(64);
        devices.Property(row => row.SigningKey).HasMaxLength(512); devices.Property(row => row.WrappingKey).HasMaxLength(2048);
        devices.Property(row => row.AdmissionProof).HasMaxLength(65536); devices.Property(row => row.SessionProof).HasMaxLength(65536);
        devices.HasOne<HouseholdMemberRow>().WithMany().HasForeignKey(row => new { row.SpaceId, row.UserId }).OnDelete(DeleteBehavior.Restrict);
        devices.HasIndex(row => new { row.SpaceId, row.UserId, row.Fingerprint }).IsUnique();
        var invitations = model.Entity<HouseholdInvitationRow>(); invitations.ToTable("HouseholdInvitations"); invitations.HasKey(row => row.Id);
        invitations.Property(row => row.CapabilityHash).HasMaxLength(64); invitations.Property(row => row.ClaimedFingerprint).HasMaxLength(64);
        invitations.Property(row => row.ClaimedUserId).HasMaxLength(450);
        invitations.Property(row => row.SignedInvitation).HasMaxLength(65536); invitations.Property(row => row.SignedClaim).HasMaxLength(65536);
        invitations.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        invitations.HasIndex(row => new { row.SpaceId, row.OperationId }).IsUnique();
        var membership = model.Entity<HouseholdMembershipEventRow>(); membership.ToTable("HouseholdMembershipEvents"); membership.HasKey(row => new { row.SpaceId, row.Version });
        membership.Property(row => row.PreviousHash).HasMaxLength(64); membership.Property(row => row.Hash).HasMaxLength(64);
        membership.Property(row => row.RequestHash).HasMaxLength(64);
        membership.Property(row => row.GrantsHash).HasMaxLength(64);
        membership.Property(row => row.SignerSigningKey).HasMaxLength(512); membership.Property(row => row.SignerWrappingKey).HasMaxLength(2048);
        membership.Property(row => row.ActorId).HasMaxLength(450); membership.Property(row => row.SignedEvent).HasMaxLength(65536);
        membership.Property(row => row.TransferAcceptance).HasMaxLength(65536);
        membership.Property(row => row.SignedAcceptanceClaim).HasMaxLength(65536);
        membership.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        membership.HasIndex(row => new { row.SpaceId, row.OperationId }).IsUnique();
        var grants = model.Entity<HouseholdKeyGrantRow>(); grants.ToTable("HouseholdKeyGrants"); grants.HasKey(row => new { row.SpaceId, row.Epoch, row.DeviceId });
        grants.Property(row => row.Ciphertext).HasMaxLength(32768); grants.Property(row => row.Digest).HasMaxLength(64);
        grants.HasOne<HouseholdDeviceRow>().WithMany().HasForeignKey(row => row.DeviceId).OnDelete(DeleteBehavior.Restrict);
        grants.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        var rounds = model.Entity<HouseholdRoundRow>(); rounds.ToTable("HouseholdRounds"); rounds.HasKey(row => row.Id);
        rounds.Property(row => row.ContentHash).HasMaxLength(64); rounds.Property(row => row.SignedCommit).HasMaxLength(65536);
        rounds.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        rounds.HasOne<HouseholdDeviceRow>().WithMany().HasForeignKey(row => row.DeviceId).OnDelete(DeleteBehavior.Restrict);
        rounds.HasIndex(row => new { row.SpaceId, row.DeviceId, row.OperationId, row.AttemptId }).IsUnique();
        rounds.HasIndex(row => new { row.SpaceId, row.DeviceId, row.OperationId }).IsUnique().HasFilter("\"CommittedCursor\" IS NOT NULL");
        rounds.HasIndex(row => new { row.SpaceId, row.CommittedCursor });
        var parts = model.Entity<HouseholdPartRow>(); parts.ToTable("HouseholdParts"); parts.HasKey(row => new { row.RoundId, row.Index });
        parts.Property(row => row.RecordId).HasMaxLength(64); parts.Property(row => row.Digest).HasMaxLength(64); parts.Property(row => row.Ciphertext).HasMaxLength(1048576);
        parts.HasOne<HouseholdRoundRow>().WithMany().HasForeignKey(row => row.RoundId).OnDelete(DeleteBehavior.Cascade);
        parts.HasIndex(row => new { row.RoundId, row.RecordId }).IsUnique();
        var records = model.Entity<HouseholdRecordRow>(); records.ToTable("HouseholdRecords"); records.HasKey(row => new { row.SpaceId, row.RecordId });
        records.Property(row => row.RecordId).HasMaxLength(64); records.Property(row => row.Digest).HasMaxLength(64); records.Property(row => row.Ciphertext).HasMaxLength(1048576);
        records.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        var events = model.Entity<HouseholdEventRow>(); events.ToTable("HouseholdEvents"); events.HasKey(row => new { row.SpaceId, row.Cursor, row.RecordId });
        events.Property(row => row.RecordId).HasMaxLength(64); events.Property(row => row.Digest).HasMaxLength(64); events.Property(row => row.Ciphertext).HasMaxLength(1048576);
        events.HasOne<HouseholdRoundRow>().WithMany().HasForeignKey(row => row.RoundId).OnDelete(DeleteBehavior.Restrict);
        var recovery = model.Entity<HouseholdRecoveryRow>(); recovery.ToTable("HouseholdRecovery"); recovery.HasKey(row => row.SpaceId);
        recovery.Property(row => row.OwnerId).HasMaxLength(450); recovery.Property(row => row.SigningKey).HasMaxLength(512); recovery.Property(row => row.WrappingKey).HasMaxLength(2048);
        recovery.Property(row => row.Ciphertext).HasMaxLength(65536); recovery.Property(row => row.Digest).HasMaxLength(64);
        recovery.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRecoveryArchive(model);
    }
}
