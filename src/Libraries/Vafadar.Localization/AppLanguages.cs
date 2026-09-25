namespace Vafadar.Localization;

/// <summary>
/// The languages supported across Vafadar apps.
/// </summary>
/// <remarks>
/// Adding a language means: add it here and to <see cref="All"/>, add <c>*.{culture}.resx</c> files to every app and
/// library that has resources, and add the culture to <c>SatelliteResourceLanguages</c> in Directory.Build.props.
/// </remarks>
public static class AppLanguages
{
    /// <summary>English (the neutral language of all resources).</summary>
    public static AppLanguage English { get; } = new("en", "English", "English");

    /// <summary>Persian (Farsi), right-to-left.</summary>
    public static AppLanguage Persian { get; } = new("fa", "فارسی", "Persian");

    /// <summary>German.</summary>
    public static AppLanguage German { get; } = new("de", "Deutsch", "German");

    /// <summary>Gets all supported languages in display order.</summary>
    public static IReadOnlyList<AppLanguage> All { get; } = [English, Persian, German];
}
