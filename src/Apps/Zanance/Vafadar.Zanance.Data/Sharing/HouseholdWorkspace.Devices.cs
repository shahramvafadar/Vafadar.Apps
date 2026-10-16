using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdWorkspace
{
    /// <summary>Approves only the original self-enrollment of an accepted identity after independent full fingerprint comparison.</summary>
    public async Task ApproveDeviceAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        long reviewedVersion, HouseholdPendingDevice reviewed, string independentlyConfirmedFingerprint, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (state.MembershipVersion != reviewedVersion || await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            var actual = (await transport.PendingDevicesAsync(state.SpaceId, state.DeviceId, ct)).SingleOrDefault(row => row == reviewed) ?? throw Refusal();
            var device = new PublicSigningRecipientIdentity(actual.DeviceKeys.SigningKey, actual.DeviceKeys.WrappingKey);
            if (device.Fingerprint != independentlyConfirmedFingerprint) throw Refusal();
            var possession = StandardJoseSignatures.VerifyJson(actual.SignedPossession, device, "zanance-household-device-enrollment+jwt",
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
            if (possession.Format != 1 || possession.SpaceId != state.SpaceId || possession.DeviceId != Guid.Empty || possession.OperationId == Guid.Empty ||
                possession.SessionId == Guid.Empty || possession.IdentityId != actual.IdentityId || possession.DeviceKeys != actual.DeviceKeys || possession.ValidUntilUnixSeconds < 1) throw Refusal();
            var current = (await journal.ReadCurrentMembershipAsync(authority, ct)).Snapshot();
            var member = current.Members.SingleOrDefault(row => row.IdentityId == actual.IdentityId) ?? throw Refusal();
            if (member.Devices.Length >= HouseholdLimits.DevicesPerIdentity || current.Members.SelectMany(row => row.Devices).Any(row => row.Id == actual.DeviceId)) throw Refusal();
            var members = current.Members.Select(row => row.IdentityId == actual.IdentityId
                ? row with { Devices = [.. row.Devices, new(actual.DeviceId, actual.DeviceKeys)] } : row).ToArray();
            await PublishChangeAsync(transport, state, current, keys.Owner!, HouseholdMembershipChange.ApproveDevice, members, null, null, actual.DeviceId, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Revokes one explicitly reviewed other device; an owner's delegated root and both key roles are retired together.</summary>
    public async Task<HouseholdJournalState> RevokeDeviceAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        long reviewedVersion, string memberIdentity, Guid deviceId, string reviewedFingerprint, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (state.MembershipVersion != reviewedVersion || deviceId == state.DeviceId ||
                await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            using var preparing = await journal.ReadRecoveryPreparationAsync(authority, ct); if (preparing is not null) throw Refusal();
            var before = await journal.ReadCurrentMembershipAsync(authority, ct); var current = before.Snapshot();
            var member = current.Members.SingleOrDefault(row => row.IdentityId == memberIdentity) ?? throw Refusal();
            var selected = member.Devices.SingleOrDefault(row => row.Id == deviceId) ?? throw Refusal();
            if (member.Devices.Length < 2 || new PublicSigningRecipientIdentity(selected.Keys.SigningKey, selected.Keys.WrappingKey).Fingerprint != reviewedFingerprint)
                throw Refusal();
            var members = current.Members.Select(row => row.IdentityId == memberIdentity
                ? row with { Devices = row.Devices.Where(device => device.Id != deviceId).ToArray() } : row).ToArray();
            if (member.Role != HouseholdRole.Owner)
            {
                await PublishChangeAsync(transport, state, current, keys.Owner!, HouseholdMembershipChange.RevokeDevice, members, null, null, null, ct);
                return await journal.CaptureAsync(transport.Peer, authority.IdentityId, ct) ?? throw Refusal();
            }
            using var old = await journal.ReadEpochAsync(authority, before.Epoch, ct); using var next = old.Next();
            using var root = SigningRecipientIdentity.Create(); var operation = Guid.NewGuid();
            var statement = RotateStatement(state, current, next, root, operation) with { Change = HouseholdMembershipChange.RevokeDevice, Members = members };
            var proposal = RootPublication(state.DeviceId, before, statement, keys.Owner!, root, next);
            var recovery = await ObserveRecoveryAsync(transport, state, ct);
            if (recovery is not null)
            {
                var prior = StandardJoseSignatures.VerifyJson(recovery.SignedEnrollment, before.Root, HouseholdRecoveryProofTypes.Enrollment,
                    HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
                var kit = new PublicSigningRecipientIdentity(prior.RecoveryKeys.SigningKey, prior.RecoveryKeys.WrappingKey);
                var rootCipher = HouseholdRecoveryMaterial.SealRoot(cryptography, state.SpaceId, recovery.EnrollmentId, next.Epoch, root, kit);
                var archive = HouseholdRecoveryMaterial.SealEpoch(cryptography, recovery.EnrollmentId, statement, statement, next, root, kit);
                var certificate = new HouseholdRecoveryCertificate(1, state.SpaceId, recovery.EnrollmentId, authority.IdentityId, Public(root), prior.RecoveryKeys,
                    statement.Version, HouseholdTrustChain.Digest(proposal.SignedStatement), HouseholdPublicationDigest.Ciphertext(rootCipher));
                proposal = proposal with { Recovery = new(HouseholdRecoveryMutationKind.Refresh, recovery.EnrollmentId,
                    Sign(certificate, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate, root, HouseholdRecoveryProofTypes.Enrollment),
                    rootCipher, certificate.RootCiphertextDigest, [archive]) };
            }
            await journal.StageOwnerRootAsync(authority, root, ct); await journal.StageEpochAsync(authority, operation, next, ct);
            await journal.StageMembershipProposalAsync(authority, proposal, ct);
            _ = await transport.PublishMembershipAsync(state.SpaceId, proposal, ct);
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await journal.CompleteMembershipProposalAsync(authority, ct); return state;
        }
        finally { changing.Release(); }
    }
}
