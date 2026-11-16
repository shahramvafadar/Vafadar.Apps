using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Testing;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The pages of the first phase of the website, asked over HTTP as a browser asks them.</summary>
public sealed class SiteTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"{Story}/entity/character-rostam";
    private const string Sohrab = $"{Story}/entity/character-sohrab";

    // --- Language ---

    [Theory]
    [InlineData("fa-IR,fa;q=0.9,en;q=0.8", "/fa/")]
    [InlineData("de-DE,de;q=0.9", "/en/")]
    [InlineData(null, "/en/")]
    public async Task The_start_of_the_site_goes_to_the_visitors_language(string? accept, string expected)
    {
        var page = await site.Visitor(accept).Get("/");

        Assert.Equal(HttpStatusCode.Found, page.Status);
        Assert.Equal(expected, page.Response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("en", "ltr", "Discover how stories connect.")]
    [InlineData("fa", "rtl", "ببینید قصه‌ها چگونه به هم می‌پیوندند.")]
    public async Task A_page_says_its_language_and_direction_and_is_written_in_it(string language, string direction, string tagline)
    {
        var page = await site.Visitor().Get($"/{language}/");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal(language, page.Root("lang"));
        Assert.Equal(direction, page.Root("dir"));
        Assert.Contains($"<base href=\"/{language}/\"", page.Html, StringComparison.Ordinal);
        Assert.Contains(tagline, page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_other_language_is_one_link_away_on_the_same_page()
    {
        var english = await site.Visitor().Get($"/en/{Rostam}");
        var persian = await site.Visitor().Get($"/fa/{Rostam}");

        Assert.Contains($"/fa/{Rostam}", english.Links);
        Assert.Contains($"/en/{Rostam}", persian.Links);
        Assert.Contains("<h1><bdi>Rostam</bdi></h1>", english.Html, StringComparison.Ordinal);
        Assert.Contains("رستم", persian.Main, StringComparison.Ordinal);
        Assert.Contains("پهلوانی که جست‌وجوی اسب گم‌شده‌اش", persian.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Persian_page_counts_in_Persian_digits_and_says_that_its_text_is_a_draft()
    {
        var persian = await site.Visitor().Get($"/fa/{Rostam}");
        var english = await site.Visitor().Get($"/en/{Rostam}");

        Assert.Contains("گام ۱", persian.Main, StringComparison.Ordinal);
        Assert.Contains("Step 1", english.Main, StringComparison.Ordinal);
        Assert.Contains("پیش‌نویس", persian.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", english.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Text_the_editors_keep_in_English_is_marked_as_English_on_a_Persian_page()
    {
        var page = await site.Visitor().Get($"/fa/{Story}");

        Assert.Matches("<span class=\"foreign\" lang=\"en\" dir=\"ltr\">Eight selected narrative beats", page.Html);
        Assert.Contains("(به انگلیسی)", page.Text, StringComparison.Ordinal);
    }

    // --- The first pages ---

    [Fact]
    public async Task A_visitor_enters_opens_the_story_reads_an_entity_and_follows_a_connection()
    {
        var visitor = site.Visitor();

        var discover = await visitor.Open("/");
        Assert.Contains(Story, discover.Links);

        var story = await visitor.Get($"/en/{Story}");
        Assert.Contains("<h1><bdi>Rostam and Sohrab</bdi></h1>", story.Html, StringComparison.Ordinal);
        Assert.Contains(Rostam, story.Links);

        var rostam = await visitor.Get($"/en/{Rostam}");
        Assert.Matches("Zal</bdi></a>\\s*<span class=\"relation\"><bdi>Father of</bdi></span>\\s*<strong class=\"self\"><bdi>Rostam", rostam.Html);
        Assert.Contains($"{Story}/entity/character-zal", rostam.Links);

        var zal = await visitor.Get($"/en/{Story}/entity/character-zal");
        Assert.Equal(HttpStatusCode.OK, zal.Status);
        Assert.Matches("<strong class=\"self\"><bdi>Zal</bdi></strong>\\s*<span class=\"relation\"><bdi>Father of</bdi></span>\\s*<a href=\"[^\"]*character-rostam\"", zal.Html);
        Assert.Contains(Story, zal.Links);
    }

    [Fact]
    public async Task Somebody_who_is_only_mentioned_is_shown_as_mentioned()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var rostam = await visitor.Get($"/en/{Rostam}");

        Assert.Matches("Sohrab learns his lineage.*father identified in the explanation</bdi> <span class=\"presence\">is only mentioned, not present", rostam.Main.ReplaceLineEndings(" "));
    }

    // --- Reading position ---

    [Fact]
    public async Task A_visitor_starts_at_the_opening_and_moves_on_only_by_their_own_act()
    {
        var visitor = site.Visitor();
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Sohrab}")).Status);

        var answer = await visitor.ReadTo(2);

        Assert.Equal(HttpStatusCode.Found, answer.Status);
        Assert.Equal($"/en/{Story}#reading", answer.Response.Headers.Location!.OriginalString);
        Assert.Equal("2", visitor.Cookies[$"dastan.reading.{Story}"]);
        var sohrab = await visitor.Get($"/en/{Sohrab}");
        Assert.Equal(HttpStatusCode.OK, sohrab.Status);
        Assert.Contains("A young warrior who seeks his father", sohrab.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Moving_back_takes_the_later_part_away_again()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Sohrab}")).Status);

        await visitor.ReadTo(0);

        Assert.False(visitor.Cookies.ContainsKey($"dastan.reading.{Story}"));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Sohrab}")).Status);
        Assert.DoesNotContain("Sohrab</bdi></a>", (await visitor.Get($"/en/{Rostam}")).Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?level=4")]
    [InlineData("?boundary=4&reading=4")]
    [InlineData("?dastan.reading.story-rostam-sohrab=4")]
    public async Task An_address_cannot_move_a_visitor_forward(string query)
    {
        var page = await site.Visitor().Get($"/en/{Sohrab}{query}");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
    }

    [Fact]
    public async Task Something_not_read_yet_and_something_that_does_not_exist_are_the_same_page()
    {
        var visitor = site.Visitor();

        var hidden = await visitor.Get($"/en/{Sohrab}");
        var unknown = await visitor.Get($"/en/{Story}/entity/character-nobody");

        static string Shape(Page page, string id) => Regex.Replace(page.Html, "name=\"__RequestVerificationToken\" value=\"[^\"]+\"", "token").Replace(id, "ID", StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);
        Assert.Equal(HttpStatusCode.NotFound, unknown.Status);
        Assert.Equal(Shape(unknown, "character-nobody"), Shape(hidden, "character-sohrab"));
    }

    [Fact]
    public async Task The_reading_control_names_the_stages_and_says_only_that_the_story_continues()
    {
        var visitor = site.Visitor();

        var opening = await visitor.Get($"/en/{Story}");
        await visitor.ReadTo(4);
        var end = await visitor.Get($"/en/{Story}");

        Assert.Contains("Stage 1: Opening and orientation", opening.Main, StringComparison.Ordinal);
        Assert.Contains("Stage 5: Complete selected arc", opening.Main, StringComparison.Ordinal);
        Assert.Matches("id=\"stage-0\" value=\"0\" checked", opening.Html);
        Assert.Contains("The story continues beyond this point.", opening.Main, StringComparison.Ordinal);
        Assert.Matches("id=\"stage-4\" value=\"4\" checked", end.Html);
        Assert.Contains("You have reached the end of this collection.", end.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("story-other", "2")]
    [InlineData(Story, "5")]
    [InlineData(Story, "-1")]
    [InlineData(Story, "two")]
    public async Task A_reading_position_the_collection_does_not_have_is_refused(string collection, string level)
    {
        var visitor = site.Visitor();

        var answer = await visitor.Post("/preferences/reading", $"/en/{Story}", ("collection", collection), ("level", level), ("returnUrl", "/en/"));

        Assert.Equal(HttpStatusCode.BadRequest, answer.Status);
        Assert.DoesNotContain(visitor.Cookies.Keys, name => name.StartsWith("dastan.reading", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_form_from_another_site_changes_nothing()
    {
        var visitor = site.Visitor();
        await visitor.Open($"/en/{Story}");

        var page = await visitor.Get($"/en/{Story}");
        var forged = await new Browser(site.CreateClient(), null).Post("/preferences/reading", "/en/", ("collection", Story), ("level", "4"), ("returnUrl", "/en/"));

        // A token of one visitor is of no use with the cookies of another – and without any token nothing happens.
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.NotEqual(HttpStatusCode.Found, forged.Status);
    }

    [Fact]
    public async Task After_the_reading_form_a_visitor_stays_on_this_site()
    {
        var visitor = site.Visitor();

        var answer = await visitor.Post("/preferences/reading", $"/en/{Story}", ("collection", Story), ("level", "1"), ("returnUrl", "https://evil.example/"));

        Assert.Equal("/", answer.Response.Headers.Location!.OriginalString);
    }

    // --- Two visitors (M0-09) ---

    [Fact]
    public async Task Two_visitors_keep_their_own_language_reading_position_and_appearance()
    {
        var reader = site.Visitor("fa-IR,fa;q=0.9");
        var newcomer = site.Visitor("en-GB,en;q=0.9");
        await reader.ReadTo(2, "fa");
        await reader.Post("/preferences/theme", $"/fa/{Story}", ("theme", "dark"), ("returnUrl", "/fa/"));

        // The two ask at the same time, again and again; neither may ever get the other's page.
        var rounds = await Task.WhenAll(Enumerable.Range(0, 25).Select(async _ =>
        {
            var readers = reader.Get($"/fa/{Sohrab}");
            var newcomers = newcomer.Get($"/en/{Sohrab}");
            var starts = newcomer.Get("/");
            return (Reader: await readers, Newcomer: await newcomers, Start: await starts);
        }));

        foreach (var round in rounds)
        {
            Assert.Equal(HttpStatusCode.OK, round.Reader.Status);
            Assert.Equal(("fa", "rtl", "dark"), (round.Reader.Root("lang"), round.Reader.Root("dir"), round.Reader.Root("data-theme")));
            Assert.Contains("پهلوانی جوان که در پی پدر است", round.Reader.Main, StringComparison.Ordinal);

            Assert.Equal(HttpStatusCode.NotFound, round.Newcomer.Status);
            Assert.Equal(("en", "ltr", null), (round.Newcomer.Root("lang"), round.Newcomer.Root("dir"), round.Newcomer.Root("data-theme")));
            Assert.Contains("Not here", round.Newcomer.Main, StringComparison.Ordinal);
            Assert.DoesNotContain("سهراب", round.Newcomer.Text.Replace("رستم و سهراب", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
            Assert.Equal("/en/", round.Start.Response.Headers.Location!.OriginalString);
        }

        Assert.Equal("/fa/", (await reader.Get("/")).Response.Headers.Location!.OriginalString);
        Assert.DoesNotContain(newcomer.Cookies.Keys, name => name.StartsWith("dastan.reading", StringComparison.Ordinal));
    }

    // --- Nothing of a later stage on any page (M0-06) ---

    [Theory]
    [InlineData(0, "en")]
    [InlineData(0, "fa")]
    [InlineData(1, "en")]
    [InlineData(2, "fa")]
    [InlineData(3, "en")]
    [InlineData(4, "fa")]
    public async Task No_page_a_visitor_can_reach_shows_anything_of_a_later_stage(int level, string language)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level, language);
        var projection = Projection.Of(site.Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

        var pages = await Crawl(visitor, $"/{language}/");

        // Every link of the site leads somewhere, and every entity that the pages of this phase lead to has its
        // page: the subjects of the entry, the work, and whoever a relation connects them to.
        Assert.All(pages, page => Assert.Equal(HttpStatusCode.OK, page.Value.Status));
        Assert.Contains($"/{language}/{Story}", pages.Keys);
        Assert.Equal(
            Reachable(projection).Order(StringComparer.Ordinal),
            pages.Keys.Where(address => Regex.IsMatch(address, "/entity/[^/]+$")).Select(address => address.Split('/')[^1]).Order(StringComparer.Ordinal));

        // A secret is a text that belongs only to hidden records. One that also stands inside a visible text – the
        // name Sohrab in the title "Rostam and Sohrab" – is the editors' wording and cannot be searched for.
        var shown = Disclosure.Of(projection, site.Catalogue);
        var secrets = Disclosure.Secrets(projection, site.Catalogue)
            .Where(secret => secret.Length > 2 && !shown.Any(text => text.Contains(secret, StringComparison.Ordinal)))

            // The type of a file ("image/svg+xml") is the same for a hidden file and for the icon of the site.
            .Where(secret => !Regex.IsMatch(secret, "^[a-z]+/[a-z0-9.+-]+$"))
            .ToList();
        Assert.Equal(level < 4, secrets.Contains("event-08"));
        foreach (var (address, page) in pages)
        {
            var html = WebUtility.HtmlDecode(page.Html);
            Assert.All(secrets, secret => Assert.False(html.Contains(secret, StringComparison.Ordinal), $"{address} shows \"{secret}\"."));
        }
    }

    // --- What a page is made of ---

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab")]
    [InlineData("/en/story-rostam-sohrab/entity/character-rostam")]
    [InlineData("/en/story-rostam-sohrab/entity/character-nobody")]
    public async Task A_page_needs_no_script_and_no_inline_style_and_says_what_it_may_do(string address)
    {
        var page = await site.Visitor().Get(address);

        Assert.DoesNotContain("<script", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", page.Html);
        Assert.Contains("default-src 'self'", Assert.Single(page.Response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
        Assert.Equal("nosniff", Assert.Single(page.Response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Matches("<a class=\"skip-link\" href=\"[^\"]*#content\"", page.Html);
        Assert.Single(Regex.Matches(page.Html, "<h1"));
        Assert.Single(Regex.Matches(page.Html, "<main id=\"content\""));
    }

    [Fact]
    public async Task The_style_sheets_and_the_icon_of_a_page_are_served()
    {
        var visitor = site.Visitor();
        var page = await visitor.Get("/fa/");

        var files = Regex.Matches(page.Html, "<link[^>]*href=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();

        Assert.Equal(3, files.Count);
        foreach (var file in files)
        {
            Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/fa/{file}")).Status);
        }
    }

    [Fact]
    public async Task A_local_demonstration_says_that_it_is_one()
    {
        var page = await site.Visitor().Get("/en/");

        Assert.Contains("Local demonstration.", page.Text, StringComparison.Ordinal);
    }

    // --- The editorial input is not a web asset (M0-12) ---

    [Theory]
    [InlineData("/content/samples/shahnameh-pilot.json")]
    [InlineData("/en/content/samples/shahnameh-pilot.json")]
    [InlineData("/samples/shahnameh-pilot.json")]
    [InlineData("/shahnameh-pilot.fa.json")]
    [InlineData("/media/dastan-family-connections.png")]
    [InlineData("/content/media/dastan-parentage-explainer.mp4")]
    [InlineData("/en/media/dastan-parentage-transcript.md")]
    [InlineData("/cross-domain-fixtures.json")]
    public async Task The_collection_its_media_and_the_fixtures_cannot_be_downloaded(string address)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var page = await visitor.Get(address);

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("schemaVersion", page.Html, StringComparison.Ordinal);
    }

    /// <summary>Returns the entities the pages of this phase lead to: from the entry of the collection along the relations.</summary>
    private static HashSet<string> Reachable(Projection projection)
    {
        var overview = projection.Overview();
        var reached = new HashSet<string>(StringComparer.Ordinal);
        var waiting = new Queue<EntityId>(overview.Subjects.Select(entity => entity.Id).Append(overview.Work.Id));
        while (waiting.TryDequeue(out var id))
        {
            if (reached.Add(id.Value))
            {
                foreach (var connection in projection.Entity(id)!.Connections)
                {
                    waiting.Enqueue(connection.Other.Id);
                }
            }
        }

        return reached;
    }

    /// <summary>Follows every link of the site from a start page and returns every page by its address.</summary>
    private static async Task<Dictionary<string, Page>> Crawl(Browser visitor, string start)
    {
        var pages = new Dictionary<string, Page>(StringComparer.Ordinal);
        var waiting = new Queue<string>([start]);
        while (waiting.TryDequeue(out var address))
        {
            if (pages.ContainsKey(address))
            {
                continue;
            }

            var page = await visitor.Get(address);
            pages[address] = page;
            foreach (var link in page.Links)
            {
                // Links are relative to the language of the page; other sites and the other language are left alone.
                if (link.Contains(':', StringComparison.Ordinal) || link.StartsWith('/'))
                {
                    continue;
                }

                var target = new Uri(new Uri($"http://localhost{start}"), link).AbsolutePath;
                if (!pages.ContainsKey(target))
                {
                    waiting.Enqueue(target);
                }
            }
        }

        return pages;
    }
}

/// <summary>The site as the public gets it: the sample is not approved for the public, so nothing of it is shown.</summary>
public sealed class PublicSiteTests(PublicSite site) : IClassFixture<PublicSite>
{
    [Theory]
    [InlineData("en", "Nothing is published here yet.")]
    [InlineData("fa", "هنوز چیزی این‌جا منتشر نشده است.")]
    public async Task The_public_sees_the_site_and_nothing_of_the_sample(string language, string expected)
    {
        var page = await site.Visitor().Get($"/{language}/");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(expected, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Rostam", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("رستم", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Local demonstration", page.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/story-rostam-sohrab")]
    [InlineData("/en/story-rostam-sohrab/entity/character-rostam")]
    [InlineData("/fa/story-rostam-sohrab/entity/work-shahnameh")]
    [InlineData("/en/story-rostam-sohrab/story")]
    [InlineData("/en/story-rostam-sohrab/entity/character-rostam/connections")]
    [InlineData("/fa/story-rostam-sohrab/entity/character-rostam/family")]
    public async Task The_public_cannot_open_a_page_of_the_sample(string address)
    {
        var page = await site.Visitor().Get(address);

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("Rostam", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_public_cannot_set_a_reading_position_that_shows_the_sample()
    {
        var visitor = site.Visitor();
        await visitor.Post("/preferences/reading", "/en/", ("collection", "story-rostam-sohrab"), ("level", "4"), ("returnUrl", "/en/"));

        var page = await visitor.Get("/en/story-rostam-sohrab/entity/character-sohrab");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
    }
}
