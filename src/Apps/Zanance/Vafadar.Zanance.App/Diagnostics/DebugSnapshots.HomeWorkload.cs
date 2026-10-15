#if DEBUG && WINDOWS
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Measures original native Home publication on retained protected reference and tenfold fixtures.</summary>
    private static async Task ReviewHomeWorkloadAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new IOException("Loaded Home review requires the independent compiled installation identity.");
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("VAFADAR_HOME_PERFORMANCE_ROOT")
            ?? throw new IOException("Provide the retained fictitious protected workload root."));
        if (!File.Exists(Path.Combine(root, "fictitious-performance.marker")))
            throw new IOException("Retained workload marker is missing; this route never seeds financial data.");
        for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
            if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected workload root refused.");
        var local = services.GetRequiredService<ILocalizationService>();
        var results = new List<object>();
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10)); var ct = deadline.Token;
        foreach (var (name, entryCount, accountCount) in new[] { ("reference", 10_000, 20), ("tenfold", 100_000, 200) })
        {
            var directory = Path.Combine(root, name);
            void Validate()
            {
                if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0
                    || !File.Exists(Path.Combine(directory, "fictitious-performance.marker")))
                    throw new IOException("Retained independent workload identity is missing or redirected.");
            }
            Validate();
            var original = Path.Combine(directory, "fixture.db");
            var identityPath = Path.Combine(directory, "identities.sqlite");
            var historyPath = original + ".restore-history.sqlite";
            if (!File.Exists(identityPath) || !File.Exists(historyPath) || !Directory.Exists(Path.Combine(directory, "keys")))
                throw new IOException("Only an already protected workload can be resumed; never create replacement keys.");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new WindowsGenerationKeyStore(Path.Combine(directory, "keys"), Validate);
            SqlCipherProfileGeneration generation;
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
            {
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate);
                var identity = (await identities.ReadAsync(ct)).Single();
                var history = new SqliteProfileRestoreHistory(historyPath, identity.InitialCandidateKey, Validate);
                generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                    _ => throw new IOException("Unexpected restored workload."), keys, new WindowsGenerationFileDurability(directory),
                    owner, Validate, 256 * 1024 * 1024, "4.19.0", ct);
                generation.Publish(location, owner);
            }
            using (generation)
            {
                var store = data.GetRequiredService<ZananceStore>();
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var expected = await store.GetHomeSnapshotAsync(today, 0, ct);
                if (expected.Entries.Count != entryCount || expected.QuickResources.Accounts.Count != accountCount)
                    throw new IOException("The retained complete reference workload differs from its declared shape.");
                var balances = LedgerCalculator.TotalBalances(expected.QuickResources.Accounts, expected.Entries, today);
                foreach (var language in new[] { "en", "fa", "de" })
                {
                    await SetLanguageAsync(app, local, language);
                    app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                    if (Shell.Current is not AppShell shell || shell.CurrentPage is not HomePage originalHome)
                        throw new IOException("Original Home navigation is unavailable.");
                    await originalHome.PendingPublication;
                    var samples = new List<double>(); var gaps = new List<double>();
                    for (var sample = 0; sample < 2; sample++)
                    {
                        var reads = new HomeWindowReadFactory(data.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
                        var measuredStore = new ZananceStore(reads, data.GetRequiredService<TimeProvider>(), data.GetRequiredService<ICommercialWriteAccessSource>());
                        var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, measuredStore);
                        var page = new HomePage(vm)
                        { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                        var presentations = 0;
                        vm.PresentingSnapshot += () =>
                        {
                            if (vm.Loading.IsReady) throw new IOException("Original section layout was applied after uncovering Home.");
                            presentations++;
                        };
                        var heartbeat = Stopwatch.StartNew(); var last = heartbeat.Elapsed.TotalMilliseconds; var largest = 0d;
                        var timer = app.Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(50);
                        timer.Tick += (_, _) => { var now = heartbeat.Elapsed.TotalMilliseconds; largest = Math.Max(largest, now - last); last = now; };
                        var clock = Stopwatch.StartNew(); timer.Start();
                        await shell.Navigation.PushAsync(page, false);
                        try
                        {
                            await page.PendingPublication;
                            if (reads.SynchronousCaptures != 0 || presentations != 1)
                                throw new IOException("Home appearance reopened settings synchronously or missed covered layout publication.");
                            if (!vm.Loading.IsReady || vm.Accounts.Count != accountCount || !vm.HasEntries
                                || !page.FindByName<ScrollView>("ContentViewport").IsVisible)
                                throw new IOException("Loaded original Home did not publish every expected account.");
                            foreach (var balance in balances)
                                if (!vm.Balances.Any(value => value.CurrencyCode == balance.Key
                                    && value.Text == Core.Money.MoneyText.Format(balance.Value, balance.Key, local.CurrentCulture)))
                                    throw new IOException("Native Home balance differs from the complete original ledger.");
                            // Include actual WinUI layout in publication timing; PNG encoding is recorded separately.
                            if (page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native)
                                throw new IOException("The actual native Home tree is unavailable.");
                            native.UpdateLayout(); clock.Stop();
                            await Task.Delay(75, ct); timer.Stop();
                            samples.Add(clock.Elapsed.TotalMilliseconds); gaps.Add(largest);
                            if (sample == 0)
                                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{name}-{language}-loaded.png"));
                        }
                        finally
                        {
                            timer.Stop(); page.RetirePublication(); await shell.Navigation.PopAsync(false);
                            await originalHome.PendingPublication;
                        }
                    }
                    results.Add(new { Shape = name, Language = language, Entries = entryCount, Accounts = accountCount,
                        FirstThenSecondPublicationMilliseconds = samples, LargestDispatcherGapMilliseconds = gaps,
                        OriginalBalancesMatched = true, ActualNativeLayout = true, NoSynchronousSettingsRead = true,
                        LayoutAppliedBeforeUncovering = true });
                    await File.WriteAllTextAsync(Path.Combine(folder, "home-workload-progress.json"), JsonSerializer.Serialize(results), ct);
                }
                using var bytes = new MemoryStream(); await data.GetRequiredService<IBackupSource>().WriteAsync(bytes, ct); bytes.Position = 0;
                await using var portable = await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
                var key = await keys.ReadAsync(generation.Binding, ct);
                try
                {
                    await using var rollback = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                        Password = Convert.ToBase64String(key) }.ToString());
                    await rollback.OpenAsync(ct);
                    await SqliteSnapshotVerifier.VerifyAsync(rollback, portable.Connection, ct);
                }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "home-workload-proof.json"), JsonSerializer.Serialize(new
        { Results = results, OriginalProductionPage = true, CompleteSchemaAndTypedRowsUnchanged = true,
            NoFinancialSave = true, ColdStartupOrPhoneAcceptance = false }), ct);
    }

    /// <summary>Delegates actual protected contexts and rejects accidental synchronous appearance reads.</summary>
    private sealed class HomeWindowReadFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        /// <summary>Gets the number of synchronous context captures made by the original page operation.</summary>
        public int SynchronousCaptures { get; private set; }
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() { SynchronousCaptures++; return inner.CreateDbContext(); }
        /// <inheritdoc />
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => inner.CreateDbContextAsync(ct);
    }
}
#endif
