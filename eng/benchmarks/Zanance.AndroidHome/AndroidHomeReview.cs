using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Android.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>Measures original Release/AOT Home publication against independently owned protected financial workloads.</summary>
internal static partial class AndroidHomeReview
{
    /// <summary>The independent install allowed to own these fictitious workloads.</summary>
    internal const string Package = "pro.vafadar.zanance.androidhomereview";
    /// <summary>Public derived-result channel scoped by the host to the exact review process.</summary>
    internal const string LogTag = "ZananceHomeReview";
    private static object? _balanceEvidence;

    /// <summary>Prepares a fresh workload or measures its existing protected generations.</summary>
    internal static async Task RunAsync(App app, IServiceProvider services, string phase, string run, string? comparison, string? baseline)
    {
        if (AppInfo.Current.PackageName != Package) throw new ReviewCheckException("Independent compiled identity required.");
        // Android's /data/user/0 is an OS alias. Resolve it before applying the unchanged no-redirect fixture guard.
        var parent = Path.Combine(new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath, "android-home");
        Directory.CreateDirectory(parent);
        var root = Path.Combine(parent, run);
        _balanceEvidence = null;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(12)); var ct = deadline.Token;
        try
        {
            if (phase == "prepare")
            {
                await Task.Run(() => PrepareAsync(root, ct), ct);
                await WriteReportAsync(root, phase, new { Prepared = true, Fictitious = true, MainPackageTouched = false, Device = DeviceEvidence() }, ct);
            }
            else if (phase == "ledger")
            {
                RequireRoot(root);
                await WriteReportAsync(root, phase, await ReviewPhoneLedgerAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "tables")
            {
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed financial table review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewFinancialTablesAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "billing-owned")
            {
                if (File.Exists(Path.Combine(root, phase + "-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed billing review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewOwnedBillingAsync(app, services, root, comparison!, ct), ct);
            }
            else if (phase == "schema")
            {
                RequireRoot(root);
                await WriteReportAsync(root, phase, await ReviewSchemaConnectionAsync(root, comparison!, ct), ct);
            }
            else if (phase == "filters")
            {
                if (File.Exists(Path.Combine(root, "filters-" + comparison + "-result.json")))
                    throw new ReviewCheckException("Completed filter review must not be repeated.");
                Directory.CreateDirectory(root);
                await WriteReportAsync(root, phase, await ReviewFilterActionsAsync(app, services, root, comparison!, ct), ct);
            }
            else
            {
                RequireRoot(root);
                if (comparison is null || !System.Text.RegularExpressions.Regex.IsMatch(comparison, "^[0-9a-f]{32}$")
                    || phase is not ("measure" or "compare" or "guard")) throw new ReviewCheckException("Explicit original comparison required.");
                if (baseline is null || !System.Text.RegularExpressions.Regex.IsMatch(baseline, "^[0-9a-f]{32}$"))
                    throw new ReviewCheckException("Original baseline presentation identity required.");
                await MeasureAsync(app, services, root, phase, comparison, baseline, ct);
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Never serialize exception messages: native command errors can contain key-bearing connection text.
            var report = new { Failed = true, Comparison = comparison, ErrorType = error.GetType().FullName, Stack = error.StackTrace,
                ReviewCheck = error is ReviewCheckException check ? check.Message : null,
                WindowPageType = app.Windows.FirstOrDefault()?.Page?.GetType().Name,
                ShellPageType = (app.Windows.FirstOrDefault()?.Page as AppShell)?.CurrentPage?.GetType().Name,
                BalanceEvidence = _balanceEvidence,
                CompletedCases = File.Exists(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")) ? await File.ReadAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")) : null,
                RootExists = Directory.Exists(root), MarkerExists = File.Exists(Path.Combine(root, "fictitious-performance.marker")),
                RedirectedAncestors = RedirectedAncestors(root) };
            var path = Directory.Exists(root) ? Path.Combine(root, "failure-" + Guid.NewGuid().ToString("N") + ".json")
                : Path.Combine(parent, run + "-failure-" + Guid.NewGuid().ToString("N") + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report), CancellationToken.None);
            LogReport(phase, run, report);
        }
    }

    private static string[] RedirectedAncestors(string root)
    {
        var names = new List<string>();
        for (var item = new DirectoryInfo(root); item is not null; item = item.Parent)
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) names.Add(item.Name);
        return names.ToArray();
    }

    private static void RequireRoot(string root)
    {
        if (!File.Exists(Path.Combine(root, "fictitious-performance.marker"))) throw new ReviewCheckException("Owned workload marker missing.");
        for (var item = new DirectoryInfo(root); item is not null; item = item.Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new ReviewCheckException("Redirected workload root refused.");
    }

    private static async Task PrepareAsync(string root, CancellationToken ct)
    {
        FixtureDirectory.Create(root);
        foreach (var (name, shape) in new[] { ("reference", Workload.Reference), ("tenfold", Workload.Tenfold) })
        {
            var folder = Path.Combine(root, name);
            await PerformanceFixture.CreateAsync(folder, shape, ct);
            var original = Path.Combine(folder, "fixture.db");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keyFolder = Path.Combine(folder, "keys"); Directory.CreateDirectory(keyFolder);
            var keys = new AndroidGenerationKeyStore(keyFolder);
            using var owner = await location.Access.AcquireExclusiveAsync(ct);
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, Path.Combine(folder, "identities.sqlite"),
                new Dictionary<string, string> { [""] = original }, true, keys, new AndroidGenerationFileDurability(folder),
                (_, _) => throw new ReviewCheckException("No restored intent belongs to a new workload."), () => RequireRoot(folder),
                256 * 1024 * 1024, "4.19.0", ct);
        }
    }

    private static async Task MeasureAsync(App app, IServiceProvider services, string root, string phase, string comparison, string baseline, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>();
        app.OpenReviewShell();
        await Task.Delay(1000, ct); // Let original empty-install foreground work settle outside the measurement.
        var results = new List<object>();
        var nativeReadProfiles = new List<object>();
        var reviewedInputGuards = new List<object>();
        var completed = new HashSet<string>(StringComparer.Ordinal);
        if (File.Exists(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json")))
        {
            using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json"), ct));
            foreach (var item in previous.RootElement.EnumerateArray())
            {
                var name = item.GetProperty("Shape").GetString(); var language = item.GetProperty("Language").GetString();
                if (name is not ("reference" or "tenfold") || language is not ("en" or "fa" or "de") ||
                    !item.GetProperty("OriginalBalancesAndNativeTextMatched").GetBoolean() ||
                    !item.GetProperty("OriginalFinancialPresentationMatched").GetBoolean() ||
                    item.GetProperty("FirstThenSecondPublicationMilliseconds").GetArrayLength() != (phase == "guard" ? 1 : 2) ||
                    !completed.Add(name + ":" + language)) throw new ReviewCheckException("Invalid retained completed case.");
                results.Add(item.Clone());
            }
        }
        foreach (var (name, shape) in new[] { ("reference", Workload.Reference), ("tenfold", Workload.Tenfold) })
        {
            var folder = Path.Combine(root, name); RequireRoot(folder);
            var original = Path.Combine(folder, "fixture.db");
            var identityPath = Path.Combine(folder, "identities.sqlite");
            if (File.Exists(original) || !File.Exists(identityPath) || !Directory.Exists(Path.Combine(folder, "keys")))
                throw new ReviewCheckException("Only the retained protected workload can be measured; never regenerate keys or finance.");
            using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
            SqlCipherProfileGeneration generation;
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
            {
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, () => RequireRoot(folder));
                var identity = (await identities.ReadAsync(ct)).Single();
                var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
                generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                    _ => throw new ReviewCheckException("No restored workload is expected."), keys, new AndroidGenerationFileDurability(folder),
                    owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
                generation.Publish(location, owner);
            }
            using (generation)
            {
                if (phase != "guard") nativeReadProfiles.Add(await ProfileNativeReadAsync(data, shape.Entries, ct));
                var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
                var expected = await data.GetRequiredService<ZananceStore>().GetHomeSnapshotAsync(today, 0, ct);
                if (expected.Entries.Count != shape.Entries || expected.QuickResources.Accounts.Count != shape.Accounts || expected.Plans.Schedules.Count != shape.Schedules)
                    throw new ReviewCheckException("Retained complete workload shape differs.");
                var balances = LedgerCalculator.TotalBalances(expected.QuickResources.Accounts, expected.Entries, today);
                foreach (var language in phase == "guard" ? new[] { "en" } : new[] { "en", "fa", "de" })
                {
                    if (completed.Contains(name + ":" + language)) continue;
                    await SetLanguageAsync(app, local, language, ct);
                    if (app.Windows[0].Page is not AppShell shell || shell.CurrentPage is not HomePage originalHome)
                        throw new ReviewCheckException("Original native Shell/Home is unavailable.");
                    await originalHome.PendingPublication;
                    var samples = new List<double>(); var gaps = new List<double>();
                    for (var sample = 0; sample < (phase == "guard" ? 1 : 2); sample++)
                    {
                        var reads = new WindowReadFactory(data.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
                        var store = new ZananceStore(reads, data.GetRequiredService<TimeProvider>(), data.GetRequiredService<ICommercialWriteAccessSource>());
                        var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                        var page = new HomePage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                        var presentations = 0;
                        vm.PresentingSnapshot += () => { if (vm.Loading.IsReady) throw new ReviewCheckException("Layout published after uncovering."); presentations++; };
                        var heartbeat = Stopwatch.StartNew(); var last = 0d; var largest = 0d;
                        var timer = app.Dispatcher.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(50);
                        timer.Tick += (_, _) => { var now = heartbeat.Elapsed.TotalMilliseconds; largest = Math.Max(largest, now - last); last = now; };
                        var clock = Stopwatch.StartNew(); timer.Start();
                        await shell.Navigation.PushAsync(page, false);
                        try
                        {
                            await page.PendingPublication;
                            if (!vm.Loading.IsReady || vm.Accounts.Count != shape.Accounts || !vm.HasEntries ||
                                reads.SynchronousCaptures != 0 || presentations != 1 || !page.FindByName<ScrollView>("ContentViewport").IsVisible)
                                throw new ReviewCheckException("Complete original Home publication differs.");
                            if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Android native Home tree unavailable.");
                            await WaitLayoutAsync(native, ct); clock.Stop();
                            var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
                            foreach (var balance in balances)
                            {
                                var packet = Core.Money.MoneyText.Format(balance.Value, balance.Key, local.CurrentCulture);
                                // Native label mapping applies the original digit/separator choice; canonical VM money remains Latin.
                                var displayed = Vafadar.Localization.Formatting.NativeDigits.Apply(packet)!;
                                _balanceEvidence = new { Shape = name, Language = language, Expected = packet,
                                    ViewModel = vm.Balances.Select(value => new { value.CurrencyCode, value.Text }).ToArray(),
                                    NativeTextCount = texts.Count, NativeCurrencyTexts = texts.Where(text => text.Contains(balance.Key, StringComparison.Ordinal)).Take(8).ToArray() };
                                if (!vm.Balances.Any(value => value.CurrencyCode == balance.Key && value.Text == packet) || !texts.Contains(displayed))
                                    throw new ReviewCheckException("Real native balance differs from the complete original ledger.");
                            }
                            await Task.Delay(75, ct); timer.Stop();
                            var presentation = CapturePresentation(vm);
                            var originalPath = Path.Combine(folder, "presentation-" + baseline + "-" + language + ".json");
                            if (phase == "measure" && !File.Exists(originalPath))
                                await File.WriteAllTextAsync(originalPath, presentation, ct);
                            if (!File.Exists(originalPath) || presentation != await File.ReadAllTextAsync(originalPath, ct))
                                throw new ReviewCheckException("Original complete financial presentation differs.");
                            samples.Add(clock.Elapsed.TotalMilliseconds); gaps.Add(largest);
                        }
                        finally { timer.Stop(); page.RetirePublication(); await shell.Navigation.PopAsync(false); await originalHome.PendingPublication; }
                    }
                    results.Add(new { Shape = name, Language = language, shape.Entries, shape.Accounts, shape.Schedules,
                        FirstThenSecondPublicationMilliseconds = samples, LargestDispatcherGapMilliseconds = gaps,
                        OriginalBalancesAndNativeTextMatched = true, OriginalFinancialPresentationMatched = true, ActualNativeLayout = true, NoSynchronousSettingsRead = true });
                    await File.WriteAllTextAsync(Path.Combine(root, "progress-" + comparison + "-" + phase + ".json"), JsonSerializer.Serialize(results), ct);
                    _balanceEvidence = null;
                }
                if (phase == "guard")
                {
                    var store = data.GetRequiredService<ZananceStore>();
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    var originalAmount = expected.Plans.Schedules[0].Amount;
                    var planRejected = false;
                    try
                    {
                        expected.Plans.Schedules[0].Amount = (originalAmount ?? 0) + 1;
                        try { await store.ValidateHomeSnapshotAsync(expected, ct); }
                        catch (InvalidOperationException) { planRejected = true; }
                    }
                    finally { expected.Plans.Schedules[0].Amount = originalAmount; }
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    var added = new Core.Goals.Goal { Name = "Fictitious altered review only", CurrencyCode = "EUR", TargetAmount = 123 };
                    var goalRejected = false;
                    try
                    {
                        expected.Goals.Goals.Add(added);
                        try { await store.ValidateHomeSnapshotAsync(expected, ct); }
                        catch (InvalidOperationException) { goalRejected = true; }
                    }
                    finally { expected.Goals.Goals.Remove(added); }
                    await store.ValidateHomeSnapshotAsync(expected, ct);
                    if (!planRejected || !goalRejected) throw new ReviewCheckException("Altered original input review was accepted.");
                    reviewedInputGuards.Add(new { Shape = name, AlteredPlanInputRefused = true, AlteredGoalInputRefused = true,
                        SameRestoredOriginalReviewAccepted = true, NoDatabaseSave = true });
                }
                var rawKey = await keys.ReadAsync(generation.Binding, ct);
                try
                {
                    await using var retainedRollback = new SqliteConnection(new SqliteConnectionStringBuilder
                    { DataSource = generation.Path + ".rollback.db", Mode = SqliteOpenMode.ReadOnly, Pooling = false,
                        Password = Convert.ToBase64String(rawKey) }.ToString());
                    await retainedRollback.OpenAsync(ct);
                    await using var current = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                    await current.Database.OpenConnectionAsync(ct);
                    await SqliteSnapshotVerifier.VerifyAsync(retainedRollback, (SqliteConnection)current.Database.GetDbConnection(), ct);
                }
                finally { CryptographicOperations.ZeroMemory(rawKey); }
            }
        }
        await WriteReportAsync(root, phase, new { Comparison = comparison, BaselineComparison = baseline, Results = results, NativeReadProfiles = nativeReadProfiles, ReviewedInputGuards = reviewedInputGuards,
            DataModuleVersionId = typeof(ZananceStore).Module.ModuleVersionId, CallerModuleVersionId = typeof(HomePage).Module.ModuleVersionId,
            OriginalProductionPage = true, RetainedCompletedCases = completed.ToArray(),
            ModuleIdentityScope = "Current completion caller; retained case packets preserve their original caller evidence",
            CompleteRawSchemaAndTypedRowsUnchanged = true, NoFinancialSave = true, Device = DeviceEvidence(), WholeAppColdStartupOrFullDeviceAcceptance = false,
            Boundary = "Original navigation/read/projection/publication plus Android layout and frame callbacks; excludes fixture preparation and cold startup" }, ct);
    }

    /// <summary>Separates native stepping from entity materialization, with the original operation-local cache and no financial writes.</summary>
    private static Task<object> ProfileNativeReadAsync(IServiceProvider data, int expected, CancellationToken ct) => Task.Run(async () =>
    {
        await using var db = await data.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -32768", ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT * FROM Entries NOT INDEXED ORDER BY rowid DESC";
        var clock = Stopwatch.StartNew(); var rows = 0;
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) rows++;
        clock.Stop();
        if (rows != expected) throw new ReviewCheckException("Native read profile lost original rows.");
        var entity = db.Model.FindEntityType(typeof(LedgerEntry)) as Microsoft.EntityFrameworkCore.Metadata.RuntimeEntityType
            ?? throw new ReviewCheckException("Original compiled runtime model unavailable.");
        var binding = entity.ConstructorBinding as Microsoft.EntityFrameworkCore.Metadata.ConstructorBinding
            ?? throw new ReviewCheckException("Original ledger materialization binding unavailable.");
        return (object)new { Entries = rows, NativeSteppingMilliseconds = clock.Elapsed.TotalMilliseconds,
            ConstructorParameters = binding.Constructor.GetParameters().Select(parameter => parameter.Name).ToArray(),
            Boundary = "native full-row stepping only; excludes key opening, CLR field conversion and managed sorting" };
    }, ct);

    /// <summary>Captures original financial presentation without commands, colours or copied calculation algorithms.</summary>
    private static string CapturePresentation(HomeViewModel vm) => JsonSerializer.Serialize(new
    {
        Balances = vm.Balances.Select(value => new { value.CurrencyCode, value.Text }),
        Periods = vm.Periods.Select(value => new { value.IncomeText, value.ExpenseText, value.ResultText, value.RefundsText }),
        Accounts = vm.Accounts.Select(value => new { value.Id, value.Name, value.TypeName, value.BalanceText, value.IsNegative, value.NotInTotals, value.OpeningUnknown }),
        Upcoming = vm.Upcoming.Select(value => new { value.ScheduleId, value.OriginalDate, value.Title, value.Subtitle, value.AmountText, value.Badge }),
        Slices = vm.Slices.Select(value => new { value.CategoryIds, value.Name, value.Value, value.AmountText, value.PercentText }),
        vm.BudgetText, vm.BudgetDailyText, vm.BudgetProgress, vm.ForecastEndText, vm.ForecastLowText, vm.ForecastNote,
        vm.LowBalanceText, vm.DueCount, vm.DueText, vm.Next30Text, vm.ContractText, vm.ReimbursementText,
        vm.UnreviewedCount, vm.UnreviewedText, vm.ConfirmedBalanceText, vm.CombinedText, vm.CombinedIncomplete,
        vm.HoldingsText, vm.GoalAttentionText, vm.ScopeText,
    });

    private static async Task SetLanguageAsync(App app, ILocalizationService local, string language, CancellationToken ct)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rebuilt(object? sender, EventArgs args) => completion.TrySetResult();
        app.ShellRebuilt += Rebuilt;
        try
        {
            var changed = local.CurrentLanguage.CultureName != language;
            local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
            if (changed) await completion.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        }
        finally { app.ShellRebuilt -= Rebuilt; }
        await Task.Delay(500, ct);
    }

    private static async Task WaitLayoutAsync(Android.Views.View view, CancellationToken ct)
    {
        // Keep the original successful frame boundary; allow the same real layout to settle on slower hardware.
        for (var frame = 0; frame < 300; frame++)
        {
            using var callback = new FrameCompletion();
            Choreographer.Instance!.PostFrameCallback(callback);
            await callback.Completion.Task.WaitAsync(ct);
            if (frame > 0 && view.IsLaidOut && !view.IsLayoutRequested && view.Width > 0 && view.Height > 0) return;
        }
        throw new ReviewCheckException("Original native Home did not finish layout.");
    }

    private static IEnumerable<string> NativeTexts(Android.Views.View view)
    {
        if (view is Android.Widget.TextView text && text.Text is { } value) yield return value;
        if (view is ViewGroup group)
            for (var index = 0; index < group.ChildCount; index++)
                foreach (var item in NativeTexts(group.GetChildAt(index)!)) yield return item;
    }

    private static async Task WriteReportAsync(string root, string phase, object report, CancellationToken ct)
    {
        var packet = JsonSerializer.SerializeToElement(report);
        var suffix = packet.TryGetProperty("Comparison", out var comparison) ? "-" + comparison.GetString() : "";
        await using (var output = new FileStream(Path.Combine(root, phase + suffix + "-result.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(output, report, cancellationToken: ct);
        LogReport(phase, Path.GetFileName(root), report);
    }

    private static void LogReport(string phase, string run, object report)
    {
        // Only counts/timings/verification outcomes or type/stack are emitted, never key material or native error messages.
        // Chunk the public JSON rather than enabling debugging or exposing private files in this Release application.
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report));
        var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
        {
            var offset = index * 1536; var length = Math.Min(1536, bytes.Length - offset);
            Android.Util.Log.Info(LogTag, phase + "|" + run + "|" + index + "/" + count + "|" + Convert.ToBase64String(bytes.AsSpan(offset, length)));
        }
    }

    private sealed class ReviewCheckException(string check) : IOException(check) { }

    private sealed class FrameCompletion : Java.Lang.Object, Choreographer.IFrameCallback
    {
        /// <summary>Completes on the actual Android frame callback.</summary>
        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void DoFrame(long frameTimeNanos) => Completion.TrySetResult();
    }

    private sealed class WindowReadFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        /// <summary>Counts forbidden synchronous context captures during original page publication.</summary>
        internal int SynchronousCaptures;
        public ZananceDbContext CreateDbContext() { SynchronousCaptures++; return inner.CreateDbContext(); }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => inner.CreateDbContextAsync(ct);
    }
}
