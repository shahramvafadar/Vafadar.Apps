using System.Globalization;
using Vafadar.Core.Dates;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// A date of one calendar as a date of another (ADR 0021, R-TIME-05): Gregorian, Solar Hijri and Lunar Hijri.
/// The date of a record is never changed: it is shown as it was written, and a converted date may stand beside
/// it and is said to be converted. A day is converted when it is stated to the day, in a calendar it names. A
/// year of one calendar is no year of another: a year of the common era lies in two years of a Hijri calendar,
/// and both are said (<see cref="YearsIn"/>, D-100) – never one of them as if it were the year. A month is
/// not converted.
/// </summary>
public static class CalendarConversion
{
    private static readonly PersianCalendar SolarHijri = new();

    /// <summary>
    /// Returns the calendar a reader of a language reads dates in: Solar Hijri for Persian, Gregorian otherwise.
    /// </summary>
    /// <param name="language">The tag of the language of the page.</param>
    public static CalendarSystem Of(string language) => language == "fa" ? CalendarSystem.SolarHijri : CalendarSystem.Gregorian;

    /// <summary>
    /// Returns the date in another calendar, or <see langword="null"/> when it is not to be converted: it is not
    /// stated to the day, its calendar is not stated or is the one asked for, it lies before the era of its
    /// calendar, or it lies where the calendars are not counted against each other here.
    /// </summary>
    /// <param name="position">The date as the record states it.</param>
    /// <param name="calendar">The calendar asked for.</param>
    public static CalendarPosition? To(CalendarPosition position, CalendarSystem calendar)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Month is not { } month || position.Day is not { } day || position.Calendar == calendar
            || calendar is not (CalendarSystem.Gregorian or CalendarSystem.SolarHijri or CalendarSystem.LunarHijri))
        {
            return null;
        }

        try
        {
            DateOnly? date = (position.Calendar, position.Era) switch
            {
                (CalendarSystem.Gregorian, Era.CommonEra) when position.Year <= 9999 => new DateOnly(position.Year, month, day),
                (CalendarSystem.SolarHijri, Era.SolarHijri) => DateOnly.FromDateTime(SolarHijri.ToDateTime(position.Year, month, day, 0, 0, 0, 0)),
                (CalendarSystem.LunarHijri, Era.LunarHijri) when position.Year <= LunarHijri.MaxYear => LunarHijri.ToDate(position.Year, month, day),
                _ => null,
            };
            if (date is not { } found)
            {
                return null;
            }

            var moment = found.ToDateTime(TimeOnly.MinValue);
            CalendarPosition? converted = calendar switch
            {
                CalendarSystem.Gregorian => CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, found.Year, found.Month, found.Day),
                CalendarSystem.SolarHijri when moment >= SolarHijri.MinSupportedDateTime =>
                    CalendarPosition.OfDay(CalendarSystem.SolarHijri, Era.SolarHijri, SolarHijri.GetYear(moment), SolarHijri.GetMonth(moment), SolarHijri.GetDayOfMonth(moment)),
                CalendarSystem.LunarHijri when LunarHijri.Supports(found) && LunarHijri.Parts(found) is var (year, lunarMonth, lunarDay) =>
                    CalendarPosition.OfDay(CalendarSystem.LunarHijri, Era.LunarHijri, year, lunarMonth, lunarDay),
                _ => null,
            };
            return converted is not null && position.IsApproximate ? converted.Approximately() : converted;
        }
        catch (ArgumentException)
        {
            // A day the calendar does not have, or a year outside of what is counted here: not converted.
            return null;
        }
    }

    /// <summary>
    /// Returns the years of a Hijri calendar a year of the common era lies in – the year of its first day and
    /// the year of its last, which are two years, or one where the calendars happen to agree – or
    /// <see langword="null"/> when the year is not to be converted: it is no bare year, it is not of the common
    /// era, it lies before the calendar asked for begins, or the calendar asked for is none of the two. What is
    /// "about" a year stays "about" both. Every date is stored in the Gregorian calendar (D-043), so a year
    /// whose calendar the record does not name is a Gregorian year here.
    /// </summary>
    /// <param name="position">The year as the record states it.</param>
    /// <param name="calendar">The calendar asked for: Solar Hijri or Lunar Hijri.</param>
    public static (CalendarPosition First, CalendarPosition Last)? YearsIn(CalendarPosition position, CalendarSystem calendar)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Month is not null || position.Era != Era.CommonEra || position.Year is < 1 or > 9998
            || position.Calendar is not (CalendarSystem.Gregorian or CalendarSystem.Unspecified))
        {
            return null;
        }

        var (first, last) = (new DateOnly(position.Year, 1, 1), new DateOnly(position.Year, 12, 31));
        (Era Era, int First, int Last)? years = calendar switch
        {
            CalendarSystem.SolarHijri when first.ToDateTime(TimeOnly.MinValue) >= SolarHijri.MinSupportedDateTime =>
                (Era.SolarHijri, SolarHijri.GetYear(first.ToDateTime(TimeOnly.MinValue)), SolarHijri.GetYear(last.ToDateTime(TimeOnly.MinValue))),
            CalendarSystem.LunarHijri when LunarHijri.Supports(first) && LunarHijri.Supports(last) =>
                (Era.LunarHijri, LunarHijri.Parts(first).Year, LunarHijri.Parts(last).Year),
            _ => null,
        };
        if (years is not { } found)
        {
            return null;
        }

        CalendarPosition Of(int year)
        {
            var converted = CalendarPosition.OfYear(calendar, found.Era, year);
            return position.IsApproximate ? converted.Approximately() : converted;
        }

        return (Of(found.First), Of(found.Last));
    }

    /// <summary>
    /// Returns the years of the common era a year of a Hijri calendar lies in – the year of its first day and
    /// the year of its last – or <see langword="null"/> where the calendar is none of the two or the year is
    /// not counted here. It is what a visitor who types a Hijri year asks for.
    /// </summary>
    /// <param name="calendar">Solar Hijri or Lunar Hijri.</param>
    /// <param name="year">The year of that calendar.</param>
    public static (int First, int Last)? CommonEraYearsOf(CalendarSystem calendar, int year)
    {
        if (year < 1)
        {
            return null;
        }

        try
        {
            return calendar switch
            {
                CalendarSystem.SolarHijri when year < 9377 =>
                    (SolarHijri.ToDateTime(year, 1, 1, 0, 0, 0, 0).Year, SolarHijri.ToDateTime(year + 1, 1, 1, 0, 0, 0, 0).AddDays(-1).Year),
                CalendarSystem.LunarHijri when year < LunarHijri.MaxYear =>
                    (LunarHijri.ToDate(year, 1, 1).Year, LunarHijri.ToDate(year + 1, 1, 1).AddDays(-1).Year),
                _ => null,
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
