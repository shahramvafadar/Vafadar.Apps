using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The connections page: the chart written by the server on its stage, and the list that says the same.</summary>
public sealed class ConnectionsPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static string Connections(string entity, string? open = null) =>
        $"{Story}/entity/{entity}/connections{(open is null ? null : $"?open={open}")}";

    private static string Graph(Page page) => Regex.Match(page.Html, "<svg class=\"graph flow\".*?</svg>", RegexOptions.Singleline).Value;

    private static List<string> Rows(Page page) =>
        [.. Regex.Matches(Graph(page), "<a class=\"graph-name\" href=\"[^\"]*/entity/([^/\"]+)/connections\"").Select(match => match.Groups[1].Value)];

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    // Where the box of an entity – or the focus – begins in the chart, in em: across and down.
    private static (double X, double Y) Place(Page page, string entity)
    {
        var box = Regex.Match(
            Graph(page),
            "<foreignObject x=\"([0-9.]+)em\" y=\"([0-9.]+)em\"[^>]*>(?:(?!</foreignObject>).)*?<a class=\"graph-name\" href=\"[^\"]*/entity/" + entity + "(/connections)?\"",
            RegexOptions.Singleline);
        Assert.True(box.Success, $"{entity} has no box.");
        return (Number(box.Groups[1].Value), Number(box.Groups[2].Value));
    }

    // The link of a step at a side of the stage, or null when the step cannot be taken.
    private static string? Step(Page page, string words)
    {
        var step = Regex.Match(page.Html, "<a class=\"stage-step\" href=\"([^\"]*)\" title=\"" + Regex.Escape(words) + "\"");
        return step.Success ? WebUtility.HtmlDecode(step.Groups[1].Value) : null;
    }

    [Fact]
    public async Task The_neighbours_of_an_entity_are_drawn_around_it_with_the_relation_that_leads_to_each()
    {
        var page = await site.Visitor().Get($"/en/{Connections("character-rostam")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Connections of Rostam</h1>", page.Html, StringComparison.Ordinal);
        Assert.Equal(["character-zal", "character-rakhsh", "place-samangan", "place-turan"], Rows(page));
        Assert.Matches("<span class=\"graph-relation\"><bdi>Rides Rakhsh</bdi></span>\\s*<span class=\"graph-entity\">\\s*<a class=\"graph-name\" href=\"[^\"]*character-rakhsh/connections\"><bdi>Rakhsh</bdi>", Graph(page));
        Assert.Matches("class=\"graph-node graph-focus\">\\s*<a class=\"graph-name\" href=\"story-rostam-sohrab/entity/character-rostam\"", Graph(page));
        Assert.Contains("aria-label=\"The connections of Rostam as a diagram\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_leads_to_the_name_stands_above_it_and_what_it_leads_to_below()
    {
        var page = await site.Visitor().Get($"/en/{Connections("character-rostam")}");
        var focus = Place(page, "character-rostam");

        // Zal is the father of Rostam; Rostam rides Rakhsh, visits Samangan and hunts near Turan.
        Assert.True(Place(page, "character-zal").Y < focus.Y);
        Assert.True(Place(page, "character-rakhsh").Y > focus.Y);
        Assert.True(Place(page, "place-samangan").Y > Place(page, "character-rakhsh").Y);
        Assert.True(Place(page, "place-turan").Y > Place(page, "place-samangan").Y);
    }

    [Fact]
    public async Task A_name_whose_relation_is_the_same_from_both_sides_stands_beside_the_name_in_the_middle()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Connections("character-rostam")}");
        var focus = Place(page, "character-rostam");
        var tahmineh = Place(page, "character-tahmineh");

        // Rostam marries Tahmineh: neither stands above the other.
        Assert.Equal(focus.Y, tahmineh.Y);
        Assert.True(tahmineh.X > focus.X);
    }

    [Fact]
    public async Task An_arrow_points_from_the_first_entity_of_a_relation_to_the_second()
    {
        var graph = Graph(await site.Visitor().Get($"/en/{Connections("character-rostam")}"));
        var arrows = Regex.Matches(graph, "<line class=\"graph-line\"[^>]*marker-[^>]*>").Select(match => match.Value).ToList();

        // Zal is the father of Rostam: the arrow leaves the box of Zal. Rostam rides Rakhsh: it points to the box.
        Assert.Equal(4, arrows.Count);
        Assert.Contains("marker-start=\"url(#graph-arrow)\"", arrows[0], StringComparison.Ordinal);
        Assert.DoesNotContain("marker-end", arrows[0], StringComparison.Ordinal);
        Assert.All(arrows.Skip(1), arrow =>
        {
            Assert.Contains("marker-end=\"url(#graph-arrow)\"", arrow, StringComparison.Ordinal);
            Assert.DoesNotContain("marker-start", arrow, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_chart_and_the_list_show_the_same_relations_for_every_entity(string language)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4, language);
        var projection = Projection.Of(site.Catalogue, new ReadingLevel(4), Audience.LocalDemonstration)!;

        foreach (var entity in projection.Entities)
        {
            var open = string.Join(',', projection.Entities.Select(candidate => candidate.Id.Value));
            foreach (var address in new[] { Connections(entity.Id.Value), Connections(entity.Id.Value, open) })
            {
                var page = await visitor.Get($"/{language}/{address}");
                var view = projection.Connections(entity.Id, address.Contains('?', StringComparison.Ordinal) ? projection.Entities.Select(candidate => candidate.Id) : null)!;

                Assert.Equal(HttpStatusCode.OK, page.Status);
                Assert.Equal(view.Edges.Length, Regex.Matches(Graph(page), "class=\"graph-relation\"").Count);
                Assert.Equal(view.Edges.Length, Regex.Matches(page.Html, "<li><span class=\"statement\">").Count);
            }
        }
    }

    [Fact]
    public async Task Opening_a_box_shows_its_own_connections_beyond_it_and_closing_takes_them_away()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var closed = await visitor.Get($"/en/{Connections("character-rostam")}");
        var open = await visitor.Get($"/en/{Connections("character-rostam", "character-tahmineh")}");

        Assert.Contains($"{Connections("character-rostam", "character-tahmineh")}#chart", closed.Links);
        Assert.Matches("aria-expanded=\"false\"\\s+aria-label=\"Show the connections of Tahmineh\"", closed.Html);
        Assert.DoesNotContain("character-king-samangan", Rows(closed));

        // Her father and the token she keeps hang on her box; her son is a neighbour of Rostam already and is
        // referred to, not drawn twice.
        Assert.Contains("character-king-samangan", Rows(open));
        Assert.Equal(2, Rows(open).Count(row => row == "character-sohrab"));
        Assert.Contains("(already shown)", Graph(open), StringComparison.Ordinal);
        Assert.Matches("aria-expanded=\"true\"\\s+aria-label=\"Hide the connections of Tahmineh\"", open.Html);
        Assert.Contains($"{Connections("character-rostam")}#chart", open.Links);
        Assert.True(Regex.Matches(Graph(open), "<foreignObject").Count > Regex.Matches(Graph(closed), "<foreignObject").Count);

        // What hangs on her stays at her side of the chart, a step further from the middle.
        Assert.True(Place(open, "character-king-samangan").X > Place(open, "character-tahmineh").X);
    }

    [Fact]
    public async Task The_chart_stands_on_a_stage_whose_sides_open_it_a_step_further_and_close_it_again()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);
        var closed = await visitor.Get($"/en/{Connections("character-rostam")}");

        // The four sides: what leads to Rostam, what Rostam leads to, and – at both of the other sides – the
        // names beside him.
        Assert.Matches("<div class=\"stage \" id=\"chart\" dir=\"ltr\">", closed.Html);
        Assert.Matches("class=\"stage-edge stage-top\"[^>]*><span class=\"stage-label\">Towards Rostam</span>", closed.Html);
        Assert.Matches("class=\"stage-edge stage-bottom\"[^>]*><span class=\"stage-label\">From Rostam</span>", closed.Html);
        Assert.Matches("class=\"stage-edge stage-left\"[^>]*><span class=\"stage-label\">Mutual</span>", closed.Html);
        Assert.Matches("class=\"stage-edge stage-right\"[^>]*><span class=\"stage-label\">Mutual</span>", closed.Html);

        // Nothing is open yet: no side can be closed, and the side of Tahmineh can be opened.
        Assert.Null(Step(closed, "Mutual: close a step"));
        Assert.Null(Step(closed, "From Rostam: close a step"));
        Assert.Contains("title=\"Mutual: close a step (not possible here)\"", closed.Html, StringComparison.Ordinal);
        var further = Step(closed, "Mutual: open a step further");
        Assert.Equal($"{Connections("character-rostam", "character-tahmineh")}#chart", further);

        // A step further opens her box; a step back from there is the chart as it was.
        var open = await visitor.Get($"/en/{further}");
        Assert.Contains("character-king-samangan", Rows(open));
        Assert.Equal($"{Connections("character-rostam")}#chart", Step(open, "Mutual: close a step"));

        // A side opens every box of it that can be opened, and only boxes of that side.
        var below = Step(closed, "From Rostam: open a step further")!;
        var opened = Regex.Match(below, "[?]open=([^#]*)#chart$").Groups[1].Value.Split(',');
        Assert.NotEmpty(opened);
        Assert.DoesNotContain("character-tahmineh", opened);
        Assert.DoesNotContain("character-zal", opened);
        var focus = Place(closed, "character-rostam");
        Assert.All(opened, entity => Assert.True(Place(closed, entity).Y > focus.Y));
    }

    [Fact]
    public async Task Choosing_a_name_puts_it_in_the_middle()
    {
        var visitor = site.Visitor();

        var rostam = await visitor.Get($"/en/{Connections("character-rostam")}");
        var zal = await visitor.Get($"/en/{Connections("character-zal")}");

        Assert.Contains(Connections("character-zal"), rostam.Links);
        Assert.Contains("<h1>Connections of Zal</h1>", zal.Html, StringComparison.Ordinal);
        Assert.Equal(["character-rostam"], Rows(zal));
    }

    [Theory]
    [InlineData("character-sohrab,character-tahmineh")]
    [InlineData("nobody, ,character-rostam,,place-zabul")]
    [InlineData("story-rostam-sohrab")]
    public async Task An_address_cannot_open_what_the_visitor_has_not_read_or_what_is_not_connected(string open)
    {
        var visitor = site.Visitor();

        var plain = await visitor.Get($"/en/{Connections("character-rostam")}");
        var tried = await visitor.Get($"/en/{Connections("character-rostam", open)}");

        static string Shape(Page page) => Regex.Replace(Regex.Replace(page.Html, "value=\"[^\"]*\"", "value"), "\\?open=[^\"#]*", string.Empty);
        Assert.Equal(HttpStatusCode.OK, tried.Status);
        Assert.Equal(Shape(plain), Shape(tried));
        Assert.DoesNotContain("Sohrab</bdi>", Graph(tried), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("character-sohrab")]
    [InlineData("character-nobody")]
    public async Task The_connections_of_something_not_read_yet_are_not_found(string entity)
    {
        var page = await site.Visitor().Get($"/en/{Connections(entity)}");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("<svg", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task On_a_Persian_page_the_chart_starts_on_the_right_and_its_numbers_stay_plain()
    {
        var english = await site.Visitor().Get($"/en/{Connections("character-rostam")}");
        var persian = await site.Visitor().Get($"/fa/{Connections("character-rostam")}");
        var width = Number(Regex.Match(Graph(persian), "<svg class=\"graph flow\" width=\"([0-9.]+)em\"").Groups[1].Value);

        // The focus begins where reading begins, and what hangs on it steps away from that side.
        Assert.True(Place(english, "character-rostam").X < 1);
        Assert.Equal(width - ConnectionsChart.BoxWidth - Place(english, "character-rostam").X, Place(persian, "character-rostam").X, 3);
        Assert.True(Place(english, "character-rakhsh").X > Place(english, "character-rostam").X);
        Assert.True(Place(persian, "character-rakhsh").X < Place(persian, "character-rostam").X);
        Assert.Contains("پدرِ", WebUtility.HtmlDecode(Graph(persian)), StringComparison.Ordinal);
        Assert.Matches("class=\"stage-edge stage-top\"[^>]*><span class=\"stage-label\">به سوی رستم</span>", WebUtility.HtmlDecode(persian.Html));

        // Measures are for the browser, not for the reader: never in Persian digits, whatever the culture.
        Assert.DoesNotMatch("(x|y|x1|x2|y1|y2|width|height)=\"[^\"]*[\u06F0-\u06F9\u066B]", WebUtility.HtmlDecode(Graph(persian)));
        Assert.Matches("height=\"[0-9.]+em\"", Graph(persian));
    }

    [Fact]
    public async Task An_entity_without_connections_says_so_and_draws_nothing()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Connections("place-zabul")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("No connections to show.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"stage", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_an_entity_leads_to_its_chart_and_back()
    {
        var visitor = site.Visitor();

        var entity = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var chart = await visitor.Get($"/en/{Connections("character-rostam")}");

        Assert.Contains(Connections("character-rostam"), entity.Links);
        Assert.Contains($"{Story}/entity/character-rostam", chart.Links);
        Assert.DoesNotContain("<script", chart.Own, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", chart.Html);
    }
}
