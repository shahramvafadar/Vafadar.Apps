using System.Globalization;

namespace Vafadar.Finance.Core.Budgets;

/// <summary>Month periods in the Gregorian and the Persian calendar.</summary>
public static class PeriodMath
{
    private static readonly PersianCalendar Persian = new();

    /// <summary>Returns the first and last day of a month.</summary>
    public static (DateOnly First, DateOnly Last) MonthRange(int year, int month, PeriodCalendar calendar)
    {
        if (calendar == PeriodCalendar.Persian)
        {
            var first = DateOnly.FromDateTime(Persian.ToDateTime(year, month, 1, 0, 0, 0, 0));
            var days = Persian.GetDaysInMonth(year, month);
            return (first, first.AddDays(days - 1));
        }

        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    /// <summary>Returns the (year, month) that contains <paramref name="date"/>.</summary>
    public static (int Year, int Month) MonthOf(DateOnly date, PeriodCalendar calendar)
    {
        if (calendar == PeriodCalendar.Persian)
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            return (Persian.GetYear(dateTime), Persian.GetMonth(dateTime));
        }

        return (date.Year, date.Month);
    }

    /// <summary>Returns the month after (year, month).</summary>
    public static (int Year, int Month) Next(int year, int month) => month == 12 ? (year + 1, 1) : (year, month + 1);

    /// <summary>Returns the month before (year, month).</summary>
    public static (int Year, int Month) Previous(int year, int month) => month == 1 ? (year - 1, 12) : (year, month - 1);

    /// <summary>Returns the first and last day of the year that contains <paramref name="date"/>.</summary>
    public static (DateOnly First, DateOnly Last) YearRange(DateOnly date, PeriodCalendar calendar)
    {
        var (year, _) = MonthOf(date, calendar);
        return (MonthRange(year, 1, calendar).First, MonthRange(year, 12, calendar).Last);
    }
}
