using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data.PortableBudgetChecks;

/// <summary>Checks the actual native export's page budget without an app profile, financial command or copied exporter.</summary>
public static partial class PortableBounds
{
    /// <summary>Executes seven finite new/affected native boundary cases and preserves each result before the next case.</summary>
    public static async Task RunAsync(string output, CancellationToken cancellationToken = default)
    {
        var cases = new[]
        {
            new Case("missing-policy-oversized", null, 131072, 0, false, false),
            new Case("empty-policy-oversized", [], 131072, 0, false, false),
            new Case("missing-policy-fitting", null, 64, 0, true, false),
            new Case("empty-policy-fitting", [], 64, 0, true, false),
            new Case("omitted-large-private-journal", ["private_journal"], 64, 131072, true, false),
            new Case("nonempty-policy-oversized", ["private_journal"], 131072, 64, false, false),
            new Case("cancel-before-native-export", null, 64, 0, false, true)
        };
        foreach (var test in cases)
        {
            var result = await RunCaseAsync(test, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(output, test.Name + ".json"),
                JsonSerializer.Serialize(result, BoundsJson.Default.Result), cancellationToken);
            if (!result.Passed) throw new InvalidOperationException("Original native portable boundary failed: " + test.Name);
        }
    }

    private static async Task<Result> RunCaseAsync(Case test, CancellationToken token)
    {
        const int maximum = 32768;
        await using var source = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await source.OpenAsync(token);
        await using var command = source.CreateCommand();
        command.CommandText = "CREATE TABLE packet(id INTEGER PRIMARY KEY, amount INTEGER, label TEXT, data BLOB); "
            + "CREATE TABLE private_journal(data BLOB); PRAGMA user_version=73; PRAGMA application_id=4660;";
        await command.ExecuteNonQueryAsync(token);
        command.CommandText = "INSERT INTO packet VALUES(1,$amount,$label,zeroblob($size))";
        command.Parameters.AddWithValue("$amount", long.MinValue);
        command.Parameters.AddWithValue("$label", "Fictitious بانک Ü");
        command.Parameters.AddWithValue("$size", test.Payload);
        await command.ExecuteNonQueryAsync(token); command.Parameters.Clear();
        if (test.PrivatePayload != 0)
        {
            command.CommandText = "INSERT INTO private_journal VALUES(zeroblob($size))";
            command.Parameters.AddWithValue("$size", test.PrivatePayload);
            await command.ExecuteNonQueryAsync(token); command.Parameters.Clear();
        }
        var before = await ReadAsync(source, token);
        var prepared = 0; long? pages = null; long? pageLimit = null;
        using var bytes = new MemoryStream(); Exception? failure = null;
        using var cancelled = new CancellationTokenSource(); if (test.Cancel) cancelled.Cancel();
        try
        {
            await SqlCipherPortableSnapshot.WriteAsync(source, bytes, maximum, null,
                test.Cancel ? cancelled.Token : token, async (connection, alias, ct) =>
                {
                    prepared++;
                    await using var inspect = connection.CreateCommand();
                    inspect.CommandText = $"PRAGMA {alias}.page_count";
                    pages = Convert.ToInt64(await inspect.ExecuteScalarAsync(ct));
                    inspect.CommandText = $"PRAGMA {alias}.max_page_count";
                    pageLimit = Convert.ToInt64(await inspect.ExecuteScalarAsync(ct));
                }, test.Omitted);
        }
        catch (Exception error) when (error is not OutOfMemoryException) { failure = error; }
        var after = await ReadAsync(source, token);
        command.CommandText = "PRAGMA database_list";
        var aliases = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) aliases.Add(reader.GetString(1));
        var released = !aliases.Any(name => name.StartsWith("portable_", StringComparison.Ordinal));
        var same = before == after; var validOutput = false;
        if (test.Fits && failure is null)
        {
            bytes.Position = 0;
            await using var image = await SqliteMemorySnapshot.OpenAsync(bytes, maximum, token);
            var actual = await ReadAsync(image.Connection, token);
            // Only the explicitly omitted journal rows differ; financial/header/schema inputs remain exact.
            validOutput = actual == before with { PrivateRows = test.Omitted?.Contains("private_journal") == true ? 0 : before.PrivateRows };
        }
        var refused = test.Cancel ? failure is OperationCanceledException
            : failure is InvalidDataException { InnerException: SqliteException };
        var passed = same && released && (test.Fits
            ? failure is null && prepared == 1 && bytes.Length <= maximum && pages <= 8 && pageLimit == 8 && validOutput
            : refused && prepared == 0 && bytes.Length == 0);
        return new(test.Name, passed, prepared, pages, pageLimit, bytes.Length, same, released, validOutput,
            failure?.GetType().FullName, failure?.InnerException?.GetType().FullName);
    }

    private static async Task<Source> ReadAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,amount,label,data FROM packet ORDER BY id";
        int id; long amount; string label; string digest;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new InvalidDataException("Original packet missing.");
            id = reader.GetInt32(0); amount = reader.GetInt64(1); label = reader.GetString(2);
            digest = Convert.ToHexString(SHA256.HashData((byte[])reader.GetValue(3)));
            if (await reader.ReadAsync(ct)) throw new InvalidDataException("Unexpected packet row.");
        }
        command.CommandText = "SELECT count(*) FROM private_journal";
        var privateRows = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
        command.CommandText = "PRAGMA user_version"; var version = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
        command.CommandText = "PRAGMA application_id"; var application = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
        return new(id, amount, label, digest, privateRows, version, application);
    }

    private sealed record Case(string Name, string[]? Omitted, int Payload, int PrivatePayload, bool Fits, bool Cancel);
    private sealed record Source(int Id, long Amount, string Label, string PayloadDigest, long PrivateRows, long UserVersion, long ApplicationId);
    private sealed record Result(string Case, bool Passed, int PreparationCalls, long? ExportedPages,
        long? MaximumPages, long OutputBytes, bool SourceEqual, bool AttachmentReleased, bool ValidOriginalOutput,
        string? FailureType, string? InnerFailureType);

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Result))]
    private partial class BoundsJson : JsonSerializerContext;
}
