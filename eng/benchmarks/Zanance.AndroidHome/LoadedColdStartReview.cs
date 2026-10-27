#if LOADED_COLD_START_REVIEW
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>One-purpose original-profile cold review; no historical writer phase or shipping package is admitted.</summary>
internal static class LoadedColdStartReview
{
    /// <summary>Starts only an explicit owned shell request after the original activity and covered startup have begun.</summary>
    internal static void Start(Activity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(36)) return;
        var phase = activity.Intent?.GetStringExtra("review_phase");
        var run = activity.Intent?.GetStringExtra("review_run");
        var shape = activity.Intent?.GetStringExtra("review_shape");
        if (phase is not ("cold-setup" or "cold-presentation" or "cold-measure" or "cold-shell-measure" or "cold-finish") || run is null
            || !Regex.IsMatch(run, "^[0-9a-f]{32}$") || shape is not ("reference" or "tenfold")) return;
        if (activity.PackageName != "pro.vafadar.zanance.androidhomereview" || Android.OS.Process.MyUid() != 10381
            || Android.OS.Build.Model != "SM-A176B" || (int)Android.OS.Build.VERSION.SdkInt != 36) return;
        _ = RunGuardedAsync(activity, phase, run, shape);
    }

    private static void Emit(string run, string phase, string shape, object packet)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Run = run, Phase = phase, Shape = shape,
            Pid = Android.OS.Process.MyPid(), Uid = Android.OS.Process.MyUid(), Packet = packet });
        if (bytes.Length > 600 * 1024) throw new InvalidDataException("Bounded owned cold packet required.");
        var emission = Guid.NewGuid().ToString("N"); var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
            Android.Util.Log.Info("ZananceLoadedCold", emission + "|" + index + "/" + count + "|"
                + Convert.ToBase64String(bytes.AsSpan(index * 1536, Math.Min(1536, bytes.Length - index * 1536))));
    }

    [SupportedOSPlatform("android36.0")]
    private static async Task RunGuardedAsync(Activity activity, string phase, string run, string shape)
    {
        var app = (App)Microsoft.Maui.Controls.Application.Current!;
        var services = IPlatformApplication.Current!.Services;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(12));
        var parent = Path.Combine(new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath, "android-home");
        var root = Path.Combine(parent, "loaded-cold-" + run);
        try
        {
            await app.WaitForStartupAsync().WaitAsync(deadline.Token);
            // Startup completion can precede native focus. Await the actual owner instead of
            // treating the first covered/onboarding frame as permission for a source operation.
            while (!activity.HasWindowFocus) await Task.Delay(100, deadline.Token);
            await AndroidHomeReview.RunLoadedColdPhaseAsync(app, activity, services, root, run, shape, phase, deadline.Token);
            var name = phase == "cold-setup" ? shape + "-prepared.json"
                : phase == "cold-presentation" ? shape + "-presentation-correction.json"
                : phase is "cold-measure" or "cold-shell-measure" ? shape + "-cold-result.json" : "finished.json";
            var path = Path.Combine(root, name);
            if (new FileInfo(path).Length > 512 * 1024) throw new InvalidDataException("Bounded cold result required.");
            using var packet = JsonDocument.Parse(await File.ReadAllTextAsync(path, deadline.Token));
            Emit(run, phase, shape, packet.RootElement);
        }
        catch (Exception error)
        {
            object? afterTiming = null;
            if (phase is "cold-measure" or "cold-shell-measure")
                afterTiming = await AndroidHomeReview.PreserveRefusedColdSourceAsync(services, root, shape, CancellationToken.None);
            // A deadline only ends this observer. Do not restart, replace a profile, force-stop or retry a writer.
            if (Directory.Exists(root))
                await AndroidHomeReview.SaveLoadedColdNewAsync(Path.Combine(root, "failure-" + phase + "-" + shape + "-" + Guid.NewGuid().ToString("N") + ".json"),
                    new { Phase = phase, Shape = shape, Pid = Android.OS.Process.MyPid(), ErrorType = error.GetType().FullName,
                        ObserverOnlyEnded = true, ProcessRestartRequested = false, AcceptedOperationsNotReplayed = true }, CancellationToken.None);
            Emit(run, phase, shape, new { Refused = true, ErrorType = error.GetType().FullName,
                Checkpoint = AndroidHomeReview.LoadedColdCheckpoint,
                Admission = AndroidHomeReview.ClassifyLoadedColdRefusal(error),
                SourceDifference = AndroidHomeReview.LoadedColdSourceDifference,
                OriginalUnitAdmission = AndroidHomeReview.LoadedColdUnitAdmission,
                OwnNativeStartAdmission = LoadedColdStartNative.LastRecordAdmission,
                AfterTimingSource = afterTiming,
                SourceMethods = new System.Diagnostics.StackTrace(error, false).GetFrames().Take(12)
                    .Select(frame => frame.GetMethod()).Select(method => method?.DeclaringType?.FullName + "." + method?.Name).ToArray(),
                OriginalPid = Android.OS.Process.MyPid(), ObservationOnlyEnded = true, NoRestartOrWriterReplay = true });
        }
    }
}

/// <summary>Reuses retained protected datasets only through original source/restore/profile effects.</summary>
internal static partial class AndroidHomeReview
{
    private const string LoadedColdSourceRun = "d53542fe57d6446ab52b82a757ee7f6e";
    private const string LoadedColdSourceBaseline = "94af8eba610f44e48cce22fe410601bd";

    /// <summary>Content-free progress retained when an admission refuses before a source operation.</summary>
    internal static string LoadedColdCheckpoint { get; private set; } = "startup-admission";

    /// <summary>Contains only differing owned table names/counts/column ordinals, never row values.</summary>
    internal static string? LoadedColdSourceDifference { get; private set; }
    /// <summary>Fixed diagnostic category for the original startup-only display-unit boundary.</summary>
    internal static string? LoadedColdUnitAdmission { get; private set; }
    private static bool _loadedColdOriginalUnitMigration;

    /// <summary>Exports only fixed known admission categories, never exception text or connection values.</summary>
    internal static string ClassifyLoadedColdRefusal(Exception error) => error is ReviewCheckException check
        ? check.Message switch
        {
            "Original Home Activity must own actual window focus." => "native-window-focus",
            "Only the original independent Main without accepted ledger entries can host new cold profiles." => "independent-main-or-ledger",
            "Original and adopted schema boundaries differ." => "adopted-schema-boundary",
            "Original restored typed rows differ before any warm publication." => "restored-typed-rows",
            "Original independent physical owner and unredirected cold root required." => "owner-or-root",
            "Original loaded financial presentation differs." => "original-presentation",
            _ => "other-original-review-admission"
        } : "non-review-exception";

    private sealed record ColdBase(string Id, string[] ProfileIds, string SnapshotHash);
    private sealed record ColdProfile(string Id, string Shape, string SourceRowsHash);
    private sealed record ColdCookies(string Financial, string Source, string? Published, long? Schema, long ActualSchema);
    private sealed record ColdPrepared(string ProfileId, string Shape, string Day, string SnapshotHash, string Presentation,
        string Financial, string Source, string Boot, Guid DataModule, Guid CoreModule, bool OriginalDisplayUnitStartupAccepted);
    private sealed record ColdPresentationCorrection(string OriginalPreparedFileHash, string ProfileId,
        string SnapshotHash, string Financial, string Source, string Presentation, int OriginalCards, int CorrectedCards);

    /// <summary>Creates immutable durable operation receipts; an existing request must be observed, never overwritten.</summary>
    internal static async Task SaveLoadedColdNewAsync(string path, object value, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        await file.WriteAsync(bytes, ct); await file.FlushAsync(ct); file.Flush(true);
    }

    private static async Task<T> ReadColdAsync<T>(string path, CancellationToken ct) =>
        JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path, ct)) ?? throw new InvalidDataException("Original cold receipt is absent.");

    private static void ColdStage(string shape, string stage)
    {
        LoadedColdCheckpoint = stage;
        Android.Util.Log.Info("ZananceLoadedColdStage", Android.OS.Process.MyPid() + "|" + shape + "|" + stage);
    }

    private static string ColdHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static async Task<string> ColdRawAsync(IServiceProvider services, CancellationToken ct) =>
        await ImportFinancialNativeReview.StoredAsync(services, ct);

    /// <summary>Dispatches only the new read/owned-profile fixture scope, after actual covered startup.</summary>
    [SupportedOSPlatform("android36.0")]
    internal static async Task RunLoadedColdPhaseAsync(App app, Activity activity, IServiceProvider services,
        string root, string run, string shape, string phase, CancellationToken ct)
    {
        if (!MainThread.IsMainThread || Package != "pro.vafadar.zanance.androidhomereview"
            || Android.OS.Process.MyUid() != 10381 || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Original independent physical owner and unredirected cold root required.");
        if (services.GetRequiredService<AppLockService>() is { IsEnabled: true } or { IsLocked: true })
            throw new ReviewCheckException("This cohort neither configures nor bypasses authentication.");
        if (phase == "cold-setup")
        {
            RequireHomeWindowFocus(); Directory.CreateDirectory(root);
            await SetupLoadedColdAsync(app, services, root, shape, ct);
        }
        else
        {
            if (!Directory.Exists(root) || !File.Exists(Path.Combine(root, "base.json")))
                throw new ReviewCheckException("Existing original cold preparation required.");
            if (phase == "cold-presentation") await CorrectColdPresentationAsync(app, services, root, shape, ct);
            else if (phase is "cold-measure" or "cold-shell-measure") await ObserveLoadedColdAsync(app, activity, services, root, run, shape, ct);
            else await FinishLoadedColdAsync(app, services, root, ct);
        }
        Android.Util.Log.Info("ZananceLoadedCold", run + "|" + phase + "|" + shape + "|complete");
    }

    [SupportedOSPlatform("android36.0")]
    private static async Task SetupLoadedColdAsync(App app, IServiceProvider services, string root, string shape, CancellationToken ct)
    {
        var profiles = services.GetRequiredService<ProfileService>();
        ColdStage(shape, "validate-independent-main");
        var basePath = Path.Combine(root, "base.json");
        if (!File.Exists(basePath))
        {
            if (app.Windows.SingleOrDefault()?.Page is AppShell { CurrentPage: HomePage })
                _ = await LoadedColdStartNative.WaitOriginalHomeAsync(app, ct);
            var store = services.GetRequiredService<ZananceStore>();
            var entryCount = (await store.GetEntriesAsync(cancellationToken: ct)).Count;
            var accountCount = (await store.GetAccountsAsync(cancellationToken: ct)).Count;
            ColdStage(shape, $"base-counts-profiles-{profiles.All.Count}-main-{profiles.Current.IsMain}-entries-{entryCount}-accounts-{accountCount}");
            // An original empty ledger may retain its onboarding account. Never delete that
            // account to satisfy a fixture assumption: preserve all its typed rows in the base
            // digest and restore the workload only into a separate newly owned profile.
            if (profiles.All.Count != 1 || !profiles.Current.IsMain || entryCount != 0)
                throw new ReviewCheckException("Only the original independent Main without accepted ledger entries can host new cold profiles.");
            await SaveLoadedColdNewAsync(basePath, new ColdBase(profiles.Current.Id, profiles.All.Select(p => p.Id).ToArray(),
                ColdHash(await ColdRawAsync(services, ct))), ct);
        }
        var original = await ReadColdAsync<ColdBase>(basePath, ct);
        var preparedPath = Path.Combine(root, shape + "-prepared.json");
        if (File.Exists(preparedPath)) throw new ReviewCheckException("Cold setup already accepted; never repeat it.");
        var profilePath = Path.Combine(root, shape + "-profile.json");
        var source = Path.Combine(Path.GetDirectoryName(root)!, "current-home-" + LoadedColdSourceBaseline, LoadedColdSourceRun, shape);
        ColdStage(shape, "audit-retained-source");
        var sourceFiles = CurrentHomeFileDigests(source);
        _ = await AuditCurrentHomeSourceAsync(source, adopted: true, ct);
        var profileName = "D574 " + shape + " " + Path.GetFileName(root)[^8..];
        ColdStage(shape, "open-existing-donor");
        await using var donor = await OpenLoadedColdDonorAsync(source, ct);
        ColdStage(shape, "read-original-source");
        var sourceRows = await ColdRawAsync(donor.Services, ct);
        var sourceCookies = await ColdCookiesAsync(donor.Services, ct);
        ColdStage(shape, "original-profile-admission");
        var existing = profiles.All.Where(p => p.Name == profileName).ToArray();
        if (existing.Length > 1) throw new ReviewCheckException("Ambiguous owned cold profile.");
        LocalProfile owned;
        if (File.Exists(profilePath))
        {
            var receipt = await ReadColdAsync<ColdProfile>(profilePath, ct);
            owned = profiles.All.Single(p => p.Id == receipt.Id && p.Name == profileName);
            if (receipt.Shape != shape || receipt.SourceRowsHash != ColdHash(sourceRows)) throw new ReviewCheckException("Original cold source changed.");
        }
        else
        {
            var creation = Path.Combine(root, shape + "-creation-request.json");
            if (!File.Exists(creation)) await SaveLoadedColdNewAsync(creation, new { Shape = shape, SourceRowsHash = ColdHash(sourceRows), OneNewProfile = true }, ct);
            owned = existing.SingleOrDefault() ?? await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), profileName, ct);
            await SaveLoadedColdNewAsync(profilePath, new ColdProfile(owned.Id, shape, ColdHash(sourceRows)), ct);
        }
        ColdStage(shape, "select-owned-target");
        if (profiles.Current.Id != owned.Id && !await profiles.SwitchToAsync(owned)) throw new ReviewCheckException("Owned profile switch refused.");
        var restored = Path.Combine(root, shape + "-restore-accepted.json");
        var previouslyAcceptedRestore = File.Exists(restored);
        if (!File.Exists(restored))
        {
            var requested = Path.Combine(root, shape + "-restore-request.json");
            var continued = File.Exists(requested);
            if (continued)
            {
                // D-575: only the original missing-preference preflight is eligible. Prove
                // no restore intent or populated target exists; never replay selected data.
                await VerifyUnacceptedColdRestoreAsync(services, owned, root, shape, ct);
                if (File.Exists(Path.Combine(root, shape + "-corrected-restore-request.json")))
                    throw new ReviewCheckException("Observe the corrected restore outcome; never repeat its request.");
            }
            var sourceBackup = new BackupService(donor.Services.GetServices<IBackupSource>(), services.GetRequiredService<IAppEnvironment>(),
                new InMemorySettingsStore(), services.GetRequiredService<TimeProvider>(), new BackupOptions());
            ColdStage(shape, "original-package-in-memory");
            var package = await sourceBackup.CreatePackageAsync(cancellationToken: ct);
            try
            {
                await SaveLoadedColdNewAsync(continued ? Path.Combine(root, shape + "-corrected-restore-request.json") : requested,
                    new { Profile = owned.Id, SourceRowsHash = ColdHash(sourceRows), MemoryOnlyPackage = true,
                        PreviouslyRejectedBeforeRestoreIntent = continued }, ct);
                var memory = new LoadedColdPackage(package);
                ColdStage(shape, "original-protected-restore");
                await services.GetRequiredService<IBackupService>().RestoreAsync(memory, memory.File, cancellationToken: ct);
                await SaveLoadedColdNewAsync(restored, new { Profile = owned.Id, OriginalProtectedPackageReceiverReturned = true }, ct);
            }
            finally { CryptographicOperations.ZeroMemory(package); }
        }
        ColdStage(shape, "compare-complete-copied-rows");
        var copiedRows = await ColdRawAsync(services, ct);
        if (ColdHash(copiedRows) != ColdHash(sourceRows))
        {
            // A prior accepted restore can already have published its own derived cache before
            // an app-owned permission rationale interrupts focus. Preserve that success; admit
            // only exact authoritative rows, then validate the actual current cookies around
            // the remaining read. Never replay the restore or claim old cache bytes are equal.
            if (!previouslyAcceptedRestore || !await ColdAuthoritativeRowsEqualAsync(services, sourceRows, copiedRows, ct))
                throw new ReviewCheckException("Original restored typed rows differ before any warm publication.");
        }
        var beforeWarm = await ColdCookiesAsync(services, ct);
        if (sourceCookies.Financial != beforeWarm.Financial || sourceCookies.Source != beforeWarm.Source)
            throw new ReviewCheckException("Original accepted source cookies differ; no derived normalization is permitted.");
        var beforeRows = copiedRows;
        var beforePath = Path.Combine(root, shape + "-warm-validation-before-" + Android.OS.Process.MyPid() + ".json");
        await SaveLoadedColdNewAsync(beforePath, new { ActualTargetRowsHash = ColdHash(beforeRows), OriginalSourceRowsHash = ColdHash(sourceRows),
            ActualTargetCookies = beforeWarm, OriginalSourceCookies = sourceCookies, AcceptedRestoreNotReplayed = previouslyAcceptedRestore,
            AllOtherAuthoritativeRowsMatched = await ColdAuthoritativeRowsEqualAsync(services, sourceRows, copiedRows, ct),
            OriginalDisplayUnitStartupAccepted = _loadedColdOriginalUnitMigration }, ct);
        ColdStage(shape, "original-warm-publication");
        if ((await services.GetRequiredService<ZananceStore>().GetSettingsAsync(ct)).DisplayUnits is null)
        {
            // Finish the original one-time profile load before measuring a later cold start.
            // This copies only the existing owned legacy display choice, never a money amount.
            Presentation.DisplayUnitPreferences.Load(services.GetRequiredService<ZananceStore>());
        }
        // A direct ProfileService switch opens storage but does not replace the old shell.
        // The original UI switch wrapper also calls ShowCurrentProfile: do that before
        // capturing this new profile's presentation, even when the prior page was Home.
        app.ShowCurrentProfile();
        var page = await LoadedColdStartNative.WaitOriginalHomeAsync(app, ct);
        if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Original warm preparation tree missing.");
        await WaitLayoutAsync(native, ct);
        while (Platform.CurrentActivity?.HasWindowFocus != true) await Task.Delay(100, ct);
        RequireHomeWindowFocus();
        ColdStage(shape, "validate-original-warm-source");
        await ValidateColdWarmSourceAsync(services, beforeRows, beforeWarm, ct);
        var vm = (HomeViewModel)page.BindingContext;
        var expectedCount = shape == "reference" ? Workload.Reference.Entries : Workload.Tenfold.Entries;
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
            if (await db.Entries.CountAsync(ct) != expectedCount) throw new ReviewCheckException("Original complete workload row count differs.");
        var after = await ColdCookiesAsync(services, ct);
        if (sourceCookies.Financial != beforeWarm.Financial || sourceCookies.Source != beforeWarm.Source)
            throw new ReviewCheckException("Original restore changed a protected financial-source cookie.");
        ColdStage(shape, "recheck-unchanged-original-files");
        await donor.DisposeAsync();
        if (!sourceFiles.SequenceEqual(CurrentHomeFileDigests(source))) throw new ReviewCheckException("Retained source files changed during original portable copying.");
        var boot = (await File.ReadAllTextAsync("/proc/sys/kernel/random/boot_id", ct)).Trim();
        await SaveLoadedColdNewAsync(preparedPath, new ColdPrepared(owned.Id, shape, DateOnly.FromDateTime(
            services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime).ToString("yyyy-MM-dd"), ColdHash(await ColdRawAsync(services, ct)),
            CapturePresentation(vm), after.Financial, after.Source, boot, typeof(ZananceStore).Module.ModuleVersionId,
            typeof(Vafadar.Zanance.Core.Ledger.LedgerEntry).Module.ModuleVersionId, _loadedColdOriginalUnitMigration), ct);
    }

    private static async Task<bool> ColdAuthoritativeRowsEqualAsync(IServiceProvider services, string expected, string actual, CancellationToken ct)
    {
        var previous = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(expected)!;
        var current = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(actual)!;
        foreach (var name in new[] { "HomeLedgerReadState", "HomeLedgerReadBlocks", "HomeLedgerDirtyBlocks" })
        {
            if (!previous.Remove(name) || !current.Remove(name)) return false;
        }
        if (previous.TryGetValue("Settings", out var originalSettings) && current.TryGetValue("Settings", out var currentSettings)
            && originalSettings.Count == 1 && currentSettings.Count == 1 && originalSettings[0] != currentSettings[0])
        {
            await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA table_info(Settings)"; var columns = new List<string>();
            using (var reader = await command.ExecuteReaderAsync(ct)) while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(1));
            var accepted = LoadedColdStartPolicy.IsOriginalDisplayUnitStartup(columns, originalSettings[0], currentSettings[0],
                Preferences.Default.Get("display_units", string.Empty), services.GetRequiredService<TimeProvider>().GetUtcNow(), out var admission);
            LoadedColdUnitAdmission = admission;
            if (accepted)
            {
                // The installed original App.PrepareStartupAsync/DisplayUnitPreferences.Load owns
                // this existing null migration. No unit, amount, other setting or source cookie
                // is replaced by this observer; keep its original actual saved row as the baseline.
                previous["Settings"] = currentSettings; _loadedColdOriginalUnitMigration = true;
            }
        }
        if (JsonSerializer.Serialize(previous) == JsonSerializer.Serialize(current)) return true;
        var differences = previous.Keys.Union(current.Keys, StringComparer.Ordinal).Where(name =>
            !previous.TryGetValue(name, out var before) || !current.TryGetValue(name, out var after) || !before.SequenceEqual(after))
            .Select(name =>
            {
                previous.TryGetValue(name, out var before); current.TryGetValue(name, out var after);
                var ordinals = new SortedSet<int>();
                if (before is not null && after is not null && before.Count == after.Count)
                    for (var row = 0; row < before.Count; row++)
                    {
                        if (before[row] == after[row]) continue;
                        using var left = JsonDocument.Parse(before[row]); using var right = JsonDocument.Parse(after[row]);
                        if (left.RootElement.GetArrayLength() != right.RootElement.GetArrayLength()) continue;
                        for (var column = 0; column < left.RootElement.GetArrayLength(); column++)
                            if (left.RootElement[column].GetRawText() != right.RootElement[column].GetRawText()) ordinals.Add(column);
                    }
                return new { Table = name, BeforeCount = before?.Count, AfterCount = after?.Count, ChangedColumnOrdinals = ordinals.ToArray() };
            }).ToArray();
        LoadedColdSourceDifference = JsonSerializer.Serialize(differences);
        return false;
    }

    private static async Task VerifyUnacceptedColdRestoreAsync(IServiceProvider services, LocalProfile owned,
        string root, string shape, CancellationToken ct)
    {
        var profiles = services.GetRequiredService<ProfileService>();
        if (profiles.Current != owned || owned.IsMain) throw new ReviewCheckException("Only the recorded fresh target can reconcile preflight refusal.");
        var capture = profiles.CaptureStorage(); capture.Validate();
        var original = capture.CurrentOriginalPath;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var validate = location.CaptureValidation();
        using var owner = await location.Access.AcquireExclusiveAsync(ct);
        void Check() { owner.Validate(); validate(); capture.Validate(); }
        var identity = (await new SqliteDatabaseIdentityCatalogue(Path.Combine(Path.GetDirectoryName(original)!,
            "database-identities.sqlite"), Check).ReadAsync(ct)).Single(row => row.LegacyProfileId == owned.Id);
        var history = await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, Check).ReadAsync(ct);
        if (history.Count != 1 || history[0].Sequence != 0 || !history[0].Selected
            || history[0].CandidateGeneration != identity.CandidateGeneration
            || location.Path != original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db")
            throw new ReviewCheckException("Restore ancestry already changed; reconcile the actual outcome without another restore.");
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, ct);
        await db.Database.OpenConnectionAsync(ct); ProtectedSqliteAccess.Require((Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection());
        if (await db.Entries.CountAsync(ct) != 0 || await db.Accounts.CountAsync(ct) != 0)
            throw new ReviewCheckException("The target already has original financial data; no restore replay is authorized.");
        Check();
        var receipt = Path.Combine(root, shape + "-rejected-preflight-verified.json");
        if (!File.Exists(receipt)) await SaveLoadedColdNewAsync(receipt, new { Profile = owned.Id, InitialSelectedHistoryOnly = true,
            RestoreIntents = 0, Entries = 0, Accounts = 0, NoAcceptedRestoreReplayed = true }, ct);
    }

    [SupportedOSPlatform("android36.0")]
    private static async Task CorrectColdPresentationAsync(App app, IServiceProvider services, string root, string shape, CancellationToken ct)
    {
        if (shape != "tenfold") throw new ReviewCheckException("Only the unfinished tenfold presentation admission may continue.");
        var originalPath = Path.Combine(root, shape + "-prepared.json");
        var originalFile = await File.ReadAllTextAsync(originalPath, ct);
        var prepared = JsonSerializer.Deserialize<ColdPrepared>(originalFile)!;
        var reference = await ReadColdAsync<ColdPrepared>(Path.Combine(root, "reference-prepared.json"), ct);
        var resultPath = Path.Combine(root, shape + "-presentation-correction.json");
        if (File.Exists(resultPath) || File.Exists(Path.Combine(root, shape + "-cold-result.json"))
            || prepared.Presentation != reference.Presentation || prepared.ProfileId == reference.ProfileId
            || prepared.SnapshotHash == reference.SnapshotHash || services.GetRequiredService<ProfileService>().Current.Id != prepared.ProfileId)
            throw new ReviewCheckException("An original miscaptured old-shell baseline and unfinished selected tenfold profile are required.");
        var sourceProofs = Directory.GetFiles(root, "tenfold-refused-native-source-*.json");
        var hasOriginalFullSourceRefusal = false;
        foreach (var path in sourceProofs)
        {
            using var proof = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
            var value = proof.RootElement;
            if (value.GetProperty("ProfileId").GetString() == prepared.ProfileId
                && new[] { "AllPreparedTypedRowsMatched", "ProtectedFinancialCookieMatched", "ProtectedReadCookieMatched", "FinalCacheCookieMatched" }
                    .All(key => value.GetProperty(key).GetBoolean())) hasOriginalFullSourceRefusal = true;
        }
        if (!hasOriginalFullSourceRefusal) throw new ReviewCheckException("Preserved actual refused-lifetime source proof required.");
        await SaveLoadedColdNewAsync(Path.Combine(root, shape + "-presentation-correction-request.json"),
            new { prepared.ProfileId, OriginalPreparedFileHash = ColdHash(originalFile), NoRestoreSeedOrFinancialReplay = true }, ct);
        async Task RequireSource()
        {
            var cookies = await ColdCookiesAsync(services, ct);
            if (prepared.SnapshotHash != ColdHash(await ColdRawAsync(services, ct)) || cookies.Financial != prepared.Financial
                || cookies.Source != prepared.Source || cookies.Published != cookies.Source)
                throw new ReviewCheckException("The entire accepted tenfold source must stay exact during presentation-only correction.");
        }
        await RequireSource();
        app.ShowCurrentProfile();
        var page = await LoadedColdStartNative.WaitOriginalHomeAsync(app, ct);
        if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Original correction page missing.");
        await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
        var vm = (HomeViewModel)page.BindingContext;
        var presentation = CapturePresentation(vm);
        if (presentation == prepared.Presentation || vm.Accounts.Count != Workload.Tenfold.Accounts)
            throw new ReviewCheckException("Fresh original tenfold publication must replace the old reference cards.");
        await RequireSource();
        if (await File.ReadAllTextAsync(originalPath, ct) != originalFile) throw new ReviewCheckException("Original failed baseline must remain immutable.");
        await SaveLoadedColdNewAsync(resultPath, new ColdPresentationCorrection(ColdHash(originalFile), prepared.ProfileId,
            prepared.SnapshotHash, prepared.Financial, prepared.Source, presentation, Workload.Reference.Accounts, vm.Accounts.Count), ct);
    }

    [SupportedOSPlatform("android36.0")]
    private static async Task ObserveLoadedColdAsync(App app, Activity activity, IServiceProvider services,
        string root, string run, string shape, CancellationToken ct)
    {
        var prepared = await ReadColdAsync<ColdPrepared>(Path.Combine(root, shape + "-prepared.json"), ct);
        var resultPath = Path.Combine(root, shape + "-cold-result.json");
        if (File.Exists(resultPath)) throw new ReviewCheckException("Cold result already accepted; do not replay it.");
        var profiles = services.GetRequiredService<ProfileService>();
        if (profiles.Current.Id != prepared.ProfileId || prepared.Shape != shape
            || prepared.DataModule != typeof(ZananceStore).Module.ModuleVersionId
            || prepared.CoreModule != typeof(Vafadar.Zanance.Core.Ledger.LedgerEntry).Module.ModuleVersionId
            || prepared.Day != DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime).ToString("yyyy-MM-dd"))
            throw new ReviewCheckException("Original cold profile, source, code or real-day scope changed.");
        var shellBoundary = activity.Intent?.GetStringExtra("review_phase") == "cold-shell-measure";
        var request = activity.Intent?.GetStringExtra("review_request");
        if (shellBoundary)
        {
            if (request is null || !Regex.IsMatch(request, "^[0-9a-f]{32}$")) throw new ReviewCheckException("One original shell request required.");
            // CreateNew is durable before the draw: automatic activity restoration cannot
            // replay this request, even in a later process or after an interrupted observer.
            await SaveLoadedColdNewAsync(Path.Combine(root, "shell-consumed-" + request + ".json"),
                new { Request = request, Run = run, Shape = shape, Pid = Android.OS.Process.MyPid() }, ct);
        }
        var page = await LoadedColdStartNative.WaitOriginalHomeAsync(app, ct);
        var vm = (HomeViewModel)page.BindingContext;
        var expectedPresentation = prepared.Presentation;
        var correctionPath = Path.Combine(root, shape + "-presentation-correction.json");
        if (File.Exists(correctionPath))
        {
            var correction = await ReadColdAsync<ColdPresentationCorrection>(correctionPath, ct);
            if (shape != "tenfold" || correction.ProfileId != prepared.ProfileId || correction.SnapshotHash != prepared.SnapshotHash
                || correction.Financial != prepared.Financial || correction.Source != prepared.Source
                || correction.OriginalPreparedFileHash != ColdHash(await File.ReadAllTextAsync(Path.Combine(root, shape + "-prepared.json"), ct)))
                throw new ReviewCheckException("Original presentation-only correction is not bound to this immutable prepared source.");
            expectedPresentation = correction.Presentation;
        }
        if (CapturePresentation(vm) != expectedPresentation) throw new ReviewCheckException("Original loaded financial presentation differs.");
        RequireHomeWindowFocus();
        var draw = await LoadedColdStartNative.ReportOriginalReadyDrawAsync(activity, page, ct);
        await SaveLoadedColdNewAsync(Path.Combine(root, shape + "-ready-draw-" + Android.OS.Process.MyPid() + ".json"),
            new { DrawNanoseconds = draw, Pid = Android.OS.Process.MyPid(), OriginalReportFullyDrawnCalled = true }, ct);
        if (shellBoundary)
        {
            var shellCookies = await ColdCookiesAsync(services, ct);
            var shellSourceMatched = prepared.SnapshotHash == ColdHash(await ColdRawAsync(services, ct))
                && shellCookies.Financial == prepared.Financial && shellCookies.Source == prepared.Source && shellCookies.Published == shellCookies.Source;
            var shellBoot = (await File.ReadAllTextAsync("/proc/sys/kernel/random/boot_id", ct)).Trim();
            var entry = LoadedColdStartNative.ActivityEntry; var ready = LoadedColdStartNative.ReportFrame;
            var shellFacts = new LoadedColdShellFacts(request!, activity.Intent!.GetStringExtra("review_request")!,
                run, activity.Intent.GetStringExtra("review_run")!, shape, activity.Intent.GetStringExtra("review_shape")!,
                Android.OS.Process.MyPid(), Android.OS.Process.MyUid(), activity.Intent.GetBooleanExtra("review_process_absent", false), true,
                activity.Intent.GetLongExtra("review_launch_monotonic", 0), activity.Intent.GetLongExtra("review_launch_boottime", 0),
                entry.Monotonic, entry.BootTime, ready.Monotonic, ready.BootTime,
                activity.Intent.GetStringExtra("review_launch_boot") ?? "", shellBoot,
                vm.Loading.IsReady && page.PendingPublication.IsCompletedSuccessfully && page.FindByName<Button>("AddAction").IsEnabled,
                profiles.Current.Id == prepared.ProfileId && prepared.Boot == shellBoot && shellSourceMatched);
            var shellDuration = LoadedColdShellPolicy.RequireShellToReadyMilliseconds(shellFacts);
            await SaveLoadedColdNewAsync(resultPath, new { Shape = shape, prepared.ProfileId, ShellFacts = shellFacts,
                ShellCommandToReadyMilliseconds = shellDuration, ReadyDrawNanoseconds = draw,
                OSLaunchMetricAccepted = false, ShellDispatchOverheadIncluded = true,
                OriginalSourceMatchedAllTypedRows = shellSourceMatched, FinancialCommands = 0,
                NoBeforeBoundaryDatabaseOrOracleReadByObserver = true, GeneralTwoSecondOrStabilityAccepted = false }, ct);
            return;
        }
        LoadedColdStartNative.Launch launch;
        var attempts = 0;
        while (true)
        {
            try { launch = LoadedColdStartNative.ReadCurrent(activity, run, shape); break; }
            catch (InvalidOperationException) when (++attempts < 30) { await Task.Delay(100, ct); }
        }
        while (launch.FullyDrawn == 0)
        {
            await Task.Delay(100, ct);
            launch = LoadedColdStartNative.ReadCurrent(activity, run, shape);
        }
        // All database/oracle work follows the native timing boundary and cannot warm this cold process beforehand.
        var after = await ColdCookiesAsync(services, ct);
        var sourceMatched = prepared.SnapshotHash == ColdHash(await ColdRawAsync(services, ct))
            && after.Financial == prepared.Financial && after.Source == prepared.Source && after.Published == after.Source;
        var boot = (await File.ReadAllTextAsync("/proc/sys/kernel/random/boot_id", ct)).Trim();
        var facts = new LoadedColdStartFacts(Android.OS.Process.MyPid(), launch.Pid, 10381, launch.Uid,
            activity.Intent?.GetBooleanExtra("review_process_absent", false) == true, launch.ColdActivity, launch.Complete,
            launch.Start, launch.FirstFrame, draw, launch.FullyDrawn, vm.Loading.IsReady, page.PendingPublication.IsCompletedSuccessfully,
            page.FindByName<Button>("AddAction").IsEnabled, prepared.Boot, boot, profiles.Current.Id == prepared.ProfileId, sourceMatched);
        var duration = LoadedColdStartPolicy.RequireLoadedColdMilliseconds(facts);
        await SaveLoadedColdNewAsync(resultPath, new { Shape = shape, prepared.ProfileId, Facts = facts,
            LoadedColdMilliseconds = duration, InitialCoveredFrameMilliseconds = (launch.FirstFrame - launch.Start) / 1_000_000d,
            OriginalSourceMatchedAllTypedRows = sourceMatched, ProcessColdWarmOsCaches = true,
            NoBeforeBoundaryDatabaseOrOracleReadByObserver = true, FinancialCommands = 0,
            GeneralTwoSecondOrStabilityAccepted = false }, ct);
    }

    /// <summary>Preserves the original prepared source after a native-record refusal; never repeats its draw or launch.</summary>
    internal static async Task<object?> PreserveRefusedColdSourceAsync(IServiceProvider services, string root, string shape, CancellationToken ct)
    {
        var path = Path.Combine(root, shape + "-prepared.json");
        if (!File.Exists(path) || Package != "pro.vafadar.zanance.androidhomereview" || Android.OS.Process.MyUid() != 10381) return null;
        try
        {
            var prepared = await ReadColdAsync<ColdPrepared>(path, ct);
            if (services.GetRequiredService<ProfileService>().Current.Id != prepared.ProfileId) return new { WrongProfileRefused = true };
            var cookies = await ColdCookiesAsync(services, ct);
            var proof = new { Pid = Android.OS.Process.MyPid(), prepared.ProfileId,
                AllPreparedTypedRowsMatched = prepared.SnapshotHash == ColdHash(await ColdRawAsync(services, ct)),
                ProtectedFinancialCookieMatched = cookies.Financial == prepared.Financial,
                ProtectedReadCookieMatched = cookies.Source == prepared.Source, FinalCacheCookieMatched = cookies.Published == cookies.Source,
                ObservationOnly = true, NoLaunchDrawRestoreOrFinancialReplay = true };
            await SaveLoadedColdNewAsync(Path.Combine(root, shape + "-refused-native-source-" + Android.OS.Process.MyPid() + ".json"), proof, ct);
            return proof;
        }
        catch (Exception error) { return new { SourceObservationErrorType = error.GetType().FullName, NoReplay = true }; }
    }

    private static async Task FinishLoadedColdAsync(App app, IServiceProvider services, string root, CancellationToken ct)
    {
        foreach (var shape in new[] { "reference", "tenfold" })
            if (!File.Exists(Path.Combine(root, shape + "-cold-result.json"))) throw new ReviewCheckException("Both original cold observations must finish before retirement.");
        var original = await ReadColdAsync<ColdBase>(Path.Combine(root, "base.json"), ct);
        var profiles = services.GetRequiredService<ProfileService>(); var previous = profiles.All.Single(p => p.Id == original.Id);
        if (profiles.Current.Id != previous.Id && !await profiles.SwitchToAsync(previous)) throw new ReviewCheckException("Original independent base return refused.");
        foreach (var shape in new[] { "reference", "tenfold" })
        {
            var receipt = await ReadColdAsync<ColdProfile>(Path.Combine(root, shape + "-profile.json"), ct);
            var owned = profiles.All.SingleOrDefault(p => p.Id == receipt.Id);
            if (owned is not null) await profiles.DeleteReviewedAsync(profiles.ReadWorkReview(), owned, ct);
        }
        app.ShowCurrentProfile();
        if (!profiles.All.Select(p => p.Id).SequenceEqual(original.ProfileIds) || ColdHash(await ColdRawAsync(services, ct)) != original.SnapshotHash)
            throw new ReviewCheckException("Original independent Main or catalogue did not return exactly.");
        await SaveLoadedColdNewAsync(Path.Combine(root, "finished.json"), new { OnlyTwoFreshProfilesRetired = true,
            OriginalMainRowsAndCatalogueMatched = true, RetainedOriginalSourcesNeverSeeded = true, ShippingMainTouched = false }, ct);
    }

    private sealed class LoadedColdPackage(byte[] package) : IBackupStorage
    {
        /// <summary>Only this owned immutable package can be opened by the original reader.</summary>
        internal BackupFileInfo File { get; } = new("owned-memory", "loaded-cold.vbak", package.Length, DateTimeOffset.UtcNow);
        /// <inheritdoc />
        public string Id => "owned-loaded-cold-memory";
        /// <inheritdoc />
        public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BackupFileInfo>>([File]);
        /// <inheritdoc />
        public Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default) => fileId == File.Id
            ? Task.FromResult<Stream>(new MemoryStream(package, writable: false)) : throw new IOException("Foreign memory package refused.");
        /// <inheritdoc />
        public Task<BackupFileInfo> UploadAsync(string name, Stream content, CancellationToken cancellationToken = default) => throw new IOException("No upload in cold review.");
        /// <inheritdoc />
        public Task DeleteAsync(string fileId, CancellationToken cancellationToken = default) => throw new IOException("No portable deletion in cold review.");
    }

    private sealed class ColdDonor(ServiceProvider services, SqlCipherProfileGeneration generation) : IAsyncDisposable
    {
        /// <summary>The actual original protected donor context, never a financial source substitute.</summary>
        internal ServiceProvider Services { get; } = services;
        private bool disposed;
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (disposed) return; disposed = true; generation.Dispose(); await Services.DisposeAsync();
        }
    }

    private static async Task<ColdDonor> OpenLoadedColdDonorAsync(string folder, CancellationToken ct)
    {
        RequireRoot(folder); var original = Path.Combine(folder, "fixture.db");
        var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        try
        {
            var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
            using var owner = await location.Access.AcquireExclusiveAsync(ct);
            var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => RequireRoot(folder));
            var identity = (await identities.ReadAsync(ct)).Single();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
            var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                _ => throw new ReviewCheckException("No donor restore or preparation is allowed."), keys, new AndroidGenerationFileDurability(folder),
                owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
            generation.Publish(location, owner); return new ColdDonor(data, generation);
        }
        catch { await data.DisposeAsync(); throw; }
    }

    private static async Task<ColdCookies> ColdCookiesAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT f.Revision,s.Revision,s.PublishedRevision,s.SchemaVersion FROM HomeFinancialRevision f CROSS JOIN HomeLedgerReadState s WHERE f.Id=1 AND s.Id=1";
        string financial; string source; string? published; long? schema;
        using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new InvalidDataException("Original protected cookies missing.");
            financial = reader.GetString(0); source = reader.GetString(1); published = reader.IsDBNull(2) ? null : reader.GetString(2);
            schema = reader.IsDBNull(3) ? null : reader.GetInt64(3); if (await reader.ReadAsync(ct)) throw new InvalidDataException("Ambiguous protected cookies.");
        }
        if (!Guid.TryParseExact(financial, "N", out _) || !Guid.TryParseExact(source, "N", out _)
            || published is not null && !Guid.TryParseExact(published, "N", out _)) throw new InvalidDataException("Malformed protected cookies.");
        command.CommandText = "PRAGMA schema_version"; return new(financial, source, published, schema, Convert.ToInt64(await command.ExecuteScalarAsync(ct)));
    }

    private static async Task ValidateColdWarmSourceAsync(IServiceProvider services, string beforeRows, ColdCookies before, CancellationToken ct)
    {
        var after = await ColdCookiesAsync(services, ct);
        if (before.Financial != after.Financial || before.Source != after.Source || after.Published != after.Source || after.Schema != after.ActualSchema)
            throw new ReviewCheckException("Actual protected-source cookies changed or final cache cookie is not bound to them.");
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var original = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct);
            var snapshot = await services.GetRequiredService<ZananceStore>().GetHomeSnapshotAsync(
                DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime), 0, ct);
            if (JsonSerializer.Serialize(original) != JsonSerializer.Serialize(snapshot.Entries.OrderBy(e => e.Id)))
                throw new ReviewCheckException("Original complete32-field ledger differs from independent EF rows.");
            await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT CodecVersion,RowCount FROM HomeLedgerReadState WHERE Id=1";
            long expected;
            using (var state = await command.ExecuteReaderAsync(ct))
            {
                if (!await state.ReadAsync(ct) || state.IsDBNull(0) || state.GetInt64(0) != 1 || state.IsDBNull(1))
                    throw new ReviewCheckException("Original derived publication is incomplete.");
                expected = state.GetInt64(1);
            }
            command.CommandText = "SELECT COUNT(*) FROM HomeLedgerDirtyBlocks";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct)) != 0) throw new ReviewCheckException("Original derived publication is still dirty.");
            command.CommandText = "SELECT RowCount,Payload,Digest FROM HomeLedgerReadBlocks ORDER BY BlockId";
            long rows = 0;
            using (var blocks = await command.ExecuteReaderAsync(ct)) while (await blocks.ReadAsync(ct))
            {
                var count = blocks.GetInt64(0);
                if (count is < 1 or > 512 || blocks.IsDBNull(1) || blocks.IsDBNull(2))
                    throw new ReviewCheckException("Original derived block is incomplete.");
                var payload = blocks.GetFieldValue<byte[]>(1); var digest = blocks.GetFieldValue<byte[]>(2);
                if (digest.Length != 32 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), digest))
                    throw new ReviewCheckException("Original derived block digest differs.");
                rows += count;
            }
            if (rows != expected || rows != original.Count) throw new ReviewCheckException("Original derived complete-row count differs.");
        }
        // Actual before/after source cookies and the complete codec above remain mandatory.
        // Only the separately bound original null-unit migration may accompany this first read.
        if (!await ColdAuthoritativeRowsEqualAsync(services, beforeRows, await ColdRawAsync(services, ct), ct))
            throw new ReviewCheckException("Warm source preparation changed original authoritative rows.");
    }
}
#endif
