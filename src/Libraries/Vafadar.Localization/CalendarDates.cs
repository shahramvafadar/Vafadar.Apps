using System.Globalization;
using Vafadar.Core.Dates;

namespace Vafadar.Localization;

/// <summary>A part of a date that is entered on its own.</summary>
public enum DatePart
{
    /// <summary>The day of the month.</summary>
    Day,

    /// <summary>The month number (1–12).</summary>
    Month,

    /// <summary>The year.</summary>
    Year,
}

/// <summary>
/// Converts between a stored Gregorian <see cref="DateOnly"/> and the year, month and day of a display calendar, for
/// date input by numbers (the date field). Every calendar has twelve months; their lengths come from the calendar.
/// </summary>
public static class CalendarDates
{
    /// <summary>The earliest date that can be entered (the date field turns earlier values into today).</summary>
    public static DateOnly MinDate { get; } = new(1900, 1, 1);

    /// <summary>The latest date that can be entered.</summary>
    public static DateOnly MaxDate { get; } = new(2199, 12, 31);

    private static readonly PersianCalendar Persian = new();

    /// <summary>Returns the year, month and day of <paramref name="date"/> in <paramref name="calendar"/>.</summary>
    public static (int Year, int Month, int Day) Parts(DateOnly date, CalendarSystem calendar)
    {
        switch (calendar)
        {
            case CalendarSystem.Persian:
                var value = date.ToDateTime(TimeOnly.MinValue);
                return (Persian.GetYear(value), Persian.GetMonth(value), Persian.GetDayOfMonth(value));
            case CalendarSystem.Hijri:
                return LunarHijri.Parts(date);
            default:
                return (date.Year, date.Month, date.Day);
        }
    }

    /// <summary>
    /// Returns the number of days of a month in <paramref name="calendar"/>, or 0 when the year or month does not exist
    /// there or lies outside <see cref="MinDate"/> to <see cref="MaxDate"/>.
    /// </summary>
    public static int DaysInMonth(int year, int month, CalendarSystem calendar)
    {
        if (month is < 1 or > 12 || year < 1)
        {
            return 0;
        }

        var (minYear, maxYear) = YearRange(calendar);
        if (year < minYear || year > maxYear)
        {
            return 0;
        }

        return calendar switch
        {
            CalendarSystem.Persian => Persian.GetDaysInMonth(year, month),
            CalendarSystem.Hijri => LunarHijri.DaysInMonth(year, month),
            _ => DateTime.DaysInMonth(year, month),
        };
    }

    /// <summary>
    /// Creates the Gregorian date of a year, month and day in <paramref name="calendar"/>. Fails for a day or month that
    /// does not exist (e.g. day 31 of a 30-day month) and outside <see cref="MinDate"/> to <see cref="MaxDate"/>; the
    /// values are never adjusted silently.
    /// </summary>
    public static bool TryCreate(int year, int month, int day, CalendarSystem calendar, out DateOnly date)
    {
        date = default;
        var days = DaysInMonth(year, month, calendar);
        if (day < 1 || day > days)
        {
            return false;
        }

        var result = calendar switch
        {
            CalendarSystem.Persian => DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0)),
            CalendarSystem.Hijri => LunarHijri.ToDate(year, month, day),
            _ => new DateOnly(year, month, day),
        };
        if (result < MinDate || result > MaxDate)
        {
            return false;
        }

        date = result;
        return true;
    }

    /// <summary>
    /// The order of day, month and year for input in <paramref name="culture"/> and <paramref name="calendar"/>.
    /// </summary>
    /// <remarks>
    /// Gregorian dates follow the culture's short date pattern (English month/day/year, German day.month.year).
    /// Without an explicit regional pattern, Persian and lunar Hijri dates are written year/month/day ("1405/07/03", as the app shows them). In a
    /// right-to-left language the parts are listed day, month, year: laid out from the right, they read
    /// "1405 / 07 / 03" on screen like the written date. An explicit regional pattern controls all calendars.
    /// </remarks>
    public static IReadOnlyList<DatePart> InputOrder(CultureInfo culture, CalendarSystem calendar, bool useRegionalPattern = false)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!useRegionalPattern && culture.TextInfo.IsRightToLeft)
        {
            return [DatePart.Day, DatePart.Month, DatePart.Year];
        }

        if (!useRegionalPattern && calendar != CalendarSystem.Gregorian)
        {
            return [DatePart.Year, DatePart.Month, DatePart.Day];
        }

        var pattern = culture.DateTimeFormat.ShortDatePattern;
        var order = new[] { (DatePart.Day, pattern.IndexOf('d', StringComparison.Ordinal)), (DatePart.Month, pattern.IndexOf('M', StringComparison.Ordinal)), (DatePart.Year, pattern.IndexOf('y', StringComparison.Ordinal)) }
            .OrderBy(p => p.Item2 < 0 ? int.MaxValue : p.Item2)
            .Select(p => p.Item1)
            .ToArray();
        // MAUI mirrors the horizontal fields in RTL; reverse the logical order to retain the selected numeric layout.
        return culture.TextInfo.IsRightToLeft ? order.Reverse().ToArray() : order;
    }

    // The years of MinDate and MaxDate in each calendar (a year at an edge is checked again by TryCreate).
    private static (int Min, int Max) YearRange(CalendarSystem calendar) => calendar switch
    {
        CalendarSystem.Persian => (Parts(MinDate, calendar).Year, Parts(MaxDate, calendar).Year),
        CalendarSystem.Hijri => (LunarHijri.Parts(MinDate).Year, LunarHijri.Parts(MaxDate).Year),
        _ => (MinDate.Year, MaxDate.Year),
    };
}
