using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>One original typed ledger row and its physical ordering key, never a financial aggregate.</summary>
internal readonly record struct HomeLedgerPhysicalRow(long RowId, LedgerEntry Entry);

/// <summary>
/// Bounded derived transport for all32 original ledger fields. It is not authoritative storage:
/// callers must bind blocks to the captured database/schema/revision and retain the scalar fallback.
/// </summary>
internal static class HomeLedgerBlockCodec
{
    /// <summary>Physical row identities share one fixed bounded bucket, including negative legacy identities.</summary>
    internal const int BlockShift = 9;
    /// <summary>The greatest number of physical identities in a bucket.</summary>
    internal const int MaxRows = 1 << BlockShift;
    /// <summary>A derived block may decline oversized inputs without limiting the original ledger reader.</summary>
    internal const int MaxUncompressedBytes = 32 * 1024 * 1024;
    private const int HeaderBytes = 24;
    private const int Magic = 0x31424C48;
    private const int Version = 1;
    private const int Fields = 32;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    /// <summary>Encodes original references in descending physical order, with no financial interpretation or normalization.</summary>
    internal static byte[] Encode(IReadOnlyList<HomeLedgerPhysicalRow> rows, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count is < 1 or > MaxRows) throw new InvalidDataException("A bounded nonempty ledger block is required.");
        var writer = new BlockWriter();
        var bucket = rows[0].RowId >> BlockShift;
        long? previous = null;
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            if (row.RowId >> BlockShift != bucket || previous is { } prior && row.RowId >= prior)
                throw new InvalidDataException("Original physical ledger ordering differs.");
            previous = row.RowId;
            var entry = row.Entry ?? throw new InvalidDataException("Missing original typed row.");
            writer.Long(row.RowId);
            // Match the guarded scalar projection order; preserve nullable tags and undefined enum values too.
            writer.Guid(entry.Id); writer.Guid(entry.AccountId);
            writer.Day(entry.AggregatedFrom); writer.Day(entry.AggregatedTo); writer.Long(entry.Amount);
            writer.Guid(entry.CategoryId); writer.Timestamp(entry.CreatedAt); writer.Int(entry.Date.DayNumber);
            writer.Int(entry.Direction is { } direction ? (int)direction : null); writer.Guid(entry.GroupId);
            writer.Text(entry.Icon); writer.Guid(entry.ImportBatchId); writer.Bool(entry.IsAggregated);
            writer.Bool(entry.IsPartialPayment); writer.Int((int)entry.Kind); writer.Text(entry.Note);
            writer.Day(entry.OccurrenceDate); writer.Long(entry.OriginalAmount); writer.Text(entry.OriginalCurrencyCode);
            writer.Text(entry.Payee); writer.Guid(entry.RefundOfId); writer.Long(entry.ReimbursableAmount);
            writer.Text(entry.ReimbursedBy); writer.Day(entry.ReimbursementDueDate); writer.Int((int)entry.Review);
            writer.Guid(entry.ScheduleId); writer.Int((int)entry.Source);
            writer.Int(entry.Tags?.Count ?? -1);
            if (entry.Tags is { } tags) foreach (var tag in tags) { ct.ThrowIfCancellationRequested(); writer.Text(tag); }
            writer.Text(entry.Title); writer.Guid(entry.ToAccountId); writer.Long(entry.ToAmount); writer.Timestamp(entry.UpdatedAt);
        }
        var raw = writer.Bytes;
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        if (!BrotliEncoder.TryCompress(raw, compressed, out var written, quality: 1, window: 22))
            throw new InvalidDataException("The bounded ledger block could not be encoded.");
        ct.ThrowIfCancellationRequested();
        var result = new byte[checked(HeaderBytes + written)];
        var header = result.AsSpan();
        BinaryPrimitives.WriteInt32LittleEndian(header, Magic);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], Version);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..], Fields);
        BinaryPrimitives.WriteInt32LittleEndian(header[12..], rows.Count);
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], raw.Length);
        BinaryPrimitives.WriteInt32LittleEndian(header[20..], written);
        compressed.AsSpan(0, written).CopyTo(header[HeaderBytes..]);
        return result;
    }

    /// <summary>Creates independent mutable domain rows only from an exact bounded block and expected physical bucket.</summary>
    internal static List<HomeLedgerPhysicalRow> Decode(ReadOnlySpan<byte> payload, long bucket, CancellationToken ct = default)
    {
        if (payload.Length < HeaderBytes || payload.Length > HeaderBytes + BrotliEncoder.GetMaxCompressedLength(MaxUncompressedBytes)
            || BinaryPrimitives.ReadInt32LittleEndian(payload) != Magic
            || BinaryPrimitives.ReadInt32LittleEndian(payload[4..]) != Version
            || BinaryPrimitives.ReadInt32LittleEndian(payload[8..]) != Fields)
            throw new InvalidDataException("Unknown or oversized derived ledger block.");
        var count = BinaryPrimitives.ReadInt32LittleEndian(payload[12..]);
        var rawBytes = BinaryPrimitives.ReadInt32LittleEndian(payload[16..]);
        var compressedBytes = BinaryPrimitives.ReadInt32LittleEndian(payload[20..]);
        if (count is < 1 or > MaxRows || rawBytes is < 1 or > MaxUncompressedBytes
            || compressedBytes < 1 || compressedBytes != payload.Length - HeaderBytes)
            throw new InvalidDataException("Invalid derived ledger block bounds.");
        ct.ThrowIfCancellationRequested();
        var raw = new byte[rawBytes];
        var decoder = new BrotliDecoder();
        try
        {
            var status = decoder.Decompress(payload[HeaderBytes..], raw, out var consumed, out var expanded);
            if (status != OperationStatus.Done || consumed != compressedBytes || expanded != rawBytes)
                throw new InvalidDataException("Incomplete or trailing derived ledger block content.");
        }
        finally { decoder.Dispose(); }
        var reader = new BlockReader(raw);
        var rows = new List<HomeLedgerPhysicalRow>(count);
        long? previous = null;
        for (var index = 0; index < count; index++)
        {
            ct.ThrowIfCancellationRequested();
            var rowId = reader.Long();
            if (rowId >> BlockShift != bucket || previous is { } prior && rowId >= prior)
                throw new InvalidDataException("Derived ledger ordering or bucket differs.");
            previous = rowId;
            var entry = new LedgerEntry(reader.Guid())
            {
                AccountId = reader.Guid(), AggregatedFrom = reader.Day(), AggregatedTo = reader.Day(), Amount = reader.Long(),
                CategoryId = reader.OptionalGuid(), CreatedAt = reader.Timestamp(), Date = reader.RequiredDay(),
                Direction = reader.OptionalInt() is { } direction ? (AdjustmentDirection)direction : null,
                GroupId = reader.OptionalGuid(), Icon = reader.Text(), ImportBatchId = reader.OptionalGuid(),
                IsAggregated = reader.Bool(), IsPartialPayment = reader.Bool(), Kind = (EntryKind)reader.Int(), Note = reader.Text(),
                OccurrenceDate = reader.Day(), OriginalAmount = reader.OptionalLong(), OriginalCurrencyCode = reader.Text(),
                Payee = reader.Text(), RefundOfId = reader.OptionalGuid(), ReimbursableAmount = reader.OptionalLong(),
                ReimbursedBy = reader.Text(), ReimbursementDueDate = reader.Day(), Review = (ReviewState)reader.Int(),
                ScheduleId = reader.OptionalGuid(), Source = (EntrySource)reader.Int()
            };
            var tags = reader.Int();
            if (tags < -1 || tags > reader.Remaining / sizeof(int))
                throw new InvalidDataException("Invalid derived tag collection bounds.");
            entry.Tags = tags < 0 ? null! : new List<string>(tags);
            for (var tag = 0; tag < tags; tag++) { ct.ThrowIfCancellationRequested(); entry.Tags.Add(reader.Text()!); }
            entry.Title = reader.Text(); entry.ToAccountId = reader.OptionalGuid();
            entry.ToAmount = reader.OptionalLong(); entry.UpdatedAt = reader.Timestamp();
            rows.Add(new(rowId, entry));
        }
        if (reader.Remaining != 0) throw new InvalidDataException("Unexpected derived ledger fields remain.");
        return rows;
    }

    /// <summary>Writes bounded primitives directly into one owned buffer, avoiding per-value temporary byte arrays.</summary>
    private sealed class BlockWriter
    {
        private readonly ArrayBufferWriter<byte> buffer = new();
        internal ReadOnlySpan<byte> Bytes => buffer.WrittenSpan;
        private Span<byte> Reserve(int size)
        {
            if (size < 0 || size > MaxUncompressedBytes - buffer.WrittenCount)
                throw new InvalidDataException("The derived block is too large; retain scalar reading.");
            return buffer.GetSpan(size)[..size];
        }
        internal void Int(int value) { BinaryPrimitives.WriteInt32LittleEndian(Reserve(sizeof(int)), value); buffer.Advance(sizeof(int)); }
        internal void Long(long value) { BinaryPrimitives.WriteInt64LittleEndian(Reserve(sizeof(long)), value); buffer.Advance(sizeof(long)); }
        internal void Bool(bool value) { Reserve(1)[0] = value ? (byte)1 : (byte)0; buffer.Advance(1); }
        internal void Guid(Guid value) { value.TryWriteBytes(Reserve(16)); buffer.Advance(16); }
        internal void Guid(Guid? value) { Bool(value.HasValue); if (value.HasValue) Guid(value.Value); }
        internal void Int(int? value) { Bool(value.HasValue); if (value.HasValue) Int(value.Value); }
        internal void Long(long? value) { Bool(value.HasValue); if (value.HasValue) Long(value.Value); }
        internal void Day(DateOnly? value) => Int(value?.DayNumber);
        internal void Timestamp(DateTimeOffset value) { Long(value.Ticks); Long(value.Offset.Ticks); }
        internal void Text(string? value)
        {
            if (value is null) { Int(-1); return; }
            try
            {
                var size = Utf8.GetByteCount(value); Int(size);
                Utf8.GetBytes(value, Reserve(size)); buffer.Advance(size);
            }
            catch (EncoderFallbackException error) { throw new InvalidDataException("The derived block requires exact UTF-8; retain scalar reading.", error); }
        }
    }

    /// <summary>Reads only checked slices; malformed cache bytes never become truncated values or partial rows.</summary>
    private ref struct BlockReader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> remaining = bytes;
        internal int Remaining => remaining.Length;
        private ReadOnlySpan<byte> Take(int count)
        {
            if (count < 0 || count > remaining.Length) throw new InvalidDataException("Incomplete derived ledger primitive.");
            var result = remaining[..count]; remaining = remaining[count..]; return result;
        }
        internal int Int() => BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));
        internal long Long() => BinaryPrimitives.ReadInt64LittleEndian(Take(sizeof(long)));
        internal bool Bool() => Take(1)[0] switch { 0 => false, 1 => true, _ => throw new InvalidDataException("Invalid derived boolean.") };
        internal Guid Guid() => new(Take(16));
        internal Guid? OptionalGuid() => Bool() ? Guid() : null;
        internal int? OptionalInt() => Bool() ? Int() : null;
        internal long? OptionalLong() => Bool() ? Long() : null;
        internal DateOnly? Day() => Bool() ? RequiredDay() : null;
        internal DateOnly RequiredDay()
        {
            var value = Int();
            if (value < DateOnly.MinValue.DayNumber || value > DateOnly.MaxValue.DayNumber)
                throw new InvalidDataException("Invalid derived date.");
            return DateOnly.FromDayNumber(value);
        }
        internal DateTimeOffset Timestamp()
        {
            var ticks = Long(); var offset = Long();
            try { return new(ticks, TimeSpan.FromTicks(offset)); }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid derived timestamp.", error); }
        }
        internal string? Text()
        {
            var length = Int();
            if (length == -1) return null;
            try { return Utf8.GetString(Take(length)); }
            catch (DecoderFallbackException error) { throw new InvalidDataException("Invalid derived UTF-8.", error); }
        }
    }
}
