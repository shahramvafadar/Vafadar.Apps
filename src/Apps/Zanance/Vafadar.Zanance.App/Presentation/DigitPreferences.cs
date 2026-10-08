using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Applies portable digit-shape choices (D-67), retaining the legacy Persian-interface preference (D-27).
/// Stored values, exports and input are not affected.
/// </summary>
internal static class DigitPreferences
{
    private const string Key = "ui.persian_digits";

    /// <summary>Raised after the choice changed, so the screens are rebuilt.</summary>
    public static event EventHandler? Changed;

    /// <summary>Gets a value indicating whether the Persian interface shows Persian digits.</summary>
    public static bool PersianDigits => Preferences.Default.Get(Key, true);

    /// <summary>Migrates an explicit legacy digit choice once, before application change handlers are registered.</summary>
    public static void Initialize(ILocalizationService localization)
    {
        if (!Preferences.Default.ContainsKey("localization.digits") && Preferences.Default.ContainsKey(Key))
        {
            localization.SetDigits(PersianDigits ? DigitStyle.LanguageDefault : DigitStyle.Latin);
        }

        Apply(localization);
    }

    /// <summary>Saves the choice and applies it.</summary>
    public static void Set(bool persianDigits, ILocalizationService localization)
    {
        Preferences.Default.Set(Key, persianDigits);
        localization.SetDigits(persianDigits ? DigitStyle.Persian : DigitStyle.Latin);
        Apply(localization);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Applies the choice to the current language.</summary>
    public static void Apply(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        NativeDigits.PreserveSeparators = localization.FormattingCultureName is not null || localization.CurrentDigits != DigitStyle.LanguageDefault;
        NativeDigits.IsEnabled = localization.CurrentDigits switch
        {
            DigitStyle.Persian => true,
            DigitStyle.Latin => false,
            _ => IsPersian(localization),
        };
    }

    /// <summary>Returns whether the interface language is Persian.</summary>
    public static bool IsPersian(ILocalizationService localization) =>
        localization.CurrentCulture.TwoLetterISOLanguageName == "fa";
}
