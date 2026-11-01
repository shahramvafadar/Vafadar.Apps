using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App.Reminders;

/// <summary>Resolves a financial reminder's local wall time without changing its calendar date in storage.</summary>
internal static class ReminderLocalTime
{
    /// <summary>Only a known tomorrow snooze follows local time; an elapsed or unclassified historical snooze keeps its instant.</summary>
    internal static bool FollowsLocalClock(int id, IEnumerable<SnoozedReminder> snoozes)
    {
        ArgumentNullException.ThrowIfNull(snoozes);
        var matches = snoozes.Where(s => s.Id == id).ToArray();
        // Historical caches did not retain the choice. Never invent one or change an elapsed
        // deadline while handling a native broadcast; normal source refresh remains authoritative.
        return matches.Length == 0 || (matches.Length == 1 && matches[0].Choice == SnoozeChoice.Tomorrow && matches[0].NotifyInstant is null);
    }

    /// <summary>Moves nonexistent wall times to the first real instant and selects only the earlier repeated hour.</summary>
    internal static DateTimeOffset Resolve(DateTime local, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var wall = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(wall))
        {
            // REM-07: search the boundary itself, not a fixed one-hour adjustment. Entire dates
            // and non-hour DST gaps exist; the financial due date remains outside this conversion.
            var invalid = wall;
            var valid = wall;
            for (var hour = 0; zone.IsInvalidTime(valid) && hour < 48; hour++) valid = valid.AddHours(1);
            if (zone.IsInvalidTime(valid)) throw new InvalidOperationException("No valid reminder wall time in the device zone.");
            while (valid.Ticks - invalid.Ticks > 1)
            {
                var middle = new DateTime(invalid.Ticks + (valid.Ticks - invalid.Ticks) / 2, DateTimeKind.Unspecified);
                if (zone.IsInvalidTime(middle)) invalid = middle;
                else valid = middle;
            }
            wall = valid;
        }
        var offset = zone.IsAmbiguousTime(wall) ? zone.GetAmbiguousTimeOffsets(wall).Max() : zone.GetUtcOffset(wall);
        return new DateTimeOffset(wall, offset);
    }
}
