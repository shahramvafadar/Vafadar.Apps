using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Where a visitor begins (step 20.7): a search, what the site has to begin with, the names to start with as
/// cards, and a real piece of a drawing – all of it from the collection, as far as the visitor has read, and
/// nothing that needs an account.
/// </summary>
public sealed class DiscoverPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static string Section(Page page, string id)
    {
        var from = page.Main.IndexOf($"aria-labelledby=\"{id}\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : page.Main[from..page.Main.IndexOf("</section>", from, StringComparison.Ordinal)];
    }

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    [Theory]
    [InlineData("en", "A first story", "Start with", "Connections of Rostam", "Explore these connections")]
    [InlineData("fa", "نخستین قصه", "از این‌ها شروع کنید", "پیوندهای رستم", "این پیوندها را بکاوید")]
    public async Task The_start_has_a_search_a_story_names_to_start_with_and_a_piece_of_a_drawing(string language, string first, string start, string drawing, string explore)
    {
        var page = await site.Visitor().Get($"/{language}/");

        Assert.Equal(HttpStatusCode.OK, page.Status);

        // The search stands in the head of the page, before anything else.
        var hero = Regex.Match(page.Main, "<section class=\"hero\">.*?</section>", RegexOptions.Singleline).Value;
        Assert.Contains("role=\"search\"", hero, StringComparison.Ordinal);
        Assert.Contains("type=\"search\"", hero, StringComparison.Ordinal);

        // The story, with the drawing of its kind where it has no picture, and the way into it.
        var story = Section(page, "first-story");
        Assert.Contains(first, Words(story), StringComparison.Ordinal);
        Assert.Contains("class=\"card feature\"", story, StringComparison.Ordinal);
        Assert.Contains("class=\"kind-drawing\"", story, StringComparison.Ordinal);
        Assert.Contains($"<a class=\"action\" href=\"{Story}\">", story, StringComparison.Ordinal);

        // The names to start with: cards with the picture above the words.
        var names = Section(page, "start-with");
        Assert.Contains(start, Words(names), StringComparison.Ordinal);
        Assert.InRange(Regex.Matches(names, "class=\"node-card tall\"").Count, 1, 6);
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", names, StringComparison.Ordinal);

        // A piece of a drawing that is a drawing: the boxes of the page of the connections, with their links.
        var piece = Section(page, "a-drawing");
        Assert.Contains(drawing, Words(piece), StringComparison.Ordinal);
        Assert.Contains("<svg class=\"graph flow\"", piece, StringComparison.Ordinal);
        Assert.Contains("class=\"drawing-peek\" tabindex=\"0\" role=\"group\"", piece, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/character-rostam/connections\"", piece, StringComparison.Ordinal);
        Assert.Contains(explore, Words(piece), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_start_shows_nothing_the_visitor_has_not_read_to()
    {
        var early = site.Visitor();
        await early.ReadTo(0);
        var late = site.Visitor();
        await late.ReadTo(4);

        var (before, after) = (await early.Get("/en/"), await late.Get("/en/"));

        // Sohrab is told of later: neither a card nor a box of the drawing names him to who has read nothing.
        Assert.DoesNotContain("Sohrab</a>", Section(before, "start-with") + Section(before, "a-drawing"), StringComparison.Ordinal);
        Assert.DoesNotContain("character-sohrab", before.Main, StringComparison.Ordinal);
        Assert.Contains("character-sohrab", after.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nothing_at_the_start_needs_an_account_and_every_link_of_it_opens()
    {
        var visitor = site.Visitor();
        var page = await visitor.Get("/en/");

        Assert.DoesNotContain("href=\"account", page.Main, StringComparison.Ordinal);
        var links = Regex.Matches(page.Main, "<a [^>]*href=\"([^\"#]+)\"").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).Distinct().ToList();
        Assert.NotEmpty(links);
        foreach (var link in links)
        {
            Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{link}")).Status);
        }
    }

    [Fact]
    public async Task A_site_with_several_collections_searches_all_of_them_from_the_start()
    {
        using var two = new TwoCollectionsSite();
        var page = await two.Visitor().Get("/en/");

        var hero = Regex.Match(page.Main, "<section class=\"hero\">.*?</section>", RegexOptions.Singleline).Value;
        Assert.Contains("action=\"search\"", hero, StringComparison.Ordinal);

        // Each collection is a feature; only the one the site begins with shows its picture or drawing here.
        var features = Section(page, "first-story");
        Assert.Equal(3, Regex.Matches(features, "<article class=\"card feature\">").Count);
        Assert.Single(Regex.Matches(features, "class=\"profile-picture"));
    }
}
