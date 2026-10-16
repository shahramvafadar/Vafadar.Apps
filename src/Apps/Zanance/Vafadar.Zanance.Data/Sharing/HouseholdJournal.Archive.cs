using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Original protected ciphertext remains the archive; this object holds no private key, open connection or plaintext blob buffer.</summary>
    private sealed class AttachmentArchive(HouseholdJournal journal, HouseholdJournalAuthority authority, long verifiedThrough) : IHouseholdAttachmentArchive
    {
        /// <inheritdoc />
        public Task<byte[]> ReadPieceAsync(string hash, long length, int index, CancellationToken ct) =>
            journal.ReadArchivedPieceAsync(authority, verifiedThrough, hash, length, index, ct);
    }

    /// <summary>Only complete validated replay can add an immutable derived pointer. Existing disagreement is corruption, not authority to rewrite the source.</summary>
    private static async Task RecordAttachmentSourcesAsync(ZananceDbContext db, IReadOnlyList<HouseholdAttachmentLocator> sources, CancellationToken ct)
    {
        var grouped = sources.GroupBy(source => source.Hash, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        foreach (var batch in grouped.Keys.Chunk(128))
        {
            var existing = await db.Set<HouseholdLocalBlobReference>().Where(row => batch.Contains(row.Hash)).ToDictionaryAsync(row => (row.Hash, row.PieceIndex), ct);
            foreach (var source in batch.SelectMany(hash => grouped[hash]))
            {
                if (!SyncProtocolDigest.IsDigest(source.Hash) || !SyncProtocolDigest.IsDigest(source.PieceHash) || source.Length is < 1 or > SyncGraphValidation.MaximumProfileBytes ||
                    source.Cursor < 1 || source.EventIndex < 0 || source.ElementIndex is < -1 or > 511 || source.PieceIndex < 0) throw Refusal();
                if (existing.TryGetValue((source.Hash, source.PieceIndex), out var prior))
                {
                    if (prior.Length != source.Length || prior.Cursor != source.Cursor || prior.EventIndex != source.EventIndex ||
                        prior.ElementIndex != source.ElementIndex || prior.PieceHash != source.PieceHash) throw Refusal();
                }
                else
                {
                    var row = new HouseholdLocalBlobReference { Hash = source.Hash, Length = source.Length, PieceIndex = source.PieceIndex,
                        Cursor = source.Cursor, EventIndex = source.EventIndex, ElementIndex = source.ElementIndex, PieceHash = source.PieceHash };
                    db.Add(row); existing.Add((row.Hash, row.PieceIndex), row);
                }
            }
        }
    }

    /// <summary>Reads at most one original2MiB content piece from its exact previously verified ciphertext sources under fresh native file/identity/key authority.</summary>
    private async Task<byte[]> ReadArchivedPieceAsync(HouseholdJournalAuthority authority, long verifiedThrough, string hash, long length, int index, CancellationToken ct)
    {
        const int originalBytes = 2 * 1024 * 1024; const int encryptedBytes = 128 * 1024;
        if (!SyncProtocolDigest.IsDigest(hash) || length is < 1 or > SyncGraphValidation.MaximumProfileBytes || index < 0 || (long)index * originalBytes >= length) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.Cursor < verifiedThrough || verifiedThrough < 1) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var first = index * (originalBytes / encryptedBytes);
        var size = (int)Math.Min(originalBytes, length - (long)index * originalBytes); var count = (size + encryptedBytes - 1) / encryptedBytes;
        var references = await context.Db.Set<HouseholdLocalBlobReference>().AsNoTracking().Where(row => row.Hash == hash && row.PieceIndex >= first && row.PieceIndex < first + count)
            .OrderBy(row => row.PieceIndex).ToArrayAsync(ct);
        if (references.Length != count || references.Where((row, position) => row.PieceIndex != first + position || row.Length != length || row.Cursor > verifiedThrough).Any()) throw Refusal();
        var output = new byte[size]; var written = 0; var epochs = new Dictionary<long, HouseholdEpochMaterial>();
        try
        {
            foreach (var source in references)
            {
                ct.ThrowIfCancellationRequested();
                var packet = await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == source.Cursor, ct) ?? throw Refusal();
                var receipt = ParseReceipt(packet);
                var stored = await context.Db.Set<HouseholdLocalEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == source.Cursor && row.Index == source.EventIndex, ct) ?? throw Refusal();
                var item = ParseEvent(stored);
                if (!membership.ByVersion.TryGetValue(item.MembershipVersion, out var origin)) throw Refusal();
                var previous = source.Cursor == 1 ? new string('0', 64) :
                    (await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == source.Cursor - 1, ct) ?? throw Refusal()).Hash;
                var commit = HouseholdTrustChain.Publication(origin, receipt, item.DeviceId, item.OperationId, item.AttemptId, source.Cursor - 1, previous);
                if (membership.ByVersion.TryGetValue(origin.Version + 1, out var successor))
                    HouseholdTrustChain.RequirePublicationHorizon(origin, successor, receipt, commit.Cursor);
                RequireInboxEvent(item, receipt, commit);
                if (!epochs.TryGetValue(origin.Epoch, out var epoch))
                { epoch = await ReadEpochCoreAsync(context.Db, membership, origin.Epoch, ct); epochs.Add(origin.Epoch, epoch); }
                var clear = epoch.Decrypt(cryptography, new(binding.SpaceId, epoch.Epoch, item.OperationId, HouseholdProtectionPurpose.Financial, "members"),
                    item.Ciphertext, origin.Device(item.DeviceId, requireWrite: true));
                HouseholdFinancialPayload payload;
                try { payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes); }
                finally { CryptographicOperations.ZeroMemory(clear); }
                try
                {
                    if (payload.Format != 1 || payload.SchemaHash != HouseholdFinancialSchema.Hash || payload.AttemptId != item.AttemptId || payload.Index != item.Index ||
                        payload.Record is not null || payload.Stream is not null) throw Refusal();
                    HouseholdAttachmentPiece piece;
                    if (source.ElementIndex == -1)
                    {
                        if (payload.Kind != HouseholdFinancialContentKind.Attachment || payload.Blobs is not null || payload.Attachment is not { } standalone ||
                            epoch.RecordAddress("attachment-piece", FormattableString.Invariant($"{hash}:{source.PieceIndex}")) != item.RecordId) throw Refusal();
                        piece = standalone;
                    }
                    else
                    {
                        if (payload.Kind != HouseholdFinancialContentKind.AttachmentBatch || payload.Attachment is not null || payload.Blobs is not { Length: >= 1 and <= 512 } batch ||
                            source.ElementIndex < 0 || source.ElementIndex >= batch.Length ||
                            epoch.RecordAddress("attachment-batch", item.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)) != item.RecordId) throw Refusal();
                        piece = batch[source.ElementIndex];
                    }
                    var expected = Math.Min(encryptedBytes, length - (long)source.PieceIndex * encryptedBytes);
                    if (piece is null || piece.Sha256 != hash || piece.Length != length || piece.Index != source.PieceIndex || piece.Bytes is null ||
                        piece.Bytes.Length != expected || Convert.ToHexStringLower(SHA256.HashData(piece.Bytes)) != source.PieceHash) throw Refusal();
                    piece.Bytes.CopyTo(output, written); written += piece.Bytes.Length;
                }
                finally
                {
                    if (payload.Attachment?.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes);
                    if (payload.Stream?.Bytes is { } bytes2) CryptographicOperations.ZeroMemory(bytes2);
                    if (payload.Blobs is { } blobs) foreach (var blob in blobs) if (blob?.Bytes is { } bytes3) CryptographicOperations.ZeroMemory(bytes3);
                }
            }
            if (written != output.Length) throw Refusal(); context.Check(); return output;
        }
        catch { CryptographicOperations.ZeroMemory(output); throw; }
        finally { foreach (var epoch in epochs.Values) epoch.Dispose(); }
    }
}
