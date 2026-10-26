using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>Optional complete-row transport owned by the original encrypted SQLite transaction, never financial authority.</summary>
internal static class HomeLedgerReadBlocks
{
    private const int CodecVersion = 1;
    private const int MaxPayloadBytes = HomeLedgerBlockCodec.MaxUncompressedBytes + 1024 * 1024;

    /// <summary>Returns fresh complete rows only for the captured schema/revision; getters never rebuild or repair storage.</summary>
    internal static async Task<List<LedgerEntry>?> TryReadAsync(ZananceDbContext db, CancellationToken ct,
        Func<HomeLedgerBlockBuffer>? createBuffer = null)
    {
        if (!db.CanUseHomeLedgerBlocks || db.Database.CurrentTransaction is null) return null;
        var state = await ReadStateAsync(db, ct).ConfigureAwait(false);
        if (state is null || state.Revision != state.PublishedRevision || state.Codec != CodecVersion
            || state.Schema != await SchemaAsync(db, ct).ConfigureAwait(false) || state.Rows is null or < 0 or > int.MaxValue
            || await NumberAsync(db, "SELECT count(*) FROM HomeLedgerDirtyBlocks", ct).ConfigureAwait(false) != 0) return null;
        // A damaged optional count/block set must never turn an existing ledger into an empty/partial Home.
        if (state.Rows != await NumberAsync(db, "SELECT count(*) FROM Entries", ct).ConfigureAwait(false)) return null;
        var entries = new List<LedgerEntry>();
        await using var result = await QueryAsync(db, $"""
            SELECT BlockId, CASE WHEN typeof(RowCount)='integer' THEN RowCount ELSE 0 END,
                CASE WHEN typeof(Payload)='blob' AND length(Payload) <= {MaxPayloadBytes} THEN Payload ELSE NULL END,
                CASE WHEN typeof(Digest)='blob' AND length(Digest)=32 THEN Digest ELSE NULL END
            FROM HomeLedgerReadBlocks ORDER BY BlockId DESC
            """, ct).ConfigureAwait(false);
        // D-547: disposal precedes return/fallback and the caller's projection, including reader failure/cancellation.
        using var buffer = createBuffer is null ? new HomeLedgerBlockBuffer()
            : createBuffer() ?? throw new InvalidOperationException("A fresh owned ledger buffer is required.");
        var reader = result.DbDataReader;
        long? previous = null;
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            var id = reader.GetInt64(0); var count = reader.GetInt64(1);
            if (previous is { } prior && id >= prior || count is < 1 or > HomeLedgerBlockCodec.MaxRows
                || reader.IsDBNull(2) || reader.IsDBNull(3)) return null;
            previous = id;
            var payload = reader.GetFieldValue<byte[]>(2); var digest = reader.GetFieldValue<byte[]>(3);
            if (digest.Length != 32 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), digest)) return null;
            List<HomeLedgerPhysicalRow> rows;
            try { rows = HomeLedgerBlockCodec.DecodeWithBuffer(payload, id, buffer, ct); }
            catch (Exception error) when (error is InvalidDataException or DecoderFallbackException) { return null; }
            if (rows.Count != count || entries.Count + (long)rows.Count > state.Rows) return null;
            foreach (var row in rows) entries.Add(row.Entry);
        }
        return entries.Count == state.Rows ? entries : null;
    }

    /// <summary>
    /// Refreshes affected buckets before the actual original commit. Unsupported values decline transport;
    /// database errors/cancellation still abort the same transaction, with no separately accepted financial write.
    /// </summary>
    internal static async Task MaintainAsync(ZananceDbContext db, CancellationToken ct)
    {
        if (!db.CanUseHomeLedgerBlocks || db.Database.CurrentTransaction is null) return;
        var state = await ReadStateAsync(db, ct).ConfigureAwait(false);
        if (state is null) return; // Older/mid-migration schemas keep their original scalar path.
        var schema = await SchemaAsync(db, ct).ConfigureAwait(false);
        if (!await HasOriginalTriggersAsync(db, ct).ConfigureAwait(false)) return;
        var dirtyCount = await NumberAsync(db, "SELECT count(*) FROM HomeLedgerDirtyBlocks", ct).ConfigureAwait(false);
        if (state.Schema != schema || state.Codec != CodecVersion || state.Revision != state.PublishedRevision && dirtyCount == 0)
        {
            // VACUUM/schema replacement may change physical identities without entry triggers.
            await db.Database.ExecuteSqlRawAsync("""
                UPDATE HomeLedgerReadState SET PublishedRevision=NULL;
                DELETE FROM HomeLedgerReadBlocks;
                DELETE FROM HomeLedgerDirtyBlocks;
                INSERT INTO HomeLedgerDirtyBlocks SELECT DISTINCT rowid >> 9 FROM Entries;
                """, ct).ConfigureAwait(false);
        }
        else if (state.Revision == state.PublishedRevision && dirtyCount == 0) return;

        var dirty = new List<long>();
        await using (var result = await QueryAsync(db, "SELECT BlockId FROM HomeLedgerDirtyBlocks ORDER BY BlockId", ct).ConfigureAwait(false))
            while (await result.DbDataReader.ReadAsync(ct).ConfigureAwait(false)) dirty.Add(result.DbDataReader.GetInt64(0));
        foreach (var id in dirty)
        {
            ct.ThrowIfCancellationRequested();
            byte[]? payload = null;
            List<HomeLedgerPhysicalRow>? rows = null;
            try
            {
                rows = await CompleteHomeLedgerReader.ReadPhysicalBlockAsync(db, id, ct).ConfigureAwait(false);
                if (rows is { Count: > 0 }) payload = HomeLedgerBlockCodec.Encode(rows, ct);
            }
            catch (Exception error) when (error is InvalidDataException or EncoderFallbackException or FormatException or OverflowException)
            { /* Original storage remains authoritative; unsupported derived values keep the scalar/error behavior. */ }
            if (rows is { Count: 0 })
                await db.Database.ExecuteSqlRawAsync("DELETE FROM HomeLedgerReadBlocks WHERE BlockId={0}", [id], ct).ConfigureAwait(false);
            else
            {
                var count = rows?.Count ?? await CountBucketAsync(db, id, ct).ConfigureAwait(false);
                if (count > 0)
                    await db.Database.ExecuteSqlRawAsync("""
                        INSERT INTO HomeLedgerReadBlocks(BlockId, RowCount, Payload, Digest) VALUES({0},{1},{2},{3})
                        ON CONFLICT(BlockId) DO UPDATE SET RowCount=excluded.RowCount, Payload=excluded.Payload, Digest=excluded.Digest
                        """, [id, count, new SqliteParameter("blockPayload", SqliteType.Blob) { Value = (object?)payload ?? DBNull.Value },
                            new SqliteParameter("blockDigest", SqliteType.Blob) { Value = payload is null ? DBNull.Value : SHA256.HashData(payload) }], ct).ConfigureAwait(false);
            }
            await db.Database.ExecuteSqlRawAsync("DELETE FROM HomeLedgerDirtyBlocks WHERE BlockId={0}", [id], ct).ConfigureAwait(false);
        }
        var total = await NumberAsync(db, "SELECT count(*) FROM Entries", ct).ConfigureAwait(false);
        var complete = await NumberAsync(db, "SELECT coalesce(sum(RowCount),0) FROM HomeLedgerReadBlocks", ct).ConfigureAwait(false) == total
            && await NumberAsync(db, "SELECT count(*) FROM HomeLedgerReadBlocks WHERE Payload IS NULL OR Digest IS NULL", ct).ConfigureAwait(false) == 0;
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE HomeLedgerReadState SET PublishedRevision=CASE WHEN {0} AND Revision={4} THEN Revision ELSE NULL END,
                SchemaVersion={1}, CodecVersion={2}, RowCount={3} WHERE Id=1
            """, [complete, schema, CodecVersion, total, state.Revision], ct).ConfigureAwait(false);
    }

    private static async Task<long> CountBucketAsync(ZananceDbContext db, long id, CancellationToken ct)
    {
        var lower = unchecked(id << HomeLedgerBlockCodec.BlockShift);
        if (lower >> HomeLedgerBlockCodec.BlockShift != id) throw new InvalidDataException("Invalid derived bucket identity.");
        return await NumberAsync(db, $"SELECT count(*) FROM Entries WHERE rowid BETWEEN {lower.ToString(CultureInfo.InvariantCulture)} AND {(lower | 511).ToString(CultureInfo.InvariantCulture)}", ct).ConfigureAwait(false);
    }

    private static Task<long> SchemaAsync(ZananceDbContext db, CancellationToken ct) => NumberAsync(db, "PRAGMA schema_version", ct);

    private static async Task<State?> ReadStateAsync(ZananceDbContext db, CancellationToken ct)
    {
        if (await NumberAsync(db, "SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('HomeLedgerReadState','HomeLedgerReadBlocks','HomeLedgerDirtyBlocks')", ct).ConfigureAwait(false) != 3) return null;
        await using var result = await QueryAsync(db, """
            SELECT CASE WHEN typeof(Revision)='text' THEN Revision ELSE NULL END,
                CASE WHEN typeof(PublishedRevision)='text' THEN PublishedRevision ELSE NULL END,
                CASE WHEN typeof(SchemaVersion)='integer' THEN SchemaVersion ELSE NULL END,
                CASE WHEN typeof(CodecVersion)='integer' THEN CodecVersion ELSE NULL END,
                CASE WHEN typeof(RowCount)='integer' THEN RowCount ELSE NULL END
            FROM HomeLedgerReadState WHERE Id=1
            """, ct).ConfigureAwait(false);
        var reader = result.DbDataReader;
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.IsDBNull(0)) return null;
        var revision = reader.GetString(0);
        if (!Guid.TryParseExact(revision, "N", out _)) return null;
        return new(revision, reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetInt64(3), reader.IsDBNull(4) ? null : reader.GetInt64(4));
    }

    private static async Task<bool> HasOriginalTriggersAsync(ZananceDbContext db, CancellationToken ct)
    {
        // Derived maintenance must have no path back into authoritative tables or recursive invalidation.
        if (await NumberAsync(db, "SELECT count(*) FROM sqlite_master WHERE type='trigger' AND tbl_name IN ('HomeLedgerReadBlocks','HomeLedgerReadState','HomeLedgerDirtyBlocks')", ct).ConfigureAwait(false) != 0) return false;
        foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
        {
            var expected = Migrations.HomeLedgerDirtyBlockConflictGuards.TriggerSql(operation);
            await using var result = await QueryAsync(db, $"SELECT sql FROM sqlite_master WHERE type='trigger' AND name='HomeLedgerReadBlocks_Entries_{operation}'", ct).ConfigureAwait(false);
            if (!await result.DbDataReader.ReadAsync(ct).ConfigureAwait(false)
                || Compact(result.DbDataReader.GetString(0).TrimEnd(';')) != Compact(expected.TrimEnd(';'))) return false;
        }
        // New/missing/shadowed source columns require review rather than obsolete32-field blocks.
        var columns = db.Model.FindEntityType(typeof(LedgerEntry))!.GetProperties()
            .Select(property => property.GetColumnName()).ToHashSet(StringComparer.Ordinal);
        if (columns.Count != 32) return false;
        await using (var result = await QueryAsync(db, "SELECT name FROM pragma_table_info('Entries')", ct).ConfigureAwait(false))
            while (await result.DbDataReader.ReadAsync(ct).ConfigureAwait(false))
                if (!columns.Remove(result.DbDataReader.GetString(0))) return false;
        return columns.Count == 0;
    }

    private static string Compact(string sql) => string.Concat(sql.Where(character => !char.IsWhiteSpace(character)));

    private static async Task<long> NumberAsync(ZananceDbContext db, string sql, CancellationToken ct)
    {
        await using var result = await QueryAsync(db, sql, ct).ConfigureAwait(false);
        if (!await result.DbDataReader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("Missing derived ledger metadata.");
        return result.DbDataReader.GetInt64(0);
    }

    /// <summary>Retains the same protected command guards, diagnostics, timeout and joined native transaction.</summary>
    private static Task<RelationalDataReader> QueryAsync(ZananceDbContext db, string sql, CancellationToken ct)
    {
        var command = db.GetService<IRelationalCommandBuilderFactory>().Create().Append(sql, sensitive: false).Build();
        return command.ExecuteReaderAsync(new(db.GetService<IRelationalConnection>(), null, null, db,
            db.GetService<IRelationalCommandDiagnosticsLogger>(), CommandSource.FromSqlQuery), ct);
    }

    private sealed record State(string Revision, string? PublishedRevision, long? Schema, long? Codec, long? Rows);
}
