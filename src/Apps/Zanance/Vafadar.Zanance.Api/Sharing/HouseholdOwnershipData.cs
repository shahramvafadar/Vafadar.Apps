using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Participant-scoped public ownership handshake and opaque grants; no root, epoch, recovery kit or financial plaintext.</summary>
public sealed class HouseholdOwnershipRow
{
    /// <summary>Exact owning room and cascade lifetime.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Original root-signed operation, retained after completion/cancellation for exact acknowledgement.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Original owning identity; never a request-body replacement for authenticated authorization.</summary>
    public string OwnerId { get; set; } = "";
    /// <summary>Original approved source device.</summary>
    public Guid OwnerDeviceId { get; set; }
    /// <summary>Existing accepted target identity.</summary>
    public string TargetId { get; set; } = "";
    /// <summary>Exact approved target device selected by the old owner.</summary>
    public Guid TargetDeviceId { get; set; }
    /// <summary>Finite original signed consent.</summary>
    public string SignedOffer { get; set; } = "";
    /// <summary>Original bounded stage, never inferred from a current role.</summary>
    public HouseholdOwnershipStage Stage { get; set; }
    /// <summary>Original verified source checkpoint for participant-side old-root verification after a completed handoff.</summary>
    public long AnchorVersion { get; set; }
    /// <summary>Original target-generated public candidate/ciphertext, byte-identical on retry.</summary>
    public string? PreparationJson { get; set; }
    /// <summary>Exact literal old-root signature; the target's final acceptance binds these bytes.</summary>
    public string? SignedStatement { get; set; }
    /// <summary>Original target acceptance and opaque private-root delegations.</summary>
    public string? AcceptanceJson { get; set; }
    /// <summary>Public final membership receipt only; removed participants cannot retrieve encrypted key grants through observation.</summary>
    public string? PublishedProofJson { get; set; }
    /// <summary>Original injected UTC creation instant.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Exclusive original proposal deadline.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Additive transfer persistence, with one active handshake per room and durable exact-operation receipts.</summary>
internal static class HouseholdOwnershipModel
{
    /// <summary>Configures only public/ciphertext protocol metadata under the room's existing lifetime.</summary>
    internal static void Configure(ModelBuilder model)
    {
        var row = model.Entity<HouseholdOwnershipRow>(); row.ToTable("HouseholdOwnershipTransfers");
        row.HasKey(value => new { value.SpaceId, value.OperationId });
        row.HasOne<HouseholdSpaceRow>().WithMany().HasForeignKey(value => value.SpaceId).OnDelete(DeleteBehavior.Cascade);
        row.HasIndex(value => value.SpaceId).IsUnique().HasFilter("\"Stage\" >= 1 AND \"Stage\" <= 4");
        row.HasIndex(value => new { value.TargetId, value.Stage });
        row.Property(value => value.OwnerId).HasMaxLength(450); row.Property(value => value.TargetId).HasMaxLength(450);
        row.Property(value => value.SignedOffer).HasMaxLength(8192);
        row.Property(value => value.PreparationJson).HasMaxLength(HouseholdOwnershipProtocol.MaximumBytes);
        row.Property(value => value.AcceptanceJson).HasMaxLength(HouseholdOwnershipProtocol.MaximumBytes);
        row.Property(value => value.SignedStatement).HasMaxLength(HouseholdLimits.MembershipBytes * 2);
        row.Property(value => value.PublishedProofJson).HasMaxLength(512 * 1024);
    }
}
