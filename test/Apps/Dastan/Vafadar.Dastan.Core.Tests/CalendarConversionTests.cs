using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A date of one calendar as a date of another (step 19.9, ADR 0021, R-TIME-05): only a date stated to the day
/// in a calendar it names is converted, and what is not exact enough is left as it was written.
/// </summary>
public sealed class CalendarConversionTests
{
    private static CalendarPosition Day(CalendarSystem calendar, Era era, int year, int month, int day) => CalendarPosition.OfDay(calendar, era, year, month, day);

    [Theory]
    [InlineData(2026, 11, 20, 1405, 8, 29)]
    [InlineData(2026, 3, 21, 1405, 1, 1)]
    [InlineData(1979, 2, 11, 1357, 11, 22)]
    [InlineData(2000, 1, 1, 1378, 10, 11)]
    public void A_Gregorian_day_is_a_day_of_the_Solar_Hijri_calendar_and_back(int year, int month, int day, int solarYear, int solarMonth, int solarDay)
    {
        var gregorian = Day(CalendarSystem.Gregorian, Era.CommonEra, year, month, day);
        var solar = Day(CalendarSystem.SolarHijri, Era.SolarHijri, solarYear, solarMonth, solarDay);

        Assert.Equal(solar, CalendarConversion.To(gregorian, CalendarSystem.SolarHijri));
        Assert.Equal(gregorian, CalendarConversion.To(solar, CalendarSystem.Gregorian));
    }

    [Theory]
    [InlineData(2026, 11, 20)]
    [InlineData(2000, 1, 1)]
    [InlineData(1010, 3, 8)]
    public void A_day_goes_through_the_Lunar_Hijri_calendar_and_comes_back_as_itself(int year, int month, int day)
    {
        var gregorian = Day(CalendarSystem.Gregorian, Era.CommonEra, year, month, day);
        var lunar = CalendarConversion.To(gregorian, CalendarSystem.LunarHijri);

        Assert.NotNull(lunar);
        Assert.Equal((CalendarSystem.LunarHijri, Era.LunarHijri), (lunar.Calendar, lunar.Era));
        Assert.Equal(gregorian, CalendarConversion.To(lunar, CalendarSystem.Gregorian));

        // And from the Lunar to the Solar Hijri calendar: the same day as from the Gregorian.
        Assert.Equal(CalendarConversion.To(gregorian, CalendarSystem.SolarHijri), CalendarConversion.To(lunar, CalendarSystem.SolarHijri));
    }

    [Fact]
    public void A_known_day_of_the_Lunar_Hijri_calendar_is_the_day_the_calendar_of_Umm_al_Qura_gives()
    {
        // The first of Ramadan 1447 is 18 February 2026 in the calendar of Umm al-Qura.
        Assert.Equal(
            Day(CalendarSystem.Gregorian, Era.CommonEra, 2026, 2, 18),
            CalendarConversion.To(Day(CalendarSystem.LunarHijri, Era.LunarHijri, 1447, 9, 1), CalendarSystem.Gregorian));
    }

    [Fact]
    public void What_is_not_exact_enough_is_not_converted()
    {
        // A year and a month: a year of one calendar is no year of another.
        Assert.Null(CalendarConversion.To(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1010), CalendarSystem.SolarHijri));
        Assert.Null(CalendarConversion.To(CalendarPosition.OfMonth(CalendarSystem.Gregorian, Era.CommonEra, 1010, 3), CalendarSystem.SolarHijri));

        // A calendar the source does not say: none is given to it here.
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Unspecified, Era.CommonEra, 1010, 3, 8), CalendarSystem.SolarHijri));

        // The calendar it is already in, a calendar that is none of the three, and a day before the era.
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11, 20), CalendarSystem.Gregorian));
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11, 20), CalendarSystem.Julian));
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Gregorian, Era.BeforeCommonEra, 500, 3, 8), CalendarSystem.SolarHijri));

        // Before the Hijri calendars begin there is no day of them.
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Gregorian, Era.CommonEra, 500, 3, 8), CalendarSystem.SolarHijri));
        Assert.Null(CalendarConversion.To(Day(CalendarSystem.Gregorian, Era.CommonEra, 500, 3, 8), CalendarSystem.LunarHijri));
    }

    [Fact]
    public void A_day_that_is_about_stays_about_and_the_record_is_not_changed()
    {
        var stated = Day(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11, 20).Approximately();
        var converted = CalendarConversion.To(stated, CalendarSystem.SolarHijri);

        Assert.True(converted!.IsApproximate);
        Assert.Equal((2026, 11, 20, CalendarSystem.Gregorian), (stated.Year, stated.Month, stated.Day, stated.Calendar));
    }

    [Theory]
    [InlineData("fa", CalendarSystem.SolarHijri)]
    [InlineData("en", CalendarSystem.Gregorian)]
    [InlineData("de", CalendarSystem.Gregorian)]
    public void A_reader_reads_in_the_calendar_of_the_language_of_the_page(string language, CalendarSystem calendar)
    {
        Assert.Equal(calendar, CalendarConversion.Of(language));
    }
}
