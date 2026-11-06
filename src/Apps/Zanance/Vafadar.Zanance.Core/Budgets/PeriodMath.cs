using System.Globalization;
using Vafadar.Core.Dates;

namespace Vafadar.Zanance.Core.Budgets;

/// <summary>
/// Months and days in the Gregorian, the Persian (Solar Hijri) and the lunar Hijri calendar. Every calendar calculation of
/// periods, plans and imports goes through <see cref="ToDate"/>, <see cref="DaysInMonth"/>, <see cref="DayOf"/> and
/// <see cref="MonthOf(DateOnly, PeriodCalendar)"/>, so a calendar is added in one place.
/// </summary>
public static class PeriodMath
{
    private static readonly PersianCalendar Persian = new();

    /// <summary>The latest day a financial month can start on, so that it exists in every month.</summary>
    public const int MaxStartDay = 28;

    /// <summary>
    /// Returns the first and last day of a financial month (§10.3, pay-cycle periods). With a start day above 1 the month
    /// runs from that day to the day before it in the next month and is named after the month it starts in: with the
    /// 25th, "September" is 25 September to 24 October. The last representable period ends at DateOnly.MaxValue.
    /// </summary>
    public static (DateOnly First, DateOnly Last) MonthRange(int year, int month, PeriodCalendar calendar, int startDay)
    {
        var day = Math.Clamp(startDay, 1, MaxStartDay);
        if (day == 1)
        {
            return MonthRange(year, month, calendar);
        }

        var (first, last) = MonthRange(year, month, calendar);
        // A real start is still required; clipping the end must not invent a start
        // beyond the partial final month or require an unavailable next month (D-810).
        var financialFirst = first.AddDays(day - 1);
        if (last == DateOnly.MaxValue)
        {
            return (financialFirst, last);
        }

        var (nextYear, nextMonth) = Next(year, month);
        var nextFirst = ToDate(nextYear, nextMonth, 1, calendar);
        var daysToEnd = Math.Min(day - 2, DateOnly.MaxValue.DayNumber - nextFirst.DayNumber);
        return (financialFirst, nextFirst.AddDays(daysToEnd));
    }

    /// <summary>Returns the financial month (year, month) that contains <paramref name="date"/>.</summary>
    public static (int Year, int Month) MonthOf(DateOnly date, PeriodCalendar calendar, int startDay)
    {
        var (year, month) = MonthOf(date, calendar);
        // Identity needs only the local day; materializing a complete range can
        // overflow at a valid calendar endpoint (D-810).
        return DayOf(date, calendar) < Math.Clamp(startDay, 1, MaxStartDay) ? Previous(year, month) : (year, month);
    }

    /// <summary>Returns the first and last representable day of a month.</summary>
    public static (DateOnly First, DateOnly Last) MonthRange(int year, int month, PeriodCalendar calendar)
    {
        var first = ToDate(year, month, 1, calendar);
        var remainingDays = DateOnly.MaxValue.DayNumber - first.DayNumber;
        return (first, first.AddDays(Math.Min(DaysInMonth(year, month, calendar) - 1, remainingDays)));
    }

    /// <summary>Returns the (year, month) that contains <paramref name="date"/>.</summary>
    public static (int Year, int Month) MonthOf(DateOnly date, PeriodCalendar calendar)
    {
        switch (calendar)
        {
            case PeriodCalendar.Persian:
                var dateTime = date.ToDateTime(TimeOnly.MinValue);
                return (Persian.GetYear(dateTime), Persian.GetMonth(dateTime));
            case PeriodCalendar.Hijri:
                var (year, month, _) = LunarHijri.Parts(date);
                return (year, month);
            default:
                return (date.Year, date.Month);
        }
    }

    /// <summary>Returns the day of the month of <paramref name="date"/> in a calendar.</summary>
    public static int DayOf(DateOnly date, PeriodCalendar calendar) => calendar switch
    {
        PeriodCalendar.Persian => Persian.GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue)),
        PeriodCalendar.Hijri => LunarHijri.Parts(date).Day,
        _ => date.Day,
    };

    /// <summary>Returns the date of a day of a calendar month.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year, month or day does not exist in the calendar.</exception>
    public static DateOnly ToDate(int year, int month, int day, PeriodCalendar calendar) => calendar switch
    {
        PeriodCalendar.Persian => DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0)),
        PeriodCalendar.Hijri => LunarHijri.ToDate(year, month, day),
        _ => new DateOnly(year, month, day),
    };

    /// <summary>Returns the number of days of a calendar month (lunar Hijri months have 29 or 30).</summary>
    public static int DaysInMonth(int year, int month, PeriodCalendar calendar) => calendar switch
    {
        PeriodCalendar.Persian => Persian.GetDaysInMonth(year, month),
        PeriodCalendar.Hijri => LunarHijri.DaysInMonth(year, month),
        _ => DateTime.DaysInMonth(year, month),
    };

    /// <summary>Returns whether a year can be converted in a calendar.</summary>
    public static bool IsSupportedYear(int year, PeriodCalendar calendar) => calendar switch
    {
        PeriodCalendar.Persian => year is >= 1 and <= 9378,
        PeriodCalendar.Hijri => year >= LunarHijri.MinYear && year <= LunarHijri.MaxYear,
        _ => year is >= 1 and <= 9999,
    };

    /// <summary>Returns the complete preceding financial-month window, or null when the calendar has too few earlier months.</summary>
    /// <remarks>The selected current period must itself be representable; missing history never legitimizes an invalid current period.</remarks>
    public static (DateOnly First, DateOnly Last)? PreviousCompleteMonthsRange(DateOnly today, PeriodCalendar calendar, int startDay, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        var current = MonthOf(today, calendar, startDay);
        _ = MonthRange(current.Year, current.Month, calendar, startDay);
        var cursor = current;
        DateOnly first = default, last = default;
        for (var i = 0; i < count; i++)
        {
            cursor = Previous(cursor.Year, cursor.Month);
            // D-904: an unsupported earlier year is missing history, never a fabricated complete month.
            if (!IsSupportedYear(cursor.Year, calendar)) return null;
            var range = MonthRange(cursor.Year, cursor.Month, calendar, startDay);
            first = range.First;
            if (i == 0) last = range.Last;
        }

        return (first, last);
    }

    /// <summary>Returns the month after (year, month).</summary>
    public static (int Year, int Month) Next(int year, int month) => month == 12 ? (year + 1, 1) : (year, month + 1);

    /// <summary>Returns the month before (year, month).</summary>
    public static (int Year, int Month) Previous(int year, int month) => month == 1 ? (year - 1, 12) : (year, month - 1);

    /// <summary>Returns the first and last representable day of the year that contains <paramref name="date"/>.</summary>
    public static (DateOnly First, DateOnly Last) YearRange(DateOnly date, PeriodCalendar calendar)
    {
        var (year, _) = MonthOf(date, calendar);
        // Persian and Hijri final years end before month twelve; Gregorian's
        // final year also has no representable following year (D-810).
        var last = IsSupportedYear(year + 1, calendar) ? MonthRange(year, 12, calendar).Last : DateOnly.MaxValue;
        return (ToDate(year, 1, 1, calendar), last);
    }
}
