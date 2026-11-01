using Android.OS;
using Android.Provider;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App;

/// <summary>Reads a sleep-inclusive elapsed clock and coarse boot counter without device identity, permissions or financial storage.</summary>
internal static class ReminderElapsedClock
{
    /// <summary>Captures the actual one-hour interval before source-read awaits can consume part of it.</summary>
    internal static ReminderElapsedDeadline? CaptureHour()
    {
        var boot = BootGeneration();
        var now = SystemClock.ElapsedRealtime();
        if (boot < 0 || now < 0 || now > long.MaxValue - ReminderElapsedDeadline.HourMilliseconds) return null;
        return new(boot, now + ReminderElapsedDeadline.HourMilliseconds);
    }

    /// <summary>Refuses a different boot or impossible future interval instead of rebasing an unknown timing origin.</summary>
    internal static bool IsCurrent(ReminderElapsedDeadline? deadline) => deadline is not null &&
        deadline.IsCurrent(BootGeneration(), SystemClock.ElapsedRealtime());

    /// <summary>Maps remaining boot time to a bounded current UTC sample; sleep does not pause this counter.</summary>
    internal static DateTimeOffset Resolve(ReminderElapsedDeadline deadline, DateTimeOffset fallback)
    {
        var boot = BootGeneration();
        var before = SystemClock.ElapsedRealtime();
        var utc = DateTimeOffset.UtcNow;
        var after = SystemClock.ElapsedRealtime();
        return deadline.Resolve(boot, before + (after - before) / 2, utc, fallback);
    }

    private static int BootGeneration()
    {
        try { return Settings.Global.GetInt(Android.App.Application.Context.ContentResolver, Settings.Global.BootCount, -1); }
        catch (Java.Lang.SecurityException) { return -1; }
    }
}
