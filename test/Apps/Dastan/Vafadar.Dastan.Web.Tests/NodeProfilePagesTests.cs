using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The profile of a node on the site (step 19.5, ADR 0017): the page of an entity shows the facts its kind
/// needs, each stated or said to be unknown, and its picture – or a drawing of its kind; a card in a list
/// shows the same in small; and nothing of it needs a script.
/// </summary>
public sealed class NodeProfilePagesTests(DemoSite site, PublishedSite published) : IClassFixture<DemoSite>, IClassFixture<PublishedSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Diagram = PublishedSite.PublicDiagram;

    private static string Profile(Page page)
    {
        var from = page.Html.IndexOf("<section class=\"profile\"", StringComparison.Ordinal);
        Assert.True(from >= 0, "The page has no profile.");
        return WebUtility.HtmlDecode(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)]);
    }

    // The head of the page: what it is, its picture or the drawing of its kind, its name (step 20.9).
    private static string Head(Page page)
    {
        var from = page.Html.IndexOf("<header class=\"identity ", StringComparison.Ordinal);
        Assert.True(from >= 0, "The page has no head of an identity.");
        return WebUtility.HtmlDecode(page.Html[from..page.Html.IndexOf("</header>", from, StringComparison.Ordinal)]);
    }

    [Theory]
    [InlineData("en", "Profile", "In the story", "A warrior")]
    [InlineData("fa", "نمایه", "در داستان", "پهلوان")]
    public async Task The_page_of_an_entity_shows_the_facts_of_its_kind(string language, string title, string fact, string words)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/character-rostam");
        var profile = Profile(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"<h2 id=\"profile\">{title}</h2>", profile, StringComparison.Ordinal);
        Assert.Contains($"<dt>{fact}</dt>", profile, StringComparison.Ordinal);
        Assert.Contains(words, profile, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Not known", "No picture yet")]
    [InlineData("fa", "دانسته نیست", "هنوز تصویری نیست")]
    public async Task An_entity_without_a_picture_or_a_fact_says_so(string language, string unknown, string noPicture)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/character-zal");
        var profile = Profile(page);

        Assert.Contains($"<dd class=\"unknown\">{unknown}</dd>", profile, StringComparison.Ordinal);
        Assert.Contains(noPicture, profile, StringComparison.Ordinal);

        // A drawing of the kind stands in the place of the picture: decoration, with nothing to read out.
        Assert.Contains("<svg class=\"kind-drawing\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"", Head(page), StringComparison.Ordinal);
        Assert.DoesNotContain("<img", Head(page), StringComparison.Ordinal);

        // A fact that may be added and is not stated is not listed at all.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(profile, "<dt>"));
    }

    [Fact]
    public async Task A_fact_that_names_another_entity_leads_to_it()
    {
        var profile = Profile(await site.Visitor().Get($"/en/{Story}/entity/character-rakhsh"));

        Assert.Contains("<dt>Companion of</dt>", profile, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"{Story}/entity/character-rostam\">", profile, StringComparison.Ordinal);
        Assert.Contains("Rostam's horse", profile, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_list_of_a_collection_shows_every_subject_as_its_card()
    {
        var page = await site.Visitor().Get($"/en/{Story}");
        var html = WebUtility.HtmlDecode(page.Html);
        var subjects = html[html.IndexOf("id=\"subjects\"", StringComparison.Ordinal)..];

        Assert.Contains("class=\"profile-picture small\"", subjects, StringComparison.Ordinal);
        Assert.Matches($"<h3><a href=\"{Story}/entity/[a-z-]+\">", subjects);
        Assert.Contains("class=\"node-card\"", subjects, StringComparison.Ordinal);

        // A card says what is stated and nothing about what is not: "not known" belongs to the page.
        Assert.DoesNotContain("Not known", subjects, StringComparison.Ordinal);
        Assert.Contains("kind-drawing", subjects, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_picture_is_shown_from_the_stage_its_media_item_is_told_at_and_not_before()
    {
        // Rostam is told of from the first stage; the diagram that is his picture here from the third.
        var early = published.Visitor();
        var before = await early.Get($"/en/{Story}/entity/character-rostam");

        Assert.Contains("kind-drawing", Head(before), StringComparison.Ordinal);
        Assert.Contains("No picture yet", Profile(before), StringComparison.Ordinal);
        Assert.DoesNotContain(Diagram, before.Html, StringComparison.Ordinal);

        var late = published.Visitor();
        await late.ReadTo(2);
        var after = await late.Get($"/en/{Story}/entity/character-rostam");
        var (shown, profile) = (Head(after), Profile(after));

        // The picture is the media item of the record, described by its own words, and leads to its page.
        Assert.Contains($"<img src=\"/media/{Story}/{Diagram}/file\"", shown, StringComparison.Ordinal);
        Assert.DoesNotContain("alt=\"\"", shown, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/media/{Diagram}\"", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("kind-drawing", shown, StringComparison.Ordinal);

        // On a card the picture has no words of its own: the name beside it says who it is.
        var list = WebUtility.HtmlDecode((await late.Get($"/en/{Story}")).Html);
        Assert.Contains($"<img src=\"/media/{Story}/{Diagram}/file\"", list, StringComparison.Ordinal);
        Assert.Contains("alt=\"\"", list[list.IndexOf("id=\"subjects\"", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_picture_that_is_not_cleared_for_the_public_is_not_shown_to_the_public()
    {
        var visitor = published.Visitor();
        await visitor.ReadTo(4);

        var page = await visitor.Get($"/en/{Story}/entity/character-rakhsh");
        var profile = Profile(page);

        Assert.Contains("kind-drawing", Head(page), StringComparison.Ordinal);
        Assert.DoesNotContain("<img", Head(page), StringComparison.Ordinal);
        Assert.Contains("No picture yet", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("media-met-452419", profile, StringComparison.Ordinal);
    }
}
