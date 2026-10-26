using System.Diagnostics;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Captures self-process aggregate memory only after the native timer stops and before post-control graphs.</summary>
    private static object CaptureNativePublicationMemory(int sample)
    {
        RequireHomeWindowFocus();
        var start = Stopwatch.GetTimestamp();
        var counters = CaptureSelfMemoryCounters();
        return new { Sample = sample, ProcessId = Environment.ProcessId, CallerUid = Android.OS.Process.MyUid(),
            StartTicks = start, EndTicks = Stopwatch.GetTimestamp(), Counters = counters,
            Boundary = "native-ready-before-post-controls", BeforeFinalSourceAudit = true,
            NoPeakMemoryOrLowMemoryAcceptance = true };
    }

    /// <summary>Reads only owned self-process counters; the preparation preflight makes no native-target boundary claim.</summary>
    private static NativePublicationMemory CaptureSelfMemoryCounters()
    {
        using var info = new Android.OS.Debug.MemoryInfo();
        Android.OS.Debug.GetMemoryInfo(info);
        // D-547: original Android total PSS already includes proportional swap; retain VmSwap separately.
        return NativePublicationMemory.Parse(File.ReadAllText("/proc/self/status"), info.TotalPss);
    }
}
