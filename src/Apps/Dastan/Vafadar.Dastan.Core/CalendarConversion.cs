using System.Globalization;
using Vafadar.Core.Dates;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// A date of one calendar as a date of another (ADR 0021, R-TIME-05): Gregorian, Solar Hijri and Lunar Hijri.
/// The date of a record is never changed: it is shown as it was written, and a converted date may stand beside
/// it and is said to be converted. A date is converted only when it is exact enough for that – stated to the
/// day, in a calendar it names. A year or a month of one calendar is no year or month of another, and a date
/// whose calendar the source does not say is not given one here.
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
}
