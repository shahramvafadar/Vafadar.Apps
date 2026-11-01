using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original travel failure, actual zone transition rules and existing SQLite snooze flows.</summary>
public sealed class ReminderLocalTimeTests
{
    [Theory]
    [InlineData("America/New_York", -5)]
    [InlineData("Asia/Tokyo", 9)]
    [InlineData("Europe/Berlin", 1)]
    [InlineData("Australia/Lord_Howe", 11)]
    public void Travel_keeps_original_wall_time_and_date_instead_of_original_instant(string id, int hours)
    {
        var original = new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(1));
        var changed = ReminderLocalTime.Resolve(original.DateTime, TimeZoneInfo.FindSystemTimeZoneById(id));
        Assert.Equal(original.DateTime, changed.DateTime);
        Assert.Equal(TimeSpan.FromHours(hours), changed.Offset);
        Assert.Equal(new DateOnly(2026, 11, 3), DateOnly.FromDateTime(changed.DateTime));
    }

    [Theory]
    [InlineData("Europe/Berlin", 2026, 3, 29, 2, 30, 2026, 3, 29, 3, 0, 120)]
    [InlineData("Australia/Lord_Howe", 2026, 10, 4, 2, 15, 2026, 10, 4, 2, 30, 660)]
    public void A_gap_moves_to_first_valid_instant_in_actual_supported_DST_rules(string id, int y, int m, int d, int h, int minute, int ey, int em, int ed, int eh, int eminute, int offset)
    {
        var original = new DateTime(y, m, d, h, minute, 0);
        var resolved = ReminderLocalTime.Resolve(original, TimeZoneInfo.FindSystemTimeZoneById(id));
        Assert.Equal(new DateTime(ey, em, ed, eh, eminute, 0), resolved.DateTime);
        Assert.Equal(TimeSpan.FromMinutes(offset), resolved.Offset);
        Assert.Equal(new DateTime(y, m, d, h, minute, 0), original);
    }


    [Fact]
    public void A_twelve_hour_gap_uses_its_actual_boundary_instead_of_assuming_one_hour()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new(1, 1, 1, 0, 0, 0), 3, 29);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new(1, 1, 1, 13, 0, 0), 10, 25);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new(2026, 1, 1), new(2026, 12, 31), TimeSpan.FromHours(12), start, end);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Owned large gap", TimeSpan.Zero, "Owned", "Standard", "Summer", [rule]);
        var at = new DateTime(2026, 3, 29, 9, 0, 0);
        Assert.True(zone.IsInvalidTime(at));
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.FromHours(12)), ReminderLocalTime.Resolve(at, zone));
    }

    [Fact]
    public async Task Original_elapsed_snooze_survives_repeated_hour_even_when_its_wall_clock_appears_earlier()
    {
        await using var f = new ProfileReminderFlowTests.Fixture();
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new(1, 1, 1, 0, 0, 0), 3, 29);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new(1, 1, 1, 9, 30, 0), 10, 10);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new(2026, 1, 1), new(2026, 12, 31), TimeSpan.FromHours(1), start, end);
        f.Clock.Zone = TimeZoneInfo.CreateCustomTimeZone("Owned repeated hour", TimeSpan.Zero, "Owned", "Standard", "Summer", [rule]);
        f.Clock.Now = new(2026, 10, 10, 7, 50, 0, TimeSpan.Zero);
        await f.SeedAsync(); f.Data.Enable(); await f.Service.RefreshAsync();
        var original = Assert.Single(f.Native.Pending); var before = await f.RowsAsync();
        await f.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
        var saved = Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        f.Clock.Now = f.Clock.Now.AddMinutes(5);
        var expected = TimeZoneInfo.ConvertTime(saved.NotifyInstant!.Value, f.Clock.Zone).DateTime;
        Assert.True(expected < f.Clock.GetLocalNow().DateTime);
        Assert.True(saved.NotifyInstant > f.Clock.Now);
        await f.Service.RefreshAsync();
        Assert.Equal(saved.NotifyInstant, Assert.Single(f.Native.Pending, n => n.Id == saved.Id).NotifyInstant);
        Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public void Repeated_hour_selects_the_earlier_instant_only()
    {
        var resolved = ReminderLocalTime.Resolve(new(2026, 10, 25, 2, 30, 0), TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        Assert.Equal(TimeSpan.FromHours(2), resolved.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), resolved);
    }

    [Fact]
    public void A_calendar_input_kind_does_not_reinterpret_its_wall_components_as_an_instant()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        var wall = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(9)), ReminderLocalTime.Resolve(wall, zone));
    }

    [Fact]
    public void Ordinary_calendar_request_follows_clock_without_a_snooze_record() => Assert.True(ReminderLocalTime.FollowsLocalClock(7, []));

    [Theory]
    [InlineData(null, false)]
    [InlineData(SnoozeChoice.OneHour, false)]
    [InlineData(SnoozeChoice.Tomorrow, true)]
    [InlineData((SnoozeChoice)99, false)]
    public void Only_known_tomorrow_snooze_changes_wall_binding(SnoozeChoice? choice, bool expected)
    {
        var saved = new SnoozedReminder(7, "review", "Zanance", "Fictitious", new(2026, 11, 3, 9, 0, 0), choice);
        Assert.Equal(expected, ReminderLocalTime.FollowsLocalClock(7, [saved]));
        Assert.Equal(saved, Assert.Single(ReminderSnoozes.Deserialize(ReminderSnoozes.Serialize([saved]))));
    }

    [Fact]
    public void Ambiguous_or_elapsed_cache_does_not_invent_calendar_authority()
    {
        var saved = new SnoozedReminder(7, "review", "Zanance", "Fictitious", new(2026, 11, 3, 9, 0, 0), SnoozeChoice.Tomorrow);
        Assert.False(ReminderLocalTime.FollowsLocalClock(7, [saved, saved]));
        Assert.False(ReminderLocalTime.FollowsLocalClock(7, [saved with { NotifyInstant = DateTimeOffset.UnixEpoch }]));
    }

    [Theory]
    [InlineData(SnoozeChoice.OneHour)]
    [InlineData(SnoozeChoice.Tomorrow)]
    public async Task Original_SQLite_flow_preserves_choice_and_only_hour_elapsed_deadline_through_travel(SnoozeChoice choice)
    {
        await using var f = new ProfileReminderFlowTests.Fixture();
        await f.SeedAsync(); f.Data.Enable();
        await f.Service.RefreshAsync(); var original = Assert.Single(f.Native.Pending);
        var before = await f.RowsAsync(); var issued = f.Clock.Now;
        await f.Service.SnoozeAsync(new(original, choice));
        var saved = Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        Assert.Equal(choice, saved.Choice);
        Assert.Equal(choice == SnoozeChoice.OneHour ? issued.AddHours(1) : (DateTimeOffset?)null, saved.NotifyInstant);
        f.Clock.Zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        f.Clock.Now = issued.AddMinutes(20);
        await f.Service.RefreshAsync();
        var refreshed = Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        var pending = Assert.Single(f.Native.Pending, n => n.Id == saved.Id);
        Assert.Equal(saved.NotifyInstant, pending.NotifyInstant);
        Assert.Equal(choice == SnoozeChoice.OneHour, pending.PreserveInstant);
        Assert.Equal(choice == SnoozeChoice.OneHour ? TimeZoneInfo.ConvertTime(issued.AddHours(1), f.Clock.Zone).DateTime : saved.NotifyAt, pending.NotifyAt);
        Assert.Equal(saved.Choice, refreshed.Choice);
        Assert.Equal(before, await f.RowsAsync()); Assert.Empty(f.Native.Shown);
    }
}
