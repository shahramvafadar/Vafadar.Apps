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

    /// <summary>The name of the third collection: the story told once more.</summary>
    public const string Retold = "story-retold";

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

        // A third collection: the story once more under another name – another telling of the same world.
        var again = File.ReadAllText(Path.Combine(_content, "samples", "shahnameh-pilot.json"))
            .Replace("story-rostam-sohrab", Retold, StringComparison.Ordinal)
            .Replace("dastan-shahnameh-rostam-sohrab-pilot-v0.5", "dastan-shahnameh-retold", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(_content, "samples", "retold.json"), again);

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

        Assert.Equal([Story, Club, TwoCollectionsSite.Retold], library.Collections.Select(collection => collection.Collection));
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

        // With several collections the search of the start page looks through all of them.
        Assert.Contains("<form class=\"search\" method=\"get\" action=\"search\"", page.Html, StringComparison.Ordinal);
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

/// <summary>
/// The same world in another collection of the site (step 14.11): a collection says where else its world is
/// told, and that the reading position there is another one.
/// </summary>
public sealed class SameWorldTests(TwoCollectionsSite site) : IClassFixture<TwoCollectionsSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Retold = TwoCollectionsSite.Retold;

    [Theory]
    [InlineData("en", "The same world, told elsewhere on this site", "with its own reading position")]
    [InlineData("fa", "همین جهان، در جای دیگری از این سایت", "جایگاه خواندن خودش")]
    public async Task A_collection_names_the_other_collections_of_its_world(string language, string title, string reading)
    {
        var story = await site.Visitor().Get($"/{language}/{Story}");
        var retold = await site.Visitor().Get($"/{language}/{Retold}");

        Assert.Contains(title, story.Main, StringComparison.Ordinal);
        Assert.Contains(reading, story.Main, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"{Retold}\"", story.Html, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"{Story}\"", retold.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_collection_of_another_world_is_not_named_and_names_nothing()
    {
        var club = await site.Visitor().Get($"/en/{TwoCollectionsSite.Club}");
        var story = await site.Visitor().Get($"/en/{Story}");

        Assert.DoesNotContain("told elsewhere", club.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"<a href=\"{TwoCollectionsSite.Club}\"", story.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_was_read_in_one_telling_is_not_read_in_the_other()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        var later = site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;

        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Story}/entity/{later}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Retold}/entity/{later}")).Status);

        // Read on in the other telling, and it opens there too – by its own cookie.
        await visitor.ReadTo(4, collection: Retold);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Retold}/entity/{later}")).Status);
    }
}

/// <summary>
/// The search of a site with several collections (step 14.10): a name is looked for in all of them, each in
/// what the visitor has read of it.
/// </summary>
public sealed class SiteSearchTests(TwoCollectionsSite site, DemoSite one) : IClassFixture<TwoCollectionsSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "A name, in everything that is here to read")]
    [InlineData("fa", "یک نام، در هر آنچه این‌جا برای خواندن هست")]
    public async Task A_name_is_found_in_every_collection_under_its_collection(string language, string label)
    {
        var page = await site.Visitor().Get($"/{language}/search?q=club");
        var hero = await site.Visitor().Get($"/{language}/search?q=rostam");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(label, page.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{TwoCollectionsSite.Club}/entity/{TwoCollectionsSite.Club}\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"found-{Story}\"", page.Html, StringComparison.Ordinal);

        // The hero is told of in two collections of the site: he is found in both, each under its name.
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", hero.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{TwoCollectionsSite.Retold}/entity/character-rostam\"", hero.Html, StringComparison.Ordinal);
        Assert.Contains("content=\"noindex, follow\"", hero.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_a_visitor_has_not_read_in_a_collection_is_not_found_there()
    {
        var visitor = site.Visitor();
        var later = site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0);
        var name = later.Label.Resolve("en").Value;
        var before = await visitor.Get($"/en/search?q={Uri.EscapeDataString(name)}");

        Assert.DoesNotContain($"/entity/{later.Id}\"", before.Html, StringComparison.Ordinal);

        // Read on in one telling: the name is found there, and still not in the other.
        await visitor.ReadTo(4);
        var after = await visitor.Get($"/en/search?q={Uri.EscapeDataString(name)}");
        Assert.Contains($"href=\"{Story}/entity/{later.Id}\"", after.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"href=\"{TwoCollectionsSite.Retold}/entity/{later.Id}\"", after.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?q=", "A name, in everything")]
    [InlineData("", "A name, in everything")]
    [InlineData("?q=zzzzzz", "Nothing")]
    [InlineData("?q=%3Cscript%3E", "Nothing")]
    public async Task Nothing_typed_and_nothing_found_say_so(string query, string said)
    {
        var page = await site.Visitor().Get($"/en/search{query}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(said, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"cards\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_with_one_collection_keeps_the_search_of_that_collection_on_its_start_page()
    {
        var page = await one.Visitor().Get("/en/");

        Assert.Contains($"action=\"{Story}/search\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"search\"", page.Html, StringComparison.Ordinal);
    }
}
