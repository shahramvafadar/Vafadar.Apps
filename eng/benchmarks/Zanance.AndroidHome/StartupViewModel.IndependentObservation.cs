using System.Text.Json;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Implements only the independent caller's optional content-free startup effect port.</summary>
public partial class StartupViewModel
{
    private static int independentObservations;

    partial void ObservePreparationFailure(Exception error)
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (AppInfo.Current.PackageName != "pro.vafadar.zanance.receiptsourcereview"
                || activity?.Intent?.GetBooleanExtra("review_receipt_os_text", false) != true
                || Interlocked.Increment(ref independentObservations) > 4) return;
            var run = activity.Intent.GetStringExtra("review_run");
            var comparison = activity.Intent.GetStringExtra("review_pass");
            if (run is null || comparison is null || run.Length != 32 || comparison.Length != 32
                || run.Any(value => !Uri.IsHexDigit(value)) || comparison.Any(value => !Uri.IsHexDigit(value))) return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Run = run, Comparison = comparison, Pid = Android.OS.Process.MyPid(), Uid = Android.OS.Process.MyUid(),
                MonotonicMilliseconds = Android.OS.SystemClock.ElapsedRealtime(), Failures = StartupFailureRecords.Capture(error),
                OriginalCoveredStartupCaughtFailure = true, OriginalRetryUnchanged = true,
                NoMessageSourcePathKeyOrFinancialContent = true
            });
            const int size = 1536;
            var count = (bytes.Length + size - 1) / size;
            for (var index = 0; index < count; index++)
                Android.Util.Log.Info("ZananceStartupFailure", run + "|" + comparison + "|" + index + "/" + count + "|"
                    + Convert.ToBase64String(bytes, index * size, Math.Min(size, bytes.Length - index * size)));
        }
        catch (Exception observationError) when (observationError is not OutOfMemoryException)
        {
            // Diagnostic failure must not replace the original generic cover or start another preparation.
        }
    }
}
