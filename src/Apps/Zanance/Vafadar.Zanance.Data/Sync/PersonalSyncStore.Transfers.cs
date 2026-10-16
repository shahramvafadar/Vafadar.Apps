using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>A content address and exact length, without loading the whole attachment into transport memory.</summary>
internal sealed record SyncBlobNeed(string Hash, long Length);

public sealed partial class PersonalSyncStore
{
    /// <summary>Reads one already captured protected piece after checking the original file and operation.</summary>
    internal async Task<byte[]> OutgoingPieceAsync(SyncRoundWork work, string hash, int index, CancellationToken ct)
    {
        byte[]? result = null;
        await InRoundAsync(work, async (db, _, _) =>
        {
            var piece = await db.Set<SyncBlobPart>().AsNoTracking().SingleOrDefaultAsync(row => row.Hash == hash && row.Part == index, ct)
                ?? throw new InvalidDataException("The retained attachment piece is unavailable.");
            if (piece.Data.Length is < 1 or > BlobPieceBytes) throw new InvalidDataException("The retained attachment piece exceeds its bound.");
            result = piece.Data;
        }, ct);
        return result ?? throw new InvalidDataException("The retained attachment piece is unavailable.");
    }

    /// <summary>Persists only a verified complete manifest for this exact content address and connection.</summary>
    internal Task ConfirmBlobAsync(SyncRoundWork work, SyncBlob manifest, CancellationToken ct) =>
        InRoundAsync(work, async (db, _, _) =>
        {
            var transfer = await db.Set<SyncBlobTransfer>().FindAsync([manifest.Sha256], ct)
                ?? throw new InvalidDataException("The retained attachment declaration is unavailable.");
            if (transfer.Length != manifest.Length || manifest.Parts.Count != (manifest.Length + BlobPieceBytes - 1) / BlobPieceBytes ||
                manifest.Parts.Any(id => id == Guid.Empty) || manifest.Parts.Distinct().Count() != manifest.Parts.Count)
                throw new InvalidDataException("The attachment manifest is invalid.");
            transfer.Manifest = JsonSerializer.Serialize(manifest, PersonalSyncJsonContext.Default.SyncBlob);
        }, ct);

    /// <summary>Private complete-history staging advances no financial cursor and changes no original row.</summary>
    internal Task StageFeedAsync(SyncRoundWork work, SyncFeedPage page, CancellationToken ct) =>
        InRoundAsync(work, async (db, round, _) =>
        {
            if (round.Phase != 2 || round.ThroughCursor is not { } through || page.ThroughCursor != through ||
                page.Records.Count > 128 || page.NextCursor > through || page.NextCursor < round.ReadCursor ||
                page.Complete != (page.NextCursor == through) || page.NextCursor != round.ReadCursor + page.Records.Count ||
                !page.Complete && page.Records.Count == 0)
                throw new InvalidDataException("The immutable personal feed boundary is invalid.");
            var expected = round.ReadCursor; long bytes = 0;
            foreach (var record in page.Records)
            {
                if (record.Version != ++expected) throw new InvalidDataException("The immutable personal feed sequence is incomplete.");
                var table = SyncSchema.ForScope(work.Authority.TransportKind == ProfileSynchronizationKind.Household).Tables.SingleOrDefault(table => table.Name == record.Record.Table);
                if (table is null || !SyncGraphValidation.ValidIdentity(table, record.Record) ||
                    record.Values is { } row && SyncGraphValidation.ValidateRow(table, record.Record, row).Count != 0)
                    throw new InvalidDataException("The received personal record violates the model contract.");
                var body = record.Values?.GetRawText(); bytes += body is null ? 0 : System.Text.Encoding.UTF8.GetByteCount(body);
                if (bytes > (work.Authority.TransportKind == ProfileSynchronizationKind.Household ? Core.Sharing.HouseholdFinancialFraming.MaximumRecordBytes : 2 * 1024 * 1024))
                    throw new InvalidDataException("The original financial feed response exceeds its bound.");
                var target = await db.Set<SyncIncomingRow>().FindAsync([record.Record.Table, record.Record.Key], ct);
                if (target is null)
                { target = new() { TableName = record.Record.Table, RecordKey = record.Record.Key }; db.Add(target); }
                target.Version = record.Version; target.Body = body;
            }
            round.ReadCursor = page.NextCursor;
        }, ct);

    /// <summary>Lists received content references only after all financial feed pages are privately staged.</summary>
    internal async Task<SyncBlobNeed[]> IncomingBlobsAsync(SyncRoundWork work, CancellationToken ct)
    {
        SyncBlobNeed[] result = [];
        await InRoundAsync(work, async (db, round, _) =>
        {
            if (round.Phase != 2 || round.ReadCursor != round.ThroughCursor) throw new InvalidOperationException("The personal feed is incomplete.");
            var rows = await db.Set<SyncIncomingRow>().AsNoTracking().ToArrayAsync(ct);
            result = BlobNeeds(rows.Select(row => new SyncRecord(new(row.TableName, row.RecordKey), row.Version, SyncRowCodec.Parse(row.Body))));
        }, ct);
        return result;
    }

    /// <summary>Stores one bounded private incoming piece idempotently; redirected/altered replay has no financial effect.</summary>
    internal Task StageBlobAsync(SyncRoundWork work, SyncBlobNeed blob, int index, byte[] bytes, CancellationToken ct) =>
        InRoundAsync(work, async (db, round, _) =>
        {
            if (round.Phase is not (1 or 2) || !SyncProtocolDigest.IsDigest(blob.Hash) || blob.Length is < 0 or > SyncGraphValidation.MaximumProfileBytes ||
                index < 0 || (long)index * BlobPieceBytes >= blob.Length || bytes.Length != Math.Min(BlobPieceBytes, blob.Length - (long)index * BlobPieceBytes))
                throw new InvalidDataException("The incoming attachment piece is invalid.");
            if (round.Phase == 1 && !(await ReviewBlobNeedsAsync(db, ct)).Contains(blob))
                throw new InvalidOperationException("The attachment does not belong to the current authenticated review.");
            var transfer = await db.Set<SyncBlobTransfer>().FindAsync([blob.Hash], ct);
            if (transfer is null) db.Add(new SyncBlobTransfer { Hash = blob.Hash, Length = blob.Length });
            else if (transfer.Length != blob.Length) throw new InvalidDataException("The incoming attachment length changed.");
            var previous = await db.Set<SyncBlobPart>().FindAsync([blob.Hash, index], ct);
            if (previous is null) db.Add(new SyncBlobPart { Hash = blob.Hash, Part = index, Data = bytes.ToArray() });
            else if (!bytes.AsSpan().SequenceEqual(previous.Data)) throw new InvalidDataException("The immutable incoming attachment piece changed.");
        }, ct);

    /// <summary>Lists only authenticated current/related content in the retained conflict snapshots, never arbitrary hashes.</summary>
    internal async Task<SyncBlobNeed[]> ReviewBlobsAsync(SyncRoundWork work, CancellationToken ct)
    {
        SyncBlobNeed[] result = [];
        await InRoundAsync(work, async (db, _, _) => result = await ReviewBlobNeedsAsync(db, ct), ct);
        return result;
    }

    private static async Task<SyncBlobNeed[]> ReviewBlobNeedsAsync(ZananceDbContext db, CancellationToken ct)
    {
        var payloads = await db.Set<SyncConflictRow>().AsNoTracking().Select(row => row.Payload).ToArrayAsync(ct);
        var records = payloads.Select(payload => JsonSerializer.Deserialize(payload, PersonalSyncJsonContext.Default.SyncConflict)
            ?? throw new InvalidDataException("The retained attachment review is invalid."))
            .SelectMany(conflict => new[] { conflict.Current, conflict.RelatedRecord }.OfType<SyncRecord>().Concat(conflict.Context ?? []));
        return BlobNeeds(records);
    }

    /// <summary>Checks retained private pieces before requesting only those missing after interruption.</summary>
    internal async Task<int[]> MissingBlobPiecesAsync(SyncRoundWork work, SyncBlobNeed blob, CancellationToken ct)
    {
        int[] result = [];
        await InRoundAsync(work, async (db, _, _) =>
        {
            if (!SyncProtocolDigest.IsDigest(blob.Hash) || blob.Length is < 0 or > SyncGraphValidation.MaximumProfileBytes)
                throw new InvalidDataException("The incoming attachment declaration is invalid.");
            var retained = await db.Set<SyncBlobPart>().AsNoTracking().Where(row => row.Hash == blob.Hash).Select(row => row.Part).ToArrayAsync(ct);
            result = Enumerable.Range(0, (int)((blob.Length + BlobPieceBytes - 1) / BlobPieceBytes)).Except(retained).ToArray();
        }, ct);
        return result;
    }

    /// <summary>Finds typed blob references without accepting arbitrary object properties as transfer instructions.</summary>
    internal static SyncBlobNeed[] BlobNeeds(IEnumerable<SyncRecord> records)
    {
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var record in records.Where(record => record.Values is not null))
            foreach (var column in SyncSchema.Describe(record.Record.Table).Columns.Where(column => column.Kind == "blob"))
            {
                var cell = record.Values!.Value.GetProperty(column.Name); if (cell.ValueKind == JsonValueKind.Null) continue;
                var hash = cell.GetProperty("sha256").GetString()!; var length = cell.GetProperty("length").GetInt64();
                if (!SyncProtocolDigest.IsDigest(hash) || length is < 0 or > SyncGraphValidation.MaximumProfileBytes ||
                    result.TryGetValue(hash, out var previous) && previous != length)
                    throw new InvalidDataException("The personal attachment reference is invalid.");
                result[hash] = length;
            }
        if (result.Values.Sum() > SyncGraphValidation.MaximumProfileBytes) throw new SyncProfileBoundException();
        return result.Select(pair => new SyncBlobNeed(pair.Key, pair.Value)).ToArray();
    }

    private static async Task<byte[]> VerifiedBlobAsync(ZananceDbContext db, string hash, long length, CancellationToken ct)
    {
        if (!SyncProtocolDigest.IsDigest(hash) || length is < 0 or > SyncGraphValidation.MaximumProfileBytes) throw new InvalidDataException("The attachment reference is invalid.");
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); using var output = new MemoryStream((int)length);
        var pieces = db.Set<SyncBlobPart>().AsNoTracking().Where(row => row.Hash == hash).OrderBy(row => row.Part).AsAsyncEnumerable(); var index = 0;
        try
        {
            await foreach (var piece in pieces.WithCancellation(ct))
            {
                try
                {
                    if (piece.Part != index++ || piece.Data.Length != Math.Min(BlobPieceBytes, length - output.Length) || piece.Data.Length == 0)
                        throw new InvalidDataException("The retained attachment is incomplete.");
                    output.Write(piece.Data); digest.AppendData(piece.Data);
                }
                finally { CryptographicOperations.ZeroMemory(piece.Data); }
            }
            if (output.Length != length || Convert.ToHexStringLower(digest.GetHashAndReset()) != hash)
                throw new InvalidDataException("The retained attachment digest does not match.");
            return output.ToArray();
        }
        finally { if (output.TryGetBuffer(out var bytes)) CryptographicOperations.ZeroMemory(bytes.AsSpan()); }
    }
}
