using System.Text.Json;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// DastanGraph in a real browser (step 19.8, ADR 0020): the family and the connections of a node as a drawing
/// that can be moved, made larger and smaller, walked with the keyboard and saved as a picture. It draws what
/// the address of the graph answers and nothing else, sends nothing to another host, keeps Persian in its
/// direction, and leaves the still drawing where its script does not run. Each of the things the product does
/// for the control behind the contract has its test here, so that a new version of the control is tried
/// against them.
/// </summary>
public sealed class GraphBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = BrowserSite.Story;

    // The marks that give a line its direction; they have no width and are no part of what a line says.
    private static string Plain(string text) => Regex.Replace(text, "[‪-‮‎‏]", string.Empty);

    private static string Words(string text) => Regex.Replace(Plain(text), "\\s+", " ").Trim();

    private static async Task<(ILocator Host, JsonElement Data)> Drawn(IPage page, string address)
    {
        await page.GotoAsync(address);
        var host = page.Locator("dastan-graph");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 30_000 });
        var data = await page.EvaluateAsync<JsonElement>("async () => await (await fetch(new URL(document.querySelector('dastan-graph').dataset.source, document.baseURI))).json()");
        return (host, data);
    }

    private static Task<JsonElement> Seen(ILocator host) => host.EvaluateAsync<JsonElement>("host => host.graph.seen");

    private static async Task<string> Written(ILocator host) =>
        Words(string.Join(' ', await host.Locator("[data-graph-canvas] text").AllTextContentsAsync()));

    [Theory]
    [InlineData(1280, "fa", true, "family", "?family=whole")]
    [InlineData(360, "en", false, "family", "?family=whole")]
    [InlineData(1280, "en", false, "connections", "")]
    [InlineData(360, "fa", true, "connections", "")]
    public async Task The_drawing_shows_what_the_address_of_the_graph_answers_and_asks_no_other_host(int width, string language, bool dark, string view, string query)
    {
        var page = await site.Visitor(width, readTo: 4, dark: dark, graph: true);
        var asked = new List<string>();
        page.Request += (_, request) => asked.Add(request.Url);
        var refused = new List<string>();
        page.Console += (_, message) =>
        {
            if (message.Text.Contains("Content Security Policy", StringComparison.Ordinal))
            {
                refused.Add(message.Text);
            }
        };

        var (host, data) = await Drawn(page, $"/{language}/{Story}/entity/character-rostam/{view}{query}");

        // The still drawing gave its place to the one that moves; the tools are there.
        await Assertions.Expect(host.Locator("[data-graph-still]")).ToBeHiddenAsync();
        await Assertions.Expect(host.Locator("[data-graph-canvas]")).ToBeVisibleAsync();
        await Assertions.Expect(host.Locator("[data-graph-fit]")).ToBeVisibleAsync();

        // Every node by its name and the lines of its card; among connections every relation by its words.
        var written = await Written(host);
        var nodes = data.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 2);
        foreach (var node in nodes)
        {
            Assert.Contains(Words(node.GetProperty("name").GetString()!), written, StringComparison.Ordinal);
            foreach (var line in node.GetProperty("lines").EnumerateArray())
            {
                Assert.Contains(Words(line.GetString()!), written, StringComparison.Ordinal);
            }
        }

        if (view == "connections")
        {
            foreach (var edge in data.GetProperty("edges").EnumerateArray())
            {
                Assert.Contains(Words(edge.GetProperty("label").GetString()!), written, StringComparison.Ordinal);
            }
        }

        // A card for each node, each with room of its own: no card lies on another.
        var cards = (await Seen(host)).GetProperty("cards").EnumerateArray()
            .Select(card => (Id: card.GetProperty("id").GetString()!, X: card.GetProperty("x").GetDouble(), Y: card.GetProperty("y").GetDouble(), W: card.GetProperty("width").GetDouble(), H: card.GetProperty("height").GetDouble()))
            .ToList();
        Assert.Equal(nodes.Select(node => node.GetProperty("id").GetString()).Order(StringComparer.Ordinal), cards.Select(card => card.Id).Order(StringComparer.Ordinal));
        foreach (var one in cards)
        {
            Assert.True(one.W > 100 && one.H > 20, $"The card of {one.Id} has a size.");
            foreach (var other in cards.Where(other => string.CompareOrdinal(other.Id, one.Id) > 0))
            {
                Assert.False(
                    one.X < other.X + other.W && other.X < one.X + one.W && one.Y < other.Y + other.H && other.Y < one.Y + one.H,
                    $"The cards of {one.Id} and {other.Id} lie on one another.");
            }
        }

        // A family: a parent stands above its child, as the rows of the layout say; and on a page read from
        // right to left the first place of a row is at the right.
        if (view == "family")
        {
            var at = cards.ToDictionary(card => card.Id);
            var rows = nodes.ToDictionary(node => node.GetProperty("id").GetString()!, node => (Row: node.GetProperty("row").GetInt32(), Place: node.GetProperty("place").GetDouble()));
            foreach (var edge in data.GetProperty("edges").EnumerateArray().Where(edge => edge.GetProperty("role").GetString() == "parent"))
            {
                Assert.True(at[edge.GetProperty("from").GetString()!].Y < at[edge.GetProperty("to").GetString()!].Y);
            }

            foreach (var row in rows.GroupBy(pair => pair.Value.Row).Where(row => row.Count() > 1))
            {
                var first = row.MinBy(pair => pair.Value.Place).Key;
                var last = row.MaxBy(pair => pair.Value.Place).Key;
                Assert.True(language == "fa" ? at[first].X > at[last].X : at[first].X < at[last].X);
            }
        }

        // The page does not grow sideways for it, and it passes the checks of accessibility.
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
        await page.EvaluateAsync("document.fonts.ready");
        var result = await page.RunAxe(BrowserSite.PageOfDastan, new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });
        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));

        // Nothing was asked of another host – the control's own count of its use among it – and nothing the
        // page did was refused by the rules of the site.
        await page.WaitForTimeoutAsync(1500);
        var origin = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
        Assert.All(asked, address => Assert.True(
            address.StartsWith(origin, StringComparison.Ordinal) || address.StartsWith("data:", StringComparison.Ordinal) || address.StartsWith("blob:", StringComparison.Ordinal),
            address));
        Assert.Empty(refused);
    }

    [Fact]
    public async Task Persian_lines_keep_their_direction_and_their_words_whole()
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var (host, data) = await Drawn(page, $"/fa/{Story}/entity/character-rostam/connections");

        // Every line of Persian is wrapped in the marks of its direction (and the mark WebKit needs after it);
        // a line is a part of the text it was broken from at a space – never a word torn in two.
        var lines = await host.Locator("[data-graph-canvas] text tspan").AllTextContentsAsync();
        var persian = lines.Where(line => Regex.IsMatch(line, "[؀-ۿ]")).ToList();
        Assert.NotEmpty(persian);
        Assert.All(persian, line => Assert.Matches("^‫.*‬‎$", line.Trim()));

        var texts = data.GetProperty("nodes").EnumerateArray()
            .SelectMany(node => node.GetProperty("lines").EnumerateArray().Select(line => line.GetString()!).Append(node.GetProperty("name").GetString()!))
            .Concat(data.GetProperty("edges").EnumerateArray().Select(edge => edge.GetProperty("label").GetString()! + (edge.TryGetProperty("when", out var when) ? " · " + when.GetString() : string.Empty)))
            .Select(Words)
            .ToList();
        foreach (var line in persian.Select(Words).Where(line => line.Length > 0))
        {
            Assert.Contains(texts, text => (" " + text + " ").Contains(" " + line + " ", StringComparison.Ordinal));
        }

        // The text of a card stands inside its card, at the side the page is read from.
        var inside = await host.EvaluateAsync<bool>(
            @"host => {
                const frame = host.querySelector('[data-graph-canvas]').getBoundingClientRect();
                return [...host.querySelectorAll('[data-graph-canvas] text')].every(text => {
                    const box = text.getBoundingClientRect();
                    return box.width === 0 || (box.left >= frame.left - 1 && box.right <= frame.right + 1);
                });
            }");
        Assert.True(inside, "A text is drawn outside the drawing.");
    }

    [Fact]
    public async Task Two_relations_between_the_same_two_stand_apart_each_with_its_words()
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var (host, data) = await Drawn(page, $"/en/{Story}/entity/character-rostam/connections");

        var pairs = data.GetProperty("edges").EnumerateArray()
            .GroupBy(edge => string.Join('|', new[] { edge.GetProperty("from").GetString()!, edge.GetProperty("to").GetString()! }.Order(StringComparer.Ordinal)))
            .Where(pair => pair.Count() > 1)
            .ToList();
        Assert.NotEmpty(pairs);

        // Each of them is drawn as a line of its own, and no two labels of the drawing stand on one another.
        var labels = await host.EvaluateAsync<double[][]>(
            @"host => [...host.querySelectorAll('[data-graph-canvas] g[id$=""_groupElement""]')]
                .filter(group => /^e\d+_groupElement$/.test(group.id))
                .map(group => [...group.querySelectorAll('text')].map(text => text.getBoundingClientRect()))
                .flat().filter(box => box.width > 0).map(box => [box.left, box.top, box.right, box.bottom])");
        Assert.True(labels.Length >= data.GetProperty("edges").GetArrayLength(), $"{labels.Length} labels for {data.GetProperty("edges").GetArrayLength()} relations.");
        for (var one = 0; one < labels.Length; one++)
        {
            for (var other = one + 1; other < labels.Length; other++)
            {
                var (a, b) = (labels[one], labels[other]);
                Assert.False(a[0] < b[2] - 2 && b[0] < a[2] - 2 && a[1] < b[3] - 2 && b[1] < a[3] - 2, $"Two labels lie on one another: {string.Join(' ', a)} and {string.Join(' ', b)}.");
            }
        }
    }

    [Fact]
    public async Task What_is_chosen_in_the_drawing_is_in_the_address_and_in_the_links_of_the_views()
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var (host, _) = await Drawn(page, $"/en/{Story}/entity/character-rostam/family?family=whole");
        Task<string[]> Links() => page.EvaluateAsync<string[]>("[...document.querySelectorAll('a[data-explore]')].map(link => new URL(link.href).search)");

        // The one in the middle is not chosen in itself: the address says nothing.
        await host.Locator("[data-graph-canvas]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.DoesNotContain("sel=", page.Url, StringComparison.Ordinal);

        // Another card: the address of the page and every link of a view name it (ADR 0025).
        await page.Keyboard.PressAsync("ArrowUp");
        var above = (await Seen(host)).GetProperty("chosen").GetString()!;
        Assert.EndsWith($"/family?family=whole&sel={above}", page.Url, StringComparison.Ordinal);
        var links = await Links();
        Assert.True(links.Length >= 3);
        Assert.All(links, link => Assert.Contains($"sel={above}", link, StringComparison.Ordinal));

        // The page asked again with that address shows the same card chosen – on the page and in the drawing.
        var again = await site.Visitor(1280, readTo: 4, graph: true);
        var (drawn, _) = await Drawn(again, page.Url);
        Assert.Equal(above, (await Seen(drawn)).GetProperty("chosen").GetString());
        // Beside the drawing the inspector says what is chosen; the block of the page, which said the same, has gone.
        await Assertions.Expect(drawn.Locator("[data-graph-inspector]")).ToBeVisibleAsync();
        await Assertions.Expect(again.Locator("[data-explore-chosen]")).ToHaveCountAsync(0);

        // Letting go of the card takes it out of the address, of the links and of what the page said.
        await drawn.Locator("[data-graph-canvas]").FocusAsync();
        await again.Keyboard.PressAsync("Escape");
        Assert.DoesNotContain("sel=", again.Url, StringComparison.Ordinal);
        Assert.All(await again.EvaluateAsync<string[]>("[...document.querySelectorAll('a[data-explore]')].map(link => new URL(link.href).search)"), link => Assert.DoesNotContain("sel=", link, StringComparison.Ordinal));
        await Assertions.Expect(drawn.Locator("[data-graph-inspector]")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task A_page_that_explores_no_single_name_has_no_bar_a_drawing_could_write_into()
    {
        // A drawing writes what is chosen into an address only where the bar of exploring stands (explore.ts):
        // the page of a comparison has none. That a tree of an account has none either is not walked here.
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        await page.GotoAsync($"/en/{Story}/entity/character-rostam/family?family=whole");
        Assert.True(await page.EvaluateAsync<bool>("document.querySelector('nav.view-bar') !== null"));
        await page.GotoAsync("/en/compare/iran-china-rome");
        Assert.False(await page.EvaluateAsync<bool>("document.querySelector('nav.view-bar') !== null"));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_keyboard_walks_the_cards_opens_a_profile_and_leaves_the_drawing(string language)
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var (host, data) = await Drawn(page, $"/{language}/{Story}/entity/character-rostam/family?family=whole");
        var canvas = host.Locator("[data-graph-canvas]");
        var said = host.Locator("[data-graph-said]");
        var names = data.GetProperty("nodes").EnumerateArray().ToDictionary(node => node.GetProperty("id").GetString()!, node => node.GetProperty("name").GetString()!);

        // The drawing is one stop of Tab, with a name and with the keys said beside it.
        await canvas.FocusAsync();
        Assert.Equal(0, await host.Locator("[data-graph-canvas] [tabindex]:not([tabindex='-1'])").CountAsync());
        Assert.False(string.IsNullOrWhiteSpace(await canvas.GetAttributeAsync("aria-label")));
        await Assertions.Expect(page.Locator("#" + await canvas.GetAttributeAsync("aria-describedby"))).ToBeVisibleAsync();
        var inspector = host.Locator("[data-graph-inspector]");
        await Assertions.Expect(inspector).ToBeHiddenAsync();

        // The first arrow chooses the one the page is about; it is said, with its relations.
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("character-rostam", (await Seen(host)).GetProperty("chosen").GetString());
        await Assertions.Expect(said).ToContainTextAsync(names["character-rostam"]);

        // Beside the drawing the inspector shows it at once – and does not take the focus: the arrows go on.
        await Assertions.Expect(inspector).ToBeVisibleAsync();
        await Assertions.Expect(inspector.Locator("[data-inspector-name]")).ToHaveTextAsync(names["character-rostam"]);
        await Assertions.Expect(inspector.Locator("[data-inspector-wait]")).ToBeHiddenAsync();
        Assert.True(await canvas.EvaluateAsync<bool>("canvas => document.activeElement === canvas"), "The drawing keeps the focus.");

        // The one in the middle is in the middle already, and is not compared with itself.
        await Assertions.Expect(inspector.Locator("[data-inspector-centre]")).ToBeHiddenAsync();
        await Assertions.Expect(inspector.Locator("[data-inspector-compare]")).ToBeHiddenAsync();

        // What is chosen is carried on (R-EXP-05): to its pictures, where a picture shows it – and to nothing it does not have.
        await Assertions.Expect(host.Locator("[data-graph-gallery]")).ToBeVisibleAsync();
        Assert.EndsWith($"/{language}/{Story}/entity/character-rostam/gallery", await host.Locator("[data-graph-gallery]").EvaluateAsync<string>("link => link.href"), StringComparison.Ordinal);
        await Assertions.Expect(host.Locator("[data-graph-timeline]")).ToBeHiddenAsync();

        // An arrow goes to the card that stands in its direction – and moves no card.
        var before = (await Seen(host)).GetProperty("cards").GetRawText();
        await page.Keyboard.PressAsync("ArrowUp");
        var above = (await Seen(host)).GetProperty("chosen").GetString()!;
        Assert.NotEqual("character-rostam", above);
        Assert.Contains(data.GetProperty("edges").EnumerateArray(), edge => edge.GetProperty("role").GetString() == "parent" && edge.GetProperty("from").GetString() == above && edge.GetProperty("to").GetString() == "character-rostam");
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("character-rostam", (await Seen(host)).GetProperty("chosen").GetString());
        await page.Keyboard.PressAsync("ArrowDown");
        var below = (await Seen(host)).GetProperty("chosen").GetString()!;
        Assert.Contains(data.GetProperty("edges").EnumerateArray(), edge => edge.GetProperty("role").GetString() == "parent" && edge.GetProperty("to").GetString() == below);
        await page.Keyboard.PressAsync("Delete");
        Assert.Equal(before, (await Seen(host)).GetProperty("cards").GetRawText());

        // The inspector follows what is chosen: its name, its profile, and the acts – "put in the middle" leads
        // to the same view of the other name.
        await Assertions.Expect(inspector.Locator("[data-inspector-name]")).ToHaveTextAsync(names[below]);
        await Assertions.Expect(inspector.Locator("[data-inspector-wait]")).ToBeHiddenAsync();
        await Assertions.Expect(inspector.Locator(".profile-facts")).ToBeVisibleAsync();
        Assert.EndsWith($"/{language}/{Story}/entity/{below}/family", await inspector.Locator("[data-inspector-centre]").EvaluateAsync<string>("link => link.href"), StringComparison.Ordinal);
        Assert.EndsWith($"/{language}/{Story}/entity/{below}", await inspector.Locator("[data-inspector-open]").EvaluateAsync<string>("link => link.href"), StringComparison.Ordinal);

        // Enter goes to the inspector; Escape comes back to the drawing, and what is chosen stays chosen.
        await page.Keyboard.PressAsync("Enter");
        Assert.True(await inspector.EvaluateAsync<bool>("inspector => document.activeElement === inspector"), "The inspector has the focus.");
        await page.Keyboard.PressAsync("Escape");
        Assert.True(await canvas.EvaluateAsync<bool>("canvas => document.activeElement === canvas"), "The drawing has the focus again.");
        Assert.Equal(below, (await Seen(host)).GetProperty("chosen").GetString());
        await Assertions.Expect(page.Locator("#profile-panel")).ToBeHiddenAsync();

        // Larger, smaller and the whole of it, by keys; Escape lets go of the card.
        var whole = (await Seen(host)).GetProperty("zoom").GetDouble();
        await page.Keyboard.PressAsync("+");
        Assert.True((await Seen(host)).GetProperty("zoom").GetDouble() > whole);
        await page.Keyboard.PressAsync("0");
        Assert.Equal(whole, (await Seen(host)).GetProperty("zoom").GetDouble(), 0.01);
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(JsonValueKind.Null, (await Seen(host)).GetProperty("chosen").ValueKind);
        await Assertions.Expect(said).ToBeEmptyAsync();
        await Assertions.Expect(inspector).ToBeHiddenAsync();

        // Tab leaves the drawing, in both directions.
        await page.Keyboard.PressAsync("Tab");
        Assert.False(await canvas.EvaluateAsync<bool>("canvas => canvas.contains(document.activeElement)"), "Tab left the drawing.");
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.False(await canvas.EvaluateAsync<bool>("canvas => canvas.contains(document.activeElement)"), "Shift and Tab left the drawing.");
    }

    [Theory]
    [InlineData(1280, "en")]
    [InlineData(360, "fa")]
    public async Task A_visitor_who_comes_back_finds_the_drawing_as_large_and_as_far_moved_as_it_was_left(int width, string language)
    {
        var page = await site.Visitor(width, readTo: 4, graph: true);
        var address = $"/{language}/{Story}/entity/character-rostam/family?family=whole";
        var (host, _) = await Drawn(page, address);
        var whole = await Seen(host);

        // Made larger with the tool – which is written at once, not a moment later: somebody may leave at once.
        await host.Locator("[data-graph-in]").ClickAsync();
        Assert.True(await host.EvaluateAsync<bool>(
            "host => { host.querySelector('[data-graph-in]').click(); return Math.abs(history.state.drawings.g0.zoom - host.graph.seen.zoom) < 0.0001; }"),
            "What a tool did is written when the tool has done it.");

        // Moved with a drag on the ground of the drawing: written when the finger lets go.
        var box = (await host.Locator("[data-graph-canvas]").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync((float)(box.X + 12), (float)(box.Y + 12));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(box.X + 52), (float)(box.Y + 42), new() { Steps = 6 });
        await page.Mouse.UpAsync();
        await page.WaitForFunctionAsync(
            "() => { const [kept, seen] = [history.state?.drawings?.g0, document.querySelector('dastan-graph').graph.seen]; return kept && Math.abs(kept.x - seen.x) < 0.01 && Math.abs(kept.y - seen.y) < 0.01 && Math.abs(kept.zoom - seen.zoom) < 0.0001; }");
        var left = await Seen(host);
        Assert.True(left.GetProperty("zoom").GetDouble() > whole.GetProperty("zoom").GetDouble());
        Assert.NotEqual(whole.GetProperty("x").GetDouble(), left.GetProperty("x").GetDouble(), 1.0);

        // Where it stood is kept with the place of the history, not in the address that is passed on.
        Assert.EndsWith(address, page.Url, StringComparison.Ordinal);

        async Task Same(JsonElement expected)
        {
            await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 30_000 });

            // The drawing stands where it stood once the control has settled; then it is read, so that a failure
            // says where it stands instead.
            try
            {
                await page.WaitForFunctionAsync(
                    "to => { const seen = document.querySelector('dastan-graph').graph?.seen; return seen && Math.abs(seen.zoom - to[0]) < 0.001 && Math.abs(seen.x - to[1]) < 1 && Math.abs(seen.y - to[2]) < 1; }",
                    new[] { expected.GetProperty("zoom").GetDouble(), expected.GetProperty("x").GetDouble(), expected.GetProperty("y").GetDouble() },
                    new() { Timeout = 5_000 });
            }
            catch (TimeoutException)
            {
            }

            var now = await Seen(host);
            Assert.Equal(expected.GetProperty("zoom").GetDouble(), now.GetProperty("zoom").GetDouble(), 0.001);
            Assert.Equal(expected.GetProperty("x").GetDouble(), now.GetProperty("x").GetDouble(), 1.0);
            Assert.Equal(expected.GetProperty("y").GetDouble(), now.GetProperty("y").GetDouble(), 1.0);
        }

        // The page loaded anew at the same place of the history: the drawing is drawn again and put back.
        await page.ReloadAsync();
        await Same(left);

        // Another page and the Back button: the same.
        await page.GotoAsync($"/{language}/");
        await page.GoBackAsync();
        await Same(left);

        // The page gives the drawing another room, twice – as a page does while it arrives, and as a window
        // does that is made narrower: what the visitor moved stays where it was put and is not forgotten.
        // (Seen by eye first: the drawing was whole again after the second change.)
        var window = page.ViewportSize!;
        await page.SetViewportSizeAsync(window.Width - 48, window.Height - 40);
        await page.WaitForTimeoutAsync(300);
        await page.SetViewportSizeAsync(window.Width, window.Height);
        await page.WaitForTimeoutAsync(300);
        await Same(left);
        Assert.True(await page.EvaluateAsync<bool>("() => history.state?.drawings?.g0 !== undefined"), "Where the drawing stood was forgotten.");
        await page.ReloadAsync();
        await Same(left);

        // "The whole drawing" forgets where it stood: back again, it is whole.
        await host.Locator("[data-graph-fit]").ClickAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => history.state?.drawings?.g0 === undefined"));
        await page.ReloadAsync();
        await Same(whole);

        // And somebody who opens the address for the first time finds the whole of it.
        await host.Locator("[data-graph-in]").ClickAsync();
        await page.WaitForFunctionAsync("() => history.state?.drawings?.g0 !== undefined");
        await page.GotoAsync($"/{language}/");
        var (again, _) = await Drawn(page, address);
        Assert.Equal(whole.GetProperty("zoom").GetDouble(), (await Seen(again)).GetProperty("zoom").GetDouble(), 0.001);
    }

    [Fact]
    public async Task The_tools_make_it_larger_and_smaller_and_give_the_still_drawing_back()
    {
        var page = await site.Visitor(360, readTo: 4, graph: true);
        var (host, _) = await Drawn(page, $"/fa/{Story}/entity/character-rostam/family?family=whole");

        var whole = (await Seen(host)).GetProperty("zoom").GetDouble();
        await host.Locator("[data-graph-in]").ClickAsync();
        var larger = (await Seen(host)).GetProperty("zoom").GetDouble();
        Assert.True(larger > whole);
        await host.Locator("[data-graph-out]").ClickAsync();
        await host.Locator("[data-graph-out]").ClickAsync();
        Assert.True((await Seen(host)).GetProperty("zoom").GetDouble() < larger);
        await host.Locator("[data-graph-fit]").ClickAsync();
        Assert.Equal(whole, (await Seen(host)).GetProperty("zoom").GetDouble(), 0.01);

        // Every tool is large enough for a finger.
        foreach (var tool in await host.Locator("[data-graph-tools] button:visible").AllAsync())
        {
            var box = await tool.BoundingBoxAsync();
            Assert.True(box is { Height: >= 44, Width: >= 44 }, await tool.InnerTextAsync());
        }

        // The still drawing, with its links, for a reader who wants it – and back.
        // What is not used all the time stands behind "More", which says whether it is open.
        await Assertions.Expect(host.Locator("[data-graph-to-still]")).ToBeHiddenAsync();
        await host.Locator("[data-graph-more]").ClickAsync();
        Assert.Equal("true", await host.Locator("[data-graph-more]").GetAttributeAsync("aria-expanded"));
        await host.Locator("[data-graph-to-still]").ClickAsync();
        await Assertions.Expect(host.Locator("[data-graph-canvas]")).ToBeHiddenAsync();
        await Assertions.Expect(host.Locator("[data-graph-still] a.graph-name").First).ToBeVisibleAsync();
        await Assertions.Expect(host.Locator("[data-graph-in]")).ToBeHiddenAsync();
        await host.Locator("[data-graph-to-live]").ClickAsync();
        await Assertions.Expect(host.Locator("[data-graph-canvas]")).ToBeVisibleAsync();
        await Assertions.Expect(host.Locator("[data-graph-still]")).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("en")]
    public async Task The_picture_it_saves_stands_by_itself_with_its_font_and_is_sharp(string language)
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var (host, data) = await Drawn(page, $"/{language}/{Story}/entity/character-rostam/family?family=whole");

        var svg = await host.EvaluateAsync<string>("async host => await (await host.graph.picture('svg')).text()");
        foreach (var node in data.GetProperty("nodes").EnumerateArray())
        {
            Assert.Contains(Words(node.GetProperty("name").GetString()!).Split(' ')[0], Plain(svg), StringComparison.Ordinal);
        }

        // Nothing in the file points at the site: the font of the Persian text is in it, and so are the pictures.
        Assert.Equal(language == "fa", svg.Contains("@font-face", StringComparison.Ordinal));
        Assert.DoesNotMatch("(href|src)=\"(?!data:|#)[^\"]+\"", svg);
        Assert.DoesNotContain("url(http", svg, StringComparison.Ordinal);

        // The PNG is drawn from that file at twice its size.
        var sizes = await host.EvaluateAsync<double[]>(
            @"async host => {
                const svg = new DOMParser().parseFromString(await (await host.graph.picture('svg')).text(), 'image/svg+xml').documentElement;
                const png = await createImageBitmap(await host.graph.picture('png'));
                return [parseFloat(svg.getAttribute('width')), parseFloat(svg.getAttribute('height')), png.width, png.height];
            }");
        Assert.True(sizes[0] > 100 && sizes[1] > 50);
        Assert.Equal(sizes[0] * 2, sizes[2], 2.0);
        Assert.Equal(sizes[1] * 2, sizes[3], 2.0);

        // The buttons hand the files to the reader.
        await host.Locator("[data-graph-more]").ClickAsync();
        var saved = await page.RunAndWaitForDownloadAsync(() => host.Locator("[data-graph-svg]").ClickAsync());
        Assert.Equal("dastan.svg", saved.SuggestedFilename);
        saved = await page.RunAndWaitForDownloadAsync(() => host.Locator("[data-graph-png]").ClickAsync());
        Assert.Equal("dastan.png", saved.SuggestedFilename);
    }

    [Fact]
    public async Task What_the_reader_has_not_read_to_is_not_in_the_drawing()
    {
        var early = await site.Visitor(1280, readTo: 1, graph: true);
        var (earlyHost, earlyData) = await Drawn(early, $"/en/{Story}/entity/character-rostam/connections");
        var late = await site.Visitor(1280, readTo: 4, graph: true);
        var (lateHost, lateData) = await Drawn(late, $"/en/{Story}/entity/character-rostam/connections");

        var told = earlyData.GetProperty("nodes").EnumerateArray().Select(node => node.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
        var later = lateData.GetProperty("nodes").EnumerateArray().Where(node => !told.Contains(node.GetProperty("id").GetString()!)).ToList();
        Assert.NotEmpty(later);
        var (earlyWritten, lateWritten) = (await Written(earlyHost), await Written(lateHost));
        foreach (var node in later)
        {
            var name = Words(node.GetProperty("name").GetString()!);
            Assert.Contains(name, lateWritten, StringComparison.Ordinal);
            Assert.DoesNotContain(name, earlyWritten, StringComparison.Ordinal);
        }

        Assert.Equal(told.Count, (await Seen(earlyHost)).GetProperty("cards").GetArrayLength());
    }

    [Fact]
    public async Task Without_its_script_and_where_the_graph_cannot_be_fetched_the_still_drawing_stays()
    {
        // No script at all: the page is the still drawing, and nothing of the tools is shown.
        var without = await site.Visitor(360, readTo: 4, script: false, graph: true);
        await without.GotoAsync($"/en/{Story}/entity/character-rostam/family");
        await Assertions.Expect(without.Locator("dastan-graph [data-graph-still] a.graph-name").First).ToBeVisibleAsync();
        await Assertions.Expect(without.Locator("dastan-graph [data-graph-tools]")).ToBeHiddenAsync();
        await Assertions.Expect(without.Locator("dastan-graph [data-graph-live]")).ToBeHiddenAsync();

        // The address of the graph does not answer: the same.
        var failing = await site.Visitor(360, readTo: 4, graph: true);
        await failing.RouteAsync("**/family.json*", route => route.AbortAsync());
        await failing.GotoAsync($"/en/{Story}/entity/character-rostam/family");
        var host = failing.Locator("dastan-graph");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "failed", new() { Timeout = 30_000 });
        await Assertions.Expect(host.Locator("[data-graph-still] a.graph-name").First).ToBeVisibleAsync();
        await Assertions.Expect(host.Locator("[data-graph-tools]")).ToBeHiddenAsync();

        // A page without a graph does not ask for the script of the graph.
        var plain = await site.Visitor(360, readTo: 4, graph: true);
        var asked = new List<string>();
        plain.Request += (_, request) => asked.Add(request.Url);
        await plain.GotoAsync($"/en/{Story}");
        await plain.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain(asked, address => address.Contains("graph", StringComparison.Ordinal) && address.EndsWith(".js", StringComparison.Ordinal));
    }
}
