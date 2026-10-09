using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

// This executable is independent of the application. It only creates new fictitious directories.
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
var ct = deadline.Token;
var json = new JsonSerializerOptions { WriteIndented = true };
if (args is ["worker", var workerFolder, var scaleText])
{
    var workload = scaleText == "reference" ? Workload.Reference : scaleText == "tenfold" ? Workload.Tenfold : throw new ArgumentException("Unknown workload.");
    var migration = await PerformanceFixture.CreateAsync(workerFolder, workload, ct);
    SqliteConnection.ClearAllPools();
    await File.WriteAllTextAsync(Path.Combine(workerFolder, "migration.json"), JsonSerializer.Serialize(migration, json), ct);
    return;
}
if (args is not [var output]) { throw new ArgumentException("Usage: dotnet run --project eng/benchmarks/Zanance.Performance -c Release -- <new-output-directory>"); }
var root = FixtureDirectory.Create(output);
var results = new List<object>();
foreach (var scale in new[] { "reference", "tenfold" })
{
    var workload = scale == "reference" ? Workload.Reference : Workload.Tenfold;
    var migrations = new List<MigrationProof>();
    for (var i = 0; i < 3; i++)
    {
        var folder = Path.Combine(root, $"{scale}-{i}");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("worker"); start.ArgumentList.Add(folder); start.ArgumentList.Add(scale);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start independent migration worker.");
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0) { throw new InvalidOperationException("Migration worker failed."); }
        migrations.Add(JsonSerializer.Deserialize<MigrationProof>(await File.ReadAllTextAsync(Path.Combine(folder, "migration.json"), ct))!);
    }
    var path = Path.Combine(root, $"{scale}-0", "fixture.db");
    await using var services = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
    var store = services.GetRequiredService<ZananceStore>();
    var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
    var firstRead = Stopwatch.StartNew();
    services.MigrateLocalDatabase<ZananceDbContext>();
    var entries = await store.GetEntriesAsync(cancellationToken: ct);
    firstRead.Stop();
    var accounts = await store.GetAccountsAsync(cancellationToken: ct);
    var schedules = await services.GetRequiredService<PlanStore>().GetSchedulesAsync(ct);
    if (entries.Count != workload.Entries || accounts.Count != workload.Accounts || schedules.Count != workload.Schedules)
    { throw new InvalidOperationException("Fixture counts changed."); }
    var byId = accounts.ToDictionary(a => a.Id);
    var metrics = new Dictionary<string, double[]>();
    async Task Measure(string name, Func<Task> action)
    {
        await action(); // Record first-use separately; these are warmed operation samples, not cold starts.
        var samples = new double[7];
        for (var i = 0; i < samples.Length; i++)
        {
            var watch = Stopwatch.StartNew(); await action(); watch.Stop(); samples[i] = watch.Elapsed.TotalMilliseconds;
        }
        metrics.Add(name, samples);
    }
    await Measure("PendingMigrationCheck", () => services.MigrateLocalDatabaseAsync<ZananceDbContext>(ct));
    await Measure("ReadAllEntries", async () => { if ((await store.GetEntriesAsync(cancellationToken: ct)).Count != workload.Entries) { throw new InvalidOperationException("Lost entries."); } });
    await Measure("ReadMonthEntries", async () => { _ = await store.GetEntriesAsync(new(2026, 10, 1), PerformanceFixture.Today, ct); });
    foreach (var text in new[] { "QA06 item 000012", "not-present", "12.50", "QA06 item" })
    {
        await Measure($"SearchAndGroup:{text}", () =>
        {
            _ = EntrySearch.ByDay(EntrySearch.Apply(entries, new EntryFilter(Text: text), _ => "Fictitious category", byId), byId);
            return Task.CompletedTask;
        });
    }
    await Measure("HomeCalculations", () =>
    {
        var accountEntries = new AccountEntryIndex(entries);
        _ = LedgerCalculator.TotalBalances(accounts, entries, PerformanceFixture.Today);
        _ = LedgerCalculator.Totals(accounts, entries, new LedgerFilter(new(2026, 10, 1), PerformanceFixture.Today));
        foreach (var account in accounts) { _ = LedgerCalculator.Balance(account, accountEntries.For(account.Id), PerformanceFixture.Today); }
        _ = ForecastCalculator.Compute(accounts, entries, schedules, [], PerformanceFixture.Today, PerformanceFixture.Today.AddDays(90));
        return Task.CompletedTask;
    });
    // Integrity, read-back and cleanup are outside the timed SaveEntryAsync boundary.
    var beforeSaves = await PerformanceFixture.FingerprintAsync(path, ct);
    var initialSave = new LedgerEntry { AccountId = accounts[0].Id, Amount = 1234, Date = PerformanceFixture.Today, Kind = EntryKind.Expense, Title = "QA06 initial save" };
    var initialWatch = Stopwatch.StartNew(); var initialResult = await store.SaveEntryAsync(initialSave, ct); initialWatch.Stop();
    if (!initialResult.Succeeded) { throw new InvalidOperationException("Initial save failed."); }
    await store.DeleteEntryAsync(initialSave.Id, ct);
    var saveSamples = new List<double>();
    for (var i = 0; i < 7; i++)
    {
        var entry = new LedgerEntry { AccountId = accounts[0].Id, Amount = 1234, Date = PerformanceFixture.Today, Kind = EntryKind.Expense, Title = "QA06 measured save" };
        var watch = Stopwatch.StartNew(); var save = await store.SaveEntryAsync(entry, ct); watch.Stop();
        if (!save.Succeeded || await store.GetEntryAsync(entry.Id, ct) is not { Amount: 1234 }) { throw new InvalidOperationException("Saved row proof failed."); }
        saveSamples.Add(watch.Elapsed.TotalMilliseconds); await store.DeleteEntryAsync(entry.Id, ct);
    }
    metrics.Add("SaveValidatedEntry", [.. saveSamples]);
    if (beforeSaves != await PerformanceFixture.FingerprintAsync(path, ct)) { throw new InvalidOperationException("Save probe changed fixture rows."); }
    await using (var db = await factory.CreateDbContextAsync(ct))
    {
        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", ct);
        if (await db.Entries.CountAsync(ct) != workload.Entries) { throw new InvalidOperationException("Save cleanup count mismatch."); }
    }
    results.Add(new { Scale = scale, Workload = workload, Migration = migrations, FirstReadAfterWorkerMilliseconds = firstRead.Elapsed.TotalMilliseconds,
        FirstValidatedSaveMilliseconds = initialWatch.Elapsed.TotalMilliseconds,
        MetricsMilliseconds = metrics, Memory = new { Process.GetCurrentProcess().WorkingSet64, GCHeapBytes = GC.GetTotalMemory(false) } });
    SqliteConnection.ClearAllPools();
}
await File.WriteAllTextAsync(Path.Combine(root, "measurements.json"), JsonSerializer.Serialize(new
{
    CapturedAtUtc = DateTimeOffset.UtcNow, Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Environment.ProcessorCount,
    Method = "Three fresh migration workers per shape; seven warmed operation samples; fixture generation excluded; no UI/render/device timing implied.", Results = results,
}, json), ct);
Console.WriteLine("Fictitious performance measurements and legacy/current fixtures written successfully.");
