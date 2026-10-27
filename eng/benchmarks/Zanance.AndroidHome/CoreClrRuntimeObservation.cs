#if CORECLR_RUNTIME_REVIEW
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>Read-only, shell-only evidence from the new experimental identity after its original covered startup.</summary>
[ContentProvider([CoreClrRuntimeReadPolicy.Authority], Exported = true,
    ReadPermission = Android.Manifest.Permission.Dump, WritePermission = Android.Manifest.Permission.Dump)]
public sealed class CoreClrRuntimeObservation : ContentProvider
{
    private static WeakReference<Activity>? originalActivity;
    private static readonly SemaphoreSlim Reads = new(1, 1);
    private static WeakReference<AppShell>? observedShell;
    private static long retainedAccountActivations;
    private static string? lastRetainedNavigationSource;

    private static void ObserveNavigation(object? sender, ShellNavigatedEventArgs args)
    {
        if (sender is AppShell { CurrentPage: Features.Accounts.AccountsPage }
            && args.Source is ShellNavigationSource.ShellItemChanged or ShellNavigationSource.ShellSectionChanged
                or ShellNavigationSource.ShellContentChanged)
        {
            retainedAccountActivations++;
            lastRetainedNavigationSource = args.Source.ToString();
        }
    }

    /// <summary>Records no services, source, window transition or financial operation.</summary>
    internal static void Attach(Activity activity) => originalActivity = new(activity);

    /// <inheritdoc />
    public override bool OnCreate() => true;

    /// <inheritdoc />
    public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection,
        string[]? selectionArgs, string? sortOrder)
    {
        var parts = uri.PathSegments?.ToArray() ?? [];
        var coreClr = AppContext.TryGetSwitch("Microsoft.Android.Runtime.RuntimeFeature.IsCoreClrRuntime", out var enabled) && enabled;
        if (projection is not null || selection is not null || selectionArgs is not null || sortOrder is not null
            || uri.Authority != CoreClrRuntimeReadPolicy.Authority || parts.Length != 3 || parts[0] != "v1"
            || !CoreClrRuntimeReadPolicy.IsOwnedShellRequest(Context?.PackageName ?? "", Android.OS.Process.MyUid(),
                Android.OS.Binder.CallingUid, (int)Android.OS.Build.VERSION.SdkInt, Android.OS.Build.Model ?? "",
                coreClr, parts[1], parts[2])) throw new Java.Lang.SecurityException("Owned experimental shell source port required.");
        string packet;
        try { packet = Task.Run(() => ReadAsync(parts[1], parts[2])).GetAwaiter().GetResult(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // An ordinary refused observation must not escape across CoreCLR's ContentProvider JNI boundary.
            // Preserve content-free failure identities; neither retry a source write nor export exception messages.
            packet = JsonSerializer.Serialize(new { Run = parts[1], Stage = parts[2], Pid = Android.OS.Process.MyPid(),
                Uid = Android.OS.Process.MyUid(), ObservationRefused = true, SourceReadState = "unqualified",
                Failures = StartupFailureRecords.Capture(error) });
        }
        var result = new MatrixCursor(["packet"]);
        result.NewRow()?.Add(new Java.Lang.String(packet));
        return result;
    }

    private static async Task<string> ReadAsync(string run, string stage)
    {
        await Reads.WaitAsync();
        try
        {
            // Readiness is inspected, never awaited or caused by a provider that Android starts cold.
            var app = Microsoft.Maui.Controls.Application.Current as App;
            var window = await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var weakAlive = originalActivity?.TryGetTarget(out var activity) == true;
                var current = Platform.CurrentActivity;
                return new { WeakActivityAlive = weakAlive, PlatformActivityPresent = current is not null,
                    Focused = current?.PackageName == CoreClrRuntimeReadPolicy.Package && current.HasWindowFocus };
            });
            var ready = app?.WaitForStartupAsync().IsCompletedSuccessfully == true;
            if (!ready || !window.Focused)
                return JsonSerializer.Serialize(new { Run = run, Stage = stage, Pid = Android.OS.Process.MyPid(),
                    Uid = Android.OS.Process.MyUid(), ObservationRefused = true, SourceRead = false,
                    OriginalStartupReady = ready, Window = window });
            var services = IPlatformApplication.Current!.Services;
            var profiles = services.GetRequiredService<ProfileService>();
            var appLock = services.GetRequiredService<AppLockService>();
            return await CoreClrRuntimeReadPolicy.ReadOnlyWhenReadyAsync(true, window.Focused,
                !appLock.IsEnabled && !appLock.IsLocked, profiles.Current.IsMain && profiles.All.Count == 1,
                async () =>
                {
                    var capture = profiles.CaptureStorage(); capture.Validate();
                    var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                    var validate = location.CaptureValidation();
                    await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
                    await db.Database.OpenConnectionAsync();
                    var connection = (SqliteConnection)db.Database.GetDbConnection();
                    ProtectedSqliteAccess.Require(connection);
                    var before = await ReadBlockSchemaRetirement.CompleteFingerprintAsync(connection, CancellationToken.None);
                    var accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToArrayAsync();
                    var entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToArrayAsync();
                    // Only this bounded, freshly sealed fictitious source may leave the owned process.
                    if (accounts.Length > 2 || entries.Length > 4
                        || accounts.Any(a => a.CurrencyCode != "EUR" || !a.Name.StartsWith("D576_", StringComparison.Ordinal))
                        || entries.Any(e => e.Title is null || !e.Title.StartsWith("D576_", StringComparison.Ordinal)))
                        throw new InvalidOperationException("Only the bounded new fictitious cohort is eligible.");
                    var seal = Path.Combine(FileSystem.AppDataDirectory, "coreclr-runtime-source-seal.json");
                    if (stage == "seal" && !File.Exists(seal))
                    {
                        if (accounts.Length != 0 || entries.Length != 0) throw new InvalidOperationException("Seal precedes every financial command.");
                        using var file = new FileStream(seal, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                        JsonSerializer.Serialize(file, new Seal(run, Android.OS.Process.MyUid(), profiles.Current.Id));
                        file.Flush(flushToDisk: true);
                    }
                    if (!File.Exists(seal) || new FileInfo(seal).Length > 1024
                        || JsonSerializer.Deserialize<Seal>(await File.ReadAllTextAsync(seal)) != new Seal(run, 10399, ""))
                        throw new InvalidOperationException("Original empty-source admission seal required; never reseed or replace it.");
                    var after = await ReadBlockSchemaRetirement.CompleteFingerprintAsync(connection, CancellationToken.None);
                    capture.Validate(); validate();
                    if (before != after) throw new InvalidOperationException("Source changed during observation; retain refusal without another write.");
                    var navigation = await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var shell = app!.Windows.FirstOrDefault()?.Page as AppShell;
                        if (shell is not null && (observedShell?.TryGetTarget(out var previous) != true || !ReferenceEquals(previous, shell)))
                        {
                            // Observe the real event only; never refresh/navigate or synthesize a tab callback here.
                            shell.Navigated += ObserveNavigation; observedShell = new(shell);
                        }
                        var accountsPage = shell?.CurrentPage as Features.Accounts.AccountsPage;
                        return new { RetainedAccountActivations = retainedAccountActivations,
                            LastRetainedNavigationSource = lastRetainedNavigationSource,
                            AccountsPageIdentity = accountsPage is null ? (int?)null : RuntimeHelpers.GetHashCode(accountsPage),
                            OriginalAccountReadCompleted = accountsPage?.PendingPublication.IsCompletedSuccessfully,
                            OriginalAccountReadTask = accountsPage?.PendingPublication.Id };
                    });
                    return JsonSerializer.Serialize(new
                    {
                        Run = run, Stage = stage, Pid = Android.OS.Process.MyPid(), Uid = Android.OS.Process.MyUid(),
                        SourceFingerprint = after, CompleteSchemaHeaderStorageClassValueGuard = true,
                        Framework = RuntimeInformation.FrameworkDescription, CoreLib = typeof(object).Module.ModuleVersionId,
                        IsCoreClrRuntime = true, OriginalStartupReady = true, SourceReadOnly = true, Navigation = navigation,
                        Accounts = accounts.Select(a => new { a.Id, a.Name, a.CurrencyCode, a.OpeningBalance }),
                        Entries = entries.Select(e => new { e.Id, e.Kind, e.Date, e.AccountId, e.Amount, e.ToAccountId,
                            e.ToAmount, e.Title, e.CategoryId, e.RefundOfId, e.Source, e.Review })
                    });
                });
        }
        finally { Reads.Release(); }
    }

    private sealed record Seal(string Run, int Uid, string Profile);

    /// <inheritdoc />
    public override string? GetType(Android.Net.Uri uri) => "vnd.android.cursor.item/vnd.zanance.runtime-proof";
    /// <inheritdoc />
    public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
}
#endif
