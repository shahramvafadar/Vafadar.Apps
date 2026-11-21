using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The address that gives the browser the profile of an entity (step 19.7, ADR 0019): what it answers is what
/// the page of the same visitor shows – the same facts in the same words, the same picture, at the stage the
/// visitor has read to and for the audience of the site – and what the page does not show, the address does
/// not give. Its answer is kept by nothing.
/// </summary>
public sealed class ProfileDataTests(DemoSite site, PublishedSite published) : IClassFixture<DemoSite>, IClassFixture<PublishedSite>
{
    private const string Story = "story-rostam-sohrab";
    private static readonly string[] Characters = ["character-rostam", "character-zal", "character-tahmineh", "character-sohrab", "character-rakhsh", "character-king-samangan", "character-afrasiab", "character-kay-kavus"];

    private static string Address(string language, string entity) => $"/{language}/{Story}/entity/{entity}";

    private static async Task<JsonNode> Data(Browser visitor, string language, string entity)
    {
        var answer = await visitor.Get(Address(language, entity) + "/profile.json");
        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("application/json", answer.Response.Content.Headers.ContentType?.MediaType);
        return JsonNode.Parse(answer.Html)!;
    }

    // The section of the profile of a page, as a reader reads it: its words, in order.
    private static string Section(Page page)
    {
        var from = page.Html.IndexOf("<section class=\"profile\"", StringComparison.Ordinal);
        Assert.True(from >= 0, "The page has no profile.");
        return WebUtility.HtmlDecode(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)]);
    }

    private static string Read(string html) => Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), "\\s+", " ").Trim();

    [Theory]
    [InlineData("en", "character-rostam")]
    [InlineData("fa", "character-rostam")]
    [InlineData("en", "character-rakhsh")]
    [InlineData("fa", "character-rakhsh")]
    [InlineData("en", "character-zal")]
    [InlineData("fa", "character-zal")]
    public async Task The_address_gives_what_the_page_of_the_same_visitor_shows(string language, string entity)
    {
        var visitor = site.Visitor();
        var page = await visitor.Get(Address(language, entity));
        var data = await Data(visitor, language, entity);
        var section = Section(page);
        var read = Read(section);

        // The name and the kind of the page, and where the page is.
        Assert.Contains($">{(string)data["name"]!}</", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains((string)data["kind"]!, WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Equal($"{Story}/entity/{entity}", (string?)data["address"]);

        // Every fact of the page, with every value, in the order of the page – and no fact the page has not.
        var facts = data["facts"]!.AsArray();
        Assert.Equal(Regex.Matches(section, "<dt>").Count, facts.Count);
        Assert.Equal(Regex.Matches(section, "<dd").Count, facts.Sum(fact => fact!["values"]!.AsArray().Count));
        var written = string.Join(" ", facts.Select(fact => $"{(string)fact!["label"]!} {string.Join(" ", fact["values"]!.AsArray().Select(value => (string)value!["text"]!))}"));
        Assert.Contains(written, read, StringComparison.Ordinal);

        // What is not known is said to be not known in both, and a value that names a node leads where the page leads.
        Assert.Equal(Regex.Matches(section, "<dd class=\"unknown\">").Count, facts.Sum(fact => fact!["values"]!.AsArray().Count(value => (bool?)value!["unknown"] == true)));
        foreach (var value in facts.SelectMany(fact => fact!["values"]!.AsArray()).Where(value => value!["address"] is not null))
        {
            Assert.Contains($"<a href=\"{(string)value!["address"]!}\">", section, StringComparison.Ordinal);
        }

        // The sample has no picture that may be shown: both say so with the same words.
        Assert.Null(data["picture"]);
        Assert.Contains((string)data["noPicture"]!, read, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_text_that_has_no_translation_says_its_language_like_the_page_does()
    {
        var visitor = site.Visitor();
        var data = await Data(visitor, "fa", "character-rostam");
        var page = WebUtility.HtmlDecode((await visitor.Get(Address("fa", "character-rostam"))).Html);

        // Each text is in the language the page shows it in; where that is not the language of the page, both say which.
        foreach (var words in data["facts"]!.AsArray().SelectMany(fact => fact!["values"]!.AsArray()).Append(data["summary"]).Where(words => words is not null))
        {
            var foreign = page.Contains($"<bdi lang=\"en\" dir=\"ltr\">{(string)words!["text"]!}</bdi>", StringComparison.Ordinal);
            Assert.Equal(foreign ? "en" : null, (string?)words["language"]);
        }
    }

    [Fact]
    public async Task What_the_visitor_has_not_read_to_is_not_found_at_the_address_as_on_the_page()
    {
        var visitor = published.Visitor();
        var statuses = new List<(HttpStatusCode Page, HttpStatusCode Data)>();
        foreach (var character in Characters)
        {
            statuses.Add(((await visitor.Get(Address("en", character))).Status, (await visitor.Get(Address("en", character) + "/profile.json")).Status));
        }

        // Entity by entity the same answer – and the story has both: somebody told of at the start, somebody not yet.
        Assert.All(statuses, status => Assert.Equal(status.Page, status.Data));
        Assert.Contains(statuses, status => status.Data == HttpStatusCode.OK);
        Assert.Contains(statuses, status => status.Data == HttpStatusCode.NotFound);

        // Read to the end, everybody is there in both.
        await visitor.ReadTo(4);
        foreach (var character in Characters)
        {
            Assert.Equal((await visitor.Get(Address("en", character))).Status, (await visitor.Get(Address("en", character) + "/profile.json")).Status);
        }

        // What is not there is the status and no page; an address that names no entity and another collection too.
        var missing = await published.Visitor().Get(Address("en", "nobody-at-all") + "/profile.json");
        Assert.Equal((HttpStatusCode.NotFound, string.Empty), (missing.Status, missing.Html));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/no-such-story/entity/character-rostam/profile.json")).Status);
    }

    [Fact]
    public async Task The_picture_is_given_from_the_stage_and_for_the_audience_the_page_shows_it()
    {
        // Rostam is told of from the first stage; the diagram that is his picture here from the third.
        var visitor = published.Visitor();
        Assert.Null((await Data(visitor, "en", "character-rostam"))["picture"]);

        await visitor.ReadTo(2);
        var picture = (await Data(visitor, "en", "character-rostam"))["picture"]!;
        // The picture stands in the head of the page (step 20.9).
        var section = WebUtility.HtmlDecode((await visitor.Get(Address("en", "character-rostam"))).Main);

        Assert.Equal($"/media/{Story}/{PublishedSite.PublicDiagram}/file", (string?)picture["source"]);
        Assert.Contains($"<img src=\"{(string)picture["source"]!}\"", section, StringComparison.Ordinal);
        Assert.Contains($"alt=\"{(string)picture["words"]!}\"", section, StringComparison.Ordinal);

        // A picture that is not cleared for the public is not given to the public either.
        await visitor.ReadTo(4);
        Assert.Null((await Data(visitor, "en", "character-rakhsh"))["picture"]);
        Assert.DoesNotContain("media-met-452419", (await visitor.Get(Address("en", "character-rakhsh") + "/profile.json")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_answer_is_kept_by_nothing_and_a_card_offers_it_only_through_the_script()
    {
        var visitor = site.Visitor();
        var answer = await visitor.Get(Address("en", "character-rostam") + "/profile.json");

        Assert.Equal("no-store", answer.Response.Headers.CacheControl?.ToString());
        Assert.Contains("Cookie", answer.Response.Headers.Vary);
        Assert.Equal("noindex", string.Join(",", answer.Response.Headers.GetValues("X-Robots-Tag")).Split(',')[0].Trim());
        Assert.False(answer.Response.Headers.Contains("Dastan-Keep"));

        // The list of the collection offers the look hidden: a page without the script shows the name and its link.
        var list = (await visitor.Get($"/en/{Story}")).Html;
        Assert.Matches("<p class=\"peek\" data-profile-peek hidden>\\s*<button type=\"button\" data-profile=\"story-rostam-sohrab/entity/[a-z-]+/profile.json\"", list);
        Assert.Contains("<dialog id=\"profile-panel\" class=\"profile-panel\" aria-labelledby=\"profile-panel-name\">", list, StringComparison.Ordinal);

        // The service worker keeps pages and what a page is drawn with – and names no address of data.
        var worker = (await visitor.Get("/service-worker.js")).Html;
        Assert.DoesNotContain("profile.json", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("'json'", worker, StringComparison.Ordinal);
        Assert.Contains("// Anything else is not touched.", worker, StringComparison.Ordinal);
    }
}
