using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// Chooses the resource keys of weekday phrases ("on the first Monday", "the last Sunday") so that their words agree
/// with the grammatical gender of the weekday.
/// </summary>
/// <remarks>
/// Italian "domenica" is feminine while the other six weekdays are masculine: "il primo lunedì" but "la prima
/// domenica", "l’ultimo venerdì" but "l’ultima domenica". A Sunday in Italian therefore uses the <c>_Feminine</c>
/// keys (<c>Ordinal_Feminine_{n}</c>, <c>Rule_NthWeekday_Feminine</c>, <c>Rule_LastWeekday_Feminine</c>,
/// <c>DayRule_LastWeekday_Feminine</c>); every other language keeps the plain keys, and its <c>_Feminine</c> keys hold
/// the same texts only so that every language has the same keys. The choice depends on the culture and the
/// <see cref="DayOfWeek"/> value, never on the spelling of the weekday name (D-57). <c>DayRule_NthWeekday</c> has no
/// article in Italian and takes the feminine ordinal as it is, so it has no feminine variant.
/// </remarks>
public static class WeekdayGrammar
{
    /// <summary>Whether a phrase about <paramref name="day"/> takes feminine words in the language of <paramref name="culture"/>.</summary>
    public static bool IsFeminine(CultureInfo culture, DayOfWeek day)
    {
        ArgumentNullException.ThrowIfNull(culture);
        // it, it-IT, it-CH … all share the neutral Italian texts.
        return day == DayOfWeek.Sunday && culture.TwoLetterISOLanguageName == AppLanguages.Italian.CultureName;
    }

    /// <summary>The key of the ordinal for the <paramref name="week"/>-th weekday of a month, e.g. <c>Ordinal_2</c> or <c>Ordinal_Feminine_2</c>.</summary>
    public static string OrdinalKey(int week, CultureInfo culture, DayOfWeek day) =>
        IsFeminine(culture, day) ? $"Ordinal_Feminine_{week}" : $"Ordinal_{week}";

    /// <summary>
    /// The key of a weekday template (<c>Rule_NthWeekday</c>, <c>Rule_LastWeekday</c> or <c>DayRule_LastWeekday</c>),
    /// with <c>_Feminine</c> appended where the weekday needs it.
    /// </summary>
    public static string TemplateKey(string key, CultureInfo culture, DayOfWeek day)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return IsFeminine(culture, day) ? key + "_Feminine" : key;
    }
}
