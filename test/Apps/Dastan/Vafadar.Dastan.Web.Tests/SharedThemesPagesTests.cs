using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A site with two collections that share themes: test data, written onto the sample and onto a copy of it
/// told under another name.
/// <list type="bullet">
/// <item>The story has "Combat" from the start and "Lineage" from its third stage.</item>
/// <item>The retelling has "Fighting" – said to be the same theme as "Combat" by an exact identifier –, "Descent"
/// – the same theme as "Lineage" –, and a theme that is also called "Combat" and carries the identifier only as
/// close: a name and a close match join nothing.</item>
/// </list>
/// </summary>
public class SharedThemesSite : DatedSite
{
    /// <summary>The name of the second collection in addresses.</summary>
    public const string Retold = "story-retold";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var first = !File.Exists(path);
        base.ConfigureWebHost(builder);
        if (!first)
        {
            return;
        }

        static JsonObject Theme(string id, string label, string labelFa, int level, string identifier, string match) => new()
        {
            ["id"] = id, ["type"] = "Theme", ["label"] = label, ["labelFa"] = labelFa, ["summary"] = "Test data.", ["spoilerLevel"] = level,
            ["identifiers"] = JsonNode.Parse($$"""[ { "issuer": "made-up-themes", "value": "{{identifier}}", "match": "{{match}}" } ]"""),
        };
        static void Under(JsonObject sample, string item, params string[] themes) =>
            sample["media"]!.AsArray().Select(one => one!.AsObject()).Single(one => (string?)one["id"] == item)["themeIds"] = new JsonArray([.. themes.Select(theme => (JsonNode)theme)]);

        var written = File.ReadAllText(path);

        var story = JsonNode.Parse(written)!.AsObject();
        story["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", "نبرد", 0, "T-1", "exact"));
        story["entities"]!.AsArray().Add(Theme("theme-lineage", "Lineage", "تبار", 2, "T-2", "exact"));
        Under(story, "media-met-452419", "theme-combat");
        Under(story, "media-dastan-family-illustration", "theme-combat", "theme-lineage");
        Under(story, "media-dastan-parentage-video", "theme-lineage");
        File.WriteAllText(path, story.ToJsonString());

        var retold = JsonNode.Parse(written
            .Replace("story-rostam-sohrab", Retold, StringComparison.Ordinal)
            .Replace("dastan-shahnameh-rostam-sohrab-pilot-v0.5", "dastan-shahnameh-retold", StringComparison.Ordinal))!.AsObject();
        retold["entities"]!.AsArray().Add(Theme("theme-fighting", "Fighting", "جنگ", 0, "T-1", "exact"));
        retold["entities"]!.AsArray().Add(Theme("theme-descent", "Descent", "نسب", 0, "T-2", "exact"));
        retold["entities"]!.AsArray().Add(Theme("theme-combat", "Combat", "نبرد", 0, "T-1", "close"));
        Under(retold, "media-met-452419", "theme-fighting", "theme-descent", "theme-combat");
        File.WriteAllText(Path.Combine(Content, "samples", "retold.json"), retold.ToJsonString());
    }
}

/// <summary>
/// A theme across collections on the pages (D-083, delivery plan 25.4): the themes of the site with the shared
/// ones once, the page of a theme with every collection that has it under its own name, nothing of a stage a
/// reader has not reached, and nothing joined by a name.
/// </summary>
public sealed class SharedThemesPagesTests(SharedThemesSite site) : IClassFixture<SharedThemesSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Retold = SharedThemesSite.Retold;

    private static string Html(Page page) => WebUtility.HtmlDecode(page.Html);

    private async Task<Browser> Reader(int stage)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(stage);
        return visitor;
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;

    [Fact]
    public async Task The_themes_of_the_site_name_a_shared_theme_once_and_join_nothing_by_a_name()
    {
        var html = Html(await (await Reader(4)).Get("/en/themes"));

        // Combat and Fighting are one theme, Lineage and Descent are one; the other "Combat" stands alone.
        Assert.Equal(3, Count(html, "class=\"card theme-card\""));
        Assert.Contains($"href=\"themes/{Story}/theme-combat\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"themes/{Story}/theme-lineage\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"themes/{Retold}/theme-combat\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"themes/{Retold}/theme-fighting", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"themes/{Retold}/theme-descent", html, StringComparison.Ordinal);

        // What stands under a shared theme is counted over its collections: two in the story, one in the retelling.
        Assert.Contains("3 to see", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_theme_of_a_stage_that_was_not_read_to_is_neither_named_nor_counted()
    {
        var visitor = await Reader(0);
        var html = Html(await visitor.Get("/en/themes"));

        // The story's "Lineage" belongs to its third stage: the retelling's "Descent" stands alone, under its own address.
        Assert.DoesNotContain("theme-lineage", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Lineage", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"themes/{Retold}/theme-descent\"", html, StringComparison.Ordinal);

        // Asked for by its address, it is a page that does not exist – as a theme nobody wrote is.
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/themes/{Story}/theme-lineage")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/themes/{Story}/theme-nobody-wrote")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/themes/no-such-collection/theme-combat")).Status);

        // The page of "Descent" names the retelling alone.
        var descent = Html(await visitor.Get($"/en/themes/{Retold}/theme-descent"));
        Assert.Contains($"id=\"theme-in-{Retold}\"", descent, StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"theme-in-{Story}\"", descent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Theme “Combat”", "Several collections have this theme.")]
    [InlineData("fa", "موضوع «نبرد»", "چند مجموعه این موضوع را دارند.")]
    public async Task The_page_of_a_shared_theme_shows_every_collection_under_its_own_name(string language, string title, string lead)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4, language);
        var html = Html(await visitor.Get($"/{language}/themes/{Story}/theme-combat"));

        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains(lead, html, StringComparison.Ordinal);
        Assert.Contains($"id=\"theme-in-{Story}\"", html, StringComparison.Ordinal);
        Assert.Contains($"id=\"theme-in-{Retold}\"", html, StringComparison.Ordinal);

        // The pictures of each collection lead to that collection, and each keeps the name it gives the theme.
        Assert.Contains($"href=\"{Story}/entity/theme-combat/gallery#media-met-452419\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/media/media-dastan-family-illustration?entity=theme-combat\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Retold}/entity/theme-fighting/gallery#media-met-452419\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Retold}/entity/theme-fighting/gallery\"", html, StringComparison.Ordinal);

        // The same page under the address of the other collection.
        var other = Html(await visitor.Get($"/{language}/themes/{Retold}/theme-fighting"));
        Assert.Contains($"id=\"theme-in-{Story}\"", other, StringComparison.Ordinal);
        Assert.Contains($"id=\"theme-in-{Retold}\"", other, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_theme_that_only_one_collection_has_is_a_page_of_that_collection_alone()
    {
        var html = Html(await (await Reader(4)).Get($"/en/themes/{Retold}/theme-combat"));

        Assert.Contains("Images and videos the editors gathered under this theme", html, StringComparison.Ordinal);
        Assert.Contains($"id=\"theme-in-{Retold}\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"theme-in-{Story}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_gallery_of_a_theme_leads_to_the_other_collections_that_have_it()
    {
        var visitor = await Reader(4);

        var shared = Html(await visitor.Get($"/en/{Story}/entity/theme-combat/gallery"));
        Assert.Contains("This theme also in", shared, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Retold}/entity/theme-fighting/gallery\"", shared, StringComparison.Ordinal);
        Assert.Contains($"href=\"themes/{Story}/theme-combat\"", shared, StringComparison.Ordinal);

        // A theme of this collection alone, and the gallery of the collection, lead to the themes of the site.
        var whole = Html(await visitor.Get($"/en/{Story}/gallery"));
        Assert.DoesNotContain("This theme also in", whole, StringComparison.Ordinal);
        Assert.Contains("href=\"themes\"", whole, StringComparison.Ordinal);

        // A reader who has not reached the theme of the other collection is told nothing of it.
        var early = Html(await (await Reader(0)).Get($"/en/{Retold}/entity/theme-descent/gallery"));
        Assert.DoesNotContain("This theme also in", early, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_search_of_the_site_finds_a_shared_theme_once()
    {
        var html = Html(await (await Reader(4)).Get("/en/search?q=Fighting"));

        Assert.Contains("id=\"found-themes\"", html, StringComparison.Ordinal);
        // It is named as the collection names it in which the words were found, and stands there once.
        Assert.Contains($"href=\"themes/{Retold}/theme-fighting\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"themes/{Story}/theme-combat\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain($"{Retold}/entity/theme-fighting\"", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "class=\"card\""));
    }

    [Fact]
    public void No_collection_can_take_the_address_of_the_themes_of_the_site()
    {
        // A collection about something called "themes" could not be reached: its address is the page of the themes.
        Assert.True(DastanLibrary.IsTaken("themes"));
    }
}
