using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Portable digit formatting, used by the native preference adapter and independent application flows.</summary>
internal static partial class DigitPreferences
{
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
