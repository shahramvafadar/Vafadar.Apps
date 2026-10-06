using System.Globalization;

namespace Vafadar.Localization.Tests;

/// <summary>Date input by numbers: parts in each calendar, real month lengths, no silent adjustment, box order.</summary>
public sealed class CalendarDatesTests
{
    // 25 September 2026 = 3 Mehr 1405 = 14 Rabi al-Thani 1448.
    private static readonly DateOnly Date = new(2026, 9, 25);

    [Theory]
    [InlineData(CalendarSystem.Gregorian, 2026, 9, 25)]
    [InlineData(CalendarSystem.Persian, 1405, 7, 3)]
    [InlineData(CalendarSystem.Hijri, 1448, 4, 14)]
    public void Parts_and_creation_round_trip_in_every_calendar(CalendarSystem calendar, int year, int month, int day)
    {
        Assert.Equal((year, month, day), CalendarDates.Parts(Date, calendar));
        Assert.True(CalendarDates.TryCreate(year, month, day, calendar, out var date));
        Assert.Equal(Date, date);
    }

    [Theory]
    [InlineData(CalendarSystem.Gregorian, 2026, 2, 28)]
    [InlineData(CalendarSystem.Gregorian, 2028, 2, 29)]
    [InlineData(CalendarSystem.Persian, 1405, 1, 31)]
    [InlineData(CalendarSystem.Persian, 1405, 7, 30)]
    [InlineData(CalendarSystem.Persian, 1405, 12, 29)]
    public void A_month_has_the_length_of_its_calendar(CalendarSystem calendar, int year, int month, int days)
    {
        Assert.Equal(days, CalendarDates.DaysInMonth(year, month, calendar));
    }

    [Fact]
    public void A_lunar_month_has_29_or_30_days()
    {
        Assert.Contains(CalendarDates.DaysInMonth(1448, 4, CalendarSystem.Hijri), new[] { 29, 30 });
    }

    [Theory]
    [InlineData(CalendarSystem.Gregorian, 2026, 2, 30)]   // no 30 February
    [InlineData(CalendarSystem.Gregorian, 2026, 13, 1)]   // no month 13
    [InlineData(CalendarSystem.Gregorian, 2026, 0, 1)]
    [InlineData(CalendarSystem.Gregorian, 2026, 1, 0)]
    [InlineData(CalendarSystem.Persian, 1405, 7, 31)]     // Mehr has 30 days
    [InlineData(CalendarSystem.Gregorian, 1899, 12, 31)]  // before the first date that can be entered
    [InlineData(CalendarSystem.Gregorian, 2200, 1, 1)]
    [InlineData(CalendarSystem.Gregorian, 26, 9, 25)]     // a year still being typed
    public void A_date_that_does_not_exist_is_rejected_not_adjusted(CalendarSystem calendar, int year, int month, int day)
    {
        Assert.False(CalendarDates.TryCreate(year, month, day, calendar, out _));
    }

    [Theory]
    [InlineData("en", CalendarSystem.Gregorian, "Month,Day,Year")]
    [InlineData("de", CalendarSystem.Gregorian, "Day,Month,Year")]
    [InlineData("es", CalendarSystem.Gregorian, "Day,Month,Year")]
    [InlineData("fr", CalendarSystem.Gregorian, "Day,Month,Year")]
    [InlineData("it", CalendarSystem.Gregorian, "Day,Month,Year")]
    [InlineData("en", CalendarSystem.Persian, "Year,Month,Day")]
    [InlineData("de", CalendarSystem.Hijri, "Year,Month,Day")]
    [InlineData("fa", CalendarSystem.Persian, "Day,Month,Year")]   // laid out right to left: 1405 / 07 / 03
    [InlineData("fa", CalendarSystem.Gregorian, "Day,Month,Year")]
    public void The_boxes_follow_the_written_order_of_the_language_and_calendar(string culture, CalendarSystem calendar, string expected)
    {
        Assert.Equal(expected, string.Join(',', CalendarDates.InputOrder(CultureInfo.GetCultureInfo(culture), calendar)));
    }
}
