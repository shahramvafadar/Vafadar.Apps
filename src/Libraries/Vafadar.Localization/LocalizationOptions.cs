using System.Resources;

namespace Vafadar.Localization;

/// <summary>
/// Configures localization for an app.
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>Gets the languages the app offers. Defaults to <see cref="AppLanguages.All"/>.</summary>
    public IList<AppLanguage> SupportedLanguages { get; } = [.. AppLanguages.All];

    /// <summary>Gets or sets the language used when neither a saved choice nor the device language is supported.</summary>
    public AppLanguage DefaultLanguage { get; set; } = AppLanguages.English;

    /// <summary>
    /// Gets or sets the calendar used for a language until the user explicitly picks one.
    /// Defaults to Persian for Persian and Gregorian for everything else.
    /// </summary>
    public Func<AppLanguage, CalendarSystem> DefaultCalendar { get; set; } =
        language => language == AppLanguages.Persian ? CalendarSystem.Persian : CalendarSystem.Gregorian;

    /// <summary>
    /// Gets the app's string resources. They are searched in order, before the shared strings of this library,
    /// so an app can override any shared string.
    /// </summary>
    public IList<ResourceManager> Resources { get; } = [];
}
