using System.Data;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Vafadar.Data;

/// <summary>Verifies logical schema, header identifiers and every typed row before accepting an encrypted staging image.</summary>
/// <remarks>Caller owns exclusive generation/key coordination. This method does not commit a file or choose a profile.</remarks>
public static class SqliteSnapshotVerifier
{
    /// <summary>Compares open captured databases through stable read transactions without logging financial values.</summary>
    public static async Task VerifyAsync(SqliteConnection expected, SqliteConnection actual,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected); ArgumentNullException.ThrowIfNull(actual);
        if (expected.State != ConnectionState.Open || actual.State != ConnectionState.Open)
            throw new InvalidOperationException("Open both captured snapshot connections before verification.");
        await using var expectedTransaction = expected.BeginTransaction(deferred: true);
        await using var actualTransaction = actual.BeginTransaction(deferred: true);
        foreach (var header in new[] { "user_version", "application_id" })
        {
            if (!Equals(await Scalar(expected, expectedTransaction, "PRAGMA " + header, cancellationToken),
                await Scalar(actual, actualTransaction, "PRAGMA " + header, cancellationToken)))
                throw new InvalidDataException("Staging database header metadata differs.");
        }
        if (!string.Equals(await Scalar(actual, actualTransaction, "PRAGMA integrity_check", cancellationToken) as string, "ok", StringComparison.Ordinal))
            throw new InvalidDataException("Staging database integrity failed.");
        foreach (var check in new[] { "PRAGMA foreign_key_check", "PRAGMA cipher_integrity_check" })
        {
            // Unknown cipher pragma is empty on plain SQLite; SQLCipher reports damaged pages as rows.
            await using var command = actual.CreateCommand(); command.Transaction = actualTransaction; command.CommandText = check;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken)) throw new InvalidDataException("Staging relational or cipher integrity failed.");
        }
        var schema = await Schema(expected, expectedTransaction, cancellationToken);
        var other = await Schema(actual, actualTransaction, cancellationToken);
        if (!schema.SequenceEqual(other)) throw new InvalidDataException("Staging database logical schema differs.");
        foreach (var entry in schema.Where(x => x.Type == "table"))
        {
            var columns = new List<string>();
            await using (var command = expected.CreateCommand())
            {
                command.Transaction = expectedTransaction; command.CommandText = "PRAGMA table_xinfo(" + Quote(entry.Name) + ")";
                await using var rows = await command.ExecuteReaderAsync(cancellationToken);
                while (await rows.ReadAsync(cancellationToken))
                    if (rows.GetInt32(6) != 1) columns.Add(rows.GetString(1));
            }
            if (columns.Count == 0) throw new InvalidDataException("Snapshot table has no verifiable columns.");
            // Sort every storage class/value, retaining duplicate multiplicity without guessing a primary key.
            var projection = string.Join(",", columns.Select(x => $"typeof({Quote(x)}),CASE WHEN typeof({Quote(x)}) IN ('text','blob') THEN CAST({Quote(x)} AS BLOB) ELSE {Quote(x)} END"));
            var order = string.Join(",", columns.Select(x => $"typeof({Quote(x)}),{Quote(x)} COLLATE BINARY"));
            var query = $"SELECT {projection} FROM {Quote(entry.Name)} ORDER BY {order}";
            CompareRows(expected, actual, query, columns.Count, cancellationToken);
        }
        await expectedTransaction.CommitAsync(cancellationToken); await actualTransaction.CommitAsync(cancellationToken);
    }

    private static void CompareRows(SqliteConnection expected, SqliteConnection actual, string query, int columns, CancellationToken token)
    {
        sqlite3_stmt? first = null; sqlite3_stmt? second = null;
        try
        {
            if (raw.sqlite3_prepare_v2(expected.Handle!, query, out first) != raw.SQLITE_OK ||
                raw.sqlite3_prepare_v2(actual.Handle!, query, out second) != raw.SQLITE_OK)
                throw new InvalidDataException("Snapshot row comparison could not be prepared.");
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var a = raw.sqlite3_step(first); var b = raw.sqlite3_step(second);
                if (a == raw.SQLITE_DONE && b == raw.SQLITE_DONE) break;
                if (a != raw.SQLITE_ROW || b != raw.SQLITE_ROW)
                    throw new InvalidDataException("Snapshot row multiplicity or read status differs.");
                for (var i = 0; i < columns; i++)
                {
                    var index = i*2;
                    if (!raw.sqlite3_column_blob(first,index).SequenceEqual(raw.sqlite3_column_blob(second,index)))
                        throw new InvalidDataException("Snapshot storage class differs.");
                    index++;
                    var type = raw.sqlite3_column_type(first,index);
                    if (type != raw.sqlite3_column_type(second,index)) throw new InvalidDataException("Snapshot storage value type differs.");
                    var equal = type switch
                    {
                        raw.SQLITE_NULL => true,
                        raw.SQLITE_INTEGER => raw.sqlite3_column_int64(first,index) == raw.sqlite3_column_int64(second,index),
                        raw.SQLITE_FLOAT => BitConverter.DoubleToInt64Bits(raw.sqlite3_column_double(first,index)) == BitConverter.DoubleToInt64Bits(raw.sqlite3_column_double(second,index)),
                        raw.SQLITE_BLOB => raw.sqlite3_column_blob(first,index).SequenceEqual(raw.sqlite3_column_blob(second,index)),
                        _ => false
                    };
                    if (!equal) throw new InvalidDataException("Snapshot record content differs.");
                }
            }
        }
        finally
        {
            // Direct native spans avoid the driver's incremental-BLOB metadata lookup on expression columns.
            if (first is not null) raw.sqlite3_finalize(first);
            if (second is not null) raw.sqlite3_finalize(second);
        }
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private static async Task<object?> Scalar(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
        return await command.ExecuteScalarAsync(token);
    }

    private static async Task<List<(string Type, string Name, string Table, string? Sql)>> Schema(SqliteConnection connection, SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT type,name,tbl_name,sql FROM sqlite_schema ORDER BY type COLLATE BINARY,name COLLATE BINARY";
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<(string,string,string,string?)>();
        while (await reader.ReadAsync(token)) result.Add((reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.IsDBNull(3)?null:reader.GetString(3)));
        return result;
    }
}
