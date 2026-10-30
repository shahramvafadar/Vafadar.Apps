using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Persists a candidate key before any root transition reaches the network; a candidate alone grants no role or outgoing authority.</summary>
    public async Task StageEpochAsync(HouseholdJournalAuthority authority, Guid membershipOperation, HouseholdEpochMaterial material, CancellationToken ct = default)
    {
        if (membershipOperation == Guid.Empty) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        if (binding.MembershipVersion == 0)
        { if (!binding.OwnerCreation || material.Epoch != 1) throw Refusal(); }
        else
        {
            var state = (await ReadMembershipAsync(context.Db, binding, device, ct)).Current;
            if (binding.SendingDisabled || state.Epoch == long.MaxValue || material.Epoch != state.Epoch + 1 ||
                state.Device(binding.DeviceId, requireWrite: false) != device.Public ||
                state.Snapshot().AddressKeyCommitment != material.AddressKeyCommitment || state.Snapshot().KeyCommitment == material.KeyCommitment) throw Refusal();
        }
        var slot = PreparedEpochSlot(membershipOperation);
        if (!await context.Db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == slot, ct) &&
            await context.Db.Set<HouseholdLocalSecret>().CountAsync(row => row.Slot.StartsWith("prepared-epoch:"), ct) >= 16) throw Refusal();
        var frame = material.ExportForProtectedStorage();
        try { await AddImmutableSecretAsync(context.Db, slot, frame, ct); await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Resumes only the same staged original generation; a missing candidate never generates a new replacement key.</summary>
    public async Task<HouseholdEpochMaterial> ReadStagedEpochAsync(HouseholdJournalAuthority authority, Guid membershipOperation, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == PreparedEpochSlot(membershipOperation), ct) ?? throw Refusal();
        HouseholdEpochMaterial material;
        try { material = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Frame); } finally { CryptographicOperations.ZeroMemory(row.Frame); }
        try { context.Check(); return material; } catch { material.Dispose(); throw; }
    }

    /// <summary>Promotes an original staged key only after that exact root-signed operation/epoch/commitments are actually verified locally.</summary>
    public async Task PromoteStagedEpochAsync(HouseholdJournalAuthority authority, Guid membershipOperation, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var origin = membership.ByVersion.Values.SingleOrDefault(state => state.Snapshot().OperationId == membershipOperation) ?? throw Refusal();
        if (origin.Snapshot().OwnerId != binding.IdentityId) throw Refusal();
        var slot = PreparedEpochSlot(membershipOperation);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == slot, ct);
        if (row is null)
        { using var existing = await ReadEpochCoreAsync(context.Db, membership, origin.Epoch, ct); return; }
        try
        {
            using var material = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Frame);
            if (material.Epoch != origin.Epoch || material.KeyCommitment != origin.Snapshot().KeyCommitment ||
                material.AddressKeyCommitment != origin.Snapshot().AddressKeyCommitment) throw Refusal();
            await AddImmutableSecretAsync(context.Db, EpochSlot(material.Epoch), row.Frame, ct);
            await context.Db.Set<HouseholdLocalSecret>().Where(value => value.Slot == slot).ExecuteDeleteAsync(ct);
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    /// <summary>Retains the exact original signed/encrypted membership bytes before the first remote publish attempt.</summary>
    public async Task StageMembershipProposalAsync(HouseholdJournalAuthority authority, HouseholdPublishMembership proposal, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var trustedRoot = binding.MembershipVersion == 0 ? new(binding.PinnedSigningKey, binding.PinnedWrappingKey)
            : (await ReadMembershipAsync(context.Db, binding, device, ct)).Current.Root;
        var statement = StandardJoseSignatures.VerifyJson(proposal.SignedStatement, trustedRoot, "zanance-household-membership+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
        if (HouseholdMembershipRules.ValidateShape(statement).Count != 0 || statement.SpaceId != binding.SpaceId || proposal.DeviceId != binding.DeviceId ||
            statement.PreviousOwnerId != binding.IdentityId || statement.ExpectedVersion != binding.MembershipVersion ||
            statement.PublicationCursor != binding.Cursor || statement.PublicationHead != binding.PublicationHead || binding.SendingDisabled)
            throw Refusal();
        if (proposal.Grants is null || proposal.Grants.Length is < 1 or > 384 || proposal.Grants.Any(grant => grant is null ||
            grant.Ciphertext is null || grant.Ciphertext.Length > 32768 || HouseholdPublicationDigest.Ciphertext(grant.Ciphertext) != grant.Digest)) throw Refusal();
        var json = JsonSerializer.Serialize(proposal, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership);
        if (Encoding.UTF8.GetByteCount(json) > 8 * 1024 * 1024) throw Refusal();
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var existing = await context.Db.Set<HouseholdLocalMembershipProposal>().SingleOrDefaultAsync(ct);
        if (existing is not null)
        { if (existing.OperationId != statement.OperationId || existing.ExpectedVersion != statement.ExpectedVersion || existing.Hash != hash || existing.BodyJson != json) throw Refusal(); return; }
        context.Db.Add(new HouseholdLocalMembershipProposal { OperationId = statement.OperationId, ExpectedVersion = statement.ExpectedVersion, Hash = hash, BodyJson = json });
        await context.CommitAsync(ct);
    }

    /// <summary>Returns exact retained original bytes after process loss, never fresh IVs/signatures for the same operation.</summary>
    public async Task<HouseholdPublishMembership?> ReadMembershipProposalAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct);
        return await ReadMembershipProposalInContextAsync(context, authority, ct);
    }

    /// <summary>Uses the caller's original protected transaction while preserving this reader's complete validation.</summary>
    private async Task<HouseholdPublishMembership?> ReadMembershipProposalInContextAsync(JournalContext context,HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
         var binding = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalMembershipProposal>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (row is null) return null;
        if (Encoding.UTF8.GetByteCount(row.BodyJson) > 8 * 1024 * 1024 || Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(row.BodyJson))) != row.Hash) throw Refusal();
        var result = JsonSerializer.Deserialize(row.BodyJson, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership) ?? throw Refusal();
        if (result.DeviceId != binding.DeviceId) throw Refusal(); context.Check(); return result;
    }

    /// <summary>Clears only a root operation already present with the exact original signed bytes in the verified local chain.</summary>
    public async Task CompleteMembershipProposalAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalMembershipProposal>().SingleOrDefaultAsync(ct);
        if (row is null) return;
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var original = JsonSerializer.Deserialize(row.BodyJson, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership) ?? throw Refusal();
        var accepted = membership.ByVersion.Values.SingleOrDefault(state => state.Snapshot().OperationId == row.OperationId);
        if (accepted is null || accepted.Hash != HouseholdTrustChain.Digest(original.SignedStatement)) throw Refusal();
        context.Db.Remove(row); await context.CommitAsync(ct);
    }

    /// <summary>Only an exact original operation can address candidate private material.</summary>
    private static string PreparedEpochSlot(Guid operation) => operation != Guid.Empty ? "prepared-epoch:" + operation.ToString("N") : throw Refusal();
}
