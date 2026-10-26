using Android.App;
using Android.OS;
using Android.Views;
using System.Runtime.Versioning;
using System.Text.Json;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

/// <summary>Reads an explicitly identified prior crash in this independent UID without starting MAUI or opening data.</summary>
[Activity(Exported = true, Theme = "@style/Maui.SplashTheme")]
[SupportedOSPlatform("android31.0")]
public sealed class NativeExitInspectionActivity : Activity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.Secure);
        if (savedInstanceState is not null || PackageName != "pro.vafadar.zanance.androidhomereview"
            || Android.OS.Process.MyUid() != Intent?.GetIntExtra("review_exit_uid", -1)) return;
        var pid = Intent?.GetIntExtra("review_exit_pid", -1) ?? -1;
        var run = Intent?.GetStringExtra("review_exit_run");
        if (pid <= 0 || run is null || !System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$")) return;
        _ = Task.Run(() => Inspect(pid, run));
    }

    private void Inspect(int pid, string run)
    {
        object packet;
        try
        {
            using var manager = (ActivityManager?)GetSystemService(ActivityService);
            var exits = manager?.GetHistoricalProcessExitReasons(PackageName, pid, 4)
                ?? throw new InvalidDataException("Owned exit history unavailable.");
            var match = exits.Where(e => e.Pid == pid && e.RealUid == Android.OS.Process.MyUid()).ToArray();
            if (match.Length != 1) throw new InvalidDataException("Exactly one owned exit is required.");
            if ((int)match[0].Reason != 5) // Android REASON_CRASH_NATIVE, not an inferred timeout or fatal log line.
            {
                // Some native deaths are retained as EXIT_SELF; their trace must not become a guessed tombstone.
                packet = new { Run = run, OriginalPid = pid, RealUid = match[0].RealUid,
                    ActualExitReason = (int)match[0].Reason, match[0].Status, match[0].Timestamp,
                    NativeTraceAccepted = false, Refusal = "OS_EXIT_REASON_IS_NOT_NATIVE_CRASH",
                    NoDatabaseStartupOrFinancialOperation = true };
            }
            else
            {
                using var trace = match[0].TraceInputStream ?? throw new InvalidDataException("Original native trace unavailable.");
                var frames = NativeExitFrames.Read(trace, checked((uint)pid), checked((uint)Android.OS.Process.MyUid()));
                packet = new { Run = run, OriginalPid = pid, NativeReason = (int)match[0].Reason, match[0].Status,
                    match[0].Timestamp, Frames = frames, NativeTraceAccepted = true, NoDatabaseStartupOrFinancialOperation = true,
                    NoMemoryRegistersAbsoluteStackOrUnrelatedCrashExported = true };
            }
        }
        catch (Exception error)
        {
            // Exception messages and raw traces can contain private paths or data; emit types only.
            packet = new { Run = run, OriginalPid = pid, Failed = true, ErrorType = error.GetType().FullName,
                NoDatabaseStartupOrFinancialOperation = true };
        }
        var json = JsonSerializer.SerializeToUtf8Bytes(packet);
        const int chunkSize = 1500;
        var count = (json.Length + chunkSize - 1) / chunkSize;
        for (var index = 0; index < count; index++)
        {
            var chunk = Convert.ToBase64String(json, index * chunkSize, Math.Min(chunkSize, json.Length - index * chunkSize));
            Android.Util.Log.Info("ZananceNativeExit", $"{run}|{index}/{count}|{chunk}");
        }
    }
}
