using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The one reader of a time a visitor typed (step 19.9, ADR 0021, R-PUB-04): a year of the common era or before
/// it, a stretch of years and a depth of time, in English and in Persian – and what such a time reaches on the
/// one axis of years, which a depth of time shares with the calendar.
/// </summary>
public sealed class AskedTimeTests
{
    private const int Present = 2026;

    private static YearOfEra Ce(int year) => new(Era.CommonEra, year);

    private static YearOfEra Bce(int year) => new(Era.BeforeCommonEra, year);

    private static CalendarPosition Y(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    [Theory]
    [InlineData("1200", 1200, false)]
    [InlineData(" 1200 CE ", 1200, false)]
    [InlineData("1200 AD", 1200, false)]
    [InlineData("500 BCE", 500, true)]
    [InlineData("500bc", 500, true)]
    [InlineData("-500", 500, true)]
    [InlineData("−500", 500, true)]
    [InlineData("۵۰۰ پ.م", 500, true)]
    [InlineData("۵۰۰ پ.م.", 500, true)]
    [InlineData("۵۰۰ ق.م", 500, true)]
    [InlineData("۵۰۰ پیش از میلاد", 500, true)]
    [InlineData("۱۲۰۰ میلادی", 1200, false)]
    [InlineData("12,000 BCE", 12000, true)]
    [InlineData("1000000", 1000000, false)]
    public void A_year_is_read_with_its_era(string typed, int number, bool before)
    {
        var asked = Assert.IsType<AskedTime.Years>(AskedTime.Read(typed));
        Assert.True(asked.IsOneYear);
        Assert.Equal(before ? Bce(number) : Ce(number), asked.First);
        Assert.Equal(asked, AskedTime.Read(asked.Written));
    }

    [Theory]
    [InlineData("300 to 400", 300, false, 400, false)]
    [InlineData("300-400", 300, false, 400, false)]
    [InlineData("300–400 CE", 300, false, 400, false)]
    [InlineData("600 to 500 BCE", 600, true, 500, true)]
    [InlineData("600 BCE to 500 BCE", 600, true, 500, true)]
    [InlineData("50 BCE to 50", 50, true, 50, false)]
    [InlineData("-50..50", 50, true, 50, false)]
    [InlineData("۶۰۰ تا ۵۰۰ پ.م", 600, true, 500, true)]
    [InlineData("۳۰۰ تا ۴۰۰", 300, false, 400, false)]
    public void A_stretch_of_years_is_read_and_an_era_said_once_is_said_of_both(string typed, int first, bool firstBefore, int last, bool lastBefore)
    {
        var asked = Assert.IsType<AskedTime.Years>(AskedTime.Read(typed));
        Assert.Equal((firstBefore ? Bce(first) : Ce(first), lastBefore ? Bce(last) : Ce(last)), (asked.First, asked.Last));
        Assert.False(asked.IsOneYear);
        Assert.Equal(asked, AskedTime.Read(asked.Written));
    }

    [Theory]
    [InlineData("12 ka", 12_000, 12_000)]
    [InlineData("12 kya", 12_000, 12_000)]
    [InlineData("12,000 years ago", 12_000, 12_000)]
    [InlineData("55 Ma", 55_000_000, 55_000_000)]
    [InlineData("55 million years ago", 55_000_000, 55_000_000)]
    [InlineData("50-60 Ma", 60_000_000, 50_000_000)]
    [InlineData("50 to 60 million years ago", 60_000_000, 50_000_000)]
    [InlineData("4.5 Ga", 4_500_000_000, 4_500_000_000)]
    [InlineData("2.5 billion years ago", 2_500_000_000, 2_500_000_000)]
    [InlineData("10 to 20 thousand years ago", 20_000, 10_000)]
    [InlineData("۵۵ میلیون سال پیش", 55_000_000, 55_000_000)]
    [InlineData("۵۰ تا ۶۰ میلیون سال پیش", 60_000_000, 50_000_000)]
    [InlineData("۱۰ تا ۲۰ هزار سال پیش", 20_000, 10_000)]
    [InlineData("۲ میلیارد سال پیش", 2_000_000_000, 2_000_000_000)]
    [InlineData("۳۰۰ سال پیش", 300, 300)]
    public void A_depth_of_time_is_read_in_its_unit(string typed, long oldest, long youngest)
    {
        var asked = Assert.IsType<AskedTime.Ago>(AskedTime.Read(typed));
        Assert.Equal((oldest, youngest), ((long)asked.Oldest, (long)asked.Youngest));
        Assert.Equal(asked, AskedTime.Read(asked.Written));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("0 BCE")]
    [InlineData("-0")]
    [InlineData("+300")]
    [InlineData("1000001")]
    [InlineData("-2147483648")]
    [InlineData("2147483648")]
    [InlineData("99999999999999999999999999999999")]
    [InlineData("-500 CE")]
    [InlineData("400 to 300")]
    [InlineData("500 BCE to 600 BCE")]
    [InlineData("early")]
    [InlineData("12 kb")]
    [InlineData("0 Ma")]
    [InlineData("200 Ga")]
    [InlineData("1e9")]
    [InlineData("12.5")]
    [InlineData("<script>")]
    [InlineData("1200; drop")]
    public void What_is_no_time_is_no_time_and_never_an_error(string? typed)
    {
        Assert.Null(AskedTime.Read(typed));
    }

    [Fact]
    public void A_typed_text_of_any_length_is_answered()
    {
        Assert.Null(AskedTime.Read(new string('9', 5000)));
        Assert.Null(AskedTime.Read(new string('-', 79) + "1"));
    }

    [Fact]
    public void A_stretch_of_years_reaches_what_lies_in_it_and_may_reach_what_is_about()
    {
        var asked = AskedTime.Read("600 to 500 BCE")!.Span(Present);
        CalendarPosition B(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, year);

        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(B(550)), asked, Present));
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(B(600)), asked, Present));
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(B(500)), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(B(601)), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(B(499)), asked, Present));
        Assert.Equal(Holding.Possibly, YearCover.Of(new Occurrence(B(550).Approximately()), asked, Present));

        // A window that lies in the stretch happened in it; one that reaches out of it may have.
        Assert.Equal(Holding.Yes, YearCover.Of(new OccurrenceWindow(B(580), B(520)), asked, Present));
        Assert.Equal(Holding.Possibly, YearCover.Of(new OccurrenceWindow(B(650), B(590)), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new OccurrenceWindow(B(700), B(650)), asked, Present));

        // What lasted through it held in it.
        Assert.Equal(Holding.Yes, YearCover.Of(new Period(PeriodStart.At(B(700)), PeriodEnd.At(B(400))), asked, Present));
        Assert.Equal(Holding.Yes, YearCover.Of(new Period(PeriodStart.At(B(700)), PeriodEnd.Ongoing), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Period(PeriodStart.At(B(400)), PeriodEnd.At(B(300))), asked, Present));
    }

    [Fact]
    public void A_depth_of_time_and_the_calendar_share_one_axis_of_years()
    {
        // An age that says what it is counted from lies at a year: 12 thousand years before 2000 is the year 10001 BCE.
        var age = AgePosition.Of(12, AgeUnit.ThousandYears, AgeReference.Year(2000));
        Assert.Equal(YearAxis.StartOf(Era.BeforeCommonEra, 10001), TimePlaces.Of(age, present: null)!.Value.From);
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(age), AskedTime.Read("10001 BCE")!.Span(Present), Present));
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(age), AskedTime.Read("10 to 20 thousand years ago")!.Span(Present), Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(age), AskedTime.Read("20 to 30 ka")!.Span(Present), Present));

        // And a year of the calendar is so many years ago: 500 BCE is found among "2 to 3 thousand years ago".
        var year = CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, 500);
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(year), AskedTime.Read("2 to 3 ka")!.Span(Present), Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(year), AskedTime.Read("3 to 4 ka")!.Span(Present), Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(Y(1200)), AskedTime.Read("55 Ma")!.Span(Present), Present));
    }

    [Fact]
    public void Deep_time_keeps_its_meaning_and_what_is_less_than_exact_only_may_be_reached()
    {
        var between = AskedTime.Read("50 to 60 million years ago")!.Span(Present);
        AgePosition Ma(decimal value, AgeReference reference) => AgePosition.Of(value, AgeUnit.MillionYears, reference);

        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(Ma(55, AgeReference.BeforePresent)), between, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(Ma(66, AgeReference.BeforePresent)), between, Present));

        // "Years ago" that does not say from when is counted from the year of the asking, and only may be reached.
        Assert.Equal(Holding.Possibly, YearCover.Of(new Occurrence(Ma(55, AgeReference.Unspecified)), between, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(Ma(66, AgeReference.Unspecified)), between, Present));
        Assert.Null(TimePlaces.Of(Ma(55, AgeReference.Unspecified), present: null));

        // Said to be about, or counted in radiocarbon years: may be.
        Assert.Equal(Holding.Possibly, YearCover.Of(new Occurrence(Ma(55, AgeReference.BeforePresent).Approximately()), between, Present));
        Assert.Equal(
            Holding.Possibly,
            YearCover.Of(new Occurrence(AgePosition.Of(12, AgeUnit.ThousandYears, AgeReference.BeforePresent, AgeBasis.RadiocarbonYears)), AskedTime.Read("10-20 ka")!.Span(Present), Present));

        // A window between two depths, and a stretch from one depth to another.
        Assert.Equal(Holding.Yes, YearCover.Of(new OccurrenceWindow(Ma(58, AgeReference.BeforePresent), Ma(52, AgeReference.BeforePresent)), between, Present));
        Assert.Equal(Holding.Possibly, YearCover.Of(new OccurrenceWindow(Ma(70, AgeReference.BeforePresent), Ma(55, AgeReference.BeforePresent)), between, Present));
        Assert.Equal(
            Holding.Yes,
            YearCover.Of(new Period(PeriodStart.At(Ma(66, AgeReference.BeforePresent)), PeriodEnd.At(Ma(23, AgeReference.BeforePresent))), between, Present));

        // A year of the calendar alone is asked without a present: a depth of time then reaches no year, as before.
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(Ma(55, AgeReference.Unspecified)), new YearOfEra(Era.CommonEra, 1200)));

        // A place in a telling has no year at all.
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(NarrativePosition.Of("story", NarrativeOrder.Presentation, 3)), between, Present));
    }

    [Theory]
    [InlineData("55 Ma", 500_000)]
    [InlineData("5.3 Ma", 50_000)]
    [InlineData("12 ka", 500)]
    [InlineData("12,000 years ago", 500)]
    [InlineData("1028 years ago", 100)]
    [InlineData("300 years ago", 100)]
    [InlineData("4.5 Ga", 50_000_000)]
    [InlineData("50 to 60 Ma", 0)]
    public void One_depth_of_time_is_taken_with_half_of_its_last_digit_to_each_side_and_a_stretch_as_it_is(string typed, long slack)
    {
        var asked = Assert.IsType<AskedTime.Ago>(AskedTime.Read(typed));
        Assert.Equal(slack, (long)asked.Slack);
        var span = asked.Span(Present);
        Assert.Equal(asked.Oldest - asked.Youngest + (2 * slack) + 1, span.Length);
    }

    [Fact]
    public void A_depth_that_is_asked_for_finds_what_is_said_to_be_so_old_whatever_it_is_counted_from()
    {
        var asked = AskedTime.Read("55 Ma")!.Span(Present);
        foreach (var reference in new[] { AgeReference.BeforePresent, AgeReference.Year(2000), AgeReference.Year(Present) })
        {
            Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(AgePosition.Of(55, AgeUnit.MillionYears, reference)), asked, Present));
        }

        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(AgePosition.Of(55.4m, AgeUnit.MillionYears, AgeReference.BeforePresent)), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(AgePosition.Of(56, AgeUnit.MillionYears, AgeReference.BeforePresent)), asked, Present));
        Assert.Equal(Holding.No, YearCover.Of(new Occurrence(AgePosition.Of(54, AgeUnit.MillionYears, AgeReference.BeforePresent)), asked, Present));
    }

    [Fact]
    public void The_furthest_depth_of_time_has_a_place()
    {
        var asked = Assert.IsType<AskedTime.Ago>(AskedTime.Read("100 Ga"));
        var span = asked.Span(Present);
        Assert.True(span.From < -99_000_000_000m);
        Assert.Equal(Holding.Yes, YearCover.Of(new Occurrence(AgePosition.Of(100, AgeUnit.BillionYears, AgeReference.Year(Present))), span, Present));
    }
}
