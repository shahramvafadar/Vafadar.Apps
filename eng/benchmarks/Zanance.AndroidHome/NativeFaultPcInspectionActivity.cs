#if NATIVE_FAULT_PC_REVIEW
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;
using Android.OS;
using Android.Views;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>Reads only a named own signal record with an externally verified APK-bound code catalog, without starting a data review.</summary>
[Activity(Exported = true, Theme = "@style/Maui.SplashTheme")]
public sealed partial class NativeFaultPcInspectionActivity : Activity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Window?.AddFlags(WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
        if (savedInstanceState is not null || PackageName != "pro.vafadar.zanance.androidhomereview"
            || Intent?.GetIntExtra("review_fault_uid", -1) != Android.OS.Process.MyUid()) return;
        var run = Intent.GetStringExtra("review_run");
        var comparison = Intent.GetStringExtra("review_pass");
        var pid = Intent.GetIntExtra("review_fault_pid", -1);
        var apkHash = Intent.GetStringExtra("review_fault_apk_sha256");
        var catalog = Intent.GetStringExtra("review_fault_module_catalog");
        if (pid <= 0 || run is null || comparison is null || !Identifier().IsMatch(run) || !Identifier().IsMatch(comparison)
            || apkHash is null || !HashIdentity().IsMatch(apkHash) || catalog is null || catalog.Length > 24_000) return;
        _ = Task.Run(() => Inspect(run, comparison, pid, apkHash, catalog));
    }

    private void Inspect(string run, string comparison, int pid, string expectedApkHash, string catalog)
    {
        object packet;
        try
        {
            var apkPath = ApplicationInfo?.SourceDir ?? throw new InvalidDataException("Original installed package unavailable.");
            using (var apk = File.OpenRead(apkPath))
                if (Convert.ToHexStringLower(SHA256.HashData(apk)) != expectedApkHash)
                    throw new InvalidDataException("Actual installed observer package differs from the bound original package.");
            var catalogBytes = Convert.FromBase64String(catalog);
            Dictionary<byte, NativeFaultPcRecords.Module> modules;
            try
            {
                if (catalogBytes.Length > 16_384) throw new InvalidDataException("Bounded original module catalog required.");
                modules = JsonSerializer.Deserialize<Dictionary<byte, NativeFaultPcRecords.Module>>(catalogBytes)
                    ?? throw new InvalidDataException("Original verified code catalog required.");
            }
            finally { CryptographicOperations.ZeroMemory(catalogBytes); }
            var path = Path.Combine(FilesDir?.AbsolutePath ?? throw new InvalidDataException("Owned observation directory unavailable."),
                "native-fault-observations", run + "-" + comparison + "-" + pid.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".bin");
            var length = new FileInfo(path).Length;
            if (length is < 0 or > 32 * 72) throw new InvalidDataException("Original signal observation bound exceeded.");
            var uid = checked((uint)Android.OS.Process.MyUid());
            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => (byte)1, Architecture.X64 => (byte)2,
                _ => throw new InvalidDataException("Qualified original observation ABI required.")
            };
            NativeFaultPcRecords.Result? locations = null;
            if (length != 0)
            {
                using var stream = File.OpenRead(path);
                locations = NativeFaultPcRecords.Read(stream, checked((uint)pid), uid, run, architecture, modules);
            }
            object? exit = null;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                using var manager = (ActivityManager?)GetSystemService(ActivityService);
                var history = manager?.GetHistoricalProcessExitReasons(PackageName, pid, 4);
                ApplicationExitInfo? match = null;
                var matchCount = 0;
                if (history is not null)
                    foreach (var item in history)
                        if (item.Pid == pid && item.RealUid == uid) { match = item; ++matchCount; }
                if (matchCount == 1 && match is not null)
                    exit = new { ActualOsReason = (int)match.Reason, match.Status, match.Timestamp };
            }
            packet = new { Run = run, Comparison = comparison, OriginalPid = pid, OwnUid = uid,
                ActualInstalledAPK_SHA256 = expectedApkHash, InProcessSignalLocations = locations,
                ObservedRecordBytes = length, OriginalExitMetadata = exit,
                ModuleCatalogExternallyBoundToVerifiedAPK = true, NoOsTombstoneClaim = true,
                RequiresIndependentFatalCorrelation = true, NoDatabaseStartupOrFinancialOperation = true,
                NoMemoryRegistersOrAbsoluteStackExported = true };
        }
        catch (Exception error)
        {
            packet = new { Run = run, Comparison = comparison, OriginalPid = pid, Refused = true,
                ErrorType = error.GetType().FullName, NoDatabaseStartupOrFinancialOperation = true };
        }
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet);
        const int size = 1500;
        var count = (bytes.Length + size - 1) / size;
        for (var index = 0; index < count; ++index)
            Android.Util.Log.Info("ZananceFaultPcReport", $"{run}|{comparison}|{pid}|{index}/{count}|"
                + Convert.ToBase64String(bytes, index * size, Math.Min(size, bytes.Length - index * size)));
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)] private static partial Regex Identifier();
    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)] private static partial Regex HashIdentity();
}
#endif
