using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site with a copy of the collection in which something dated is stated about the work: a period from the
/// first reading stage on, and an event from the fourth. The statements are test data – the collection of the
/// repository states nothing with a date about its entities.
/// </summary>
public class DatedSite : DemoSite
{
    /// <summary>Gets the folder of the content of this site: a copy the tests may add to.</summary>
    protected string Content { get; } = Path.Combine(Path.GetTempPath(), "dastan-dated-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", Content);
        if (Directory.Exists(Content))
        {
            return;
        }

        // The collection and, beside it, the documents of the editors it points to for reviewers.
        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(Content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var work = sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == "work-shahnameh")!;
        work["dated"] = JsonNode.Parse(
            """
            [
              {
                "key": "work-written",
                "label": "The work is written",
                "summary": "Test data of the tests.",
                "time": {
                  "kind": "period",
                  "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 977, "approximate": true } },
                  "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
                  "continuity": "notAsserted"
                },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 0,
                "reviewStatus": "proposed"
              },
              {
                "key": "work-copied",
                "label": "A secret copy is made",
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1330 } },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 3,
                "reviewStatus": "proposed"
              },
              {
                "key": "work-finished",
                "label": "The work is finished",
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 0,
                "reviewStatus": "disputed",
                "review": { "by": "an editor", "on": "2026-11-16", "note": "The manuscripts give two years." }
              },
              {
                "key": "work-lost",
                "label": "A lost first version",
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 990 } },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 0,
                "reviewStatus": "withdrawn",
                "review": { "by": "an editor", "on": "2026-11-16", "note": "The source does not say this." }
              }
            ]
            """);
        File.WriteAllText(path, sample.ToJsonString());

        var persianPath = Path.Combine(Content, "samples", "shahnameh-pilot.fa.json");
        var persian = JsonNode.Parse(File.ReadAllText(persianPath))!.AsObject();
        persian["dated"] = JsonNode.Parse("""{ "work-written": { "label": "سروده شدنِ اثر" } }""");
        File.WriteAllText(persianPath, persian.ToJsonString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(Content))
        {
            Directory.Delete(Path.GetDirectoryName(Content)!, recursive: true);
        }
    }
}

/// <summary>
/// The timeline of a collection (step 9.4): what of it has a time of the calendar, as the chart and the table of
/// a comparison – and never a statement of a later reading stage.
/// </summary>
/// <summary>
/// The timeline of one name (step 19.9, R-EXP-05): what is chosen elsewhere – in a graph, on a page – is carried
/// to the timeline, which then shows what is stated about that name and the pictures that show it.
/// </summary>
public sealed class TimelineOfANameTests(DatedSite site) : IClassFixture<DatedSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "Shahnameh in time")]
    [InlineData("fa", "در گذر زمان")]
    public async Task A_name_has_a_timeline_of_what_is_stated_about_it(string language, string title)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/work-shahnameh/timeline");

        Assert.Equal(System.Net.HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains("id=\"item-work-written\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/work-shahnameh\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/timeline\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/work-shahnameh/connections\"", page.Html, StringComparison.Ordinal);

        // The choices of the chart stay with the name: a step of the chart does not lead back to the whole collection.
        Assert.Contains($"href=\"{Story}/entity/work-shahnameh/timeline?view=across", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"{Story}/timeline?view=", page.Html, StringComparison.Ordinal);

        // The pictures of others are not on it.
        Assert.DoesNotContain("id=\"item-media-", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_nothing_is_dated_of_says_so_and_a_name_that_is_none_is_not_found()
    {
        var visitor = site.Visitor();
        var nothing = await visitor.Get($"/en/{Story}/entity/character-rostam/timeline");
        Assert.Equal(System.Net.HttpStatusCode.OK, nothing.Status);
        Assert.DoesNotContain("dastan-timeline", nothing.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/timeline\"", nothing.Html, StringComparison.Ordinal);

        // Somebody the reader has not read to, and nobody at all: the same answer.
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await visitor.Get($"/en/{Story}/entity/character-nobody/timeline")).Status);
        Assert.Equal((await visitor.Get($"/en/{Story}/entity/character-sohrab")).Status, (await visitor.Get($"/en/{Story}/entity/character-sohrab/timeline")).Status);
    }

    [Fact]
    public async Task A_graph_offers_the_timeline_of_a_node_that_has_one()
    {
        var visitor = site.Visitor();
        var page = await visitor.Get($"/en/{Story}/entity/work-shahnameh/connections.json");
        if (page.Status != System.Net.HttpStatusCode.OK)
        {
            // The work has no connections at this stage: the address of the graph answers as its page does.
            Assert.Equal((await visitor.Get($"/en/{Story}/entity/work-shahnameh/connections")).Status, page.Status);
            return;
        }

        var node = System.Text.Json.Nodes.JsonNode.Parse(page.Html)!["nodes"]!.AsArray().Single(found => (string?)found!["id"] == "work-shahnameh")!;
        Assert.Equal($"{Story}/entity/work-shahnameh/timeline", (string?)node["timeline"]);
    }
}

public sealed class TimelinePageTests(DemoSite site, DatedSite dated) : IClassFixture<DemoSite>, IClassFixture<DatedSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Timeline = $"{Story}/timeline";

    private static async Task<Page> At(DemoSite host, int level, string language = "en", string query = "")
    {
        var visitor = host.Visitor();
        if (level > 0)
        {
            await visitor.ReadTo(level, language);
        }

        return await visitor.Get($"/{language}/{Timeline}{query}");
    }

    private static List<string> Rows(Page page) => [.. Regex.Matches(page.Html, "<tr id=\"item-([^\"]+)\"").Select(match => match.Groups[1].Value)];

    [Fact]
    public async Task At_the_opening_the_collection_of_the_repository_has_nothing_with_a_date_to_show_and_says_so()
    {
        var page = await At(site, 0);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Rostam and Sohrab in time</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("Nothing with a date can be shown here.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}#reading", page.Links);
        Assert.DoesNotContain("class=\"stage", page.Html, StringComparison.Ordinal);
        Assert.Empty(Rows(page));
    }

    [Fact]
    public async Task The_images_and_videos_a_reader_may_see_stand_on_the_chart_at_the_time_they_were_made()
    {
        var page = await At(site, 4);
        var shown = site.Catalogue.Media.Where(item => item.DemoRenderAllowed).Select(item => item.Id.Value).ToList();

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.NotEmpty(shown);
        Assert.Equal(shown, Rows(page));
        Assert.Contains("class=\"stage", page.Html, StringComparison.Ordinal);
        Assert.Contains("<caption><bdi>Images and videos</bdi></caption>", page.Html, StringComparison.Ordinal);
        Assert.Contains("class=\"tl-callout", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", page.Html);
    }

    [Fact]
    public async Task What_is_stated_about_an_entity_with_a_date_is_a_lane_of_the_chart_and_a_row_of_the_table()
    {
        var page = await At(dated, 0);

        Assert.Equal(["work-written", "work-finished"], Rows(page));
        Assert.Contains("<caption><bdi>Shahnameh</bdi></caption>", page.Html, StringComparison.Ordinal);
        Assert.Matches("<tr id=\"item-work-written\">\\s*<th scope=\"row\">\\s*<bdi>The work is written</bdi>", page.Html);
        Assert.Contains("Test data of the tests.", page.Main, StringComparison.Ordinal);
        Assert.Contains("about 977 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("Suggested by the editors, not checked yet", page.Main, StringComparison.Ordinal);
        Assert.Contains("class=\"tl-bar", page.Html, StringComparison.Ordinal);

        // The chart begins with the stretch of what it holds, with room at both ends.
        Assert.Matches("Shown: 9\\d\\d CE (to|–) 10\\d\\d CE", page.Main);
    }

    [Fact]
    public async Task A_statement_of_a_later_stage_is_on_no_page_of_a_reader_before_it_whatever_the_address_asks_for()
    {
        foreach (var query in new[] { string.Empty, "?from=1300&to=1400", "?view=down&events=shown&lanes=9", "?scale=3" })
        {
            var early = await At(dated, 2, query: query);

            Assert.Equal(HttpStatusCode.OK, early.Status);
            Assert.DoesNotContain("secret copy", early.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("work-copied", early.Html, StringComparison.Ordinal);
        }

        var late = await At(dated, 3);
        Assert.Contains("work-copied", Rows(late));
        Assert.Contains("A secret copy is made", late.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task On_a_Persian_page_the_statements_carry_their_Persian_texts_and_the_choices_stay_on_the_page()
    {
        var page = await At(dated, 0, "fa", "?view=down");
        var text = WebUtility.HtmlDecode(page.Html);

        Assert.Contains("رستم و سهراب در گذر زمان", text, StringComparison.Ordinal);
        Assert.Contains("سروده شدنِ اثر", text, StringComparison.Ordinal);
        Assert.Contains(page.Links, link => link.StartsWith($"{Timeline}?", StringComparison.Ordinal) && link.Contains("view=down", StringComparison.Ordinal));
        Assert.DoesNotContain(page.Links, link => link.StartsWith("compare/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_source_of_a_statement_is_reached_through_the_page_of_the_site_that_says_where_it_leads()
    {
        var page = await At(dated, 0);

        Assert.Contains(page.Links, link => link.StartsWith($"{Story}/source/src-ganjoor-index", StringComparison.Ordinal));
        Assert.DoesNotContain(page.Links, link => link.StartsWith("https://ganjoor.net", StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_page_of_the_collection_leads_to_its_timeline_and_the_timeline_back()
    {
        var visitor = site.Visitor();

        var collection = await visitor.Get($"/en/{Story}");
        var timeline = await visitor.Get($"/en/{Timeline}");

        Assert.Contains(Timeline, collection.Links);
        Assert.Contains(Story, timeline.Links);
    }

    [Theory]
    [InlineData("/en/story-nobody/timeline")]
    [InlineData("/en/compare/story-rostam-sohrab")]
    public async Task A_timeline_of_what_is_not_the_collection_is_not_found(string address)
    {
        var page = await site.Visitor().Get(address);

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
    }
}
