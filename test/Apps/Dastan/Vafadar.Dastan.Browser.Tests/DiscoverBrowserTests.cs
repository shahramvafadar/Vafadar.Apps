using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The start of the site in a real browser (step 20.7): whether it fits a phone and a wide window in both
/// directions of writing, whether the piece of a drawing scrolls inside its frame and not the page, and
/// whether the search is what a visitor reaches first.
/// </summary>
public sealed class DiscoverBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    [Theory]
    [InlineData(360, "fa", false)]
    [InlineData(412, "en", true)]
    [InlineData(1280, "fa", true)]
    [InlineData(1280, "en", false)]
    public async Task The_start_fits_the_window_and_breaks_no_rule(int width, string language, bool dark)
    {
        var page = await site.Visitor(width: width, readTo: 4, dark: dark);
        await page.GotoAsync($"/{language}/");

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"), "The page is wider than the window.");

        // The cards stand side by side in a wide window and under each other on a phone; none is cut.
        var cards = page.Locator("#start-with ~ ul .card, section[aria-labelledby=start-with] .card");
        Assert.True(await cards.CountAsync() >= 2);
        var (first, second) = ((await cards.Nth(0).BoundingBoxAsync())!, (await cards.Nth(1).BoundingBoxAsync())!);
        Assert.Equal(width >= 1280, Math.Abs(first.Y - second.Y) < 2);
        Assert.True(first.Width <= width && first.X >= 0);

        // The piece of a drawing scrolls inside its frame, which the keyboard reaches.
        var peek = page.Locator(".drawing-peek");
        var frame = (await peek.BoundingBoxAsync())!;
        Assert.True(frame.Width <= width && frame.Height <= 26 * 16 + 2, $"The frame is {frame.Width} by {frame.Height} px.");
        Assert.Equal("0", await peek.GetAttributeAsync("tabindex"));

        await page.EvaluateAsync("document.fonts.ready");
        var found = await page.RunAxe(new AxeRunOptions { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] } });
        Assert.True(found.Violations.Length == 0, string.Join("; ", found.Violations.Select(rule => $"{rule.Id}: {string.Join(" | ", rule.Nodes.Select(node => node.Html).Take(2))}")));
    }

    [Fact]
    public async Task The_search_is_the_first_field_of_the_page_and_finds_a_name()
    {
        var page = await site.Visitor(width: 360, readTo: 4);
        await page.GotoAsync("/en/");

        var search = page.Locator(".hero input[type=search]");
        var box = (await search.BoundingBoxAsync())!;
        Assert.True(box.Height >= 44, $"The field is {box.Height} px high.");
        Assert.True(box.Y < 800, "The search is in the first screen.");
        await search.FillAsync("Rostam");
        await search.PressAsync("Enter");
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("q=Rostam"));
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("Rostam");
    }
}
