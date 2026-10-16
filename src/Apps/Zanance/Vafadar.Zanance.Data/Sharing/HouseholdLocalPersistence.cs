using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Device-local captured household authority; all these tables are excluded from portable exports.</summary>
internal sealed class HouseholdLocalBinding
{
    /// <summary>One household identity per dedicated local financial profile.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Original stable creation/join operation, persisted before its first remote request.</summary>
    public Guid SetupOperation { get; set; }
    /// <summary>Exact protected actual file, invalidated by portable restore or different generation publication.</summary>
    public string GenerationPath { get; set; } = "";
    /// <summary>Reviewed endpoint/certificate/actual identity digest, without an authentication credential.</summary>
    public string PeerKey { get; set; } = "";
    /// <summary>Actual maintained identity; another login cannot adopt this local queue.</summary>
    public string IdentityId { get; set; } = "";
    /// <summary>Owner-creation versus independently confirmed invitation bootstrap.</summary>
    public bool OwnerCreation { get; set; }
    /// <summary>Server-assigned actual room; empty only while the original draft reservation is unresolved.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Server-assigned actual device; empty only while original reservation/claim is unresolved.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Original independently confirmed root signing public key, never silently replaced by a server directory.</summary>
    public string PinnedSigningKey { get; set; } = "";
    /// <summary>Original jointly confirmed root recipient public key.</summary>
    public string PinnedWrappingKey { get; set; } = "";
    /// <summary>Original signed invitation, without storing its capability as a URL or logging it.</summary>
    public string? SignedInvitation { get; set; }
    /// <summary>Original device's public fingerprint; private material is separately held in the protected secret table.</summary>
    public string DeviceFingerprint { get; set; } = "";
    /// <summary>Last fully verified signed membership generation.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Exact last verified signed membership head.</summary>
    public string MembershipHead { get; set; } = new('0', 64);
    /// <summary>Last completely verified atomic ciphertext publication.</summary>
    public long Cursor { get; set; }
    /// <summary>Last complete original financial row sequence, independent of ciphertext attachment piece count.</summary>
    public long FinancialCursor { get; set; }
    /// <summary>Exact complete author-signed publication head.</summary>
    public string PublicationHead { get; set; } = new('0', 64);
    /// <summary>Remote revocation/explicit local pause stops outgoing work without pretending prior downloaded copies can be recalled.</summary>
    public bool SendingDisabled { get; set; }
    /// <summary>Original explicit one-time source-copy review; it is not a live personal/household link.</summary>
    public Guid? ProjectionReviewId { get; set; }
    /// <summary>Local-only reviewed source content binding, never sent to the service.</summary>
    public string? ProjectionSourceDigest { get; set; }
    /// <summary>Complete original copied destination graph at its atomic creation boundary.</summary>
    public string? ProjectionContentHash { get; set; }
    /// <summary>Bounded original creation counts, retained for cold setup continuation without reconstructing missing consent.</summary>
    public string? ProjectionReceiptJson { get; set; }
    /// <summary>Actual authenticated host observation; missing metadata is never an offline write grant.</summary>
    public long? AccessObservedAtUnixSeconds { get; set; }
    /// <summary>Actual observed host expiry; null is meaningful only alongside a complete observation.</summary>
    public long? HostExpiresAtUnixSeconds { get; set; }
    /// <summary>Last authenticated host retention state; local time can only restrict its write authority.</summary>
    public Core.Sharing.HouseholdRetentionState AccessRetention { get; set; }
    /// <summary>Host freezes financial publication until the current signed rotation completes.</summary>
    public bool RotationRequired { get; set; }
    /// <summary>Authenticated scope refusal is separate from an explicit local sending pause.</summary>
    public bool RemoteAccessRefused { get; set; }
}

/// <summary>Bounded private frame, written/read only on an actual captured protected native factory connection.</summary>
internal sealed class HouseholdLocalSecret
{
    /// <summary>Fixed device/root/epoch/pending-invitation slot, never an arbitrary filesystem path.</summary>
    public string Slot { get; set; } = "";
    /// <summary>Exact private key/epoch/capability bytes inside the protected local database only.</summary>
    public byte[] Frame { get; set; } = [];
}

/// <summary>Original complete public proof chain, locally pinned and validated before any key/financial use.</summary>
internal sealed class HouseholdLocalMembership
{
    /// <summary>Exact strictly increasing roster version.</summary>
    public long Version { get; set; }
    /// <summary>Complete original standard public evidence, not an advertised mutable directory key.</summary>
    public string ProofJson { get; set; } = "";
    /// <summary>Exact signed statement hash for altered-replay/corruption refusal.</summary>
    public string Hash { get; set; } = "";
}

/// <summary>Original complete received ciphertext transaction; no partially received packet can advance the local financial cursor.</summary>
internal sealed class HouseholdLocalPacket
{
    /// <summary>Exact original atomic publication cursor.</summary>
    public long Cursor { get; set; }
    /// <summary>Complete author-signed manifest/receipt, not a row-count substitute.</summary>
    public string ReceiptJson { get; set; } = "";
    /// <summary>Exact author-signed completion head.</summary>
    public string Hash { get; set; } = "";
}

/// <summary>One retained original ciphertext event; its complete signature/manifest is checked before graph application.</summary>
internal sealed class HouseholdLocalEvent
{
    /// <summary>Exact original atomic packet.</summary>
    public long Cursor { get; set; }
    /// <summary>Exact signed part order, independent of feed's public address ordering.</summary>
    public int Index { get; set; }
    /// <summary>Complete original bounded encrypted event with public routing metadata.</summary>
    public string EventJson { get; set; } = "";
    /// <summary>Exact previously authenticated opaque address; prefix checks never need all ciphertext strings in memory.</summary>
    public string RecordId { get; set; } = "";
    /// <summary>Exact previously authenticated original opaque predecessor.</summary>
    public long BaseVersion { get; set; }
    /// <summary>Exact previously authenticated ciphertext digest, matched to the original author-signed manifest.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Exact canonical stored UTF8 length for bounded staging accounting without repeatedly reading old ciphertext.</summary>
    public int SerializedBytes { get; set; }
}

/// <summary>Derived protected pointer to a completely verified immutable attachment piece, never clear attachment bytes or a service-provided pointer.</summary>
internal sealed class HouseholdLocalBlobReference
{
    /// <summary>Original private complete-content SHA256.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Original128KiB piece index.</summary>
    public int PieceIndex { get; set; }
    /// <summary>Original complete declared content length.</summary>
    public long Length { get; set; }
    /// <summary>Actually verified original atomic publication containing this piece.</summary>
    public long Cursor { get; set; }
    /// <summary>Exact original signed ciphertext part index.</summary>
    public int EventIndex { get; set; }
    /// <summary>Negative one for a standalone piece, otherwise its exact original bounded batch position.</summary>
    public int ElementIndex { get; set; }
    /// <summary>Original verified piece digest, checked after reauthentication/decryption.</summary>
    public string PieceHash { get; set; } = "";
}

/// <summary>One immutable original outbound ciphertext plan, persisted before any upload or acknowledgement.</summary>
internal sealed class HouseholdLocalAttempt
{
    /// <summary>One captured original local financial round at a time.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Stable original financial operation, never regenerated after a lost committed response.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Distinct CAS attempt for that same logical operation.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Exact original local financial cutoff.</summary>
    public long Cutoff { get; set; }
    /// <summary>Exact original financial capture digest.</summary>
    public string CaptureHash { get; set; } = "";
    /// <summary>Complete exact original public open request.</summary>
    public string OpenJson { get; set; } = "";
    /// <summary>Original complete author-signed completion, never re-encrypted/re-signed after an acknowledged attempt.</summary>
    public string CommitJson { get; set; } = "";
    /// <summary>Complete financial row sequence represented by this prepared attempt.</summary>
    public long FinancialCursor { get; set; }
    /// <summary>Original immutable ciphertext part count; it is not proof of an acknowledged prefix.</summary>
    public int Parts { get; set; }
    /// <summary>Exact original server staging id after its actual acknowledgement.</summary>
    public Guid? RemoteId { get; set; }
    /// <summary>True only after the original signed completion was actually acknowledged/recovered.</summary>
    public bool Committed { get; set; }
    /// <summary>Explicitly acknowledged abandonment before another CAS attempt can replace this plan.</summary>
    public bool Abandoned { get; set; }
}

/// <summary>Original outbound literal ciphertext bytes; staging retries cannot regenerate a successful message.</summary>
internal sealed class HouseholdLocalAttemptPart
{
    /// <summary>Exact canonical original part order.</summary>
    public int Index { get; set; }
    /// <summary>Complete original opaque part, including original address/base version/digest.</summary>
    public string PartJson { get; set; } = "";
    /// <summary>Original immutable signed-manifest metadata, available without loading all ciphertext bodies.</summary>
    public string RecordId { get; set; } = "";
    /// <summary>Original immutable opaque predecessor.</summary>
    public long BaseVersion { get; set; }
    /// <summary>Original exact encoded ciphertext digest.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Original encoded ciphertext length, checked again before bounded upload.</summary>
    public int CiphertextBytes { get; set; }
    /// <summary>Original canonical serialized length, checked before bounded upload.</summary>
    public int SerializedBytes { get; set; }
}

/// <summary>Original complete membership request, retained before network publication so signatures/encrypted grants are never regenerated after a lost acknowledgement.</summary>
internal sealed class HouseholdLocalMembershipProposal
{
    /// <summary>One explicitly prepared root transition at a time.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Original signed transition operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Exact original predecessor roster.</summary>
    public long ExpectedVersion { get; set; }
    /// <summary>Complete public proof plus standard encrypted recipient grants; no clear private keys.</summary>
    public string BodyJson { get; set; } = "";
    /// <summary>Exact immutable request identity, not a substitute for matching its complete original bytes.</summary>
    public string Hash { get; set; } = "";
}

/// <summary>Finite ordinary metadata tables; no new financial table or user field is silently included in sync/export.</summary>
internal sealed class HouseholdLocalModel : IEntityTypeConfiguration<HouseholdLocalBinding>, IEntityTypeConfiguration<HouseholdLocalSecret>,
    IEntityTypeConfiguration<HouseholdLocalMembership>, IEntityTypeConfiguration<HouseholdLocalPacket>, IEntityTypeConfiguration<HouseholdLocalEvent>,
    IEntityTypeConfiguration<HouseholdLocalAttempt>, IEntityTypeConfiguration<HouseholdLocalAttemptPart>, IEntityTypeConfiguration<HouseholdLocalMembershipProposal>,
    IEntityTypeConfiguration<HouseholdLocalBlobReference>, IEntityTypeConfiguration<HouseholdLocalFinancialReference>
{
    /// <summary>Exact scrub order, children before captured authority. Device-only automatic copies use separate existing protection.</summary>
    internal static IReadOnlyList<string> PortableTables { get; } = ["HouseholdLocalFinancialReferences", "HouseholdLocalOwnership", "HouseholdLocalMembershipProposals", "HouseholdLocalAttemptParts", "HouseholdLocalAttempts", "HouseholdLocalBlobReferences", "HouseholdLocalEvents",
        "HouseholdLocalPackets", "HouseholdLocalMemberships", "HouseholdLocalSecrets", "HouseholdLocalBinding"];
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalBinding> model)
    {
        model.ToTable("HouseholdLocalBinding", table => table.HasCheckConstraint("CK_HouseholdLocalBinding_Singleton", "\"Id\" = 1"));
        model.HasKey(row => row.Id); model.Property(row => row.Id).ValueGeneratedNever();
        model.Property(row => row.GenerationPath).HasMaxLength(4096); model.Property(row => row.PeerKey).HasMaxLength(64);
        model.Property(row => row.IdentityId).HasMaxLength(128); model.Property(row => row.PinnedSigningKey).HasMaxLength(512);
        model.Property(row => row.PinnedWrappingKey).HasMaxLength(2048); model.Property(row => row.SignedInvitation).HasMaxLength(131072);
        model.Property(row => row.DeviceFingerprint).HasMaxLength(64); model.Property(row => row.MembershipHead).HasMaxLength(64);
        model.Property(row => row.PublicationHead).HasMaxLength(64);
        model.Property(row => row.ProjectionSourceDigest).HasMaxLength(64); model.Property(row => row.ProjectionContentHash).HasMaxLength(64);
        model.Property(row => row.ProjectionReceiptJson).HasMaxLength(4096);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalSecret> model)
    {
        model.ToTable("HouseholdLocalSecrets", table => table.HasCheckConstraint("CK_HouseholdLocalSecret_FrameBound",
            "length(\"Frame\") BETWEEN 1 AND CASE WHEN \"Slot\" IN ('pending-recovery-preparation','pending-owner-recovery') THEN 131072 ELSE 8192 END"));
        model.HasKey(row => row.Slot); model.Property(row => row.Slot).HasMaxLength(96);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalMembership> model)
    {
        model.ToTable("HouseholdLocalMemberships"); model.HasKey(row => row.Version);
        model.Property(row => row.Version).ValueGeneratedNever(); model.Property(row => row.Hash).HasMaxLength(64); model.Property(row => row.ProofJson).HasMaxLength(524288);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalPacket> model)
    {
        model.ToTable("HouseholdLocalPackets"); model.HasKey(row => row.Cursor); model.Property(row => row.Cursor).ValueGeneratedNever();
        model.Property(row => row.ReceiptJson).HasMaxLength(Core.Sharing.HouseholdPublicationDigest.MaximumReceiptBytes); model.Property(row => row.Hash).HasMaxLength(64);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalEvent> model)
    {
        model.HasIndex(row => new { row.RecordId, row.Cursor });
        model.ToTable("HouseholdLocalEvents"); model.HasKey(row => new { row.Cursor, row.Index }); model.Property(row => row.EventJson).HasMaxLength(2 * 1024 * 1024);
        model.Property(row => row.RecordId).HasMaxLength(64).HasDefaultValue(""); model.Property(row => row.Digest).HasMaxLength(64).HasDefaultValue("");
        model.Property(row => row.BaseVersion).HasDefaultValue(0L); model.Property(row => row.SerializedBytes).HasDefaultValue(0);
        model.HasOne<HouseholdLocalPacket>().WithMany().HasForeignKey(row => row.Cursor).OnDelete(DeleteBehavior.Cascade);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalAttempt> model)
    {
        model.ToTable("HouseholdLocalAttempts", table => table.HasCheckConstraint("CK_HouseholdLocalAttempt_Singleton", "\"Id\" = 1"));
        model.HasKey(row => row.Id); model.Property(row => row.Id).ValueGeneratedNever(); model.Property(row => row.CaptureHash).HasMaxLength(64);
        model.Property(row => row.OpenJson).HasMaxLength(4096); model.Property(row => row.CommitJson).HasMaxLength(131072);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalAttemptPart> model)
    {
        model.ToTable("HouseholdLocalAttemptParts"); model.HasKey(row => row.Index); model.Property(row => row.Index).ValueGeneratedNever();
        model.Property(row => row.PartJson).HasMaxLength(2 * 1024 * 1024);
        model.Property(row => row.RecordId).HasMaxLength(64).HasDefaultValue(""); model.Property(row => row.Digest).HasMaxLength(64).HasDefaultValue("");
        model.Property(row => row.BaseVersion).HasDefaultValue(0L); model.Property(row => row.CiphertextBytes).HasDefaultValue(0);
        model.Property(row => row.SerializedBytes).HasDefaultValue(0);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalMembershipProposal> model)
    {
        model.ToTable("HouseholdLocalMembershipProposals", table => table.HasCheckConstraint("CK_HouseholdMembershipProposal_Singleton", "\"Id\" = 1"));
        model.HasKey(row => row.Id); model.Property(row => row.Id).ValueGeneratedNever(); model.Property(row => row.BodyJson).HasMaxLength(8 * 1024 * 1024);
        model.Property(row => row.Hash).HasMaxLength(64);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalFinancialReference> model)
    {
        model.ToTable("HouseholdLocalFinancialReferences", table => table.HasCheckConstraint("CK_HouseholdFinancialReference_Position",
            "\"Version\" > 0 AND \"Cursor\" > 0 AND \"EventIndex\" >= 0 AND ((\"Offset\" = -1 AND \"Length\" = 0) OR (\"Offset\" >= 4 AND \"Length\" BETWEEN 1 AND 16777216))"));
        model.HasKey(row => row.Version); model.Property(row => row.Version).ValueGeneratedNever();
        model.Property(row => row.Table).HasMaxLength(96); model.Property(row => row.Key).HasMaxLength(2048);
        model.HasOne<HouseholdLocalPacket>().WithMany().HasForeignKey(row => row.Cursor).OnDelete(DeleteBehavior.Restrict);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalBlobReference> model)
    {
        model.ToTable("HouseholdLocalBlobReferences", table => table.HasCheckConstraint("CK_HouseholdLocalBlobReference_Position",
            "\"Cursor\" > 0 AND \"Length\" > 0 AND \"PieceIndex\" >= 0 AND \"EventIndex\" >= 0 AND \"ElementIndex\" BETWEEN -1 AND 511"));
        model.HasKey(row => new { row.Hash, row.PieceIndex }); model.Property(row => row.Hash).HasMaxLength(64); model.Property(row => row.PieceHash).HasMaxLength(64);
        model.HasOne<HouseholdLocalPacket>().WithMany().HasForeignKey(row => row.Cursor).OnDelete(DeleteBehavior.Restrict);
        model.HasIndex(row => new { row.Cursor, row.EventIndex });
    }
}
