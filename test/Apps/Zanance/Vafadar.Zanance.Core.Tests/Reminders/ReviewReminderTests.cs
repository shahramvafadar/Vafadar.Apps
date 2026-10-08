using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Reminders;

/// <summary>Checks opt-in, financial boundaries and calendar-specific reminder dates.</summary>
[Trait("AT", "AT-78")]
public sealed class ReviewReminderTests
{
    [Fact]
    public void New_profiles_and_profiles_without_old_enough_data_do_not_remind()
    {
        var now = new DateTime(2026, 1, 31, 8, 0, 0);
        Assert.False(new ZananceSettings().ReviewReminderEnabled);
        Assert.Empty(ReviewReminderPlanner.Plan(false, null, now, PeriodCalendar.Gregorian, 1, new DateOnly(2025, 1, 1)));
        Assert.Empty(ReviewReminderPlanner.Plan(true, null, now, PeriodCalendar.Gregorian, 1, null));
        var first = ReviewReminderPlanner.Plan(true, null, now, PeriodCalendar.Gregorian, 1, new DateOnly(2026, 2, 1))[0];
        Assert.Equal(new DateTime(2026, 3, 1, 9, 0, 0), first.NotifyAt);
    }

    [Fact]
    public void Pay_cycle_closes_on_the_24th_and_reminds_on_the_25th()
    {
        var first = ReviewReminderPlanner.Plan(true, null, new DateTime(2026, 10, 1), PeriodCalendar.Gregorian, 25, new DateOnly(2026, 1, 1))[0];
        Assert.Equal(new DateOnly(2026, 9, 25), first.PeriodFirst);
        Assert.Equal(new DateTime(2026, 10, 25, 9, 0, 0, DateTimeKind.Local), first.NotifyAt);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 2027, 1)]
    [InlineData(PeriodCalendar.Persian, 1405, 1)]
    [InlineData(PeriodCalendar.Hijri, 1448, 1)]
    public void Year_boundaries_follow_the_selected_calendar(PeriodCalendar calendar, int year, int month)
    {
        var boundary = PeriodMath.ToDate(year, month, 1, calendar);
        var previous = PeriodMath.Previous(year, month);
        var range = PeriodMath.MonthRange(previous.Year, previous.Month, calendar);
        var first = ReviewReminderPlanner.Plan(true, null, boundary.AddDays(-1).ToDateTime(new TimeOnly(12, 0)), calendar, 1, range.First)[0];
        Assert.Equal(boundary.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local), first.NotifyAt);
        Assert.Equal(range.First, first.PeriodFirst);
    }

    [Fact]
    public void Finishing_skips_that_period_while_partial_steps_still_remind()
    {
        var now = new DateTime(2026, 2, 1, 8, 0, 0);
        var firstData = new DateOnly(2025, 1, 1);
        var partial = ReviewReminderPlanner.Plan(true, "2026-01:Unreviewed,Plans", now, PeriodCalendar.Gregorian, 1, firstData);
        Assert.Equal(now.Date.AddHours(9), partial[0].NotifyAt);
        var finished = ReviewReminderPlanner.Plan(true, "2026-01", now, PeriodCalendar.Gregorian, 1, firstData);
        Assert.Equal(new DateTime(2026, 3, 1, 9, 0, 0), finished[0].NotifyAt);
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(15, 30)]
    public void Resume_drops_missed_times_without_a_catch_up_burst(int hour, int minute)
    {
        var now = new DateTime(2026, 2, 1, hour, minute, 0);
        var items = ReviewReminderPlanner.Plan(true, null, now, PeriodCalendar.Gregorian, 1, new DateOnly(2025, 1, 1));
        Assert.Equal(new DateTime(2026, 3, 1, 9, 0, 0), items[0].NotifyAt);
        Assert.All(items, r => Assert.InRange(r.NotifyAt, now.AddTicks(1), now.AddDays(ReminderPlanner.HorizonDays)));
    }

    [Fact]
    public void Rebuilding_keeps_ids_and_does_not_collide_with_plan_or_goal_fixtures()
    {
        var now = new DateTime(2026, 1, 31, 8, 0, 0);
        var items = ReviewReminderPlanner.Plan(true, null, now, PeriodCalendar.Gregorian, 1, new DateOnly(2025, 1, 1));
        Assert.Equal(items, ReviewReminderPlanner.Plan(true, null, now.AddMinutes(1), PeriodCalendar.Gregorian, 1, new DateOnly(2025, 1, 1)));
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
        Assert.All(items, i => Assert.True(i.Id > 0));
        var fixture = Guid.Parse("b8177d5e-5491-4280-90cc-a7b3ee392d14");
        Assert.All(items, i => Assert.NotEqual(GoalReminderPlanner.StableId(fixture, i.PeriodFirst), i.Id));
        Assert.All(items, i => Assert.NotEqual(ReminderPlanner.StableId(fixture, i.PeriodFirst), i.Id));
    }
}
