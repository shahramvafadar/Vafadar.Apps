using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Vafadar.Data;

namespace Vafadar.Zanance.ProtectedSync;

internal static partial class HistoryQualification
{
    /// <summary>Small native allocation-budget and failure-cleanup cases; the separate retained encrypted fixture proves actual household export.</summary>
    internal static async Task NativeBoundAsync(string root, Action<string> proof, CancellationToken ct)
    {
        var done = Path.Combine(root, "history-native-bound.complete.json");
        if (File.Exists(done)) throw new IOException("Never replay completed native allocation qualification.");
        void Require(bool value) { if (!value) throw new InvalidDataException("Native portable allocation invariant failed."); }
        await using var source = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await source.OpenAsync(ct);
        await using (var create = source.CreateCommand())
        {
            create.CommandText = "CREATE TABLE Money(Id INTEGER PRIMARY KEY, Amount INTEGER NOT NULL, Currency TEXT NOT NULL);" +
                "INSERT INTO Money VALUES(1,-9223372036854775808,'KWD');" +
                "CREATE TABLE PrivateHistory(Payload BLOB NOT NULL); INSERT INTO PrivateHistory VALUES(randomblob(2097152));" +
                "CREATE TABLE MoneyPayload(Payload BLOB NOT NULL);";
            await create.ExecuteNonQueryAsync(ct);
        }
        using var first = new MemoryStream();
        await SqlCipherPortableSnapshot.WriteAsync(source, first, 512 * 1024, null, ct, emptyRowTables: ["PrivateHistory"]);
        Require(first.Length < 512 * 1024);
        await using (var add = source.CreateCommand())
        { add.CommandText = "INSERT INTO MoneyPayload VALUES(randomblob(786432));"; await add.ExecuteNonQueryAsync(ct); }
        using var refused = new MemoryStream(); var full = false; var nativeCode = 0;
        try { await SqlCipherPortableSnapshot.WriteAsync(source, refused, 512 * 1024, null, ct, emptyRowTables: ["PrivateHistory"]); }
        // sqlcipher_export reports a pager failure through sqlite3_result_error, which may wrap
        // SQLITE_FULL as SQLITE_ERROR. The sole changed input is the over-budget financial row.
        catch (SqliteException error) when (error.SqliteErrorCode is raw.SQLITE_FULL or raw.SQLITE_ERROR)
        { full = true; nativeCode = error.SqliteErrorCode; }
        Require(full && refused.Length == 0 && raw.sqlite3_get_autocommit(source.Handle!) == 1);
        await using (var list = source.CreateCommand())
        {
            list.CommandText = "PRAGMA database_list;";
            await using var reader = await list.ExecuteReaderAsync(ct);
            Require(await reader.ReadAsync(ct) && reader.GetString(1) == "main" && !await reader.ReadAsync(ct));
        }
        using var final = new MemoryStream();
        await SqlCipherPortableSnapshot.WriteAsync(source, final, 2 * 1024 * 1024, null, ct, emptyRowTables: ["PrivateHistory"]);
        final.Position = 0;
        await using (var portable = await SqliteMemorySnapshot.OpenAsync(final, 2 * 1024 * 1024, ct))
        {
            await using var read = portable.Connection.CreateCommand();
            read.CommandText = "SELECT Amount FROM Money WHERE Id=1;";
            Require(Convert.ToInt64(await read.ExecuteScalarAsync(ct)) == long.MinValue);
            read.CommandText = "SELECT Currency FROM Money WHERE Id=1;"; Require((string?)await read.ExecuteScalarAsync(ct) == "KWD");
            read.CommandText = "SELECT length(Payload) FROM MoneyPayload;"; Require(Convert.ToInt64(await read.ExecuteScalarAsync(ct)) == 786432);
            read.CommandText = "SELECT count(*) FROM PrivateHistory;"; Require(Convert.ToInt64(await read.ExecuteScalarAsync(ct)) == 0);
        }
        await using (var read = source.CreateCommand())
        {
            read.CommandText = "SELECT length(Payload) FROM PrivateHistory;"; Require(Convert.ToInt64(await read.ExecuteScalarAsync(ct)) == 2097152);
            read.CommandText = "SELECT length(Payload) FROM MoneyPayload;"; Require(Convert.ToInt64(await read.ExecuteScalarAsync(ct)) == 786432);
        }
        await File.WriteAllTextAsync(done, JsonSerializer.Serialize(new NativePortableBoundReport(2097152, first.Length,
            full, nativeCode, true, final.Length, true, true, true), NativePortableBoundJsonContext.Default.NativePortableBoundReport), ct);
        proof("native-destination-budget-precedes-allocation-private-rows-never-copy-and-full-refusal-detaches-with-source-finance-intact");
    }
}

/// <summary>Content-free native allocation/failure proof, also rooted explicitly for the independent trimmed Android caller.</summary>
internal sealed record NativePortableBoundReport(int PrivateBytesNeverCopied, long InitialPortableBytes, bool OverBudgetRefusedBeforeOutput,
    int NativeBudgetRefusalCode, bool OriginalConnectionDetachedAndUsable, long FinalPortableBytes,
    bool ExactSignedInt64AndCurrencyPreserved, bool OriginalPrivateAndFinancialRowsPreserved, bool NativeMechanismOnly);

/// <summary>No reflection metadata is required for the independent native report.</summary>
[JsonSerializable(typeof(NativePortableBoundReport))]
internal partial class NativePortableBoundJsonContext : JsonSerializerContext;
