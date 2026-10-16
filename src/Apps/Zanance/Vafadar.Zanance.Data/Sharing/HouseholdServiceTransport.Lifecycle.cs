using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdServiceTransport
{
    /// <summary>Reads only actual owner-scoped privacy checkpoints, without a financial refresh or epoch-key request.</summary>
    public Task<HouseholdDeletionCheckpoint> DeletionCheckpointAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/deletion/checkpoint?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdDeletionCheckpoint, ct);
    /// <summary>Reads only this actual approved member's service deadlines, without asking for financial content.</summary>
    public Task<HouseholdLifecycleStatus> LifecycleAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/lifecycle?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdLifecycleStatus, ct);
    /// <summary>Reads one finite original room audit prefix; later appends cannot extend an explicitly retained horizon.</summary>
    public Task<HouseholdAuditPage> AuditAsync(Guid space, Guid device, long after, long? through, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/audit?device=" + Id(device) + "&after=" + after.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            (through is { } end ? "&through=" + end.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""), HouseholdMetadataJsonContext.Default.HouseholdAuditPage, ct);
    /// <summary>Submits only the protected original current-owner consent to remote permanent deletion.</summary>
    public Task<HouseholdDeletionReceipt> DeleteSpaceAsync(Guid space, HouseholdDeleteRequest request, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/deletion", request, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt, ct);
    /// <summary>Only an exact authenticated original outcome permits completion or explicit withdrawal of a request that cannot be accepted.</summary>
    public Task<HouseholdDeletionReview> ReviewDeletionAsync(Guid space, HouseholdDeleteRequest request, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/deletion/review", request, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, HouseholdMetadataJsonContext.Default.HouseholdDeletionReview, ct);
    /// <summary>Observes only the owning identity's minimal receipt after ciphertext/member/key retirement.</summary>
    public Task<HouseholdDeletionReceipt> DeletionReceiptAsync(Guid space, Guid operation, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/deletion/" + Id(operation), HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt, ct);
}
