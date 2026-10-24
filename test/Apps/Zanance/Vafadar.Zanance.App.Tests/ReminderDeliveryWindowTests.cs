using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-142: real delivery-window source follows local day boundaries without modifying the system zone.</summary>
[Trait("AT", "AT-142")]
public sealed class ReminderDeliveryWindowTests
{
    [Theory]
    [InlineData("2026-10-24T09:00:00+02:00", "2026-10-24T22:00:00Z")]
    [InlineData("2026-03-29T00:30:00+01:00", "2026-03-29T22:00:00Z")]
    [InlineData("2026-10-25T00:30:00+02:00", "2026-10-25T23:00:00Z")]
    [InlineData("2026-10-24T23:59:59.9999999+02:00", "2026-10-24T22:00:00Z")]
    public void Original_day_expires_before_next_midnight_including_DST(string due, string end)
    {
        var scheduled = DateTimeOffset.Parse(due, System.Globalization.CultureInfo.InvariantCulture);
        var midnight = DateTimeOffset.Parse(end, System.Globalization.CultureInfo.InvariantCulture);
        var allowance = ReminderDeliveryWindow.UntilEndOfDay(scheduled, TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        Assert.Equal(midnight.AddTicks(-1), scheduled + allowance);
        Assert.True(midnight - scheduled > allowance);
    }

    [Theory]
    [InlineData("2026-10-24T10:36:59.194+02:00")]
    [InlineData("2026-10-24T10:36:59.200+02:00")]
    public void Actual_D494_late_delivery_remains_inside_the_approved_original_day(string delivered)
    {
        var due = new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.FromHours(2));
        var actual = DateTimeOffset.Parse(delivered, System.Globalization.CultureInfo.InvariantCulture);
        var allowance = ReminderDeliveryWindow.UntilEndOfDay(due, TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        Assert.True(actual - due > TimeSpan.FromHours(1));
        Assert.True(actual - due < allowance);
    }

    [Fact]
    public void UTC_input_uses_the_supplied_local_day_instead_of_its_UTC_date()
    {
        var due = new DateTimeOffset(2026, 10, 24, 23, 30, 0, TimeSpan.Zero);
        var allowance = ReminderDeliveryWindow.UntilEndOfDay(due, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"));
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 15, 0, 0, TimeSpan.Zero).AddTicks(-1), due + allowance);
    }

    [Fact]
    public void Midnight_gap_expires_at_the_first_real_instant_of_tomorrow()
    {
        var zone = MidnightZone();
        var due = new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero);
        var allowance = ReminderDeliveryWindow.UntilEndOfDay(due, zone);
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero).AddTicks(-1), due + allowance);
    }

    private static TimeZoneInfo MidnightZone()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 3, 29);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 1, 0, 0), 10, 25);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromHours(1), start, end);
        return TimeZoneInfo.CreateCustomTimeZone("Midnight boundary fixture", TimeSpan.Zero, "Fixture", "Standard", "Summer", [rule]);
    }

    [Fact]
    public void Ambiguous_midnight_expires_at_its_earlier_occurrence()
    {
        var due = new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.FromHours(1));
        var allowance = ReminderDeliveryWindow.UntilEndOfDay(due, MidnightZone());
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 23, 0, 0, TimeSpan.Zero).AddTicks(-1), due + allowance);
    }
}
