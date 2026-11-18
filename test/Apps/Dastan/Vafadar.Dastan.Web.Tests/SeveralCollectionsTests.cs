using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site with two collections: the story of the repository and, beside it, a collection about a club that
/// tells no story – written from the template of the editors' tools. The second collection is test data.
/// </summary>
public sealed class TwoCollectionsSite : DemoSite
{
    private readonly string subject;

    public TwoCollectionsSite()
        : this("club")
    {
    }

    /// <summary>Starts the site with a second collection about something of that name.</summary>
    internal TwoCollectionsSite(string subject) => this.subject = subject;

    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-two-" + Guid.NewGuid().ToString("N"), "content");

    /// <summary>The name of the second collection in addresses.</summary>
    public const string Club = "subject-club";

    /// <summary>Gets the folder the reports of visitors are kept in.</summary>
    public string Reports => Path.Combine(Path.GetDirectoryName(_content)!, "reports");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        builder.UseSetting("Dastan:Reports", Reports);
        if (Directory.Exists(_content))
        {
            return;
        }

        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(_content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        // The second collection: what the template gives, about something of another name. Its file comes
        // before the story's in the alphabet – the site still begins with the one it was told first.
        var club = JsonNode.Parse(ContentTemplates.Subject(subject, new DateOnly(2026, 11, 18)))!.AsObject();
        if (subject != "club")
        {
            // A collection about something whose name the site uses itself.
            var text = club.ToJsonString().Replace($"subject-{subject}", subject, StringComparison.Ordinal);
            club = JsonNode.Parse(text)!.AsObject();
        }

        var about = club["entities"]!.AsArray().First(entity => (string?)entity!["id"] == (string?)club["subjectId"])!;
        about["label"] = "A club of the tests";
        about["summary"] = "Test data: a club that tells no story.";
        File.WriteAllText(Path.Combine(_content, "samples", "a-club.json"), club.ToJsonString());

        // Beside the collections, a file that is none: the site passes it over.
        File.WriteAllText(Path.Combine(_content, "samples", "notes.json"), """{ "note": "No collection." }""");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }
}

/// <summary>
/// Several collections on one site (step 14.10): each has its address, its reading position and its search;
/// the start page lists them; nothing of one is reached under the name of another.
/// </summary>
public sealed class SeveralCollectionsTests(TwoCollectionsSite site) : IClassFixture<TwoCollectionsSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Club = TwoCollectionsSite.Club;

    [Fact]
    public void The_site_begins_with_the_collection_it_was_told_and_passes_over_what_is_none()
    {
        var library = site.Services.GetRequiredService<DastanLibrary>();

        Assert.Equal([Story, Club], library.Collections.Select(collection => collection.Collection));
        Assert.Same(library.Collections[1], library.Find(Club));
        Assert.Null(library.Find("notes"));

        // The pages a search engine would be given are those of both.
        var pages = SearchEngineFiles.Pages(library);
        Assert.Contains(Story, pages);
        Assert.Contains(Club, pages);
        Assert.Contains($"{Club}/entity/{Club}", pages);
    }

    [Theory]
    [InlineData("en", "What is here to read")]
    [InlineData("fa", "آنچه این‌جا برای خواندن هست")]
    public async Task The_start_page_lists_every_collection_the_first_at_the_top(string language, string heading)
    {
        var page = await site.Visitor().Get($"/{language}/");

        Assert.Contains(heading, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Club}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("A club of the tests", page.Main, StringComparison.Ordinal);
        Assert.True(page.Html.IndexOf($"href=\"{Story}\"", StringComparison.Ordinal) < page.Html.IndexOf($"href=\"{Club}\"", StringComparison.Ordinal));

        // A search looks through one collection: the start page of a site with several has none.
        Assert.DoesNotContain("role=\"search\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Each_collection_has_its_pages_under_its_own_name()
    {
        var visitor = site.Visitor();
        var club = await visitor.Get($"/en/{Club}");
        var about = await visitor.Get($"/en/{Club}/entity/{Club}");
        var story = await visitor.Get($"/en/{Story}");
        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");

        Assert.Equal(HttpStatusCode.OK, club.Status);
        Assert.Contains("A club of the tests", club.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, about.Status);
        Assert.Equal(HttpStatusCode.OK, story.Status);
        Assert.Equal(HttpStatusCode.OK, rostam.Status);

        // The search of a collection is asked of that collection.
        Assert.Contains($"action=\"{Club}/search\"", club.Html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Story}/search\"", story.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData($"/en/{Club}/entity/character-rostam")]
    [InlineData($"/en/{Story}/entity/{Club}")]
    [InlineData($"/en/{Club}/story")]
    [InlineData("/en/another-collection")]
    public async Task Nothing_of_one_collection_is_reached_under_the_name_of_another(string address)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get(address)).Status);
    }

    [Fact]
    public async Task A_search_finds_in_its_own_collection_only()
    {
        var visitor = site.Visitor();
        var inStory = await visitor.Get($"/en/{Story}/search?q=club");
        var inClub = await visitor.Get($"/en/{Club}/search?q=club");
        var heroInClub = await visitor.Get($"/en/{Club}/search?q=rostam");

        Assert.DoesNotContain("A club of the tests", inStory.Main, StringComparison.Ordinal);
        Assert.Contains("A club of the tests", inClub.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("character-rostam", heroInClub.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task How_far_a_visitor_has_read_one_collection_says_nothing_about_another()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        var later = site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;

        // The story is read to its end, with its cookie; the club has its own stage and is not moved by it.
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Story}/entity/{later}")).Status);
        Assert.Contains($"dastan.reading.{Story}", visitor.Cookies.Keys);
        Assert.DoesNotContain($"dastan.reading.{Club}", visitor.Cookies.Keys);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Club}")).Status);

        // The files of the story are still delivered under its name, and none under the name of the club.
        var video = "media-dastan-parentage-video";
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/media/{Story}/{video}/poster")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/media/{Club}/{video}/poster")).Status);
    }

    [Fact]
    public async Task A_report_is_kept_under_the_collection_of_its_statement()
    {
        var from = $"/en/{Club}/evidence/entity/{Club}";
        var sent = await site.Visitor().Post("/report", from, ("collection", Club), ("statement", $"evidence/entity/{Club}"), ("text", "The club is older."), ("website", ""), ("returnUrl", from));

        Assert.Equal($"{from}?reported=thanks#report", sent.Response.Headers.Location!.OriginalString);
        var report = Assert.Single(new ReportBox(site.Reports).Waiting());
        Assert.Equal((Club, $"evidence/entity/{Club}"), (report.Collection, report.Statement));

        // A statement of the story is none of the club.
        var wrong = await site.Visitor().Post("/report", from, ("collection", Club), ("statement", "evidence/entity/character-rostam"), ("text", "Words."), ("website", ""), ("returnUrl", from));
        Assert.Equal(HttpStatusCode.SeeOther, wrong.Status);
        Assert.Single(new ReportBox(site.Reports).Waiting());
    }

    [Fact]
    public void A_collection_about_something_the_site_has_an_address_for_stops_the_site()
    {
        using var unreachable = new TwoCollectionsSite("media");

        var refusal = Assert.ThrowsAny<Exception>(() => unreachable.Services.GetRequiredService<DastanLibrary>());

        Assert.Contains("cannot be reached", refusal.ToString(), StringComparison.Ordinal);
    }
}
