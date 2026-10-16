using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Reads the original already-verified signed origin of an existing key for explicit historical recipient grants.</summary>
    public async Task<HouseholdMembershipStatement> ReadEpochOriginAsync(HouseholdJournalAuthority authority, long epoch, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var state = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (!state.ByEpoch.TryGetValue(epoch, out var origin)) throw Refusal(); context.Check(); return origin.Snapshot();
    }
    /// <summary>Returns exact already-verified public proofs for bounded incremental continuation, without exposing private frames.</summary>
    public async Task<IReadOnlyList<HouseholdMembershipProof>> ReadHistoryAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.MembershipVersion == 0)
        {
            // Initial app setup can lose the committed genesis response before it records any
            // verified chain. Empty history is valid only when no unexplained local proof exists.
            if (await context.Db.Set<HouseholdLocalMembership>().AnyAsync(ct)) throw Refusal();
            context.Check(); return Array.Empty<HouseholdMembershipProof>();
        }
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        _ = await ReadMembershipAsync(context.Db, binding, device, ct);
        var rows = await context.Db.Set<HouseholdLocalMembership>().AsNoTracking().OrderBy(row => row.Version).ToArrayAsync(ct);
        var proofs = rows.Select(row => StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.ProofJson),
            HouseholdMetadataJsonContext.Default.HouseholdMembershipProof, 512 * 1024)).ToArray();
        context.Check(); return Array.AsReadOnly(proofs);
    }

    /// <summary>Finds only actually missing historical keys; a corrupt existing frame is refused rather than downloaded over or silently replaced.</summary>
    public async Task<IReadOnlyList<long>> MissingEpochsAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var state = await ReadMembershipAsync(context.Db, binding, device, ct); var missing = new List<long>();
        foreach (var epoch in state.ByEpoch.Keys.Order())
        {
            var slot = EpochSlot(epoch);
            if (!await context.Db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == slot, ct)) missing.Add(epoch);
            else { using var verified = await ReadEpochCoreAsync(context.Db, state, epoch, ct); }
        }
        context.Check(); return missing.AsReadOnly();
    }

    /// <summary>Verifies only exact bound original public evidence; sender/owner replacement after a legitimate transfer stays part of signed continuity.</summary>
    private static MembershipState VerifyMembership(HouseholdLocalBinding binding, IReadOnlyList<HouseholdMembershipProof> proofs,
        PublicSigningRecipientIdentity device)
    {
        if (binding.SpaceId == Guid.Empty || binding.DeviceId == Guid.Empty || binding.DeviceFingerprint != device.Fingerprint ||
            proofs.Count is < 1 or > 4096 || proofs.Any(proof => proof is null || proof.SignedStatement is null) ||
            proofs.Sum(proof => (long)proof.SignedStatement.Length + (proof.SignedAcceptanceClaim?.Length ?? 0) + (proof.TransferAcceptance?.Length ?? 0)) > 64L * 1024 * 1024)
            throw Refusal();
        var root = new PublicSigningRecipientIdentity(binding.PinnedSigningKey, binding.PinnedWrappingKey);
        IReadOnlyList<HouseholdVerifiedMembership> states;
        if (binding.OwnerCreation)
        {
            var first = HouseholdTrustChain.Genesis(binding.SpaceId, root, proofs[0]);
            var genesis = first.Snapshot();
            if (genesis.OwnerId != binding.IdentityId || genesis.Members[0].Devices[0].Id != binding.DeviceId ||
                genesis.Members[0].Devices[0].Keys != new HouseholdPublicKeys(device.SigningKey, device.WrappingKey)) throw Refusal();
            var list = new List<HouseholdVerifiedMembership> { first };
            foreach (var proof in proofs.Skip(1)) list.Add(HouseholdTrustChain.Advance(list[^1], proof));
            states = list;
        }
        else
        {
            if (binding.SignedInvitation is null) throw Refusal();
            if (HouseholdSetupAnchors.Kind(binding.SignedInvitation) == HouseholdSetupAnchors.DeviceAccess)
            {
                var anchor = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(binding.SignedInvitation),
                    HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage, 8192);
                if (anchor.OwnerKeys != new HouseholdPublicKeys(root.SigningKey, root.WrappingKey)) throw Refusal();
                states = HouseholdTrustChain.DeviceHistory(anchor, proofs, binding.IdentityId, binding.DeviceId, device, requireCurrentMembership: false).States;
            }
            else if (HouseholdSetupAnchors.Kind(binding.SignedInvitation) == HouseholdSetupAnchors.OwnerRecovery)
            {
                var anchor = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(binding.SignedInvitation),
                    HouseholdMetadataJsonContext.Default.HouseholdRecoveryAnchor, 8192);
                if (anchor.SpaceId != binding.SpaceId || anchor.InitialOwnerRoot != new HouseholdPublicKeys(root.SigningKey, root.WrappingKey)) throw Refusal();
                states = HouseholdTrustChain.RecoveredHistory(anchor, proofs, binding.IdentityId, binding.DeviceId, device).States;
            }
            else
            {
            // Historical consent remains verifiable after a later revocation; it does not grant
            // current device/write authority. The binding is explicitly disabled when absent below.
            states = HouseholdTrustChain.JoinHistory(root, binding.SignedInvitation, proofs, binding.IdentityId, binding.DeviceId, device,
                requireCurrentMembership: false).States;
            }
        }
        return new(states[^1], states.ToDictionary(state => state.Version), states.ToDictionary(state => state.Epoch));
    }

    /// <summary>Protected local bytes are rechecked against exact original signed authority rather than inferred from a stored counter.</summary>
    private static async Task<MembershipState> ReadMembershipAsync(ZananceDbContext db, HouseholdLocalBinding binding,
        SigningRecipientIdentity device, CancellationToken ct)
    {
        var rows = await db.Set<HouseholdLocalMembership>().AsNoTracking().OrderBy(row => row.Version).ToArrayAsync(ct);
        var proofs = new List<HouseholdMembershipProof>();
        foreach (var row in rows)
        {
            if (row.Version != proofs.Count + 1 || row.ProofJson.Length > 524288) throw Refusal();
            var bytes = Encoding.UTF8.GetBytes(row.ProofJson);
            var proof = StrictJsonPayload.Deserialize(bytes, HouseholdMetadataJsonContext.Default.HouseholdMembershipProof, 512 * 1024);
            if (HouseholdTrustChain.Digest(proof.SignedStatement) != row.Hash) throw Refusal();
            proofs.Add(proof);
        }
        var state = VerifyMembership(binding, proofs, device.Public);
        if (state.Current.Version != binding.MembershipVersion || state.Current.Hash != binding.MembershipHead) throw Refusal();
        return state;
    }

    /// <summary>Canonical original public proof representation; no private material participates in JSON.</summary>
    private static string SerializeProof(HouseholdMembershipProof proof)
    {
        var result = JsonSerializer.Serialize(proof, HouseholdMetadataJsonContext.Default.HouseholdMembershipProof);
        if (Encoding.UTF8.GetByteCount(result) > 512 * 1024) throw Refusal();
        return result;
    }
}
