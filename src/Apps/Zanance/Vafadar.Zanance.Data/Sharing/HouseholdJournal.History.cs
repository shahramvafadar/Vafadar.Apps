using System.Buffers.Binary;
using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>No connection, private key or historical plaintext survives a bounded archive operation.</summary>
    private sealed class FinancialArchive(HouseholdJournal journal, HouseholdJournalAuthority authority, long verifiedThrough) : IHouseholdFinancialArchive
    {
        /// <inheritdoc />
        public Task<SyncFeedPage> ReadFeedAsync(long after, long through, CancellationToken ct) =>
            journal.ReadArchivedFinancialFeedAsync(authority, verifiedThrough, after, through, ct);
        /// <inheritdoc />
        public Task<IReadOnlyDictionary<SyncRecordKey, SyncRecord>> ReadBaselinesAsync(IEnumerable<SyncProposal> proposals, CancellationToken ct) =>
            journal.ReadArchivedBaselinesAsync(authority, verifiedThrough, proposals, ct);
        /// <inheritdoc />
        public async Task<IHouseholdOpaqueVersionSource> OpenVersionsAsync(CancellationToken ct)
        {
            var context = await journal.OpenAsync(ct);
            try
            {
                var binding = await RequireBindingAsync(context, authority, ct);
                if (binding.Cursor != verifiedThrough) throw Refusal();
                return new OpaqueVersionSource(context, verifiedThrough, ct);
            }
            catch { await context.DisposeAsync(); throw; }
        }
    }

    /// <summary>Scoped lookup reuses the original indexed event headers; disposal releases native authority before any caller can perform HTTP.</summary>
    private sealed class OpaqueVersionSource(JournalContext context, long through, CancellationToken ct) : IHouseholdOpaqueVersionSource
    {
        /// <inheritdoc />
        public long Read(string address)
        {
            ct.ThrowIfCancellationRequested(); context.Check();
            return context.Db.Set<HouseholdLocalEvent>().Where(row => row.RecordId == address && row.Cursor <= through)
                .Select(row => (long?)row.Cursor).Max() ?? 0;
        }
        /// <inheritdoc />
        public ValueTask DisposeAsync() => context.DisposeAsync();
    }

    /// <summary>A reusable native statement writes only derived positions under the same rollback transaction as complete graph verification.</summary>
    private sealed class FinancialSourceWriter : IDisposable
    {
        private readonly DbCommand command;
        private readonly CancellationToken cancellation;
        private readonly string root;
        private int records;
        internal FinancialSourceWriter(ZananceDbContext db, CancellationToken ct)
        {
            cancellation = ct; command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
            root = Path.GetPathRoot(db.Database.GetDbConnection().DataSource) ?? throw Refusal();
            command.CommandText = """
                INSERT INTO "HouseholdLocalFinancialReferences" ("Version","Table","Key","Cursor","EventIndex","Offset","Length")
                VALUES ($version,$table,$key,$cursor,$event,$offset,$length)
                ON CONFLICT("Version") DO UPDATE SET "Version"=excluded."Version"
                WHERE "Table"=excluded."Table" AND "Key"=excluded."Key" AND "Cursor"=excluded."Cursor"
                  AND "EventIndex"=excluded."EventIndex" AND "Offset"=excluded."Offset" AND "Length"=excluded."Length"
                """;
            foreach (var name in new[] { "$version", "$table", "$key", "$cursor", "$event", "$offset", "$length" })
            { var parameter = command.CreateParameter(); parameter.ParameterName = name; command.Parameters.Add(parameter); }
            command.Prepare();
        }
        /// <summary>Existing disagreement is corruption; neither accepted source pointers nor financial rows are silently rewritten.</summary>
        internal void Record(SyncRecord row, long cursor, int eventIndex, long offset, int length)
        {
            cancellation.ThrowIfCancellationRequested();
            if (records++ % 128 == 0 && new DriveInfo(root).AvailableFreeSpace < 18L * 1024 * 1024)
                throw new IOException("Insufficient space for the protected household history index.");
            command.Parameters[0].Value = row.Version; command.Parameters[1].Value = row.Record.Table; command.Parameters[2].Value = row.Record.Key;
            command.Parameters[3].Value = cursor; command.Parameters[4].Value = eventIndex; command.Parameters[5].Value = offset; command.Parameters[6].Value = length;
            if (command.ExecuteNonQuery() != 1) throw Refusal();
        }
        /// <inheritdoc />
        public void Dispose() => command.Dispose();
    }

    /// <summary>Contiguous global financial versions are read in bounded pages; ciphertext part counts never substitute for row sequence.</summary>
    private async Task<SyncFeedPage> ReadArchivedFinancialFeedAsync(HouseholdJournalAuthority authority, long verifiedThrough, long after, long through, CancellationToken ct)
    {
        if (after < 0 || through < after) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (verifiedThrough < 1 || binding.Cursor < verifiedThrough || through > binding.FinancialCursor) throw Refusal();
        var sources = await context.Db.Set<HouseholdLocalFinancialReference>().AsNoTracking()
            .Where(row => row.Version > after && row.Version <= through && row.Cursor <= verifiedThrough).OrderBy(row => row.Version).Take(128).ToArrayAsync(ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        using var reader = new FinancialSourceReader(this, context, binding, membership, ct);
        var rows = new List<SyncRecord>(); var next = after; long bytes = 0;
        foreach (var source in sources)
        {
            if (source.Version != next + 1) throw Refusal();
            var row = await reader.ReadAsync(source); var size = source.Length;
            if (size == 0) size = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(row, PersonalSyncJsonContext.Default.SyncRecord).Length;
            if (size > HouseholdFinancialFraming.MaximumRecordBytes) throw Refusal();
            if (rows.Count != 0 && bytes + size > 2 * 1024 * 1024) break;
            rows.Add(row); bytes += size; next = row.Version;
        }
        if (next != through && rows.Count == 0) throw Refusal();
        context.Check(); return new(rows, next, through, next == through);
    }

    /// <summary>Only an exact previously verified row/version can become the original three-way planner baseline.</summary>
    private async Task<IReadOnlyDictionary<SyncRecordKey, SyncRecord>> ReadArchivedBaselinesAsync(HouseholdJournalAuthority authority, long verifiedThrough,
        IEnumerable<SyncProposal> proposals, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.Cursor < verifiedThrough || verifiedThrough < 0) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        using var reader = new FinancialSourceReader(this, context, binding, membership, ct);
        var result = new Dictionary<SyncRecordKey, SyncRecord>();
        foreach (var proposal in proposals.Where(row => row.BaseVersion > 0).OrderBy(row => row.BaseVersion))
        {
            ct.ThrowIfCancellationRequested();
            var source = await context.Db.Set<HouseholdLocalFinancialReference>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Version == proposal.BaseVersion && row.Table == proposal.Record.Table && row.Key == proposal.Record.Key && row.Cursor <= verifiedThrough, ct);
            if (source is not null) result.Add(proposal.Record, await reader.ReadAsync(source));
        }
        context.Check(); return result;
    }

    /// <summary>Reauthenticates one original packet at a time, then reads only the exact standalone row or framing prefix and JSON body.</summary>
    private sealed class FinancialSourceReader(HouseholdJournal journal, JournalContext context, HouseholdLocalBinding binding,
        MembershipState membership, CancellationToken ct) : IDisposable
    {
        private long cursor;
        private HouseholdPublicationReceipt? receipt;
        private HouseholdCommitStatement? commit;
        private HouseholdVerifiedMembership? origin;
        private HouseholdEpochMaterial? epoch;
        private HouseholdRecordBlock? cachedBlock;
        private int cachedEvent = -1;
        internal async Task<SyncRecord> ReadAsync(HouseholdLocalFinancialReference source)
        {
            if (source.Cursor < 1 || source.EventIndex < 0 || source.Version < 1) throw Refusal();
            if (cursor != source.Cursor)
            {
                epoch?.Dispose(); epoch = null; cursor = 0;
                ClearBlock();
                var packet = await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == source.Cursor, ct) ?? throw Refusal();
                receipt = ParseReceipt(packet);
                var first = ParseEvent(await context.Db.Set<HouseholdLocalEvent>().AsNoTracking()
                    .SingleOrDefaultAsync(row => row.Cursor == source.Cursor && row.Index == 0, ct) ?? throw Refusal());
                if (!membership.ByVersion.TryGetValue(first.MembershipVersion, out origin)) throw Refusal();
                var previous = source.Cursor == 1 ? new string('0', 64) :
                    (await context.Db.Set<HouseholdLocalPacket>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == source.Cursor - 1, ct) ?? throw Refusal()).Hash;
                commit = HouseholdTrustChain.Publication(origin, receipt, first.DeviceId, first.OperationId, first.AttemptId, source.Cursor - 1, previous);
                if (membership.ByVersion.TryGetValue(origin.Version + 1, out var successor))
                    HouseholdTrustChain.RequirePublicationHorizon(origin, successor, receipt, commit.Cursor);
                epoch = await ReadEpochCoreAsync(context.Db, membership, origin.Epoch, ct); cursor = source.Cursor;
            }
            SyncRecord record;
            if (source.Offset == -1 && source.Length == 0)
            {
                var payload = await PayloadAsync(source.EventIndex);
                try
                {
                    if (payload.Kind != HouseholdFinancialContentKind.Record || payload.Record is not { } row ||
                        payload.Attachment is not null || payload.Stream is not null || payload.Blobs is not null ||
                        epoch!.RecordAddress(row.Record.Table, row.Record.Key) != receipt!.Parts[source.EventIndex].RecordId) throw Refusal();
                    record = row;
                }
                finally { Clear(payload); }
            }
            else
            {
                if (source.Offset < 4 || source.Length is < 1 or > HouseholdFinancialFraming.MaximumRecordBytes ||
                    source.Offset > HouseholdFinancialFraming.MaximumStreamBytes - source.Length) throw Refusal();
                var bytes = new byte[source.Length + 4]; var start = source.Offset - 4; var written = 0;
                try
                {
                    while (written < bytes.Length)
                    {
                        ct.ThrowIfCancellationRequested();
                        var position = start + written; var blockIndex = checked((int)(position / HouseholdFinancialFraming.BlockBytes));
                        var block = await StreamBlockAsync(checked(source.EventIndex + blockIndex), blockIndex);
                        var offset = (int)(position % HouseholdFinancialFraming.BlockBytes);
                        var count = Math.Min(bytes.Length - written, block.Bytes.Length - offset);
                        if (count <= 0) throw Refusal(); block.Bytes.AsSpan(offset, count).CopyTo(bytes.AsSpan(written)); written += count;
                    }
                    // A real binary length prefix cannot occur inside strict JSON text. Require it as well
                    // as the exact signed row identity/version; an arbitrary JSON substring is not a row.
                    if (BinaryPrimitives.ReadInt32BigEndian(bytes) != source.Length) throw Refusal();
                    record = StrictJsonPayload.Deserialize(bytes.AsMemory(4), PersonalSyncJsonContext.Default.SyncRecord, HouseholdFinancialFraming.MaximumRecordBytes);
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            if (record.Record is null || record.Record.Table != source.Table || record.Record.Key != source.Key || record.Version != source.Version) throw Refusal();
            return record;
        }
        /// <summary>Sequential rows share only the last bounded authenticated block; advancing a block clears its predecessor immediately.</summary>
        private async Task<HouseholdRecordBlock> StreamBlockAsync(int eventIndex, int blockIndex)
        {
            if (cachedBlock is not null && cachedEvent == eventIndex && cachedBlock.Index == blockIndex) return cachedBlock;
            ClearBlock();
            var payload = await PayloadAsync(eventIndex); var retained = false;
            try
            {
                if (payload.Kind != HouseholdFinancialContentKind.RecordStream || payload.Record is not null || payload.Attachment is not null || payload.Blobs is not null ||
                    payload.Stream is not { Bytes: not null } block || block.Index != blockIndex || block.Bytes.Length > HouseholdFinancialFraming.BlockBytes ||
                    !block.Final && (block.Bytes.Length != HouseholdFinancialFraming.BlockBytes || block.Length != 0 || block.Records != 0) ||
                    block.Final && (block.Length != (long)blockIndex * HouseholdFinancialFraming.BlockBytes + block.Bytes.Length || block.Records < 1) ||
                    epoch!.RecordAddress("financial-stream", blockIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)) != receipt!.Parts[eventIndex].RecordId) throw Refusal();
                cachedBlock = block; cachedEvent = eventIndex; retained = true; return block;
            }
            finally { if (!retained) Clear(payload); }
        }
        /// <summary>Clears the single owned block when changing packets or disposing the bounded reader.</summary>
        private void ClearBlock()
        { if (cachedBlock?.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes); cachedBlock = null; cachedEvent = -1; }
        /// <summary>Original signatures, roster cutoffs, event manifest and standard envelope authentication remain mandatory for every requested block.</summary>
        private async Task<HouseholdFinancialPayload> PayloadAsync(int index)
        {
            ct.ThrowIfCancellationRequested(); context.Check();
            if (receipt is null || commit is null || epoch is null || origin is null || index < 0 || index >= receipt.Parts.Length) throw Refusal();
            var stored = await context.Db.Set<HouseholdLocalEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.Cursor == cursor && row.Index == index, ct) ?? throw Refusal();
            var item = ParseEvent(stored); RequireInboxEvent(item, receipt, commit);
            var clear = epoch.Decrypt(journal.cryptography, new(binding.SpaceId, epoch.Epoch, item.OperationId, HouseholdProtectionPurpose.Financial, "members"),
                item.Ciphertext, origin.Device(item.DeviceId, requireWrite: true));
            try
            {
                var payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes);
                if (payload.Format == 1 && payload.SchemaHash == HouseholdFinancialSchema.Hash && payload.AttemptId == item.AttemptId && payload.Index == item.Index) return payload;
                Clear(payload); throw Refusal();
            }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
        /// <summary>All owned decoded content buffers are cleared, including unexpected payload kinds on a corrupt derived position.</summary>
        private static void Clear(HouseholdFinancialPayload payload)
        {
            if (payload.Stream?.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes);
            if (payload.Attachment?.Bytes is { } piece) CryptographicOperations.ZeroMemory(piece);
            if (payload.Blobs is { } blobs) foreach (var blob in blobs) if (blob?.Bytes is { } value) CryptographicOperations.ZeroMemory(value);
        }
        /// <inheritdoc />
        public void Dispose() { ClearBlock(); epoch?.Dispose(); }
    }
}
