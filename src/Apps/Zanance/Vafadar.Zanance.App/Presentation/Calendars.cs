using Vafadar.Localization;
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The one translation between the display calendar (<see cref="CalendarSystem"/>, chosen in the settings) and the calendar
/// of periods, plans and imports (<see cref="PeriodCalendar"/>). Screens must not compare with a single calendar
/// themselves: a third calendar (lunar Hijri) would silently become Gregorian.
/// </summary>
internal static class Calendars
{
    /// <summary>Gets the calendars in the order of every calendar choice; the index is the value of <see cref="PeriodCalendar"/>.</summary>
    public static IReadOnlyList<PeriodCalendar> All { get; } = [PeriodCalendar.Gregorian, PeriodCalendar.Persian, PeriodCalendar.Hijri];

    /// <summary>Returns the period calendar of a display calendar.</summary>
    public static PeriodCalendar ToPeriod(CalendarSystem calendar) => calendar switch
    {
        CalendarSystem.Persian => PeriodCalendar.Persian,
        CalendarSystem.Hijri => PeriodCalendar.Hijri,
        _ => PeriodCalendar.Gregorian,
    };

    /// <summary>Returns the display calendar of a period calendar.</summary>
    public static CalendarSystem ToDisplay(PeriodCalendar calendar) => calendar switch
    {
        PeriodCalendar.Persian => CalendarSystem.Persian,
        PeriodCalendar.Hijri => CalendarSystem.Hijri,
        _ => CalendarSystem.Gregorian,
    };

    /// <summary>Returns the resource key of a calendar's name, e.g. <c>Calendar_Hijri</c>.</summary>
    public static string NameKey(PeriodCalendar calendar) => $"Calendar_{calendar}";

    /// <summary>Returns the names of <see cref="All"/> in the current language.</summary>
    public static IReadOnlyList<string> Names(Translator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);
        return [.. All.Select(c => translator[NameKey(c)])];
    }
}
