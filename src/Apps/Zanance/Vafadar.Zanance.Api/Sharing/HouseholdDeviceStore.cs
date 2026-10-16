using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Real account/session plus private-key possession, separate from owner-root approval and recipient key grants.</summary>
internal sealed class HouseholdDeviceStore(ServiceDbContext db)
{
    private const string EnrollmentType = "zanance-household-device-enrollment+jwt";
    private const string SessionType = "zanance-household-device-session+jwt";

    /// <summary>Registers only this actual accepted member's key; a pending device receives no financial or epoch-key resources.</summary>
    internal async Task<HouseholdDeviceObservation> EnrollAsync(HouseholdRequestScope scope, HouseholdEnrollDevice input, CancellationToken ct)
    {
        var member = await scope.MemberAsync(includePending: false, ct);
        if (member is null || scope.Space.Deleted || scope.Retention != HouseholdRetentionState.Active)
            throw Refuse(404, "HouseholdMemberUnavailable");
        var key = HouseholdMetadataProofs.Public(input.DeviceKeys);
        if (key.Fingerprint == HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)).Fingerprint)
            throw Refuse(400, "HouseholdIndependentDeviceRequired");
        var proof = Possession(scope, input.SignedPossession, key, Guid.Empty, EnrollmentType);
        var prior = await db.Set<HouseholdDeviceRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id &&
            row.UserId == scope.ActorId && row.Fingerprint == key.Fingerprint, ct);
        if (prior is not null)
        {
            if (prior.Revoked) throw Refuse(410, "HouseholdFreshDeviceKeyRequired");
            if (prior.AdmissionOperationId != proof.OperationId)
                throw Refuse(409, "HouseholdExistingDeviceRebindRequired");
            if (prior.SessionId != scope.SessionId || prior.AdmissionProof != input.SignedPossession)
            {
                RequireFresh(proof, scope.Now);
                // Only the same original private key/operation can resume pending enrollment.
                // Keep its first admission proof for independent owner review. Approved devices
                // still require the separate current-session rebind before accessing resources.
                if (!prior.Approved) prior.SessionId = scope.SessionId;
            }
            return Observation(prior);
        }
        RequireFresh(proof, scope.Now);
        if (await db.Set<HouseholdDeviceRow>().CountAsync(row => row.SpaceId == scope.Space.Id && row.UserId == scope.ActorId && !row.Revoked, ct) >= HouseholdLimits.DevicesPerIdentity)
            throw Refuse(413, "HouseholdDeviceBound");
        var device = new HouseholdDeviceRow { Id = Guid.NewGuid(), SpaceId = scope.Space.Id, UserId = scope.ActorId,
            SessionId = scope.SessionId, Fingerprint = key.Fingerprint, SigningKey = input.DeviceKeys.SigningKey, WrappingKey = input.DeviceKeys.WrappingKey,
            AdmissionProof = input.SignedPossession, AdmissionOperationId = proof.OperationId, LastSeenAt = scope.Now };
        db.Add(device); return Observation(device);
    }

    /// <summary>Rebinds an existing exact accepted-member device key after private possession against the actual session; pending devices remain unapproved.</summary>
    internal async Task<HouseholdDeviceObservation> RebindAsync(HouseholdRequestScope scope, Guid deviceId, HouseholdRebindDevice input, CancellationToken ct)
    {
        var member = await scope.MemberAsync(includePending: false, ct);
        if (member is null || scope.Space.Deleted || scope.Retention == HouseholdRetentionState.Deleted)
            throw Refuse(404, "HouseholdMemberUnavailable");
        // Existing private-key possession may reattach a session for lifecycle/security
        // metadata after expiry. Every financial/key request still independently checks
        // retention; this neither admits a new device nor reactivates financial access.
        var device = await db.Set<HouseholdDeviceRow>().SingleOrDefaultAsync(row => row.Id == deviceId && row.SpaceId == scope.Space.Id &&
            row.UserId == scope.ActorId && !row.Revoked, ct) ?? throw Refuse(404, "HouseholdResourceUnavailable");
        var proof = Possession(scope, input.SignedPossession, HouseholdMetadataProofs.Public(new(device.SigningKey, device.WrappingKey)), deviceId, SessionType);
        if (device.SessionOperationId == proof.OperationId)
        {
            if (device.SessionProof != input.SignedPossession || device.SessionId != scope.SessionId) throw Refuse(409, "AlteredHouseholdDeviceSession");
            return Observation(device);
        }
        RequireFresh(proof, scope.Now);
        var changedSession = device.SessionId != scope.SessionId;
        device.SessionId = scope.SessionId; device.SessionOperationId = proof.OperationId;
        device.SessionProof = input.SignedPossession; device.LastSeenAt = scope.Now;
        if (changedSession && device.Approved)
            await new HouseholdAuditStore(db).AppendAsync(scope, HouseholdAuditAction.DeviceRebound, deviceId, proof.OperationId, null, ct);
        return Observation(device);
    }

    /// <summary>The expected signer comes from the original stored key or explicit new-key proof, never from an unauthenticated header lookup.</summary>
    private static HouseholdDeviceSessionStatement Possession(HouseholdRequestScope scope, string signed, PublicSigningRecipientIdentity key,
        Guid expectedDevice, string type)
    {
        var value = HouseholdMetadataProofs.Read(signed, key, type, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
        if (value.Format != 1 || value.SpaceId != scope.Space.Id || value.DeviceId != expectedDevice || value.OperationId == Guid.Empty ||
            value.IdentityId != scope.ActorId || value.SessionId != scope.SessionId || value.DeviceKeys != new HouseholdPublicKeys(key.SigningKey, key.WrappingKey))
            throw Refuse(403, "HouseholdDevicePossessionMismatch");
        return value;
    }
    /// <summary>A prior successful exact proof can be replayed as acknowledgement; a new operation requires a bounded fresh proposal.</summary>
    private static void RequireFresh(HouseholdDeviceSessionStatement proof, DateTimeOffset now)
    {
        if (proof.ValidUntilUnixSeconds <= now.ToUnixTimeSeconds() || proof.ValidUntilUnixSeconds > now.Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds())
            throw Refuse(409, "HouseholdDeviceProposalExpiredOrFuture");
    }
    /// <summary>Public-only observation without session credentials or key material.</summary>
    private static HouseholdDeviceObservation Observation(HouseholdDeviceRow device) => new(device.Id, device.UserId, device.Fingerprint, device.Approved, device.Revoked);
    /// <summary>Content-free failure, without signed requests, keys or session credentials.</summary>
    private static HouseholdProtocolRefusal Refuse(int status, string code) => new(status, code);
}
