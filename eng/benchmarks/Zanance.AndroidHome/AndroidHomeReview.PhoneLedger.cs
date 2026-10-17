using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reports nonsecret hardware/runtime scope without a device identifier or credential.</summary>
    private static object DeviceEvidence() => new
    {
        Model = Android.OS.Build.Model, Api = (int)Android.OS.Build.VERSION.SdkInt,
        ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        PageBytes = Environment.SystemPageSize, DeviceType = DeviceInfo.Current.DeviceType.ToString(),
        IndependentPackage = AppInfo.Current.PackageName
    };

    /// <summary>Exercises original native search and validated writes only inside retained independent ciphertext.</summary>
    private static async Task<object> ReviewPhoneLedgerAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, bool includeWrites = true)
    {
        if (File.Exists(Path.Combine(root, (includeWrites ? "ledger-" : "ledger-read-") + comparison + "-result.json")))
            throw new ReviewCheckException("Completed ledger comparison must not be repeated.");
        var results = new List<JsonElement>();
        var progress = Path.Combine(root, "ledger-progress-" + comparison + ".json");
        if (File.Exists(progress))
        {
            results = JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(progress, ct)) ?? [];
            if (results.Count > 2 || results.Select(row => row.GetProperty("Shape").GetString()).Distinct().Count() != results.Count ||
                results.Any(row => row.GetProperty("Shape").GetString() is not ("reference" or "tenfold") ||
                    !row.GetProperty("CompletePortableSchemaAndTypedRowsUnchanged").GetBoolean() || row.GetProperty("SavedRowsReadBackAndRemoved").GetInt32() != (includeWrites ? 4 : 0)))
                throw new ReviewCheckException("Retained ledger progress differs.");
        }
        app.OpenReviewShell(); await WaitRootAsync();
        await SetLanguageAsync(app, services.GetRequiredService<ILocalizationService>(), "en", ct); await WaitRootAsync();
        foreach (var (name, shape) in new[] { ("reference", Workload.Reference), ("tenfold", Workload.Tenfold) })
        {
            if (results.Any(row => row.GetProperty("Shape").GetString() == name)) continue;
            var folder = Path.Combine(root, name); RequireRoot(folder);
            var pendingPath = Path.Combine(folder, "ledger-pending-" + comparison + ".json");
            if (File.Exists(pendingPath)) throw new ReviewCheckException("Interrupted owned write requires same-fixture inspection; do not reseed or repeat Save.");
            var original = Path.Combine(folder, "fixture.db"); var identityPath = Path.Combine(folder, "identities.sqlite");
            if (File.Exists(original) || !File.Exists(identityPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                throw new ReviewCheckException("Retained protected ledger required; no replacement keys or financial fixture.");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new Security.AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
            SqlCipherProfileGeneration generation;
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
            {
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, () => RequireRoot(folder));
                var identity = (await identities.ReadAsync(ct)).Single();
                var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                    _ => throw new ReviewCheckException("No restored workload expected."), keys, new Security.AndroidGenerationFileDurability(folder),
                    owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                generation.Publish(location, owner);
            }
            using (generation)
            {
                await using var before = await PortableAsync(data);
                var store = data.GetRequiredService<ZananceStore>();
                var time = services.GetRequiredService<TimeProvider>();
                var vm = new TransactionsViewModel(store, services.GetRequiredService<Translator>(),
                    services.GetRequiredService<ILocalizationService>(), services.GetRequiredService<IDateFormatter>(), time,
                    new UndoService(store, time), services.GetRequiredService<IAppInteraction>());
                var page = new TransactionsPage(vm); var searchSamples = new List<object>(); var savedTimes = new List<double>();
                var clock = Stopwatch.StartNew(); var heartbeat = Stopwatch.StartNew(); var last = 0d; var largestGap = 0d;
                var timer = app.Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(50);
                timer.Tick += (_, _) => { var now = heartbeat.Elapsed.TotalMilliseconds; largestGap = Math.Max(largestGap, now - last); last = now; };
                timer.Start();
                try
                {
                    if (app.Windows[0].Page is not AppShell shell) throw new ReviewCheckException("Original native Shell required.");
                    await shell.Navigation.PushAsync(page, false); if (!vm.Loading.IsReady) await vm.LoadAsync();
                    vm.PeriodIndex = 3;
                    if (vm.Days.Sum(day => day.Count) != shape.Entries) throw new ReviewCheckException("Original complete ledger shape differs.");
                    if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Original ledger native view missing.");
                    await WaitLayoutAsync(native, ct); clock.Stop(); var loadedMs = clock.Elapsed.TotalMilliseconds;
                    var searchBar = Visuals(page).OfType<SearchBar>().Single();
                    if (searchBar.Handler?.PlatformView is not AndroidX.AppCompat.Widget.SearchView input)
                        throw new ReviewCheckException("Original native search input missing.");
                    foreach (var query in new[] { "QA06 item 000012", "not-present", "QA06 item" })
                    {
                        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
                        { if (args.PropertyName == nameof(vm.Days)) changed.TrySetResult(); }
                        vm.PropertyChanged += OnChanged; clock.Restart();
                        try { input.SetQuery(query, false); await changed.Task.WaitAsync(TimeSpan.FromSeconds(45), ct); }
                        finally { vm.PropertyChanged -= OnChanged; }
                        await WaitLayoutAsync(native, ct); clock.Stop();
                        var rows = vm.Days.SelectMany(day => day).ToArray();
                        var expected = query == "QA06 item" ? shape.Entries : query == "not-present" ? 0 : 1;
                        if (vm.SearchText != query || rows.Length != expected || rows.Select(row => row.Id).Distinct().Count() != rows.Length ||
                            (expected == 1 && rows[0].Id != PerformanceFixture.Id(2, 12)))
                            throw new ReviewCheckException("Original native search results differ.");
                        if (expected == 1)
                        {
                            var viewport = page.FindByName<CollectionView>("ContentViewport");
                            if (viewport.Handler?.PlatformView is not Android.Views.View list || !NativeTexts(list).Contains(query))
                                throw new ReviewCheckException("Matching original result row was not realized in the native viewport.");
                        }
                        searchSamples.Add(new { Query = query, ResultCount = rows.Length, DebounceThroughNativeLayoutMs = clock.Elapsed.TotalMilliseconds });
                    }
                    await shell.Navigation.PopAsync(false);
                    for (var sample = 0; sample < (includeWrites ? 4 : 0); sample++)
                    {
                        var accounts = await store.GetAccountsAsync(cancellationToken: ct);
                        var entry = new LedgerEntry { AccountId = accounts[0].Id, Amount = 1234, Date = PerformanceFixture.Today,
                            Kind = EntryKind.Expense, Title = "QA06 independent phone save" };
                        // Retain the exact owned intent before invoking Save; an interrupted write is never guessed or repeated.
                        await File.WriteAllTextAsync(pendingPath, JsonSerializer.Serialize(entry), ct);
                        clock.Restart(); var saved = await store.SaveEntryAsync(entry, ct); clock.Stop();
                        if (!saved.Succeeded) throw new ReviewCheckException("Original validated phone write refused.");
                        var actual = await store.GetEntryAsync(entry.Id, ct);
                        if (actual is null || actual.AccountId != entry.AccountId || actual.Amount != entry.Amount || actual.Date != entry.Date ||
                            actual.Kind != entry.Kind || actual.Title != entry.Title) throw new ReviewCheckException("Owned saved-row read-back differs.");
                        var removed = await store.DeleteEntryAsync(entry.Id, ct);
                        if (removed.Count != 1 || removed[0].Id != entry.Id || await store.GetEntryAsync(entry.Id, ct) is not null)
                            throw new ReviewCheckException("Owned saved-row cleanup differs.");
                        File.Delete(pendingPath); savedTimes.Add(clock.Elapsed.TotalMilliseconds);
                    }
                    timer.Stop();
                    await using var after = await PortableAsync(data);
                    await SqliteSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, ct);
                    results.Add(JsonSerializer.SerializeToElement(new { Shape = name, shape.Entries, shape.Accounts, shape.Schedules,
                        NativeLedgerLoadAndLayoutMs = loadedMs, NativeSearch = searchSamples, SaveValidatedTransactionMs = savedTimes,
                        SavedRowsReadBackAndRemoved = savedTimes.Count, LargestDispatcherGapMs = largestGap,
                        CompletePortableSchemaAndTypedRowsUnchanged = true }));
                    await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(results), ct);
                }
                finally { timer.Stop(); }
            }
        }
        return new { Comparison = comparison, Device = DeviceEvidence(), Results = results,
            MainPackageTouched = false, SystemSettingsChanged = false, ValidatedWritesIncluded = includeWrites, FullColdStartupOrLowMemoryAcceptance = false,
            Boundary = "Original native ledger input/grouping/layout and validated store Save/read-back/cleanup; no main data or copied financial algorithm" };

        async Task<SqliteMemorySnapshot> PortableAsync(IServiceProvider data)
        {
            using var bytes = new MemoryStream(); await data.GetRequiredService<IBackupSource>().WriteAsync(bytes, ct); bytes.Position = 0;
            return await SqliteMemorySnapshot.OpenAsync(bytes, 256 * 1024 * 1024, ct);
        }
        async Task WaitRootAsync()
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                if (app.Windows[0].Page?.Handler?.PlatformView is Android.Views.View view && view.IsAttachedToWindow) return;
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original Shell root did not attach.");
        }
        static IEnumerable<Microsoft.Maui.Controls.View> Visuals(IVisualTreeElement element)
        {
            if (element is Microsoft.Maui.Controls.View view) yield return view;
            foreach (var child in element.GetVisualChildren()) foreach (var nestedView in Visuals(child)) yield return nestedView;
        }
    }
}
