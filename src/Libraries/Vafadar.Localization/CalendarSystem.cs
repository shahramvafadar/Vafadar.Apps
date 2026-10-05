namespace Vafadar.Localization;

/// <summary>
/// The calendar used to display dates. It is chosen independently of the UI language.
/// </summary>
/// <remarks>Dates are always stored as Gregorian values; the calendar only affects how they are shown and entered.</remarks>
public enum CalendarSystem
{
    /// <summary>The Gregorian calendar.</summary>
    Gregorian = 0,

    /// <summary>The Persian (Solar Hijri) calendar.</summary>
    Persian = 1,

    /// <summary>The lunar Hijri (Islamic) calendar: Umm al-Qura, see <see cref="Vafadar.Core.Dates.LunarHijri"/>.</summary>
    Hijri = 2,
}
