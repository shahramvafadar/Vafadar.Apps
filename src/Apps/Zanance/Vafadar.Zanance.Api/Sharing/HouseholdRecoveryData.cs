using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Sharing;

public sealed partial class HouseholdMembershipEventRow
{
    /// <summary>Exact original public enrolled-kit consent for owner recovery; never a kit/private-key frame.</summary>
    public string? RecoveryEvidenceJson { get; set; }
}

public sealed partial class HouseholdRecoveryRow
{
    /// <summary>Current owner-root signed kit certificate; the directory fields alone never establish recovery authority.</summary>
    public string SignedEnrollment { get; set; } = "";
}

/// <summary>One immutable original recipient-encrypted epoch in the current explicitly enrolled owner's archive.</summary>
public sealed class HouseholdRecoveryEpochRow
{
    /// <summary>Exact room, independent of personal sync profiles.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Exact enrolled kit; replacement never reuses another kit's frames.</summary>
    public Guid EnrollmentId { get; set; }
    /// <summary>Original financial key epoch, never inferred from an array position.</summary>
    public long Epoch { get; set; }
    /// <summary>Verified membership root authorizing this explicit historical rewrap.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Standard recipient-encrypted key frame, opaque to the service.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Exact encrypted-byte digest for idempotent publication.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Actual bounded ASCII ciphertext length; never a client-provided storage counter.</summary>
    public int Bytes { get; set; }
}

/// <summary>One current-owner staged archive; it cannot publish membership or advance a financial cursor.</summary>
public sealed class HouseholdRecoverySeedRow
{
    /// <summary>Exact room under its original lock.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Original root-rotation operation, retained across a lost upload response.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Current approved owner device that declared this archive.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Original kit identity, fixed before its first encrypted frame.</summary>
    public Guid EnrollmentId { get; set; }
    /// <summary>Actual current owner identity captured under the service lock.</summary>
    public string OwnerId { get; set; } = "";
    /// <summary>Current root-signed complete public preparation declaration.</summary>
    public string SignedDeclaration { get; set; } = "";
    /// <summary>Exact complete request identity, not a financial-content digest.</summary>
    public string RequestHash { get; set; } = "";
    /// <summary>Original target root's standard kit-recipient envelope, opaque to the service.</summary>
    public string RootCiphertext { get; set; } = "";
    /// <summary>Exact committed old membership version required for later atomic promotion.</summary>
    public long ExpectedVersion { get; set; }
    /// <summary>Original old membership hash; a new roster cannot silently adopt uploaded recovery material.</summary>
    public string ExpectedHead { get; set; } = "";
    /// <summary>Finite exact key inventory expected before enrollment can commit.</summary>
    public long ThroughEpoch { get; set; }
    /// <summary>Contiguous durably uploaded prefix, rechecked against immutable keyed rows.</summary>
    public long UploadedThroughEpoch { get; set; }
    /// <summary>Actual staged root/epoch bytes; the service computes every increment.</summary>
    public long StoredBytes { get; set; }
    /// <summary>Operational creation instant from the injected clock.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Finite preparation lifetime; it never extends subscription or financial access.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>One exact staged key frame; its completed prefix is reusable without resealing after a process/network failure.</summary>
public sealed class HouseholdRecoverySeedEpochRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Original preparation operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Exact original epoch.</summary>
    public long Epoch { get; set; }
    /// <summary>Future independently signed root version which grants this original key.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Standard opaque recipient-encrypted key frame.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Exact encrypted bytes' digest.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Actual server-computed stored bytes.</summary>
    public int Bytes { get; set; }
}

/// <summary>One current owner-root delegation to an approved device of the owning identity, distinct from ordinary financial keys.</summary>
public sealed class HouseholdOwnerRootGrantRow
{
    /// <summary>Exact room.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Only an explicit device of the current owning identity.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Signed membership which authorized this exact recipient envelope.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Current independently verified joint root identity.</summary>
    public string RootFingerprint { get; set; } = "";
    /// <summary>Standard owner-device recipient envelope; never a clear root key.</summary>
    public string Ciphertext { get; set; } = "";
    /// <summary>Exact encrypted-byte digest.</summary>
    public string Digest { get; set; } = "";
}

internal static partial class HouseholdServiceModel
{
    private static void ConfigureRecoveryArchive(ModelBuilder model)
    {
        model.Entity<HouseholdMembershipEventRow>().Property(row => row.RecoveryEvidenceJson).HasMaxLength(16384);
        model.Entity<HouseholdRecoveryRow>().Property(row => row.SignedEnrollment).HasMaxLength(16384);
        var epoch = model.Entity<HouseholdRecoveryEpochRow>(); epoch.ToTable("HouseholdRecoveryEpochs");
        epoch.HasKey(row => new { row.SpaceId, row.EnrollmentId, row.Epoch });
        epoch.Property(row => row.Ciphertext).HasMaxLength(8192); epoch.Property(row => row.Digest).HasMaxLength(64);
        epoch.HasOne<HouseholdRecoveryRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Cascade);
        var seed = model.Entity<HouseholdRecoverySeedRow>(); seed.ToTable("HouseholdRecoverySeeds");
        seed.HasKey(row => new { row.SpaceId, row.OperationId }); seed.HasIndex(row => row.SpaceId).IsUnique();
        seed.Property(row => row.OwnerId).HasMaxLength(450); seed.Property(row => row.SignedDeclaration).HasMaxLength(16384);
        seed.Property(row => row.RequestHash).HasMaxLength(64); seed.Property(row => row.RootCiphertext).HasMaxLength(65536);
        seed.Property(row => row.ExpectedHead).HasMaxLength(64);
        seed.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        var staged = model.Entity<HouseholdRecoverySeedEpochRow>(); staged.ToTable("HouseholdRecoverySeedEpochs");
        staged.HasKey(row => new { row.SpaceId, row.OperationId, row.Epoch });
        staged.Property(row => row.Ciphertext).HasMaxLength(8192); staged.Property(row => row.Digest).HasMaxLength(64);
        staged.HasOne<HouseholdRecoverySeedRow>().WithMany().HasForeignKey(row => new { row.SpaceId, row.OperationId }).OnDelete(DeleteBehavior.Cascade);
        var roots = model.Entity<HouseholdOwnerRootGrantRow>(); roots.ToTable("HouseholdOwnerRootGrants");
        roots.HasKey(row => new { row.SpaceId, row.DeviceId });
        roots.Property(row => row.RootFingerprint).HasMaxLength(64); roots.Property(row => row.Digest).HasMaxLength(64);
        roots.Property(row => row.Ciphertext).HasMaxLength(65536);
        roots.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        roots.HasOne<HouseholdDeviceRow>().WithMany().HasForeignKey(row => row.DeviceId).OnDelete(DeleteBehavior.Restrict);
    }
}
