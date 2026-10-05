using System.Globalization;

namespace Vafadar.Core.Dates;

/// <summary>
/// The lunar Hijri (Islamic) calendar of the apps: one place that turns dates into Hijri years, months and days and back,
/// so display, periods, plans and imports always agree.
/// </summary>
/// <remarks>
/// For the years 1318 to 1500 AH (30 April 1900 to 16 November 2077) it is the Umm al-Qura calendar – the official
/// calendar of Saudi Arabia and the "Islamic (Umm al-Qura)" calendar of Android and iOS. Outside these years it falls back
/// to the tabular Hijri calendar without adjustment, so every date stays convertible; the switch happens at year
/// boundaries. Months announced by moon sighting in other countries can differ by a day; the app does not claim them.
/// </remarks>
public static class LunarHijri
{
    /// <summary>The first year covered by the Umm al-Qura calendar.</summary>
    public const int FirstUmAlQuraYear = 1318;

    /// <summary>The last year covered by the Umm al-Qura calendar.</summary>
    public const int LastUmAlQuraYear = 1500;

    private static readonly UmAlQuraCalendar UmAlQura = new();

    // No adjustment: the machine's Hijri setting must not move a date from one device to another.
    private static readonly HijriCalendar Tabular = new() { HijriAdjustment = 0 };

    /// <summary>Gets the first year that can be converted.</summary>
    public static int MinYear => 1;

    /// <summary>Gets the last year that can be converted (the tabular calendar ends in 9666 AH).</summary>
    public static int MaxYear => 9666;

    /// <summary>Returns the calendar used for a Hijri year.</summary>
    public static Calendar ForYear(int year) => year is >= FirstUmAlQuraYear and <= LastUmAlQuraYear ? UmAlQura : Tabular;

    /// <summary>Returns the calendar used for a date.</summary>
    public static Calendar ForDate(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return dateTime >= UmAlQura.MinSupportedDateTime && dateTime <= UmAlQura.MaxSupportedDateTime ? UmAlQura : Tabular;
    }

    /// <summary>Returns whether <paramref name="date"/> can be shown as a Hijri date.</summary>
    public static bool Supports(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return dateTime >= Tabular.MinSupportedDateTime && dateTime <= Tabular.MaxSupportedDateTime;
    }

    /// <summary>Returns the Hijri year, month (1–12) and day of <paramref name="date"/>.</summary>
    public static (int Year, int Month, int Day) Parts(DateOnly date)
    {
        var calendar = ForDate(date);
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return (calendar.GetYear(dateTime), calendar.GetMonth(dateTime), calendar.GetDayOfMonth(dateTime));
    }

    /// <summary>Returns the Gregorian date of a Hijri day.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year, month or day does not exist.</exception>
    public static DateOnly ToDate(int year, int month, int day) => DateOnly.FromDateTime(ForYear(year).ToDateTime(year, month, day, 0, 0, 0, 0));

    /// <summary>Returns the number of days (29 or 30) of a Hijri month.</summary>
    public static int DaysInMonth(int year, int month) => ForYear(year).GetDaysInMonth(year, month);
}
