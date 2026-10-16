using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdServiceTransport
{
    /// <summary>Reads only this approved current-owner device's separate encrypted root delegation.</summary>
    public Task<HouseholdOwnerRootGrant> OwnerRootAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/owner-root-grant?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdOwnerRootGrant, ct);
    /// <summary>Discovers only the actual owner's current opaque enrolled kit, even after all device keys have been lost.</summary>
    public Task<HouseholdRecoveryObservation> RecoveryAsync(Guid space, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/recovery", HouseholdMetadataJsonContext.Default.HouseholdRecoveryObservation, ct);
    /// <summary>Reads finite public continuity for an explicit kit recovery; no finance, device approval or private key follows.</summary>
    public Task<HouseholdMembershipProof[]> RecoveryHistoryAsync(Guid space, long after, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(after);
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/recovery/membership?after={after}"),
            HouseholdMetadataJsonContext.Default.HouseholdMembershipProofArray, ct);
    }
    /// <summary>Reads a finite original kit-recipient archive prefix frozen at one explicit key inventory.</summary>
    public Task<HouseholdRecoveryEpochPage> RecoveryEpochsAsync(Guid space, Guid enrollment, long after, long through, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(after); ArgumentOutOfRangeException.ThrowIfLessThan(through, after);
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/recovery/{Id(enrollment)}/epochs?after={after}&through={through}"),
            HouseholdMetadataJsonContext.Default.HouseholdRecoveryEpochPage, ct);
    }
    /// <summary>Stages the original owner-signed finite archive declaration without publishing its proposed root.</summary>
    public Task<HouseholdRecoverySeedProgress> OpenRecoverySeedAsync(Guid space, HouseholdOpenRecoverySeed input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/recovery/preparations", input, HouseholdMetadataJsonContext.Default.HouseholdOpenRecoverySeed,
            HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedProgress, ct);
    /// <summary>Observes the exact durable original upload prefix before choosing which retained ciphertext page remains.</summary>
    public Task<HouseholdRecoverySeedProgress> RecoverySeedAsync(Guid space, Guid device, Guid operation, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync($"{Space(space)}/recovery/preparations/{Id(operation)}?device={Id(device)}",
            HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedProgress, ct);
    /// <summary>Uploads at most one bounded immutable archive page; retry retains the exact prior encrypted bytes.</summary>
    public Task<HouseholdRecoverySeedProgress> UploadRecoveryEpochsAsync(Guid space, Guid operation, HouseholdUploadRecoveryEpochs input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync($"{Space(space)}/recovery/preparations/{Id(operation)}/epochs", input,
            HouseholdMetadataJsonContext.Default.HouseholdUploadRecoveryEpochs, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedProgress, ct);
    /// <summary>Cancels only this current-owner device's explicit unpromoted archive, without modifying financial history or enrolled keys.</summary>
    public Task CancelRecoverySeedAsync(Guid space, Guid device, Guid operation, CancellationToken ct = default) =>
        authentication.HouseholdEmptyAsync(HttpMethod.Delete, $"{Space(space)}/recovery/preparations/{Id(operation)}?device={Id(device)}", ct);
    /// <summary>Commits the exact independent kit, new device possession and complete signed new-root membership as one transaction.</summary>
    public Task<HouseholdCheckpoint> RecoverOwnerAsync(Guid space, HouseholdRecoverOwner input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/recovery/commit", input, HouseholdMetadataJsonContext.Default.HouseholdRecoverOwner,
            HouseholdMetadataJsonContext.Default.HouseholdCheckpoint, ct);
}
