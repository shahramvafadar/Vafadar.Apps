using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The family drawing and the story step by step with its lanes.</summary>
public sealed class FamilyAndStoryPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static string Family(string entity) => $"{Story}/entity/{entity}/family";

    private static string Graph(Page page) => Regex.Match(page.Html, "<svg class=\"graph\".*?</svg>", RegexOptions.Singleline).Value;

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
        Assert.Equal(4, Regex.Matches(graph, "<line class=\"graph-line \"[^>]*marker-end=\"url\\(#graph-arrow\\)\"").Count);
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
            var page = await visitor.Get($"/{language}/{Family(member.Entity.Id.Value)}");
            var family = projection.Family(member.Entity.Id)!;

            Assert.Equal(family.Members.Select(entry => entry.Entity.Id.Value), Boxes(page));
            Assert.Equal(family.Parentage.Length + family.Partnerships.Length, Regex.Matches(Graph(page), "<line ").Count);
            Assert.Equal(family.Parentage.Length + family.Partnerships.Length, Regex.Matches(page.Html, "<li><span class=\"statement\">").Count);
        }
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

        // Two members in a row: the first stands at the start of the row, which is the right on a Persian page.
        Assert.Matches("<foreignObject x=\"2%\"[^>]*width=\"46%\"", english);
        Assert.Matches("<foreignObject x=\"52%\"[^>]*width=\"46%\"", persian);
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
        Assert.DoesNotContain("<script", story.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", story.Html);
    }
}
