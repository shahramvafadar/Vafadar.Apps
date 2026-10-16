using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Backup;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Upgrades the retained large native ciphertext fixture without network, new money, replacement keys or replaying its completed publication cases.</summary>
internal static partial class HistoryQualification
{
    /// <summary>Checks only the newly added private index's portable boundary on the retained accepted fixture.</summary>
    internal static async Task ExportAsync(string root, Action<string> proof, CancellationToken ct, bool compact = false)
    {
        var done = Path.Combine(root, compact ? "history-compact-export.complete.json" : "history-export.complete.json");
        if (File.Exists(done)) throw new IOException("Never replay a completed export-boundary qualification.");
        using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "client"), new("https://localhost:55555", new('a', 64)), false, ct);
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var before = await DigestAsync(factory, ct);
        await using (var db = await factory.CreateDbContextAsync(ct))
            if (await db.Set<HouseholdLocalFinancialReference>().CountAsync(ct) < 12_000) throw new InvalidDataException();
        using var output = new MemoryStream();
        await client.Services.GetServices<IBackupSource>().Single(source => source.Name == "database.sqlite").WriteAsync(output, ct);
        if (compact && output.Length >= 8 * 1024 * 1024) throw new InvalidDataException("Private ciphertext history entered the portable allocation.");
        output.Position = 0;
        await using var portable = await SqliteMemorySnapshot.OpenAsync(output, SqliteDatabaseBackupSource<ZananceDbContext>.MaximumProtectedSnapshotBytes, ct);
        var names = new List<string>();
        await using (var command = portable.Connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name LIKE 'HouseholdLocal%' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0));
        }
        if (!names.ToHashSet(StringComparer.Ordinal).SetEquals(HouseholdLocalModel.PortableTables)) throw new InvalidDataException();
        foreach (var name in names)
        {
            if (!name.All(char.IsAsciiLetterOrDigit)) throw new InvalidDataException();
            await using var command = portable.Connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM \"" + name + "\"";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct)) != 0) throw new InvalidDataException();
        }
        if (before != await DigestAsync(factory, ct) || before != await DigestConnectionAsync(portable.Connection, ct)) throw new InvalidDataException();
        await File.WriteAllTextAsync(done, JsonSerializer.Serialize(new { Tables = names.Count, PortableBytes = output.Length, PrivateRowsInPortableCopy = 0, OriginalFinancialHash = before }), ct);
        proof(compact ? "private-ciphertext-history-never-enters-bounded-portable-copy-all-original-financial-columns-remain-identical"
            : "all-current-private-household-tables-including-derived-financial-positions-scrubbed-only-from-portable-memory-copy-original-finance-unchanged");
    }
    /// <summary>Actual SQLCipher paging, old baselines, framed large rows, corruption refusal and cold same-key reopening.</summary>
    internal static async Task RunAsync(string root, bool cold, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original bounded household history invariant failed."); }
        var resultPath = Path.Combine(root, cold ? "history-cold.complete.json" : "history.complete.json");
        if (File.Exists(resultPath)) throw new IOException("Never replay a completed history qualification.");
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "client"), peer, false, ct);
        client.Services.MigrateLocalDatabase<ZananceDbContext>();
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        HouseholdJournalAuthority authority;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var binding = await db.Set<HouseholdLocalBinding>().AsNoTracking().SingleAsync(ct);
            Require(binding.Cursor >= 8 && binding.FinancialCursor > 12_000);
            authority = new(binding.GenerationPath, binding.SetupOperation, binding.PeerKey, binding.IdentityId);
        }
        var original = await DigestAsync(factory, ct); var journal = new HouseholdJournal(factory);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using var replica = await journal.ReadReplicaAsync(authority, ct);
        Require(replica.ResidentHistoricalRows == 0);
        var current = replica.Snapshot(); var last = new Dictionary<SyncRecordKey, (long Version, string Hash)>();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long cursor = 0; var pages = 0; var largest = 0; SyncProposal? old = null;
        while (cursor < replica.FinancialCursor)
        {
            var page = await replica.FinancialFeedAsync(cursor, replica.FinancialCursor, ct);
            Require(page.Records.Count is >= 1 and <= 128 && page.NextCursor > cursor);
            long pageBytes = 0;
            foreach (var row in page.Records)
            {
                Require(row.Version == ++cursor);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(row, PersonalSyncJsonContext.Default.SyncRecord);
                try
                {
                    pageBytes += bytes.Length; largest = Math.Max(largest, bytes.Length); hash.AppendData(bytes);
                    last[row.Record] = (row.Version, Convert.ToHexStringLower(SHA256.HashData(bytes)));
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                if (old is null && current[row.Record].Version > row.Version) old = new(row.Record, row.Version, row.Values);
            }
            Require(page.Records.Count == 1 || pageBytes <= 2 * 1024 * 1024);
            Require(page.NextCursor == cursor && page.Complete == (cursor == replica.FinancialCursor)); pages++;
            Require(replica.ResidentHistoricalRows == 0);
        }
        Require(last.Count == current.Count && largest > HouseholdFinancialFraming.BlockBytes && old is not null);
        foreach (var row in current.Values)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(row, PersonalSyncJsonContext.Default.SyncRecord);
            try { Require(last[row.Record] == (row.Version, Convert.ToHexStringLower(SHA256.HashData(bytes)))); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        var baselines = await replica.ReadBaselinesAsync([old!], ct);
        Require(baselines.Count == 1 && baselines[old!.Record].Version == old.BaseVersion && JsonElement.DeepEquals(baselines[old.Record].Values!.Value, old.Values!.Value));
        if (!cold)
        {
            using var keys = await journal.ReadKeysAsync(authority, ct);
            var membership = await journal.ReadCurrentMembershipAsync(authority, ct);
            using var epoch = await journal.ReadEpochAsync(authority, membership.Epoch, ct);
            var local = JsonNode.Parse(old!.Values!.Value.GetRawText())!.AsObject();
            local["Name"] = "Fictitious independently conflicting historical edit";
            var candidate = await replica.PrepareAsync(membership, epoch,
                (await journal.CaptureAsync(peer, authority.IdentityId, ct))!.DeviceId, keys.Device, Guid.NewGuid(), Guid.NewGuid(),
                [new(old.Record, old.BaseVersion, JsonSerializer.SerializeToElement(local))], [], new Dictionary<string, byte[]>(), new(TimeProvider.System), ct);
            Require(!candidate.Plan.Succeeded && candidate.Plan.Conflicts.Count != 0 && candidate.Parts.Count == 0);
            proof("original-three-way-planner-refuses-divergent-old-baseline-without-sealing-ciphertext-or-writing-financial-data");
        }
        var historyHash = Convert.ToHexStringLower(hash.GetHashAndReset());
        proof("bounded-native-original-history-pages-" + pages + "-rows-" + cursor + "-largest-json-bytes-" + largest + "-resident-old-bodies-0");
        proof("old-original-three-way-baseline-and-complete-current-graph-match-authenticated-history-without-network-or-new-publication");
        if (!cold)
        {
            HouseholdLocalFinancialReference saved;
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                saved = await db.Set<HouseholdLocalFinancialReference>().AsNoTracking().Where(row => row.Offset >= 4).OrderByDescending(row => row.Length).FirstAsync(ct);
                Require(saved.Length > HouseholdFinancialFraming.BlockBytes);
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE HouseholdLocalFinancialReferences SET Offset = {saved.Offset + 1} WHERE Version = {saved.Version} AND Offset = {saved.Offset}", ct);
            }
            try
            {
                var refused = false;
                try { await replica.ReadBaselinesAsync([new(new(saved.Table, saved.Key), saved.Version, null)], ct); }
                catch (CryptographicException) { refused = true; }
                Require(refused);
                refused = false;
                try { using var unexpected = await journal.ReadReplicaAsync(authority, ct); }
                catch (CryptographicException) { refused = true; }
                Require(refused);
            }
            finally
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                Require(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE HouseholdLocalFinancialReferences SET Offset = {saved.Offset} WHERE Version = {saved.Version} AND Offset = {saved.Offset + 1}", ct) == 1);
            }
            proof("altered-framed-history-position-refused-before-baseline-return-and-complete-replay-retains-original-index-disagreement");
            await using (var db = await factory.CreateDbContextAsync(ct))
                Require(await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM HouseholdLocalFinancialReferences WHERE Version = {saved.Version} AND Offset = {saved.Offset}", ct) == 1);
            using var rebuilt = await journal.ReadReplicaAsync(authority, ct);
            Require(rebuilt.ResidentHistoricalRows == 0 && rebuilt.Cursor == replica.Cursor && rebuilt.PublicationHead == replica.PublicationHead);
            var restored = await rebuilt.ReadBaselinesAsync([new(new(saved.Table, saved.Key), saved.Version, null)], ct);
            Require(restored.Count == 1 && restored.Values.Single().Version == saved.Version);
            proof("missing-derived-row-position-rebuilt-only-from-completely-verified-original-ciphertext-without-changing-keys-or-financial-data");
        }
        Require(await DigestAsync(factory, ct) == original);
        timer.Stop();
        var result = new { Cold = cold, Cursor = replica.Cursor, FinancialCursor = cursor, Pages = pages, LargestRowBytes = largest,
            ResidentHistoricalRows = replica.ResidentHistoricalRows, HistoryHash = historyHash, FinancialHash = original, ElapsedMilliseconds = timer.ElapsedMilliseconds,
            NoNetworkRequests = true, OriginalKeysAndCiphertextRetained = true };
        if (cold)
        {
            using var warm = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "history.complete.json"), ct));
            Require(warm.RootElement.GetProperty("HistoryHash").GetString() == historyHash && warm.RootElement.GetProperty("FinancialHash").GetString() == original);
        }
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(result), ct);
        proof(cold ? "cold-same-key-native-history-and-complete-financial-digests-unchanged" : "original-financial-data-and-ciphertext-history-preserved-after-bounded-index-upgrade");
    }

    /// <summary>Qualification-only full financial comparison; original schema names alone form SQL, without logging row contents.</summary>
    private static async Task<string> DigestAsync(IDbContextFactory<ZananceDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); await db.Database.OpenConnectionAsync(ct);
        return await DigestConnectionAsync(db.Database.GetDbConnection(), ct);
    }
    /// <summary>Compares every financial column/storage value in the source and the actual compact portable memory image.</summary>
    private static async Task<string> DigestConnectionAsync(System.Data.Common.DbConnection connection, CancellationToken ct)
    {
        using var command = connection.CreateCommand(); var tables = new SortedDictionary<string, List<string>>();
        foreach (var table in HouseholdFinancialSchema.Manifest.Tables)
        {
            static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
            command.CommandText = "SELECT " + string.Join(",", table.Columns.Select(column => Quote(column.Name))) + " FROM " + Quote(table.Name);
            using var reader = await command.ExecuteReaderAsync(ct); var rows = new List<string>();
            while (await reader.ReadAsync(ct)) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
            rows.Sort(StringComparer.Ordinal); tables[table.Name] = rows;
        }
        var bytes = JsonSerializer.SerializeToUtf8Bytes(tables);
        try { return Convert.ToHexStringLower(SHA256.HashData(bytes)); } finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}
