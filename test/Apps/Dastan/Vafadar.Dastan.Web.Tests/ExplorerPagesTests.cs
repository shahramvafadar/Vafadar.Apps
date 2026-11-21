using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The explorer as the server gives it (step 20.8): the tools of a drawing and its inspector in the page, the
/// graph of a family that grows across the page, the address that puts another name in the middle, what stands
/// under a drawing, and the comparison of the times of several names.
/// </summary>
public sealed class ExplorerPagesTests(DatedSite site) : IClassFixture<DatedSite>
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

    private static string Section(Page page, string id)
    {
        var from = page.Main.IndexOf($"aria-labelledby=\"{id}\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : page.Main[from..page.Main.IndexOf("</section>", from, StringComparison.Ordinal)];
    }

    private static async Task<JsonElement> Graph(Browser visitor, string address)
    {
        var answer = await visitor.Get(address);
        Assert.Equal(HttpStatusCode.OK, answer.Status);
        return JsonDocument.Parse(answer.Html).RootElement;
    }

    [Fact]
    public async Task The_tools_in_sight_carry_their_keys_and_the_rest_stands_behind_more()
    {
        var page = await (await Reader()).Get($"/en/{Rostam}/connections");
        var tools = Regex.Match(page.Main, "<div class=\"graph-tools\".*?</div>", RegexOptions.Singleline).Value;

        // Hidden until the script has drawn: a page without it shows the still drawing and no tool that does nothing.
        Assert.StartsWith("<div class=\"graph-tools\" data-graph-tools hidden>", tools, StringComparison.Ordinal);
        Assert.Matches("<button type=\"button\" data-graph-in aria-keyshortcuts=\"\\+\">Larger <kbd aria-hidden=\"true\">\\+</kbd></button>", tools);
        Assert.Matches("data-graph-fit aria-keyshortcuts=\"0\">", tools);
        Assert.Contains("<button type=\"button\" data-graph-more aria-expanded=\"false\" aria-controls=\"graph-more\">More</button>", tools, StringComparison.Ordinal);
        var rest = Regex.Match(tools, "<span class=\"graph-more\" id=\"graph-more\" data-graph-more-tools hidden>.*?</span>", RegexOptions.Singleline).Value;
        Assert.Equal("Save as SVG Save as PNG Show the still drawing", Words(rest));

        // The inspector is in the page, empty and hidden; its acts are named by the page, not by the script.
        var inspector = Regex.Match(page.Main, "<aside class=\"inspector\".*?</aside>", RegexOptions.Singleline).Value;
        Assert.StartsWith("<aside class=\"inspector\" data-graph-inspector hidden tabindex=\"-1\" aria-labelledby=\"inspector-name\">", inspector, StringComparison.Ordinal);
        Assert.StartsWith("Close ", Words(inspector), StringComparison.Ordinal);
        Assert.EndsWith(" Put in the middle The whole profile Add to the comparison On the timeline Its pictures", Words(inspector), StringComparison.Ordinal);
        Assert.Contains("data-inspector-wait role=\"status\"", inspector, StringComparison.Ordinal);
        Assert.Contains("data-inspector-failed role=\"alert\" hidden", inspector, StringComparison.Ordinal);
        Assert.DoesNotContain("data-graph-profile", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_family_that_grows_across_the_page_is_a_graph_too()
    {
        var visitor = await Reader();

        // The page: the still drawing stands inside the graph, in both directions of the generations.
        var across = await visitor.Get($"/en/{Rostam}/family?family=whole&view=across");
        Assert.Matches("<dastan-graph data-source=\"[^\"]*family\\.json\\?family=whole&view=across\"", across.Main);
        Assert.Contains("class=\"graph tree", Regex.Match(across.Main, "<div class=\"graph-still\" data-graph-still>.*?</svg>", RegexOptions.Singleline).Value, StringComparison.Ordinal);

        // The graph says which way it grows; the rows and the places are those of the tree that grows down.
        var grows = await Graph(visitor, $"/en/{Rostam}/family.json?family=whole&view=across");
        var down = await Graph(visitor, $"/en/{Rostam}/family.json?family=whole");
        Assert.Equal("across", grows.GetProperty("flow").GetString());
        Assert.False(down.TryGetProperty("flow", out _));
        static string Placed(JsonElement graph) => string.Join(';', graph.GetProperty("nodes").EnumerateArray().Select(node => $"{node.GetProperty("id")}:{node.GetProperty("row")}:{node.GetProperty("place")}"));
        Assert.Equal(Placed(down), Placed(grows));

        // Carried from another view the direction is "tree", and the graph of that page does not grow across.
        Assert.False((await Graph(visitor, $"/en/{Rostam}/connections.json?tree=across")).TryGetProperty("flow", out _));
    }

    [Fact]
    public async Task Every_card_but_the_one_in_the_middle_says_where_it_is_put_in_the_middle()
    {
        var visitor = await Reader();
        var graph = await Graph(visitor, $"/en/{Rostam}/family.json?family=whole&view=across&order=oldest&sel=character-zal&up=3");
        var nodes = graph.GetProperty("nodes").EnumerateArray().ToDictionary(node => node.GetProperty("id").GetString()!);

        Assert.False(nodes["character-rostam"].TryGetProperty("centre", out _));

        // The same view of the other name, with how the visitor looks and nothing of the first name's drawing.
        Assert.Equal($"{Story}/entity/character-zal/family?view=across&order=oldest", nodes["character-zal"].GetProperty("centre").GetString());
        Assert.Equal($"{Story}/entity/character-sohrab/family?view=across&order=oldest", nodes["character-sohrab"].GetProperty("centre").GetString());

        // The connections: the same, and only what is open in the drawing would be carried.
        var connections = await Graph(visitor, $"/en/{Rostam}/connections.json?open=character-nobody&from=900&to=1000");
        var zal = connections.GetProperty("nodes").EnumerateArray().Single(node => node.GetProperty("id").GetString() == "character-zal");
        Assert.Equal($"{Story}/entity/character-zal/connections?from=900&to=1000", zal.GetProperty("centre").GetString());
    }

    [Fact]
    public async Task Under_a_drawing_stand_the_steps_of_the_telling_as_far_as_they_were_read_and_what_comes_next()
    {
        var late = await (await Reader()).Get($"/en/{Rostam}/connections");
        var early = await (await Reader(stage: 0)).Get($"/en/{Rostam}/connections");

        var (all, some) = (Section(late, "telling"), Section(early, "telling"));
        var read = Regex.Matches(all, "<li>").Count;
        Assert.True(read > Regex.Matches(some, "<li>").Count && Regex.Matches(some, "<li>").Count > 0);

        // Who has not read to the end is told that the telling goes on, and nothing of how.
        Assert.Contains("The telling goes on after what you have read.", Words(some), StringComparison.Ordinal);
        Assert.DoesNotContain("The telling goes on", Words(all), StringComparison.Ordinal);
        Assert.DoesNotContain("Sohrab", Words(some), StringComparison.Ordinal);

        // What comes next is offered where it leads to something: the pictures of a name that has any, and no
        // chart of time for a name of which nothing is dated.
        Assert.Contains($"href=\"{Rostam}/gallery\"", Section(late, "next"), StringComparison.Ordinal);
        Assert.DoesNotContain("/timeline", Section(late, "next"), StringComparison.Ordinal);

        // The family has the same under its drawing.
        Assert.Equal(read, Regex.Matches(Section(await (await Reader()).Get($"/en/{Rostam}/family"), "telling"), "<li>").Count);
    }

    [Fact]
    public async Task A_name_with_times_can_be_added_to_the_comparison_and_stands_beside_the_first_in_the_chart_of_time()
    {
        var visitor = await Reader();
        const string Work = "work-shahnameh";

        // Something chosen that has times offers the comparison; something without times does not.
        var chosen = await visitor.Get($"/en/{Rostam}/connections?sel={Work}");
        var add = WebUtility.HtmlDecode(Regex.Match(chosen.Main, "<a class=\"action secondary\" href=\"([^\"]*)\">Add to the comparison").Groups[1].Value);
        Assert.Equal($"{Rostam}/connections?sel={Work}&with={Work}", add);
        static string Block(Page page) => Regex.Match(page.Main, "<div class=\"explore-chosen\".*?</div>", RegexOptions.Singleline).Value;
        var other = Block(await visitor.Get($"/en/{Rostam}/connections?sel=character-zal"));
        Assert.Contains("Put in the middle", other, StringComparison.Ordinal);
        Assert.DoesNotContain("Add to the comparison", other, StringComparison.Ordinal);

        // Added, it is named on every view, can be taken out, and the chart of time is one step away.
        var compared = await visitor.Get($"/en/{add}");
        var bar = Regex.Match(compared.Main, "<div class=\"explore-compared\">.*?</div>", RegexOptions.Singleline).Value;
        Assert.Equal("Compared with: Shahnameh × – take out of the comparison Compare the times", Words(bar));
        Assert.Contains($"href=\"{Rostam}/connections?sel={Work}\"", bar, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Rostam}/timeline?sel={Work}&with={Work}\"", bar, StringComparison.Ordinal);
        Assert.DoesNotContain("Add to the comparison", Block(compared), StringComparison.Ordinal);

        // The chart of time of the first name shows the times of the other beside it.
        var chart = await visitor.Get($"/en/{Rostam}/timeline?with={Work}");
        Assert.Equal(HttpStatusCode.OK, chart.Status);
        Assert.Contains("The work is written", chart.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("The work is written", (await visitor.Get($"/en/{Rostam}/timeline")).Main, StringComparison.Ordinal);

        // The graph offers the same for its cards.
        var graph = await Graph(visitor, $"/en/{Story}/entity/{Story}/connections.json");
        var nodes = graph.GetProperty("nodes").EnumerateArray().ToDictionary(node => node.GetProperty("id").GetString()!);
        Assert.Equal($"{Story}/entity/{Story}/connections?sel={Work}&with={Work}", nodes[Work].GetProperty("compare").GetString());
        Assert.All(nodes.Where(node => node.Key != Work), node => Assert.False(node.Value.TryGetProperty("compare", out _)));
    }

    [Fact]
    public async Task What_is_compared_is_named_only_when_the_visitor_may_see_it()
    {
        // A statement of the work is told from a later stage; the work itself is public. A name nobody has, and
        // one the visitor has not read to, are compared with nothing – the page is the page without them.
        var early = await Reader(stage: 0);
        var plain = await early.Get($"/en/{Rostam}/connections");
        var asked = await early.Get($"/en/{Rostam}/connections?with=character-sohrab,character-nobody");

        Assert.DoesNotContain("explore-compared", asked.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sohrab", asked.Main, StringComparison.Ordinal);
        Assert.Equal(Words(plain.Main), Words(asked.Main));

        // And the chart of time shows nothing of a later stage for what is compared.
        var chart = await early.Get($"/en/{Rostam}/timeline?with=work-shahnameh");
        Assert.Contains("The work is written", chart.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("A secret copy is made", chart.Main, StringComparison.Ordinal);
    }
}
