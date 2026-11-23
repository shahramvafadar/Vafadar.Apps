using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The same theme in another collection, as the tool of the editors is told it (ADR 0027, delivery plan 25.4):
/// by the same exact identifier of the same catalogue, and by nothing else. The records are test data.
/// </summary>
public sealed class SharedThemesStoreTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private const string Other = "dastan-shahnameh-retold";

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Theme(string id, string label, string value, string match) => new()
    {
        ["id"] = id, ["type"] = "Theme", ["label"] = label, ["summary"] = "Test data.", ["spoilerLevel"] = 0,
        ["identifiers"] = JsonNode.Parse($$"""[ { "issuer": "made-up-themes", "value": "{{value}}", "match": "{{match}}" } ]"""),
    };

    [Fact]
    public void A_theme_names_the_themes_of_other_collections_that_carry_its_identifier_as_exact()
    {
        var store = _copy.Filled();
        var retold = Path.Combine(_copy.Samples, "retold.json");
        File.WriteAllText(retold, File.ReadAllText(_copy.Sample)
            .Replace("story-rostam-sohrab", "story-retold", StringComparison.Ordinal)
            .Replace(Collection, Other, StringComparison.Ordinal));
        Assert.Empty(store.Import(retold, "an editor", "A second collection.").Problems);

        Assert.True(store.Put(Collection, "entities", Theme("theme-combat", "Combat", "T-1", "exact"), "a writer", "Test data.").Done);
        Assert.True(store.Put(Collection, "entities", Theme("theme-alone", "Alone", "T-9", "exact"), "a writer", "Test data.").Done);
        Assert.True(store.Put(Other, "entities", Theme("theme-fighting", "Fighting", "T-1", "exact"), "a writer", "Test data.").Done);
        Assert.True(store.Put(Other, "entities", Theme("theme-combat", "Combat", "T-1", "close"), "a writer", "Test data.").Done);

        Assert.Equal([(Other, "theme-fighting", "Fighting")], store.SameThemeElsewhere(Collection, "theme-combat"));
        Assert.Equal([(Collection, "theme-combat", "Combat")], store.SameThemeElsewhere(Other, "theme-fighting"));

        // A close match, a theme nobody else has, a record that is no theme and one that is not there: none.
        Assert.Empty(store.SameThemeElsewhere(Other, "theme-combat"));
        Assert.Empty(store.SameThemeElsewhere(Collection, "theme-alone"));
        Assert.Empty(store.SameThemeElsewhere(Collection, "character-rostam"));
        Assert.Empty(store.SameThemeElsewhere(Collection, "theme-nobody-wrote"));
    }
}
