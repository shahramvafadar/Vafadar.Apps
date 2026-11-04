using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Representable endpoints of full calendar and financial ranges (D-810).</summary>
public sealed class CalendarRangeEndpointTests
{
    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void The_final_year_ends_at_the_last_representable_day(PeriodCalendar calendar)
    {
        var (year, _) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);

        Assert.Equal((PeriodMath.ToDate(year, 1, 1, calendar), DateOnly.MaxValue), PeriodMath.YearRange(DateOnly.MaxValue, calendar));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void The_final_month_ends_at_the_last_representable_day(PeriodCalendar calendar)
    {
        var (year, month) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);

        Assert.Equal((PeriodMath.ToDate(year, month, 1, calendar), DateOnly.MaxValue), PeriodMath.MonthRange(year, month, calendar));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void A_representable_final_financial_start_does_not_require_a_future_month(PeriodCalendar calendar)
    {
        var (year, month) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var startDay = Math.Min(25, PeriodMath.DayOf(DateOnly.MaxValue, calendar));

        Assert.Equal((PeriodMath.ToDate(year, month, startDay, calendar), DateOnly.MaxValue), PeriodMath.MonthRange(year, month, calendar, startDay));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void Financial_month_identity_does_not_materialize_an_unavailable_range(PeriodCalendar calendar)
    {
        var current = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var expected = PeriodMath.DayOf(DateOnly.MaxValue, calendar) < 28 ? PeriodMath.Previous(current.Year, current.Month) : current;

        Assert.Equal(expected, PeriodMath.MonthOf(DateOnly.MaxValue, calendar, 28));
    }

    [Theory]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void A_financial_month_ending_in_the_partial_final_month_is_bounded(PeriodCalendar calendar)
    {
        var (lastYear, lastMonth) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var (year, month) = PeriodMath.Previous(lastYear, lastMonth);

        Assert.Equal((PeriodMath.ToDate(year, month, 28, calendar), DateOnly.MaxValue), PeriodMath.MonthRange(year, month, calendar, 28));
    }

    [Theory]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void An_unrepresentable_financial_start_is_still_refused(PeriodCalendar calendar)
    {
        var (year, month) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);

        Assert.Throws<ArgumentOutOfRangeException>(() => PeriodMath.MonthRange(year, month, calendar, 28));
    }
}
