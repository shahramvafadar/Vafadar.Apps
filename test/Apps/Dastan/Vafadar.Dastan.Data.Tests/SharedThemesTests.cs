using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// A theme across collections (D-083, delivery plan 25.4): two themes of two collections are one theme when the
/// editors say so with the same exact identifier of the same catalogue, and by nothing else; a collection has a
/// theme once; a reader is told of a theme only as far as its collection was read. The records are test data.
/// </summary>
public sealed class SharedThemesTests
{
    private static JsonObject Theme(string id, string label, int level, params (string Value, string Match)[] identifiers) => new()
    {
        ["id"] = id, ["type"] = "Theme", ["label"] = label, ["summary"] = "Test data.", ["spoilerLevel"] = level,
        ["identifiers"] = new JsonArray([.. identifiers.Select(one => (JsonNode)new JsonObject { ["issuer"] = "made-up-themes", ["value"] = one.Value, ["match"] = one.Match })]),
    };

    private static void Under(JsonObject sample, string item, params string[] themes) =>
        sample.Record("media", item)["themeIds"] = new JsonArray([.. themes.Select(theme => (JsonNode)theme)]);

    private static Projection Read(int level, Action<JsonObject> change) =>
        Projection.Of(SampleFiles.Read(change), new ReadingLevel(level), Audience.LocalDemonstration)!;

    private static Projection Story(int level) => Read(level, sample =>
    {
        sample["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", 0, ("T-1", "exact")));
        sample["entities"]!.AsArray().Add(Theme("theme-lineage", "Lineage", 2, ("T-2", "exact"), ("T-3", "exact")));
        Under(sample, "media-met-452419", "theme-combat");
        Under(sample, "media-dastan-family-illustration", "theme-combat", "theme-lineage");
    });

    private static Projection Other(int level) => Read(level, sample =>
    {
        sample["entities"]!.AsArray().Add(Theme("theme-fighting", "Fighting", 0, (" T-1 ", "exact")));
        sample["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", 0, ("T-1", "close")));
        sample["entities"]!.AsArray().Add(Theme("theme-unnamed", "Unnamed", 0));
        Under(sample, "media-met-452419", "theme-fighting", "theme-combat", "theme-unnamed");
    });

    private static Projection Third(int level) => Read(level, sample =>
    {
        sample["entities"]!.AsArray().Add(Theme("theme-descent", "Descent", 0, ("T-3", "exact")));
        Under(sample, "media-met-452419", "theme-descent");
    });

    private static string[] Names(SharedTheme theme) => [.. theme.In.Select(one => $"{one.Collection}:{one.Theme.Id}")];

    [Fact]
    public void Themes_with_the_same_exact_identifier_are_one_theme_and_a_name_or_a_close_match_joins_nothing()
    {
        var themes = SharedThemes.Of([("story", Story(4)), ("other", Other(4))]);

        Assert.Equal(4, themes.Length);
        Assert.Equal(["story:theme-combat", "other:theme-fighting"], Names(themes[0]));
        Assert.True(themes[0].IsShared);
        Assert.Equal(3, themes[0].Items);
        Assert.Equal(["story:theme-lineage"], Names(themes[1]));
        Assert.Equal(["other:theme-combat"], Names(themes[2]));
        Assert.Equal(["other:theme-unnamed"], Names(themes[3]));
        Assert.False(themes[2].IsShared);
    }

    [Fact]
    public void A_theme_is_found_by_any_collection_that_has_it_and_named_as_the_collection_a_visitor_came_from_names_it()
    {
        var themes = SharedThemes.Of([("story", Story(4)), ("other", Other(4))]);

        var shared = themes.Find("other", new EntityId("theme-fighting"))!;
        Assert.Same(themes.Find("story", new EntityId("theme-combat")), shared);
        Assert.Equal("theme-fighting", shared.From("other").Theme.Id.Value);
        Assert.Equal("theme-combat", shared.From(null).Theme.Id.Value);
        Assert.Equal("theme-combat", shared.From("no-such").Theme.Id.Value);
        Assert.Null(themes.Find("story", new EntityId("theme-fighting")));
        Assert.Null(themes.Find("other", new EntityId("theme-nobody-wrote")));
    }

    [Fact]
    public void A_theme_of_a_later_stage_is_neither_named_nor_counted_nor_does_it_join()
    {
        var early = SharedThemes.Of([("story", Story(0)), ("third", Third(0))]);
        Assert.Equal(["story:theme-combat"], Names(early[0]));
        Assert.Equal(["third:theme-descent"], Names(early[1]));
        Assert.Equal(2, early.Length);
        Assert.Null(early.Find("story", new EntityId("theme-lineage")));

        var later = SharedThemes.Of([("story", Story(4)), ("third", Third(0))]);
        Assert.Equal(["story:theme-lineage", "third:theme-descent"], Names(later.Find("third", new EntityId("theme-descent"))!));
    }

    [Fact]
    public void A_theme_that_names_two_identifiers_brings_together_what_each_names()
    {
        // Lineage carries T-2 and T-3; one collection has T-3, another T-2: all three are one theme.
        var fourth = Read(4, sample =>
        {
            sample["entities"]!.AsArray().Add(Theme("theme-kin", "Kin", 0, ("T-2", "exact")));
            Under(sample, "media-met-452419", "theme-kin");
        });

        var themes = SharedThemes.Of([("third", Third(4)), ("fourth", fourth), ("story", Story(4))]);

        Assert.Equal(["third:theme-descent", "fourth:theme-kin", "story:theme-lineage"], Names(themes.Find("story", new EntityId("theme-lineage"))!));
        Assert.Equal(2, themes.Length);
    }

    [Fact]
    public void A_site_without_themes_has_none()
    {
        Assert.Empty(SharedThemes.Of([("story", Read(4, _ => { }))]));
        Assert.Empty(SharedThemes.Of([]));
    }

    [Fact]
    public void A_collection_has_a_theme_once()
    {
        var refused = Assert.Throws<SampleFormatException>(() => SampleFiles.Read(sample =>
        {
            sample["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", 0, ("T-1", "exact")));
            sample["entities"]!.AsArray().Add(Theme("theme-fighting", "Fighting", 0, ("T-1", "exact")));
        }));

        Assert.Contains("theme-combat and theme-fighting: both themes carry the identifier", string.Join('\n', refused.Problems), StringComparison.Ordinal);

        // A close match says nothing of the kind.
        SampleFiles.Read(sample =>
        {
            sample["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", 0, ("T-1", "exact")));
            sample["entities"]!.AsArray().Add(Theme("theme-fighting", "Fighting", 0, ("T-1", "close")));
        });
    }
}
