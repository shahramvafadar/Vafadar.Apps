using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>
/// AT-62 in the three languages: the reference results of specification §24.2 are shown exactly, with the separators of
/// each language, and in Persian with Persian digits (D-27).
/// </summary>
[Trait("AT", "AT-62")]
public sealed class GoldenDataDisplayTests
{
    [Theory]
    [InlineData("en", 2_507_63, false, "2,507.63 EUR")]
    [InlineData("en", 2_027_63, true, "+2,027.63 EUR")]
    [InlineData("de", 2_507_63, false, "2.507,63 EUR")]
    [InlineData("de", 97_237, false, "972,37 EUR")]
    [InlineData("fa", 2_507_63, false, "۲٬۵۰۷٫۶۳ EUR")]
    [InlineData("fa", 2_763, false, "۲۷٫۶۳ EUR")]
    [InlineData("fa", 2_027_63, true, "+۲٬۰۲۷٫۶۳ EUR")]
    public void Reference_results_read_the_same_in_every_language(string language, long minor, bool showPlus, string expected)
    {
        // The culture the app shows, from the same service it uses.
        var localization = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), new Translator());
        localization.Initialize();
        localization.SetLanguage(AppLanguages.All.Single(l => l.CultureName == language));
        var culture = localization.CurrentCulture;

        var text = Plain(MoneyText.Format(minor, "EUR", culture, showPlus: showPlus));

        Assert.Equal(expected, language == "fa" ? NativeDigits.ToPersian(text) : text);
    }

    // Without the direction marks and with a normal space, as a reader sees it.
    private static string Plain(string text) => new string([.. text.Where(c => c is not ('\u200E' or '\u2066' or '\u2069'))]).Replace('\u00A0', ' ');
}
