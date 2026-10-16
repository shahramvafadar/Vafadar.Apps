using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Actual two-sided enrollment and serial owner-root authority. This store never receives a clear epoch or financial key.</summary>
internal sealed class HouseholdMembershipStore(ServiceDbContext db)
{
    /// <summary>Reserves original room/device ids under the actual owner's session lock; no financial access exists before genesis.</summary>
    internal async Task<HouseholdSpaceReservation> ReserveAsync(SyncRequestScope scope, HouseholdReserveSpace input, CancellationToken ct)
    {
        if (input.OperationId == Guid.Empty || !HouseholdProtectionContext.IsFingerprint(input.SchemaHash))
            throw Refuse(400, "InvalidHouseholdReservation");
        var root = HouseholdMetadataProofs.Public(input.OwnerKeys); var deviceKey = HouseholdMetadataProofs.Public(input.DeviceKeys);
        if (root.Fingerprint == deviceKey.Fingerprint) throw Refuse(400, "HouseholdIndependentOwnerRootRequired");
        var prior = await db.Set<HouseholdSpaceRow>().SingleOrDefaultAsync(row => row.OwnerId == scope.Owner && row.CreationOperationId == input.OperationId, ct);
        if (prior is not null)
        {
            var device = await db.Set<HouseholdDeviceRow>().SingleOrDefaultAsync(row => row.SpaceId == prior.Id && row.UserId == scope.Owner && row.Fingerprint == deviceKey.Fingerprint, ct);
            if (prior.Deleted || prior.SchemaHash != input.SchemaHash || prior.OwnerSigningKey != input.OwnerKeys.SigningKey ||
                prior.OwnerWrappingKey != input.OwnerKeys.WrappingKey || device is null || device.SessionId != scope.Session || device.Revoked)
                throw Refuse(409, "AlteredHouseholdReservation");
            return new(prior.Id, device.Id, scope.Owner, prior.MembershipVersion > 0);
        }
        // This technical bound is not a verified purchase/subscription grant. Public activation requires separate authority.
        if (await db.Set<HouseholdSpaceRow>().AnyAsync(row => row.OwnerId == scope.Owner && !row.Deleted, ct))
            throw Refuse(413, "HouseholdHostedSpaceBound");
        var room = new HouseholdSpaceRow { Id = Guid.NewGuid(), OwnerId = scope.Owner, CreationOperationId = input.OperationId,
            SchemaHash = input.SchemaHash, OwnerSigningKey = input.OwnerKeys.SigningKey, OwnerWrappingKey = input.OwnerKeys.WrappingKey,
            MembershipHead = new('0', 64), PublicationHead = new('0', 64), CreatedAt = scope.Now };
        var first = new HouseholdDeviceRow { Id = Guid.NewGuid(), SpaceId = room.Id, UserId = scope.Owner, SessionId = scope.Session,
            Fingerprint = deviceKey.Fingerprint, SigningKey = input.DeviceKeys.SigningKey, WrappingKey = input.DeviceKeys.WrappingKey, LastSeenAt = scope.Now };
        db.Add(room); db.Add(new HouseholdMemberRow { SpaceId = room.Id, UserId = scope.Owner, Role = HouseholdRole.Owner }); db.Add(first);
        return new(room.Id, first.Id, scope.Owner, false);
    }

    /// <summary>Reserves a finite unexpired seat with a pinned-root signature; no recipient obtains encrypted data at this stage.</summary>
    internal async Task<Guid> InviteAsync(HouseholdRequestScope scope, HouseholdCreateInvitation input, CancellationToken ct)
    {
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.Invite, ct);
        var value = HouseholdMetadataProofs.Read(input.SignedInvitation, Root(scope.Space), HouseholdMetadataProofs.InvitationType,
            HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
        if (value.Format != 1 || value.SpaceId != scope.Space.Id || value.InvitationId == Guid.Empty || value.OperationId == Guid.Empty ||
            value.Role is not (HouseholdRole.Editor or HouseholdRole.Viewer) || !HouseholdProtectionContext.IsFingerprint(value.CapabilityHash))
            throw Refuse(400, "InvalidHouseholdInvitation");
        var prior = await db.Set<HouseholdInvitationRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.OperationId == value.OperationId, ct);
        if (prior is not null)
        {
            if (prior.SignedInvitation != input.SignedInvitation) throw Refuse(409, "AlteredHouseholdInvitation");
            if (prior.Revoked || prior.ExpiresAt <= scope.Now && !prior.Accepted) throw Refuse(410, "HouseholdInvitationUnavailable");
            return prior.Id;
        }
        if (value.MembershipVersion != scope.Space.MembershipVersion || value.MembershipHead != scope.Space.MembershipHead ||
            value.ExpiresUnixSeconds <= scope.Now.ToUnixTimeSeconds() ||
            value.ExpiresUnixSeconds > scope.Now.Add(HouseholdLimits.InvitationLifetime).ToUnixTimeSeconds())
            throw Refuse(409, "HouseholdInvitationCheckpointOrExpiry");
        var accepted = await db.Set<HouseholdMemberRow>().CountAsync(row => row.SpaceId == scope.Space.Id && row.Accepted && !row.Revoked, ct);
        var reserved = await db.Set<HouseholdInvitationRow>().CountAsync(row => row.SpaceId == scope.Space.Id && !row.Accepted && !row.Revoked && row.ExpiresAt > scope.Now, ct);
        if (accepted + reserved >= QuotaPolicy.SharedSpaceSeatLimit) throw Refuse(413, "HouseholdSeatBound");
        if (await db.Set<HouseholdInvitationRow>().AnyAsync(row => row.Id == value.InvitationId, ct)) throw Refuse(409, "HouseholdInvitationIdUnavailable");
        db.Add(new HouseholdInvitationRow { Id = value.InvitationId, SpaceId = scope.Space.Id, Role = value.Role,
            OperationId = value.OperationId, CapabilityHash = value.CapabilityHash, ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(value.ExpiresUnixSeconds),
            SignedInvitation = input.SignedInvitation });
        return value.InvitationId;
    }

    /// <summary>Records actual authenticated recipient consent and possession of the new private signing key, but still grants no financial access.</summary>
    internal async Task<HouseholdInvitationClaimResult> ClaimAsync(HouseholdRequestScope scope, Guid invitationId,
        HouseholdClaimInvitation input, CancellationToken ct)
    {
        if (scope.Space.Deleted || scope.Retention != HouseholdRetentionState.Active || scope.Space.RotationRequired)
            throw Refuse(404, "HouseholdInvitationUnavailable");
        var invitation = await db.Set<HouseholdInvitationRow>().SingleOrDefaultAsync(row => row.Id == invitationId && row.SpaceId == scope.Space.Id && !row.Revoked, ct)
            ?? throw Refuse(404, "HouseholdInvitationUnavailable");
        var key = HouseholdMetadataProofs.Public(input.DeviceKeys);
        var proof = HouseholdMetadataProofs.Read(input.SignedClaim, key, HouseholdMetadataProofs.ClaimType,
            HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement);
        byte[] capability;
        try
        {
            if (input.Capability is null || input.Capability.Length != 64 || input.Capability.Any(character => !char.IsAsciiHexDigitLower(character)))
                throw Refuse(404, "HouseholdInvitationUnavailable");
            capability = Convert.FromHexString(input.Capability);
        }
        catch (FormatException) { throw Refuse(404, "HouseholdInvitationUnavailable"); }
        var capabilityHash = Convert.ToHexStringLower(SHA256.HashData(capability)); CryptographicOperations.ZeroMemory(capability);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(capabilityHash), Encoding.ASCII.GetBytes(invitation.CapabilityHash)) ||
            proof.Format != 1 || proof.SpaceId != scope.Space.Id || proof.InvitationId != invitationId || proof.OperationId == Guid.Empty ||
            proof.IdentityId != scope.ActorId || proof.DeviceKeys != input.DeviceKeys ||
            proof.InvitationHash != HouseholdMetadataProofs.Digest(invitation.SignedInvitation, HouseholdLimits.MembershipBytes * 2) ||
            proof.OwnerFingerprint != Root(scope.Space).Fingerprint)
            throw Refuse(404, "HouseholdInvitationUnavailable");
        if (invitation.ClaimedUserId is not null)
        {
            if (invitation.ClaimedUserId != scope.ActorId || invitation.ClaimedFingerprint != key.Fingerprint ||
                invitation.SignedClaim != input.SignedClaim || invitation.ClaimOperationId != proof.OperationId || invitation.ClaimedDeviceId is not { } claimed)
                throw Refuse(409, "HouseholdInvitationAlreadyClaimed");
            var prior = await scope.DeviceAsync(claimed, includePending: true, ct);
            if (prior is null) throw Refuse(404, "HouseholdInvitationUnavailable");
            if (!invitation.Accepted && invitation.ExpiresAt <= scope.Now) throw Refuse(410, "HouseholdInvitationUnavailable");
            return new(invitationId, claimed, scope.ActorId, key.Fingerprint, invitation.Role, invitation.Accepted);
        }
        if (invitation.Accepted || invitation.ExpiresAt <= scope.Now) throw Refuse(410, "HouseholdInvitationUnavailable");
        if (await db.Set<HouseholdMemberRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && row.UserId == scope.ActorId && !row.Revoked, ct))
            throw Refuse(409, "HouseholdIdentityAlreadyEnrolled");
        // A revoked identity cannot reclaim old authority by reusing its old row/key. A fresh invitation may
        // re-enroll it only with explicit owner acceptance; all previous devices remain revoked.
        var member = await db.Set<HouseholdMemberRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.UserId == scope.ActorId, ct);
        if (member is null) { member = new() { SpaceId = scope.Space.Id, UserId = scope.ActorId }; db.Add(member); }
        member.Role = invitation.Role; member.Accepted = false; member.Revoked = false;
        if (await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && row.UserId == scope.ActorId && row.Fingerprint == key.Fingerprint, ct))
            throw Refuse(409, "HouseholdFreshDeviceKeyRequired");
        var device = new HouseholdDeviceRow { Id = Guid.NewGuid(), SpaceId = scope.Space.Id, UserId = scope.ActorId, SessionId = scope.SessionId,
            Fingerprint = key.Fingerprint, SigningKey = input.DeviceKeys.SigningKey, WrappingKey = input.DeviceKeys.WrappingKey, LastSeenAt = scope.Now };
        db.Add(device); invitation.ClaimedUserId = scope.ActorId; invitation.ClaimedFingerprint = key.Fingerprint;
        invitation.SignedClaim = input.SignedClaim; invitation.ClaimOperationId = proof.OperationId; invitation.ClaimedDeviceId = device.Id;
        return new(invitationId, device.Id, scope.ActorId, key.Fingerprint, invitation.Role, false);
    }

    /// <summary>Commits one exact root-signed roster transition plus complete fresh recipient grants; no partial role/key publication is visible.</summary>
    internal async Task<HouseholdCheckpoint> PublishAsync(HouseholdRequestScope scope, HouseholdPublishMembership input, CancellationToken ct,
        bool recoveryDevicePossessionVerified = false)
    {
        var first = scope.Space.MembershipVersion == 0;
        // Actual owner/resource ownership precedes even public-key parsing. Knowing another room id
        // must not authorize expensive proof handling or expose its current signature interpretation.
        _ = await scope.RequireOwnerPendingDeviceAsync(input.DeviceId, ct);
        var requestHash = RequestHash(input);
        // A committed root change is no longer signed by the current root. Compare original complete
        // bytes only after real current owner/device authorization, before attempting current-root verification.
        var replay = await db.Set<HouseholdMembershipEventRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.RequestHash == requestHash, ct);
        if (replay is not null) return Checkpoint(scope);
        var trustedRoot = Root(scope.Space);
        // The exact signed action selects its permission. Security revocation/departure confirmation
        // stays possible during grace/retention, while invitations and transfers require active hosting.
        var next = HouseholdMetadataProofs.Membership(input.SignedStatement, trustedRoot);
        if (next.Change == HouseholdMembershipChange.RecoverOwner)
        {
            if (!recoveryDevicePossessionVerified) throw Refuse(409, "HouseholdRecoveryDevicePossessionRequired");
            await new HouseholdRecoveryStore(db).VerifyRecoveryAsync(scope, input, next, ValidateGrants(next, input.Grants), ct);
        }
        else if (input.RecoveryEvidence is not null || recoveryDevicePossessionVerified) throw Refuse(400, "UnexpectedHouseholdRecoveryEvidence");
        if (!first && next.Change != HouseholdMembershipChange.RecoverOwner) _ = await scope.RequireAsync(input.DeviceId,
            next.Change is HouseholdMembershipChange.RevokeMember or HouseholdMembershipChange.RevokeDevice or HouseholdMembershipChange.ConfirmDeparture
                ? HouseholdRemoteOperation.Revoke : HouseholdRemoteOperation.Invite, ct);
        if (next.SpaceId != scope.Space.Id || next.PreviousOwnerId != scope.ActorId || next.ExpectedVersion != scope.Space.MembershipVersion)
            throw Refuse(409, "HouseholdMembershipCheckpointMismatch");
        if (next.PublicationCursor != scope.Space.Cursor || next.PublicationHead != scope.Space.PublicationHead)
            throw Refuse(409, "HouseholdMembershipFinancialBoundaryChanged");
        HouseholdMetadataProofs.RequireCurrentProposal(next, scope.Now);
        HouseholdMembershipStatement? previous = null;
        if (!first)
        {
            var row = await db.Set<HouseholdMembershipEventRow>().SingleAsync(row => row.SpaceId == scope.Space.Id && row.Version == scope.Space.MembershipVersion, ct);
            previous = HouseholdMetadataProofs.Membership(row.SignedEvent, HouseholdMetadataProofs.Public(new(row.SignerSigningKey, row.SignerWrappingKey)));
            if (row.Hash != scope.Space.MembershipHead || row.Hash != HouseholdMetadataProofs.Digest(row.SignedEvent, HouseholdLimits.MembershipBytes * 2))
                throw Refuse(409, "HouseholdMembershipStorageMismatch");
        }
        if (HouseholdMembershipRules.ValidateContinuity(next, previous, scope.Space.MembershipHead).Count != 0 ||
            HouseholdMembershipRules.ValidateRosterTransition(next, previous).Count != 0)
            throw Refuse(409, "HouseholdMembershipTransitionMismatch");
        if (await db.Set<HouseholdMembershipEventRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && row.OperationId == next.OperationId, ct))
            throw Refuse(409, "AlteredHouseholdMembershipOperation");
        var members = await db.Set<HouseholdMemberRow>().Where(row => row.SpaceId == scope.Space.Id).ToDictionaryAsync(row => row.UserId, ct);
        var devices = await db.Set<HouseholdDeviceRow>().Where(row => row.SpaceId == scope.Space.Id).ToDictionaryAsync(row => row.Id, ct);
        HouseholdInvitationRow? acceptedInvitation = null;
        if (next.Change == HouseholdMembershipChange.AcceptMember)
        {
            acceptedInvitation = await db.Set<HouseholdInvitationRow>().SingleOrDefaultAsync(row => row.Id == next.InvitationId && row.SpaceId == scope.Space.Id &&
                !row.Accepted && !row.Revoked && row.ExpiresAt > scope.Now, ct) ?? throw Refuse(409, "HouseholdInvitationUnavailable");
            var accepted = next.Members.Single(member => !previous!.Members.Any(old => old.IdentityId == member.IdentityId));
            if (acceptedInvitation.ClaimedUserId != accepted.IdentityId || acceptedInvitation.ClaimedDeviceId != accepted.Devices[0].Id ||
                acceptedInvitation.Role != accepted.Role || acceptedInvitation.SignedClaim != input.SignedAcceptanceClaim || input.SignedAcceptanceClaim is null ||
                accepted.AcceptanceHash != HouseholdMetadataProofs.Digest(input.SignedAcceptanceClaim, HouseholdLimits.MembershipBytes * 2))
                throw Refuse(409, "HouseholdRecipientConsentRequired");
        }
        else if (input.SignedAcceptanceClaim is not null) throw Refuse(400, "UnexpectedHouseholdRecipientConsent");
        if (first && (next.OwnerId != scope.ActorId || next.OwnerKeys != new HouseholdPublicKeys(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey) ||
            next.Members[0].Devices[0].Id != input.DeviceId)) throw Refuse(409, "HouseholdBootstrapDeviceMismatch");
        if (next.Change == HouseholdMembershipChange.ConfirmDeparture && previous!.Members.Where(old => !next.Members.Any(member => member.IdentityId == old.IdentityId))
            .Any(old => !members[old.IdentityId].Revoked)) throw Refuse(409, "HouseholdDepartureNotRequested");
        foreach (var member in next.Members)
        {
            if (!members.TryGetValue(member.IdentityId, out var stored) || stored.Revoked)
                throw Refuse(409, "HouseholdMemberConsentUnavailable");
            foreach (var device in member.Devices)
                if (!devices.TryGetValue(device.Id, out var storedDevice) || storedDevice.UserId != member.IdentityId || storedDevice.Revoked ||
                    device.Keys != new HouseholdPublicKeys(storedDevice.SigningKey, storedDevice.WrappingKey))
                    throw Refuse(409, "HouseholdDeviceConsentUnavailable");
        }
        var grantsHash = ValidateGrants(next, input.Grants);
        if (previous is not null && next.OwnerKeys != previous.OwnerKeys)
        {
            if (input.TransferAcceptance is null) throw Refuse(409, "HouseholdNewOwnerConsentRequired");
            var acceptance = HouseholdMetadataProofs.Read(input.TransferAcceptance, HouseholdMetadataProofs.Public(next.OwnerKeys), HouseholdMetadataProofs.TransferType,
                HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement);
            if (acceptance.Format != 1 || acceptance.SpaceId != next.SpaceId || acceptance.OperationId != next.OperationId ||
                acceptance.MembershipStatementHash != HouseholdMetadataProofs.Digest(input.SignedStatement, HouseholdLimits.MembershipBytes * 2) || acceptance.GrantsHash != grantsHash)
                throw Refuse(409, "HouseholdNewOwnerConsentMismatch");
            if (next.Change == HouseholdMembershipChange.TransferOwner && await db.Set<HouseholdSpaceRow>().AnyAsync(row => row.OwnerId == next.OwnerId && row.Id != scope.Space.Id && !row.Deleted, ct))
                throw Refuse(413, "HouseholdHostedSpaceBound");
        }
        else if (input.TransferAcceptance is not null) throw Refuse(400, "UnexpectedHouseholdTransferConsent");
        await new HouseholdOwnershipStore(db).BeforePublicationAsync(scope, next, input, ct);
        var recovery = new HouseholdRecoveryStore(db);
        await recovery.ApplyAsync(scope, input, previous, next, ct);
        await recovery.ApplyRootGrantsAsync(input, previous, next, ct);
        if (previous is not null && previous.OwnerKeys != next.OwnerKeys)
        {
            // A retired signing root cannot keep reserving seats through old unaccepted
            // invitations. Self-enrolled accepted-member devices have a separate proof/path.
            var expiredDevices = db.Set<HouseholdInvitationRow>().Where(row => row.SpaceId == next.SpaceId && !row.Accepted && !row.Revoked && row.ClaimedDeviceId != null)
                .Select(row => row.ClaimedDeviceId!.Value);
            await db.Set<HouseholdDeviceRow>().Where(row => row.SpaceId == next.SpaceId && !row.Approved && expiredDevices.Contains(row.Id))
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Revoked, true), ct);
            await db.Set<HouseholdInvitationRow>().Where(row => row.SpaceId == next.SpaceId && !row.Accepted && !row.Revoked)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Revoked, true), ct);
        }
        // Demote the old owner first to honor PostgreSQL's immediate unique active-owner index.
        if (!first && next.OwnerId != scope.Space.OwnerId)
        { members[scope.Space.OwnerId].Role = HouseholdRole.Editor; await db.SaveChangesAsync(ct); }
        foreach (var stored in members.Values)
        {
            var selected = next.Members.SingleOrDefault(member => member.IdentityId == stored.UserId);
            // An unrelated roster publication must not revoke an unaccepted original claim.
            // Only a formerly accepted member omitted from this signed roster is being removed.
            if (selected is null && stored.Accepted) { stored.Revoked = true; stored.Accepted = false; }
            if (selected is not null) { stored.Revoked = false; stored.Accepted = true; }
            if (selected is not null) { stored.Role = selected.Role; if (stored.JoinedVersion == 0) stored.JoinedVersion = next.Version; }
        }
        var activeDevices = next.Members.SelectMany(member => member.Devices).Select(device => device.Id).ToHashSet();
        foreach (var stored in devices.Values)
        {
            if (activeDevices.Contains(stored.Id)) { stored.Approved = true; stored.Revoked = false; }
            else if (stored.Approved || previous?.Members.Any(member => member.IdentityId == stored.UserId) == true &&
                !next.Members.Any(member => member.IdentityId == stored.UserId) ||
                next.Change == HouseholdMembershipChange.RecoverOwner && stored.UserId == next.OwnerId)
            { stored.Revoked = true; stored.Approved = false; }
        }
        foreach (var grant in input.Grants)
        {
            var prior = await db.Set<HouseholdKeyGrantRow>().SingleOrDefaultAsync(row => row.SpaceId == next.SpaceId && row.Epoch == grant.Epoch && row.DeviceId == grant.DeviceId, ct);
            if (prior is not null)
            {
                if (prior.Ciphertext != grant.Ciphertext || prior.Digest != grant.Digest) throw Refuse(409, "AlteredHouseholdHistoricalGrant");
            }
            else db.Add(new HouseholdKeyGrantRow { SpaceId = next.SpaceId, Epoch = grant.Epoch, DeviceId = grant.DeviceId,
                MembershipVersion = next.Version, Ciphertext = grant.Ciphertext, Digest = grant.Digest });
        }
        var eventHash = HouseholdMetadataProofs.Digest(input.SignedStatement, HouseholdLimits.MembershipBytes * 2);
        db.Add(new HouseholdMembershipEventRow { SpaceId = next.SpaceId, Version = next.Version, PreviousHash = next.PreviousHash, Hash = eventHash,
            SignedEvent = input.SignedStatement, SignerSigningKey = trustedRoot.SigningKey, SignerWrappingKey = trustedRoot.WrappingKey,
            RequestHash = requestHash, GrantsHash = grantsHash, TransferAcceptance = input.TransferAcceptance, SignedAcceptanceClaim = input.SignedAcceptanceClaim,
            RecoveryEvidenceJson = input.RecoveryEvidence is { } evidence ? JsonSerializer.Serialize(evidence, HouseholdMetadataJsonContext.Default.HouseholdRecoveryEvidence) : null,
            ActorId = scope.ActorId, OperationId = next.OperationId, CreatedAt = scope.Now });
        if (acceptedInvitation is not null) acceptedInvitation.Accepted = true;
        if (next.OwnerId != scope.Space.OwnerId)
            foreach (var invitation in await db.Set<HouseholdInvitationRow>().Where(row => row.SpaceId == next.SpaceId && !row.Accepted && !row.Revoked).ToArrayAsync(ct))
                invitation.Revoked = true;
        scope.Space.OwnerId = next.OwnerId; scope.Space.OwnerSigningKey = next.OwnerKeys.SigningKey; scope.Space.OwnerWrappingKey = next.OwnerKeys.WrappingKey;
        scope.Space.MembershipVersion = next.Version; scope.Space.MembershipHead = eventHash; scope.Space.KeyEpoch = next.KeyEpoch; scope.Space.RotationRequired = false;
        await new HouseholdOwnershipStore(db).PublishedAsync(scope, next, input, ct);
        await new HouseholdAuditStore(db).AppendAsync(scope, HouseholdAuditAction.MembershipPublished, input.DeviceId, next.OperationId,
            new(new(input.SignedStatement, input.TransferAcceptance, input.SignedAcceptanceClaim, grantsHash, input.RecoveryEvidence)), ct);
        return Checkpoint(scope);
    }

    /// <summary>Immediate explicit departure revokes all this member's devices and freezes financial writes until a fresh root-signed epoch excludes it.</summary>
    internal async Task LeaveAsync(HouseholdRequestScope scope, Guid deviceId, CancellationToken ct)
    {
        _ = await scope.RequireAsync(deviceId, HouseholdRemoteOperation.Leave, ct);
        var member = await scope.MemberAsync(includePending: false, ct) ?? throw Refuse(404, "HouseholdResourceUnavailable");
        member.Revoked = true;
        foreach (var device in await db.Set<HouseholdDeviceRow>().Where(row => row.SpaceId == scope.Space.Id && row.UserId == scope.ActorId).ToArrayAsync(ct))
            device.Revoked = true;
        scope.Space.RotationRequired = true;
        await new HouseholdAuditStore(db).AppendAsync(scope, HouseholdAuditAction.MemberLeft, deviceId, Guid.NewGuid(), null, ct);
    }

    /// <summary>Checkpoint metadata is never a substitute for recipient verification of its pinned signed chain.</summary>
    internal static HouseholdCheckpoint Checkpoint(HouseholdRequestScope scope) => new(scope.Space.Id, scope.Space.OwnerId,
        scope.Space.MembershipVersion, scope.Space.MembershipHead, scope.Space.KeyEpoch, scope.Space.Cursor, scope.Space.PublicationHead,
        scope.Space.RotationRequired, scope.Retention, scope.Now.ToUnixTimeSeconds(), scope.Space.HostExpiresAt?.ToUnixTimeSeconds());

    /// <summary>Exact bounded whole request distinguishes an immutable successful retry from altered grant/acceptance bytes.</summary>
    internal static string RequestHash(HouseholdPublishMembership input)
    {
        if (input.Recovery is { } recovery && (recovery.Epochs is null || recovery.Epochs.Length > HouseholdRecoveryBounds.PageSize ||
            recovery.Epochs.Any(row => row is null || row.Ciphertext is null || row.Ciphertext.Length > HouseholdRecoveryBounds.EpochCiphertextCharacters) ||
            recovery.SignedEnrollment?.Length > HouseholdRecoveryBounds.MetadataCharacters || recovery.RootCiphertext?.Length > HouseholdRecoveryBounds.RootCiphertextCharacters) ||
            input.RecoveryEvidence is { } evidence && (evidence.SignedEnrollment is null || evidence.SignedConsent is null ||
                evidence.SignedEnrollment.Length > HouseholdRecoveryBounds.MetadataCharacters || evidence.SignedConsent.Length > HouseholdRecoveryBounds.MetadataCharacters) ||
            input.OwnerRootGrants is { } roots && (roots.Length > HouseholdLimits.DevicesPerIdentity || roots.Any(row => row is null ||
                row.Ciphertext is null || row.Ciphertext.Length > HouseholdRecoveryBounds.RootCiphertextCharacters)))
            throw Refuse(413, "HouseholdMembershipRequestBound");
        if (input.Grants is null || input.Grants.Length is < 1 or > 384 || input.Grants.Any(grant => grant is null || grant.Ciphertext is null || grant.Ciphertext.Length > 32768) ||
            input.SignedStatement is null || input.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2 ||
            input.TransferAcceptance?.Length > HouseholdLimits.MembershipBytes * 2 || input.SignedAcceptanceClaim?.Length > HouseholdLimits.MembershipBytes * 2 ||
            input.Grants.Sum(grant => (long)grant.Ciphertext.Length) > 4 * 1024 * 1024)
            throw Refuse(413, "HouseholdMembershipRequestBound");
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership)));
    }

    /// <summary>Requires exactly one current-epoch grant per approved device, plus bounded explicit historical grants only to those recipients.</summary>
    internal static string ValidateGrants(HouseholdMembershipStatement next, HouseholdEncryptedGrant[] grants)
    {
        var approved = next.Members.SelectMany(member => member.Devices).Select(device => device.Id).ToHashSet();
        var seen = new HashSet<(long, Guid)>();
        foreach (var grant in grants)
        {
            if (grant.Epoch < 1 || grant.Epoch > next.KeyEpoch || !approved.Contains(grant.DeviceId) || !seen.Add((grant.Epoch, grant.DeviceId)) ||
                !HouseholdProtectionContext.IsFingerprint(grant.Digest)) throw Refuse(400, "InvalidHouseholdRecipientGrant");
            try { if (HouseholdPublicationDigest.Ciphertext(grant.Ciphertext) != grant.Digest) throw Refuse(400, "HouseholdGrantDigestMismatch"); }
            catch (ArgumentException) { throw Refuse(400, "InvalidHouseholdRecipientGrant"); }
        }
        if (approved.Any(device => !seen.Contains((next.KeyEpoch, device)))) throw Refuse(409, "IncompleteHouseholdCurrentGrants");
        return HouseholdPublicationDigest.Grants(grants);
    }

    /// <summary>Current stored public root, never chosen from an incoming proposal's advertised replacement.</summary>
    private static Vafadar.Cryptography.PublicSigningRecipientIdentity Root(HouseholdSpaceRow room) =>
        HouseholdMetadataProofs.Public(new(room.OwnerSigningKey, room.OwnerWrappingKey));
    /// <summary>Content-free refusal without invitation capabilities, cryptographic payloads or internal exception details.</summary>
    private static HouseholdProtocolRefusal Refuse(int status, string code) => new(status, code);
}
