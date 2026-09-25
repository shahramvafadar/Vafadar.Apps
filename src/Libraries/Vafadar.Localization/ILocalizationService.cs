using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// Owns the current UI language and calendar, persists the user's choice and applies it to the running app.
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

    /// <summary>Raised after the language or calendar changed.</summary>
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
}
