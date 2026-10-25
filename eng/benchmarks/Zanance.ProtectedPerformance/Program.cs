using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

if (!OperatingSystem.IsWindows() || args.Length is < 1 or > 2 || (args.Length == 2 && args[1] is not ("resume" or "ledger" or "home" or "home-stages" or "ledger-stages" or "verify-retained" or "tenfold")))
    throw new ArgumentException("Provide a new Windows fictitious output directory, or explicit resume/ledger/home/home-stages/ledger-stages/verify-retained/tenfold mode.");
var requested = Path.GetFullPath(args[0]);
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts"));
if (!requested.StartsWith(allowed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new IOException("Independent performance output must be inside repository artifacts.");
var homeOnly = args.Length == 2 && args[1] == "home";
var homeStagesOnly = args.Length == 2 && args[1] == "home-stages";
var ledgerStagesOnly = args.Length == 2 && args[1] == "ledger-stages";
var verificationOnly = args.Length == 2 && args[1] == "verify-retained";
var newTenfoldOnly = args.Length == 2 && args[1] == "tenfold";
var resume = homeOnly || homeStagesOnly || ledgerStagesOnly || verificationOnly || newTenfoldOnly || (args.Length == 2 && args[1] == "resume");
var ledgerOnly = args.Length == 2 && args[1] == "ledger";
var root = resume ? requested : FixtureDirectory.Create(requested);
if (!File.Exists(Path.Combine(root, "fictitious-performance.marker"))) throw new IOException("Independent root marker missing.");
for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
    if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected workload roots are refused.");
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10)); var ct = deadline.Token;
var results = new List<object>();
var output = Path.Combine(root, ledgerStagesOnly ? "ledger-stages-" + Guid.NewGuid().ToString("N") + ".json"
    : homeStagesOnly ? "home-stages-" + Guid.NewGuid().ToString("N") + ".json"
    : homeOnly ? "home-measurements-" + Guid.NewGuid().ToString("N") + ".json"
    : verificationOnly ? "readonly-verification-" + Guid.NewGuid().ToString("N") + ".json"
    : newTenfoldOnly ? "tenfold-measurements.json" : "measurements.json");
foreach (var shape in new[] { Workload.Reference, Workload.Tenfold })
{
    if (newTenfoldOnly && shape == Workload.Reference) continue;
    var folder = Path.Combine(root, shape == Workload.Reference ? "reference" : "tenfold");
    var existing = resume && Directory.Exists(folder);
    if (newTenfoldOnly && existing) throw new IOException("Tenfold-only continuation refuses already prepared data; never repeat its accepted writes.");
    if (verificationOnly && !existing) continue; // Never seed the still-unexecuted next shape to repair an observation.
    if ((homeOnly || homeStagesOnly || ledgerStagesOnly) && !existing) throw new IOException("Home comparison requires both retained protected fixtures; never regenerate them.");
    if (!existing) _ = await PerformanceFixture.CreateAsync(folder, shape, ct);
    var original = Path.Combine(folder, "fixture.db");
    using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
    var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
    void Validate()
    {
        if (!File.Exists(Path.Combine(folder, "fictitious-performance.marker"))) throw new IOException("Independent workload marker missing.");
    }
    var keyFolder = Path.Combine(folder, "keys");
    if (ledgerStagesOnly && (!Directory.Exists(keyFolder) || File.Exists(original)))
        throw new IOException("Ledger stages require the existing protected source and keys; no preparation is allowed.");
    if (!ledgerStagesOnly) Directory.CreateDirectory(keyFolder);
    var keys = new WindowsGenerationKeyStore(keyFolder, Validate);
    var durability = new WindowsGenerationFileDurability(folder);
    var identityPath = Path.Combine(folder, "identities.sqlite");
    SqlCipherProfileGeneration generation;
    double migrationMs;
    using (var owner = await location.Access.AcquireExclusiveAsync(ct))
    {
        var clock = Stopwatch.StartNew();
        if (!ledgerStagesOnly)
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath,
                new Dictionary<string, string> { [""] = original }, !existing, keys, durability,
                (_, _) => throw new IOException("No restore intent belongs to the fresh workload."), Validate,
                256 * 1024 * 1024, "4.19.0", ct);
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate);
        var identity = (await identities.ReadAsync(ct)).Single();
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, Validate);
        if (ledgerStagesOnly)
        {
            // The ordinary benchmark can resume preparation. This diagnostic must refuse it before
            // opening the original generation rather than silently finishing an interrupted migration.
            var ancestry = await history.ReadAsync(ct);
            var checkpoint = await new SqliteDatabaseGenerationJournal(original + ".generation.sqlite",
                identity.InitialMigration).ReadAsync(ct);
            if (ancestry.Count != 1 || ancestry[0].Sequence != 0 || !ancestry[0].Selected
                || checkpoint.Phase != DatabaseGenerationPhase.Completed)
                throw new IOException("Ledger stages require completed, already selected original ancestry.");
        }
        generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
            _ => throw new IOException("No restored generation belongs to the initial workload."), keys, durability,
            owner, Validate, 256 * 1024 * 1024, "4.19.0", ct);
        generation.Publish(location, owner); clock.Stop(); migrationMs = clock.Elapsed.TotalMilliseconds;
    }
    using (generation)
    {
        if (!existing)
        {
            var originalKey = await keys.ReadAsync(generation.Binding, ct);
            try
            {
                await using var expected = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                    Password = Convert.ToBase64String(originalKey) }.ToString());
                await expected.OpenAsync(ct);
                await using var current = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                await current.Database.OpenConnectionAsync(ct);
                // Verify every original raw schema/header/typed row before the measured writes can legitimately
                // advance device-only capture history. The immutable pre-migration rollback stays unchanged.
                await SqliteSnapshotVerifier.VerifyAsync(expected, (SqliteConnection)current.Database.GetDbConnection(), ct);
            }
            finally { CryptographicOperations.ZeroMemory(originalKey); }
        }
        using var beforeBytes = new MemoryStream();
        if (verificationOnly)
        {
            // Recover the exact original portable projection from the immutable encrypted rollback.
            // The actual backup policy/factory remains in charge; no financial write or key preparation repeats.
            var rollback = generation.Path + ".rollback.db";
            if (!File.Exists(rollback)) throw new IOException("Retained verification requires its original encrypted rollback.");
            foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
                if (File.Exists(rollback + suffix)) throw new IOException("Retained rollback must be an immutable closed image.");
            var baselinePath = Path.Combine(folder, "comparison-" + Guid.NewGuid().ToString("N") + ".db");
            byte[] originalDigest;
            using (var originalFile = File.OpenRead(rollback)) originalDigest = SHA256.HashData(originalFile);
            var originalKey = await keys.ReadAsync(generation.Binding, ct);
            try
            {
                File.Copy(rollback, baselinePath, overwrite: false);
                // The original factory intentionally requires ReadWrite. Give it only a fresh encrypted
                // copy, retaining the original rollback byte-for-byte instead of weakening that guard.
                using var baseline = new ServiceCollection().AddZananceData(baselinePath, protectionRequired: true).BuildServiceProvider();
                var baselineLocation = baseline.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                using (var owner = await baselineLocation.Access.AcquireExclusiveAsync(ct))
                    baselineLocation.MoveToProtected(baselinePath, () => new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = baselinePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false,
                        Password = Convert.ToBase64String(originalKey) }.ToString()), "4.19.0", owner);
                await baseline.GetRequiredService<IBackupSource>().WriteAsync(beforeBytes, ct);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(originalKey);
                foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" }) File.Delete(baselinePath + suffix);
            }
            using var retainedOriginal = File.OpenRead(rollback);
            if (!CryptographicOperations.FixedTimeEquals(originalDigest, SHA256.HashData(retainedOriginal)))
                throw new IOException("Retained rollback bytes changed during verification.");
        }
        else await services.GetRequiredService<IBackupSource>().WriteAsync(beforeBytes, ct);
        beforeBytes.Position = 0;
        await using var beforePortable = await SqliteMemorySnapshot.OpenAsync(beforeBytes, 256 * 1024 * 1024, ct);
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var metrics = new Dictionary<string, List<double>>();
        async Task Measure(string name, Func<Task> operation)
        {
            var times = new List<double>();
            for (var sample = 0; sample < 4; sample++)
            { var clock = Stopwatch.StartNew(); await operation(); clock.Stop(); times.Add(clock.Elapsed.TotalMilliseconds); }
            metrics.Add(name, times);
        }
        var searchCounts = new Dictionary<string, int>();
        var savedTimes = new List<double>();
        object? homeStages = null;
        if (verificationOnly) { /* Original accepted writes/timers are deliberately not replayed. */ }
        else if (ledgerStagesOnly)
            homeStages = await LedgerStageDiagnostics.MeasureAsync(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
                shape.Entries, PerformanceFixture.Today, ct);
        else if (homeStagesOnly)
            homeStages = await HomeReadStages.MeasureAsync(store, shape.Entries, PerformanceFixture.Today, ct);
        else if (homeOnly)
        {
            var goals = services.GetRequiredService<GoalStore>(); var holdings = services.GetRequiredService<HoldingStore>();
            string? originalPacket = null;
            await Measure("OriginalHomeReadSequence", async () =>
            {
                var packet = await HomeReadWorkload.ReadOriginalAsync(store, plans, goals, holdings, PerformanceFixture.Today, ct);
                var captured = HomeReadWorkload.Capture(packet);
                if (originalPacket is not null && originalPacket != captured) throw new IOException("Original Home fixture changed between samples.");
                originalPacket = captured;
            });
            await Measure("CompleteHomeReadAndValidate", async () =>
            {
                var packet = await store.GetHomeSnapshotAsync(PerformanceFixture.Today, 0, ct);
                await store.ValidateHomeSnapshotAsync(packet, ct);
                if (packet.Entries.Count != shape.Entries || originalPacket != HomeReadWorkload.Capture(packet))
                    throw new IOException("Complete Home financial or work packets differ from their original operations.");
            });
        }
        else
        {
            if (!ledgerOnly)
            {
                await Measure("ReadAllEntries", async () =>
                { if ((await store.GetEntriesAsync(cancellationToken: ct)).Count != shape.Entries) throw new IOException("Protected workload entry count changed."); });
                await Measure("CompletePlanReadAndValidate", async () =>
                {
                    var packet = await plans.GetWorkSnapshotAsync(ct);
                    if (packet.Schedules.Count != shape.Schedules) throw new IOException("Protected schedule count changed.");
                    await plans.ValidateWorkSnapshotAsync(packet, ct);
                });
                await Measure("QuickPacketReadAndValidate", async () =>
                {
                    var packet = await store.GetQuickResourceReviewAsync(cancellationToken: ct);
                    if (packet.Accounts.Count != shape.Accounts) throw new IOException("Protected account count changed.");
                    await store.ValidateQuickResourceReviewAsync(packet, ct);
                });
            }
            if (!ledgerOnly) await Measure("CompleteHomeReadAndValidate", async () =>
            {
                var packet = await store.GetHomeSnapshotAsync(PerformanceFixture.Today, 0, ct);
                await store.ValidateHomeSnapshotAsync(packet, ct);
                if (packet.Entries.Count != shape.Entries || packet.QuickResources.Accounts.Count != shape.Accounts)
                    throw new IOException("Complete protected Home inputs lost original rows.");
            });
            var entries = await store.GetEntriesAsync(cancellationToken: ct); var accounts = await store.GetAccountsAsync(cancellationToken: ct);
            if (!ledgerOnly) await Measure("OriginalBalancesAndPeriodTotals", () =>
            {
                _ = LedgerCalculator.TotalBalances(accounts, entries, PerformanceFixture.Today);
                _ = LedgerCalculator.Totals(accounts, entries, new LedgerFilter(new(2026, 10, 1), PerformanceFixture.Today));
                return Task.CompletedTask;
            });
            var byId = accounts.ToDictionary(account => account.Id);
            foreach (var query in new[] { "QA06 item 000012", "not-present", "12.50", "QA06 item" })
            {
                await Measure("SearchAndGroup:" + query, () =>
                {
                    // Execute the same complete Core filtering/grouping path as Transactions, without a result cap.
                    var matching = EntrySearch.Apply(entries, new EntryFilter(Text: query), _ => "Fictitious category", byId).ToList();
                    var groups = EntrySearch.ByDay(matching, byId);
                    if (groups.Sum(group => group.Entries.Count) != matching.Count
                        || matching.Select(entry => entry.Id).Distinct().Count() != matching.Count
                        || (query == "QA06 item 000012" && (matching.Count != 1 || matching[0].Id != PerformanceFixture.Id(2, 12)
                            || matching[0].Kind != EntryKind.Transfer))
                        || (query == "not-present" && matching.Count != 0)
                        || (query == "QA06 item" && matching.Count != shape.Entries))
                        throw new IOException("Protected search results differ from the complete known fixture.");
                    searchCounts[query] = matching.Count;
                    return Task.CompletedTask;
                });
            }
            for (var sample = 0; sample < 4; sample++)
            {
                var entry = new LedgerEntry { AccountId = accounts[0].Id, Amount = 1234,
                    Date = PerformanceFixture.Today, Kind = EntryKind.Expense, Title = "QA06 protected save" };
                // Time only the real validated transaction. Read-back and exact cleanup are outside this boundary.
                var clock = Stopwatch.StartNew();
                var saved = await store.SaveEntryAsync(entry, ct);
                clock.Stop();
                if (!saved.Succeeded) throw new IOException("Protected validated entry save failed.");
                var actual = await store.GetEntryAsync(entry.Id, ct);
                if (actual is null || actual.AccountId != entry.AccountId || actual.Amount != 1234
                    || actual.Date != entry.Date || actual.Kind != EntryKind.Expense || actual.Title != entry.Title)
                    throw new IOException("Protected saved row read-back differs.");
                var removed = await store.DeleteEntryAsync(entry.Id, ct);
                if (removed.Count != 1 || removed[0].Id != entry.Id || await store.GetEntryAsync(entry.Id, ct) is not null)
                    throw new IOException("Protected saved-row cleanup did not remove exactly its own row.");
                savedTimes.Add(clock.Elapsed.TotalMilliseconds);
            }
            metrics.Add("SaveValidatedEntry", savedTimes);
        }

        using var afterBytes = new MemoryStream(); await services.GetRequiredService<IBackupSource>().WriteAsync(afterBytes, ct);
        afterBytes.Position = 0;
        await using var portable = await SqliteMemorySnapshot.OpenAsync(afterBytes, 256 * 1024 * 1024, ct);
        // Portable exports intentionally omit private sync/household rows and their sequence metadata.
        // Compare the complete original portable projection, not that projection against a raw private rollback.
        var expectedRevisionChange = verificationOnly || savedTimes.Count != 0;
        await WorkloadSnapshotVerifier.VerifyAsync(beforePortable.Connection, portable.Connection,
            acceptedWrites: expectedRevisionChange, ct);
        results.Add(new { Shape = shape, ExistingFixtureResumed = existing, ProtectedPreparationMs = migrationMs,
            FirstThenThreeWarmMilliseconds = metrics, CompleteMigrationSchemaAndTypedRowsUnchanged = !existing,
            CompletePortableSchemaAndTypedRowsUnchanged = !expectedRevisionChange,
            CompletePortableFinancialSchemaAndTypedRowsUnchanged = true,
            OnlyValidatedOpaqueRevisionNormalized = expectedRevisionChange,
            HomeStages = homeStages,
            SearchResultCounts = searchCounts, SavedRowsReadBackAndRemoved = savedTimes.Count,
            PlaintextOriginalRemoved = !File.Exists(original), CipherBytes = new FileInfo(generation.Path).Length });
    }
    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
    { Results = results, LedgerOnly = ledgerOnly, HomeOnly = homeOnly, HomeStagesOnly = homeStagesOnly, LedgerStagesOnly = ledgerStagesOnly, VerificationOnly = verificationOnly, NewTenfoldOnly = newTenfoldOnly,
        CipherVersion = "4.19.0", ActualProductionAssemblies = true,
        NativeUiOrDeviceAcceptance = false }, new JsonSerializerOptions { WriteIndented = true }), ct);
}
if (results.Count == 0) throw new IOException("No retained workload was verified.");
Console.WriteLine($"Independent protected verification completed for {results.Count} workload(s), verification-only={verificationOnly}: " + output);
