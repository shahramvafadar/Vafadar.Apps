using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The profile of a node as a page (step 20.9): one head for every kind of thing that says at the first glance
/// what it is, the two things a visitor does first, what shows it and where the telling has to do with it as
/// cards, and everything else folded until asked for – in the page, and never past what was read.
/// </summary>
public sealed class ProfilePageTests(WorldSite site) : IClassFixture<WorldSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"{Story}/entity/character-rostam";

    private async Task<Browser> Reader(int stage = 4, string language = "en")
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(stage, language);
        return visitor;
    }

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    private static string Head(Page page) => Regex.Match(page.Main, "<header class=\"identity .*?</header>", RegexOptions.Singleline).Value;

    private static string Fold(Page page) => Regex.Match(page.Main, "<details class=\"fold\".*?</details>", RegexOptions.Singleline).Value;

    private static string InStory(Page page) => Regex.Match(page.Main, "<section class=\"in-story\".*?</section>", RegexOptions.Singleline).Value;

    [Theory]
    [InlineData("en", "character-rostam", "story", "Of a story", "Explore the connections")]
    [InlineData("fa", "character-rostam", "story", "از یک قصه", "پیوندها را بکاو")]
    [InlineData("en", "person-poet", "world", "Of the world", "Explore the connections")]
    [InlineData("fa", "place-city", "world", "از جهان", "پیوندها را بکاو")]
    public async Task The_head_says_what_kind_of_thing_it_is_and_offers_the_two_things_done_first(string language, string id, string flavour, string mark, string explore)
    {
        var page = await (await Reader(language: language)).Get($"/{language}/{Story}/entity/{id}");
        var head = Head(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.StartsWith($"<header class=\"identity is-{flavour}\">", head, StringComparison.Ordinal);

        // What it is, in words and with a drawing that says nothing to a screen reader; then the one title of the page.
        Assert.Matches($"<p class=\"identity-mark mark-{flavour}\"><svg class=\"icon\"[^>]*aria-hidden=\"true\"[^>]*>(?s:.*?)</svg><span>{mark}</span></p>", head);
        Assert.Single(Regex.Matches(page.Main, "<h1[ >]"));
        Assert.True(head.IndexOf("identity-mark", StringComparison.Ordinal) < head.IndexOf("<h1", StringComparison.Ordinal));

        // The picture or the drawing of the kind, exploring and keeping stand in the head.
        Assert.Contains("class=\"profile-picture", head, StringComparison.Ordinal);
        Assert.Contains($"<a class=\"action\" href=\"{Story}/entity/{id}/connections\">{explore}</a>", head, StringComparison.Ordinal);
        Assert.Contains("name=\"kind\" value=\"entity\"", head, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_shows_it_and_where_the_telling_has_to_do_with_it_stand_as_cards_as_far_as_was_read()
    {
        var late = await (await Reader()).Get($"/en/{Rostam}");
        var early = await (await Reader(stage: 0)).Get($"/en/{Rostam}");
        var (all, some) = (InStory(late), InStory(early));

        Assert.Contains("<h2 id=\"in-story\">In story and picture</h2>", all, StringComparison.Ordinal);
        var steps = Regex.Matches(all, "<li class=\"card step-card\">").Count;
        Assert.True(steps > Regex.Matches(some, "<li class=\"card step-card\">").Count);
        Assert.True(Regex.Matches(some, "<li class=\"card step-card\">").Count > 0);

        // Each card of a step leads to the step and to what supports the statement.
        Assert.Equal(steps, Regex.Matches(all, "<a class=\"evidence-link\"").Count);
        Assert.Equal(steps + Regex.Matches(all, "<li class=\"card media-card\">").Count, Regex.Matches(all, "<h3>").Count);

        // Pictures stand first, at most three of them, and the rest is one step away – only where there are any.
        var pictures = Regex.Matches(all, "<li class=\"card media-card\">").Count;
        Assert.InRange(pictures, 1, 3);
        Assert.True(all.IndexOf("media-card", StringComparison.Ordinal) < all.IndexOf("step-card", StringComparison.Ordinal));
        Assert.Contains($"<a class=\"action secondary\" href=\"{Rostam}/gallery\">All its pictures</a>", all, StringComparison.Ordinal);
        Assert.DoesNotContain("media-card", some, StringComparison.Ordinal);

        // Nothing of what was not read: the cards name no step and nobody of a later stage (the collection
        // itself is called after both, so the head is not asked).
        Assert.DoesNotContain("Sohrab", Words(some), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Details and sources", "Profile", "What supports this", "Other names", "In other catalogues")]
    [InlineData("fa", "جزئیات و منبع‌ها", "نمایه", null, "نام‌های دیگر", "در فهرست‌های دیگر")]
    public async Task Details_other_names_and_sources_are_folded_until_asked_for_and_are_in_the_page(string language, string summary, string profile, string? evidence, string names, string catalogues)
    {
        var page = await (await Reader(language: language)).Get($"/{language}/{Story}/entity/person-poet");
        var fold = Fold(page);

        // Closed as the page comes, and opened by the browser itself: no script is asked.
        Assert.StartsWith($"<details class=\"fold\" id=\"details\"><summary>{summary}</summary>", fold, StringComparison.Ordinal);
        Assert.DoesNotContain("<details class=\"fold\" id=\"details\" open", page.Main, StringComparison.Ordinal);

        // In it: every fact of the profile, the other names, the sources, what others call it.
        Assert.Contains($"<h2 id=\"profile\">{profile}</h2>", fold, StringComparison.Ordinal);
        Assert.Contains(names, Words(fold), StringComparison.Ordinal);
        Assert.Contains(catalogues, Words(fold), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/evidence/entity/person-poet?", fold, StringComparison.Ordinal);
        if (evidence is not null)
        {
            Assert.Contains(evidence, Words(fold), StringComparison.Ordinal);
        }

        // And not before it: the head carries the facts of a card, not the list of names or catalogues.
        var before = page.Main[..page.Main.IndexOf("<details class=\"fold\"", StringComparison.Ordinal)];
        Assert.DoesNotContain(names, Words(before), StringComparison.Ordinal);
        Assert.DoesNotContain(catalogues, Words(before), StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"profile\"", before, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_head_the_card_and_the_inspector_are_fed_by_the_one_profile()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"/en/{Rostam}");
        var data = System.Text.Json.JsonDocument.Parse((await visitor.Get($"/en/{Rostam}/profile.json")).Html).RootElement;

        // The name, the kind and the line of the head are those the inspector is given.
        var head = Words(Head(page));
        Assert.Contains(data.GetProperty("name").GetString()!, head, StringComparison.Ordinal);
        Assert.Contains(data.GetProperty("kind").GetString()!, head, StringComparison.Ordinal);
        Assert.Contains(data.GetProperty("summary").GetProperty("text").GetString()!, head, StringComparison.Ordinal);
    }
}
