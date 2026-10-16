using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdWorkspace
{
    /// <summary>Imports only into a separate empty captured native profile after actual identity, peer, public continuity and private-kit checks.</summary>
    public async Task<HouseholdJournalState> BeginOwnerRecoveryAsync(HouseholdServiceTransport transport,
        HouseholdRecoveryKit kit, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var captured = await finances.ReviewAsync(ct); var identity = await transport.IdentityAsync(ct);
            if (identity.Id != kit.OwnerId || transport.Peer.BindingKey(identity.Id) != kit.Peer.BindingKey(kit.OwnerId)) throw Refusal();
            var anchor = new HouseholdRecoveryAnchor(1, "zanance.household.recovered-anchor.v1", kit.SpaceId, kit.EnrollmentId,
                kit.OwnerId, Public(kit.Public), kit.InitialOwnerAnchor);
            var anchorJson = JsonSerializer.Serialize(anchor, HouseholdMetadataJsonContext.Default.HouseholdRecoveryAnchor);
            var retained = await journal.CaptureAsync(transport.Peer, identity.Id, ct);
            if (retained is not null)
            {
                if (retained.OwnerCreation || retained.Authority.GenerationPath != captured.GenerationPath || retained.SignedInvitation != anchorJson ||
                    retained.SpaceId != Guid.Empty && retained.SpaceId != kit.SpaceId) throw Refusal();
                if (retained.MembershipVersion > 0 || await journal.HasOwnerRecoveryAsync(retained.Authority, ct)) return retained;
            }
            var observed = await transport.RecoveryAsync(kit.SpaceId, ct);
            var history = await DownloadRecoveryHistoryAsync(transport, kit.SpaceId, ct);
            var trusted = HouseholdTrustChain.RecoveryHistory(kit, history, observed, identity.Id);
            using (var opened = HouseholdRecoveryMaterial.OpenRoot(cryptography, kit.SpaceId, kit.EnrollmentId,
                trusted.States[checked((int)observed.MembershipVersion - 1)].Epoch, observed.RootCiphertext, trusted.Current.Root, kit))
            { if (opened.Public != trusted.Current.Root) throw Refusal(); }
            HouseholdJournalState draft;
            if (retained is not null) draft = retained;
            else
            {
            using var device = SigningRecipientIdentity.Create();
            draft = await journal.BeginDraftAsync(transport.Peer, identity.Id,
                new(kit.InitialOwnerAnchor.SigningKey, kit.InitialOwnerAnchor.WrappingKey), false, device, null, anchorJson, ct,
                requireEmptyFinancial: true, expectedGenerationPath: captured.GenerationPath);
            }
            if (draft.SpaceId == Guid.Empty) await journal.BindRemoteAsync(draft.Authority, kit.SpaceId, Guid.NewGuid(), ct);
            await journal.RetainOwnerRecoveryAsync(draft.Authority, kit, observed, history, ct);
            return await RequireStateAsync(transport, draft.Authority, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Resumes the original import/request without replacing keys and adopts the complete historical finance only after independent actual owner/device approval.</summary>
    public async Task<HouseholdJournalState> CompleteOwnerRecoveryAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, string reviewedName, CancellationToken ct = default)
    {
        RequireName(reviewedName); await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            if (state.MembershipVersion > 0 && !await journal.HasOwnerRecoveryAsync(authority, ct))
            {
                state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
                await ConnectAsync(transport, state, reviewedName, receiving: (await finances.ReviewAsync(ct)).RemoteProfile is null, ct);
                var resumed = await coordinator.SynchronizeAsync(transport, authority, ct);
                if (!resumed.Applied || resumed.NeedsReview) throw Refusal(); return state;
            }
            using var frame = await journal.ReadOwnerRecoveryAsync(authority, ct);
            using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit);
            var pending = await journal.ReadMembershipProposalAsync(authority, ct);
            if (state.MembershipVersion == 0 && pending is null)
            {
                var history = await journal.OwnerRecoveryHistoryAsync(authority, frame, ct);
                var through = await journal.OwnerRecoveryImportedThroughAsync(authority, history, ct);
                while (through < frame.Observation.ThroughEpoch)
                {
                    var page = await transport.RecoveryEpochsAsync(state.SpaceId, kit.EnrollmentId, through, frame.Observation.ThroughEpoch, ct);
                    if (page.Epochs is not { Length: > 0 and <= HouseholdRecoveryBounds.PageSize } || page.Epochs[0].Epoch != through + 1) throw Refusal();
                    through = await journal.RecordOwnerRecoveryPageAsync(authority, frame, page, ct);
                }
                var session = await transport.SessionAsync(ct); if (session.IdentityId != authority.IdentityId) throw Refusal();
                using var keys = await journal.ReadKeysAsync(authority, ct);
                using var old = await journal.ReadRecoveryEpochAsync(authority, history, history.Current.Epoch, ct); using var next = old.Next();
                using var oldRoot = HouseholdRecoveryMaterial.OpenRoot(cryptography, state.SpaceId, kit.EnrollmentId,
                    history.States[checked((int)frame.Observation.MembershipVersion - 1)].Epoch, frame.Observation.RootCiphertext, history.Current.Root, kit);
                using var root = SigningRecipientIdentity.Create(); var current = history.Current.Snapshot(); var operation = Guid.NewGuid();
                var statement = current with { OperationId = operation, Change = HouseholdMembershipChange.RecoverOwner, InvitationId = null,
                    ExpectedVersion = current.Version, Version = current.Version + 1, PreviousHash = history.Current.Hash, PreviousOwnerId = authority.IdentityId,
                    OwnerKeys = Public(root), KeyEpoch = next.Epoch, KeyCommitment = next.KeyCommitment, AddressKeyCommitment = next.AddressKeyCommitment,
                    ValidUntilUnixSeconds = ProposalDeadline(), PublicationCursor = frame.Observation.PublicationCursor, PublicationHead = frame.Observation.PublicationHead,
                    Members = current.Members.Select(member => member.Role == HouseholdRole.Owner ? member with { Devices = [new(state.DeviceId, Public(keys.Device))] } : member).ToArray() };
                var proposal = RootPublication(state.DeviceId, history.Current, statement, oldRoot, root, next, (signed, grantsHash) =>
                    new(frame.Observation.SignedEnrollment, Sign(new HouseholdRecoveryConsent(1, state.SpaceId, kit.EnrollmentId, operation, authority.IdentityId,
                        session.SessionId, state.DeviceId, HouseholdTrustChain.Digest(signed), grantsHash), HouseholdMetadataJsonContext.Default.HouseholdRecoveryConsent,
                        kit.Secret, HouseholdRecoveryProofTypes.Consent)));
                var rootCipher = HouseholdRecoveryMaterial.SealRoot(cryptography, state.SpaceId, kit.EnrollmentId, next.Epoch, root, kit.Public);
                var certificate = new HouseholdRecoveryCertificate(1, state.SpaceId, kit.EnrollmentId, authority.IdentityId, Public(root), Public(kit.Public),
                    statement.Version, HouseholdTrustChain.Digest(proposal.SignedStatement), HouseholdPublicationDigest.Ciphertext(rootCipher));
                var archived = HouseholdRecoveryMaterial.SealEpoch(cryptography, kit.EnrollmentId, statement, statement, next, root, kit.Public);
                proposal = proposal with { Recovery = new(HouseholdRecoveryMutationKind.Refresh, kit.EnrollmentId,
                    Sign(certificate, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate, root, HouseholdRecoveryProofTypes.Enrollment),
                    rootCipher, certificate.RootCiphertextDigest, [archived]) };
                var possession = Sign(new HouseholdDeviceSessionStatement(1, state.SpaceId, state.DeviceId, operation, authority.IdentityId,
                    session.SessionId, Public(keys.Device), statement.ValidUntilUnixSeconds), HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement,
                    keys.Device, HouseholdRecoveryProofTypes.DevicePossession);
                await journal.StageOwnerRecoveryRequestAsync(authority, frame, new(proposal, possession), root, next, ct);
                pending = proposal;
            }
            if (pending is not null)
            {
                using var original = await journal.ReadOwnerRecoveryAsync(authority, ct);
                if (original.DevicePossession is null || pending.RecoveryEvidence is null) throw Refusal();
                _ = await transport.RecoverOwnerAsync(state.SpaceId, new(pending, original.DevicePossession), ct);
                var history = await DownloadRecoveryHistoryAsync(transport, state.SpaceId, ct);
                await journal.RecordMembershipAsync(authority, history, ct);
                using var acceptedKeys = await journal.ReadKeysAsync(authority, ct);
                var admitted = HouseholdTrustChain.RecoveredHistory(JsonSerializer.Deserialize(state.SignedInvitation!,
                    HouseholdMetadataJsonContext.Default.HouseholdRecoveryAnchor) ?? throw Refusal(), history, authority.IdentityId, state.DeviceId,
                    acceptedKeys.Device.Public);
                var accepted = admitted.States.Single(item => item.Hash == HouseholdTrustChain.Digest(pending.SignedStatement));
                await journal.PromoteStagedEpochAsync(authority, accepted.Snapshot().OperationId, ct);
                await journal.CompleteMembershipProposalAsync(authority, ct);
            }
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await ConnectAsync(transport, state, reviewedName, receiving: (await finances.ReviewAsync(ct)).RemoteProfile is null, ct);
            var outcome = await coordinator.SynchronizeAsync(transport, authority, ct);
            if (!outcome.Applied || outcome.NeedsReview) throw Refusal();
            await journal.CompleteOwnerRecoveryAsync(authority, ct); return state;
        }
        finally { changing.Release(); }
    }

    private static async Task<IReadOnlyList<HouseholdMembershipProof>> DownloadRecoveryHistoryAsync(HouseholdServiceTransport transport, Guid space, CancellationToken ct)
    {
        var proofs = new List<HouseholdMembershipProof>(); long bytes = 0;
        while (true)
        {
            var page = await transport.RecoveryHistoryAsync(space, proofs.Count, ct);
            if (page is null || page.Length > 8) throw Refusal();
            if (page.Length == 0) return proofs.AsReadOnly();
            if (proofs.Count > 4096 - page.Length) throw Refusal();
            foreach (var proof in page)
            {
                if (proof?.SignedStatement is null || proof.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2) throw Refusal();
                bytes += proof.SignedStatement.Length + (proof.TransferAcceptance?.Length ?? 0) + (proof.RecoveryEvidence?.SignedEnrollment.Length ?? 0) +
                    (proof.RecoveryEvidence?.SignedConsent.Length ?? 0) + (proof.SignedAcceptanceClaim?.Length ?? 0);
                if (bytes > 64L * 1024 * 1024) throw Refusal();
                proofs.Add(proof);
            }
        }
    }
}
