namespace Vafadar.Zanance.Core.Security;

/// <summary>Reads an optional platform same-boot, sleep-inclusive attempt clock without a device identifier.</summary>
public interface IPinAttemptClock
{
    /// <summary>Returns a current supported boot/counter pair, or null when that clock is unavailable.</summary>
    PinAttemptClockReading? Read();
}

/// <summary>A public coarse boot generation and elapsed milliseconds; neither is a unique device identity.</summary>
/// <param name="BootGeneration">The platform boot generation owning the counter.</param>
/// <param name="ElapsedMilliseconds">Sleep-inclusive elapsed milliseconds since that boot.</param>
public sealed record PinAttemptClockReading(int BootGeneration, long ElapsedMilliseconds);

/// <summary>The protected attempt deadline for one known boot, never a credential or financial date.</summary>
/// <param name="BootGeneration">The boot generation which accepted the penalty.</param>
/// <param name="TargetMilliseconds">The sleep-inclusive deadline in that boot.</param>
public sealed record PinRetryDeadline(int BootGeneration, long TargetMilliseconds)
{
    /// <summary>Captures an actual supported penalty; unavailable clocks retain the original UTC representation.</summary>
    internal static PinRetryDeadline? Capture(PinAttemptClockReading? clock, TimeSpan delay)
    {
        var milliseconds = (long)delay.TotalMilliseconds;
        return milliseconds > 0 && clock is { BootGeneration: >= 0, ElapsedMilliseconds: >= 0 } &&
            clock.ElapsedMilliseconds <= long.MaxValue - milliseconds
            ? new(clock.BootGeneration, clock.ElapsedMilliseconds + milliseconds) : null;
    }

    /// <summary>Resolves this boot's remaining penalty; only a proven different boot retains the original UTC fallback.</summary>
    internal TimeSpan? Remaining(PinAttemptClockReading? current, TimeSpan maximum)
    {
        // D-722: losing a required clock cannot turn an active protected penalty into access.
        if (current is not { BootGeneration: >= 0, ElapsedMilliseconds: >= 0 })
            throw new InvalidOperationException("The protected PIN retry clock is unavailable.");
        if (current.BootGeneration != BootGeneration) return null;
        var remaining = TargetMilliseconds - current.ElapsedMilliseconds;
        if (remaining > maximum.TotalMilliseconds)
            throw new InvalidOperationException("The protected PIN retry deadline is invalid.");
        return TimeSpan.FromMilliseconds(Math.Max(remaining, 0));
    }
}
