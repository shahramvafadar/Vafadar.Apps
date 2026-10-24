namespace Vafadar.Zanance.App.Reminders;

/// <summary>Bounds Android's inexact delivery allowance to the originally scheduled local calendar day.</summary>
internal static class ReminderDeliveryWindow
{
    /// <summary>Returns the delay through the last instant of the scheduled day in the supplied device zone.</summary>
    internal static TimeSpan UntilEndOfDay(DateTimeOffset scheduled, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var local = TimeZoneInfo.ConvertTime(scheduled, zone);
        var boundary = DateTime.SpecifyKind(local.Date.AddDays(1), DateTimeKind.Unspecified);

        // Some zones skip midnight. Find the first real instant of the next local day rather
        // than allowing a fixed 24 hours to cross the calendar boundary (D-507).
        if (zone.IsInvalidTime(boundary))
        {
            var invalid = boundary;
            var valid = boundary;
            for (var hour = 0; zone.IsInvalidTime(valid) && hour < 48; hour++) valid = valid.AddHours(1);
            if (zone.IsInvalidTime(valid)) throw new InvalidOperationException("No valid next-day boundary in the device zone.");
            while (valid.Ticks - invalid.Ticks > 1)
            {
                var middle = new DateTime(invalid.Ticks + (valid.Ticks - invalid.Ticks) / 2, DateTimeKind.Unspecified);
                if (zone.IsInvalidTime(middle)) invalid = middle;
                else valid = middle;
            }
            boundary = valid;
        }

        // For an ambiguous midnight, the earlier occurrence already belongs to the next day.
        var offset = zone.IsAmbiguousTime(boundary)
            ? zone.GetAmbiguousTimeOffsets(boundary).Max()
            : zone.GetUtcOffset(boundary);
        var end = new DateTimeOffset(boundary, offset);
        // The SDK includes the allowance endpoint, so exclude the first tick of tomorrow.
        var allowance = end - scheduled - TimeSpan.FromTicks(1);
        return allowance < TimeSpan.Zero ? TimeSpan.Zero : allowance;
    }
}
