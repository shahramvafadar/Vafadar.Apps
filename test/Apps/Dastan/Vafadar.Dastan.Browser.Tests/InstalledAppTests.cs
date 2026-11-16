using System.Text.Json;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The installable app in a real browser (ADR 0007): what the service worker keeps and what it shows without a
/// connection, that the site works without its one script, what the browser says about installing it, and the
/// automatic part of the accessibility check.
/// </summary>
public sealed class InstalledAppTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = BrowserSite.Story;

    // --- No connection ---

    [Fact]
    public async Task Without_a_connection_the_visitor_gets_the_page_that_says_so_and_nothing_of_the_story_from_a_store()
    {
        var page = await site.Visitor(readTo: 2);
        await page.GotoAsync($"/en/{Story}/media/media-dastan-family-illustration");
        await Assertions.Expect(page.Locator(".media-figure img")).ToBeVisibleAsync();

        // The worker of the site takes over the pages of the whole site.
        var scope = await page.EvaluateAsync<string>("navigator.serviceWorker.ready.then(registration => new URL(registration.scope).pathname)");
        Assert.Equal("/", scope);
        await page.EvaluateAsync("() => new Promise(done => navigator.serviceWorker.controller ? done() : navigator.serviceWorker.addEventListener('controllerchange', () => done()))");

        // What it keeps: the page for "no connection" and what that page is made of. No page, no file of the story.
        var kept = await page.EvaluateAsync<string[]>(
            """
            async () => {
                const all = [];
                for (const name of await caches.keys()) {
                    for (const request of await (await caches.open(name)).keys()) all.push(name + ' ' + new URL(request.url).pathname);
                }
                return all.sort();
            }
            """);
        Assert.Equal(
            [
                "dastan-shell-1 /_content/Vafadar.Web/vafadar.css",
                "dastan-shell-1 /app.css",
                "dastan-shell-1 /fonts/vazirmatn-nl-wght.woff2",
                "dastan-shell-1 /icon.svg",
                "dastan-shell-1 /offline.html",
            ],
            kept);

        await page.Context.SetOfflineAsync(true);

        // The image the browser showed a moment ago is not answered from anywhere.
        Assert.Equal("no answer", await page.EvaluateAsync<string>(
            $"fetch('/media/{Story}/media-dastan-family-illustration/file').then(answer => 'answered ' + answer.status, () => 'no answer')"));

        // A page that is asked for now is the page that says there is no connection – in both languages, looking
        // like the site, with nothing of the story on it.
        await page.GotoAsync($"/fa/{Story}/story");
        Assert.Equal("No connection", await page.Locator("h1").InnerTextAsync());
        Assert.Equal("اتصال برقرار نیست", await page.Locator("h2").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("parseFloat(getComputedStyle(document.querySelector('a.action')).minHeight) >= 44"));
        var text = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("Rostam", text, StringComparison.Ordinal);
        Assert.DoesNotContain("رستم", text, StringComparison.Ordinal);

        // With the connection back, "Try again" asks for the same address and gets the page.
        await page.Context.SetOfflineAsync(false);
        await page.Locator("a.action").First.ClickAsync();
        await Assertions.Expect(page.Locator("ol.steps > li.step")).ToHaveCountAsync(4);
        Assert.Equal("fa", await page.EvaluateAsync<string>("document.documentElement.lang"));
        Assert.EndsWith($"/fa/{Story}/story", page.Url, StringComparison.Ordinal);
    }

    // --- Without the script ---

    [Fact]
    public async Task With_scripts_turned_off_the_journey_and_the_reading_position_work_as_before()
    {
        var page = await site.Visitor(width: 412, script: false);

        await page.GotoAsync("/en/");
        await page.Locator("main h3 a").First.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/en/{Story}$"));
        await Assertions.Expect(page.Locator(".media-card")).ToHaveCountAsync(0);

        await page.Locator("#stage-2").CheckAsync();
        await page.Locator(".reading-form button[type=submit]").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("#reading$"));

        await page.Locator("main a").GetByText("Images and videos", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".media-card")).ToHaveCountAsync(2);
        await page.Locator("#media-dastan-family-illustration h2 a").ClickAsync();
        await Assertions.Expect(page.Locator(".media-figure img")).ToBeVisibleAsync();

        await page.Locator(".subjects a").GetByText("Sohrab", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Sohrab");
        await page.Locator(".actions a").GetByText("Family").ClickAsync();
        await Assertions.Expect(page.Locator("svg.graph foreignObject")).ToHaveCountAsync(5);

        var search = page.Locator("#search-q");
        await page.GotoAsync($"/en/{Story}");
        await search.FillAsync("tahm");
        await search.PressAsync("Enter");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/search[?]q=tahm$"));
        await Assertions.Expect(page.Locator(".cards h2 a")).ToHaveTextAsync("Tahmineh");
    }

    // --- Installing ---

    [Fact]
    public async Task The_browser_reads_the_manifest_without_complaint_and_names_no_obstacle_to_installing_but_its_own_window()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync("/en/");
        await page.EvaluateAsync("navigator.serviceWorker.ready.then(() => true)");
        var session = await page.Context.NewCDPSessionAsync(page);

        var manifest = (await session.SendAsync("Page.getAppManifest"))!.Value;
        Assert.EndsWith("/manifest.webmanifest", manifest.GetProperty("url").GetString(), StringComparison.Ordinal);
        Assert.Empty(manifest.GetProperty("errors").EnumerateArray());
        using var read = JsonDocument.Parse(manifest.GetProperty("data").GetString()!);
        Assert.Equal("Dastan", read.RootElement.GetProperty("name").GetString());

        // What Chromium has against installing. A window of the tests is a private one, which cannot install
        // anything; that is the only obstacle it may name. An installation itself is not observed here.
        var obstacles = (await session.SendAsync("Page.getInstallabilityErrors"))!.Value
            .GetProperty("installabilityErrors").EnumerateArray()
            .Select(error => error.GetProperty("errorId").GetString())
            .ToList();
        Assert.All(obstacles, obstacle => Assert.Equal("in-incognito", obstacle));
    }

    // --- Accessibility, the part a tool can check ---

    public static TheoryData<string, bool, string> Views()
    {
        var views = new TheoryData<string, bool, string>();
        foreach (var language in new[] { "en", "fa" })
        {
            foreach (var dark in new[] { false, true })
            {
                foreach (var view in new[]
                {
                    string.Empty,
                    Story,
                    $"{Story}/entity/character-rostam",
                    $"{Story}/entity/character-rostam/connections?open=character-sohrab",
                    $"{Story}/entity/character-sohrab/family",
                    $"{Story}/story",
                    $"{Story}/gallery",
                    $"{Story}/media/media-dastan-parentage-video",
                    $"{Story}/media/media-dastan-family-illustration",
                    $"{Story}/evidence/relation/relation-02",
                    $"{Story}/source/src-iranica-zal",
                    $"{Story}/search?q=ro",
                    $"{Story}/entity/character-nobody",
                })
                {
                    views.Add(language, dark, view);
                }
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task No_view_breaks_a_rule_of_accessibility_that_a_tool_can_check(string language, bool dark, string view)
    {
        var page = await site.Visitor(width: 412, readTo: 2, dark: dark);
        await page.GotoAsync($"/{language}/{view}");
        await page.EvaluateAsync("document.fonts.ready");

        // The rules of WCAG 2.1 at the levels A and AA that axe can decide: names, roles, contrast, language,
        // headings, landmarks, the structure of lists and tables.
        var result = await page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });

        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));
    }
}
