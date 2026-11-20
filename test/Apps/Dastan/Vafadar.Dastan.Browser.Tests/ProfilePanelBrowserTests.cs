using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The layer of TypeScript in a real browser (step 19.7, ADR 0019), by its first piece: the panel of a profile.
/// A card of a list offers a look at the whole profile; the panel is filled from the address of the profile
/// and shows what the page of the node shows. Without the script nothing of it is there and the list works
/// as before; and nothing the layer fetched is kept on the device.
/// </summary>
public sealed class ProfilePanelBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = BrowserSite.Story;

    private static string Words(string text) => Regex.Replace(text, "\\s+", " ").Trim();

    private static ILocator Offer(IPage page, string entity) => page.Locator($"[data-profile-peek] button[data-profile$='{entity}/profile.json']");

    [Theory]
    [InlineData(360, "en", false, "character-rostam")]
    [InlineData(1280, "fa", true, "character-rakhsh")]
    public async Task A_card_opens_the_profile_in_a_panel_that_says_what_the_page_says(int width, string language, bool dark, string entity)
    {
        // What the page of the node says, read first, by the same visitor.
        var page = await site.Visitor(width, readTo: 4, dark: dark);
        await page.GotoAsync($"/{language}/{Story}/entity/{entity}");
        var name = Words(await page.Locator("main h1").InnerTextAsync());
        var facts = Words(await page.Locator(".profile .profile-facts").InnerTextAsync());

        await page.GotoAsync($"/{language}/{Story}");
        var offer = Offer(page, entity);
        await Assertions.Expect(offer).ToBeVisibleAsync();
        var panel = page.Locator("#profile-panel");
        await Assertions.Expect(panel).ToBeHiddenAsync();

        await offer.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel.Locator("[data-panel-wait]")).ToBeHiddenAsync();

        // The same name, the same facts in the same words, and the way to the page.
        Assert.Equal(name, Words(await panel.Locator("#profile-panel-name").InnerTextAsync()));
        Assert.Equal(facts, Words(await panel.Locator(".profile-facts").InnerTextAsync()));
        Assert.EndsWith($"/{language}/{Story}/entity/{entity}", await panel.Locator("[data-panel-open]").EvaluateAsync<string>("link => link.href"), StringComparison.Ordinal);

        // It fits the screen: over the page on a narrow one, a column at the end of the line on a wide one.
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
        var box = await panel.BoundingBoxAsync();
        Assert.NotNull(box);
        if (width >= 1000)
        {
            Assert.InRange(box.Width, 400, 520);
            Assert.True(language == "fa" ? box.X < 2 : box.X + box.Width > width - 2, "The panel stands at the end of the line.");
        }
        else
        {
            Assert.Equal(width, box.Width, 1.0);
        }

        await page.EvaluateAsync("document.fonts.ready");
        var result = await page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });
        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));

        // The keyboard closes it, and the reader is back where they were.
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(panel).ToBeHiddenAsync();
        Assert.True(await offer.EvaluateAsync<bool>("button => document.activeElement === button"), "The offer that opened the panel has the focus again.");

        // Opened by the keyboard too, and closed by its button.
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await panel.Locator("form[method=dialog] button").ClickAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Without_the_script_the_list_is_whole_and_offers_nothing_it_cannot_do()
    {
        var page = await site.Visitor(360, readTo: 4, script: false);
        await page.GotoAsync($"/en/{Story}");

        await Assertions.Expect(page.Locator("[data-profile-peek]").First).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator("#profile-panel")).ToBeHiddenAsync();

        // The name of the card leads to the page of the node, as before.
        await page.Locator("#subjects ~ ul .node-card h3 a, section[aria-labelledby=subjects] .node-card h3 a").First.ClickAsync();
        await BrowserSite.Arrived(page, new Regex("/entity/"));
        await Assertions.Expect(page.Locator(".profile .profile-facts")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_profile_that_cannot_be_fetched_says_so_and_leads_to_the_page()
    {
        var page = await site.Visitor(360, readTo: 4);
        await page.RouteAsync("**/profile.json", route => route.AbortAsync());
        await page.GotoAsync($"/en/{Story}");

        await Offer(page, "character-rostam").ClickAsync();

        var panel = page.Locator("#profile-panel");
        await Assertions.Expect(panel.Locator("[data-panel-failed]")).ToBeVisibleAsync();
        await Assertions.Expect(panel.Locator("[data-panel-wait]")).ToBeHiddenAsync();
        await panel.Locator("[data-panel-open]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex("/entity/character-rostam$"));
    }

    [Fact]
    public async Task What_the_layer_fetched_is_not_kept_on_the_device()
    {
        // Somebody who turned reading without a connection on: the site has a worker that keeps pages.
        var page = await site.Visitor(360, readTo: 4, offline: true);
        await page.GotoAsync($"/en/{Story}");
        await page.EvaluateAsync("navigator.serviceWorker.ready");
        await page.ReloadAsync();
        await Offer(page, "character-rostam").ClickAsync();
        await Assertions.Expect(page.Locator("#profile-panel .profile-facts")).ToBeVisibleAsync();

        var kept = await page.EvaluateAsync<string[]>(
            "async () => { const all = []; for (const name of await caches.keys()) { for (const request of await (await caches.open(name)).keys()) { all.push(request.url); } } return all; }");

        // The worker kept the page – and nothing of the data the panel asked for.
        Assert.Contains(kept, address => address.EndsWith($"/en/{Story}", StringComparison.Ordinal));
        Assert.DoesNotContain(kept, address => address.Contains("profile.json", StringComparison.Ordinal));
    }
}
