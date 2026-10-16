using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Private resumable owner recovery; the kit is removed after original verified device adoption, never included in portable settings.</summary>
internal sealed record HouseholdOwnerRecoveryFrame([property: JsonRequired] byte[] Kit,
    [property: JsonRequired] HouseholdRecoveryObservation Observation, [property: JsonRequired] string? DevicePossession) : IDisposable
{
    /// <inheritdoc />
    public void Dispose() => CryptographicOperations.ZeroMemory(Kit);
    /// <inheritdoc />
    public override string ToString() => "Private household owner recovery";
}

public sealed partial class HouseholdJournal
{
    private const string OwnerRecoverySlot = "pending-owner-recovery";

    /// <summary>Observes only whether the exact captured private import still needs explicit continuation.</summary>
    internal async Task<bool> HasOwnerRecoveryAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var result = await context.Db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == OwnerRecoverySlot, ct); context.Check(); return result;
    }

    /// <summary>Retains only independently kit-verified original public history and private import inside an empty captured recovery draft.</summary>
    internal async Task RetainOwnerRecoveryAsync(HouseholdJournalAuthority authority, HouseholdRecoveryKit kit,
        HouseholdRecoveryObservation observation, IReadOnlyList<HouseholdMembershipProof> proofs, CancellationToken ct)
    {
        _ = HouseholdTrustChain.RecoveryHistory(kit, proofs, observation, authority.IdentityId);
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.OwnerCreation || binding.MembershipVersion != 0 || binding.SpaceId != kit.SpaceId || binding.IdentityId != kit.OwnerId ||
            binding.PeerKey != kit.Peer.BindingKey(kit.OwnerId) || binding.SignedInvitation is null || !binding.SignedInvitation.StartsWith('{')) throw Refusal();
        var anchor = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(binding.SignedInvitation), HouseholdMetadataJsonContext.Default.HouseholdRecoveryAnchor, 8192);
        if (anchor.EnrollmentId != kit.EnrollmentId || anchor.KitKeys != new HouseholdPublicKeys(kit.Public.SigningKey, kit.Public.WrappingKey) ||
            anchor.InitialOwnerRoot != kit.InitialOwnerAnchor) throw Refusal();
        using var frame = new HouseholdOwnerRecoveryFrame(kit.ExportForProtectedStorage(), observation, null);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdOwnerRecoveryFrame);
        try
        {
            if (bytes.Length > 128 * 1024) throw Refusal();
            await AddImmutableSecretAsync(context.Db, OwnerRecoverySlot, bytes, ct);
            var existing = await context.Db.Set<HouseholdLocalMembership>().OrderBy(row => row.Version).ToArrayAsync(ct);
            if (existing.Length > proofs.Count) throw Refusal();
            for (var index = 0; index < proofs.Count; index++)
            {
                var json = SerializeProof(proofs[index]); var hash = HouseholdTrustChain.Digest(proofs[index].SignedStatement);
                if (index < existing.Length)
                { if (existing[index].Version != index + 1 || existing[index].ProofJson != json || existing[index].Hash != hash) throw Refusal(); }
                else context.Db.Add(new HouseholdLocalMembership { Version = index + 1, ProofJson = json, Hash = hash });
            }
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Opens only the fixed original private import; missing state never generates a replacement kit or operation.</summary>
    internal async Task<HouseholdOwnerRecoveryFrame> ReadOwnerRecoveryAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == OwnerRecoverySlot, ct) ?? throw Refusal();
        try
        {
            var frame = StrictJsonPayload.Deserialize(row.Frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdOwnerRecoveryFrame, 128 * 1024);
            try { context.Check(); return frame; } catch { frame.Dispose(); throw; }
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    /// <summary>Loads the original independently verified public prefix, without claiming the pending new device was already admitted.</summary>
    internal async Task<HouseholdVerifiedHistory> OwnerRecoveryHistoryAsync(HouseholdJournalAuthority authority,
        HouseholdOwnerRecoveryFrame frame, CancellationToken ct)
    {
        using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit);
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.MembershipVersion != 0) throw Refusal();
        var rows = await context.Db.Set<HouseholdLocalMembership>().AsNoTracking().OrderBy(row => row.Version).ToArrayAsync(ct);
        var proofs = rows.Select(row => StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.ProofJson),
            HouseholdMetadataJsonContext.Default.HouseholdMembershipProof, 512 * 1024)).ToArray();
        if (rows.Where((row, index) => row.Version != index + 1 || row.Hash != HouseholdTrustChain.Digest(proofs[index].SignedStatement)).Any()) throw Refusal();
        var result = HouseholdTrustChain.RecoveryHistory(kit, proofs, frame.Observation, authority.IdentityId); context.Check(); return result;
    }

    /// <summary>Stores exactly one verified original kit archive page; an interrupted import reuses all existing original key frames.</summary>
    internal async Task<long> RecordOwnerRecoveryPageAsync(HouseholdJournalAuthority authority, HouseholdOwnerRecoveryFrame frame,
        HouseholdRecoveryEpochPage page, CancellationToken ct)
    {
        var history = await OwnerRecoveryHistoryAsync(authority, frame, ct); using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit);
        if (page.EnrollmentId != kit.EnrollmentId || page.ThroughEpoch != frame.Observation.ThroughEpoch || page.Epochs is not { Length: > 0 and <= HouseholdRecoveryBounds.PageSize }) throw Refusal();
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        long previous = 0; var buffers = new List<byte[]>();
        try
        {
            foreach (var grant in page.Epochs)
            {
                if (previous != 0 && grant.Epoch != previous + 1 || grant.Epoch < 1 || grant.Epoch > history.Current.Epoch || grant.MembershipVersion < 1 ||
                    grant.MembershipVersion > history.Current.Version) throw Refusal();
                var granting = history.States[checked((int)grant.MembershipVersion - 1)].Snapshot();
                var origin = history.States.Single(state => state.Epoch == grant.Epoch).Snapshot();
                using var material = HouseholdRecoveryMaterial.OpenEpoch(cryptography, granting, origin, grant, kit);
                var bytes = material.ExportForProtectedStorage();
                buffers.Add(bytes); await AddImmutableSecretAsync(context.Db, EpochSlot(material.Epoch), bytes, ct);
                previous = grant.Epoch;
            }
            if (page.NextEpoch != previous || page.Complete != (previous == history.Current.Epoch)) throw Refusal();
            await context.CommitAsync(ct); return previous;
        }
        finally { foreach (var bytes in buffers) CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Reads only a previously imported old key checked against the independently verified kit prefix.</summary>
    internal async Task<HouseholdEpochMaterial> ReadRecoveryEpochAsync(HouseholdJournalAuthority authority,
        HouseholdVerifiedHistory history, long epoch, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var state = new MembershipState(history.Current, history.States.ToDictionary(state => state.Version), history.States.ToDictionary(state => state.Epoch));
        var result = await ReadEpochCoreAsync(context.Db, state, epoch, ct);
        try { context.Check(); return result; } catch { result.Dispose(); throw; }
    }

    /// <summary>Checks every already imported original key once before skipping its network page; holes or corrupt frames are never repaired over.</summary>
    internal async Task<long> OwnerRecoveryImportedThroughAsync(HouseholdJournalAuthority authority, HouseholdVerifiedHistory history, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var slots = await context.Db.Set<HouseholdLocalSecret>().Where(row => row.Slot.StartsWith("epoch:")).Select(row => row.Slot).ToArrayAsync(ct);
        if (slots.Length > history.Current.Epoch) throw Refusal();
        var known = slots.ToHashSet(StringComparer.Ordinal); long through = 0;
        while (through < history.Current.Epoch && known.Contains(EpochSlot(through + 1))) through++;
        if (through != known.Count) throw Refusal();
        for (long offset = 0; offset < through; offset += HouseholdRecoveryBounds.PageSize)
        {
            var names = Enumerable.Range(checked((int)offset + 1), checked((int)Math.Min(HouseholdRecoveryBounds.PageSize, through - offset)))
                .Select(epoch => EpochSlot(epoch)).ToArray();
            var rows = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().Where(row => names.Contains(row.Slot)).ToArrayAsync(ct);
            try
            {
                foreach (var row in rows)
                {
                    using var key = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Frame);
                    var origin = history.States.SingleOrDefault(state => state.Epoch == key.Epoch)?.Snapshot() ?? throw Refusal();
                    if (row.Slot != EpochSlot(key.Epoch) || key.KeyCommitment != origin.KeyCommitment || key.AddressKeyCommitment != origin.AddressKeyCommitment) throw Refusal();
                }
            }
            finally { foreach (var row in rows) CryptographicOperations.ZeroMemory(row.Frame); }
        }
        context.Check(); return through;
    }

    /// <summary>Freezes new root/epoch, actual-session device proof and complete recovery request together before its only remote mutation.</summary>
    internal async Task StageOwnerRecoveryRequestAsync(HouseholdJournalAuthority authority, HouseholdOwnerRecoveryFrame frame,
        HouseholdRecoverOwner request, SigningRecipientIdentity root, HouseholdEpochMaterial epoch, CancellationToken ct)
    {
        var history = await OwnerRecoveryHistoryAsync(authority, frame, ct);
        var input = request.Publication;
        var next = HouseholdTrustChain.Advance(history.Current, new(input.SignedStatement, input.TransferAcceptance, null,
            HouseholdPublicationDigest.Grants(input.Grants), input.RecoveryEvidence));
        if (next.Snapshot().Change != HouseholdMembershipChange.RecoverOwner || next.Root != root.Public || next.Epoch != epoch.Epoch ||
            next.Snapshot().KeyCommitment != epoch.KeyCommitment || next.Snapshot().AddressKeyCommitment != epoch.AddressKeyCommitment) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        if (input.DeviceId != binding.DeviceId || next.Device(binding.DeviceId, true) != device.Public || next.Snapshot().OwnerId != binding.IdentityId || binding.MembershipVersion != 0 ||
            await context.Db.Set<HouseholdLocalMembershipProposal>().AnyAsync(ct)) throw Refusal();
        var row = await context.Db.Set<HouseholdLocalSecret>().SingleAsync(row => row.Slot == OwnerRecoverySlot, ct);
        var original = StrictJsonPayload.Deserialize(row.Frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdOwnerRecoveryFrame, 128 * 1024);
        using (original) { if (original.DevicePossession is not null) throw Refusal(); }
        var stored = JsonSerializer.SerializeToUtf8Bytes(frame with { DevicePossession = request.SignedDevicePossession }, HouseholdRecoveryPrivateJsonContext.Default.HouseholdOwnerRecoveryFrame);
        var rootBytes = root.ExportForProtectedStorage(); var epochBytes = epoch.ExportForProtectedStorage();
        try
        {
            if (stored.Length > 128 * 1024) throw Refusal();
            var json = JsonSerializer.Serialize(input, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership);
            if (Encoding.UTF8.GetByteCount(json) > 8 * 1024 * 1024) throw Refusal();
            await StoreRootAsync(context.Db, root.Public, rootBytes, ct);
            await AddImmutableSecretAsync(context.Db, PreparedEpochSlot(next.Snapshot().OperationId), epochBytes, ct);
            CryptographicOperations.ZeroMemory(row.Frame); row.Frame = stored;
            context.Db.Add(new HouseholdLocalMembershipProposal { OperationId = next.Snapshot().OperationId, ExpectedVersion = history.Current.Version,
                BodyJson = json, Hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json))) });
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(stored); CryptographicOperations.ZeroMemory(rootBytes); CryptographicOperations.ZeroMemory(epochBytes); }
    }

    /// <summary>Removes only imported kit state after its device is actually present in the verified retained recovery proof and original request is complete.</summary>
    internal async Task CompleteOwnerRecoveryAsync(HouseholdJournalAuthority authority, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var history = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (history.Current.Device(binding.DeviceId, true) != device.Public || await context.Db.Set<HouseholdLocalMembershipProposal>().AnyAsync(ct)) throw Refusal();
        await context.Db.Set<HouseholdLocalSecret>().Where(row => row.Slot == OwnerRecoverySlot).ExecuteDeleteAsync(ct); await context.CommitAsync(ct);
    }
}
