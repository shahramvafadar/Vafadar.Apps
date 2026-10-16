using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Seals the original frozen source directly into its captured protected transaction. No partial seal or source read can reach HTTP.</summary>
    private async Task<HouseholdSealedPublication> SealOriginalAsync(HouseholdJournalAuthority authority, SyncRoundWork work,
        HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch, HouseholdJournalKeys keys,
        HouseholdFinancialReplica replica, IReadOnlyDictionary<SyncRecordKey, SyncRecord> baselines, CancellationToken ct, Guid? ownershipReception = null)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var writePermission = await CaptureLocalWritePermissionAsync(context.Db, context.Db.WorkClock, ct) ?? throw Refusal();
        if (binding.SendingDisabled || binding.Cursor != replica.Cursor || binding.FinancialCursor != replica.FinancialCursor ||
            binding.PublicationHead != replica.PublicationHead || binding.MembershipVersion != membership.Version || binding.MembershipHead != membership.Hash ||
            binding.DeviceId != work.Authority.Device || membership.Device(binding.DeviceId, requireWrite: false) != keys.Device.Public ||
            epoch.Epoch != membership.Epoch) throw Refusal();
        await RequireOriginalRoundAsync(context.Db, binding, work.Operation, work.Cutoff, work.ContentHash, ct);
        if (await context.Db.Set<HouseholdLocalAttempt>().AnyAsync(ct) || await context.Db.Set<HouseholdLocalAttemptPart>().AnyAsync(ct)) throw Refusal();
        try { writePermission.Demand(); }
        catch (HouseholdWriteRejectedException error) when (error.Reason is HouseholdWriteBlockReason.Viewer or HouseholdWriteBlockReason.Expired or HouseholdWriteBlockReason.Unpublished or HouseholdWriteBlockReason.Rotation)
        {
            // An authenticated read-only recipient with no own proposals needs only a local
            // original-cutoff receipt before consuming already verified incoming finance.
            // Retained local edits remain blocked rather than silently discarded or uploaded.
            var boundary = membership.Snapshot();
            if (error.Reason == HouseholdWriteBlockReason.Rotation)
            {
                if (ownershipReception is not { } operation) throw;
                await RequireOwnershipReceptionAsync(context.Db, operation, ct, replica.FinancialCursor);
            }
            if (error.Reason == HouseholdWriteBlockReason.Unpublished)
            {
                var receiving = await context.Db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
                if (binding.OwnerCreation || receiving is null || receiving.LastSuccessAt is not null ||
                    receiving.TransportKind != ProfileSynchronizationKind.Household || receiving.ProfileId != binding.SpaceId || receiving.DeviceId != binding.DeviceId ||
                    receiving.IdentityId != binding.IdentityId || receiving.PeerKey != binding.PeerKey || receiving.GenerationPath != binding.GenerationPath ||
                    receiving.SchemaHash != HouseholdFinancialSchema.Hash) throw;
                // This exact connected first receiver can acknowledge an empty local cutoff;
                // it still cannot seal/send any financial proposal before its first atomic apply.
            }
            if (work.Parts.Any(part => part.Rows.Count != 0) || work.Resolutions.Length != 0 || !AllowsLocalReception(binding, membership, context.Db.WorkClock,
                ownershipRotation: error.Reason == HouseholdWriteBlockReason.Rotation && ownershipReception is not null) ||
                replica.Cursor < boundary.PublicationCursor || replica.Cursor == boundary.PublicationCursor && replica.PublicationHead != boundary.PublicationHead) throw;
            var open = new HouseholdOpenRound(binding.DeviceId, work.Operation, Guid.NewGuid(), replica.Cursor, membership.Version, epoch.Epoch, HouseholdFinancialSchema.Hash);
            var commit = new HouseholdCommitRound(binding.DeviceId, "");
            var readReceipt = new HouseholdLocalAttempt { OperationId = open.OperationId, AttemptId = open.AttemptId, Cutoff = work.Cutoff, CaptureHash = work.ContentHash,
                OpenJson = JsonSerializer.Serialize(open, HouseholdMetadataJsonContext.Default.HouseholdOpenRound),
                CommitJson = JsonSerializer.Serialize(commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound), FinancialCursor = replica.FinancialCursor, Parts = 0 };
            context.Db.Add(readReceipt); await context.CommitAsync(ct);
            return new(open, commit, new(true, replica.Snapshot(), []), replica.FinancialCursor, [], 0);
        }
        using var source = new FrozenAttachmentSource(context, PersonalSyncStore.BlobNeeds(work.Parts.SelectMany(part => part.Rows)
            .Select(row => new SyncRecord(row.Record, row.BaseVersion, row.Values))), ct);
        var pending = new List<HouseholdLocalAttemptPart>(8);
        var root = Path.GetPathRoot(context.Path) ?? throw Refusal();
        var result = replica.SealTo(membership, epoch, binding.DeviceId, keys.Device, work.Operation, Guid.NewGuid(),
            work.Parts.SelectMany(part => part.Rows).ToArray(), work.Resolutions, source, cryptography, part =>
            {
                ct.ThrowIfCancellationRequested(); context.Check();
                var stored = StoredPart(part); context.Db.Add(stored); pending.Add(stored);
                if (pending.Count == 8) Flush();
            }, ct, baselines, address => context.Db.Set<HouseholdLocalEvent>().Where(row => row.RecordId == address && row.Cursor <= replica.Cursor)
                .Select(row => (long?)row.Cursor).Max() ?? 0);
        // A failed original planner emits nothing. Disposing this transaction rolls back without
        // recording review through a second connection while this protected writer is held.
        if (!result.Plan.Succeeded) return result;
        Flush();
        var row = new HouseholdLocalAttempt { OperationId = result.Open.OperationId, AttemptId = result.Open.AttemptId,
            Cutoff = work.Cutoff, CaptureHash = work.ContentHash, FinancialCursor = result.FinancialCursor, Parts = result.Parts.Count,
            OpenJson = JsonSerializer.Serialize(result.Open, HouseholdMetadataJsonContext.Default.HouseholdOpenRound),
            CommitJson = JsonSerializer.Serialize(result.Commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound) };
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        ValidateAttempt(binding, await ReadMembershipAsync(context.Db, binding, device, ct), row, result.Open, result.Commit, result.Parts, result.CiphertextBytes);
        ct.ThrowIfCancellationRequested(); writePermission.Demand(); context.Db.Add(row); await context.CommitAsync(ct); return result;

        void Flush()
        {
            if (pending.Count == 0) return;
            ct.ThrowIfCancellationRequested(); context.Check();
            // SQLite may need both a new encrypted page and its rollback copy. Refuse before
            // extending the transaction rather than deleting another profile's data for space.
            if (new DriveInfo(root).AvailableFreeSpace < pending.Sum(part => (long)part.SerializedBytes) * 2 + 16L * 1024 * 1024)
                throw new IOException("Insufficient space for the captured protected household attempt.");
            context.Db.SaveChanges();
            foreach (var part in pending) context.Db.Entry(part).State = EntityState.Detached;
            pending.Clear();
        }
    }

    /// <summary>One frozen original2MiB buffer feeds owned128KiB pieces under the same protected transaction as the seal.</summary>
    private sealed class FrozenAttachmentSource : IHouseholdAttachmentSource, IDisposable
    {
        private readonly JournalContext context;
        private readonly Dictionary<string, long> needs;
        private readonly CancellationToken cancellation;
        private byte[]? cached;
        private string? cachedHash;
        private int cachedIndex = -1;
        internal FrozenAttachmentSource(JournalContext context, IReadOnlyList<SyncBlobNeed> needs, CancellationToken ct)
        {
            this.context = context; cancellation = ct;
            this.needs = needs.ToDictionary(item => item.Hash, item => item.Length, StringComparer.Ordinal);
            if (needs.Any(item => item.Length < 0) || needs.Sum(item => item.Length) > SyncGraphValidation.MaximumProfileBytes) throw Refusal();
        }
        /// <inheritdoc />
        public IEnumerable<string> Hashes => needs.Keys;
        /// <inheritdoc />
        public byte[] ReadPiece(string hash, long length, int index)
        {
            cancellation.ThrowIfCancellationRequested(); context.Check();
            const int originalBytes = 2 * 1024 * 1024; const int sealedBytes = 128 * 1024;
            var offset = (long)index * sealedBytes;
            if (!needs.TryGetValue(hash, out var expected) || expected != length || index < 0 || offset >= length) throw Refusal();
            var originalIndex = (int)(offset / originalBytes);
            if (cachedHash != hash || cachedIndex != originalIndex)
            {
                Dispose();
                cached = context.Db.Set<SyncBlobPart>().AsNoTracking().Where(row => row.Hash == hash && row.Part == originalIndex)
                    .Select(row => row.Data).SingleOrDefault() ?? throw Refusal();
                if (cached.Length != Math.Min(originalBytes, length - (long)originalIndex * originalBytes))
                { Dispose(); throw Refusal(); }
                cachedHash = hash; cachedIndex = originalIndex;
            }
            cancellation.ThrowIfCancellationRequested();
            return cached!.AsSpan((int)(offset % originalBytes), (int)Math.Min(sealedBytes, length - offset)).ToArray();
        }
        /// <inheritdoc />
        public void Dispose()
        { if (cached is not null) CryptographicOperations.ZeroMemory(cached); cached = null; cachedHash = null; cachedIndex = -1; }
    }
}
