using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The connections page: the drawing written by the server and the list that says the same.</summary>
public sealed class ConnectionsPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static string Connections(string entity, string? open = null) =>
        $"{Story}/entity/{entity}/connections{(open is null ? null : $"?open={open}")}";

    private static string Graph(Page page) => Regex.Match(page.Html, "<svg class=\"graph\".*?</svg>", RegexOptions.Singleline).Value;

    private static List<string> Rows(Page page) =>
        [.. Regex.Matches(Graph(page), "<a class=\"graph-name\" href=\"[^\"]*/entity/([^/\"]+)/connections\"").Select(match => match.Groups[1].Value)];

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
    public async Task An_arrow_points_from_the_first_entity_of_a_relation_to_the_second()
    {
        var graph = Graph(await site.Visitor().Get($"/en/{Connections("character-rostam")}"));
        var ticks = Regex.Matches(graph, "<line class=\"graph-line\"[^>]*x2=\"37\\.2%\"[^>]*>").Select(match => match.Value).ToList();

        // Zal is the father of Rostam: the arrow points to the focus. Rostam rides Rakhsh: it points to the row.
        Assert.Equal(4, ticks.Count);
        Assert.Contains("marker-start=\"url(#graph-arrow)\"", ticks[0], StringComparison.Ordinal);
        Assert.DoesNotContain("marker-end", ticks[0], StringComparison.Ordinal);
        Assert.Contains("marker-end=\"url(#graph-arrow)\"", ticks[1], StringComparison.Ordinal);
        Assert.DoesNotContain("marker-start", ticks[1], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_drawing_and_the_list_show_the_same_relations_for_every_entity(string language)
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
    public async Task Opening_a_row_shows_its_own_connections_below_it_and_closing_takes_them_away()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var closed = await visitor.Get($"/en/{Connections("character-rostam")}");
        var open = await visitor.Get($"/en/{Connections("character-rostam", "character-tahmineh")}");

        Assert.Contains($"{Connections("character-rostam", "character-tahmineh")}", closed.Links);
        Assert.Matches("aria-expanded=\"false\"\\s+aria-label=\"Show the connections of Tahmineh\"", closed.Html);
        Assert.DoesNotContain("character-king-samangan", Rows(closed));

        // Her father and the token she keeps hang below her; her son is a neighbour of Rostam already and is
        // referred to, not drawn twice.
        Assert.Contains("character-king-samangan", Rows(open));
        Assert.Equal(2, Rows(open).Count(row => row == "character-sohrab"));
        Assert.Contains("(already shown)", Graph(open), StringComparison.Ordinal);
        Assert.Matches("aria-expanded=\"true\"\\s+aria-label=\"Hide the connections of Tahmineh\"", open.Html);
        Assert.Contains(Connections("character-rostam"), open.Links);
        Assert.True(Regex.Matches(Graph(open), "<foreignObject").Count > Regex.Matches(Graph(closed), "<foreignObject").Count);
    }

    [Fact]
    public async Task Choosing_a_name_puts_it_in_the_centre()
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
        Assert.DoesNotContain("<svg", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task On_a_Persian_page_the_drawing_starts_on_the_right_and_its_numbers_stay_plain()
    {
        var english = Graph(await site.Visitor().Get($"/en/{Connections("character-rostam")}"));
        var persian = Graph(await site.Visitor().Get($"/fa/{Connections("character-rostam")}"));

        Assert.Matches("<foreignObject x=\"0%\"[^>]*width=\"30%\"", english);
        Assert.Matches("<foreignObject x=\"70%\"[^>]*width=\"30%\"", persian);
        Assert.Matches("<foreignObject x=\"38%\"[^>]*width=\"62%\"", english);
        Assert.Matches("<foreignObject x=\"0%\"[^>]*width=\"62%\"", persian);
        Assert.Contains("پدرِ", WebUtility.HtmlDecode(persian), StringComparison.Ordinal);

        // Measures are for the browser, not for the reader: never in Persian digits, whatever the culture.
        Assert.DoesNotMatch("(x|y|x1|x2|y1|y2|width|height)=\"[^\"]*[\u06F0-\u06F9\u066B]", WebUtility.HtmlDecode(persian));
        Assert.Matches("height=\"[0-9.]+em\"", persian);
    }

    [Fact]
    public async Task An_entity_without_connections_says_so_and_draws_nothing()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Connections("place-zabul")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("No connections to show.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_an_entity_leads_to_its_drawing_and_back()
    {
        var visitor = site.Visitor();

        var entity = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var drawing = await visitor.Get($"/en/{Connections("character-rostam")}");

        Assert.Contains(Connections("character-rostam"), entity.Links);
        Assert.Contains($"{Story}/entity/character-rostam", drawing.Links);
        Assert.DoesNotContain("<script", drawing.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", drawing.Html);
    }
}
