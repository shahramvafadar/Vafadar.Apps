using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the dated records with two themes: test data, written onto the sample. "Combat" is there from
/// the start and rests on no source; "Lineage" belongs to the third stage and names a source. The illustration
/// of the site stands under both, its video under "Lineage", the paintings of the museums under "Combat".
/// </summary>
public class ThemedSite : DatedSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var first = !File.Exists(path);
        base.ConfigureWebHost(builder);
        if (!first)
        {
            return;
        }

        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var entities = sample["entities"]!.AsArray();
        entities.Add(new JsonObject
        {
            ["id"] = "theme-combat", ["type"] = "Theme", ["label"] = "Combat", ["labelFa"] = "نبرد",
            ["summary"] = "Pictures of fighting.", ["spoilerLevel"] = 0,
        });
        entities.Add(new JsonObject
        {
            ["id"] = "theme-lineage", ["type"] = "Theme", ["label"] = "Lineage", ["labelFa"] = "تبار",
            ["summary"] = "Pictures of who descends from whom.", ["spoilerLevel"] = 2, ["sourceIds"] = new JsonArray("src-ganjoor-s02"),
        });
        JsonObject Item(string id) => sample["media"]!.AsArray().Select(item => item!.AsObject()).Single(item => (string?)item["id"] == id);
        Item("media-met-452419")["themeIds"] = new JsonArray("theme-combat");
        Item("media-met-453265")["themeIds"] = new JsonArray("theme-combat");
        Item("media-dastan-parentage-video")["themeIds"] = new JsonArray("theme-lineage");
        Item("media-dastan-family-illustration")["themeIds"] = new JsonArray("theme-lineage", "theme-combat");
        File.WriteAllText(path, sample.ToJsonString());
    }
}

/// <summary>
/// The themes of a gallery on the pages (R-MED-02): the gallery of a collection offers its themes, a theme has
/// a gallery of its own that says what gathers its pictures, a picture names its themes, and a reader is offered
/// nothing of a theme that was not read to.
/// </summary>
public sealed class GalleryThemesTests(ThemedSite site) : IClassFixture<ThemedSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Illustration = "media-dastan-family-illustration";
    private const string Video = "media-dastan-parentage-video";

    // The page as a browser shows it: the names of the themes and the quotation marks around them are written as characters.
    private static string Html(Page page) => WebUtility.HtmlDecode(page.Html);

    private async Task<Browser> Reader(int stage)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(stage);
        return visitor;
    }

    [Fact]
    public async Task The_gallery_of_a_collection_offers_its_themes_with_what_stands_under_each()
    {
        var page = await (await Reader(4)).Get($"/en/{Story}/gallery");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("aria-label=\"Themes\"", Html(page), StringComparison.Ordinal);
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/gallery\" aria-current=\"true\">everything</a>", Html(page), StringComparison.Ordinal);
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/entity/theme-combat/gallery\">Combat (3)</a>", Html(page), StringComparison.Ordinal);
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/entity/theme-lineage/gallery\">Lineage (2)</a>", Html(page), StringComparison.Ordinal);

        // The themes stand above the pictures, outside the fold: they are a way through the gallery.
        Assert.True(Html(page).IndexOf("aria-label=\"Themes\"", StringComparison.Ordinal) < Html(page).IndexOf("class=\"fold gallery-about\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_gallery_of_a_theme_says_what_gathers_its_pictures_and_shows_them()
    {
        var visitor = await Reader(4);
        var page = await visitor.Get($"/en/{Story}/entity/theme-lineage/gallery");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Theme “Lineage” – images and videos</h1>", Html(page), StringComparison.Ordinal);
        Assert.Contains("gathered under the theme “Lineage”", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("that show Lineage", page.Main, StringComparison.Ordinal);
        Assert.Contains($"id=\"{Illustration}\"", Html(page), StringComparison.Ordinal);
        Assert.Contains($"id=\"{Video}\"", Html(page), StringComparison.Ordinal);

        // The theme that is shown says so; the other one and the whole gallery are a step away.
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/entity/theme-lineage/gallery\" aria-current=\"true\">Lineage (2)</a>", Html(page), StringComparison.Ordinal);
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/entity/theme-combat/gallery\">Combat (3)</a>", Html(page), StringComparison.Ordinal);
        Assert.Contains($"<a class=\"choice\" href=\"{Story}/gallery\">everything</a>", Html(page), StringComparison.Ordinal);

        // The other theme holds the illustration and names the paintings the museums hold.
        var combat = await visitor.Get($"/en/{Story}/entity/theme-combat/gallery");
        Assert.Contains($"id=\"{Illustration}\"", Html(combat), StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"{Video}\"", Html(combat), StringComparison.Ordinal);
        Assert.Contains("metmuseum.org", Html(combat), StringComparison.Ordinal);

        // Arranged like every gallery: videos only leaves the video.
        var videos = await visitor.Get($"/en/{Story}/entity/theme-lineage/gallery?kind=video");
        Assert.Contains($"id=\"{Video}\"", Html(videos), StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"{Illustration}\"", Html(videos), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_picture_names_its_themes_and_leads_back_to_the_theme_it_was_opened_from()
    {
        var visitor = await Reader(4);
        var page = await visitor.Get($"/en/{Story}/media/{Illustration}?entity=theme-lineage");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h2 id=\"themes\">Themes</h2>", Html(page), StringComparison.Ordinal);
        var themes = Html(page)[Html(page).IndexOf("id=\"themes\"", StringComparison.Ordinal)..];
        themes = themes[..themes.IndexOf("</section>", StringComparison.Ordinal)];
        Assert.Matches($"<a href=\"{Story}/entity/theme-lineage/gallery\">(<[^>]+>)*Lineage", themes);
        Assert.Matches($"<a href=\"{Story}/entity/theme-combat/gallery\">(<[^>]+>)*Combat", themes);
        Assert.True(themes.IndexOf("Lineage", StringComparison.Ordinal) < themes.IndexOf("Combat", StringComparison.Ordinal));
        Assert.Contains("Theme “Lineage” – images and videos", page.Main, StringComparison.Ordinal);

        // A theme is not among who and what the picture shows.
        var shows = Html(page)[Html(page).IndexOf("id=\"shows\"", StringComparison.Ordinal)..Html(page).IndexOf("id=\"themes\"", StringComparison.Ordinal)];
        Assert.DoesNotContain("theme-", shows, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_a_theme_says_what_a_theme_is_and_leads_to_its_pictures()
    {
        var visitor = await Reader(4);
        var page = await visitor.Get($"/en/{Story}/entity/theme-combat");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Theme", page.Main, StringComparison.Ordinal);
        Assert.Contains("A theme is a grouping by the editors of this collection", page.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/entity/theme-combat/gallery", Html(page), StringComparison.Ordinal);

        // A theme that names a source leads to it like everything else.
        var sourced = await visitor.Get($"/en/{Story}/entity/theme-lineage");
        Assert.DoesNotContain("A theme is a grouping by the editors", sourced.Main, StringComparison.Ordinal);
        Assert.Contains("evidence", Html(sourced), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_reader_is_offered_no_theme_that_was_not_read_to()
    {
        var early = await Reader(1);
        var whole = await early.Get($"/en/{Story}/gallery");

        Assert.Contains("Combat (1)", Html(whole), StringComparison.Ordinal);
        Assert.DoesNotContain("Lineage", Html(whole), StringComparison.Ordinal);
        Assert.DoesNotContain("theme-lineage", Html(whole), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await early.Get($"/en/{Story}/entity/theme-lineage/gallery")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await early.Get($"/en/{Story}/entity/theme-lineage")).Status);
    }

    [Fact]
    public async Task The_themes_are_named_in_Persian_on_a_Persian_page()
    {
        var page = await (await Reader(4)).Get($"/fa/{Story}/gallery");

        Assert.Contains("aria-label=\"مضمون‌ها\"", Html(page), StringComparison.Ordinal);
        Assert.Contains("نبرد (۳)", Html(page), StringComparison.Ordinal);
        Assert.Contains("تبار (۲)", Html(page), StringComparison.Ordinal);

        var theme = await (await Reader(4)).Get($"/fa/{Story}/entity/theme-lineage/gallery");
        Assert.Contains("مضمون «تبار» – تصویرها و ویدیوها", theme.Main, StringComparison.Ordinal);
    }
}
