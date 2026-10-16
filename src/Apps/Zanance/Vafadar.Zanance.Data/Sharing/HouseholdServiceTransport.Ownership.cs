using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdServiceTransport
{
    /// <summary>Reads only an actual approved participant's active original handoff, never an arbitrary member directory.</summary>
    public Task<HouseholdOwnershipObservation> CurrentOwnershipAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/ownership?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Starts only the exact original source-root consent, freezing new financial publication while the handoff is pending.</summary>
    public Task<HouseholdOwnershipObservation> OpenOwnershipAsync(Guid space, HouseholdOpenOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/ownership", input, HouseholdMetadataJsonContext.Default.HouseholdOpenOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Submits the original target-generated public candidate and standard opaque grants.</summary>
    public Task<HouseholdOwnershipObservation> PrepareOwnershipAsync(Guid space, Guid operation, HouseholdPrepareOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Ownership(space, operation) + "/prepare", input, HouseholdMetadataJsonContext.Default.HouseholdPrepareOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Retains the exact old-root signature for the target's independent final acceptance.</summary>
    public Task<HouseholdOwnershipObservation> AuthorizeOwnershipAsync(Guid space, Guid operation, HouseholdAuthorizeOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Ownership(space, operation) + "/authorize", input, HouseholdMetadataJsonContext.Default.HouseholdAuthorizeOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Submits only the original target's new-root acceptance and exact private-root recipient inventory.</summary>
    public Task<HouseholdOwnershipObservation> AcceptOwnershipAsync(Guid space, Guid operation, HouseholdAcceptOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Ownership(space, operation) + "/accept", input, HouseholdMetadataJsonContext.Default.HouseholdAcceptOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Observes an original participant's public receipt through actual-session private possession, even after the old owner leaves.</summary>
    public Task<HouseholdOwnershipObservation> ObserveOwnershipAsync(Guid space, Guid operation, HouseholdObserveOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Ownership(space, operation) + "/observe", input, HouseholdMetadataJsonContext.Default.HouseholdObserveOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Explicitly declines/cancels only the original active handoff; published membership cannot be undone.</summary>
    public Task<HouseholdOwnershipObservation> CancelOwnershipAsync(Guid space, Guid operation, HouseholdCancelOwnership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Ownership(space, operation) + "/cancel", input, HouseholdMetadataJsonContext.Default.HouseholdCancelOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, ct);
    /// <summary>Opaque operation identifiers only; no capability, signature or credential enters a URL.</summary>
    private static string Ownership(Guid space, Guid operation) => Space(space) + "/ownership/" + Id(operation);
}
