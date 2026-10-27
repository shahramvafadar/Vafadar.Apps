#if LOADED_COLD_START_REVIEW
using System.Runtime.Versioning;
using Android.App;
using Android.Views;
using Vafadar.Zanance.App.Features.Home;

namespace Vafadar.Zanance.App;

/// <summary>Uses the current OS cold-activity record and original ready Home draw, never the initial covered frame.</summary>
[SupportedOSPlatform("android36.0")]
internal static class LoadedColdStartNative
{
    /// <summary>Earliest native activity entry, captured before original covered startup without data reads.</summary>
    internal static (long Monotonic, long BootTime) ActivityEntry { get; private set; }
    /// <summary>Actual report frame in both clock domains; no SDK record is inferred from these values.</summary>
    internal static (long Monotonic, long BootTime) ReportFrame { get; private set; }
    /// <summary>Runs once at the admitted activity's native entry; a new activity in the same process refuses.</summary>
    internal static void CaptureActivityEntry()
    {
        if (ActivityEntry.Monotonic != 0) return;
        ActivityEntry = (Java.Lang.JavaSystem.NanoTime(), Android.OS.SystemClock.ElapsedRealtimeNanos());
    }

    /// <summary>Bounded own-package OS metadata retained when a current PID record is absent; never a PID substitute.</summary>
    internal static object? LastRecordAdmission { get; private set; }
    /// <summary>Public native startup facts, with no Intent, profile payload, private memory or credentials.</summary>
    internal sealed record Launch(int Pid, int Uid, bool ColdActivity, bool Complete, long Start, long FirstFrame, long FullyDrawn);

    /// <summary>Reads only this actual process; an unrelated old ring-buffer record cannot satisfy readiness.</summary>
    internal static Launch ReadCurrent(Activity activity, string run, string shape)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(36)) throw new InvalidOperationException("Native startup records require the admitted API36 activity component.");
        var pid = Android.OS.Process.MyPid(); var uid = Android.OS.Process.MyUid();
        using var manager = (ActivityManager?)activity.GetSystemService(Android.Content.Context.ActivityService)
            ?? throw new InvalidOperationException("Original activity manager unavailable.");
        var records = manager.GetHistoricalProcessStartReasons(8) ?? throw new InvalidDataException("Original native startup list missing.");
        try
        {
            var matches = records.Where(item => item.Pid == pid && item.RealUid == uid && item.PackageUid == uid).ToArray();
            LastRecordAdmission = new { ExpectedPid = pid, ExpectedUid = uid, CurrentPidMatches = matches.Length,
                OwnRecords = records.Where(item => item.RealUid == uid && item.PackageUid == uid).Take(8)
                    .Select(item => new { item.Pid, item.RealUid, item.PackageUid, item.StartType, item.StartupState, item.StartComponent }).ToArray() };
            if (matches.Length != 1) throw new InvalidOperationException("One current native startup record required.");
            var record = matches[0];
            long Timestamp(ApplicationStartInfoStartTimestamp key)
            {
                using var boxed = Java.Lang.Integer.ValueOf((int)key) ?? throw new InvalidDataException("Native timestamp key unavailable.");
                var timestamps = record.StartupTimestamps ?? throw new InvalidDataException("Native timestamp map missing.");
                if (!timestamps.TryGetValue(boxed, out var value) || value is null) return 0;
                using (value) return value.LongValue();
            }
            return new(pid, uid, record.StartType == ApplicationStartInfoStartType.Cold
                && record.StartComponent == ApplicationStartInfoStartComponent.Activity
                && record.Intent?.Component?.FlattenToString() == activity.ComponentName?.FlattenToString()
                // ApplicationStartInfo.setIntent strips extras. Bind the immutable native
                // current-PID/component record to this shell-protected activity's actual request.
                && activity.Intent?.GetStringExtra("review_phase") == "cold-measure"
                && activity.Intent?.GetStringExtra("review_run") == run
                && activity.Intent?.GetStringExtra("review_shape") == shape,
                record.StartupState == ApplicationStartInfoStartupState.FirstFrameDrawn,
                Timestamp(ApplicationStartInfoStartTimestamp.Launch), Timestamp(ApplicationStartInfoStartTimestamp.FirstFrame),
                Timestamp(ApplicationStartInfoStartTimestamp.FullyDrawn));
        }
        finally { foreach (var record in records) record.Dispose(); }
    }

    /// <summary>Waits on the original appearance task; it never calls LoadAsync, navigates or reads financial storage.</summary>
    internal static async Task<HomePage> WaitOriginalHomeAsync(App app, CancellationToken ct)
    {
        await app.WaitForStartupAsync().WaitAsync(ct);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (app.Windows.SingleOrDefault()?.Page is AppShell shell && shell.CurrentPage is HomePage home
                && home.BindingContext is HomeViewModel vm)
            {
                await home.PendingPublication.WaitAsync(ct);
                if (vm.Loading.HasFailed) throw new InvalidOperationException("Original Home publication failed; preserve its cover and process.");
                if (vm.Loading.IsReady && home.IsVisible && home.FindByName<ScrollView>("ContentViewport").IsVisible
                    && home.FindByName<Button>("AddAction") is { IsEnabled: true, IsVisible: true }) return home;
            }
            await Task.Delay(10, ct);
        }
    }

    /// <summary>Observes an actual draw with ready content, then lets the framework record full draw on the following native frame.</summary>
    internal static async Task<long> ReportOriginalReadyDrawAsync(Activity activity, HomePage page, CancellationToken ct)
    {
        if (page.Handler?.PlatformView is not Android.Views.View native || !native.HasWindowFocus || !activity.HasWindowFocus)
            throw new InvalidOperationException("Original owned Home must own the native window.");
        using var listener = new ReadyDraw(page, native);
        var tree = native.ViewTreeObserver ?? throw new InvalidOperationException("Original native tree unavailable.");
        tree.AddOnDrawListener(listener); native.Invalidate();
        try
        {
            var observed = await listener.Completion.Task.WaitAsync(ct);
            // Removal occurs outside OnDraw; Android rejects modifying the observer during its draw dispatch.
            if (tree.IsAlive) tree.RemoveOnDrawListener(listener);
            using var frame = new NextFrame(); var choreographer = Choreographer.Instance!;
            choreographer.PostFrameCallback(frame);
            try { await frame.Completion.Task.WaitAsync(ct); }
            finally { choreographer.RemoveFrameCallback(frame); }
            if (!native.HasWindowFocus || !activity.HasWindowFocus || page.BindingContext is not HomeViewModel { Loading.IsReady: true })
                throw new InvalidOperationException("Ready draw lost its original owner or uncovered publication.");
            activity.ReportFullyDrawn();
            ReportFrame = (Java.Lang.JavaSystem.NanoTime(), Android.OS.SystemClock.ElapsedRealtimeNanos());
            return observed;
        }
        finally { if (tree.IsAlive) tree.RemoveOnDrawListener(listener); }
    }

    /// <summary>Only an actually arranged, visible original Home with completed publication may produce this observation.</summary>
    private sealed class ReadyDraw(HomePage page, Android.Views.View view) : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnDrawListener
    {
        /// <summary>Completes only from this original native lifetime.</summary>
        internal TaskCompletionSource<long> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc />
        public void OnDraw()
        {
            if (view.HasWindowFocus && view.IsLaidOut && !view.IsLayoutRequested && view.Width > 0 && view.Height > 0
                && page.BindingContext is HomeViewModel { Loading.IsReady: true } && page.PendingPublication.IsCompletedSuccessfully)
                Completion.TrySetResult(Java.Lang.JavaSystem.NanoTime());
        }
    }

    /// <summary>Retains the actual main-looper next-frame completion without altering original rendering.</summary>
    private sealed class NextFrame : Java.Lang.Object, Choreographer.IFrameCallback
    {
        /// <summary>Completes only from this original native lifetime.</summary>
        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc />
        public void DoFrame(long frameTimeNanos) => Completion.TrySetResult();
    }
}
#endif
