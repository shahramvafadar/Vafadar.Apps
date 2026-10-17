using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Retains individually authenticated ciphertext pieces without publishing any incomplete financial state or advancing a cursor.</summary>
    public async Task StageInboxAsync(HouseholdJournalAuthority authority, HouseholdPublicationReceipt receipt,
        IReadOnlyList<HouseholdOpaqueEvent> events, CancellationToken ct = default)
    {
        if (events is null || events.Count is < 1 or > HouseholdPublicationDigest.MaximumParts || events.Any(item => item is null)) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var first = events[0];
        if (binding.Cursor == long.MaxValue || first.Cursor != binding.Cursor + 1 ||
            !membership.ByVersion.TryGetValue(first.MembershipVersion, out var origin)) throw Refusal();
        var commit = HouseholdTrustChain.Publication(origin, receipt, first.DeviceId, first.OperationId, first.AttemptId,
            binding.Cursor, binding.PublicationHead);
        if (membership.ByVersion.TryGetValue(origin.Version + 1, out var successor))
            HouseholdTrustChain.RequirePublicationHorizon(origin, successor, receipt, commit.Cursor);
        var receiptJson = SerializeReceipt(receipt); var hash = HouseholdTrustChain.Digest(receipt.SignedCommit);
        var packet = await context.Db.Set<HouseholdLocalPacket>().SingleOrDefaultAsync(row => row.Cursor == commit.Cursor, ct);
        if (packet is not null)
        { if (packet.ReceiptJson != receiptJson || packet.Hash != hash) throw Refusal(); }
        else context.Db.Add(new HouseholdLocalPacket { Cursor = commit.Cursor, ReceiptJson = receiptJson, Hash = hash });
        var existing = (await context.Db.Set<HouseholdLocalEvent>().Where(row => row.Cursor == commit.Cursor).Select(row => row.Index).ToArrayAsync(ct)).ToHashSet();
        long total = await context.Db.Set<HouseholdLocalEvent>().Where(row => row.Cursor == commit.Cursor).SumAsync(row => (long)row.SerializedBytes, ct);
        var seen = new HashSet<int>();
        foreach (var item in events)
        {
            if (!seen.Add(item.Index)) throw Refusal();
            RequireInboxEvent(item, receipt, commit);
            var json = SerializeEvent(item);
            if (existing.Contains(item.Index))
            {
                var prior = await context.Db.Set<HouseholdLocalEvent>().AsNoTracking().SingleAsync(row => row.Cursor == commit.Cursor && row.Index == item.Index, ct);
                if (prior.EventJson != json || prior.RecordId != item.RecordId || prior.BaseVersion != item.BaseVersion || prior.Digest != item.Digest ||
                    prior.SerializedBytes != Encoding.UTF8.GetByteCount(json)) throw Refusal();
            }
            else
            {
                var size = Encoding.UTF8.GetByteCount(json); total = checked(total + size);
                // Public routing JSON overhead is bounded separately from the complete ciphertext
                // cap. This is storage protection, not permission to apply a partial financial graph.
                if (total > HouseholdPublicationDigest.MaximumBytes + HouseholdPublicationDigest.MaximumParts * 4096L) throw Refusal();
                context.Db.Add(new HouseholdLocalEvent { Cursor = commit.Cursor, Index = item.Index, EventJson = json, RecordId = item.RecordId,
                    BaseVersion = item.BaseVersion, Digest = item.Digest, SerializedBytes = size });
            }
        }
        await context.CommitAsync(ct);
    }

    /// <summary>Returns only the contiguous authenticated address prefix of the pending packet, never a count or highest received position with holes.</summary>
    public async Task<HouseholdFeedPosition> InboxPositionAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.Cursor == long.MaxValue) throw Refusal();
        var packet = await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == binding.Cursor + 1, ct);
        if (packet is null) return new(binding.Cursor, new('f', 64));
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var receipt = ParseReceipt(packet);
        var stored = await context.Db.Set<HouseholdLocalEvent>().AsNoTracking().Where(row => row.Cursor == packet.Cursor)
            .Select(row => new { row.Index, row.RecordId, row.BaseVersion, row.Digest }).ToArrayAsync(ct);
        if (stored.Length == 0) throw Refusal();
        var byIndex = stored.ToDictionary(row => row.Index);
        var original = await context.Db.Set<HouseholdLocalEvent>().AsNoTracking().Where(row => row.Cursor == packet.Cursor).OrderBy(row => row.Index).FirstAsync(ct);
        var first = ParseEvent(original);
        if (!membership.ByVersion.TryGetValue(first.MembershipVersion, out var origin)) throw Refusal();
        var commit = HouseholdTrustChain.Publication(origin, receipt, first.DeviceId, first.OperationId, first.AttemptId, binding.Cursor, binding.PublicationHead);
        if (membership.ByVersion.TryGetValue(origin.Version + 1, out var successor))
            HouseholdTrustChain.RequirePublicationHorizon(origin, successor, receipt, commit.Cursor);
        RequireInboxEvent(first, receipt, commit);
        foreach (var item in stored)
        {
            if (item.Index < 0 || item.Index >= receipt.Parts.Length) throw Refusal();
            var expected = receipt.Parts[item.Index];
            if (item.RecordId != expected.RecordId || item.BaseVersion != expected.BaseVersion || item.Digest != expected.Digest) throw Refusal();
        }
        var address = "";
        foreach (var part in receipt.Parts.OrderBy(part => part.RecordId, StringComparer.Ordinal))
        { if (!byIndex.ContainsKey(part.Index)) break; address = part.RecordId; }
        context.Check();
        return address.Length == 0 ? new(binding.Cursor, new('f', 64)) : new(packet.Cursor, address);
    }

    /// <summary>Rebuilds only the complete verified retained graph; incomplete pending ciphertext remains durable and unapplied.</summary>
    public async Task<HouseholdFinancialReplica> ReadReplicaAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var replica = await ReplayAsync(context.Db, binding, membership, binding.Cursor, ct);
        try
        {
            if (replica.Cursor != binding.Cursor || replica.FinancialCursor != binding.FinancialCursor || replica.PublicationHead != binding.PublicationHead) throw Refusal();
            await context.CommitAsync(ct); return replica;
        }
        catch { replica.Dispose(); throw; }
    }

    /// <summary>Validates every ciphertext piece, epoch, original financial row and attachment before atomically accepting the next complete inbox cursor.</summary>
    public async Task<bool> AcceptInboxAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.Cursor == long.MaxValue) throw Refusal();
        var packet = await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == binding.Cursor + 1, ct);
        if (packet is null) return false;
        var receipt = ParseReceipt(packet);
        var count = await context.Db.Set<HouseholdLocalEvent>().CountAsync(row => row.Cursor == packet.Cursor, ct);
        if (count < receipt.Parts.Length) return false;
        if (count != receipt.Parts.Length) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        using var replica = await ReplayAsync(context.Db, binding, membership, packet.Cursor, ct);
        binding.Cursor = replica.Cursor; binding.FinancialCursor = replica.FinancialCursor; binding.PublicationHead = replica.PublicationHead;
        await context.CommitAsync(ct); return true;
    }

    /// <summary>Uses original complete verification and all known successor cutoffs; persisted counters cannot manufacture financial authority.</summary>
    private async Task<HouseholdFinancialReplica> ReplayAsync(ZananceDbContext db, HouseholdLocalBinding binding,
        MembershipState membership, long through, CancellationToken ct)
    {
        var authority = new HouseholdJournalAuthority(binding.GenerationPath, binding.SetupOperation, binding.PeerKey, binding.IdentityId);
        var replica = new HouseholdFinancialReplica(binding.SpaceId, new AttachmentArchive(this, authority, through), new FinancialArchive(this, authority, through));
        try
        {
            // One packet at a time bounds database materialization. The original financial replica
            // owns its validated graph and attachment buffers and clears them when disposed.
            using var sources = new FinancialSourceWriter(db, ct);
            for (long cursor = 1; cursor <= through; cursor = checked(cursor + 1))
            {
                ct.ThrowIfCancellationRequested();
                var packet = await db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == cursor, ct) ?? throw Refusal();
                var receipt = ParseReceipt(packet);
                var actualCount = await db.Set<HouseholdLocalEvent>().CountAsync(row => row.Cursor == cursor, ct);
                if (actualCount != receipt.Parts.Length || actualCount == 0) throw Refusal();
                var first = await db.Set<HouseholdLocalEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == cursor && row.Index == 0, ct) ?? throw Refusal();
                var firstEvent = ParseEvent(first);
                if (!membership.ByVersion.TryGetValue(firstEvent.MembershipVersion, out var origin)) throw Refusal();
                using var epoch = await ReadEpochCoreAsync(db, membership, origin.Epoch, ct);
                var events = db.Set<HouseholdLocalEvent>().AsNoTracking().Where(row => row.Cursor == cursor).OrderBy(row => row.Index).AsEnumerable().Select(row =>
                { ct.ThrowIfCancellationRequested(); return ParseEvent(row); });
                replica.ApplyOrdered(origin, epoch, receipt, events, actualCount, cryptography, membership.ByVersion.GetValueOrDefault(origin.Version + 1), ct,
                    address => db.Set<HouseholdLocalEvent>().Where(row => row.RecordId == address && row.Cursor < cursor)
                        .Select(row => (long?)row.Cursor).Max() ?? 0,
                    (record, eventIndex, offset, length) => sources.Record(record, cursor, eventIndex, offset, length), db.WorkClock);
                foreach (var document in replica.VerifiedReferences)
                    await Rates.VerifiedMarketSourcePersistence.AcceptAsync(db, document, ct);
                await RecordAttachmentSourcesAsync(db, replica.DrainAttachmentSources(), ct);
                if (cursor == through) break;
            }
            return replica;
        }
        catch { replica.Dispose(); throw; }
    }

    /// <summary>Exact authenticated event/manifest comparison also applies to partial downloads and literal duplicate retries.</summary>
    private static void RequireInboxEvent(HouseholdOpaqueEvent item, HouseholdPublicationReceipt receipt, HouseholdCommitStatement commit)
    {
        if (item.Index < 0 || item.Index >= receipt.Parts.Length) throw Refusal();
        var expected = receipt.Parts[item.Index];
        if (item.Cursor != commit.Cursor || item.RoundId != receipt.RoundId || item.KeyEpoch != commit.KeyEpoch || item.MembershipVersion != commit.MembershipVersion ||
            item.DeviceId != commit.DeviceId || item.OperationId != commit.OperationId || item.AttemptId != commit.AttemptId || expected.Index != item.Index ||
            item.RecordId != expected.RecordId || item.BaseVersion != expected.BaseVersion || item.Digest != expected.Digest ||
            HouseholdPublicationDigest.Ciphertext(item.Ciphertext) != item.Digest) throw Refusal();
    }
    /// <summary>Strict original duplicate-free bounded receipt parsing with retained exact signed-byte identity.</summary>
    private static HouseholdPublicationReceipt ParseReceipt(HouseholdLocalPacket packet)
    {
        var receipt = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(packet.ReceiptJson), HouseholdMetadataJsonContext.Default.HouseholdPublicationReceipt, HouseholdPublicationDigest.MaximumReceiptBytes);
        if (HouseholdTrustChain.Digest(receipt.SignedCommit) != packet.Hash) throw Refusal();
        return receipt;
    }
    /// <summary>Strict original duplicate-free bounded ciphertext event parsing; no financial plaintext is exposed.</summary>
    private static HouseholdOpaqueEvent ParseEvent(HouseholdLocalEvent row)
    {
        var item = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.EventJson), HouseholdMetadataJsonContext.Default.HouseholdOpaqueEvent, 2 * 1024 * 1024);
        if (item.Cursor != row.Cursor || item.Index != row.Index || item.RecordId != row.RecordId || item.BaseVersion != row.BaseVersion || item.Digest != row.Digest ||
            Encoding.UTF8.GetByteCount(row.EventJson) != row.SerializedBytes) throw Refusal();
        return item;
    }
    /// <summary>Only complete bounded public receipts enter protected storage.</summary>
    private static string SerializeReceipt(HouseholdPublicationReceipt receipt)
    {
        var json = JsonSerializer.Serialize(receipt, HouseholdMetadataJsonContext.Default.HouseholdPublicationReceipt);
        if (Encoding.UTF8.GetByteCount(json) > HouseholdPublicationDigest.MaximumReceiptBytes) throw Refusal(); return json;
    }
    /// <summary>Only bounded ciphertext events enter protected storage.</summary>
    private static string SerializeEvent(HouseholdOpaqueEvent item)
    {
        var json = JsonSerializer.Serialize(item, HouseholdMetadataJsonContext.Default.HouseholdOpaqueEvent);
        if (Encoding.UTF8.GetByteCount(json) > 2 * 1024 * 1024) throw Refusal(); return json;
    }
}
