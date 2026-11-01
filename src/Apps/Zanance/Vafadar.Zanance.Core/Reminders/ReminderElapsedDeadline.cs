namespace Vafadar.Zanance.Core.Reminders;

/// <summary>A device-local, same-boot sleep-inclusive deadline for one elapsed hour; never a financial date.</summary>
/// <param name="BootGeneration">The platform boot generation that owns the elapsed counter.</param>
/// <param name="TargetMilliseconds">The requested elapsed milliseconds since that boot, including sleep.</param>
public sealed record ReminderElapsedDeadline(int BootGeneration, long TargetMilliseconds)
{
    /// <summary>The existing one-hour snooze duration expressed in elapsed milliseconds.</summary>
    public const long HourMilliseconds = 3_600_000;

    /// <summary>Checks only this boot's plausible one-hour interval; stale or corrupted anchors retain the old UTC fallback.</summary>
    public bool IsCurrent(int bootGeneration, long elapsedMilliseconds) => BootGeneration >= 0 && BootGeneration == bootGeneration &&
        TargetMilliseconds >= 0 && elapsedMilliseconds >= 0 && TargetMilliseconds - elapsedMilliseconds <= HourMilliseconds;

    /// <summary>Maps this boot's remaining interval to the current wall clock without extending it when UTC changes.</summary>
    public DateTimeOffset Resolve(int bootGeneration, long elapsedMilliseconds, DateTimeOffset utcNow, DateTimeOffset fallback)
    {
        if (!IsCurrent(bootGeneration, elapsedMilliseconds)) return fallback;
        var remaining = TargetMilliseconds - elapsedMilliseconds;
        // An old expired same-boot anchor must stay expired even if the wall clock moves back.
        // Bound date arithmetic without inventing a fresh hour or a new boot identity.
        return remaining < -HourMilliseconds ? utcNow.AddTicks(-1) : utcNow.AddMilliseconds(remaining);
    }
}
