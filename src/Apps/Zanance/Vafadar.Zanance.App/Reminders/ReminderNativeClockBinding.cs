using Vafadar.Zanance.Core.Reminders;
using System.Globalization;

namespace Vafadar.Zanance.App.Reminders;

/// <summary>A native queue deadline's exact wall/offset identity and clock binding, independent of mutable source preferences.</summary>
internal sealed record ReminderNativeClockBinding(int Id, DateTimeOffset Scheduled, bool FollowsLocalClock, ReminderElapsedDeadline? ElapsedDeadline = null)
{
    /// <summary>Encodes only the existing deadline and clock kind, without notification content or financial data.</summary>
    internal string Encode()
    {
        var original = FormattableString.Invariant($"{Scheduled.Ticks}:{Scheduled.Offset.Ticks}:{(FollowsLocalClock ? 1 : 0)}");
        return ElapsedDeadline is { } elapsed
            ? original + FormattableString.Invariant($":{elapsed.BootGeneration}:{elapsed.TargetMilliseconds}") : original;
    }

    /// <summary>Refuses absent, damaged or stale metadata, including the same instant expressed with a different wall time.</summary>
    internal static ReminderNativeClockBinding? Match(int id, string? encoded, DateTimeOffset scheduled)
    {
        if (encoded is null || encoded.Length > 96) return null;
        var parts = encoded.Split(':');
        if (parts.Length is not (3 or 5) || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var wall) ||
            !long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var offset) ||
            parts[2] is not ("0" or "1") || wall != scheduled.Ticks || offset != scheduled.Offset.Ticks) return null;
        ReminderElapsedDeadline? elapsed = null;
        if (parts.Length == 5)
        {
            if (parts[2] != "0" || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var boot) ||
                !long.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var target) || boot < 0 || target < 0) return null;
            elapsed = new(boot, target);
        }
        var binding = new ReminderNativeClockBinding(id, scheduled, parts[2] == "1", elapsed);
        return encoded == binding.Encode() ? binding : null;
    }
}
