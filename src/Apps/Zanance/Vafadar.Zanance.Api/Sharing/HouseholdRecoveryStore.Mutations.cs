using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Atomic recovery enrollment/root delegation under the original membership transaction and room lock.</summary>
internal sealed partial class HouseholdRecoveryStore
{
    /// <summary>Recovers the actual owner's room even when all eight old devices are lost; the new device exists only inside this atomic verified recovery.</summary>
    internal async Task<HouseholdCheckpoint> RecoverAsync(HouseholdRequestScope scope, HouseholdRecoverOwner input, CancellationToken ct)
    {
        await RequireOwnerIdentityAsync(scope, ct);
        if (input.Publication is null || input.SignedDevicePossession is not { Length: > 0 and <= HouseholdRecoveryBounds.MetadataCharacters })
            throw Refuse(400, "InvalidHouseholdRecoveryRequest");
        var publication = input.Publication;
        var requestHash = HouseholdMembershipStore.RequestHash(publication);
        var replay = await db.Set<HouseholdMembershipEventRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.RequestHash == requestHash, ct);
        if (replay is not null)
        {
            var committed = await scope.DeviceAsync(publication.DeviceId, includePending: false, ct);
            if (committed is null || committed.AdmissionProof != input.SignedDevicePossession || replay.ActorId != scope.ActorId)
                throw Refuse(409, "AlteredHouseholdRecoveryOperation");
            return HouseholdMembershipStore.Checkpoint(scope);
        }
        var next = HouseholdMetadataProofs.Membership(publication.SignedStatement,
            HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)));
        if (next.Change != HouseholdMembershipChange.RecoverOwner || next.OwnerId != scope.ActorId || next.SpaceId != scope.Space.Id ||
            next.ExpectedVersion != scope.Space.MembershipVersion || next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Length != 1)
            throw Refuse(409, "HouseholdRecoveryCheckpointMismatch");
        await VerifyRecoveryAsync(scope, publication, next, HouseholdMembershipStore.ValidateGrants(next, publication.Grants), ct);
        var device = next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Single();
        var key = HouseholdMetadataProofs.Public(device.Keys);
        var proof = HouseholdMetadataProofs.Read(input.SignedDevicePossession, key, HouseholdRecoveryProofTypes.DevicePossession,
            HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
        if (device.Id != publication.DeviceId || proof.Format != 1 || proof.SpaceId != next.SpaceId || proof.DeviceId != device.Id ||
            proof.OperationId != next.OperationId || proof.IdentityId != scope.ActorId || proof.SessionId != scope.SessionId || proof.DeviceKeys != device.Keys ||
            proof.ValidUntilUnixSeconds != next.ValidUntilUnixSeconds || next.Members.Any(member => member.Role != HouseholdRole.Owner &&
                member.Devices.Any(retained => retained.Id == device.Id)) || device.Keys.SigningKey == next.OwnerKeys.SigningKey || device.Keys.WrappingKey == next.OwnerKeys.WrappingKey)
            throw Refuse(403, "HouseholdRecoveryDevicePossessionMismatch");
        HouseholdMetadataProofs.RequireCurrentProposal(next, scope.Now);
        if (await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.Id == device.Id || row.SpaceId == scope.Space.Id && row.Fingerprint == key.Fingerprint, ct))
            throw Refuse(409, "HouseholdFreshRecoveryDeviceRequired");
        // No separate pending grant or ninth approved device is exposed: rollback removes this row,
        // and the complete signed roster revokes every prior owner device before commit.
        db.Add(new HouseholdDeviceRow { Id = device.Id, SpaceId = scope.Space.Id, UserId = scope.ActorId, SessionId = scope.SessionId,
            Fingerprint = key.Fingerprint, SigningKey = device.Keys.SigningKey, WrappingKey = device.Keys.WrappingKey,
            AdmissionProof = input.SignedDevicePossession, AdmissionOperationId = next.OperationId, LastSeenAt = scope.Now });
        await db.SaveChangesAsync(ct);
        return await new HouseholdMembershipStore(db).PublishAsync(scope, publication, ct, recoveryDevicePossessionVerified: true);
    }

    /// <summary>Checks the real enrolled kit, actual session and exact new device/root/grants before permitting the separate recovery entry.</summary>
    internal async Task VerifyRecoveryAsync(HouseholdRequestScope scope, HouseholdPublishMembership input,
        HouseholdMembershipStatement next, string grantsHash, CancellationToken ct)
    {
        var current = await ReadCurrentAsync(scope, ct);
        if (next.Change != HouseholdMembershipChange.RecoverOwner || input.RecoveryEvidence is not { } evidence ||
            evidence.SignedEnrollment != current.SignedEnrollment) throw Refuse(409, "HouseholdEnrolledRecoveryProofRequired");
        var kit = HouseholdMetadataProofs.Public(new(current.SigningKey, current.WrappingKey));
        var consent = HouseholdMetadataProofs.Read(evidence.SignedConsent, kit, ConsentType, HouseholdMetadataJsonContext.Default.HouseholdRecoveryConsent);
        var ownerDevice = next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Single();
        if (consent.Format != 1 || consent.SpaceId != scope.Space.Id || consent.EnrollmentId != current.Id ||
            consent.IdentityId != scope.ActorId || consent.SessionId != scope.SessionId || consent.DeviceId != input.DeviceId ||
            ownerDevice.Id != input.DeviceId || consent.OperationId != next.OperationId ||
            consent.MembershipStatementHash != HouseholdMetadataProofs.Digest(input.SignedStatement, HouseholdLimits.MembershipBytes * 2) ||
            consent.GrantsHash != grantsHash) throw Refuse(403, "HouseholdRecoveryConsentMismatch");
    }

    /// <summary>Promotes only the complete finite original archive or appends exactly one new epoch, before the enclosing roster commit.</summary>
    internal async Task ApplyAsync(HouseholdRequestScope scope, HouseholdPublishMembership input,
        HouseholdMembershipStatement? before, HouseholdMembershipStatement next, CancellationToken ct)
    {
        var current = await db.Set<HouseholdRecoveryRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id, ct);
        var mutation = input.Recovery;
        if (before is not null && before.OwnerId != next.OwnerId)
        {
            if (mutation is not null || input.RecoveryEvidence is not null) throw Refuse(409, "HouseholdTransferCannotAdoptPriorRecovery");
            if (current is not null) db.Remove(current);
            await db.Set<HouseholdRecoverySeedRow>().Where(row => row.SpaceId == scope.Space.Id).ExecuteDeleteAsync(ct);
            return;
        }
        if (mutation is null)
        {
            if (current is not null) throw Refuse(409, "HouseholdRecoveryEpochRequired");
            return;
        }
        if (mutation.Epochs is null || mutation.Epochs.Length > HouseholdRecoveryBounds.PageSize || mutation.Epochs.Any(row => row is null) ||
            mutation.ExpectedEnrollmentId != current?.Id || !Enum.IsDefined(mutation.Kind)) throw Refuse(409, "HouseholdRecoveryCheckpointMismatch");
        if (mutation.Kind == HouseholdRecoveryMutationKind.Revoke)
        {
            if (current is null || next.Change != HouseholdMembershipChange.RotateOwnerRoot || before?.OwnerKeys == next.OwnerKeys ||
                mutation.SignedEnrollment is not null || mutation.RootCiphertext is not null || mutation.RootDigest is not null ||
                mutation.Epochs.Length != 0 || mutation.ArchiveOperationId is not null) throw Refuse(409, "HouseholdRecoveryRootRetirementRequired");
            db.Remove(current);
            await db.Set<HouseholdRecoverySeedRow>().Where(row => row.SpaceId == scope.Space.Id).ExecuteDeleteAsync(ct);
            return;
        }
        if (mutation.Kind == HouseholdRecoveryMutationKind.Enroll)
        {
            if (next.Change != HouseholdMembershipChange.RotateOwnerRoot || mutation.ArchiveOperationId is not { } operation ||
                mutation.Epochs.Length != 0) throw Refuse(409, "HouseholdCompleteRecoveryArchiveRequired");
            var seed = await RequireSeedAsync(scope, input.DeviceId, operation, ct); RequireSeedCheckpoint(scope, seed);
            var declaration = HouseholdMetadataProofs.Read(seed.SignedDeclaration,
                HouseholdMetadataProofs.Public(before!.OwnerKeys), SeedType, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
            if (declaration.TargetOwnerKeys != next.OwnerKeys || declaration.OwnerId != next.OwnerId || seed.ThroughEpoch != next.KeyEpoch ||
                seed.UploadedThroughEpoch != seed.ThroughEpoch || mutation.RootCiphertext != seed.RootCiphertext ||
                declaration.RootCiphertextDigest != mutation.RootDigest) throw Refuse(409, "HouseholdRecoveryArchiveCheckpointMismatch");
            var certificate = Certificate(next, input, mutation, declaration.EnrollmentId, declaration.RecoveryKeys);
            var staged = db.Set<HouseholdRecoverySeedEpochRow>().Where(row => row.SpaceId == next.SpaceId && row.OperationId == operation);
            if (await staged.LongCountAsync(ct) != next.KeyEpoch || await staged.MinAsync(row => row.Epoch, ct) != 1 ||
                await staged.MaxAsync(row => row.Epoch, ct) != next.KeyEpoch) throw Refuse(409, "HouseholdRecoveryArchiveIncomplete");
            // The entire promotion stays in the same room/session transaction. SQL copies bounded native
            // rows directly; no all-history ciphertext array is materialized in server memory.
            await db.Set<HouseholdRecoveryEpochRow>().Where(row => row.SpaceId == next.SpaceId).ExecuteDeleteAsync(ct);
            current ??= new HouseholdRecoveryRow { SpaceId = next.SpaceId };
            if (db.Entry(current).State == EntityState.Detached) db.Add(current);
            Assign(current, certificate, mutation); await db.SaveChangesAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "HouseholdRecoveryEpochs" ("SpaceId", "EnrollmentId", "Epoch", "MembershipVersion", "Ciphertext", "Digest", "Bytes")
                SELECT "SpaceId", {certificate.EnrollmentId}, "Epoch", "MembershipVersion", "Ciphertext", "Digest", "Bytes"
                FROM "HouseholdRecoverySeedEpochs" WHERE "SpaceId" = {next.SpaceId} AND "OperationId" = {operation}
                """, ct);
            db.Remove(seed); return;
        }
        if (current is null || mutation.Kind != HouseholdRecoveryMutationKind.Refresh || mutation.ArchiveOperationId is not null ||
            mutation.Epochs.Length != 1) throw Refuse(409, "HouseholdRecoveryEpochRequired");
        if (before!.OwnerKeys != next.OwnerKeys)
        {
            var certificate = Certificate(next, input, mutation, current.Id, new(current.SigningKey, current.WrappingKey));
            Assign(current, certificate, mutation);
        }
        else if (mutation.SignedEnrollment is not null || mutation.RootCiphertext is not null || mutation.RootDigest is not null)
            throw Refuse(409, "UnexpectedHouseholdRecoveryRootReplacement");
        var epoch = mutation.Epochs[0];
        if (epoch.Epoch != next.KeyEpoch || epoch.MembershipVersion != next.Version ||
            epoch.Digest != CiphertextDigest(epoch.Ciphertext, HouseholdRecoveryBounds.EpochCiphertextCharacters))
            throw Refuse(409, "HouseholdRecoveryEpochMismatch");
        await RequireStorageAsync(next.SpaceId, epoch.Ciphertext.Length + (mutation.RootCiphertext?.Length ?? 0), ct);
        db.Add(new HouseholdRecoveryEpochRow { SpaceId = next.SpaceId, EnrollmentId = current.Id, Epoch = epoch.Epoch,
            MembershipVersion = next.Version, Ciphertext = epoch.Ciphertext, Digest = epoch.Digest, Bytes = epoch.Ciphertext.Length });
    }

    /// <summary>Current-root packages are independent of ordinary epoch grants and cannot target another member's identity.</summary>
    internal async Task ApplyRootGrantsAsync(HouseholdPublishMembership input, HouseholdMembershipStatement? before,
        HouseholdMembershipStatement next, CancellationToken ct)
    {
        var grants = input.OwnerRootGrants;
        var owner = next.Members.Single(member => member.Role == HouseholdRole.Owner);
        var rootChanged = before is not null && before.OwnerKeys != next.OwnerKeys;
        var newlyApprovedOwner = before is not null && owner.Devices.Any(device =>
            !before.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Any(old => old.Id == device.Id));
        if (grants is null)
        {
            if (rootChanged || newlyApprovedOwner) throw Refuse(409, "HouseholdCompleteOwnerRootGrantsRequired");
            return;
        }
        if (grants.Length != owner.Devices.Length || grants.Select(row => row.DeviceId).Distinct().Count() != grants.Length)
            throw Refuse(409, "HouseholdCompleteOwnerRootGrantsRequired");
        var fingerprint = HouseholdMetadataProofs.Public(next.OwnerKeys).Fingerprint;
        foreach (var grant in grants)
        {
            if (!owner.Devices.Any(device => device.Id == grant.DeviceId) || grant.MembershipVersion != next.Version ||
                grant.RootFingerprint != fingerprint || grant.Digest != CiphertextDigest(grant.Ciphertext, HouseholdRecoveryBounds.RootCiphertextCharacters))
                throw Refuse(409, "HouseholdOwnerRootGrantMismatch");
        }
        await db.Set<HouseholdOwnerRootGrantRow>().Where(row => row.SpaceId == next.SpaceId).ExecuteDeleteAsync(ct);
        foreach (var grant in grants) db.Add(new HouseholdOwnerRootGrantRow { SpaceId = next.SpaceId, DeviceId = grant.DeviceId,
            MembershipVersion = next.Version, RootFingerprint = fingerprint, Ciphertext = grant.Ciphertext, Digest = grant.Digest });
    }

    private static HouseholdRecoveryCertificate Certificate(HouseholdMembershipStatement next, HouseholdPublishMembership input,
        HouseholdRecoveryMutation mutation, Guid enrollment, HouseholdPublicKeys recoveryKeys)
    {
        if (mutation.SignedEnrollment is not { Length: > 0 and <= HouseholdRecoveryBounds.MetadataCharacters } ||
            mutation.RootDigest != CiphertextDigest(mutation.RootCiphertext!, HouseholdRecoveryBounds.RootCiphertextCharacters))
            throw Refuse(409, "HouseholdRecoveryCertificateRequired");
        var root = HouseholdMetadataProofs.Public(next.OwnerKeys); var kit = HouseholdMetadataProofs.Public(recoveryKeys);
        if (kit.SigningKey == root.SigningKey || kit.WrappingKey == root.WrappingKey || next.Members.SelectMany(member => member.Devices)
            .Any(device => device.Keys.SigningKey == kit.SigningKey || device.Keys.WrappingKey == kit.WrappingKey))
            throw Refuse(409, "HouseholdIndependentRecoveryKitRequired");
        var certificate = HouseholdMetadataProofs.Read(mutation.SignedEnrollment, root, EnrollmentType,
            HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
        if (certificate.Format != 1 || certificate.SpaceId != next.SpaceId || certificate.EnrollmentId != enrollment ||
            certificate.OwnerId != next.OwnerId || certificate.OwnerKeys != next.OwnerKeys || certificate.RecoveryKeys != recoveryKeys ||
            certificate.MembershipVersion != next.Version || certificate.MembershipHead != HouseholdMetadataProofs.Digest(input.SignedStatement, HouseholdLimits.MembershipBytes * 2) ||
            certificate.RootCiphertextDigest != mutation.RootDigest) throw Refuse(409, "HouseholdRecoveryCertificateMismatch");
        return certificate;
    }

    private static void Assign(HouseholdRecoveryRow row, HouseholdRecoveryCertificate certificate, HouseholdRecoveryMutation mutation)
    {
        row.Id = certificate.EnrollmentId; row.OwnerId = certificate.OwnerId; row.SigningKey = certificate.RecoveryKeys.SigningKey;
        row.WrappingKey = certificate.RecoveryKeys.WrappingKey; row.MembershipVersion = certificate.MembershipVersion;
        row.SignedEnrollment = mutation.SignedEnrollment!; row.Ciphertext = mutation.RootCiphertext!; row.Digest = mutation.RootDigest!;
    }
}
