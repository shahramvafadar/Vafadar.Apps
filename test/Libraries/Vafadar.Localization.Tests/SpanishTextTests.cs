using System.Globalization;
using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// Spanish texts that are put together from several resources read as one sentence once the values are inserted
/// (counts, ordinals, weekdays, month names); equal placeholder counts alone do not show that.
/// </summary>
public sealed class SpanishTextTests
{
    private static readonly Lazy<Dictionary<string, string>> App = new(() => Read("src/Apps/Zanance/Vafadar.Zanance.App/Resources/Strings/AppResources.es.resx"));

    [Theory]
    [InlineData("Rule_Weekly", "Cada semana, el viernes", "viernes")]
    [InlineData("Rule_EveryNWeeks", "Cada 2 semanas, el lunes", "lunes", 2)]
    [InlineData("Rule_NthWeekday", "el primer lunes", "primer", "lunes")]
    [InlineData("Rule_NthWeekday", "el tercer viernes", "tercer", "viernes")]
    [InlineData("Rule_LastDayOfNamedMonth", "el último día de febrero", "febrero")]
    [InlineData("Occurrence_OverdueOneDay", "1 día de retraso")]
    [InlineData("Occurrence_OverdueDays", "3 días de retraso", 3)]
    public void Composed_text_reads_as_a_sentence(string key, string expected, params object[] args)
    {
        Assert.Equal(expected, string.Format(CultureInfo.GetCultureInfo("es"), App.Value[key], args));
    }

    [Fact]
    public void Ordinals_before_a_weekday_are_the_short_forms()
    {
        // "el primer lunes", not "el primero lunes".
        Assert.Equal("primer", App.Value["Ordinal_1"]);
        Assert.Equal("tercer", App.Value["Ordinal_3"]);
    }

    private static Dictionary<string, string> Read(string path) =>
        XDocument.Load(RepositoryPaths.Combine(path)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty);
}
