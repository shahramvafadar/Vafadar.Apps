using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Exact two-sided ownership staging under the original actual-session/room lock; it never creates a new signing identity or changes finance.</summary>
internal sealed class HouseholdOwnershipStore(ServiceDbContext db)
{
    /// <summary>Starts one original current-owner consent and freezes financial publication without changing membership or keys.</summary>
    internal async Task<HouseholdOwnershipObservation> OpenAsync(HouseholdRequestScope scope, HouseholdOpenOwnership input, CancellationToken ct)
    {
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.TransferOwnership, ct);
        if (scope.Space.OwnerId != scope.ActorId || input.SignedOffer is not { Length: > 0 and <= 8192 }) throw Refuse(400, "InvalidHouseholdOwnershipOffer");
        var root = HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey));
        var offer = HouseholdMetadataProofs.Read(input.SignedOffer, root, HouseholdOwnershipProtocol.OfferType, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
        var previous = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.OperationId == offer.OperationId, ct);
        if (previous is not null)
        {
            if (previous.SignedOffer != input.SignedOffer || previous.OwnerId != scope.ActorId || previous.OwnerDeviceId != input.DeviceId) throw Refuse(409, "AlteredHouseholdOwnershipOffer");
            await ExpireAsync(scope, previous, ct); return Observe(previous);
        }
        var current = await ActiveAsync(scope, ct);
        if (current is not null || scope.Space.RotationRequired) throw Refuse(409, "HouseholdOwnershipOrRotationPending");
        // Release an expired row's immediate unique active-slot index before inserting a new
        // operation; the enclosing original room transaction still owns both effects.
        await db.SaveChangesAsync(ct);
        var before = await MembershipAsync(scope, ct);
        if (offer.Format != 1 || offer.OperationId == Guid.Empty || offer.SpaceId != scope.Space.Id || offer.OwnerId != scope.ActorId || offer.OwnerDeviceId != input.DeviceId ||
            offer.TargetId == offer.OwnerId || !Enum.IsDefined(offer.FormerRole) || offer.ExpectedVersion != scope.Space.MembershipVersion ||
            offer.ExpectedHead != scope.Space.MembershipHead || offer.PublicationCursor != scope.Space.Cursor || offer.PublicationHead != scope.Space.PublicationHead ||
            offer.ExpiresUnixSeconds < DateTimeOffset.MinValue.ToUnixTimeSeconds() || offer.ExpiresUnixSeconds > scope.Now.Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds() ||
            !before.Members.Any(member => member.IdentityId == offer.TargetId && member.Role is HouseholdRole.Editor or HouseholdRole.Viewer &&
                member.Devices.Any(device => device.Id == offer.TargetDeviceId))) throw Refuse(409, "HouseholdOwnershipCheckpointMismatch");
        if (await db.Set<HouseholdSpaceRow>().AnyAsync(row => row.OwnerId == offer.TargetId && row.Id != scope.Space.Id && !row.Deleted, ct))
            throw Refuse(413, "HouseholdHostedSpaceBound");
        var expired = offer.ExpiresUnixSeconds <= scope.Now.ToUnixTimeSeconds();
        var row = new HouseholdOwnershipRow { SpaceId = scope.Space.Id, OperationId = offer.OperationId, OwnerId = scope.ActorId, OwnerDeviceId = input.DeviceId,
            TargetId = offer.TargetId, TargetDeviceId = offer.TargetDeviceId, SignedOffer = input.SignedOffer, Stage = expired ? HouseholdOwnershipStage.Expired : HouseholdOwnershipStage.Offered,
            AnchorVersion = offer.ExpectedVersion, CreatedAt = scope.Now, ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(offer.ExpiresUnixSeconds) };
        // A valid original offer may have been retained before an offline period. Record
        // its terminal expiry without ever freezing or publishing its obsolete key candidate.
        db.Add(row); if (!expired) scope.Space.RotationRequired = true; return Observe(row);
    }

    /// <summary>Reads only an actual approved participant's active handoff; unrelated members receive no candidate or key envelope.</summary>
    internal async Task<HouseholdOwnershipObservation> CurrentAsync(HouseholdRequestScope scope, Guid deviceId, CancellationToken ct)
    {
        _ = await scope.RequireAsync(deviceId, HouseholdRemoteOperation.Read, ct);
        var row = await ActiveAsync(scope, ct);
        return row is null || row.OwnerId != scope.ActorId && row.TargetId != scope.ActorId ? Empty() : Observe(row);
    }

    /// <summary>Receives only the exact original target device's signed public candidate and opaque grants.</summary>
    internal async Task<HouseholdOwnershipObservation> PrepareAsync(HouseholdRequestScope scope, Guid operation, HouseholdPrepareOwnership input, CancellationToken ct)
    {
        var row = await PendingAsync(scope, operation, input.DeviceId, target: true, ct);
        var preparation = input.Preparation ?? throw Refuse(400, "InvalidHouseholdOwnershipPreparation");
        var json = JsonSerializer.Serialize(preparation, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation); Bound(json);
        if (row.PreparationJson is not null)
        { if (row.PreparationJson != json) throw Refuse(409, "AlteredHouseholdOwnershipPreparation"); return Observe(row); }
        if (row.Stage != HouseholdOwnershipStage.Offered || preparation.Statement is null) throw Refuse(409, "HouseholdOwnershipStageMismatch");
        if (preparation.Grants is null || preparation.Grants.Length is < 1 or > 48 || preparation.Grants.Any(grant => grant is null ||
            grant.Ciphertext is not { Length: > 0 and <= 32768 })) throw Refuse(400, "InvalidHouseholdOwnershipGrants");
        var offer = Offer(scope, row); var before = await MembershipAsync(scope, ct); var next = preparation.Statement;
        if (HouseholdMembershipRules.ValidateShape(next).Count != 0 || HouseholdMembershipRules.ValidateContinuity(next, before, scope.Space.MembershipHead).Count != 0 ||
            HouseholdMembershipRules.ValidateRosterTransition(next, before).Count != 0 || !HouseholdOwnershipProtocol.Matches(offer, before, next))
            throw Refuse(409, "HouseholdOwnershipCandidateMismatch");
        var expected = next.Members.SelectMany(member => member.Devices).Select(device => device.Id).ToHashSet();
        _ = HouseholdMembershipStore.ValidateGrants(next, preparation.Grants);
        if (preparation.Grants.Length != expected.Count || preparation.Grants.Any(grant => grant.Epoch != next.KeyEpoch)) throw Refuse(409, "HouseholdOwnershipCurrentGrantsRequired");
        await DeviceProofAsync(scope, row, preparation.SignedDeviceProof, HouseholdOwnershipStage.Prepared,
            HouseholdOwnershipProtocol.PreparationDigest(preparation), ct);
        row.PreparationJson = json; row.Stage = HouseholdOwnershipStage.Prepared; return Observe(row);
    }

    /// <summary>Retains the exact old-root signature over the original target-generated candidate; no fresh target/root is inferred.</summary>
    internal async Task<HouseholdOwnershipObservation> AuthorizeAsync(HouseholdRequestScope scope, Guid operation, HouseholdAuthorizeOwnership input, CancellationToken ct)
    {
        var row = await PendingAsync(scope, operation, input.DeviceId, target: false, ct);
        if (row.SignedStatement is not null)
        { if (row.SignedStatement != input.SignedStatement) throw Refuse(409, "AlteredHouseholdOwnershipAuthorization"); return Observe(row); }
        if (row.Stage != HouseholdOwnershipStage.Prepared || row.PreparationJson is null) throw Refuse(409, "HouseholdOwnershipStageMismatch");
        var prepared = JsonSerializer.Deserialize(row.PreparationJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) ?? throw Refuse(409, "InvalidHouseholdOwnershipStoredPreparation");
        var signed = HouseholdMetadataProofs.Membership(input.SignedStatement, HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)));
        if (HouseholdOwnershipProtocol.StatementDigest(signed) != HouseholdOwnershipProtocol.StatementDigest(prepared.Statement)) throw Refuse(409, "HouseholdOwnershipAuthorizationMismatch");
        row.SignedStatement = input.SignedStatement; row.Stage = HouseholdOwnershipStage.Authorized; return Observe(row);
    }

    /// <summary>Accepts only the exact literal old-root signature and complete new-root grant inventory from the original target device.</summary>
    internal async Task<HouseholdOwnershipObservation> AcceptAsync(HouseholdRequestScope scope, Guid operation, HouseholdAcceptOwnership input, CancellationToken ct)
    {
        var row = await PendingAsync(scope, operation, input.DeviceId, target: true, ct);
        var accepted = input.Acceptance ?? throw Refuse(400, "InvalidHouseholdOwnershipAcceptance");
        var json = JsonSerializer.Serialize(accepted, HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance); Bound(json);
        if (row.AcceptanceJson is not null)
        { if (row.AcceptanceJson != json) throw Refuse(409, "AlteredHouseholdOwnershipAcceptance"); return Observe(row); }
        if (row.Stage != HouseholdOwnershipStage.Authorized || row.SignedStatement is null || row.PreparationJson is null) throw Refuse(409, "HouseholdOwnershipStageMismatch");
        var prepared = JsonSerializer.Deserialize(row.PreparationJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) ?? throw Refuse(409, "InvalidHouseholdOwnershipStoredPreparation");
        var next = prepared.Statement;
        var consent = HouseholdMetadataProofs.Read(accepted.SignedAcceptance, HouseholdMetadataProofs.Public(next.OwnerKeys), HouseholdMetadataProofs.TransferType,
            HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement);
        if (consent.Format != 1 || consent.SpaceId != row.SpaceId || consent.OperationId != operation || consent.MembershipStatementHash !=
            HouseholdMetadataProofs.Digest(row.SignedStatement, HouseholdLimits.MembershipBytes * 2) || consent.GrantsHash != HouseholdPublicationDigest.Grants(prepared.Grants))
            throw Refuse(409, "HouseholdOwnershipAcceptanceMismatch");
        var devices = next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Select(device => device.Id).ToHashSet();
        var rootFingerprint = HouseholdMetadataProofs.Public(next.OwnerKeys).Fingerprint;
        if (accepted.OwnerRootGrants is null || accepted.OwnerRootGrants.Length != devices.Count || accepted.OwnerRootGrants.Any(grant => grant is null) || accepted.OwnerRootGrants.Select(grant => grant.DeviceId).Distinct().Count() != devices.Count ||
            accepted.OwnerRootGrants.Any(grant => !devices.Contains(grant.DeviceId) || grant.MembershipVersion != next.Version || grant.RootFingerprint != rootFingerprint ||
                grant.Ciphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.RootCiphertextCharacters } || grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext)))
            throw Refuse(409, "HouseholdOwnershipRootGrantsMismatch");
        await DeviceProofAsync(scope, row, accepted.SignedDeviceProof, HouseholdOwnershipStage.Accepted, HouseholdOwnershipProtocol.AcceptanceDigest(accepted), ct);
        row.AcceptanceJson = json; row.Stage = HouseholdOwnershipStage.Accepted; return Observe(row);
    }

    /// <summary>Only original participants may cancel their still-pending handoff; a final membership cannot be undone.</summary>
    internal async Task<HouseholdOwnershipObservation> CancelAsync(HouseholdRequestScope scope, Guid operation, HouseholdCancelOwnership input, CancellationToken ct)
    {
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.Read, ct);
        var row = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.OperationId == operation, ct) ?? throw Refuse(404, "HouseholdOwnershipUnavailable");
        if (row.OwnerId != scope.ActorId && (row.TargetId != scope.ActorId || row.TargetDeviceId != input.DeviceId)) throw Refuse(404, "HouseholdOwnershipUnavailable");
        await ExpireAsync(scope, row, ct);
        if (row.Stage is >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted)
        { row.Stage = HouseholdOwnershipStage.Canceled; await UnfreezeAsync(scope, row, ct); }
        return Observe(row);
    }

    /// <summary>Allows only private possession of the original participant device to observe its own public receipt after removal; never returns grants after completion.</summary>
    internal async Task<HouseholdOwnershipObservation> ObserveAsync(HouseholdRequestScope scope, Guid operation, HouseholdObserveOwnership input, CancellationToken ct)
    {
        if (scope.Space.Deleted) throw Refuse(404, "HouseholdOwnershipUnavailable");
        var row = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.OperationId == operation, ct) ?? throw Refuse(404, "HouseholdOwnershipUnavailable");
        var expected = row.OwnerId == scope.ActorId ? row.OwnerDeviceId : row.TargetId == scope.ActorId ? row.TargetDeviceId : Guid.Empty;
        if (expected == Guid.Empty || expected != input.DeviceId) throw Refuse(404, "HouseholdOwnershipUnavailable");
        var device = await db.Set<HouseholdDeviceRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.Id == expected && value.UserId == scope.ActorId, ct)
            ?? throw Refuse(404, "HouseholdOwnershipUnavailable");
        var proof = HouseholdMetadataProofs.Read(input.SignedPossession, HouseholdMetadataProofs.Public(new(device.SigningKey, device.WrappingKey)),
            HouseholdOwnershipProtocol.ObservationType, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
        if (proof.Format != 1 || proof.SpaceId != scope.Space.Id || proof.DeviceId != expected || proof.OperationId != operation || proof.IdentityId != scope.ActorId ||
            proof.SessionId != scope.SessionId || proof.DeviceKeys != new HouseholdPublicKeys(device.SigningKey, device.WrappingKey) || proof.ValidUntilUnixSeconds <= scope.Now.ToUnixTimeSeconds() ||
            proof.ValidUntilUnixSeconds > scope.Now.Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds()) throw Refuse(403, "HouseholdOwnershipObservationMismatch");
        await ExpireAsync(scope, row, ct);
        if (row.Stage is >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted)
        { _ = await scope.RequireAsync(expected, HouseholdRemoteOperation.Read, ct); }
        return Observe(row);
    }

    /// <summary>Checks an ordinary membership publish against the complete exact accepted handoff, or cancels it for a necessary safety rotation.</summary>
    internal async Task BeforePublicationAsync(HouseholdRequestScope scope, HouseholdMembershipStatement next, HouseholdPublishMembership input, CancellationToken ct)
    {
        var original = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.OperationId == next.OperationId, ct);
        // Possession of a newly generated root does not prove the target identity agreed:
        // the old owner could have generated that root itself. Require the actual original
        // target device's accepted handshake even through the general membership endpoint.
        if (next.Change == HouseholdMembershipChange.TransferOwner && original is null)
            throw Refuse(409, "HouseholdOwnershipTargetDeviceConsentRequired");
        if (original is not null && (next.Change != HouseholdMembershipChange.TransferOwner || original.Stage != HouseholdOwnershipStage.Accepted))
            throw Refuse(409, "HouseholdOwnershipOperationNotAccepted");
        var row = await ActiveAsync(scope, ct); if (row is null) return;
        if (next.Change is HouseholdMembershipChange.RevokeMember or HouseholdMembershipChange.RevokeDevice or HouseholdMembershipChange.RecoverOwner or HouseholdMembershipChange.RotateOwnerRoot or HouseholdMembershipChange.ConfirmDeparture)
        { row.Stage = HouseholdOwnershipStage.Canceled; return; }
        if (next.Change != HouseholdMembershipChange.TransferOwner || row.OperationId != next.OperationId || row.Stage != HouseholdOwnershipStage.Accepted ||
            row.OwnerId != scope.ActorId || row.OwnerDeviceId != input.DeviceId || row.PreparationJson is null || row.AcceptanceJson is null || row.SignedStatement != input.SignedStatement)
            throw Refuse(409, "HouseholdOwnershipPending");
        var prepared = JsonSerializer.Deserialize(row.PreparationJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) ?? throw Refuse(409, "InvalidHouseholdOwnershipStoredPreparation");
        var accepted = JsonSerializer.Deserialize(row.AcceptanceJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance) ?? throw Refuse(409, "InvalidHouseholdOwnershipStoredAcceptance");
        var expected = new HouseholdPublishMembership(input.DeviceId, row.SignedStatement, accepted.SignedAcceptance, null, prepared.Grants, OwnerRootGrants: accepted.OwnerRootGrants);
        if (HouseholdMembershipStore.RequestHash(expected) != HouseholdMembershipStore.RequestHash(input)) throw Refuse(409, "HouseholdOwnershipPublicationMismatch");
    }

    /// <summary>Retains the original public completed membership receipt inside the same atomic role/key transaction.</summary>
    internal async Task PublishedAsync(HouseholdRequestScope scope, HouseholdMembershipStatement next, HouseholdPublishMembership input, CancellationToken ct)
    {
        var row = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == next.SpaceId && value.OperationId == next.OperationId, ct);
        if (row is null || next.Change != HouseholdMembershipChange.TransferOwner) return;
        row.PublishedProofJson = JsonSerializer.Serialize(new HouseholdMembershipProof(input.SignedStatement, input.TransferAcceptance, input.SignedAcceptanceClaim,
            HouseholdPublicationDigest.Grants(input.Grants), input.RecoveryEvidence), HouseholdMetadataJsonContext.Default.HouseholdMembershipProof);
        row.Stage = HouseholdOwnershipStage.Published;
    }

    private async Task<HouseholdOwnershipRow?> ActiveAsync(HouseholdRequestScope scope, CancellationToken ct)
    {
        var row = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id &&
            value.Stage >= HouseholdOwnershipStage.Offered && value.Stage <= HouseholdOwnershipStage.Accepted, ct);
        if (row is null) return null; await ExpireAsync(scope, row, ct);
        return row.Stage is >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted ? row : null;
    }
    private async Task<HouseholdOwnershipRow> PendingAsync(HouseholdRequestScope scope, Guid operation, Guid device, bool target, CancellationToken ct)
    {
        _ = await scope.RequireAsync(device, target ? HouseholdRemoteOperation.Read : HouseholdRemoteOperation.TransferOwnership, ct);
        if (scope.Retention != HouseholdRetentionState.Active) throw Refuse(403, "HouseholdOwnershipInactive");
        var row = await db.Set<HouseholdOwnershipRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.OperationId == operation, ct) ?? throw Refuse(404, "HouseholdOwnershipUnavailable");
        if (target ? row.TargetId != scope.ActorId || row.TargetDeviceId != device : row.OwnerId != scope.ActorId || row.OwnerDeviceId != device)
            throw Refuse(404, "HouseholdOwnershipUnavailable");
        await ExpireAsync(scope, row, ct);
        if (row.Stage is not (>= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted)) throw Refuse(409, "HouseholdOwnershipStageMismatch");
        var offer = Offer(scope, row);
        if (scope.Space.OwnerId != row.OwnerId || scope.Space.MembershipVersion != offer.ExpectedVersion || scope.Space.MembershipHead != offer.ExpectedHead ||
            scope.Space.Cursor != offer.PublicationCursor || scope.Space.PublicationHead != offer.PublicationHead) throw Refuse(409, "HouseholdOwnershipCheckpointChanged");
        return row;
    }
    private async Task DeviceProofAsync(HouseholdRequestScope scope, HouseholdOwnershipRow row, string signed, HouseholdOwnershipStage stage, string digest, CancellationToken ct)
    {
        var device = await scope.DeviceAsync(row.TargetDeviceId, includePending: false, ct) ?? throw Refuse(404, "HouseholdOwnershipTargetUnavailable");
        var proof = HouseholdMetadataProofs.Read(signed, HouseholdMetadataProofs.Public(new(device.SigningKey, device.WrappingKey)), HouseholdOwnershipProtocol.DeviceProofType,
            HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof);
        if (proof.Format != 1 || proof.SpaceId != row.SpaceId || proof.OperationId != row.OperationId || proof.DeviceId != row.TargetDeviceId || proof.IdentityId != row.TargetId ||
            proof.Stage != stage || proof.PayloadDigest != digest) throw Refuse(403, "HouseholdOwnershipTargetProofMismatch");
    }
    private async Task<HouseholdMembershipStatement> MembershipAsync(HouseholdRequestScope scope, CancellationToken ct)
    {
        var row = await db.Set<HouseholdMembershipEventRow>().SingleAsync(value => value.SpaceId == scope.Space.Id && value.Version == scope.Space.MembershipVersion, ct);
        return HouseholdMetadataProofs.Membership(row.SignedEvent, HouseholdMetadataProofs.Public(new(row.SignerSigningKey, row.SignerWrappingKey)));
    }
    private static HouseholdOwnershipOffer Offer(HouseholdRequestScope scope, HouseholdOwnershipRow row) => HouseholdMetadataProofs.Read(row.SignedOffer,
        HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)), HouseholdOwnershipProtocol.OfferType, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
    /// <summary>Operational expiry runs under the original room lock before resource authorization, so a real subsequent request can commit the unfreeze.</summary>
    internal async Task ExpirePendingAsync(HouseholdRequestScope scope, CancellationToken ct) => _ = await ActiveAsync(scope, ct);

    private async Task ExpireAsync(HouseholdRequestScope scope, HouseholdOwnershipRow row, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (row.Stage is >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted && row.ExpiresAt <= scope.Now)
        { row.Stage = HouseholdOwnershipStage.Expired; await UnfreezeAsync(scope, row, ct); }
    }
    private async Task UnfreezeAsync(HouseholdRequestScope scope, HouseholdOwnershipRow row, CancellationToken ct)
    {
        if (scope.Space.OwnerId != row.OwnerId) return;
        var offer = Offer(scope, row);
        if (scope.Space.MembershipVersion != offer.ExpectedVersion || scope.Space.MembershipHead != offer.ExpectedHead) return;
        var membership = await MembershipAsync(scope, ct);
        var identities = membership.Members.Select(member => member.IdentityId).ToArray();
        var devices = membership.Members.SelectMany(member => member.Devices).Select(device => device.Id).ToArray();
        // A simultaneous departure/revocation needs its own security rotation. Expiring or
        // canceling a transfer must never unfreeze writes under a key known to that recipient.
        if (await db.Set<HouseholdMemberRow>().AnyAsync(member => member.SpaceId == scope.Space.Id && identities.Contains(member.UserId) && (member.Revoked || !member.Accepted), ct) ||
            await db.Set<HouseholdDeviceRow>().AnyAsync(device => device.SpaceId == scope.Space.Id && devices.Contains(device.Id) && (device.Revoked || !device.Approved), ct)) return;
        scope.Space.RotationRequired = false;
    }
    private static HouseholdOwnershipObservation Observe(HouseholdOwnershipRow row)
    {
        var active = row.Stage is >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted;
        return new(row.OperationId, row.Stage, row.SignedOffer, active && row.PreparationJson is not null ?
            JsonSerializer.Deserialize(row.PreparationJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipPreparation) : null, row.SignedStatement,
            active && row.AcceptanceJson is not null ? JsonSerializer.Deserialize(row.AcceptanceJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipAcceptance) : null,
            row.PublishedProofJson is null ? null : JsonSerializer.Deserialize(row.PublishedProofJson, HouseholdMetadataJsonContext.Default.HouseholdMembershipProof), row.AnchorVersion);
    }
    private static HouseholdOwnershipObservation Empty() => new(null, HouseholdOwnershipStage.None, null, null, null, null, null);
    private static void Bound(string value) { if (System.Text.Encoding.UTF8.GetByteCount(value) > HouseholdOwnershipProtocol.MaximumBytes) throw Refuse(413, "HouseholdOwnershipMetadataBound"); }
    private static HouseholdProtocolRefusal Refuse(int status, string code) => new(status, code);
}
