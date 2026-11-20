using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The addresses that give the browser a graph (step 19.8, ADR 0020): the family and the connections of an
/// entity. What an address answers is what the page of the same visitor shows – the same nodes, the same
/// relations with the same words and the same way to their sources – asked with what the page is asked with,
/// at the stage the visitor has read to. Its answer is kept by nothing.
/// </summary>
public sealed class GraphDataTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    private static async Task<(Page Page, JsonNode? Data, HttpStatusCode Status)> Both(Browser visitor, string language, string entity, string view, string query = "")
    {
        var page = await visitor.Get($"/{language}/{Story}/entity/{entity}/{view}{query}");
        var answer = await visitor.Get($"/{language}/{Story}/entity/{entity}/{view}.json{query}");
        return (page, answer.Status == HttpStatusCode.OK ? JsonNode.Parse(answer.Html) : null, answer.Status);
    }

    // The relations a page lists: the identifier each sentence gives its sources by, in the order of the page.
    private static string[] Listed(Page page) =>
        [.. Regex.Matches(page.Html, "<li><span class=\"statement\">.*?href=\"[^\"]*/evidence/relation/([^\"?]+)", RegexOptions.Singleline).Select(match => match.Groups[1].Value)];

    private static string Read(Page page) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(page.Html, "<[^>]+>", " ")), "\\s+", " ");

    [Theory]
    [InlineData("en", "character-rostam", "family", "", 4)]
    [InlineData("fa", "character-rostam", "family", "?family=whole", 4)]
    [InlineData("en", "character-sohrab", "family", "?up=1&down=0", 4)]
    [InlineData("en", "character-rostam", "family", "", 1)]
    [InlineData("en", "character-rostam", "connections", "", 4)]
    [InlineData("fa", "character-rostam", "connections", "?open=character-tahmineh", 4)]
    [InlineData("en", "character-rakhsh", "connections", "", 2)]
    public async Task A_graph_holds_the_nodes_and_the_relations_of_the_page_and_no_other(string language, string entity, string view, string query, int readTo)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(readTo, language);
        var (page, data, status) = await Both(visitor, language, entity, view, query);
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (page.Status, status));
        var (nodes, edges, read) = (data!["nodes"]!.AsArray(), data["edges"]!.AsArray(), Read(page));

        Assert.Equal(view, (string?)data["kind"]);
        Assert.Equal(entity, (string?)data["focus"]);

        // The relations: the ones the page lists, in its order, each with its words and the way to its sources.
        Assert.Equal(Listed(page), edges.Select(edge => (string)edge!["id"]!));
        foreach (var edge in edges)
        {
            Assert.Contains((string)edge!["label"]!, read, StringComparison.Ordinal);
            Assert.Contains($"href=\"{(string)edge["source"]!}", page.Html, StringComparison.Ordinal);
            Assert.Contains(nodes, node => (string?)node!["id"] == (string?)edge["from"]);
            Assert.Contains(nodes, node => (string?)node!["id"] == (string?)edge["to"]);
            if (edge["when"] is { } when)
            {
                Assert.Contains((string)when!, read, StringComparison.Ordinal);
            }
        }

        // The nodes: everybody a relation of the page names, and the one the page is about – each by the name
        // the page writes, and each with the address of its page and of its profile.
        var named = edges.SelectMany(edge => new[] { (string)edge!["from"]!, (string)edge["to"]! }).Append(entity).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(named.Order(StringComparer.Ordinal), nodes.Select(node => (string)node!["id"]!).Order(StringComparer.Ordinal));
        foreach (var node in nodes)
        {
            Assert.Contains((string)node!["name"]!, read, StringComparison.Ordinal);
            Assert.Equal($"{Story}/entity/{(string)node["id"]!}", (string?)node["address"]);
            Assert.Equal($"{Story}/entity/{(string)node["id"]!}/profile.json", (string?)node["profile"]);
            Assert.True(node["lines"]!.AsArray().Count <= 3);
        }
    }

    [Fact]
    public async Task A_family_says_its_rows_and_whether_the_page_offers_to_open_further()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        var (_, near, _) = await Both(visitor, "en", "character-sohrab", "family", "?up=1&down=0");
        var (_, whole, _) = await Both(visitor, "en", "character-sohrab", "family", "?family=whole");

        // A parent stands a row above its child, in both; and the nearer view says that there is more above.
        foreach (var graph in new[] { near!, whole! })
        {
            var rows = graph["nodes"]!.AsArray().ToDictionary(node => (string)node!["id"]!, node => (int)node!["row"]!);
            Assert.All(graph["edges"]!.AsArray().Where(edge => (string?)edge!["role"] == "parent"), edge => Assert.Equal(rows[(string)edge!["from"]!] + 1, rows[(string)edge["to"]!]));
            Assert.Contains(0, rows.Values);
        }

        Assert.True((bool)near!["more"]!["above"]!);
        Assert.False((bool)whole!["more"]!["above"]!);
        Assert.True(whole["nodes"]!.AsArray().Count > near["nodes"]!.AsArray().Count);
        Assert.All(whole["edges"]!.AsArray().Where(edge => (string?)edge!["role"] == "partner"), edge => Assert.False((bool)edge!["directed"]!));
    }

    [Fact]
    public async Task What_the_visitor_has_not_read_to_is_in_no_graph_and_an_address_that_names_nothing_is_not_found()
    {
        var early = site.Visitor();
        await early.ReadTo(1);
        var late = site.Visitor();
        await late.ReadTo(4);

        // The graph grows with the reading, exactly as the page does.
        var (earlyPage, earlyData, _) = await Both(early, "en", "character-rostam", "connections");
        var (latePage, lateData, _) = await Both(late, "en", "character-rostam", "connections");
        Assert.True(lateData!["edges"]!.AsArray().Count > earlyData!["edges"]!.AsArray().Count);
        Assert.Equal(Listed(earlyPage), earlyData["edges"]!.AsArray().Select(edge => (string)edge!["id"]!));
        Assert.Equal(Listed(latePage), lateData["edges"]!.AsArray().Select(edge => (string)edge!["id"]!));

        // Somebody the story has not told of yet: no page and no graph, and nothing in the answer.
        foreach (var view in new[] { "family", "connections" })
        {
            var fresh = site.Visitor();
            var missing = await fresh.Get($"/en/{Story}/entity/character-nobody/{view}.json");
            Assert.Equal((HttpStatusCode.NotFound, string.Empty), (missing.Status, missing.Html));
            foreach (var character in new[] { "character-sohrab", "character-tahmineh", "character-afrasiab" })
            {
                Assert.Equal((await fresh.Get($"/en/{Story}/entity/{character}/{view}")).Status, (await fresh.Get($"/en/{Story}/entity/{character}/{view}.json")).Status);
            }

            Assert.Equal(HttpStatusCode.NotFound, (await late.Get($"/en/no-such-story/entity/character-rostam/{view}.json")).Status);
        }

        // Kept by nothing.
        var answer = await late.Get($"/en/{Story}/entity/character-rostam/family.json");
        Assert.Equal("no-store", answer.Response.Headers.CacheControl?.ToString());
        Assert.False(answer.Response.Headers.Contains("Dastan-Keep"));
    }
}
