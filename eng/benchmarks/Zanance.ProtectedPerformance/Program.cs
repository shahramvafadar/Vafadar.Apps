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

if (!OperatingSystem.IsWindows() || args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "resume"))
    throw new ArgumentException("One new Windows fictitious output directory, or its explicit resume, required.");
var requested = Path.GetFullPath(args[0]);
var allowed = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts"));
if (!requested.StartsWith(allowed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new IOException("Independent performance output must be inside repository artifacts.");
var resume = args.Length == 2;
var root = resume ? requested : FixtureDirectory.Create(requested);
if (!File.Exists(Path.Combine(root, "fictitious-performance.marker"))) throw new IOException("Independent root marker missing.");
for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
    if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected workload roots are refused.");
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10)); var ct = deadline.Token;
var results = new List<object>();
foreach (var shape in new[] { Workload.Reference, Workload.Tenfold })
{
    var folder = Path.Combine(root, shape == Workload.Reference ? "reference" : "tenfold");
    var existing = resume && Directory.Exists(folder);
    if (!existing) _ = await PerformanceFixture.CreateAsync(folder, shape, ct);
    var original = Path.Combine(folder, "fixture.db");
    using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
    var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
    void Validate()
    {
        if (!File.Exists(Path.Combine(folder, "fictitious-performance.marker"))) throw new IOException("Independent workload marker missing.");
    }
    var keyFolder = Path.Combine(folder, "keys"); Directory.CreateDirectory(keyFolder);
    var keys = new WindowsGenerationKeyStore(keyFolder, Validate);
    var durability = new WindowsGenerationFileDurability(folder);
    var identityPath = Path.Combine(folder, "identities.sqlite");
    SqlCipherProfileGeneration generation;
    double migrationMs;
    using (var owner = await location.Access.AcquireExclusiveAsync(ct))
    {
        var clock = Stopwatch.StartNew();
        await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath,
            new Dictionary<string, string> { [""] = original }, !existing, keys, durability,
            (_, _) => throw new IOException("No restore intent belongs to the fresh workload."), Validate,
            256 * 1024 * 1024, "4.19.0", ct);
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate);
        var identity = (await identities.ReadAsync(ct)).Single();
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, Validate);
        generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
            _ => throw new IOException("No restored generation belongs to the initial workload."), keys, durability,
            owner, Validate, 256 * 1024 * 1024, "4.19.0", ct);
        generation.Publish(location, owner); clock.Stop(); migrationMs = clock.Elapsed.TotalMilliseconds;
    }
    using (generation)
    {
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var metrics = new Dictionary<string, List<double>>();
        async Task Measure(string name, Func<Task> operation)
        {
            var times = new List<double>();
            for (var sample = 0; sample < 4; sample++)
            { var clock = Stopwatch.StartNew(); await operation(); clock.Stop(); times.Add(clock.Elapsed.TotalMilliseconds); }
            metrics.Add(name, times);
        }
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
        var entries = await store.GetEntriesAsync(cancellationToken: ct); var accounts = await store.GetAccountsAsync(cancellationToken: ct);
        await Measure("OriginalBalancesAndPeriodTotals", () =>
        {
            _ = LedgerCalculator.TotalBalances(accounts, entries, PerformanceFixture.Today);
            _ = LedgerCalculator.Totals(accounts, entries, new LedgerFilter(new(2026, 10, 1), PerformanceFixture.Today));
            return Task.CompletedTask;
        });
        using var afterBytes = new MemoryStream(); await services.GetRequiredService<IBackupSource>().WriteAsync(afterBytes, ct);
        afterBytes.Position = 0;
        await using var portable = await SqliteMemorySnapshot.OpenAsync(afterBytes, 256 * 1024 * 1024, ct);
        var retainedKey = await keys.ReadAsync(generation.Binding, ct);
        try
        {
            await using var expected = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                Password = Convert.ToBase64String(retainedKey) }.ToString());
            await expected.OpenAsync(ct);
            // The authenticated pre-migration rollback preserves the actual complete original packet.
            // Page layout differs in portable exports; compare every original schema/header/typed value instead.
            await SqliteSnapshotVerifier.VerifyAsync(expected, portable.Connection, ct);
        }
        finally { CryptographicOperations.ZeroMemory(retainedKey); }
        results.Add(new { Shape = shape, ExistingFixtureResumed = existing, ProtectedPreparationMs = migrationMs,
            FirstThenThreeWarmMilliseconds = metrics, CompleteSchemaAndTypedRowsUnchanged = true,
            PlaintextOriginalRemoved = !File.Exists(original), CipherBytes = new FileInfo(generation.Path).Length });
    }
    await File.WriteAllTextAsync(Path.Combine(root, "measurements.json"), JsonSerializer.Serialize(new
    { Results = results, CipherVersion = "4.19.0", ActualProductionAssemblies = true,
        NativeUiOrDeviceAcceptance = false }, new JsonSerializerOptions { WriteIndented = true }), ct);
}
Console.WriteLine("Independent protected reference and tenfold workloads completed with unchanged financial data.");
