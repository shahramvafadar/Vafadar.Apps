using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Private durable original enrollment intent; byte frames never leave the protected local database except as explicit recipient/password encryption.</summary>
internal sealed record HouseholdRecoveryPreparationFrame([property: JsonRequired] HouseholdOpenRecoverySeed Seed,
    [property: JsonRequired] Guid? ExistingEnrollmentId, [property: JsonRequired] byte[] Kit,
    [property: JsonRequired] byte[] Root, [property: JsonRequired] byte[] Epoch, [property: JsonRequired] bool ExportConfirmed) : IDisposable
{
    /// <inheritdoc />
    public void Dispose() { CryptographicOperations.ZeroMemory(Kit); CryptographicOperations.ZeroMemory(Root); CryptographicOperations.ZeroMemory(Epoch); }
    /// <inheritdoc />
    public override string ToString() => "Private household recovery preparation";
}

public sealed partial class HouseholdJournal
{
    private const string RecoveryPreparationSlot = "pending-recovery-preparation";

    /// <summary>Atomically freezes independent original kit/root/epoch and signed upload header before any remote mutation or export.</summary>
    internal async Task RetainRecoveryPreparationAsync(HouseholdJournalAuthority authority, HouseholdOpenRecoverySeed seed,
        Guid? existingEnrollment, HouseholdRecoveryKit kit, SigningRecipientIdentity newRoot, HouseholdEpochMaterial epoch, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var current = (await ReadMembershipAsync(context.Db, binding, device, ct)).Current;
        var declaration = StandardJoseSignatures.VerifyJson(seed.SignedDeclaration, current.Root, HouseholdRecoveryProofTypes.Seed,
            HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
        if (current.Snapshot().OwnerId != authority.IdentityId || binding.SendingDisabled || declaration.DeviceId != binding.DeviceId ||
            seed.DeviceId != binding.DeviceId || declaration.ExpectedVersion != current.Version || declaration.ExpectedHead != current.Hash ||
            declaration.EnrollmentId != kit.EnrollmentId || kit.SpaceId != binding.SpaceId || kit.OwnerId != binding.IdentityId ||
            kit.InitialOwnerAnchor != declaration.TargetOwnerKeys ||
            declaration.TargetOwnerKeys != new HouseholdPublicKeys(newRoot.Public.SigningKey, newRoot.Public.WrappingKey) ||
            declaration.RecoveryKeys != new HouseholdPublicKeys(kit.Public.SigningKey, kit.Public.WrappingKey) ||
            declaration.TargetEpoch != epoch.Epoch || epoch.Epoch != current.Epoch + 1 ||
            declaration.RootCiphertextDigest != HouseholdPublicationDigest.Ciphertext(seed.RootCiphertext) ||
            await context.Db.Set<HouseholdLocalMembershipProposal>().AnyAsync(ct)) throw Refusal();
        using var frame = new HouseholdRecoveryPreparationFrame(seed, existingEnrollment, kit.ExportForProtectedStorage(),
            newRoot.ExportForProtectedStorage(), epoch.ExportForProtectedStorage(), false);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryPreparationFrame);
        try
        {
            if (bytes.Length > 128 * 1024) throw Refusal();
            await AddImmutableSecretAsync(context.Db, RecoveryPreparationSlot, bytes, ct);
            await StoreRootAsync(context.Db, newRoot.Public, frame.Root, ct);
            await AddImmutableSecretAsync(context.Db, PreparedEpochSlot(declaration.OperationId), frame.Epoch, ct);
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Opens only the fixed captured protected intent; no arbitrary secret slot or new key fallback is exposed.</summary>
    internal async Task<HouseholdRecoveryPreparationFrame?> ReadRecoveryPreparationAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == RecoveryPreparationSlot, ct);
        if (row is null) return null;
        try
        {
            var frame = StrictJsonPayload.Deserialize(row.Frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryPreparationFrame, 128 * 1024);
            try { context.Check(); return frame; } catch { frame.Dispose(); throw; }
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    /// <summary>Records explicit successful protected-file handoff for this original kit before publication; a different kit cannot inherit confirmation.</summary>
    public async Task ConfirmRecoveryExportAsync(HouseholdJournalAuthority authority, string kitFingerprint, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (membership.Current.Snapshot().OwnerId != authority.IdentityId) throw Refusal();
        var row = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(row => row.Slot == RecoveryPreparationSlot, ct) ?? throw Refusal();
        using var frame = StrictJsonPayload.Deserialize(row.Frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryPreparationFrame, 128 * 1024);
        using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit);
        if (kit.Public.Fingerprint != kitFingerprint) throw Refusal();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(frame with { ExportConfirmed = true }, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryPreparationFrame);
        try { CryptographicOperations.ZeroMemory(row.Frame); row.Frame = bytes; await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Seals and persists one bounded original archive page under one verified protected read; no native context survives the HTTP upload.</summary>
    internal async Task<HouseholdRecoveryEpochGrant[]> RecoveryPreparationPageAsync(HouseholdJournalAuthority authority,
        HouseholdRecoveryPreparationFrame frame, long after, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit); using var root = SigningRecipientIdentity.ImportFromProtectedStorage(frame.Root);
        using var next = HouseholdEpochMaterial.ImportFromProtectedStorage(frame.Epoch);
        var declaration = StandardJoseSignatures.VerifyJson(frame.Seed.SignedDeclaration, membership.Current.Root, HouseholdRecoveryProofTypes.Seed,
            HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
        if (!frame.ExportConfirmed || declaration.ExpectedVersion != membership.Current.Version || declaration.ExpectedHead != membership.Current.Hash ||
            after < 0 || after >= next.Epoch || declaration.TargetEpoch != next.Epoch) throw Refusal();
        var current = membership.Current.Snapshot(); var granting = current with { Version = current.Version + 1, KeyEpoch = next.Epoch,
            OperationId = declaration.OperationId, OwnerKeys = declaration.TargetOwnerKeys, KeyCommitment = next.KeyCommitment };
        var result = new List<HouseholdRecoveryEpochGrant>();
        for (var epoch = after + 1; epoch <= Math.Min(next.Epoch, after + HouseholdRecoveryBounds.PageSize); epoch++)
        {
            var slot = RecoveryPageSlot(declaration.OperationId, epoch);
            var existing = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(row => row.Slot == slot, ct);
            HouseholdRecoveryEpochGrant grant;
            if (existing is not null)
            {
                grant = StrictJsonPayload.Deserialize(existing.Frame, HouseholdMetadataJsonContext.Default.HouseholdRecoveryEpochGrant, 16384);
                if (grant.Epoch != epoch || grant.MembershipVersion != granting.Version || grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext)) throw Refusal();
            }
            else
            {
                using var material = epoch == next.Epoch ? HouseholdEpochMaterial.ImportFromProtectedStorage(frame.Epoch)
                    : await ReadEpochCoreAsync(context.Db, membership, epoch, ct);
                var origin = epoch == next.Epoch ? granting : membership.ByEpoch[epoch].Snapshot();
                grant = HouseholdRecoveryMaterial.SealEpoch(cryptography, kit.EnrollmentId, granting, origin, material, root, kit.Public);
                context.Db.Add(new HouseholdLocalSecret { Slot = slot, Frame = JsonSerializer.SerializeToUtf8Bytes(grant,
                    HouseholdMetadataJsonContext.Default.HouseholdRecoveryEpochGrant) });
            }
            result.Add(grant);
        }
        await context.CommitAsync(ct); return result.ToArray();
    }

    /// <summary>Forgets only private preparation/export state and its exact staged ciphertext after the original root operation is in verified history.</summary>
    internal async Task CompleteRecoveryPreparationAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        using var frame = await ReadRecoveryPreparationAsync(authority, ct);
        if (frame is null) return;
        using var root = SigningRecipientIdentity.ImportFromProtectedStorage(frame.Root);
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var history = await ReadMembershipAsync(context.Db, binding, device, ct);
        var transition = history.ByVersion.Values.Where(state => state.Root == root.Public).OrderBy(state => state.Version).FirstOrDefault() ?? throw Refusal();
        if (!history.ByVersion.TryGetValue(transition.Version - 1, out var before)) throw Refusal();
        var seed = StandardJoseSignatures.VerifyJson(frame.Seed.SignedDeclaration, before.Root, HouseholdRecoveryProofTypes.Seed,
            HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
        if (!history.ByVersion.Values.Any(state => state.Snapshot().OperationId == seed.OperationId && state.Root == root.Public)) throw Refusal();
        var prefix = "recovery-seed:" + seed.OperationId.ToString("N") + ":";
        await context.Db.Set<HouseholdLocalSecret>().Where(row => row.Slot == RecoveryPreparationSlot || row.Slot.StartsWith(prefix)).ExecuteDeleteAsync(ct);
        await context.CommitAsync(ct);
    }

    private static string RecoveryPageSlot(Guid operation, long epoch) => "recovery-seed:" + operation.ToString("N") + ":" +
        epoch.ToString("D20", System.Globalization.CultureInfo.InvariantCulture);
}
