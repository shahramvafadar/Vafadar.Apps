using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The user's choice of Persian digits (۱۲۳) in the Persian interface, a local preference (D-27). On by default; the
/// other languages always use Latin digits. Stored values, exports and input are not affected.
/// </summary>
internal static class DigitPreferences
{
    private const string Key = "ui.persian_digits";

    /// <summary>Raised after the choice changed, so the screens are rebuilt.</summary>
    public static event EventHandler? Changed;

    /// <summary>Gets a value indicating whether the Persian interface shows Persian digits.</summary>
    public static bool PersianDigits => Preferences.Default.Get(Key, true);

    /// <summary>Saves the choice and applies it.</summary>
    public static void Set(bool persianDigits, ILocalizationService localization)
    {
        Preferences.Default.Set(Key, persianDigits);
        Apply(localization);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Applies the choice to the current language.</summary>
    public static void Apply(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        NativeDigits.IsEnabled = PersianDigits && IsPersian(localization);
    }

    /// <summary>Returns whether the interface language is Persian.</summary>
    public static bool IsPersian(ILocalizationService localization) =>
        localization.CurrentCulture.TwoLetterISOLanguageName == "fa";
}
