using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The frame of every page in a real browser (step 20.5, ADR 0024): where the main ways stand in a wide
/// window and on a phone, whether a finger reaches them, and the control of language and appearance with a
/// pointer and with the keyboard.
/// </summary>
public sealed class FrameBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = "story-rostam-sohrab";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    [Theory]
    [InlineData(360, "fa")]
    [InlineData(412, "en")]
    public async Task On_a_phone_the_main_ways_are_a_bar_at_the_foot_that_a_finger_reaches_and_the_page_ends_above_it(int width, string language)
    {
        var page = await site.Visitor(width: width);
        await page.GotoAsync($"/{language}/{Story}/entity/character-rostam");

        // The bar lies at the foot of the window, as wide as it, and stays there while the page is scrolled.
        var window = await page.EvaluateAsync<double[]>("[innerWidth, innerHeight]");
        var bar = (await page.Locator(".site-nav").BoundingBoxAsync())!;
        Assert.InRange(bar.Width, window[0] - 1, window[0] + 1);
        Assert.InRange(bar.Y + bar.Height, window[1] - 1, window[1] + 1);
        await page.EvaluateAsync("window.scrollTo(0, document.documentElement.scrollHeight)");
        var after = (await page.Locator(".site-nav").BoundingBoxAsync())!;
        Assert.InRange(after.Y, bar.Y - 1, bar.Y + 1);

        // Every way is as high and as wide as a finger needs, and its words are whole.
        var ways = page.Locator(".site-nav a");
        Assert.InRange(await ways.CountAsync(), 3, 4);
        for (var index = 0; index < await ways.CountAsync(); index++)
        {
            var way = (await ways.Nth(index).BoundingBoxAsync())!;
            Assert.True(way.Height >= 44 && way.Width >= 44, $"A way is {way.Width} by {way.Height} px.");
            Assert.False(await ways.Nth(index).Locator("span").First.EvaluateAsync<bool>("words => words.scrollWidth > words.clientWidth + 1"), "The words of a way are cut.");
        }

        // The last thing of the page stands above the bar, not under it.
        var foot = (await page.Locator(".site-footer").BoundingBoxAsync())!;
        Assert.True(foot.Y + foot.Height <= after.Y + 1, "The foot of the page lies under the bar.");
        Assert.False(await ScrollsSideways(page));

        var found = await page.RunAxe(new AxeRunOptions { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] } });
        Assert.True(found.Violations.Length == 0, string.Join("; ", found.Violations.Select(rule => $"{rule.Id}: {rule.Nodes.Length}")));
    }

    [Fact]
    public async Task In_a_wide_window_the_ways_stand_beside_the_name_in_one_row()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync("/en/");

        var name = (await page.Locator(".wordmark").BoundingBoxAsync())!;
        var ways = (await page.Locator(".site-nav").BoundingBoxAsync())!;
        var tools = (await page.Locator(".site-tools").BoundingBoxAsync())!;
        Assert.True(ways.Y < name.Y + name.Height && ways.Y + ways.Height > name.Y, "The ways stand in the row of the name.");
        Assert.True(tools.Y < name.Y + name.Height && tools.Y + tools.Height > name.Y, "The settings stand in the row of the name.");
        Assert.True(name.X + name.Width <= ways.X && ways.X + ways.Width <= tools.X + 1, "Name, ways and settings follow each other.");
        Assert.Equal("page", await page.Locator(".site-nav a").First.GetAttributeAsync("aria-current"));

        // The name is the logo (ADR 0026): a drawing as wide as its geometry asks for its height, in the indigo
        // and the ink of the brand on a light ground, in a link that is named once and can be pressed.
        var logo = (await page.Locator(".wordmark .logo").BoundingBoxAsync())!;
        Assert.Equal(1214d / 346, logo.Width / logo.Height, 0.02);
        Assert.InRange(logo.Height, 24, 40);
        Assert.True(name.Height >= 43.5, $"The logo is pressed in {name.Height} px.");
        Assert.Equal("Dastan", await page.Locator(".wordmark").GetAttributeAsync("aria-label"));
        Assert.Equal(["rgb(59, 58, 143)", "rgb(28, 27, 46)"], await page.EvaluateAsync<string[]>("[getComputedStyle(document.querySelector('.logo-symbol')).fill, getComputedStyle(document.querySelector('.logo-word')).fill]"));
    }

    [Theory]
    [InlineData("fa", 360)]
    [InlineData("en", 360)]
    [InlineData("fa", 1280)]
    public async Task On_a_dark_ground_the_logo_is_white_and_on_a_Persian_page_it_is_not_turned_around(string language, int width)
    {
        var page = await site.Visitor(width: width, dark: true);
        await page.GotoAsync($"/{language}/");

        Assert.Equal(["rgb(255, 255, 255)", "rgb(255, 255, 255)"], await page.EvaluateAsync<string[]>("[getComputedStyle(document.querySelector('.logo-symbol')).fill, getComputedStyle(document.querySelector('.logo-word')).fill]"));

        // The symbol stands left of the wordmark in every language: the D is a Latin letter and the word is Latin.
        var places = await page.EvaluateAsync<double[]>("[document.querySelector('.logo-symbol').getBoundingClientRect().right, document.querySelector('.logo-word').getBoundingClientRect().left]");
        Assert.True(places[0] < places[1], "The symbol stands before the wordmark.");
        Assert.Equal("none", await page.EvaluateAsync<string>("getComputedStyle(document.querySelector('.logo')).transform"));
        var logo = (await page.Locator(".wordmark .logo").BoundingBoxAsync())!;
        Assert.Equal(1214d / 346, logo.Width / logo.Height, 0.02);
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"), "The page scrolls sideways.");

        // The icon of the tab and the manifest lead to files that are there.
        var addresses = (await page.EvaluateAsync<string[]>("[...document.querySelectorAll('link[rel~=icon], link[rel=apple-touch-icon]')].map(link => link.href)")).ToList();
        Assert.Equal(3, addresses.Count);
        var manifest = await (await page.APIRequest.GetAsync("/manifest.webmanifest")).JsonAsync();
        addresses.AddRange(manifest!.Value.GetProperty("icons").EnumerateArray().Select(icon => icon.GetProperty("src").GetString()!));
        foreach (var address in addresses)
        {
            Assert.True((await page.APIRequest.GetAsync(address)).Ok, $"{address} is not there.");
        }
    }

    [Theory]
    [InlineData(360)]
    [InlineData(1280)]
    public async Task The_control_of_language_and_appearance_opens_inside_the_window_and_closes_as_a_menu_does(int width)
    {
        var page = await site.Visitor(width: width);
        await page.GotoAsync("/fa/");
        var settings = page.Locator("details.site-settings");
        var panel = settings.Locator(".settings-panel");

        // Closed, nothing of it is in the way; pressed, its panel lies inside the window.
        Assert.False(await panel.IsVisibleAsync());
        await settings.Locator("summary").ClickAsync();
        var box = (await panel.BoundingBoxAsync())!;
        Assert.True(box.X >= 0 && box.X + box.Width <= width, $"The panel reaches from {box.X} to {box.X + box.Width}.");
        Assert.False(await ScrollsSideways(page));

        // Pressing elsewhere closes it; so does the Escape key, which gives the focus back to the control.
        await page.Locator("main h1").ClickAsync();
        Assert.False(await panel.IsVisibleAsync());
        await settings.Locator("summary").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.True(await panel.IsVisibleAsync());
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Escape");
        Assert.False(await panel.IsVisibleAsync());
        Assert.Equal("SUMMARY", await page.EvaluateAsync<string>("document.activeElement.tagName"));

        // The other language is chosen there, and the page is the same page in it.
        await settings.Locator("summary").ClickAsync();
        await panel.Locator("a[hreflang=en]").ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("/en/$"));
        Assert.Equal("ltr", await page.EvaluateAsync<string>("document.documentElement.dir"));
    }

    [Fact]
    public async Task Without_the_script_the_control_opens_and_closes_by_itself()
    {
        var page = await site.Visitor(width: 360, script: false);
        await page.GotoAsync("/en/");
        var settings = page.Locator("details.site-settings");

        await settings.Locator("summary").ClickAsync();
        Assert.True(await settings.Locator(".settings-panel").IsVisibleAsync());
        await settings.Locator("summary").ClickAsync();
        Assert.False(await settings.Locator(".settings-panel").IsVisibleAsync());
    }
}
