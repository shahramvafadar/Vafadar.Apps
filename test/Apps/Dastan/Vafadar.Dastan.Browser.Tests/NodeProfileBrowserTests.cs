using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The profile of a node in a real browser (step 19.5, ADR 0017): the picture or the drawing of the kind
/// beside the facts, on a narrow and on a wide screen, in both languages and both appearances.
/// </summary>
public sealed class NodeProfileBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = BrowserSite.Story;

    [Theory]
    [InlineData(360, "en", false, "character-rakhsh")]
    [InlineData(360, "fa", true, "character-rakhsh")]
    [InlineData(1280, "en", true, "character-zal")]
    [InlineData(1280, "fa", false, "character-rostam")]
    public async Task The_profile_fits_its_screen_and_breaks_no_rule_of_accessibility_that_a_tool_can_check(int width, string language, bool dark, string entity)
    {
        var page = await site.Visitor(width, dark: dark);
        await page.GotoAsync($"/{language}/{Story}/entity/{entity}");
        await page.EvaluateAsync("document.fonts.ready");

        // Nothing of the profile reaches beyond the screen, and the drawing of the kind is a square that is seen.
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
        var picture = await page.Locator(".identity .profile-picture").BoundingBoxAsync();
        Assert.NotNull(picture);
        Assert.InRange(picture.Width, 100, 240);
        Assert.Equal(picture.Width, picture.Height, 1.0);
        var facts = await page.Locator(".identity .identity-words").BoundingBoxAsync();
        Assert.NotNull(facts);

        // The head (step 20.9): the words beside the picture where there is room, under it where there is none.
        if (width >= 1000)
        {
            Assert.True(Math.Abs(facts.Y - picture.Y) < 4, "The words stand beside the picture.");
        }
        else
        {
            Assert.True(facts.Y > picture.Y + picture.Height, "The words stand under the picture.");
        }

        // Every fact is folded until asked for, opens without a script of the site, and fits the screen as well.
        await Assertions.Expect(page.Locator(".profile .profile-facts")).ToBeHiddenAsync();
        await page.Locator("details.fold:not(.explore-years) > summary").ClickAsync();
        await Assertions.Expect(page.Locator(".profile .profile-facts")).ToBeVisibleAsync();
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));

        var result = await page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });
        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));
    }

    [Fact]
    public async Task The_cards_of_a_collection_fit_a_narrow_screen_and_lead_to_the_profile()
    {
        var page = await site.Visitor(360);
        await page.GotoAsync($"/fa/{Story}");

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
        var card = page.Locator("#subjects ~ ul .node-card, section[aria-labelledby=subjects] .node-card").First;
        var small = await card.Locator(".profile-picture").BoundingBoxAsync();
        Assert.NotNull(small);
        Assert.InRange(small.Width, 48, 80);

        await card.Locator("h3 a").ClickAsync();
        await BrowserSite.Arrived(page, new System.Text.RegularExpressions.Regex("/entity/"));
        await Assertions.Expect(page.Locator(".identity h1")).ToBeVisibleAsync();
    }
}
