using System.Globalization;

namespace Vafadar.Zanance.App.Reminders;

/// <summary>A native queue deadline's exact wall/offset identity and clock binding, independent of mutable source preferences.</summary>
internal sealed record ReminderNativeClockBinding(int Id, DateTimeOffset Scheduled, bool FollowsLocalClock)
{
    /// <summary>Encodes only the existing deadline and clock kind, without notification content or financial data.</summary>
    internal string Encode() => FormattableString.Invariant($"{Scheduled.Ticks}:{Scheduled.Offset.Ticks}:{(FollowsLocalClock ? 1 : 0)}");

    /// <summary>Refuses absent, damaged or stale metadata, including the same instant expressed with a different wall time.</summary>
    internal static ReminderNativeClockBinding? Match(int id, string? encoded, DateTimeOffset scheduled)
    {
        if (encoded is null || encoded.Length > 64) return null;
        var parts = encoded.Split(':');
        if (parts.Length != 3 || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var wall) ||
            !long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var offset) ||
            parts[2] is not ("0" or "1") || wall != scheduled.Ticks || offset != scheduled.Offset.Ticks) return null;
        var binding = new ReminderNativeClockBinding(id, scheduled, parts[2] == "1");
        return encoded == binding.Encode() ? binding : null;
    }
}
