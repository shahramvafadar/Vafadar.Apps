using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data.Tests;

/// <summary>One original public verifier invocation cancelled from a real SQLite sort callback, with no timing oracle.</summary>
internal static class SnapshotSortCancellationScenario
{
    /// <summary>Runs distinct fictitious in-memory inputs; captures outcomes before checking full source and caller usability.</summary>
    internal static async Task<SortCancellationOutcome> RunAsync(bool cancelExpected, int rows, string prefix, CancellationToken ct)
    {
        using var cancelled = new CancellationTokenSource();
        await using var expected = await DatabaseAsync(rows, prefix, 1, ct);
        await using var actual = await DatabaseAsync(rows, prefix, cancelExpected ? 1 : 2, ct);
        var beforeExpected = await SnapshotAsync(expected, ct); var beforeActual = await SnapshotAsync(actual, ct);
        var firstCalls = 0; var secondCalls = 0; var armed = true;
        expected.CreateCollation("BINARY", (a, b) => Compare(a, b, true));
        actual.CreateCollation("BINARY", (a, b) => Compare(a, b, false));
        Exception? failure = null;
        try { await SqliteSnapshotVerifier.VerifyAsync(expected, actual, cancelled.Token); }
        catch (Exception error) when (error is not OutOfMemoryException) { failure = error; }
        var originalFirst = firstCalls; var originalSecond = secondCalls; armed = false;
        var unchanged = string.Equals(beforeExpected, await SnapshotAsync(expected, ct), StringComparison.Ordinal) && string.Equals(beforeActual, await SnapshotAsync(actual, ct), StringComparison.Ordinal);
        var usable = expected.State == ConnectionState.Open && actual.State == ConnectionState.Open
            && await CountAsync(expected, ct) == rows && await CountAsync(actual, ct) == rows;
        return new(cancelExpected, rows, originalFirst, originalSecond, failure?.GetType().Name,
            failure is OperationCanceledException stopped && stopped.CancellationToken == cancelled.Token,
            cancelled.IsCancellationRequested, unchanged, usable);

        int Compare(string? a, string? b, bool first)
        {
            if (armed && a?.StartsWith(prefix, StringComparison.Ordinal) == true && b?.StartsWith(prefix, StringComparison.Ordinal) == true)
            {
                if (first) firstCalls++; else secondCalls++;
                if (first == cancelExpected && !cancelled.IsCancellationRequested) cancelled.Cancel();
            }
            return StringComparer.Ordinal.Compare(a, b);
        }
    }

    private static async Task<SqliteConnection> DatabaseAsync(int rows, string prefix, int payload, CancellationToken ct)
    {
        var db = new SqliteConnection("Data Source=:memory:;Pooling=False");
        try
        {
            await db.OpenAsync(ct); await using var command = db.CreateCommand();
            command.CommandText = "CREATE TABLE packet(payload INTEGER,label TEXT); PRAGMA user_version=955; PRAGMA application_id=955; "
                + "WITH RECURSIVE n(v) AS (VALUES(1) UNION ALL SELECT v+1 FROM n WHERE v<$rows) "
                + "INSERT INTO packet SELECT $payload,$prefix||printf('%06d',1000000-v) FROM n;";
            command.Parameters.AddWithValue("$rows", rows); command.Parameters.AddWithValue("$payload", payload); command.Parameters.AddWithValue("$prefix", prefix);
            await command.ExecuteNonQueryAsync(ct); return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    private static async Task<string> SnapshotAsync(SqliteConnection db, CancellationToken ct)
    {
        await using var command = db.CreateCommand(); command.CommandText = "SELECT typeof(payload),payload,typeof(label),CAST(label AS BLOB) FROM packet ORDER BY label COLLATE BINARY";
        await using var reader = await command.ExecuteReaderAsync(ct); var rows = new List<string>();
        // Full typed values, not a digest; fixed type names/numeric/hex fields have unambiguous separators.
        // Keep this same oracle source-linkable under native AOT without reflection JSON metadata.
        while (await reader.ReadAsync(ct)) rows.Add(reader.GetString(0) + ":" + reader.GetInt64(1).ToString(CultureInfo.InvariantCulture)
            + ":" + reader.GetString(2) + ":" + Convert.ToHexString(reader.GetFieldValue<byte[]>(3)));
        return string.Join("\n", rows);
    }

    private static async Task<long> CountAsync(SqliteConnection db, CancellationToken ct)
    {
        await using var command = db.CreateCommand(); command.CommandText = "SELECT count(*) FROM packet";
        return (long)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidDataException());
    }
}

/// <summary>Bounded control counts and original cancellation/source outcomes; no financial contents are returned.</summary>
internal sealed record SortCancellationOutcome(bool CancelExpected, int Rows, int ExpectedSortCalls, int ActualSortCalls,
    string? ErrorType, bool OriginalCancellationToken, bool Cancelled, bool SourceExact, bool CallersUsable);
