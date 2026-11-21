using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the dated records with pictures that say what time they show: test data, written onto the
/// media of the sample: the illustration shows the year 1020, the video a time between 600 and 500 BCE, and a
/// picture that is only named with the museum that holds it shows the year 1020 too. All were made later.
/// </summary>
public class ShownTimeSite : DatedSite
{
    /// <summary>Gets the identifiers of the media the tests wrote a time on, in the order of the collection.</summary>
    public IReadOnlyList<string> Media { get; private set; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var first = !File.Exists(path);
        base.ConfigureWebHost(builder);
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var media = sample["media"]!.AsArray();
        Media = [.. media.Select(item => (string)item!["id"]!)];
        if (!first)
        {
            return;
        }

        media[4]!["depictedTime"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1020 } }""");
        media[0]!["depictedTime"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1020 } }""");
        media[3]!["depictedTime"] = JsonNode.Parse(
            """
            { "kind": "occurrenceWindow",
              "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 600 },
              "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 500 } }
            """);
        File.WriteAllText(path, sample.ToJsonString());
    }
}

/// <summary>
/// The gallery of a time (step 19.9, ADR 0021, R-MED-02 and R-MED-04): a picture has the time it was made and,
/// apart from it, the time it shows. Asked for a time, the gallery shows what shows that time, whenever it was
/// made – and names what was made in that time apart, so that neither is taken for the other.
/// </summary>
public sealed class GalleryOfATimeTests(ShownTimeSite site) : IClassFixture<ShownTimeSite>
{
    private const string Story = "story-rostam-sohrab";

    private async Task<Browser> Reader()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        return visitor;
    }

    [Fact]
    public async Task A_picture_says_the_time_it_shows_apart_from_the_time_it_was_made()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/media/{site.Media[4]}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Shows the time", page.Main, StringComparison.Ordinal);
        Assert.Contains("1020 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("another time than the one it was made in", page.Main, StringComparison.Ordinal);

        // It was made in 2026 and is found by neither time as the other: the year it was made in is no time it shows.
        Assert.Contains("15 November 2026", page.Main, StringComparison.Ordinal);
        var made = await visitor.Get($"/en/{Story}/gallery?time=2026");
        Assert.DoesNotContain($"id=\"{site.Media[4]}\"", made.Html, StringComparison.Ordinal);
        Assert.Contains("Made in this time", made.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1020", 0, false)]
    [InlineData("1000 to 1100", 0, false)]
    [InlineData("۱۰۲۰", 0, false)]
    [InlineData("600 to 500 BCE", 1, false)]
    [InlineData("550 BCE", 1, true)]
    [InlineData("2.5 to 2.6 ka", 1, true)]
    public async Task The_gallery_of_a_time_shows_what_shows_that_time_whenever_it_was_made(string typed, int shown, bool possibly)
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/gallery?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Pictures of ", page.Main, StringComparison.Ordinal);
        Assert.Contains($"id=\"{site.Media[4 - shown]}\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"{site.Media[3 + shown]}\"", page.Html, StringComparison.Ordinal);

        // A picture the site may not show and only names is found by the time it shows too, as a reference.
        Assert.Equal(shown == 0, page.Html.Contains("metmuseum.org", StringComparison.Ordinal));
        Assert.Equal(possibly, page.Main.Contains("may show this time", StringComparison.Ordinal));

        // The card says both times.
        Assert.Contains("Made:", page.Main, StringComparison.Ordinal);
        Assert.Contains("Shows the time:", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_was_made_in_a_time_is_named_apart_from_what_shows_it()
    {
        var visitor = await Reader();

        // The two pictures of the site were made in 2026; neither of them shows that time.
        var page = await visitor.Get($"/en/{Story}/gallery?time=2000+to+2100");
        Assert.Contains("Made in this time", page.Main, StringComparison.Ordinal);
        Assert.Contains("They do not show it, or do not say what time they show.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"card media-card\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/media/{site.Media[4]}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/media/{site.Media[3]}\"", page.Html, StringComparison.Ordinal);

        // A time nothing shows and nothing was made in: said, with the way back to the whole gallery.
        var nothing = await visitor.Get($"/en/{Story}/gallery?time=55+Ma");
        Assert.Contains("Nothing here shows this time.", nothing.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/gallery\"", nothing.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("400 to 300")]
    [InlineData("-2147483648")]
    [InlineData("<script>")]
    public async Task What_is_no_time_leaves_the_gallery_whole_and_says_so(string typed)
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/gallery?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("That is no time", page.Main, StringComparison.Ordinal);
        Assert.Contains($"id=\"{site.Media[4]}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"id=\"{site.Media[3]}\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_whole_gallery_asks_for_a_time_and_a_reader_who_has_not_read_to_a_picture_does_not_find_it_by_its_time()
    {
        var reader = await Reader();
        var whole = await reader.Get($"/en/{Story}/gallery");
        Assert.Contains("name=\"time\"", whole.Html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Story}/gallery\"", whole.Html, StringComparison.Ordinal);

        // Somebody at the beginning of the story: what a later stage shows is in no gallery, of a time either.
        var early = site.Visitor();
        var late = await reader.Get($"/en/{Story}/gallery?time=1+to+2000");
        var first = await early.Get($"/en/{Story}/gallery?time=1+to+2000");
        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Contains($"id=\"{site.Media[4]}\"", late.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(site.Media[4], first.Html, StringComparison.Ordinal);

        // What the first stage may know of – a picture that is only named – is found by its time at both stages.
        Assert.Contains(site.Media[0], late.Html, StringComparison.Ordinal);
        Assert.Contains(site.Media[0], first.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_timeline_of_the_collection_has_a_lane_for_what_the_pictures_show()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/timeline");

        Assert.Contains("What the pictures show", page.Main, StringComparison.Ordinal);
        Assert.Contains($"id=\"item-shown-{site.Media[4]}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"id=\"item-{site.Media[4]}\"", page.Html, StringComparison.Ordinal);
    }
}
