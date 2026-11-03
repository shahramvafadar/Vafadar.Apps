using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

/// <summary>Calendar exhaustion must end a series without losing its final representable occurrence.</summary>
public sealed class RecurrenceFinalMonthTests
{
    [Theory]
    [InlineData(PeriodCalendar.Persian, Frequency.Monthly)]
    [InlineData(PeriodCalendar.Persian, Frequency.Yearly)]
    [InlineData(PeriodCalendar.Hijri, Frequency.Monthly)]
    [InlineData(PeriodCalendar.Hijri, Frequency.Yearly)]
    public void Final_calendar_date_ends_the_series(PeriodCalendar calendar, Frequency frequency)
    {
        var rule = new RecurrenceRule { Start = DateOnly.MaxValue, Calendar = calendar, Frequency = frequency };

        Assert.Null(Recurrence.Validate(rule));
        Assert.Equal([new ScheduledDate(1, DateOnly.MaxValue)], Recurrence.Between(rule, rule.Start, DateOnly.MaxValue));
        Assert.Equal(DateOnly.MaxValue, rule.Start);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    public void Final_Persian_month_with_second_day_ends_the_series(int secondDay)
    {
        var rule = new RecurrenceRule { Start = DateOnly.MaxValue, Calendar = PeriodCalendar.Persian,
            Frequency = Frequency.Monthly, SecondDay = secondDay };

        Assert.Equal([new ScheduledDate(1, DateOnly.MaxValue)], Recurrence.Between(rule, rule.Start, DateOnly.MaxValue));
        Assert.Equal(secondDay, rule.SecondDay);
    }

    [Theory]
    [InlineData(Frequency.Once)]
    [InlineData(Frequency.Daily)]
    [InlineData(Frequency.Weekly)]
    [InlineData(Frequency.Monthly)]
    [InlineData(Frequency.Yearly)]
    public void Largest_interval_stops_after_its_representable_anchor(Frequency frequency)
    {
        var rule = new RecurrenceRule { Start = new DateOnly(2026, 11, 3), Frequency = frequency, Interval = int.MaxValue };

        Assert.Equal([new ScheduledDate(1, rule.Start)], Recurrence.Between(rule, rule.Start, DateOnly.MaxValue));
        Assert.Equal(int.MaxValue, rule.Interval);
    }

    [Theory]
    [InlineData(PeriodCalendar.Persian, true)]
    [InlineData(PeriodCalendar.Hijri, false)]
    public void Final_month_does_not_invent_an_unrepresentable_nominal_last_day(PeriodCalendar calendar, bool hasLastDay)
    {
        var (year, month) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var first = PeriodMath.ToDate(year, month, 1, calendar);
        var rule = new RecurrenceRule { Start = first, Calendar = calendar, Frequency = Frequency.Monthly,
            DayRule = MonthDayRule.LastDayOfMonth };

        var dates = Recurrence.Between(rule, first, DateOnly.MaxValue).ToArray();

        if (hasLastDay) Assert.Equal([new ScheduledDate(1, DateOnly.MaxValue)], dates);
        else Assert.Empty(dates);
        Assert.True(PeriodMath.IsSupportedYear(year, calendar));
        Assert.Equal(first, rule.Start);
    }
}
