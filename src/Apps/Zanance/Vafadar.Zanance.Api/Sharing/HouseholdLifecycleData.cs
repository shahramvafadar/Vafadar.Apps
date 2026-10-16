using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

public sealed partial class HouseholdSpaceRow
{
    /// <summary>Strict room-locked operational event position.</summary>
    public long AuditCursor { get; set; }
    /// <summary>Finite operational metadata bytes, independent of financial payload storage.</summary>
    public long AuditBytes { get; set; }
    /// <summary>Original permanent deletion/expiry operation, independent of later session loss.</summary>
    public Guid? DeletionOperationId { get; set; }
    /// <summary>Literal original declaration commitment, without retaining its body after retirement.</summary>
    public string? DeletionRequestHash { get; set; }
    /// <summary>Opaque owning-identity comparison, not an email address.</summary>
    public string? DeletionActorHash { get; set; }
    /// <summary>Permanent privacy reason, never a reversible subscription pause.</summary>
    public HouseholdDeletionReason? DeletionReason { get; set; }
    /// <summary>Injected operational declaration instant.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Bounded nonfinancial operational history, with explicit optional public signature evidence.</summary>
public sealed class HouseholdAuditRow
{
    /// <summary>Exact private room scope.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Strict room-local position.</summary>
    public long Sequence { get; set; }
    /// <summary>Fixed original operation classification.</summary>
    public HouseholdAuditAction Action { get; set; }
    /// <summary>Actual authenticated initiating identity.</summary>
    public string ActorId { get; set; } = "";
    /// <summary>Actual approved or independently pending initiating device.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Original bounded idempotency operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Service-reported operational instant, never a financial date.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Membership checkpoint after the event.</summary>
    public long MembershipVersion { get; set; }
    /// <summary>Public original checkpoint digest.</summary>
    public string MembershipHead { get; set; } = "";
    /// <summary>Encrypted publication checkpoint after the event.</summary>
    public long PublicationCursor { get; set; }
    /// <summary>Public original publication digest.</summary>
    public string PublicationHead { get; set; } = "";
    /// <summary>Finite public signature evidence only, never a recipient/key envelope.</summary>
    public string EvidenceJson { get; set; } = "";
    /// <summary>Exact encoded metadata byte accounting.</summary>
    public int Bytes { get; set; }
}

/// <summary>Additive operational state; original financial schema and encrypted wire are unchanged.</summary>
internal static class HouseholdLifecycleModel
{
    /// <summary>Configures private room tombstones and bounded child audit rows.</summary>
    internal static void Configure(ModelBuilder model)
    {
        var space = model.Entity<HouseholdSpaceRow>();
        space.Property(row => row.DeletionRequestHash).HasMaxLength(64); space.Property(row => row.DeletionActorHash).HasMaxLength(64);
        space.HasIndex(row => new { row.Deleted, row.StorageRetired }); space.HasIndex(row => row.HostExpiresAt);
        var audit = model.Entity<HouseholdAuditRow>(); audit.ToTable("HouseholdAudit"); audit.HasKey(row => new { row.SpaceId, row.Sequence });
        audit.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Restrict);
        audit.Property(row => row.ActorId).HasMaxLength(450); audit.Property(row => row.MembershipHead).HasMaxLength(64); audit.Property(row => row.PublicationHead).HasMaxLength(64);
        audit.Property(row => row.EvidenceJson).HasMaxLength(512 * 1024);
    }
}
