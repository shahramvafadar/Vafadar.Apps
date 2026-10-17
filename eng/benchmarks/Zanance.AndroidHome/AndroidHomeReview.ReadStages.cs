using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Locates actual protected read/conversion costs without changing production algorithms or stored inputs.</summary>
    private static async Task<object> ReviewReadStagesAsync(string root, string comparison, string baseline, CancellationToken ct)
    {
        if (File.Exists(Path.Combine(root, "read-profile-" + comparison + "-result.json")))
            throw new ReviewCheckException("Completed read profile must not be repeated.");
        return await Task.Run(async () =>
        {
            var results = new List<object>();
            foreach (var name in new[] { "reference", "tenfold" })
            {
                var folder = Path.Combine(root, name); RequireRoot(folder);
                var original = Path.Combine(folder, "fixture.db"); var identitiesPath = Path.Combine(folder, "identities.sqlite");
                if (File.Exists(original) || !File.Exists(identitiesPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                    throw new ReviewCheckException("Original ciphertext and keys required; do not prepare replacement data.");
                using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
                var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys")); SqlCipherProfileGeneration generation;
                using (var owner = await location.Access.AcquireExclusiveAsync(ct))
                {
                    var identities = new SqliteDatabaseIdentityCatalogue(identitiesPath, () => RequireRoot(folder));
                    var identity = (await identities.ReadAsync(ct)).Single();
                    var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                    generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                        _ => throw new ReviewCheckException("No restored workload expected."), keys, new AndroidGenerationFileDurability(folder),
                        owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                    generation.Publish(location, owner);
                }
                using (generation)
                {
                    await using var before = await PortableAsync(services);
                    var store = services.GetRequiredService<ZananceStore>(); var watch = Stopwatch.StartNew();
                    var entries = await store.GetEntriesAsync(cancellationToken: ct); watch.Stop(); var ledgerMs = watch.Elapsed.TotalMilliseconds;
                    var digest = Digest(entries); var count = entries.Count; entries.Clear();
                    watch.Restart(); var home = await store.GetHomeSnapshotAsync(Vafadar.Zanance.Performance.PerformanceFixture.Today, 0, ct);
                    watch.Stop(); var homeMs = watch.Elapsed.TotalMilliseconds;
                    if (home.Entries.Count != count || digest != Digest(home.Entries))
                        throw new ReviewCheckException("Complete original public read packets differ.");
                    double? genericMs = null, directMs = null, rawValuesMs = null; long? fields = null; string? converterType = null;
                    if (comparison == baseline)
                    {
                        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                        await db.Database.OpenConnectionAsync(ct);
                        var converter = db.Model.FindEntityType(typeof(LedgerEntry))!.FindProperty(nameof(LedgerEntry.CreatedAt))!.GetTypeMapping().Converter
                            ?? throw new ReviewCheckException("Actual model timestamp converter missing.");
                        converterType = converter.GetType().FullName;
                        var stamp = new DateTimeOffset(2026, 10, 9, 0, 0, 0, TimeSpan.Zero).UtcTicks;
                        long generic = 0, direct = 0; const int conversions = 200000;
                        watch.Restart();
                        for (var i = 0; i < conversions; i++)
                        {
                            if ((i & 1023) == 0) ct.ThrowIfCancellationRequested();
                            generic ^= ((DateTimeOffset)converter.ConvertFromProvider(stamp + i)!).UtcTicks;
                        }
                        watch.Stop(); genericMs = watch.Elapsed.TotalMilliseconds; watch.Restart();
                        // Diagnostic BCL comparison only; no alternate timestamp conversion is installed in the application.
                        for (var i = 0; i < conversions; i++)
                        {
                            if ((i & 1023) == 0) ct.ThrowIfCancellationRequested();
                            direct ^= new DateTimeOffset(stamp + i, TimeSpan.Zero).UtcTicks;
                        }
                        watch.Stop(); directMs = watch.Elapsed.TotalMilliseconds;
                        if (generic != direct) throw new ReviewCheckException("Timestamp conversion values differ.");
                        await using var command = db.Database.GetDbConnection().CreateCommand();
                        command.CommandText = "SELECT * FROM Entries NOT INDEXED ORDER BY rowid DESC";
                        var rows = 0; long observedFields = 0;
                        await using (var reader = await command.ExecuteReaderAsync(ct))
                        {
                            var values = new object[reader.FieldCount]; watch.Restart();
                            while (await reader.ReadAsync(ct)) { observedFields += reader.GetValues(values); rows++; }
                            watch.Stop();
                        }
                        rawValuesMs = watch.Elapsed.TotalMilliseconds;
                        fields = observedFields;
                        if (rows != count || count != (name == "reference" ? 10000 : 100000))
                            throw new ReviewCheckException("Complete read stage lost original rows.");
                    }
                    await using var after = await PortableAsync(services);
                    await SqliteSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, ct);
                    results.Add(new { Shape = name, Entries = count, CompleteOrderedEntryDigest = digest,
                        PublicGetEntriesMs = ledgerMs, PublicHomeSnapshotMs = homeMs, RawClrFieldReadMs = rawValuesMs,
                        RawFieldsRead = fields, TimestampConverter = converterType,
                        TimestampConversions = comparison == baseline ? 200000 : 0, OriginalBoxedTimestampMs = genericMs, BclTimestampMs = directMs,
                        TimestampValuesMatched = comparison == baseline ? true : (bool?)null, CompletePortableSchemaAndTypedRowsUnchanged = true });
                }
            }
            return (object)new { Comparison = comparison, BaselineComparison = baseline, Device = DeviceEvidence(), Results = results,
                DataModuleVersionId = typeof(ZananceStore).Module.ModuleVersionId, CallerModuleVersionId = typeof(AndroidHomeReview).Module.ModuleVersionId,
                NoFinancialSave = true, MainPackageTouched = false,
                Boundary = "Actual public protected reads; native raw CLR fields exclude key opening. Timestamp loop is diagnostic, not whole-app speed." };
        }, ct);

        async Task<SqliteMemorySnapshot> PortableAsync(IServiceProvider services)
        {
            using var bytes = new MemoryStream(); await services.GetRequiredService<IBackupSource>().WriteAsync(bytes, ct); bytes.Position = 0;
            return await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
        }
        static string Digest(List<LedgerEntry> entries)
        {
            using var hash = SHA256.Create();
            using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
            JsonSerializer.Serialize(output, entries); output.FlushFinalBlock();
            return Convert.ToHexStringLower(hash.Hash!);
        }
    }
}
