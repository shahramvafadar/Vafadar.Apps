using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Bounded framing only, not a cipher or a second financial algorithm. Each original row is serialized once and may span authenticated envelopes.</summary>
internal static class HouseholdRecordStream
{
    /// <summary>Writes exact original versioned rows without allocating a complete profile or equating rows with ciphertext parts.</summary>
    internal static void Write(IEnumerable<SyncRecord> records, Action<HouseholdRecordBlock> emit)
    {
        if (HouseholdFinancialSchema.Manifest.Tables.Any(table => table.Name.Length > 96 || table.Name.Any(character => !char.IsAsciiLetterOrDigit(character)) ||
            table.PrimaryKey.Length > 2 || table.PrimaryKey.Any(key => table.Columns.Single(column => column.Name == key).Kind != "guid") ||
            table.Columns.Where(column => column.Kind == "text" || column.Kind.StartsWith("json-", StringComparison.Ordinal))
                .Sum(column => (long)(column.MaximumLength ?? 1_000_000)) * 6 + 4096 > HouseholdFinancialFraming.MaximumRecordBytes))
            throw new InvalidOperationException("The changed financial identity contract needs reviewed household wire bounds.");
        var buffer = new byte[HouseholdFinancialFraming.BlockBytes]; var offset = 0; var index = 0; long total = 0; var count = 0;
        Span<byte> prefix = stackalloc byte[4];
        try
        {
            foreach (var record in records)
            {
                if (++count > SyncGraphValidation.MaximumRows) throw new SyncProfileBoundException();
                var bytes = JsonSerializer.SerializeToUtf8Bytes(record, PersonalSyncJsonContext.Default.SyncRecord);
                try
                {
                    if (bytes.Length is < 1 or > HouseholdFinancialFraming.MaximumRecordBytes) throw new SyncProfileBoundException();
                    BinaryPrimitives.WriteInt32BigEndian(prefix, bytes.Length);
                    Append(prefix); Append(bytes);
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            if (count == 0) throw new InvalidOperationException("An empty financial stream cannot publish a transaction.");
            Emit(final: true);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }

        void Append(ReadOnlySpan<byte> bytes)
        {
            total = checked(total + bytes.Length);
            if (total > HouseholdFinancialFraming.MaximumStreamBytes) throw new SyncProfileBoundException();
            while (bytes.Length != 0)
            {
                var size = Math.Min(bytes.Length, buffer.Length - offset); bytes[..size].CopyTo(buffer.AsSpan(offset));
                offset += size; bytes = bytes[size..]; if (offset == buffer.Length) Emit(final: false);
            }
        }
        void Emit(bool final)
        {
            var bytes = buffer.AsSpan(0, offset).ToArray();
            try { emit(new(index++, final, final ? total : 0, final ? count : 0, bytes)); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            CryptographicOperations.ZeroMemory(buffer); offset = 0;
        }
    }
}

/// <summary>Decodes at most one original row at a time and refuses truncation, reordered/mixed blocks, duplicate framing and dishonest final totals.</summary>
internal sealed class HouseholdRecordStreamReader(Action<SyncRecord> accept) : IDisposable
{
    private readonly byte[] prefix = new byte[4];
    private byte[]? row;
    private int prefixBytes, rowBytes, index, count;
    private long total;
    /// <summary>True after a stream block was actually seen, independently of ciphertext attachment count.</summary>
    internal bool Started => index != 0;
    /// <summary>True only after exact final totals and complete row framing were verified.</summary>
    internal bool Complete { get; private set; }
    /// <summary>Exact body position of the row currently handed to the accepted-record sink.</summary>
    internal long RecordOffset { get; private set; }
    /// <summary>Exact JSON byte length, excluding its authenticated four-byte framing prefix.</summary>
    internal int RecordLength { get; private set; }

    /// <summary>Consumes one exact owned plaintext block; all candidate byte arrays are cleared even on refusal.</summary>
    internal void Add(HouseholdRecordBlock block)
    {
        if (block.Bytes is null) throw Refusal();
        try
        {
            if (Complete || block.Index != index || block.Bytes.Length > HouseholdFinancialFraming.BlockBytes ||
                !block.Final && (block.Bytes.Length != HouseholdFinancialFraming.BlockBytes || block.Length != 0 || block.Records != 0)) throw Refusal();
            index++; total = checked(total + block.Bytes.Length);
            if (total > HouseholdFinancialFraming.MaximumStreamBytes) throw new SyncProfileBoundException();
            var bytes = block.Bytes.AsSpan();
            while (bytes.Length != 0)
            {
                if (row is null)
                {
                    var size = Math.Min(4 - prefixBytes, bytes.Length); bytes[..size].CopyTo(prefix.AsSpan(prefixBytes));
                    prefixBytes += size; bytes = bytes[size..]; if (prefixBytes != 4) continue;
                    var length = BinaryPrimitives.ReadInt32BigEndian(prefix);
                    if (length is < 1 or > HouseholdFinancialFraming.MaximumRecordBytes) throw Refusal();
                    row = new byte[length]; rowBytes = 0;
                }
                var amount = Math.Min(row.Length - rowBytes, bytes.Length); bytes[..amount].CopyTo(row.AsSpan(rowBytes));
                rowBytes += amount; bytes = bytes[amount..];
                if (rowBytes != row.Length) continue;
                if (++count > SyncGraphValidation.MaximumRows) throw new SyncProfileBoundException();
                var record = StrictJsonPayload.Deserialize(row, PersonalSyncJsonContext.Default.SyncRecord, HouseholdFinancialFraming.MaximumRecordBytes);
                RecordOffset = total - bytes.Length - row.Length; RecordLength = row.Length;
                accept(record); CryptographicOperations.ZeroMemory(row); row = null; prefixBytes = 0; rowBytes = 0;
            }
            if (block.Final)
            {
                if (count == 0 || row is not null || prefixBytes != 0 || block.Length != total || block.Records != count) throw Refusal();
                Complete = true;
            }
        }
        finally { CryptographicOperations.ZeroMemory(block.Bytes); }
    }

    /// <inheritdoc />
    public void Dispose() { CryptographicOperations.ZeroMemory(prefix); if (row is not null) CryptographicOperations.ZeroMemory(row); }
    private static CryptographicException Refusal() => new("The complete original household financial stream was refused.");
}
