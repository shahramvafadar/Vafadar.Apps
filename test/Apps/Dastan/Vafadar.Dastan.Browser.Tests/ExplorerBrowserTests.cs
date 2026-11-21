using System.Text.Json;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The explorer in a real browser (step 20.8): the inspector beside the drawing in a wide window and as a
/// sheet at the foot of a phone – who has the focus, what Escape and the Back button do –, and the family that
/// grows across the page as a drawing that moves, in both directions of writing.
/// </summary>
public sealed class ExplorerBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = "story-rostam-sohrab";

    private static async Task<ILocator> Drawn(IPage page, string address)
    {
        await page.GotoAsync(address);
        var host = page.Locator("dastan-graph");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live");
        return host;
    }

    private static Task<JsonElement> Seen(ILocator host) => host.EvaluateAsync<JsonElement>("host => host.graph.seen");

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task In_a_wide_window_the_inspector_stands_beside_the_drawing_and_covers_nothing_of_it(string language)
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var host = await Drawn(page, $"/{language}/{Story}/entity/character-rostam/family?family=whole");
        var canvas = host.Locator("[data-graph-canvas]");
        var inspector = host.Locator("[data-graph-inspector]");
        var before = (await canvas.BoundingBoxAsync())!;

        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(inspector).ToBeVisibleAsync();
        await Assertions.Expect(inspector.Locator("[data-inspector-wait]")).ToBeHiddenAsync();

        // The drawing gave the inspector its room: the two lie side by side, the inspector where the page ends.
        var (drawing, beside) = ((await canvas.BoundingBoxAsync())!, (await inspector.BoundingBoxAsync())!);
        Assert.True(drawing.Width < before.Width - 200, $"The drawing is {drawing.Width} px wide, as before ({before.Width}).");
        var apart = language == "fa" ? beside.X + beside.Width <= drawing.X + 1 : drawing.X + drawing.Width <= beside.X + 1;
        Assert.True(apart, $"The inspector ({beside.X}–{beside.X + beside.Width}) lies on the drawing ({drawing.X}–{drawing.X + drawing.Width}).");
        Assert.Equal("static", await inspector.EvaluateAsync<string>("inspector => getComputedStyle(inspector).position"));

        // What is chosen is in sight in the room that is left – once the drawing has been told its new room.
        await page.WaitForFunctionAsync(
            """
            () => {
                const graph = document.querySelector('dastan-graph').graph, diagram = graph.diagram;
                const card = diagram.getObject(graph.selected).wrapper.bounds, zoom = diagram.scrollSettings.currentZoom;
                const left = card.x * zoom - diagram.scrollSettings.horizontalOffset;
                return left >= -1 && left + card.width * zoom <= document.querySelector('[data-graph-canvas]').clientWidth + 1;
            }
            """);

        // Closing lets go of the card, gives the room back and the focus to the drawing.
        await inspector.Locator("[data-inspector-close]").ClickAsync();
        await Assertions.Expect(inspector).ToBeHiddenAsync();
        Assert.Equal(JsonValueKind.Null, (await Seen(host)).GetProperty("chosen").ValueKind);
        Assert.True(await canvas.EvaluateAsync<bool>("canvas => document.activeElement === canvas"));
        await Assertions.Expect(canvas).ToHaveJSPropertyAsync("clientWidth", (int)Math.Round(before.Width));
    }

    [Fact]
    public async Task On_a_phone_the_inspector_is_a_sheet_that_opens_when_asked_and_closes_with_escape_and_with_back()
    {
        var page = await site.Visitor(360, readTo: 4, graph: true);
        var address = $"/en/{Story}/entity/character-rostam/family?family=whole";
        var host = await Drawn(page, address);
        var canvas = host.Locator("[data-graph-canvas]");
        var sheet = host.Locator("[data-graph-inspector]");

        // The keys walk the cards without a sheet over the drawing.
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        var chosen = (await Seen(host)).GetProperty("chosen").GetString();
        await Assertions.Expect(sheet).ToBeHiddenAsync();

        // Enter opens it: at the foot of the window, above the bar of the ways, with the focus.
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        Assert.True(await sheet.EvaluateAsync<bool>("sheet => document.activeElement === sheet"), "The sheet has the focus.");
        var (box, bar) = ((await sheet.BoundingBoxAsync())!, (await page.Locator(".site-nav").BoundingBoxAsync())!);
        Assert.Equal("fixed", await sheet.EvaluateAsync<string>("sheet => getComputedStyle(sheet).position"));
        Assert.InRange(box.Y + box.Height, bar.Y - 1, bar.Y + 1);
        Assert.True(box.Height <= 800 * 0.55 + 1 && box.Width >= 358, $"The sheet is {box.Width} by {box.Height} px.");

        // Escape closes it and lets go of the card; the focus is in the drawing again.
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        Assert.True(await canvas.EvaluateAsync<bool>("canvas => document.activeElement === canvas"), "The drawing has the focus again.");
        Assert.Equal(JsonValueKind.Null, (await Seen(host)).GetProperty("chosen").ValueKind);

        // Opened again, the Back button closes the sheet – and not the page.
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        await page.GoBackAsync();
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        Assert.Contains("/entity/character-rostam/family", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live");
        Assert.NotNull(chosen);
    }

    [Theory]
    [InlineData("en", 1)]
    [InlineData("fa", -1)]
    public async Task A_family_that_grows_across_the_page_is_drawn_in_columns_of_generations(string language, int towardsTheYoung)
    {
        var page = await site.Visitor(1280, readTo: 4, graph: true);
        var host = await Drawn(page, $"/{language}/{Story}/entity/character-rostam/family?family=whole&view=across");
        var data = JsonDocument.Parse(await page.EvaluateAsync<string>("address => fetch(address).then(answer => answer.text())", await host.GetAttributeAsync("data-source"))).RootElement;
        Assert.Equal("across", data.GetProperty("flow").GetString());

        var cards = (await Seen(host)).GetProperty("cards").EnumerateArray().ToDictionary(card => card.GetProperty("id").GetString()!, card => (X: card.GetProperty("x").GetDouble(), Y: card.GetProperty("y").GetDouble(), Width: card.GetProperty("width").GetDouble(), Height: card.GetProperty("height").GetDouble()));
        var edges = data.GetProperty("edges").EnumerateArray().ToList();

        // A parent stands in the column before its child – at the left on a page read from the left, at the
        // right on one read from the right –, and partners in one column, under each other.
        var parents = edges.Where(edge => edge.GetProperty("role").GetString() == "parent").ToList();
        Assert.NotEmpty(parents);
        Assert.All(parents, edge =>
        {
            var (parent, child) = (cards[edge.GetProperty("from").GetString()!], cards[edge.GetProperty("to").GetString()!]);
            Assert.True((child.X - parent.X) * towardsTheYoung >= parent.Width, $"{edge.GetProperty("from")} at {parent.X}, {edge.GetProperty("to")} at {child.X}.");
        });
        var partners = edges.Where(edge => edge.GetProperty("role").GetString() == "partner").ToList();
        Assert.NotEmpty(partners);
        Assert.All(partners, edge =>
        {
            var (one, other) = (cards[edge.GetProperty("from").GetString()!], cards[edge.GetProperty("to").GetString()!]);
            Assert.Equal(one.X, other.X, 1.0);
            Assert.True(Math.Abs(one.Y - other.Y) >= Math.Max(one.Height, other.Height));
        });

        // No two cards lie on each other.
        var all = cards.Values.ToList();
        for (var first = 0; first < all.Count; first++)
        {
            for (var second = first + 1; second < all.Count; second++)
            {
                var (a, b) = (all[first], all[second]);
                Assert.False(a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height, "Two cards lie on each other.");
            }
        }

        // The arrow that points to the young goes from a parent to a child.
        await host.Locator("[data-graph-canvas]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("character-rostam", (await Seen(host)).GetProperty("chosen").GetString());
        await page.Keyboard.PressAsync(towardsTheYoung > 0 ? "ArrowRight" : "ArrowLeft");
        var young = (await Seen(host)).GetProperty("chosen").GetString();
        Assert.Contains(parents, edge => edge.GetProperty("from").GetString() == "character-rostam" && edge.GetProperty("to").GetString() == young);

        // The picture that is saved is the drawing as it grows: wider than it is high.
        var size = await host.EvaluateAsync<double[]>(
            """
            async host => {
                const svg = new DOMParser().parseFromString(await (await host.graph.picture('svg')).text(), 'image/svg+xml').documentElement;
                return [parseFloat(svg.getAttribute('width')), parseFloat(svg.getAttribute('height'))];
            }
            """);
        Assert.True(size[0] > size[1] && size[1] > 50, $"The saved picture is {size[0]} by {size[1]}.");
    }

    [Fact]
    public async Task Without_the_script_the_family_across_the_page_is_the_still_drawing()
    {
        var page = await site.Visitor(1280, readTo: 4, script: false, graph: true);
        await page.GotoAsync($"/fa/{Story}/entity/character-rostam/family?family=whole&view=across");

        await Assertions.Expect(page.Locator("dastan-graph svg.graph.tree")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-graph-tools]")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator("[data-graph-inspector]")).ToBeHiddenAsync();
    }
}
