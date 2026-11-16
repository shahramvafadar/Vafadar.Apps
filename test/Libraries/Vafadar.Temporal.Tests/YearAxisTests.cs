namespace Vafadar.Temporal.Tests;

/// <summary>The axis of the years of the common era, as a chart draws it.</summary>
public sealed class YearAxisTests
{
    private static CalendarPosition Year(int year, Era era = Era.CommonEra, CalendarSystem calendar = CalendarSystem.Gregorian) =>
        CalendarPosition.OfYear(calendar, era, year);

    [Fact]
    public void A_year_of_the_common_era_covers_the_stretch_from_its_number_to_the_next()
    {
        var span = YearAxis.Of(Year(224))!.Value;

        Assert.Equal(224m, span.From);
        Assert.Equal(225m, span.To);
        Assert.Equal(224.5m, span.Middle);
        Assert.Equal(1m, span.Length);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, -1)]
    [InlineData(330, -329)]
    [InlineData(550, -549)]
    public void A_year_before_the_common_era_is_not_at_the_place_of_its_number(int year, int place)
    {
        var span = YearAxis.Of(Year(year, Era.BeforeCommonEra))!.Value;

        Assert.Equal(place, span.From);
        Assert.Equal(place + 1, span.To);
    }

    [Fact]
    public void The_year_one_follows_the_year_one_before_the_era_without_a_year_between_them()
    {
        var last = YearAxis.Of(Year(1, Era.BeforeCommonEra))!.Value;
        var first = YearAxis.Of(Year(1))!.Value;

        Assert.Equal(last.To, first.From);
    }

    [Fact]
    public void From_the_year_100_before_the_era_to_the_year_100_of_it_are_199_years_not_200()
    {
        Assert.Equal(199m, YearAxis.StartOf(Era.CommonEra, 100) - YearAxis.StartOf(Era.BeforeCommonEra, 100));
    }

    [Fact]
    public void A_month_and_a_day_cover_their_part_of_the_year_in_order()
    {
        var year = YearAxis.Of(Year(116))!.Value;
        var march = YearAxis.Of(CalendarPosition.OfMonth(CalendarSystem.Julian, Era.CommonEra, 116, 3))!.Value;
        var first = YearAxis.Of(CalendarPosition.OfDay(CalendarSystem.Julian, Era.CommonEra, 116, 3, 1))!.Value;
        var last = YearAxis.Of(CalendarPosition.OfDay(CalendarSystem.Julian, Era.CommonEra, 116, 3, 31))!.Value;
        var december = YearAxis.Of(CalendarPosition.OfMonth(CalendarSystem.Julian, Era.CommonEra, 116, 12))!.Value;

        Assert.True(year.From < march.From && march.To < year.To);
        Assert.Equal(march.From, first.From);
        Assert.True(first.To <= last.From);
        Assert.Equal(march.To, last.To);
        Assert.Equal(year.To, december.To);
    }

    [Fact]
    public void An_approximate_year_stands_where_the_year_stands()
    {
        Assert.Equal(YearAxis.Of(Year(97)), YearAxis.Of(Year(97).Approximately()));
    }

    [Theory]
    [InlineData(CalendarSystem.Gregorian)]
    [InlineData(CalendarSystem.Julian)]
    [InlineData(CalendarSystem.Unspecified)]
    public void The_axis_counts_years_whatever_calendar_states_them(CalendarSystem calendar)
    {
        Assert.Equal(new YearSpan(651m, 652m), YearAxis.Of(Year(651, Era.CommonEra, calendar)));
    }

    [Fact]
    public void A_position_of_another_era_or_of_another_kind_has_no_place_on_the_axis()
    {
        Assert.Null(YearAxis.Of(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1405)));
        Assert.Null(YearAxis.Of(CalendarPosition.OfYear(CalendarSystem.LunarHijri, Era.LunarHijri, 30)));
        Assert.Null(YearAxis.Of(Times.Step(3)));
    }

    [Theory]
    [InlineData(1.0, Era.CommonEra, 1)]
    [InlineData(700.9, Era.CommonEra, 700)]
    [InlineData(0.0, Era.BeforeCommonEra, 1)]
    [InlineData(0.99, Era.BeforeCommonEra, 1)]
    [InlineData(-0.5, Era.BeforeCommonEra, 2)]
    [InlineData(-599.0, Era.BeforeCommonEra, 600)]
    public void A_place_on_the_axis_lies_in_one_year_of_one_era(double place, Era era, int year)
    {
        Assert.Equal((era, year), YearAxis.YearAt((decimal)place));
    }

    [Theory]
    [InlineData(Era.CommonEra, 1)]
    [InlineData(Era.CommonEra, 476)]
    [InlineData(Era.BeforeCommonEra, 1)]
    [InlineData(Era.BeforeCommonEra, 509)]
    public void The_start_of_a_year_and_the_year_at_a_place_agree(Era era, int year)
    {
        Assert.Equal((era, year), YearAxis.YearAt(YearAxis.StartOf(era, year)));
        Assert.Equal(YearAxis.StartOf(era, year), YearAxis.Of(Year(year, era))!.Value.From);
    }

    [Fact]
    public void The_start_of_a_year_is_asked_for_a_year_of_the_axis_only()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => YearAxis.StartOf(Era.CommonEra, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => YearAxis.StartOf(Era.SolarHijri, 1405));
    }
}
