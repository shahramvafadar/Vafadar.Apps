using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The family drawing and the story step by step with its lanes.</summary>
public sealed class FamilyAndStoryPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static string Family(string entity) => $"{Story}/entity/{entity}/family";

    private static string Graph(Page page) => Regex.Match(page.Html, "<svg class=\"graph tree\".*?</svg>", RegexOptions.Singleline).Value;

    // The lines of the drawing: one group per relation, however many bends its line has.
    private static int Lines(Page page) => Regex.Matches(Graph(page), "<g class=\"tree-link\">").Count;

    private static string Choice(Page page, string name) =>
        Regex.Match(page.Html, $"<(a|span) class=\"choice\"[^>]*>{Regex.Escape(name)}</(a|span)>").Value;

    private static string Steps(Page page, string row) =>
        Regex.Match(page.Html, $"<span class=\"chart-tool-name\">{row}</span>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;

    private static List<string> Boxes(Page page) =>
        [.. Regex.Matches(Graph(page), "<a class=\"graph-name\" href=\"[^\"]*/entity/([^/\"]+)\"").Select(match => match.Groups[1].Value)];

    private Projection At(int level) => Projection.Of(site.Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    // --- Family ---

    [Fact]
    public async Task A_family_is_drawn_in_generations_with_a_line_from_each_parent_to_the_child()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Family("character-sohrab")}");
        var graph = Graph(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Family of Sohrab</h1>", page.Html, StringComparison.Ordinal);
        Assert.Equal(["character-zal", "character-king-samangan", "character-rostam", "character-tahmineh", "character-sohrab"], Boxes(page));
        Assert.Equal(5, Lines(page));
        Assert.Equal(4, Regex.Matches(graph, "marker-end=\"url\\(#graph-arrow\\)\"").Count);
        Assert.Single(Regex.Matches(graph, "<line class=\"graph-line graph-partner\""));
        Assert.Matches("class=\"graph-node graph-focus\">\\s*<span class=\"graph-entity\">\\s*<a class=\"graph-name\" href=\"[^\"]*character-sohrab\"", graph);
        Assert.Equal(5, Regex.Matches(page.Html, "<li><span class=\"statement\">").Count);
    }

    [Fact]
    public async Task At_the_opening_the_family_has_only_the_members_known_by_then()
    {
        var page = await site.Visitor().Get($"/en/{Family("character-rostam")}");

        Assert.Equal(["character-zal", "character-rostam"], Boxes(page));
        Assert.DoesNotContain("Sohrab</bdi>", Graph(page), StringComparison.Ordinal);
        Assert.DoesNotContain("Tahmineh", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_family_drawing_and_its_list_show_the_same_relations_for_every_member(string language)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4, language);
        var projection = At(4);

        foreach (var member in projection.Family().Members)
        {
            // The line of the member as the page opens it, and the whole family it belongs to.
            foreach (var (query, family) in new[]
            {
                (string.Empty, projection.Family(member.Entity.Id, 2, 2)!),
                ("?up=1&down=0", projection.Family(member.Entity.Id, 1, 0)!),
                ("?family=whole", projection.Family(member.Entity.Id)!),
                ("?family=whole&view=across", projection.Family(member.Entity.Id)!),
            })
            {
                var page = await visitor.Get($"/{language}/{Family(member.Entity.Id.Value)}{query}");

                Assert.Equal(family.Members.Select(entry => entry.Entity.Id.Value).Order(StringComparer.Ordinal), Boxes(page).Order(StringComparer.Ordinal));
                Assert.Equal(family.Parentage.Length + family.Partnerships.Length, Lines(page));
                Assert.Equal(family.Parentage.Length + family.Partnerships.Length, Regex.Matches(page.Html, "<li><span class=\"statement\">").Count);
            }
        }
    }

    [Fact]
    public async Task The_tree_opens_with_the_line_of_somebody_and_offers_the_whole_family()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var line = await visitor.Get($"/en/{Family("character-zal")}");
        var whole = await visitor.Get($"/en/{Family("character-zal")}?family=whole");

        // The father of Tahmineh is no ancestor and no descendant of Zal: he is in the family, not in the line.
        Assert.Equal(["character-zal", "character-rostam", "character-tahmineh", "character-sohrab"], Boxes(line));
        Assert.Contains("Shown: 2 generations of ancestors and 2 of descendants, with their partners.", line.Main, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", Choice(line, "The line of Zal"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-zal")}?family=whole\"", Choice(line, "The whole family"), StringComparison.Ordinal);

        Assert.Contains("character-king-samangan", Boxes(whole));
        Assert.Equal(5, Boxes(whole).Count);
        Assert.Contains("Shown: everybody who is connected to Zal by parents, children or partners.", whole.Main, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", Choice(whole, "The whole family"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-zal")}\"", Choice(whole, "The line of Zal"), StringComparison.Ordinal);

        // The whole family has no generations to open: the steps are not links there.
        Assert.DoesNotContain("href=", Steps(whole, "Ancestors"), StringComparison.Ordinal);
        Assert.DoesNotContain("href=", Steps(whole, "Descendants"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generations_are_opened_and_closed_one_at_a_time_by_links()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var narrow = await visitor.Get($"/en/{Family("character-sohrab")}?up=1");
        var none = await visitor.Get($"/en/{Family("character-sohrab")}?up=0&down=0");

        // One generation up: the parents. Their fathers are there to be opened, and the page says so.
        Assert.Equal(["character-rostam", "character-tahmineh", "character-sohrab"], Boxes(narrow));
        Assert.Contains("There are ancestors further up.", narrow.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("There are descendants further down.", narrow.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-sohrab")}\" aria-label=\"Ancestors: One generation more\"", Steps(narrow, "Ancestors"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-sohrab")}?up=0\" aria-label=\"Ancestors: One generation fewer\"", Steps(narrow, "Ancestors"), StringComparison.Ordinal);

        // Nobody has a child of Sohrab to show: that step is no link.
        Assert.Contains("<span class=\"choice\" aria-disabled=\"true\">One generation more</span>", Steps(narrow, "Descendants"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-sohrab")}?up=1&amp;down=1\"", Steps(narrow, "Descendants"), StringComparison.Ordinal);

        // Without a generation above or below, somebody without a partner stands alone – and the page says why
        // its list is empty.
        Assert.Equal(["character-sohrab"], Boxes(none));
        Assert.Equal(0, Lines(none));
        Assert.Contains("No relation lies inside what is shown.", none.Main, StringComparison.Ordinal);
        Assert.Contains("<span class=\"choice\" aria-disabled=\"true\">One generation fewer</span>", Steps(none, "Ancestors"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-sohrab")}?up=1&amp;down=0\"", Steps(none, "Ancestors"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?up=13")]
    [InlineData("?up=-1")]
    [InlineData("?up=two&down=two")]
    [InlineData("?family=all")]
    [InlineData("?view=sideways")]
    public async Task What_an_address_asks_for_and_is_no_choice_is_not_one(string query)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var page = await visitor.Get($"/en/{Family("character-sohrab")}{query}");
        var plain = await visitor.Get($"/en/{Family("character-sohrab")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal(Graph(plain), Graph(page));
    }

    [Fact]
    public async Task The_address_never_shows_more_than_the_reading_position_allows()
    {
        // At the opening Sohrab and Tahmineh are not told yet – however many generations and whatever family an
        // address asks for.
        foreach (var query in new[] { "?down=12", "?family=whole", "?up=12&down=12&view=across" })
        {
            var page = await site.Visitor().Get($"/en/{Family("character-rostam")}{query}");

            Assert.Equal(["character-zal", "character-rostam"], Boxes(page));
            Assert.DoesNotContain("There are descendants further down.", page.Main, StringComparison.Ordinal);
            Assert.DoesNotContain("Tahmineh", page.Main, StringComparison.Ordinal);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get($"/en/{Family("character-sohrab")}?family=whole")).Status);
    }

    [Fact]
    public async Task Generations_run_down_the_page_or_across_it_and_the_tree_scrolls_in_a_frame()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var down = await visitor.Get($"/en/{Family("character-sohrab")}");
        var across = await visitor.Get($"/en/{Family("character-sohrab")}?view=across");

        Assert.Contains("<div class=\"tree-scroll\" tabindex=\"0\" role=\"region\" aria-label=\"The family of Sohrab as a diagram\">", down.Html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", Choice(down, "Down the page"), StringComparison.Ordinal);
        Assert.Contains($"href=\"{Family("character-sohrab")}?view=across\"", Choice(down, "Across the page"), StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", Choice(across, "Across the page"), StringComparison.Ordinal);

        // Three generations of two, two and one: two boxes wide and three high, or three wide and two high.
        Assert.Matches("<svg class=\"graph tree\" width=\"22.4em\" height=\"20.25em\">", down.Html);
        Assert.Matches("<svg class=\"graph tree\" width=\"37.2em\" height=\"[0-9.]+em\">", across.Html);
        Assert.Equal(Boxes(down).Order(StringComparer.Ordinal), Boxes(across).Order(StringComparer.Ordinal));
        Assert.Equal(Lines(down), Lines(across));

        // No script and no style of its own: places and sizes are attributes of the SVG.
        Assert.DoesNotContain("<script", across.Own, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", across.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_entity_without_family_says_so_and_its_page_offers_no_family_drawing()
    {
        var visitor = site.Visitor();

        var family = await visitor.Get($"/en/{Family("character-rakhsh")}");
        var rakhsh = await visitor.Get($"/en/{Story}/entity/character-rakhsh");
        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");

        Assert.Equal(HttpStatusCode.OK, family.Status);
        Assert.Contains("No family relations to show.", family.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", family.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(Family("character-rakhsh"), rakhsh.Links);
        Assert.Contains(Family("character-rostam"), rostam.Links);
    }

    [Theory]
    [InlineData("character-sohrab")]
    [InlineData("character-nobody")]
    public async Task The_family_of_something_not_read_yet_is_not_found(string entity)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get($"/en/{Family(entity)}")).Status);
    }

    [Fact]
    public async Task On_a_Persian_page_the_family_is_mirrored_and_its_numbers_stay_plain()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(1);

        var english = Graph(await visitor.Get($"/en/{Family("character-rostam")}"));
        var persian = Graph(await visitor.Get($"/fa/{Family("character-rostam")}"));

        // Zal above Rostam, Tahmineh beside Rostam: the tree is two boxes wide. Its first box stands at the start
        // of the tree, which is the right on a Persian page.
        Assert.Matches("^<svg class=\"graph tree\" width=\"22.4em\"", english);
        Assert.Matches("^<svg class=\"graph tree\" width=\"22.4em\"", persian);
        Assert.Matches("<foreignObject x=\"0.6em\"[^>]*width=\"10em\"[^>]*>\\s*<div class=\"graph-node \">\\s*<span class=\"graph-entity\">\\s*<a class=\"graph-name\" href=\"[^\"]*character-zal\"", english);
        Assert.Matches("<foreignObject x=\"11.8em\"[^>]*width=\"10em\"[^>]*>\\s*<div class=\"graph-node \">\\s*<span class=\"graph-entity\">\\s*<a class=\"graph-name\" href=\"[^\"]*character-zal\"", persian);
        Assert.DoesNotMatch("(x|y|x1|x2|y1|y2|width|height)=\"[^\"]*[\u06F0-\u06F9\u066B]", WebUtility.HtmlDecode(persian));
    }

    // --- The story ---

    [Fact]
    public async Task At_the_opening_the_story_has_one_step_and_says_only_that_it_continues()
    {
        var page = await site.Visitor().Get($"/en/{Story}/story");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Single(Regex.Matches(page.Html, "<li class=\"step\""));
        Assert.Contains("id=\"step-1\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("A missing horse and a welcome", page.Main, StringComparison.Ordinal);
        Assert.Contains("This is not a calendar", page.Main, StringComparison.Ordinal);
        Assert.Contains("The story continues beyond this point.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("step-2", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "en")]
    [InlineData(2, "fa")]
    [InlineData(4, "en")]
    public async Task The_steps_and_the_lanes_are_those_of_the_projection_in_its_order(int level, string language)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level, language);
        var projection = At(level);

        var page = await visitor.Get($"/{language}/{Story}/story");

        var labels = Regex.Matches(page.Html, "<li class=\"step\" id=\"step-(\\d+)\">.*?<h2><bdi>([^<]*)</bdi>", RegexOptions.Singleline)
            .Select(match => (Number: int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), Label: WebUtility.HtmlDecode(match.Groups[2].Value)))
            .ToList();
        Assert.Equal(projection.Steps.Select(step => (step.Number, step.Event.Label.Resolve(language, "en").Value)), labels);

        // One row per lane, one filled cell per association – the same associations the steps list.
        var lanes = projection.Lanes();
        var table = Regex.Match(page.Html, "<tbody>.*?</tbody>", RegexOptions.Singleline).Value;
        Assert.Equal(lanes.Lanes.Length, Regex.Matches(table, "<tr>").Count);
        Assert.Equal(lanes.Lanes.Length * lanes.Steps.Length, Regex.Matches(table, "<td>").Count);
        Assert.Equal(projection.Steps.Sum(step => step.Event.Associations.Length), Regex.Matches(table, "class=\"mark\"").Count);
        Assert.Equal(projection.Steps.Sum(step => step.Event.Associations.Length), Regex.Matches(page.Html, "<ul class=\"participants\".*?</ul>", RegexOptions.Singleline).Sum(list => Regex.Matches(list.Value, "<li>").Count));
        Assert.Equal(level < 4, page.Text.Contains(language == "en" ? "The story continues beyond this point." : "قصه پس از این نقطه ادامه دارد.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Somebody_who_is_only_mentioned_is_mentioned_in_the_step_and_in_the_lane()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Story}/story");
        var step = Regex.Match(page.Html, "id=\"step-3\">.*?</ul>", RegexOptions.Singleline).Value;
        var lane = Regex.Match(page.Html, "<tr><th scope=\"row\"><a href=\"[^\"]*character-rostam\">.*?</tr>", RegexOptions.Singleline).Value;
        var cells = Regex.Matches(lane, "<td>(.*?)</td>", RegexOptions.Singleline).Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).ToList();

        Assert.Matches("character-rostam\"><bdi>Rostam</bdi></a>\\s*<span class=\"role\"><bdi>father identified in the explanation</bdi></span>\\s*<span class=\"presence\">is only mentioned, not present</span>", step);
        Assert.Equal(4, cells.Count);
        Assert.Contains("●", cells[0], StringComparison.Ordinal);
        Assert.Contains("○", cells[2], StringComparison.Ordinal);
        Assert.Contains("is only mentioned, not present", cells[2], StringComparison.Ordinal);
        Assert.DoesNotContain("●", cells[2], StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Persian_story_counts_its_steps_in_Persian_digits()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4, "fa");

        var page = await visitor.Get($"/fa/{Story}/story");

        Assert.Contains("گام ۸", page.Main, StringComparison.Ordinal);
        Assert.Contains("سوگواری در زابلستان", page.Main, StringComparison.Ordinal);
        Assert.Contains("id=\"step-8\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("به پایان این مجموعه رسیده‌اید.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_story_is_reached_from_the_collection_and_from_the_steps_of_an_entity()
    {
        var visitor = site.Visitor();

        var collection = await visitor.Get($"/en/{Story}");
        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var story = await visitor.Get($"/en/{Story}/story");

        Assert.Contains($"{Story}/story", collection.Links);
        Assert.Contains($"{Story}/story#step-1", rostam.Links);
        Assert.Contains($"{Story}/entity/character-rostam", story.Links);
        Assert.Contains(Story, story.Links);
        Assert.DoesNotContain("<script", story.Own, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", story.Html);
    }
}
