using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// Whether a time reaches a year (step 14.2): what is stated holds in its whole year, what is known only between
/// two times may have held, and what nobody states holds in no year.
/// </summary>
public sealed class YearCoverTests
{
    private static CalendarPosition Y(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    private static YearOfEra Ce(int year) => new(Era.CommonEra, year);

    private static Period Lasting(PeriodStart start, PeriodEnd end) => new(start, end);

    [Theory]
    [InlineData(999, Holding.No)]
    [InlineData(1000, Holding.Yes)]
    [InlineData(1001, Holding.No)]
    public void What_happened_in_a_year_holds_in_that_year_only(int year, Holding expected)
    {
        Assert.Equal(expected, YearCover.Of(new Occurrence(Y(1000)), Ce(year)));
    }

    [Fact]
    public void A_day_holds_in_its_year_and_a_year_that_is_about_only_may()
    {
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, 1000, 12, 31)), Ce(1000)));
        Assert.Equal(Holding.Possibly, YearCover.Of(new Occurrence(Y(1000).Approximately()), Ce(1000)));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(Y(1000).Approximately()), Ce(1001)));
    }

    [Theory]
    [InlineData(989, Holding.No)]
    [InlineData(990, Holding.Possibly)]
    [InlineData(995, Holding.Possibly)]
    [InlineData(996, Holding.No)]
    public void What_happened_between_two_years_may_have_happened_in_each_of_them(int year, Holding expected)
    {
        Assert.Equal(expected, YearCover.Of(new OccurrenceWindow(Y(990), Y(995)), Ce(year)));
    }

    [Theory]
    [InlineData(993, Holding.No)]
    [InlineData(994, Holding.Yes)]
    [InlineData(996, Holding.Yes)]
    [InlineData(999, Holding.Yes)]
    [InlineData(1000, Holding.No)]
    public void What_lasted_holds_from_the_year_it_began_to_the_year_it_ended(int year, Holding expected)
    {
        Assert.Equal(expected, YearCover.Of(Lasting(PeriodStart.At(Y(994)), PeriodEnd.At(Y(999))), Ce(year)));
    }

    [Theory]
    [InlineData(989, Holding.No)]
    [InlineData(990, Holding.Possibly)]
    [InlineData(993, Holding.Possibly)]
    [InlineData(994, Holding.Yes)]
    [InlineData(1000, Holding.Yes)]
    [InlineData(1003, Holding.Possibly)]
    [InlineData(1005, Holding.Possibly)]
    [InlineData(1006, Holding.No)]
    public void A_beginning_or_an_end_between_two_years_leaves_those_years_open(int year, Holding expected)
    {
        var period = Lasting(PeriodStart.Between(Y(990), Y(994)), PeriodEnd.Between(Y(1000), Y(1005)));

        Assert.Equal(expected, YearCover.Of(period, Ce(year)));
    }

    [Fact]
    public void What_still_lasts_holds_in_every_year_after_it_began()
    {
        var period = Lasting(PeriodStart.At(Y(1900)), PeriodEnd.Ongoing);

        Assert.Equal(Holding.No, YearCover.Of(period, Ce(1899)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, Ce(1900)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, Ce(2026)));
    }

    [Fact]
    public void An_end_nobody_states_leaves_every_later_year_open()
    {
        var unknown = Lasting(PeriodStart.At(Y(1004)), PeriodEnd.Unknown);
        var ended = Lasting(PeriodStart.At(Y(1004)), PeriodEnd.EndedAtUnknownTime);

        Assert.Equal(Holding.No, YearCover.Of(unknown, Ce(1003)));
        Assert.Equal(Holding.Possibly, YearCover.Of(unknown, Ce(1004)));
        Assert.Equal(Holding.Possibly, YearCover.Of(unknown, Ce(1500)));
        Assert.Equal(Holding.Possibly, YearCover.Of(ended, Ce(1020)));
    }

    [Fact]
    public void A_beginning_nobody_knows_leaves_every_year_before_the_end_open()
    {
        var period = Lasting(PeriodStart.Unknown, PeriodEnd.At(Y(999)));

        Assert.Equal(Holding.Possibly, YearCover.Of(period, Ce(500)));
        Assert.Equal(Holding.Possibly, YearCover.Of(period, Ce(999)));
        Assert.Equal(Holding.No, YearCover.Of(period, Ce(1000)));
        Assert.Equal(Holding.No, YearCover.Of(Lasting(PeriodStart.Unknown, PeriodEnd.Unknown), Ce(999)));
    }

    [Fact]
    public void A_beginning_that_is_about_leaves_its_year_open_and_the_years_inside_stand()
    {
        var period = Lasting(PeriodStart.At(Y(994).Approximately()), PeriodEnd.At(Y(1010).Approximately()));

        Assert.Equal(Holding.Possibly, YearCover.Of(period, Ce(994)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, Ce(1000)));
        Assert.Equal(Holding.Possibly, YearCover.Of(period, Ce(1010)));
    }

    [Fact]
    public void A_year_before_the_era_is_a_year_like_any_other()
    {
        var period = Lasting(
            PeriodStart.At(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 10)),
            PeriodEnd.At(Y(5)));

        Assert.Equal(Holding.No, YearCover.Of(period, new YearOfEra(Era.BeforeCommonEra, 11)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, new YearOfEra(Era.BeforeCommonEra, 10)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, new YearOfEra(Era.BeforeCommonEra, 1)));
        Assert.Equal(Holding.Yes, YearCover.Of(period, Ce(1)));
        Assert.Equal(Holding.No, YearCover.Of(period, Ce(6)));
        Assert.Equal(new YearOfEra(Era.BeforeCommonEra, 1), YearOfEra.At(0));
    }

    [Fact]
    public void What_has_no_place_among_the_years_holds_in_none()
    {
        Assert.Equal(Holding.No, YearCover.Of(null, Ce(1000)));
        Assert.Equal(Holding.No, YearCover.Of(new UnknownTime(), Ce(1000)));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 400)), Ce(1021)));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(NarrativePosition.Of("story", NarrativeOrder.Presentation, 3)), Ce(3)));
        Assert.Null(YearCover.Reach(new UnknownTime()));
    }

    [Fact]
    public void One_of_several_times_only_may_hold()
    {
        var either = new AlternativeOccurrences([new Occurrence(Y(940)), new Occurrence(Y(935))]);

        Assert.Equal(Holding.Possibly, YearCover.Of(either, Ce(940)));
        Assert.Equal(Holding.Possibly, YearCover.Of(either, Ce(935)));
        Assert.Equal(Holding.No, YearCover.Of(either, Ce(937)));
        Assert.Equal(new YearSpan(935, 941), YearCover.Reach(either));
    }
}
