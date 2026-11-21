using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the pictures that show a time, with one picture placed twice: test data. The illustration of the
/// family stands beside Rostam with words and a source of its own – in Persian as well – and at the third step
/// of the telling with other words.
/// </summary>
public sealed class PlacedSite : ShownTimeSite
{
    /// <summary>The picture that is placed.</summary>
    public const string Picture = "media-dastan-family-illustration";

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
        var item = sample["media"]!.AsArray().Single(media => (string?)media!["id"] == Picture)!;
        item["depictedEventIds"] = new JsonArray("event-03");
        item["placements"] = JsonNode.Parse(
            """
            [
              { "entityId": "character-rostam", "caption": "Rostam stands in the middle of the drawing.", "sourceIds": ["src-ganjoor-lineage"] },
              { "eventId": "event-03", "caption": "The lineage as Sohrab hears it at this step." }
            ]
            """);
        File.WriteAllText(path, sample.ToJsonString());

        var translated = Path.Combine(Content, "samples", "shahnameh-pilot.fa.json");
        var persian = JsonNode.Parse(File.ReadAllText(translated))!.AsObject();
        persian["media"]![Picture]!["places"] = new JsonObject { ["character-rostam"] = "رستم در میانهٔ نمودار ایستاده است." };
        File.WriteAllText(translated, persian.ToJsonString());
    }
}

/// <summary>
/// Stories, times and pictures (step 20.10): the page of a story on the timeline of the site, the still chart
/// that says what the moving one says, the gallery that begins with its pictures and is found again as it was
/// left, and one picture in several places with words for each.
/// </summary>
public sealed class StoriesTimesPicturesTests(PlacedSite site, DeepTimeSite deep) : IClassFixture<PlacedSite>, IClassFixture<DeepTimeSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Picture = PlacedSite.Picture;

    private async Task<Browser> Reader(int stage = 4, string language = "en")
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(stage, language);
        return visitor;
    }

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    private static string Caption(Page page) => Words(Regex.Match(page.Main, "<figcaption>.*?</figcaption>", RegexOptions.Singleline).Value);

    private static string Card(Page page, string id) => Regex.Match(page.Main, $"<li class=\"card media-card\" id=\"{id}\">.*?</li>", RegexOptions.Singleline).Value;

    private static JsonElement Timeline(Page page) =>
        JsonDocument.Parse(WebUtility.HtmlDecode(Regex.Match(page.Html, "<dastan-timeline [^>]*data-timeline=\"([^\"]*)\"").Groups[1].Value)).RootElement;

    [Theory]
    [InlineData("en", "In the order of the telling")]
    [InlineData("fa", null)]
    public async Task The_page_of_a_story_gives_its_steps_to_the_timeline_on_the_axis_of_the_telling_and_no_other(string language, string? title)
    {
        var late = await (await Reader(language: language)).Get($"/{language}/{Story}/story");
        var early = await (await Reader(stage: 0, language: language)).Get($"/{language}/{Story}/story");
        var (all, some) = (Timeline(late), Timeline(early));

        // One view, the order of the telling: no calendar, no depth of time, no order inside the world.
        var view = Assert.Single(all.GetProperty("views").EnumerateArray());
        Assert.Equal("order", view.GetProperty("axis").GetString());
        if (title is not null)
        {
            Assert.Equal(title, view.GetProperty("title").GetString());
        }

        // The steps are numbers of the telling, from the first to the last that was read.
        var steps = Regex.Matches(late.Main, "<li class=\"step\" id=\"step-").Count;
        Assert.Equal((1m, steps), (view.GetProperty("from").GetDecimal(), view.GetProperty("to").GetDecimal()));
        var marks = view.GetProperty("lanes").EnumerateArray().SelectMany(lane => lane.GetProperty("marks").EnumerateArray()).ToList();
        Assert.All(marks, mark => Assert.Equal(("moment", mark.GetProperty("from").GetDecimal()), (mark.GetProperty("shape").GetString(), mark.GetProperty("to").GetDecimal())));

        // Every mark leads to the cell of the table that says the same, and every cell that says something has a mark.
        var cells = Regex.Matches(late.Main, "<td id=\"(item-s\\d+-[^\"]+)\">").Select(match => match.Groups[1].Value).ToHashSet();
        Assert.Equal(cells, marks.Select(mark => mark.GetProperty("address").GetString()!.Split('#')[1]).ToHashSet());
        Assert.NotEmpty(cells);

        // Who has read less is given less: the answer holds no step that was not read.
        var fewer = some.GetProperty("views")[0];
        Assert.True(fewer.GetProperty("to").GetDecimal() < steps);
        Assert.DoesNotContain("character-sohrab", early.Html, StringComparison.Ordinal);

        // Without the script the chart of the server is what the page shows.
        Assert.Matches("<div class=\"tlv-still\" data-timeline-still>(?s:.*?)<svg", late.Main);
    }

    [Fact]
    public async Task The_still_chart_draws_a_window_as_a_window_and_a_moment_as_a_point()
    {
        // The video shows a time between 600 and 500 BCE; the illustration shows the year 1020.
        var page = await (await Reader()).Get($"/en/{Story}/timeline?from=-700&to=1100");
        var chart = Regex.Match(page.Main, "<div class=\"tlv-still\" data-timeline-still>(?s:.*?)</dastan-timeline>").Value;

        // The chart that runs across and the one that runs down each draw the one window with a length, and the moments as points.
        var windows = Regex.Matches(chart, "<rect class=\"tl-window [^\"]*\" x=\"[0-9.]+em\" y=\"[0-9.]+em\" width=\"([0-9.]+)em\" height=\"([0-9.]+)em\"");
        Assert.InRange(windows.Count, 1, 2);
        Assert.All(windows, window => Assert.True(Math.Max(
            decimal.Parse(window.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(window.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)) > 1));
        Assert.Contains("class=\"tl-dot ", chart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_a_still_chart_cannot_draw_stands_as_a_table_of_its_own_axis()
    {
        var visitor = deep.Visitor();
        await visitor.ReadTo(4, "en");
        var page = await visitor.Get($"/en/{Story}/timeline");
        var section = Regex.Match(page.Main, "<section class=\"other-axes\"(?s:.*?)</section>").Value;

        Assert.Contains("Counted otherwise than in years of the calendar", section, StringComparison.Ordinal);
        Assert.Contains("nothing is converted from one into another", Words(section), StringComparison.Ordinal);

        // One table for the depth of time, named as the moving view is named, oldest first, each row leading to the table of the page.
        var caption = Assert.Single(Regex.Matches(section, "<caption>(.*?)</caption>")).Groups[1].Value;
        Assert.Equal(Timeline(page).GetProperty("views").EnumerateArray().Single(view => view.GetProperty("axis").GetString() == "depth").GetProperty("title").GetString(), WebUtility.HtmlDecode(caption));
        var rows = Regex.Matches(section, "<tr>\\s*<th scope=\"row\">(?s:.*?)</tr>").Select(match => Words(match.Value)).ToList();
        Assert.True(rows.Count >= 4);
        Assert.Contains(rows, row => row.Contains("(once, somewhere in this window)", StringComparison.Ordinal));
        Assert.Contains(rows, row => row.Contains("(lasted)", StringComparison.Ordinal));
        Assert.All(Regex.Matches(section, "<td><a href=\"([^\"]*)\"").Select(match => match.Groups[1].Value), address => Assert.Contains("/timeline#item-", address, StringComparison.Ordinal));

        // What stands in a table is not said to be without a place.
        Assert.DoesNotContain("Not drawn, because their time has no place on this axis", Regex.Match(page.Main, "<div class=\"tlv-still\"(?s:.*?)</dastan-timeline>").Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task One_picture_has_the_words_of_the_place_it_stands_in()
    {
        var visitor = await Reader();

        // Beside Rostam: the words and the source of that place, and what is said of the picture itself below.
        var beside = await visitor.Get($"/en/{Story}/media/{Picture}?entity=character-rostam");
        var own = Caption(await visitor.Get($"/en/{Story}/media/{Picture}"));
        Assert.Equal("Rostam stands in the middle of the drawing.", Caption(beside));
        Assert.Contains($"Of the picture itself: {own}", Words(beside.Main), StringComparison.Ordinal);
        Assert.Matches($"What supports the words of this place:\\s*<a href=\"{Story}/source/src-ganjoor-lineage\\?", beside.Main);

        // At the step: other words. Beside somebody without a place, and anywhere else: the words of the picture.
        Assert.Equal("The lineage as Sohrab hears it at this step.", Caption(await visitor.Get($"/en/{Story}/media/{Picture}?event=event-03")));
        var zal = await visitor.Get($"/en/{Story}/media/{Picture}?entity=character-zal");
        Assert.Equal(own, Caption(zal));
        Assert.DoesNotContain("Of the picture itself", zal.Main, StringComparison.Ordinal);
        Assert.Equal(own, Caption(await visitor.Get($"/en/{Story}/media/{Picture}?entity=character-nobody")));

        // The galleries and the profile say the same as the page of the picture in that place.
        Assert.Contains("Rostam stands in the middle of the drawing.", Card(await visitor.Get($"/en/{Story}/entity/character-rostam/gallery"), Picture), StringComparison.Ordinal);
        Assert.Contains("The lineage as Sohrab hears it at this step.", Card(await visitor.Get($"/en/{Story}/event/event-03/gallery"), Picture), StringComparison.Ordinal);
        Assert.Contains(own, Words(Card(await visitor.Get($"/en/{Story}/gallery"), Picture)), StringComparison.Ordinal);
        Assert.Contains("Rostam stands in the middle of the drawing.", (await visitor.Get($"/en/{Story}/entity/character-rostam")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Rostam stands in the middle", (await visitor.Get($"/en/{Story}/entity/character-zal")).Main, StringComparison.Ordinal);

        // In Persian the place has its translation; a place without one is said in the language it was written in.
        var persian = await Reader(language: "fa");
        Assert.Equal("رستم در میانهٔ نمودار ایستاده است.", Caption(await persian.Get($"/fa/{Story}/media/{Picture}?entity=character-rostam")));
    }

    [Fact]
    public async Task The_words_of_a_place_are_shown_to_nobody_who_has_not_read_to_the_picture()
    {
        // The picture is told from the third stage; before it neither the page nor the words of a place exist.
        var early = await Reader(stage: 1);

        Assert.Equal(HttpStatusCode.NotFound, (await early.Get($"/en/{Story}/media/{Picture}?entity=character-rostam")).Status);
        Assert.DoesNotContain("middle of the drawing", (await early.Get($"/en/{Story}/entity/character-rostam")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("middle of the drawing", (await early.Get($"/en/{Story}/entity/character-rostam/gallery")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_gallery_begins_with_its_pictures_and_keeps_what_it_is_about_in_a_fold()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/gallery");

        // The title, the fold – closed –, the pictures: no form and no tools stand before them.
        var fold = Regex.Match(page.Main, "<details class=\"fold gallery-about\"(?s:.*?)</details>");
        Assert.StartsWith("<details class=\"fold gallery-about\"><summary>About this gallery", fold.Value, StringComparison.Ordinal);
        Assert.Contains("<form class=\"search moment-form\"", fold.Value, StringComparison.Ordinal);
        Assert.Contains("class=\"chart-tools\"", fold.Value, StringComparison.Ordinal);
        var between = page.Main[(fold.Index + fold.Length)..page.Main.IndexOf("<ul class=\"cards gallery\"", StringComparison.Ordinal)];
        Assert.DoesNotContain("<form", between, StringComparison.Ordinal);
        Assert.DoesNotContain("<nav", between, StringComparison.Ordinal);

        // Open when the visitor chose something in it, or when there is nothing to show and it says why.
        Assert.Contains("<details class=\"fold gallery-about\" open>", (await visitor.Get($"/en/{Story}/gallery?order=oldest")).Main, StringComparison.Ordinal);
        Assert.Contains("<details class=\"fold gallery-about\" open>", (await visitor.Get($"/en/{Story}/gallery?time=1020")).Main, StringComparison.Ordinal);
        Assert.Contains("<details class=\"fold gallery-about\" open>", (await visitor.Get($"/en/{Story}/gallery?time=nonsense")).Main, StringComparison.Ordinal);
        Assert.Contains("<details class=\"fold gallery-about\" open>", (await visitor.Get($"/en/{Story}/gallery?time=5000")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("entity/character-rostam/gallery?order=newest", "?entity=character-rostam&order=newest", "entity/character-rostam/gallery?order=newest")]
    [InlineData("gallery?order=oldest&kind=image", "?order=oldest&kind=image", "gallery?order=oldest&kind=image")]
    [InlineData("gallery?time=1020", "?time=1020", "gallery?time=1020")]
    [InlineData("event/event-03/gallery", "?event=event-03", "event/event-03/gallery")]
    public async Task The_way_back_from_a_picture_leads_to_the_gallery_as_it_was_left(string gallery, string carried, string back)
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Story}/{gallery}");

        // Every picture of the gallery is opened with what the gallery was asked with.
        var links = Regex.Matches(page.Html, $"<h2><a href=\"({Story}/media/[^\"]*)\">").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).ToList();
        Assert.NotEmpty(links);
        Assert.All(links, link => Assert.EndsWith(carried, link, StringComparison.Ordinal));

        // The page of a picture leads back to that gallery, at that picture; the one before and the next are its neighbours there.
        var order = links.Select(link => link[(link.IndexOf("/media/", StringComparison.Ordinal) + 7)..link.IndexOf('?', StringComparison.Ordinal)]).ToList();
        foreach (var (id, at) in order.Select((id, at) => (id, at)))
        {
            var picture = await visitor.Get($"/en/{links[at]}");
            Assert.Equal(HttpStatusCode.OK, picture.Status);
            Assert.Contains($"<a href=\"{Story}/{back}#{id}\">", picture.Main, StringComparison.Ordinal);
            var next = Regex.Match(picture.Html, "rel=\"next\" href=\"([^\"]*)\"");
            Assert.Equal(at < order.Count - 1, next.Success);
            if (next.Success)
            {
                Assert.Equal(links[at + 1], WebUtility.HtmlDecode(next.Groups[1].Value));
            }
        }
    }

    [Fact]
    public async Task What_the_address_of_a_picture_claims_and_the_gallery_does_not_hold_is_not_believed()
    {
        var visitor = await Reader();

        // A kind the picture is not, a time it does not show, an order that is none: the way back is the whole gallery.
        var page = await visitor.Get($"/en/{Story}/media/{Picture}?kind=video&time=1500&order=sideways");
        Assert.Contains($"<a href=\"{Story}/gallery#{Picture}\">", page.Main, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"{Story}/gallery#{Picture}\">", (await visitor.Get($"/en/{Story}/media/{Picture}?time=not%20a%20time")).Main, StringComparison.Ordinal);
    }
}
