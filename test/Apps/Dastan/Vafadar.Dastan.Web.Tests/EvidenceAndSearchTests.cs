using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>What supports a statement, the page before a source of another site, and the search.</summary>
public sealed class EvidenceAndSearchTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private Projection At(int level) => Projection.Of(site.Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    private static List<string> Sources(Page page) =>
        [.. Regex.Matches(page.Html, $"href=\"{Story}/source/([^\"?]+)").Select(match => match.Groups[1].Value)];

    private static string Encoded(string address) => Uri.EscapeDataString(address);

    // --- Evidence ---

    [Fact]
    public async Task The_evidence_of_a_relation_restates_it_and_lists_its_own_sources()
    {
        var page = await site.Visitor().Get($"/en/{Story}/evidence/relation/relation-02");
        var statement = Regex.Match(page.Html, "<section class=\"card evidence-statement\".*?</section>", RegexOptions.Singleline).Value;

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>What supports this</h1>", page.Html, StringComparison.Ordinal);
        Assert.Matches("character-zal\"><bdi>Zal</bdi></a>\\s*<span class=\"relation\"><bdi>Father of</bdi></span>\\s*<a href=\"[^\"]*character-rostam\"><bdi>Rostam</bdi></a>", statement);

        // The editors' wording and other people's texts are told apart; the state of the review and the place to
        // look are those of this relation.
        Assert.Contains("The wording on Dastan is the editors'.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Supported by a source; the editors' review is still open", page.Main, StringComparison.Ordinal);
        Assert.Contains("Iranica opening identification; primary lineage declaration is corroborating background.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Ganjoor", page.Main, StringComparison.Ordinal);

        Assert.Equal(["src-iranica-zal", "src-ganjoor-lineage"], Sources(page));
        Assert.Contains("Zal, Encyclopaedia Iranica", page.Main, StringComparison.Ordinal);
        Assert.Contains("Scholarly reference work", page.Main, StringComparison.Ordinal);
        Assert.Contains("Looked up by the editors on 15 November 2026 CE", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task While_the_story_continues_the_evidence_warns_before_its_sources_and_links_to_no_other_site()
    {
        var visitor = site.Visitor();
        var early = await visitor.Get($"/en/{Story}/evidence/relation/relation-02");
        await visitor.ReadTo(4);
        var late = await visitor.Get($"/en/{Story}/evidence/relation/relation-02");

        Assert.Contains("can reveal what you have not read yet", early.Main, StringComparison.Ordinal);
        Assert.True(early.Html.IndexOf("class=\"note warning\"", StringComparison.Ordinal) < early.Html.IndexOf("<ul class=\"sources\"", StringComparison.Ordinal));
        Assert.DoesNotContain("can reveal what you have not read yet", late.Main, StringComparison.Ordinal);
        Assert.Contains("These sources are pages of other sites.", late.Main, StringComparison.Ordinal);

        // A source is reached through the page that says what leaving means, at every stage.
        foreach (var page in new[] { early, late })
        {
            Assert.DoesNotContain(page.Links, link => link.StartsWith("http", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("iranicaonline", page.Html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(0, "en")]
    [InlineData(2, "fa")]
    [InlineData(4, "en")]
    public async Task Every_visible_statement_has_the_sources_of_the_projection_and_no_others(int level, string language)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level, language);
        var projection = At(level);

        var statements = new List<(string Address, EvidenceView Evidence)>();
        statements.AddRange(projection.Entities.Select(entity => ($"entity/{entity.Id.Value}", projection.Evidence(entity.Id)!)));
        statements.AddRange(projection.Relations.Select(relation => ($"relation/{relation.Id.Value}", projection.Evidence(relation.Id)!)));
        statements.AddRange(projection.Media.Select(item => ($"media/{item.Id.Value}", projection.Evidence(item.Id)!)));
        foreach (var step in projection.Steps)
        {
            statements.Add(($"event/{step.Event.Id.Value}", projection.Evidence(step.Event.Id)!));
            statements.AddRange(step.Event.Associations.Select(association =>
                ($"event/{step.Event.Id.Value}/entity/{association.Entity.Value}", projection.Evidence(step.Event.Id, association.Entity)!)));
        }

        Assert.True(statements.Count >= projection.Entities.Length + projection.Relations.Length + projection.Steps.Length);
        foreach (var (address, evidence) in statements)
        {
            var page = await visitor.Get($"/{language}/{Story}/evidence/{address}");

            Assert.True(page.Status == HttpStatusCode.OK, $"{address}: {page.Status}");
            Assert.Equal(evidence.Sources.Select(source => source.Id.Value), Sources(page));
            Assert.Single(Regex.Matches(page.Html, "<h1"));
        }
    }

    [Theory]
    [InlineData("evidence/entity/character-sohrab")]
    [InlineData("evidence/entity/character-nobody")]
    [InlineData("evidence/relation/relation-99")]
    [InlineData("evidence/event/event-08")]
    [InlineData("evidence/event/event-08/entity/character-rostam")]
    [InlineData("evidence/event/event-01/entity/character-sohrab")]
    [InlineData("evidence/event/event-01/entity/character-zal")]
    [InlineData("evidence/media/media-dastan-family-illustration")]
    [InlineData("evidence/media/media-met-453265")]
    [InlineData("source/src-ganjoor-s18")]
    [InlineData("source/src-met-453265")]
    [InlineData("source/src-nothing")]
    [InlineData("source/https:%2F%2Fexample.org")]
    public async Task Evidence_of_what_is_not_read_yet_and_of_what_does_not_exist_is_not_found(string address)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get($"/en/{Story}/{address}")).Status);
    }

    [Fact]
    public async Task A_relation_that_is_not_read_yet_has_no_evidence_until_it_is()
    {
        var hidden = site.Catalogue.Relations.First(relation => relation.Level.Value == 2).Id.Value;
        var visitor = site.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Story}/evidence/relation/{hidden}")).Status);
        await visitor.ReadTo(2);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Story}/evidence/relation/{hidden}")).Status);
    }

    [Fact]
    public async Task Closing_the_evidence_returns_to_the_page_it_was_opened_from_with_what_was_open_there()
    {
        var visitor = site.Visitor();
        var graph = await visitor.Get($"/en/{Story}/entity/character-rostam/connections?open=character-zal");
        var origin = $"{Story}/entity/character-rostam/connections?open=character-zal";

        // The list below the drawing offers the sources of each relation and says where it stands.
        var link = Assert.Single(graph.Links, link => link.StartsWith($"{Story}/evidence/relation/relation-02?", StringComparison.Ordinal));
        Assert.Equal($"{Story}/evidence/relation/relation-02?from={Encoded(origin)}", link);

        var evidence = await visitor.Get($"/en/{link}");
        Assert.Equal(2, evidence.Links.Count(address => address == origin));
        Assert.Contains("Back to where you were", evidence.Main, StringComparison.Ordinal);

        // The page before a source returns to the evidence, which still knows where it was opened.
        var toSource = Assert.Single(evidence.Links, address => address.StartsWith($"{Story}/source/src-iranica-zal?", StringComparison.Ordinal));
        var source = await visitor.Get($"/en/{toSource}");
        Assert.Contains(link, source.Links);
    }

    [Theory]
    [InlineData("https://example.org/")]
    [InlineData("//example.org/")]
    [InlineData("/en/other")]
    [InlineData("another-story/entity/x")]
    [InlineData("story-rostam-sohrab-two/x")]
    [InlineData("story-rostam-sohrab/../x")]
    [InlineData("story-rostam-sohrab/x y")]
    [InlineData("javascript:alert(1)")]
    public async Task A_way_back_that_is_not_a_page_of_the_collection_is_not_followed(string from)
    {
        var visitor = site.Visitor();

        var evidence = await visitor.Get($"/en/{Story}/evidence/relation/relation-02?from={Encoded(from)}");
        var source = await visitor.Get($"/en/{Story}/source/src-iranica-zal?from={Encoded(from)}");

        Assert.Equal(HttpStatusCode.OK, evidence.Status);
        Assert.DoesNotContain(from, evidence.Links);
        Assert.Contains($"{Story}/entity/character-zal", evidence.Links);
        Assert.Contains("Back to Zal", evidence.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(from, source.Links);
        Assert.Contains(Story, source.Links);
    }

    [Fact]
    public async Task The_page_before_a_source_says_what_leaving_means_and_only_then_names_the_other_site()
    {
        var visitor = site.Visitor();
        var early = await visitor.Get($"/en/{Story}/source/src-iranica-zal?from={Encoded($"{Story}/entity/character-zal")}");
        await visitor.ReadTo(4, "fa");
        var late = await visitor.Get($"/fa/{Story}/source/src-iranica-zal");

        Assert.Equal(HttpStatusCode.OK, early.Status);
        Assert.Contains("<h1>You are leaving Dastan</h1>", early.Html, StringComparison.Ordinal);
        Assert.Contains("It can reveal what you have not read yet, and Dastan cannot hide anything there.", early.Main, StringComparison.Ordinal);
        Assert.Contains("Zal, Encyclopaedia Iranica", early.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/entity/character-zal", early.Links);

        var leave = Assert.Single(Regex.Matches(early.Html, "<a class=\"action secondary\" href=\"(https://[^\"]+)\" rel=\"external noopener noreferrer\">Continue to the source</a>"));
        var address = site.Catalogue.Sources.Single(source => source.Id.Value == "src-iranica-zal").Url;
        Assert.Equal(address.AbsoluteUri, WebUtility.HtmlDecode(leave.Groups[1].Value));

        // At the end of the story there is nothing left to give away; the page still says that it is another site.
        Assert.DoesNotContain("فاش", late.Main, StringComparison.Ordinal);
        Assert.Contains("این صفحه‌ای از سایتی دیگر است.", late.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_pages_of_the_journey_lead_to_the_evidence_of_what_they_state()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var collection = await visitor.Get($"/en/{Story}");
        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var story = await visitor.Get($"/en/{Story}/story");
        var family = await visitor.Get($"/en/{Story}/entity/character-sohrab/family");
        var media = await visitor.Get($"/en/{Story}/media/media-dastan-family-illustration");

        Assert.Contains($"{Story}/evidence/entity/{Story}?from={Encoded(Story)}", collection.Links);
        Assert.Contains($"{Story}/evidence/entity/character-rostam?from={Encoded($"{Story}/entity/character-rostam")}", rostam.Links);
        Assert.Contains($"{Story}/evidence/event/event-01/entity/character-rostam?from={Encoded($"{Story}/entity/character-rostam")}", rostam.Links);
        Assert.Equal(
            At(2).Entity(new EntityId("character-rostam"))!.Connections.Length,
            rostam.Links.Count(link => link.StartsWith($"{Story}/evidence/relation/", StringComparison.Ordinal)));

        // A step returns to itself on the page of the story.
        Assert.Contains($"{Story}/evidence/event/event-03?from={Encoded($"{Story}/story#step-3")}", story.Links);
        Assert.Equal(At(2).Steps.Length, story.Links.Count(link => link.StartsWith($"{Story}/evidence/event/", StringComparison.Ordinal)));
        Assert.Equal(5, family.Links.Count(link => link.StartsWith($"{Story}/evidence/relation/", StringComparison.Ordinal)));
        Assert.Contains($"{Story}/evidence/media/media-dastan-family-illustration?from={Encoded($"{Story}/media/media-dastan-family-illustration")}", media.Links);

        // The name of a link says which statement it belongs to.
        Assert.Matches("class=\"evidence-link\"[^>]*>Sources<span class=\"visually-hidden\"> – Zal, Father of, Rostam</span></a>", rostam.Html);
    }

    // --- Search ---

    [Fact]
    public async Task The_search_finds_a_name_by_its_beginning_in_either_language_and_says_which_name_matched()
    {
        var visitor = site.Visitor();

        var english = await visitor.Get($"/en/{Story}/search?q=rost");
        var persian = await visitor.Get($"/en/{Story}/search?q={Encoded("رستم")}");
        var onPersianPage = await visitor.Get($"/fa/{Story}/search?q=Rakhsh");

        Assert.Equal(HttpStatusCode.OK, english.Status);
        Assert.Contains($"{Story}/entity/character-rostam", english.Links);
        Assert.Contains("Found for “rost”:", english.Main, StringComparison.Ordinal);
        Assert.Matches("<input type=\"search\" id=\"search-q\" name=\"q\" value=\"rost\"", english.Html);

        Assert.Contains($"{Story}/entity/character-rostam", persian.Links);
        Assert.Contains("found by the name “رستم”", persian.Main, StringComparison.Ordinal);

        Assert.Contains($"{Story}/entity/character-rakhsh", onPersianPage.Links);
        Assert.Contains("با نام «Rakhsh» یافت شد", onPersianPage.Main, StringComparison.Ordinal);
        Assert.Contains("یافته‌ها برای «Rakhsh»: ۱", onPersianPage.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tahmineh")]
    [InlineData("Afrasiab")]
    [InlineData("zzzz")]
    public async Task A_name_that_is_not_read_yet_is_answered_like_a_name_that_does_not_exist(string name)
    {
        var page = await site.Visitor().Get($"/en/{Story}/search?q={name}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"Nothing found for “{name}” in what you have read so far.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("/entity/", string.Join(' ', page.Links.Where(link => link.Contains("character-", StringComparison.Ordinal))), StringComparison.Ordinal);
        Assert.Empty(Regex.Matches(page.Main, "<li class=\"card\">"));
    }

    [Fact]
    public async Task The_search_finds_what_the_projection_finds_at_the_visitors_stage()
    {
        var visitor = site.Visitor();
        var before = await visitor.Get($"/en/{Story}/search?q=sohrab");
        await visitor.ReadTo(2);
        var after = await visitor.Get($"/en/{Story}/search?q=sohrab");

        foreach (var (page, level) in new[] { (before, 0), (after, 2) })
        {
            Assert.Equal(
                At(level).Search("sohrab", 20).Select(hit => $"{Story}/entity/{hit.Entity.Id.Value}"),
                Regex.Matches(page.Html, "<li class=\"card\">\\s*<h2><a href=\"([^\"]+)\"").Select(match => match.Groups[1].Value));
        }

        Assert.DoesNotContain($"{Story}/entity/character-sohrab", before.Links);
        Assert.Contains($"{Story}/entity/character-sohrab", after.Links);
    }

    [Fact]
    public async Task What_a_visitor_types_is_text_and_is_cut_to_the_length_of_a_name()
    {
        var visitor = site.Visitor();

        var markup = await visitor.Get($"/en/{Story}/search?q={Encoded("<script>alert(1)</script>\"onfocus=\"x")}");
        var longText = await visitor.Get($"/en/{Story}/search?q={new string('a', 500)}");
        var nothing = await visitor.Get($"/en/{Story}/search");

        Assert.Equal(HttpStatusCode.OK, markup.Status);
        Assert.DoesNotContain("<script", markup.Own, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("value=\"[^\"]*\"\\s*onfocus", markup.Html);
        Assert.Contains("Nothing found for “<script>alert(1)</script>\"onfocus=\"x”", markup.Main, StringComparison.Ordinal);

        Assert.Contains($"value=\"{new string('a', 80)}\"", longText.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('a', 81), longText.Main, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.OK, nothing.Status);
        Assert.DoesNotContain("role=\"status\"", nothing.Main, StringComparison.Ordinal);
        Assert.Contains("Names of people, places and things, as far as you have read.", nothing.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab")]
    [InlineData("/en/story-rostam-sohrab/search")]
    public async Task The_search_is_offered_as_a_form_that_asks_for_a_page(string address)
    {
        var page = await site.Visitor().Get(address);

        Assert.Matches($"<form class=\"search\" method=\"get\" action=\"{Story}/search\" role=\"search\">", page.Html);
        Assert.Matches("<label for=\"search-q\">", page.Html);
        Assert.DoesNotContain("<script", page.Own, StringComparison.OrdinalIgnoreCase);
    }
}
