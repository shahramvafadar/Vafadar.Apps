using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

public sealed class PublicHolidayTests
{
    [Theory]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2030, 4, 21)]
    public void Easter_sunday_follows_the_gregorian_rule(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), PublicHolidays.EasterSunday(year));

    [Fact]
    public void German_holidays_are_the_nationwide_ones()
    {
        var dates = PublicHolidays.In("DE", 2026).Select(h => h.Date).ToList();

        Assert.Equal(
            [
                new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 3), new DateOnly(2026, 4, 6), new DateOnly(2026, 5, 1),
                new DateOnly(2026, 5, 14), new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 3), new DateOnly(2026, 12, 25),
                new DateOnly(2026, 12, 26),
            ],
            dates);
        Assert.All(PublicHolidays.In("DE", 2026), h => Assert.False(h.IsApproximate));
    }

    [Fact]
    public void Iranian_solar_holidays_are_exact_and_lunar_ones_are_marked_approximate()
    {
        var holidays = PublicHolidays.In("IR", 2026);

        // 1 Farvardin 1405 = 21 March 2026, 22 Bahman 1404 = 11 February 2026.
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 3, 21) && h.Key == "Nowruz" && !h.IsApproximate);
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 2, 11) && h.Key == "RevolutionDay");
        Assert.Contains(holidays, h => h.Key == "Ashura" && h.IsApproximate);
        Assert.Equal(holidays.Count, holidays.Select(h => h.Date).Distinct().Count());
    }

    [Fact]
    public void Regions_without_a_calendar_have_no_holidays()
    {
        Assert.False(PublicHolidays.IsSupported("FR"));
        Assert.False(PublicHolidays.IsHoliday(null, new DateOnly(2026, 1, 1)));
        Assert.Empty(PublicHolidays.In("FR", 2026));
    }

    [Fact]
    public void A_due_date_on_a_holiday_moves_over_the_following_weekend()
    {
        var rule = new RecurrenceRule
        {
            Frequency = Frequency.Monthly,
            Start = new DateOnly(2026, 1, 25),
            WeekendShift = WeekendShift.After,
            WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]),
            HolidayRegion = "DE",
        };

        // Friday 25 December 2026 and Saturday 26 December are holidays, Sunday is a weekend day.
        Assert.Equal(new DateOnly(2026, 12, 28), rule.ApplyWeekend(new DateOnly(2026, 12, 25)));
        var before = rule.Clone();
        before.WeekendShift = WeekendShift.Before;
        Assert.Equal(new DateOnly(2026, 12, 24), before.ApplyWeekend(new DateOnly(2026, 12, 25)));
    }

    [Fact]
    public void Moving_before_a_new_year_holiday_crosses_into_the_old_year()
    {
        var rule = new RecurrenceRule
        {
            Frequency = Frequency.Monthly,
            Start = new DateOnly(2026, 1, 1),
            WeekendShift = WeekendShift.Before,
            WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]),
            HolidayRegion = "DE",
        };

        // Friday 1 January 2027 is New Year: the working day before is Thursday 31 December 2026.
        Assert.Equal(new DateOnly(2026, 12, 31), rule.ApplyWeekend(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Years_outside_the_hijri_range_have_no_holidays_instead_of_an_error() =>
        Assert.False(PublicHolidays.IsHoliday("IR", new DateOnly(100, 3, 21)));

    [Fact]
    public void Without_a_holiday_region_only_weekends_move_a_due_date()
    {
        var rule = new RecurrenceRule
        {
            Frequency = Frequency.Monthly,
            Start = new DateOnly(2026, 1, 25),
            WeekendShift = WeekendShift.After,
            WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]),
        };

        Assert.Equal(new DateOnly(2026, 12, 25), rule.ApplyWeekend(new DateOnly(2026, 12, 25)));
    }
}
