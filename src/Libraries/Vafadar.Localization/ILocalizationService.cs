using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// Owns the current UI language, calendar, region and first day of the week, persists the user's choice and applies it to the running app.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Gets the languages the app can be displayed in.</summary>
    IReadOnlyList<AppLanguage> SupportedLanguages { get; }

    /// <summary>Gets the current UI language.</summary>
    AppLanguage CurrentLanguage { get; }

    /// <summary>Gets the calendar used to display dates.</summary>
    CalendarSystem CurrentCalendar { get; }

    /// <summary>Gets the culture used for formatting (current language combined with the current calendar).</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>Gets a value indicating whether the current language is right-to-left.</summary>
    bool IsRightToLeft { get; }

    /// <summary>Gets the chosen region (ISO 3166-1 alpha-2), or <see langword="null"/> when none was chosen.</summary>
    string? CurrentRegion { get; }

    /// <summary>Gets the device's region, offered as a suggestion; never applied without the user's choice.</summary>
    string? SuggestedRegion { get; }

    /// <summary>Gets the first day of the week used by calendars and week-based displays.</summary>
    DayOfWeek FirstDayOfWeek { get; }

    /// <summary>Gets a value indicating whether the first day of the week follows the region or language.</summary>
    bool IsFirstDayOfWeekAutomatic { get; }

    /// <summary>Raised after the language, calendar, region or first day of the week changed.</summary>
    event EventHandler? Changed;

    /// <summary>
    /// Applies the saved language and calendar, or the device language when nothing was saved yet.
    /// Call once at startup, before the first page is created.
    /// </summary>
    void Initialize();

    /// <summary>Switches the UI language and saves the choice.</summary>
    void SetLanguage(AppLanguage language);

    /// <summary>Switches the calendar and saves the choice.</summary>
    void SetCalendar(CalendarSystem calendar);

    /// <summary>Sets or clears the region and saves the choice (PR-05).</summary>
    void SetRegion(string? region);

    /// <summary>Sets the first day of the week, or <see langword="null"/> to follow the region or language.</summary>
    void SetFirstDayOfWeek(DayOfWeek? day);
}
