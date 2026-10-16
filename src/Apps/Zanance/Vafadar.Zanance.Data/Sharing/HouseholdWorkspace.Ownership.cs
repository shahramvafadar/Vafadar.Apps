using System.Net;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdWorkspace
{
    /// <summary>Reads and retains only an exact authenticated participant observation; no read changes membership or generates a candidate.</summary>
    public async Task<HouseholdOwnershipObservation?> ReadOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try { return await ObserveOwnershipCoreAsync(transport, await RequireStateAsync(transport, authority, ct), ct); }
        finally { changing.Release(); }
    }

    /// <summary>Synchronizes saved source work, then retains original explicit target/device/role consent before the first freeze request.</summary>
    public async Task<HouseholdOwnershipObservation> BeginOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        long reviewedVersion, string targetIdentity, Guid targetDevice, string independentlyConfirmedDevice, HouseholdFormerOwnerRole formerRole, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            if (await journal.ReadOwnershipAsync(authority, ct) is not null || await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            using var recovery = await journal.ReadRecoveryPreparationAsync(authority, ct); if (recovery is not null) throw Refusal();
            if (!(await coordinator.SynchronizeAsync(transport, authority, ct)).Applied) throw Refusal();
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            var current = await journal.ReadCurrentMembershipAsync(authority, ct); var roster = current.Snapshot();
            var target = roster.Members.SingleOrDefault(member => member.IdentityId == targetIdentity && member.Role != HouseholdRole.Owner)?.Devices.SingleOrDefault(device => device.Id == targetDevice) ?? throw Refusal();
            if (state.MembershipVersion != reviewedVersion || !Enum.IsDefined(formerRole) || new PublicSigningRecipientIdentity(target.Keys.SigningKey, target.Keys.WrappingKey).Fingerprint != independentlyConfirmedDevice) throw Refusal();
            var offer = new HouseholdOwnershipOffer(1, state.SpaceId, Guid.NewGuid(), state.DeviceId, authority.IdentityId, targetDevice, targetIdentity,
                formerRole, state.MembershipVersion, state.MembershipHead, state.Cursor, state.PublicationHead, ProposalDeadline());
            var signed = Sign(offer, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer, keys.Owner!, HouseholdOwnershipProtocol.OfferType);
            var retained = new HouseholdOwnershipObservation(offer.OperationId, HouseholdOwnershipStage.Offered, signed, null, null, null, null, offer.ExpectedVersion);
            await journal.RetainOwnershipAsync(authority, retained, null, null, null, ct);
            var observed = await transport.OpenOwnershipAsync(state.SpaceId, new(state.DeviceId, signed), ct);
            await RetainOwnershipObservationAsync(state, observed, retained, ct); return observed;
        }
        finally { changing.Release(); }
    }

    /// <summary>Performs exactly the current participant's next explicit original step; another operation is never silently completed.</summary>
    public async Task<HouseholdOwnershipObservation> AdvanceOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        Guid reviewedOperation, HouseholdOwnershipStage reviewedStage, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            var retained = await journal.ReadOwnershipAsync(authority, ct);
            var observed = await ObserveOwnershipCoreAsync(transport, state, ct) ?? throw Refusal();
            if (observed.OperationId != reviewedOperation || observed.Stage != reviewedStage) throw Refusal();
            retained = await journal.ReadOwnershipAsync(authority, ct) ?? throw Refusal();
            var (offer, anchor) = await VerifyOwnershipObservationAsync(state, retained, ct);
            if (observed.Stage == HouseholdOwnershipStage.Published)
                return await AdoptOwnershipAsync(transport, state, retained, ct);
            if (observed.Stage is HouseholdOwnershipStage.Canceled or HouseholdOwnershipStage.Expired) return observed;
            if (observed.Stage == HouseholdOwnershipStage.Offered && offer.OwnerId == authority.IdentityId && offer.OwnerDeviceId == state.DeviceId)
            {
                // An original offer that never reached HTTP still needs an authenticated
                // terminal receipt after expiry; it must not become an unreleasable local lock.
                observed = await transport.OpenOwnershipAsync(state.SpaceId, new(state.DeviceId, retained.SignedOffer!), ct);
                await RetainOwnershipObservationAsync(state, observed, retained, ct); return observed;
            }
            if (offer.ExpiresUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds()) throw Refusal();
            if (observed.Stage == HouseholdOwnershipStage.Offered && offer.TargetId == authority.IdentityId && offer.TargetDeviceId == state.DeviceId)
            {
                // A downloaded ciphertext replica is not an applied local financial ledger.
                // Receive the exact frozen offered horizon before creating ownership keys.
                if (!(await coordinator.ReceiveOwnershipAsync(transport, authority, offer.OperationId, ct)).Applied) throw Refusal();
                state = await RequireStateAsync(transport, authority, ct);
            }
            using var keys = await journal.ReadKeysAsync(authority, ct);
            if (offer.TargetId == authority.IdentityId && offer.TargetDeviceId == state.DeviceId)
            {
                if (observed.Stage == HouseholdOwnershipStage.Offered)
                {
                    // An absent HTTP acknowledgement must reuse the durable original root,
                    // epoch, recipient envelopes and signature, including after process loss.
                    if (retained.Preparation is { } originalPreparation)
                    {
                        using var originalRoot = await journal.ReadPreparedOwnerRootAsync(authority,
                            new(originalPreparation.Statement.OwnerKeys.SigningKey, originalPreparation.Statement.OwnerKeys.WrappingKey), ct);
                        using var originalEpoch = await journal.ReadStagedEpochAsync(authority, offer.OperationId, ct);
                        if (originalEpoch.Epoch != originalPreparation.Statement.KeyEpoch || originalEpoch.KeyCommitment != originalPreparation.Statement.KeyCommitment ||
                            originalEpoch.AddressKeyCommitment != originalPreparation.Statement.AddressKeyCommitment) throw Refusal();
                        observed = await transport.PrepareOwnershipAsync(state.SpaceId, offer.OperationId, new(state.DeviceId, originalPreparation), ct);
                        await RetainOwnershipObservationAsync(state, observed, retained, ct); return observed;
                    }
                    var status = await finances.StatusAsync(ct);
                    if (status.LastSuccessAt is null || status.PendingChanges != 0 || status.Conflicts != 0) throw Refusal();
                    using var oldEpoch = await journal.ReadEpochAsync(authority, anchor.Epoch, ct);
                    using var nextEpoch = oldEpoch.Next(); using var root = SigningRecipientIdentity.Create();
                    var members = anchor.Snapshot().Members.Where(member => member.IdentityId != offer.OwnerId || offer.FormerRole != HouseholdFormerOwnerRole.Leave)
                        .Select(member => member.IdentityId == offer.TargetId ? member with { Role = HouseholdRole.Owner } : member.IdentityId == offer.OwnerId
                            ? member with { Role = offer.FormerRole == HouseholdFormerOwnerRole.Editor ? HouseholdRole.Editor : HouseholdRole.Viewer } : member).ToArray();
                    var next = anchor.Snapshot() with { OperationId = offer.OperationId, Change = HouseholdMembershipChange.TransferOwner, InvitationId = null,
                        ExpectedVersion = anchor.Version, Version = anchor.Version + 1, PreviousHash = anchor.Hash, PreviousOwnerId = offer.OwnerId, OwnerId = offer.TargetId,
                        OwnerKeys = Public(root), KeyEpoch = nextEpoch.Epoch, KeyCommitment = nextEpoch.KeyCommitment, AddressKeyCommitment = nextEpoch.AddressKeyCommitment,
                        ValidUntilUnixSeconds = offer.ExpiresUnixSeconds, Members = members, PublicationCursor = offer.PublicationCursor, PublicationHead = offer.PublicationHead };
                    RequireOwnershipCandidate(offer, anchor, next);
                    var grants = members.SelectMany(member => member.Devices).Select(device =>
                    {
                        var cipher = nextEpoch.Grant(cryptography, next, root, device);
                        return new HouseholdEncryptedGrant(nextEpoch.Epoch, device.Id, cipher, HouseholdPublicationDigest.Ciphertext(cipher));
                    }).ToArray();
                    var prepared = new HouseholdOwnershipPreparation(next, grants, "");
                    prepared = prepared with { SignedDeviceProof = OwnershipDeviceProof(state, offer.OperationId, HouseholdOwnershipStage.Prepared,
                        HouseholdOwnershipProtocol.PreparationDigest(prepared), keys.Device) };
                    var pending = retained with { Stage = HouseholdOwnershipStage.Prepared, Preparation = prepared };
                    await journal.RetainOwnershipAsync(authority, pending, retained, root, nextEpoch, ct);
                    observed = await transport.PrepareOwnershipAsync(state.SpaceId, offer.OperationId, new(state.DeviceId, prepared), ct);
                    await RetainOwnershipObservationAsync(state, observed, pending, ct); return observed;
                }
                if (observed.Stage == HouseholdOwnershipStage.Authorized)
                {
                    if (retained.Acceptance is { } originalAcceptance)
                    {
                        observed = await transport.AcceptOwnershipAsync(state.SpaceId, offer.OperationId, new(state.DeviceId, originalAcceptance), ct);
                        await RetainOwnershipObservationAsync(state, observed, retained, ct); return observed;
                    }
                    var prepared = retained.Preparation ?? throw Refusal(); var signed = retained.SignedStatement ?? throw Refusal();
                    using var root = await journal.ReadPreparedOwnerRootAsync(authority, new(prepared.Statement.OwnerKeys.SigningKey, prepared.Statement.OwnerKeys.WrappingKey), ct);
                    using var epoch = await journal.ReadStagedEpochAsync(authority, offer.OperationId, ct);
                    if (epoch.Epoch != prepared.Statement.KeyEpoch || epoch.KeyCommitment != prepared.Statement.KeyCommitment) throw Refusal();
                    var consent = new HouseholdOwnerAcceptanceStatement(1, state.SpaceId, offer.OperationId, HouseholdTrustChain.Digest(signed), HouseholdPublicationDigest.Grants(prepared.Grants));
                    var acceptance = Sign(consent, HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement, root, "zanance-household-owner-acceptance+jwt");
                    var verified = HouseholdTrustChain.Advance(anchor, new(signed, acceptance, null, consent.GrantsHash));
                    var roots = prepared.Statement.Members.Single(member => member.Role == HouseholdRole.Owner).Devices
                        .Select(device => HouseholdRecoveryMaterial.GrantOwnerRoot(cryptography, verified, root, device.Id)).ToArray();
                    var accepted = new HouseholdOwnershipAcceptance(acceptance, roots, "");
                    accepted = accepted with { SignedDeviceProof = OwnershipDeviceProof(state, offer.OperationId, HouseholdOwnershipStage.Accepted,
                        HouseholdOwnershipProtocol.AcceptanceDigest(accepted), keys.Device) };
                    var pending = retained with { Stage = HouseholdOwnershipStage.Accepted, Acceptance = accepted };
                    await journal.RetainOwnershipAsync(authority, pending, retained, null, null, ct);
                    observed = await transport.AcceptOwnershipAsync(state.SpaceId, offer.OperationId, new(state.DeviceId, accepted), ct);
                    await RetainOwnershipObservationAsync(state, observed, pending, ct); return observed;
                }
            }
            if (offer.OwnerId == authority.IdentityId && offer.OwnerDeviceId == state.DeviceId)
            {
                using var owner = await RequireOwnerKeysAsync(transport, state, ct, allowOwnership: true);
                if (observed.Stage == HouseholdOwnershipStage.Prepared)
                {
                    var prepared = retained.Preparation ?? throw Refusal();
                    var signed = retained.SignedStatement ?? Sign(prepared.Statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, owner.Owner!, "zanance-household-membership+jwt");
                    var pending = retained with { Stage = HouseholdOwnershipStage.Authorized, SignedStatement = signed };
                    await journal.RetainOwnershipAsync(authority, pending, retained, null, null, ct);
                    observed = await transport.AuthorizeOwnershipAsync(state.SpaceId, offer.OperationId, new(state.DeviceId, signed), ct);
                    await RetainOwnershipObservationAsync(state, observed, pending, ct); return observed;
                }
                if (observed.Stage == HouseholdOwnershipStage.Accepted)
                {
                    var proposal = OwnershipPublication(state, retained);
                    await journal.StageMembershipProposalAsync(authority, proposal, ct);
                    _ = await transport.PublishMembershipAsync(state.SpaceId, proposal, ct);
                    var completed = await ObserveOwnershipCoreAsync(transport, state, ct) ?? throw Refusal();
                    return await AdoptOwnershipAsync(transport, state, completed, ct);
                }
            }
            throw Refusal();
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly adopts only the original target offer's published financial horizon without generating or replacing any ownership candidate.</summary>
    public async Task<HouseholdOwnershipObservation> CatchUpOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        Guid reviewedOperation, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            var original = await ObserveOwnershipCoreAsync(transport, state, ct) ?? throw Refusal();
            var (offer, _) = await VerifyOwnershipObservationAsync(state, original, ct);
            if (original.OperationId != reviewedOperation || original.Stage is < HouseholdOwnershipStage.Offered or > HouseholdOwnershipStage.Accepted ||
                offer.TargetId != authority.IdentityId || offer.TargetDeviceId != state.DeviceId) throw Refusal();
            if (!(await coordinator.ReceiveOwnershipAsync(transport, authority, reviewedOperation, ct)).Applied) throw Refusal();
            return original;
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly cancels/declines only the original pending operation, preserving private candidates until its authenticated terminal acknowledgement.</summary>
    public async Task<HouseholdOwnershipObservation> CancelOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        Guid reviewedOperation, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            var retained = await ObserveOwnershipCoreAsync(transport, state, ct) ?? throw Refusal();
            if (retained.OperationId != reviewedOperation) throw Refusal();
            if (retained.Stage == HouseholdOwnershipStage.Offered)
            {
                var (offer, _) = await VerifyOwnershipObservationAsync(state, retained, ct);
                if (offer.OwnerId == authority.IdentityId && offer.OwnerDeviceId == state.DeviceId)
                {
                    var opened = await transport.OpenOwnershipAsync(state.SpaceId, new(state.DeviceId, retained.SignedOffer!), ct);
                    await RetainOwnershipObservationAsync(state, opened, retained, ct); retained = opened;
                    if (opened.Stage == HouseholdOwnershipStage.Expired) return opened;
                }
            }
            var observed = await transport.CancelOwnershipAsync(state.SpaceId, reviewedOperation, new(state.DeviceId), ct);
            await RetainOwnershipObservationAsync(state, observed, retained, ct); return observed;
        }
        finally { changing.Release(); }
    }

    /// <summary>Forgets only an explicitly reviewed terminal handoff, never an uncertain original publication or another private operation.</summary>
    public async Task FinishOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, Guid reviewedOperation, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var observed = await ObserveOwnershipCoreAsync(transport, state, ct) ?? throw Refusal();
            if (observed.OperationId != reviewedOperation) throw Refusal();
            if (observed.Stage == HouseholdOwnershipStage.Published) _ = await AdoptOwnershipAsync(transport, state, observed, ct);
            else if (observed.Stage is not (HouseholdOwnershipStage.Canceled or HouseholdOwnershipStage.Expired)) throw Refusal();
            await journal.FinishOwnershipAsync(authority, reviewedOperation, ct);
        }
        finally { changing.Release(); }
    }

    private async Task<HouseholdOwnershipObservation?> ObserveOwnershipCoreAsync(HouseholdServiceTransport transport, HouseholdJournalState state, CancellationToken ct)
    {
        var local = await journal.ReadOwnershipAsync(state.Authority, ct); HouseholdOwnershipObservation observed;
        if (local is not null)
        {
            using var keys = await journal.ReadKeysAsync(state.Authority, ct); var session = await transport.SessionAsync(ct);
            var proof = new HouseholdDeviceSessionStatement(1, state.SpaceId, state.DeviceId, local.OperationId!.Value, state.Authority.IdentityId,
                session.SessionId, Public(keys.Device), ProposalDeadline());
            try { observed = await transport.ObserveOwnershipAsync(state.SpaceId, local.OperationId.Value, new(state.DeviceId,
                Sign(proof, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, keys.Device, HouseholdOwnershipProtocol.ObservationType)), ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound && local.Stage == HouseholdOwnershipStage.Offered)
            { return local; /* Only explicit source Resume may submit its still-unacknowledged original offer. */ }
        }
        else
        {
            // Discover the original offer only against a current independently verified
            // membership; a server version number cannot choose an unknown signing root.
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            observed = await transport.CurrentOwnershipAsync(state.SpaceId, state.DeviceId, ct);
        }
        if (observed.OperationId is null) return null;
        observed = MergeOwnershipObservation(observed, local);
        _ = await VerifyOwnershipObservationAsync(state, observed, ct);
        await journal.RetainOwnershipAsync(state.Authority, observed, local, null, null, ct); return observed;
    }

    private async Task RetainOwnershipObservationAsync(HouseholdJournalState state, HouseholdOwnershipObservation observed, HouseholdOwnershipObservation expected, CancellationToken ct)
    {
        var merged = MergeOwnershipObservation(observed, expected); _ = await VerifyOwnershipObservationAsync(state, merged, ct);
        await journal.RetainOwnershipAsync(state.Authority, merged, expected, null, null, ct);
    }

    private async Task<(HouseholdOwnershipOffer Offer, HouseholdVerifiedMembership Anchor)> VerifyOwnershipObservationAsync(HouseholdJournalState state, HouseholdOwnershipObservation observed, CancellationToken ct)
    {
        if (observed.OperationId is not { } operation || observed.AnchorVersion < 1 || observed.AnchorVersion > state.MembershipVersion || observed.SignedOffer is null) throw Refusal();
        var anchor = await journal.ReadMembershipVersionAsync(state.Authority, observed.AnchorVersion, ct);
        var offer = StandardJoseSignatures.VerifyJson(observed.SignedOffer, anchor.Root, HouseholdOwnershipProtocol.OfferType, 8192, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
        if (offer.Format != 1 || offer.OperationId != operation || offer.SpaceId != state.SpaceId || offer.ExpectedVersion != anchor.Version || offer.ExpectedHead != anchor.Hash ||
            offer.OwnerId != anchor.Snapshot().OwnerId || offer.OwnerId != state.Authority.IdentityId && offer.TargetId != state.Authority.IdentityId) throw Refusal();
        var target = anchor.Snapshot().Members.SingleOrDefault(member => member.IdentityId == offer.TargetId)?.Devices.SingleOrDefault(device => device.Id == offer.TargetDeviceId) ?? throw Refusal();
        var targetKeys = new PublicSigningRecipientIdentity(target.Keys.SigningKey, target.Keys.WrappingKey);
        if (observed.Preparation is { } prepared)
        {
            if (prepared.Statement is null || prepared.Grants is null || prepared.Grants.Length is < 1 or > 48 ||
                prepared.Grants.Any(grant => grant is null || grant.Ciphertext is not { Length: > 0 and <= 32768 })) throw Refusal();
            RequireOwnershipCandidate(offer, anchor, prepared.Statement);
            RequireOwnershipDeviceProof(offer, prepared.SignedDeviceProof, targetKeys, HouseholdOwnershipStage.Prepared, HouseholdOwnershipProtocol.PreparationDigest(prepared));
            var deviceIds = prepared.Statement.Members.SelectMany(member => member.Devices).Select(device => device.Id).ToHashSet();
            if (prepared.Grants is null || prepared.Grants.Length != deviceIds.Count || prepared.Grants.Any(grant => grant is null || grant.Ciphertext is not { Length: > 0 and <= 32768 }) || prepared.Grants.Select(grant => grant.DeviceId).Distinct().Count() != deviceIds.Count ||
                prepared.Grants.Any(grant => !deviceIds.Contains(grant.DeviceId) || grant.Epoch != prepared.Statement.KeyEpoch || grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext))) throw Refusal();
            if (observed.SignedStatement is { } signed)
            {
                var statement = StandardJoseSignatures.VerifyJson(signed, anchor.Root, "zanance-household-membership+jwt", HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
                if (HouseholdOwnershipProtocol.StatementDigest(statement) != HouseholdOwnershipProtocol.StatementDigest(prepared.Statement)) throw Refusal();
            }
        }
        if (observed.Acceptance is { } accepted)
        {
            if (observed.Preparation is null || observed.SignedStatement is null || accepted.OwnerRootGrants is null ||
                accepted.OwnerRootGrants.Length is < 1 or > HouseholdLimits.DevicesPerIdentity || accepted.OwnerRootGrants.Any(grant => grant is null)) throw Refusal();
            RequireOwnershipDeviceProof(offer, accepted.SignedDeviceProof, targetKeys, HouseholdOwnershipStage.Accepted, HouseholdOwnershipProtocol.AcceptanceDigest(accepted));
            var acceptedMembership = HouseholdTrustChain.Advance(anchor, new(observed.SignedStatement, accepted.SignedAcceptance, null, HouseholdPublicationDigest.Grants(observed.Preparation.Grants)));
            var ownerDevices = acceptedMembership.Snapshot().Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Select(device => device.Id).ToHashSet();
            if (accepted.OwnerRootGrants is null || accepted.OwnerRootGrants.Length != ownerDevices.Count || accepted.OwnerRootGrants.Any(grant => grant is null) ||
                accepted.OwnerRootGrants.Select(grant => grant.DeviceId).Distinct().Count() != ownerDevices.Count ||
                accepted.OwnerRootGrants.Any(grant => !ownerDevices.Contains(grant.DeviceId) || grant.MembershipVersion != acceptedMembership.Version ||
                    grant.RootFingerprint != acceptedMembership.Root.Fingerprint || grant.Ciphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.RootCiphertextCharacters } ||
                    grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext))) throw Refusal();
        }
        if (observed.Stage == HouseholdOwnershipStage.Published)
        {
            if (observed.PublishedProof is null) throw Refusal();
            var published = HouseholdTrustChain.Advance(anchor, observed.PublishedProof);
            if (published.Snapshot().OperationId != offer.OperationId || !HouseholdOwnershipProtocol.Matches(offer, anchor.Snapshot(), published.Snapshot())) throw Refusal();
        }
        return (offer, anchor);
    }

    private async Task<HouseholdOwnershipObservation> AdoptOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalState state, HouseholdOwnershipObservation observed, CancellationToken ct)
    {
        if (observed.Stage != HouseholdOwnershipStage.Published || observed.PublishedProof is null) throw Refusal();
        var (offer, anchor) = await VerifyOwnershipObservationAsync(state, observed, ct);
        var history = (await journal.ReadHistoryAsync(state.Authority, ct)).ToList();
        if (history.Count == anchor.Version) history.Add(observed.PublishedProof);
        else if (history.Count < anchor.Version + 1 || HouseholdTrustChain.Digest(history[checked((int)anchor.Version)].SignedStatement) != HouseholdTrustChain.Digest(observed.PublishedProof.SignedStatement)) throw Refusal();
        var current = await journal.RecordMembershipAsync(state.Authority, history, ct);
        if (!current.Snapshot().Members.Any(member => member.IdentityId == state.Authority.IdentityId && member.Devices.Any(device => device.Id == state.DeviceId)))
            await journal.RecordRemoteAccessRefusalAsync(state.Authority, ct);
        else
        {
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            if (offer.TargetId == state.Authority.IdentityId && observed.Preparation is not null)
            {
                await journal.PromoteStagedEpochAsync(state.Authority, offer.OperationId, ct);
                using var owner = await RequireOwnerKeysAsync(transport, state, ct, allowOwnership: true);
            }
        }
        if (await journal.ReadMembershipProposalAsync(state.Authority, ct) is not null) await journal.CompleteMembershipProposalAsync(state.Authority, ct);
        return observed;
    }

    private static HouseholdPublishMembership OwnershipPublication(HouseholdJournalState state, HouseholdOwnershipObservation observed)
    {
        if (observed.Preparation is null || observed.Acceptance is null || observed.SignedStatement is null) throw Refusal();
        return new(state.DeviceId, observed.SignedStatement, observed.Acceptance.SignedAcceptance, null, observed.Preparation.Grants,
            OwnerRootGrants: observed.Acceptance.OwnerRootGrants);
    }
    private static HouseholdOwnershipObservation MergeOwnershipObservation(HouseholdOwnershipObservation observed, HouseholdOwnershipObservation? retained)
    {
        if (retained is null) return observed;
        if (observed.OperationId != retained.OperationId || observed.AnchorVersion != retained.AnchorVersion || observed.SignedOffer != retained.SignedOffer) throw Refusal();
        // Keep locally advanced intent while an unacknowledged remote stage is older,
        // but never accept changed bytes for any already retained original payload.
        if (observed.Preparation is not null && retained.Preparation is not null &&
            JsonSerializer.Serialize(observed.Preparation, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) != JsonSerializer.Serialize(retained.Preparation, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) ||
            observed.SignedStatement is not null && retained.SignedStatement is not null && observed.SignedStatement != retained.SignedStatement ||
            observed.Acceptance is not null && retained.Acceptance is not null &&
            JsonSerializer.Serialize(observed.Acceptance, HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance) != JsonSerializer.Serialize(retained.Acceptance, HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance)) throw Refusal();
        return observed with { Preparation = observed.Preparation ?? retained.Preparation, SignedStatement = observed.SignedStatement ?? retained.SignedStatement,
            Acceptance = observed.Acceptance ?? retained.Acceptance };
    }
    private static void RequireOwnershipCandidate(HouseholdOwnershipOffer offer, HouseholdVerifiedMembership anchor, HouseholdMembershipStatement next)
    {
        if (HouseholdMembershipRules.ValidateShape(next).Count != 0 || HouseholdMembershipRules.ValidateContinuity(next, anchor.Snapshot(), anchor.Hash).Count != 0 ||
            HouseholdMembershipRules.ValidateRosterTransition(next, anchor.Snapshot()).Count != 0 || !HouseholdOwnershipProtocol.Matches(offer, anchor.Snapshot(), next)) throw Refusal();
    }
    private static void RequireOwnershipDeviceProof(HouseholdOwnershipOffer offer, string signed, PublicSigningRecipientIdentity target, HouseholdOwnershipStage stage, string digest)
    {
        var proof = StandardJoseSignatures.VerifyJson(signed, target, HouseholdOwnershipProtocol.DeviceProofType, 8192, HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof);
        if (proof.Format != 1 || proof.SpaceId != offer.SpaceId || proof.OperationId != offer.OperationId || proof.DeviceId != offer.TargetDeviceId || proof.IdentityId != offer.TargetId ||
            proof.Stage != stage || proof.PayloadDigest != digest) throw Refusal();
    }
    private static string OwnershipDeviceProof(HouseholdJournalState state, Guid operation, HouseholdOwnershipStage stage, string digest, SigningRecipientIdentity device) =>
        Sign(new HouseholdOwnershipProof(1, state.SpaceId, operation, state.DeviceId, state.Authority.IdentityId, stage, digest),
            HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof, device, HouseholdOwnershipProtocol.DeviceProofType);
}
