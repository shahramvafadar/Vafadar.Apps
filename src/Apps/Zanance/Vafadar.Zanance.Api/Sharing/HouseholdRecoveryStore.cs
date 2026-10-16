using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Current-owner public authority and bounded opaque recovery archive; no server method receives a kit or a clear root/epoch key.</summary>
internal sealed partial class HouseholdRecoveryStore(ServiceDbContext db)
{
    internal const string EnrollmentType = HouseholdRecoveryProofTypes.Enrollment;
    internal const string SeedType = HouseholdRecoveryProofTypes.Seed;
    internal const string ConsentType = HouseholdRecoveryProofTypes.Consent;

    /// <summary>Discovery needs the actual owning identity, but no old approved device: losing all device keys is the explicit recovery case.</summary>
    internal async Task<HouseholdRecoveryObservation> ReadAsync(HouseholdRequestScope scope, CancellationToken ct)
    {
        var row = await ReadCurrentAsync(scope, ct);
        return new(row.Id, row.SignedEnrollment, row.MembershipVersion, row.Ciphertext, row.Digest, scope.Space.KeyEpoch,
            scope.Space.Cursor, scope.Space.PublicationHead);
    }

    /// <summary>Only a currently approved device of the actual owning identity may discover its encrypted private-root delegation.</summary>
    internal async Task<HouseholdOwnerRootGrant> OwnerRootAsync(HouseholdRequestScope scope, Guid device, CancellationToken ct)
    {
        if (scope.Space.OwnerId != scope.ActorId) throw Refuse(404, "HouseholdResourceUnavailable");
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.ReadDeviceGrant, ct);
        var member = await scope.MemberAsync(includePending: false, ct);
        if (member?.Role != HouseholdRole.Owner) throw Refuse(404, "HouseholdResourceUnavailable");
        var fingerprint = HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey)).Fingerprint;
        var row = await db.Set<HouseholdOwnerRootGrantRow>().AsNoTracking().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id &&
            row.DeviceId == device && row.RootFingerprint == fingerprint, ct) ?? throw Refuse(404, "HouseholdOwnerRootUnavailable");
        if (row.MembershipVersion < 1 || row.MembershipVersion > scope.Space.MembershipVersion ||
            row.Digest != CiphertextDigest(row.Ciphertext, HouseholdRecoveryBounds.RootCiphertextCharacters)) throw Refuse(409, "HouseholdOwnerRootGrantMismatch");
        return new(row.DeviceId, row.MembershipVersion, row.RootFingerprint, row.Ciphertext, row.Digest);
    }

    /// <summary>Reads a fixed finite original prefix; current membership growth cannot turn one recovery into an unbounded chase.</summary>
    internal async Task<HouseholdRecoveryEpochPage> EpochsAsync(HouseholdRequestScope scope, Guid enrollment,
        long after, long through, CancellationToken ct)
    {
        var current = await ReadCurrentAsync(scope, ct);
        if (enrollment != current.Id || after < 0 || through < after || through > scope.Space.KeyEpoch || through > HouseholdRecoveryBounds.MaximumEpochs)
            throw Refuse(409, "HouseholdRecoveryCheckpointChanged");
        var rows = await db.Set<HouseholdRecoveryEpochRow>().AsNoTracking().Where(row => row.SpaceId == scope.Space.Id &&
            row.EnrollmentId == enrollment && row.Epoch > after && row.Epoch <= through).OrderBy(row => row.Epoch)
            .Take(HouseholdRecoveryBounds.PageSize).ToArrayAsync(ct);
        var cursor = after;
        foreach (var row in rows)
        {
            if (row.Epoch != cursor + 1 || row.MembershipVersion < 1 || row.MembershipVersion > scope.Space.MembershipVersion ||
                row.Bytes != row.Ciphertext.Length || row.Digest != CiphertextDigest(row.Ciphertext, HouseholdRecoveryBounds.EpochCiphertextCharacters))
                throw Refuse(409, "HouseholdRecoveryArchiveIncomplete");
            cursor = row.Epoch;
        }
        if (cursor < through && rows.Length < HouseholdRecoveryBounds.PageSize) throw Refuse(409, "HouseholdRecoveryArchiveIncomplete");
        return new(enrollment, through, rows.Select(row => new HouseholdRecoveryEpochGrant(row.Epoch, row.MembershipVersion, row.Ciphertext, row.Digest)).ToArray(), cursor, cursor == through);
    }

    /// <summary>Stages one original current-owner declaration without accepting its new root or changing a financial/membership cursor.</summary>
    internal async Task<HouseholdRecoverySeedProgress> OpenSeedAsync(HouseholdRequestScope scope, HouseholdOpenRecoverySeed input, CancellationToken ct)
    {
        await RequireApprovedOwnerAsync(scope, input.DeviceId, ct);
        if (input.SignedDeclaration is not { Length: > 0 and <= HouseholdRecoveryBounds.MetadataCharacters }) throw Refuse(413, "HouseholdRecoveryMetadataBound");
        var root = HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey));
        var declaration = HouseholdMetadataProofs.Read(input.SignedDeclaration, root, SeedType, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
        if (declaration.TargetOwnerKeys is null || declaration.RecoveryKeys is null) throw Refuse(400, "InvalidHouseholdRecoveryKeys");
        if (declaration.Format != 1 || declaration.SpaceId != scope.Space.Id || declaration.DeviceId != input.DeviceId || declaration.OperationId == Guid.Empty ||
            declaration.EnrollmentId == Guid.Empty || declaration.OwnerId != scope.ActorId || declaration.ExpectedVersion != scope.Space.MembershipVersion ||
            declaration.ExpectedHead != scope.Space.MembershipHead || scope.Space.KeyEpoch >= HouseholdRecoveryBounds.MaximumEpochs || declaration.TargetEpoch != scope.Space.KeyEpoch + 1 ||
            declaration.TargetOwnerKeys.SigningKey == root.SigningKey || declaration.TargetOwnerKeys.WrappingKey == root.WrappingKey ||
            declaration.RootCiphertextDigest != CiphertextDigest(input.RootCiphertext, HouseholdRecoveryBounds.RootCiphertextCharacters))
            throw Refuse(409, "HouseholdRecoverySeedCheckpointMismatch");
        var nextRoot = HouseholdMetadataProofs.Public(declaration.TargetOwnerKeys); var kit = HouseholdMetadataProofs.Public(declaration.RecoveryKeys);
        if (kit.SigningKey == root.SigningKey || kit.WrappingKey == root.WrappingKey || kit.SigningKey == nextRoot.SigningKey || kit.WrappingKey == nextRoot.WrappingKey ||
            await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && (row.SigningKey == kit.SigningKey || row.WrappingKey == kit.WrappingKey), ct))
            throw Refuse(400, "HouseholdIndependentRecoveryKitRequired");
        var requestHash = HouseholdMetadataProofs.Digest(JsonSerializer.Serialize(input, HouseholdMetadataJsonContext.Default.HouseholdOpenRecoverySeed), 128 * 1024);
        var prior = await db.Set<HouseholdRecoverySeedRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id, ct);
        if (prior is not null)
        {
            if (prior.OperationId != declaration.OperationId || prior.OwnerId != scope.ActorId || prior.DeviceId != input.DeviceId || prior.RequestHash != requestHash)
                throw Refuse(409, "HouseholdRecoveryPreparationPending");
            RequireSeedCheckpoint(scope, prior); return Progress(prior);
        }
        var end = declaration.ValidUntilUnixSeconds;
        if (end <= scope.Now.ToUnixTimeSeconds() || end > scope.Now.Add(HouseholdRecoveryBounds.SeedLifetime).ToUnixTimeSeconds())
            throw Refuse(409, "HouseholdRecoverySeedExpiredOrFuture");
        var seed = new HouseholdRecoverySeedRow { SpaceId = scope.Space.Id, OperationId = declaration.OperationId, DeviceId = input.DeviceId,
            EnrollmentId = declaration.EnrollmentId, OwnerId = scope.ActorId, SignedDeclaration = input.SignedDeclaration, RequestHash = requestHash,
            RootCiphertext = input.RootCiphertext, ExpectedVersion = declaration.ExpectedVersion, ExpectedHead = declaration.ExpectedHead,
            ThroughEpoch = declaration.TargetEpoch, StoredBytes = input.RootCiphertext.Length, CreatedAt = scope.Now, ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(end) };
        await RequireStorageAsync(scope.Space.Id, seed.StoredBytes, ct); db.Add(seed); return Progress(seed);
    }

    /// <summary>Returns only the current owner's original staged prefix; an expired/changed predecessor is retained for explicit cancellation.</summary>
    internal async Task<HouseholdRecoverySeedProgress> SeedAsync(HouseholdRequestScope scope, Guid device, Guid operation, CancellationToken ct)
    {
        await RequireApprovedOwnerAsync(scope, device, ct); var seed = await RequireSeedAsync(scope, device, operation, ct);
        return Progress(seed);
    }

    /// <summary>Appends at most one finite contiguous original page and exactly replays prior encrypted bytes after a lost acknowledgement.</summary>
    internal async Task<HouseholdRecoverySeedProgress> UploadAsync(HouseholdRequestScope scope, Guid operation,
        HouseholdUploadRecoveryEpochs input, CancellationToken ct)
    {
        await RequireApprovedOwnerAsync(scope, input.DeviceId, ct);
        var seed = await RequireSeedAsync(scope, input.DeviceId, operation, ct); RequireSeedCheckpoint(scope, seed);
        if (input.Epochs is not { Length: > 0 and <= HouseholdRecoveryBounds.PageSize } || input.Epochs.Any(row => row is null)) throw Refuse(413, "HouseholdRecoveryPageBound");
        var ids = input.Epochs.Select(row => row.Epoch).ToArray();
        if (ids.Distinct().Count() != ids.Length || !ids.SequenceEqual(ids.Order())) throw Refuse(400, "HouseholdRecoveryPageOrder");
        var stored = await db.Set<HouseholdRecoverySeedEpochRow>().Where(row => row.SpaceId == scope.Space.Id && row.OperationId == operation && ids.Contains(row.Epoch))
            .ToDictionaryAsync(row => row.Epoch, ct);
        long extra = 0; var through = seed.UploadedThroughEpoch;
        foreach (var part in input.Epochs)
        {
            if (part.Epoch < 1 || part.Epoch > seed.ThroughEpoch || part.MembershipVersion != seed.ExpectedVersion + 1 ||
                part.Digest != CiphertextDigest(part.Ciphertext, HouseholdRecoveryBounds.EpochCiphertextCharacters)) throw Refuse(400, "InvalidHouseholdRecoveryEpoch");
            if (stored.TryGetValue(part.Epoch, out var old))
            {
                if (old.Epoch > through || old.Ciphertext != part.Ciphertext || old.Digest != part.Digest || old.MembershipVersion != part.MembershipVersion)
                    throw Refuse(409, "AlteredHouseholdRecoveryEpoch");
                continue;
            }
            if (part.Epoch != through + 1) throw Refuse(409, "HouseholdRecoveryPrefixRequired");
            db.Add(new HouseholdRecoverySeedEpochRow { SpaceId = scope.Space.Id, OperationId = operation, Epoch = part.Epoch,
                MembershipVersion = part.MembershipVersion, Ciphertext = part.Ciphertext, Digest = part.Digest, Bytes = part.Ciphertext.Length });
            extra = checked(extra + part.Ciphertext.Length); through = part.Epoch;
        }
        await RequireStorageAsync(scope.Space.Id, extra, ct);
        seed.StoredBytes = checked(seed.StoredBytes + extra); seed.UploadedThroughEpoch = through; return Progress(seed);
    }

    /// <summary>Only explicit exact-owner cancellation retires this staged operation; no financial or enrolled key is removed.</summary>
    internal async Task CancelAsync(HouseholdRequestScope scope, Guid device, Guid operation, CancellationToken ct)
    {
        await RequireApprovedOwnerAsync(scope, device, ct);
        var row = await db.Set<HouseholdRecoverySeedRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.OperationId == operation, ct);
        if (row is null) return;
        if (row.OwnerId != scope.ActorId || row.DeviceId != device) throw Refuse(404, "HouseholdResourceUnavailable"); db.Remove(row);
    }

    private async Task<HouseholdRecoveryRow> ReadCurrentAsync(HouseholdRequestScope scope, CancellationToken ct)
    {
        await RequireOwnerIdentityAsync(scope, ct);
        var row = await db.Set<HouseholdRecoveryRow>().AsNoTracking().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.OwnerId == scope.ActorId, ct)
            ?? throw Refuse(404, "HouseholdRecoveryUnavailable");
        if (row.SignedEnrollment is not { Length: > 0 and <= HouseholdRecoveryBounds.MetadataCharacters }) throw Refuse(409, "HouseholdRecoveryCertificateUnavailable");
        var root = HouseholdMetadataProofs.Public(new(scope.Space.OwnerSigningKey, scope.Space.OwnerWrappingKey));
        var certificate = HouseholdMetadataProofs.Read(row.SignedEnrollment, root, EnrollmentType, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
        if (certificate.Format != 1 || certificate.SpaceId != scope.Space.Id || certificate.EnrollmentId != row.Id || certificate.OwnerId != scope.ActorId ||
            certificate.OwnerKeys != new HouseholdPublicKeys(root.SigningKey, root.WrappingKey) || certificate.RecoveryKeys != new HouseholdPublicKeys(row.SigningKey, row.WrappingKey) ||
            certificate.MembershipVersion != row.MembershipVersion || row.MembershipVersion < 1 || row.MembershipVersion > scope.Space.MembershipVersion ||
            certificate.RootCiphertextDigest != row.Digest || row.Digest != CiphertextDigest(row.Ciphertext, HouseholdRecoveryBounds.RootCiphertextCharacters) ||
            !await db.Set<HouseholdMembershipEventRow>().AnyAsync(item => item.SpaceId == scope.Space.Id && item.Version == row.MembershipVersion && item.Hash == certificate.MembershipHead, ct))
            throw Refuse(409, "HouseholdRecoveryCertificateMismatch");
        return row;
    }

    private static async Task RequireOwnerIdentityAsync(HouseholdRequestScope scope, CancellationToken ct)
    {
        var member = await scope.MemberAsync(includePending: false, ct);
        if (scope.Space.OwnerId != scope.ActorId || member?.Role != HouseholdRole.Owner || scope.Retention is not (HouseholdRetentionState.Active or HouseholdRetentionState.ReadOnlyGrace))
            throw Refuse(404, "HouseholdResourceUnavailable");
    }
    private static async Task RequireApprovedOwnerAsync(HouseholdRequestScope scope, Guid device, CancellationToken ct)
    { _ = await scope.RequireOwnerPendingDeviceAsync(device, ct); _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Invite, ct); }
    private async Task<HouseholdRecoverySeedRow> RequireSeedAsync(HouseholdRequestScope scope, Guid device, Guid operation, CancellationToken ct)
    {
        var row = await db.Set<HouseholdRecoverySeedRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id && row.OperationId == operation, ct);
        if (row is null || row.OwnerId != scope.ActorId || row.DeviceId != device) throw Refuse(404, "HouseholdResourceUnavailable"); return row;
    }
    private static void RequireSeedCheckpoint(HouseholdRequestScope scope, HouseholdRecoverySeedRow seed)
    {
        if (seed.ExpiresAt <= scope.Now || seed.ExpectedVersion != scope.Space.MembershipVersion || seed.ExpectedHead != scope.Space.MembershipHead ||
            seed.ThroughEpoch != scope.Space.KeyEpoch + 1 || seed.UploadedThroughEpoch < 0 || seed.UploadedThroughEpoch > seed.ThroughEpoch)
            throw Refuse(409, "HouseholdRecoverySeedCheckpointChanged");
    }
    private async Task RequireStorageAsync(Guid space, long extra, CancellationToken ct)
    {
        var root = await db.Set<HouseholdRecoveryRow>().AsNoTracking().Where(row => row.SpaceId == space).Select(row => (long)row.Ciphertext.Length).SingleOrDefaultAsync(ct);
        var current = await db.Set<HouseholdRecoveryEpochRow>().Where(row => row.SpaceId == space).SumAsync(row => (long)row.Bytes, ct);
        var pending = await db.Set<HouseholdRecoverySeedRow>().Where(row => row.SpaceId == space).SumAsync(row => row.StoredBytes, ct);
        if (extra < 0 || extra > HouseholdRecoveryBounds.MaximumStoredBytes || root + current + pending > HouseholdRecoveryBounds.MaximumStoredBytes - extra)
            throw Refuse(413, "HouseholdRecoveryStorageBound");
    }
    private static string CiphertextDigest(string value, int limit)
    {
        if (value is null || value.Length > limit || value.Length < 32 || value.Count(character => character == '.') != 4 ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.')) || value.Split('.').Any(part => part.Length == 0))
            throw Refuse(400, "InvalidHouseholdRecoveryEnvelope");
        return HouseholdMetadataProofs.Digest(value, limit);
    }
    private static HouseholdRecoverySeedProgress Progress(HouseholdRecoverySeedRow row) =>
        new(row.OperationId, row.ThroughEpoch, row.UploadedThroughEpoch, row.UploadedThroughEpoch == row.ThroughEpoch, row.StoredBytes);
    private static HouseholdProtocolRefusal Refuse(int status, string code) => new(status, code);
}
