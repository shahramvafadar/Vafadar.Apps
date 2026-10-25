using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.Performance;

/// <summary>Retires only the independent caller's optional schema after durable, complete logical source proof.</summary>
internal static class ReadBlockSchemaRetirement
{
    /// <summary>The sole additive transport migration this diagnostic can retire.</summary>
    internal const string Current = "20261025092800_HomeLedgerReadBlocks";
    /// <summary>The earlier internal revision suffix also absent from the retained original APK.</summary>
    internal const string Intermediate = "20261024115108_HomeFinancialRevision";
    /// <summary>The exact predecessor supported by the retained original caller.</summary>
    internal const string Target = "20261017013925_MarketSourceCompanionState";

    /// <summary>Actual migration/verification outcome; no financial operation is replayed to recover a missing outer report.</summary>
    internal sealed record Result(bool AlreadyRetired, string ExpectedFingerprint, bool CompleteOriginalSourceVerified,
        string TargetMigration, int FinancialCommands, string[] RemovedMigrationIds);

    private sealed record Request(int Version, string Comparison, string Scope, string DatabasePath, Guid DataModule,
        string TargetMigration, string ExpectedFingerprint);

    /// <summary>
    /// Caller supplies its real exclusive publication check and an independently verified memory copy.
    /// A durable request precedes the actual Down transaction; a resumed committed outcome is checked, never repeated.
    /// </summary>
    internal static async Task<Result> RetireAsync(ZananceDbContext db, SqliteConnection before,
        string requestPath, string comparison, string scope, Action demand,
        Func<string, string, CancellationToken, Task> saveNewDurable, CancellationToken ct)
    {
        demand();
        if (!System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$") || string.IsNullOrWhiteSpace(scope))
            throw new InvalidDataException("An explicit retirement lineage is required.");
        if (File.Exists(requestPath) && ((File.GetAttributes(requestPath) & FileAttributes.ReparsePoint) != 0
            || new FileInfo(requestPath).Length > 16 * 1024)) throw new InvalidDataException("A bounded original request is required.");
        if (db.Database.CurrentTransaction is not null || before.State != System.Data.ConnectionState.Open
            || before.DataSource is not ("" or ":memory:")
            || new SqliteConnectionStringBuilder(before.ConnectionString).DataSource != ":memory:")
            throw new InvalidOperationException("Independent open memory proof and no borrowed transaction are required.");
        var source = db.Database.GetDbConnection() as SqliteConnection
            ?? throw new InvalidOperationException("The original SQLite context is required.");
        await SqliteSnapshotVerifier.VerifyAsync(source, before, ct).ConfigureAwait(false);
        var applied = (await db.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToArray();
        var known = db.Database.GetMigrations().ToArray();
        if (applied.Length == 0 || !applied.SequenceEqual(known.Take(applied.Length))
            || applied[^1] is not (Current or Intermediate or Target))
            throw new InvalidDataException("Only the exact original caller migration prefix can be retired.");
        var retired = applied[^1] == Target;
        if (retired && !File.Exists(requestPath)) throw new InvalidDataException("An original retirement request is required for recovery.");
        await using var expected = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await expected.OpenAsync(ct).ConfigureAwait(false); before.BackupDatabase(expected);
        var originalProjection = await FingerprintAsync(before, ct, projectReadBlocks: true).ConfigureAwait(false);
        if (!retired)
        {
            await using var control = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(expected)
                .UseModel(ZananceDbContextModel.Instance).Options);
            await control.GetService<IMigrator>().MigrateAsync(Target, ct).ConfigureAwait(false);
        }
        var fingerprint = await FingerprintAsync(expected, ct).ConfigureAwait(false);
        if (originalProjection != fingerprint)
            throw new InvalidDataException("Memory retirement changed original objects or rows beyond the exact declared bookkeeping suffix.");
        var request = new Request(1, comparison, scope, source.DataSource, typeof(ZananceStore).Module.ModuleVersionId, Target, fingerprint);
        if (File.Exists(requestPath))
        {
            var original = JsonSerializer.Deserialize<Request>(await File.ReadAllTextAsync(requestPath, ct).ConfigureAwait(false));
            if (original != request) throw new InvalidDataException("The original retirement source/scope/code cannot be replaced.");
        }
        else await saveNewDurable(requestPath, JsonSerializer.Serialize(request), ct).ConfigureAwait(false);
        demand(); ct.ThrowIfCancellationRequested();
        if (!retired) await db.GetService<IMigrator>().MigrateAsync(Target, ct).ConfigureAwait(false);
        demand();
        await SqliteSnapshotVerifier.VerifyAsync(expected, source, ct).ConfigureAwait(false);
        if (await FingerprintAsync(source, ct).ConfigureAwait(false) != fingerprint)
            throw new InvalidDataException("Retirement must preserve every original schema/header/storage class/value.");
        demand();
        return new(retired, fingerprint, true, Target, 0, applied.SkipWhile(id => id != Target).Skip(1).ToArray());
    }

    /// <summary>Credential-free durable digest of the same logical schema, headers and typed rows checked by the original verifier.</summary>
    private static async Task<string> FingerprintAsync(SqliteConnection connection, CancellationToken ct, bool projectReadBlocks = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        foreach (var header in new[] { "user_version", "application_id" })
        {
            Append(hash, header); command.CommandText = "PRAGMA " + header;
            Append(hash, await command.ExecuteScalarAsync(ct).ConfigureAwait(false));
        }
        command.CommandText = "SELECT type,name,tbl_name,sql FROM sqlite_schema ORDER BY type COLLATE BINARY,name COLLATE BINARY";
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                if (projectReadBlocks && DerivedObjects.Contains(reader.GetString(1))) continue;
                hash.AppendData([0xFA]);
                for (var i = 0; i < reader.FieldCount; i++) Append(hash, reader.GetValue(i));
                if (reader.GetString(0) == "table") tables.Add(reader.GetString(1));
            }
        hash.AppendData([0xFB]);
        foreach (var table in tables)
        {
            ct.ThrowIfCancellationRequested(); Append(hash, table); hash.AppendData([0xFC]);
            command.CommandText = "PRAGMA table_info(" + Quote(table) + ")";
            var columns = new List<string>();
            await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
                while (await reader.ReadAsync(ct).ConfigureAwait(false)) columns.Add(reader.GetString(1));
            if (columns.Count == 0) throw new InvalidDataException("Complete original columns are required.");
            var projection = string.Join(",", columns.Select(column => $"typeof({Quote(column)}),CASE WHEN typeof({Quote(column)}) IN ('text','blob') THEN CAST({Quote(column)} AS BLOB) ELSE {Quote(column)} END"));
            var order = string.Join(",", columns.Select(column => $"typeof({Quote(column)}),{Quote(column)} COLLATE BINARY"));
            var filter = projectReadBlocks && table == "__EFMigrationsHistory" ? $" WHERE MigrationId NOT IN ('{Intermediate}','{Current}')" : "";
            command.CommandText = $"SELECT {projection} FROM {Quote(table)}{filter} ORDER BY {order}";
            await using var rows = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rows.ReadAsync(ct).ConfigureAwait(false))
            {
                hash.AppendData([0xFD]);
                for (var i = 0; i < rows.FieldCount; i++) { ct.ThrowIfCancellationRequested(); Append(hash, rows.GetValue(i)); }
            }
            hash.AppendData([0xFE]);
        }
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, object? value)
    {
        // Explicit storage/value tags preserve NULL, integer/real distinction, exact IEEE bits and raw text/blob bytes.
        var (tag, bytes) = value switch
        {
            null or DBNull => ((byte)0, Array.Empty<byte>()),
            long number => ((byte)1, Encoding.ASCII.GetBytes(number.ToString(CultureInfo.InvariantCulture))),
            double number => ((byte)2, Encoding.ASCII.GetBytes(BitConverter.DoubleToInt64Bits(number).ToString("X16", CultureInfo.InvariantCulture))),
            string text => ((byte)3, Encoding.UTF8.GetBytes(text)),
            byte[] blob => ((byte)4, blob),
            _ => throw new InvalidDataException("Unknown SQLite storage class in retirement proof.")
        };
        Span<byte> prefix = stackalloc byte[9]; prefix[0] = tag;
        BinaryPrimitives.WriteInt64LittleEndian(prefix[1..], bytes.LongLength);
        hash.AppendData(prefix); hash.AppendData(bytes);
    }

    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    private static readonly HashSet<string> DerivedObjects = new(new[] {
        "HomeFinancialRevision",
        "HomeLedgerReadState", "HomeLedgerReadBlocks", "HomeLedgerDirtyBlocks",
        "HomeLedgerReadBlocks_Entries_INSERT", "HomeLedgerReadBlocks_Entries_UPDATE", "HomeLedgerReadBlocks_Entries_DELETE"
    }.Concat(new[] { "Entries", "Budgets", "BudgetCategoryLimits", "ExchangeRates", "AssetTypes", "AssetEvents" }
        .SelectMany(table => new[] { "INSERT", "UPDATE", "DELETE" }.Select(operation => "HomeFinancialRevision_" + table + "_" + operation))), StringComparer.Ordinal);
}
