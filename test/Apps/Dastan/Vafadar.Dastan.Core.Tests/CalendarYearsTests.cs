using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A bare year in another calendar (D-100): a year of the common era lies in two years of a Hijri calendar and
/// both are named; nothing is converted that lies before the calendar or is no year of the common era; and a
/// Hijri year a visitor types asks for the years of the common era it lies in.
/// </summary>
public sealed class CalendarYearsTests
{
    private static CalendarPosition Year(int year, Era era = Era.CommonEra, CalendarSystem calendar = CalendarSystem.Gregorian) => CalendarPosition.OfYear(calendar, era, year);

    [Theory]
    [InlineData(2026, CalendarSystem.SolarHijri, Era.SolarHijri, 1404, 1405)]
    [InlineData(1010, CalendarSystem.SolarHijri, Era.SolarHijri, 388, 389)]
    [InlineData(623, CalendarSystem.SolarHijri, Era.SolarHijri, 1, 2)]
    [InlineData(2026, CalendarSystem.LunarHijri, Era.LunarHijri, 1447, 1448)]
    [InlineData(1010, CalendarSystem.LunarHijri, Era.LunarHijri, 400, 401)]
    public void A_year_of_the_common_era_lies_in_two_years_of_a_Hijri_calendar(int year, CalendarSystem calendar, Era era, int first, int last)
    {
        var years = CalendarConversion.YearsIn(Year(year), calendar);

        Assert.Equal((CalendarPosition.OfYear(calendar, era, first), CalendarPosition.OfYear(calendar, era, last)), years);

        // A year whose calendar the record does not name is a Gregorian year: every date is stored so (D-043).
        Assert.Equal(years, CalendarConversion.YearsIn(Year(year, calendar: CalendarSystem.Unspecified), calendar));
    }

    [Fact]
    public void About_a_year_stays_about_both()
    {
        var (first, last) = CalendarConversion.YearsIn(Year(977).Approximately(), CalendarSystem.SolarHijri)!.Value;

        Assert.True(first.IsApproximate);
        Assert.True(last.IsApproximate);
        Assert.Equal((355, 356), (first.Year, last.Year));
    }

    [Fact]
    public void What_lies_before_a_calendar_or_is_no_year_of_the_common_era_is_not_converted()
    {
        // Before the Hijra, and the year of it: its first day lies before the calendar.
        Assert.Null(CalendarConversion.YearsIn(Year(500), CalendarSystem.SolarHijri));
        Assert.Null(CalendarConversion.YearsIn(Year(622), CalendarSystem.SolarHijri));
        Assert.Null(CalendarConversion.YearsIn(Year(500), CalendarSystem.LunarHijri));
        Assert.Null(CalendarConversion.YearsIn(Year(550, Era.BeforeCommonEra), CalendarSystem.SolarHijri));

        // No bare year, no Hijri calendar asked for, a year that is one of a Hijri calendar already.
        Assert.Null(CalendarConversion.YearsIn(CalendarPosition.OfMonth(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11), CalendarSystem.SolarHijri));
        Assert.Null(CalendarConversion.YearsIn(Year(2026), CalendarSystem.Gregorian));
        Assert.Null(CalendarConversion.YearsIn(Year(1405, Era.SolarHijri, CalendarSystem.SolarHijri), CalendarSystem.LunarHijri));
    }

    [Theory]
    [InlineData(CalendarSystem.SolarHijri, 1400, 2021, 2022)]
    [InlineData(CalendarSystem.SolarHijri, 1, 622, 623)]
    [InlineData(CalendarSystem.LunarHijri, 1447, 2025, 2026)]
    public void A_year_of_a_Hijri_calendar_lies_in_years_of_the_common_era(CalendarSystem calendar, int year, int first, int last)
    {
        Assert.Equal((first, last), CalendarConversion.CommonEraYearsOf(calendar, year));
    }

    [Fact]
    public void A_year_no_calendar_counts_has_no_years_of_the_common_era()
    {
        Assert.Null(CalendarConversion.CommonEraYearsOf(CalendarSystem.SolarHijri, 0));
        Assert.Null(CalendarConversion.CommonEraYearsOf(CalendarSystem.Gregorian, 2026));
        Assert.Null(CalendarConversion.CommonEraYearsOf(CalendarSystem.LunarHijri, 20000));
    }

    [Theory]
    [InlineData("1400 SH", 2021, 2022)]
    [InlineData("1400sh", 2021, 2022)]
    [InlineData("۱۴۰۰ ه.ش", 2021, 2022)]
    [InlineData("۱۴۰۰ ه‍.ش", 2021, 2022)]
    [InlineData("۱۴۰۰ شمسی", 2021, 2022)]
    [InlineData("۱۴۰۰ هجری خورشیدی", 2021, 2022)]
    [InlineData("1447 AH", 2025, 2026)]
    [InlineData("۱۴۴۷ ه.ق", 2025, 2026)]
    [InlineData("۱۴۴۷ قمری", 2025, 2026)]
    public void A_Hijri_year_that_is_typed_asks_for_the_years_of_the_common_era_it_lies_in(string typed, int first, int last)
    {
        var asked = Assert.IsType<AskedTime.Years>(AskedTime.Read(typed));

        Assert.Equal((new YearOfEra(Era.CommonEra, first), new YearOfEra(Era.CommonEra, last)), (asked.First, asked.Last));
    }

    [Theory]
    [InlineData("0 SH")]
    [InlineData("99999 AH")]
    [InlineData("SH 1400")]
    public void A_Hijri_year_that_cannot_be_is_not_read(string typed)
    {
        Assert.Null(AskedTime.Read(typed));
    }
}
