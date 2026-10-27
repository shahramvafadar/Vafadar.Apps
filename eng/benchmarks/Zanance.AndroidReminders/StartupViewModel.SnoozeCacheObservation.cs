#if SNOOZE_CACHE_REVIEW
using System.Diagnostics;
using System.Text.Json;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Observes only bounded type/method names from this fresh independent emulator's caught startup failure.</summary>
public partial class StartupViewModel
{
    private static int observations;

    partial void ObservePreparationFailure(Exception error)
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (AppInfo.Current.PackageName != "pro.vafadar.zanance.snoozecachereview"
                || Android.OS.Build.Hardware != "ranchu" || activity?.Intent?.GetBooleanExtra("review_snooze_cache", false) != true
                || Interlocked.Increment(ref observations) > 2) return;
            var run = activity.Intent.GetStringExtra("review_run");
            if (run is null || !Guid.TryParseExact(run, "N", out _)) return;
            var failures = new List<object>();
            for (Exception? current = error; current is not null && failures.Count < 4; current = current.InnerException)
                failures.Add(new { Type = current.GetType().FullName, Refusal = KnownRestoreRefusal(current), Methods = new StackTrace(current, false).GetFrames().Take(6)
                    .Select(frame => frame.GetMethod()).Select(method => method?.DeclaringType?.FullName + "." + method?.Name).ToArray() });
            Android.Util.Log.Info("ZananceSnoozeStartup", JsonSerializer.Serialize(new { Run = run, Pid = Android.OS.Process.MyPid(),
                Uid = Android.OS.Process.MyUid(), Failures = failures, NoMessagePathKeySourceOrNativeMemory = true }));
        }
        catch (Exception observerError) when (observerError is not OutOfMemoryException)
        {
            // An observation must not change the actual covered preparation or invoke its retry.
        }
    }

    /// <summary>Maps only three fixed source refusals; never exports arbitrary exception messages or paths.</summary>
    private static string KnownRestoreRefusal(Exception error) => error is IOException ? error.Message switch
    {
        "Trusted restore directory is missing or redirected." => "restore-directory-admission",
        "Restore candidate already exists." => "restore-existing-candidate",
        "Insufficient space for bounded encrypted restore staging." => "restore-required-space",
        _ => "other-io"
    } : "other-type";
}
#endif
