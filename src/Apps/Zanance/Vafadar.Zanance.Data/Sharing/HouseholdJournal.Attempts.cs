using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Literal retained ciphertext attempt over one original actual financial cutoff; it never contains a backup password or clear private key.</summary>
public sealed class HouseholdJournalAttempt
{
    internal HouseholdJournalAttempt(HouseholdLocalAttempt row, HouseholdOpenRound open, HouseholdCommitRound commit, HouseholdPartCommitment[] parts)
    { Open = open; Commit = commit; Parts = Array.AsReadOnly(parts); Cutoff = row.Cutoff; CaptureHash = row.CaptureHash;
        FinancialCursor = row.FinancialCursor; RemoteId = row.RemoteId; Committed = row.Committed; Abandoned = row.Abandoned; }
    /// <summary>Original exact operation/attempt/predecessor/member/key request.</summary>
    public HouseholdOpenRound Open { get; }
    /// <summary>Original exact signed complete manifest.</summary>
    public HouseholdCommitRound Commit { get; }
    /// <summary>Original exact signed part commitments; ciphertext bodies are read separately in bounded pages and never regenerated after process loss.</summary>
    public IReadOnlyList<HouseholdPartCommitment> Parts { get; }
    /// <summary>Original actual local outbox cutoff.</summary>
    public long Cutoff { get; }
    /// <summary>Original actual frozen local capture identity.</summary>
    public string CaptureHash { get; }
    /// <summary>Expected original per-row financial cursor after this attempt.</summary>
    public long FinancialCursor { get; }
    /// <summary>Exact actual acknowledged original staging resource, otherwise absent.</summary>
    public Guid? RemoteId { get; }
    /// <summary>True only after an exact original completion acknowledgement was recorded.</summary>
    public bool Committed { get; }
    /// <summary>Explicit acknowledged abandonment; an uncertain operation remains retained.</summary>
    public bool Abandoned { get; }
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdJournalAttempt);
}

public sealed partial class HouseholdJournal
{
    /// <summary>Reads the exact original candidate after process loss, including a previously acknowledged completion whose local application remains unfinished.</summary>
    public async Task<HouseholdJournalAttempt?> ReadAttemptAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalAttempt>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (row is null) { if (await context.Db.Set<HouseholdLocalAttemptPart>().AnyAsync(ct)) throw Refusal(); return null; }
        if (row.Parts is < 0 or > HouseholdPublicationDigest.MaximumParts || row.OpenJson.Length > 4096 || row.CommitJson.Length > HouseholdLimits.MembershipBytes * 2) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var open = JsonSerializer.Deserialize(row.OpenJson, HouseholdMetadataJsonContext.Default.HouseholdOpenRound) ?? throw Refusal();
        var commit = JsonSerializer.Deserialize(row.CommitJson, HouseholdMetadataJsonContext.Default.HouseholdCommitRound) ?? throw Refusal();
        var storedParts = await context.Db.Set<HouseholdLocalAttemptPart>().AsNoTracking().OrderBy(part => part.Index).Take(row.Parts + 1)
            .Select(part => new { part.Index, part.RecordId, part.BaseVersion, part.Digest, part.SerializedBytes, part.CiphertextBytes }).ToArrayAsync(ct);
        if (storedParts.Length != row.Parts || storedParts.Any(part => part.SerializedBytes is < 1 or > 2 * 1024 * 1024 || part.CiphertextBytes is < 1 or > HouseholdPublicationDigest.MaximumPartCharacters)) throw Refusal();
        var parts = storedParts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray();
        ValidateAttempt(binding, membership, row, open, commit, parts, storedParts.Sum(part => (long)part.CiphertextBytes)); context.Check(); return new(row, open, commit, parts);
    }

    /// <summary>Reads at most eight original ciphertext bodies under fresh captured native authority; no connection survives a network request.</summary>
    public async Task<HouseholdOpaquePart[]> ReadAttemptPartsAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt,
        int offset, int count, CancellationToken ct = default)
    {
        if (offset < 0 || count is < 1 or > 8) throw Refusal();
        var descriptor = await ReadAttemptAsync(authority, ct) ?? throw Refusal();
        if (descriptor.Open.OperationId != operation || descriptor.Open.AttemptId != attempt || offset > descriptor.Parts.Count - count) throw Refusal();
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        if (row.Abandoned) throw Refusal();
        var stored = await context.Db.Set<HouseholdLocalAttemptPart>().AsNoTracking().Where(part => part.Index >= offset && part.Index < offset + count)
            .OrderBy(part => part.Index).ToArrayAsync(ct);
        if (stored.Length != count) throw Refusal();
        var result = new HouseholdOpaquePart[count];
        for (var index = 0; index < count; index++)
        {
            var value = stored[index]; var commitment = descriptor.Parts[offset + index];
            if (value.Index != offset + index || value.SerializedBytes is < 1 or > 2 * 1024 * 1024 ||
                Encoding.UTF8.GetByteCount(value.PartJson) != value.SerializedBytes) throw Refusal();
            var part = JsonSerializer.Deserialize(value.PartJson, HouseholdMetadataJsonContext.Default.HouseholdOpaquePart) ?? throw Refusal();
            if (part.Index != value.Index || part.RecordId != commitment.RecordId || part.BaseVersion != commitment.BaseVersion ||
                part.Digest != commitment.Digest || part.Ciphertext.Length != value.CiphertextBytes ||
                part.Ciphertext.Length is < 1 or > HouseholdPublicationDigest.MaximumPartCharacters ||
                HouseholdPublicationDigest.Ciphertext(part.Ciphertext) != part.Digest || SerializePart(part) != value.PartJson) throw Refusal();
            result[index] = part;
        }
        context.Check(); return result;
    }

    /// <summary>Only an unacknowledged zero-part local decision can be replanned without a server tombstone after the verified head changes.</summary>
    private async Task<bool> DiscardStaleUnpublishedNoOpAsync(HouseholdJournalAuthority authority, HouseholdJournalAttempt observed, CancellationToken ct)
    {
        if (observed.Committed || observed.Abandoned || observed.RemoteId is not null || observed.Parts.Count != 0 || observed.Commit.SignedCommit.Length != 0) return false;
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, observed.Open.OperationId, observed.Open.AttemptId, ct);
        if (row.Committed || row.Abandoned || row.RemoteId is not null || row.Parts != 0 || row.CommitJson != JsonSerializer.Serialize(observed.Commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound)) throw Refusal();
        if (binding.Cursor == observed.Open.ExpectedCursor && binding.MembershipVersion == observed.Open.MembershipVersion) return false;
        await RequireOriginalRoundAsync(context.Db, binding, row.OperationId, row.Cutoff, row.CaptureHash, ct);
        if (await context.Db.Set<HouseholdLocalAttemptPart>().AnyAsync(ct)) throw Refusal();
        context.Db.Remove(row); await context.CommitAsync(ct); return true;
    }

    /// <summary>Records only the original actual server resource id; another retry cannot redirect retained encrypted work.</summary>
    public async Task RecordAttemptRemoteAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, Guid remote, CancellationToken ct = default)
    {
        if (remote == Guid.Empty) throw Refusal();
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        if (row.Abandoned || row.RemoteId is { } prior && prior != remote) throw Refusal();
        row.RemoteId = remote; await context.CommitAsync(ct);
    }

    /// <summary>Records only an exact complete original acknowledgement; counts alone cannot authorize replacing ciphertext or advancing the original cutoff.</summary>
    public async Task RecordAttemptAcknowledgedAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt,
        HouseholdRoundHandle acknowledged, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        var open = JsonSerializer.Deserialize(row.OpenJson, HouseholdMetadataJsonContext.Default.HouseholdOpenRound) ?? throw Refusal();
        var commit = JsonSerializer.Deserialize(row.CommitJson, HouseholdMetadataJsonContext.Default.HouseholdCommitRound) ?? throw Refusal();
        if (row.Abandoned || row.RemoteId != acknowledged.Id || row.Parts == 0 || acknowledged.Abandoned || acknowledged.CommittedCursor != open.ExpectedCursor + 1 ||
            acknowledged.Parts != row.Parts || acknowledged.SignedCommit != commit.SignedCommit) throw Refusal();
        await RequireOriginalRoundAsync(context.Db, binding, operation, row.Cutoff, row.CaptureHash, ct);
        row.Committed = true; await context.CommitAsync(ct);
    }

    /// <summary>A verified empty financial plan needs no server publication; records only that local no-op, against the unchanged complete signed replica.</summary>
    public async Task RecordNoOpAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        var open = JsonSerializer.Deserialize(row.OpenJson, HouseholdMetadataJsonContext.Default.HouseholdOpenRound) ?? throw Refusal();
        var commit = JsonSerializer.Deserialize(row.CommitJson, HouseholdMetadataJsonContext.Default.HouseholdCommitRound) ?? throw Refusal();
        if (row.Parts != 0 || row.Abandoned || commit.SignedCommit.Length != 0 || open.ExpectedCursor != binding.Cursor ||
            row.FinancialCursor != binding.FinancialCursor || open.MembershipVersion != binding.MembershipVersion) throw Refusal();
        await RequireOriginalRoundAsync(context.Db, binding, operation, row.Cutoff, row.CaptureHash, ct);
        // This id is an explicit local original-operation receipt, never a remotely created resource.
        row.RemoteId = operation; row.Committed = true; await context.CommitAsync(ct);
    }

    /// <summary>Explicit actual remote abandonment is recorded separately; an uncertain or acknowledged commit cannot be discarded.</summary>
    public async Task RecordAttemptAbandonedAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, Guid actualRemote, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        if (row.Committed || row.RemoteId != actualRemote || actualRemote == Guid.Empty) throw Refusal();
        row.Abandoned = true; await context.CommitAsync(ct);
    }

    /// <summary>Clears only after the original applied transaction removed its frozen cutoff and advanced its original financial cursor.</summary>
    public async Task CompleteAppliedAttemptAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, CancellationToken ct = default)
    {
        if (!await TryCompleteAppliedAttemptAsync(authority, operation, attempt, ct)) throw Refusal();
    }

    /// <summary>Recovers process loss after the original financial transaction committed but before its separate ciphertext journal retirement; never creates a new cutoff first.</summary>
    public async Task<bool> TryCompleteAppliedAttemptAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        var original = await context.Db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
        if (!row.Committed || original is null || original.TransportKind != ProfileSynchronizationKind.Household ||
            original.ProfileId != binding.SpaceId || original.DeviceId != binding.DeviceId || original.GenerationPath != binding.GenerationPath ||
            original.IdentityId != binding.IdentityId || original.PeerKey != binding.PeerKey || original.Cursor < row.FinancialCursor ||
            await context.Db.Set<SyncClientRound>().AnyAsync(ct) || await context.Db.Set<SyncOutboxRow>().AnyAsync(value => value.Sequence <= row.Cutoff, ct)) return false;
        await context.Db.Set<HouseholdLocalAttemptPart>().ExecuteDeleteAsync(ct); context.Db.Remove(row); await context.CommitAsync(ct);
        return true;
    }

    /// <summary>Retires only an actually acknowledged server abandonment; the original frozen financial operation/cutoff remains intact for conflict review or replanning.</summary>
    public async Task CompleteAbandonedAttemptAsync(HouseholdJournalAuthority authority, Guid operation, Guid attempt, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await RequireAttemptAsync(context.Db, operation, attempt, ct);
        if (!row.Abandoned || row.Committed || row.RemoteId is null) throw Refusal();
        await RequireOriginalRoundAsync(context.Db, binding, operation, row.Cutoff, row.CaptureHash, ct);
        await context.Db.Set<HouseholdLocalAttemptPart>().ExecuteDeleteAsync(ct); context.Db.Remove(row); await context.CommitAsync(ct);
    }

    /// <summary>The original file/connection/cutoff is read under this same protected transaction, not guessed from a proposal body.</summary>
    private static async Task RequireOriginalRoundAsync(ZananceDbContext db, HouseholdLocalBinding household, Guid operation, long cutoff, string captureHash, CancellationToken ct)
    {
        var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct); var round = await db.Set<SyncClientRound>().SingleOrDefaultAsync(ct);
        if (binding is null || !binding.Enabled || binding.Applying || binding.TransportKind != ProfileSynchronizationKind.Household ||
            binding.ProfileId != household.SpaceId || binding.DeviceId != household.DeviceId || binding.IdentityId != household.IdentityId ||
            binding.GenerationPath != household.GenerationPath || binding.PeerKey != household.PeerKey || round is null || round.OperationId != operation ||
            round.Cutoff != cutoff || round.ContentHash != captureHash || round.Phase is not (1 or 2)) throw Refusal();
    }
    /// <summary>Validates literal complete stored ciphertext and original signed authority without regenerating any signature/IV.</summary>
    private static void ValidateAttempt(HouseholdLocalBinding binding, MembershipState membership, HouseholdLocalAttempt row,
        HouseholdOpenRound open, HouseholdCommitRound commit, IReadOnlyList<HouseholdPartCommitment> parts, long ciphertextBytes)
    {
        if (row.OperationId != open.OperationId || row.AttemptId != open.AttemptId || open.OperationId == Guid.Empty || open.AttemptId == Guid.Empty ||
            open.DeviceId != binding.DeviceId || commit.DeviceId != binding.DeviceId || open.SchemaHash != HouseholdFinancialSchema.Hash || row.Cutoff < 0 ||
            open.ExpectedCursor is < 0 or long.MaxValue || row.FinancialCursor < 0 ||
            !SyncProtocolDigest.IsDigest(row.CaptureHash) || row.Parts != parts.Count || parts.Count > HouseholdPublicationDigest.MaximumParts ||
            parts.Any(part => part is null) || ciphertextBytes < 0 || ciphertextBytes > HouseholdPublicationDigest.MaximumBytes ||
            !membership.ByVersion.TryGetValue(open.MembershipVersion, out var origin) || open.KeyEpoch != origin.Epoch) throw Refusal();
        if (parts.Count == 0)
        { if (commit.SignedCommit.Length != 0) throw Refusal(); return; }
        var statement = StandardJoseSignatures.VerifyJson(commit.SignedCommit, origin.Device(binding.DeviceId, requireWrite: true), "zanance-household-commit+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
        if (statement.Format != 1 || statement.SpaceId != binding.SpaceId || statement.DeviceId != binding.DeviceId || statement.OperationId != open.OperationId ||
            statement.AttemptId != open.AttemptId || statement.ExpectedCursor != open.ExpectedCursor || statement.Cursor != open.ExpectedCursor + 1 ||
            statement.MembershipVersion != open.MembershipVersion || statement.KeyEpoch != open.KeyEpoch || statement.Parts != parts.Count ||
            statement.ContentHash != HouseholdPublicationDigest.Manifest(parts)) throw Refusal();
    }
    /// <summary>Only the same immutable original operation/attempt can modify its acknowledgement metadata.</summary>
    private static async Task<HouseholdLocalAttempt> RequireAttemptAsync(ZananceDbContext db, Guid operation, Guid attempt, CancellationToken ct)
    {
        var row = await db.Set<HouseholdLocalAttempt>().SingleOrDefaultAsync(ct) ?? throw Refusal();
        if (row.OperationId != operation || row.AttemptId != attempt) throw Refusal(); return row;
    }
    /// <summary>Canonical bounded original ciphertext serialization; no original readable financial payload participates.</summary>
    private static string SerializePart(HouseholdOpaquePart part)
    {
        var result = JsonSerializer.Serialize(part, HouseholdMetadataJsonContext.Default.HouseholdOpaquePart);
        if (Encoding.UTF8.GetByteCount(result) > 2 * 1024 * 1024) throw Refusal(); return result;
    }
    /// <summary>Only one original literal part plus its exact bounded metadata is tracked at a time by the streaming writer.</summary>
    private static HouseholdLocalAttemptPart StoredPart(HouseholdOpaquePart part)
    {
        var json = SerializePart(part);
        return new() { Index = part.Index, PartJson = json, RecordId = part.RecordId, BaseVersion = part.BaseVersion,
            Digest = part.Digest, CiphertextBytes = part.Ciphertext.Length, SerializedBytes = Encoding.UTF8.GetByteCount(json) };
    }
}
