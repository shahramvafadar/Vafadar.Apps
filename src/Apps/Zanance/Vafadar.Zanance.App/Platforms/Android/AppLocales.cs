using Android.App;
using Android.Content;
using Android.OS;
using Vafadar.Localization;

namespace Vafadar.Zanance.App;

/// <summary>
/// Keeps the app language and the per-app language setting of Android 13+ in step (D-32): a language chosen in the
/// system settings is used by the app, and a language chosen in the app appears in the system settings (and in the
/// widget labels). Older Android versions only have the setting in the app.
/// </summary>
internal static class AppLocales
{
    private static LocaleManager? Manager =>
        OperatingSystem.IsAndroidVersionAtLeast(33) ? Android.App.Application.Context.GetSystemService(Context.LocaleService) as LocaleManager : null;

    /// <summary>Applies a choice made in the system settings, then follows every change made in the app.</summary>
    public static void Initialize(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return;
        }

        FromSystem(localization);
        ToSystem(localization);
        localization.Changed += (_, _) => ToSystem(localization);
    }

    /// <summary>Uses the language chosen for the app in the system settings, if any.</summary>
    public static void FromSystem(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        if (!OperatingSystem.IsAndroidVersionAtLeast(33) || Manager is not { } manager || manager.ApplicationLocales is not { IsEmpty: false } locales)
        {
            return;
        }

        var tag = locales.Get(0)?.Language;
        if (localization.SupportedLanguages.FirstOrDefault(l => l.CultureName == tag) is { } language
            && language.CultureName != localization.CurrentLanguage.CultureName)
        {
            localization.SetLanguage(language);
        }
    }

    private static void ToSystem(ILocalizationService localization)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33) || Manager is not { } manager)
        {
            return;
        }

        var wanted = localization.CurrentLanguage.CultureName;
        if (manager.ApplicationLocales is { IsEmpty: false } current && current.Get(0)?.Language == wanted)
        {
            return;
        }

        manager.ApplicationLocales = LocaleList.ForLanguageTags(wanted);
    }
}
