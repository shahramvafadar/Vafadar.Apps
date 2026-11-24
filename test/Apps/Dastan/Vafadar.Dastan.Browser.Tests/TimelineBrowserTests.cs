using System.Text.Json;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The site with a comparison of the tests beside its own: times of a test on all three axes – from 500 BCE to
/// today with every kind of beginning and end, 10 to 20 thousand and 50 to 60 million years ago, and the steps
/// of a telling.
/// </summary>
public sealed class TimesBrowserSite : BrowserSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-times-" + Guid.NewGuid().ToString("N"), "content");

    /// <inheritdoc />
    protected override void Settings(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Content", _content);
        if (Directory.Exists(_content))
        {
            return;
        }

        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(_content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        File.WriteAllText(Path.Combine(_content, "comparisons", "test-times.json"), Times);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }

    private const string Times =
        """
        {
          "formatVersion": "dastan-comparison-0.1",
          "id": "test-times",
          "status": "draft",
          "note": "Test data of the tests.",
          "frame": "commonEraYears",
          "title": "Times of a test",
          "summary": "Made up for the tests of the timeline.",
          "show": {
            "from": { "frame": "calendar", "era": "beforeCommonEra", "year": 500 },
            "to": { "frame": "calendar", "era": "commonEra", "year": 2026 }
          },
          "publication": { "localDemoAllowed": true, "publicReleaseApproved": false },
          "sources": [],
          "lanes": [
            {
              "key": "realm", "label": "A realm",
              "periods": [
                { "key": "long", "label": "A long rule", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "known", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 500 } },
                    "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 2000 } } } },
                { "key": "unsure", "label": "An unsure rule", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "between", "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 247 }, "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 138 } },
                    "end": { "state": "between", "earliest": { "frame": "calendar", "era": "commonEra", "year": 124 }, "latest": { "frame": "calendar", "era": "commonEra", "year": 226 } } } },
                { "key": "open", "label": "A rule that goes on", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1500 } },
                    "end": { "state": "ongoing" } } },
                { "key": "lost", "label": "A rule with no end stated", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 800 } },
                    "end": { "state": "unknown" } } },
                { "key": "from-nowhere", "label": "A rule with no beginning stated", "sourceIds": [], "reviewStatus": "disputed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "unknown" },
                    "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 400 } } } }
              ],
              "events": [
                { "key": "battle", "label": "A battle", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrenceWindow",
                    "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 331 },
                    "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 231 } } },
                { "key": "crowning", "label": "A crowning", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1000 } } }
              ]
            },
            {
              "key": "earth", "label": "The earth",
              "periods": [
                { "key": "epoch", "label": "An epoch", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "period", "continuity": "notAsserted",
                    "start": { "state": "known", "at": { "frame": "age", "value": 60, "unit": "millionYears", "reference": 1950 } },
                    "end": { "state": "known", "at": { "frame": "age", "value": 50, "unit": "millionYears", "reference": 1950 } } } }
              ],
              "events": [
                { "key": "rock", "label": "A rock is formed", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrence", "at": { "frame": "age", "value": 55, "unit": "millionYears", "reference": 1950 } } },
                { "key": "tools", "label": "Tools are made", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrenceWindow",
                    "earliest": { "frame": "age", "value": 20, "unit": "thousandYears", "reference": 2000 },
                    "latest": { "frame": "age", "value": 10, "unit": "thousandYears", "reference": 2000 } } }
              ]
            },
            {
              "key": "tale", "label": "A tale",
              "periods": [],
              "events": [
                { "key": "tale-begins", "label": "The tale begins", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrence", "at": { "frame": "narrative", "sequence": "a-tale", "order": "presentation", "ordinal": 1 } } },
                { "key": "tale-turns", "label": "The tale turns", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrence", "at": { "frame": "narrative", "sequence": "a-tale", "order": "presentation", "ordinal": 4 } } },
                { "key": "tale-ends", "label": "The tale ends", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "occurrence", "at": { "frame": "narrative", "sequence": "a-tale", "order": "presentation", "ordinal": 9 } } },
                { "key": "nobody-knows", "label": "Nobody knows when", "sourceIds": [], "reviewStatus": "proposed",
                  "time": { "kind": "unknown" } }
              ]
            }
          ]
        }
        """;
}

/// <summary>
/// DastanTimeline in a real browser (step 19.9, ADR 0021): one view for each axis – the calendar from 500 BCE to
/// today, a depth of time with 10 to 20 thousand and 50 to 60 million years on one line, and the order of a
/// telling. A window is drawn as a window, what lasted as a bar with what is known of its ends, an open end as
/// open. The views are moved and made larger and smaller in the browser, the address keeps the view, every mark
/// leads to its row of the table, and the still chart stays where the script does not run.
/// </summary>
public sealed class TimelineBrowserTests(TimesBrowserSite site) : IClassFixture<TimesBrowserSite>
{
    private const string Page = "compare/test-times";

    private static async Task<ILocator> Drawn(IPage page, string address)
    {
        await page.GotoAsync(address);
        var host = page.Locator("dastan-timeline");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 30_000 });
        return host;
    }

    private static ILocator View(ILocator host, string axis) => host.Locator($".tlv-view[data-axis='{axis}']");

    private static ILocator Mark(ILocator host, string key) => host.Locator($"a.tlv-mark[data-key='{key}']");

    private static Task<JsonElement> Seen(ILocator host, int view) => host.EvaluateAsync<JsonElement>("(host, index) => host.views[index].seen", view);

    private static async Task<(double From, double To)> Stretch(ILocator host, int view)
    {
        var seen = await Seen(host, view);
        return (seen.GetProperty("from").GetDouble(), seen.GetProperty("to").GetDouble());
    }

    // Where a place of the axis stands in the drawing, by the stretch the view says it shows.
    private static async Task<double> At(ILocator host, int view, double place)
    {
        var (from, to) = await Stretch(host, view);
        var width = await host.Locator(".tlv").Nth(view).EvaluateAsync<double>("svg => svg.getBoundingClientRect().width");
        return 16 + ((place - from) / (to - from) * (width - 32));
    }

    [Theory]
    [InlineData("en", 1280, false)]
    [InlineData("fa", 360, true)]
    public async Task Each_axis_has_a_view_and_every_statement_a_mark_that_leads_to_its_row(string language, int width, bool dark)
    {
        var page = await site.Visitor(width, dark: dark, graph: true);
        var host = await Drawn(page, $"/{language}/{Page}");

        // The calendar, the depth of time and the order of the telling, each with its own lanes.
        await Assertions.Expect(host.Locator(".tlv-view")).ToHaveCountAsync(3);
        Assert.Equal(["calendar", "depth", "order"], await host.Locator(".tlv-view").EvaluateAllAsync<string[]>("views => views.map(view => view.dataset.axis)"));
        await Assertions.Expect(View(host, "calendar").Locator("a.tlv-mark")).ToHaveCountAsync(7);
        await Assertions.Expect(View(host, "depth").Locator("a.tlv-mark")).ToHaveCountAsync(3);
        await Assertions.Expect(View(host, "order").Locator("a.tlv-mark")).ToHaveCountAsync(3);
        Assert.Equal(1, await View(host, "calendar").Locator(".tlv-lane").CountAsync());

        // The still chart gave its place; what has no place on any axis is named and leads to its row.
        await Assertions.Expect(host.Locator("[data-timeline-still]")).ToBeHiddenAsync();
        await Assertions.Expect(host.Locator("[data-timeline-live] a.chart-unplaced[href$='#item-nobody-knows']")).ToBeVisibleAsync();

        // Every mark is a link to the row of the table that says the same, named by what it is and when.
        foreach (var key in new[] { "long", "unsure", "open", "lost", "from-nowhere", "battle", "crowning", "epoch", "rock", "tools", "tale-begins", "tale-turns", "tale-ends" })
        {
            var mark = Mark(host, key);
            Assert.EndsWith($"/{language}/{Page}#item-{key}", await mark.GetAttributeAsync("href"), StringComparison.Ordinal);
            var row = Regex.Replace(await page.Locator($"#item-{key}").InnerTextAsync(), "\\s+", " ");
            var name = (await mark.GetAttributeAsync("aria-label"))!;
            var (what, when) = (name[..name.IndexOf(": ", StringComparison.Ordinal)], name[(name.IndexOf(": ", StringComparison.Ordinal) + 2)..]);
            Assert.Contains(what, row, StringComparison.Ordinal);
            Assert.Contains(when, row, StringComparison.Ordinal);
        }

        // The words of the axes are those of the page.
        var ticks = string.Join(" | ", await host.Locator(".tlv-tick").AllTextContentsAsync());
        if (language == "fa")
        {
            // A narrow view has room for few marks of its axis: the beginning of the era and the thousands after it.
            Assert.Matches("[۰-۹]+ م( |$)", ticks);
            Assert.Matches("[۰-۹]+ میلیون سال پیش", ticks);
            Assert.Matches("گام [۰-۹]", ticks);
            Assert.DoesNotMatch("[0-9]", ticks);
        }
        else
        {
            Assert.Matches("\\d+ BCE", ticks);
            Assert.Matches("\\d+ million years ago", ticks);
            Assert.Matches("\\d+ thousand years ago", ticks);
            Assert.Matches("step \\d", ticks);
        }

        // The page does not grow sideways, the drawing stays in its frame, and the checks of accessibility pass.
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

        // A mark opens its row.
        await Mark(host, "crowning").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("#item-crowning$"));
    }

    [Fact]
    public async Task What_is_known_is_what_is_drawn()
    {
        var page = await site.Visitor(1280, graph: true);
        var host = await Drawn(page, $"/en/{Page}");

        // A window in which something happened once is a frame from its first year to its last, and no bar.
        var battle = Mark(host, "battle");
        await Assertions.Expect(battle.Locator(".tlv-frame-of-window")).ToHaveCountAsync(1);
        await Assertions.Expect(battle.Locator(".tlv-bar")).ToHaveCountAsync(0);
        var frame = await battle.Locator(".tlv-frame-of-window").EvaluateAsync<double[]>("rect => [rect.x.baseVal.value, rect.width.baseVal.value]");
        Assert.Equal(await At(host, 0, 1 - 331), frame[0], 1.0);
        Assert.Equal(await At(host, 0, 1 - 231 + 1), frame[0] + frame[1], 1.0);

        // What lasted is a bar from its beginning to its end.
        var bar = await Mark(host, "long").Locator(".tlv-bar").EvaluateAsync<double[]>("rect => [rect.x.baseVal.value, rect.width.baseVal.value]");
        Assert.Equal(await At(host, 0, 1 - 500), bar[0], 1.0);
        Assert.Equal(await At(host, 0, 2001), bar[0] + bar[1], 1.0);
        await Assertions.Expect(Mark(host, "long").Locator("[data-open], .tlv-unsure")).ToHaveCountAsync(0);

        // A beginning and an end between two times: each is hatched where it may lie, and only there.
        var unsure = await Mark(host, "unsure").Locator(".tlv-unsure").EvaluateAllAsync<double[][]>("parts => parts.map(rect => [rect.x.baseVal.value, rect.width.baseVal.value])");
        Assert.Equal(2, unsure.Length);
        Assert.Equal(await At(host, 0, 1 - 247), unsure[0][0], 1.0);
        Assert.Equal(await At(host, 0, 1 - 138 + 1), unsure[0][0] + unsure[0][1], 1.0);
        Assert.Equal(await At(host, 0, 124), unsure[1][0], 1.0);
        Assert.Equal(await At(host, 0, 227), unsure[1][0] + unsure[1][1], 1.0);

        // Still going on: an arrow. No end stated: a fading line and a question mark, and no arrow. No
        // beginning stated: the bar comes out of the unknown.
        await Assertions.Expect(Mark(host, "open").Locator("[data-open='ongoing'].tlv-arrow")).ToHaveCountAsync(1);
        await Assertions.Expect(Mark(host, "lost").Locator("[data-open='unknown']")).ToHaveCountAsync(1);
        await Assertions.Expect(Mark(host, "lost").Locator(".tlv-arrow")).ToHaveCountAsync(0);
        await Assertions.Expect(Mark(host, "lost").Locator(".tlv-question")).ToHaveTextAsync("?");
        await Assertions.Expect(Mark(host, "from-nowhere").Locator("[data-open='start']")).ToHaveCountAsync(1);
        await Assertions.Expect(Mark(host, "from-nowhere")).ToHaveClassAsync(new Regex("tlv-disputed"));

        // A depth of time: 55 million years stand to the left of ten thousand, on one line; the window of the
        // tools is a frame, the epoch a bar from 60 to 50 million years.
        var rock = await Mark(host, "rock").Locator(".tlv-point").EvaluateAsync<double>("point => point.cx.baseVal.value");
        var tools = await Mark(host, "tools").Locator(".tlv-frame-of-window").EvaluateAsync<double[]>("rect => [rect.x.baseVal.value, rect.width.baseVal.value]");
        var epoch = await Mark(host, "epoch").Locator(".tlv-bar").EvaluateAsync<double[]>("rect => [rect.x.baseVal.value, rect.width.baseVal.value]");
        Assert.True(epoch[0] < rock && rock < epoch[0] + epoch[1], "The rock of 55 million years lies in the epoch of 60 to 50 million.");
        Assert.True(epoch[0] + epoch[1] < tools[0], "Millions of years stand before thousands.");
        Assert.True(tools[1] > 20, "The window of ten thousand years has a width of its own.");

        // The order of a telling: the steps in their order, with no year anywhere.
        var steps = new List<double>();
        foreach (var key in new[] { "tale-begins", "tale-turns", "tale-ends" })
        {
            steps.Add(await Mark(host, key).Locator(".tlv-point").EvaluateAsync<double>("point => point.cx.baseVal.value"));
        }

        Assert.True(steps[0] < steps[1] && steps[1] < steps[2]);
        Assert.DoesNotMatch("CE|years ago", string.Join(' ', await View(host, "order").Locator(".tlv-tick").AllTextContentsAsync()));

        // No name of a mark lies on another name of its view.
        var overlaps = await host.EvaluateAsync<string[]>(
            @"host => {
                const found = [];
                for (const view of host.querySelectorAll('.tlv')) {
                    const names = [...view.querySelectorAll('.tlv-name')].map(name => [name.textContent, name.getBoundingClientRect()]);
                    for (let one = 0; one < names.length; one++) {
                        for (let other = one + 1; other < names.length; other++) {
                            const [a, b] = [names[one][1], names[other][1]];
                            if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) found.push(names[one][0] + ' / ' + names[other][0]);
                        }
                    }
                }
                return found;
            }");
        Assert.Empty(overlaps);
    }

    [Fact]
    public async Task A_view_is_moved_and_made_larger_and_smaller_in_the_browser_and_the_address_keeps_it()
    {
        var page = await site.Visitor(1280, graph: true);
        var asked = new List<string>();
        var host = await Drawn(page, $"/en/{Page}");
        page.Request += (_, request) => asked.Add(request.Url);
        var whole = await Stretch(host, 0);
        var frame = View(host, "calendar").Locator("[data-view-frame]");

        // The keys of the view: larger, to the right, and all of it again.
        await frame.FocusAsync();
        await page.Keyboard.PressAsync("+");
        var larger = await Stretch(host, 0);
        Assert.True(larger.To - larger.From < (whole.To - whole.From) * 0.7);
        await page.Keyboard.PressAsync("ArrowRight");
        var moved = await Stretch(host, 0);
        Assert.True(moved.From > larger.From);
        Assert.Equal(larger.To - larger.From, moved.To - moved.From, 0.001);

        // The address says the stretch in years, as the still chart reads it.
        var address = new Uri(page.Url).Query;
        var years = Regex.Match(address, "from=(-?\\d+)&to=(-?\\d+)");
        Assert.True(years.Success, address);

        // The buttons do the same; the mouse moves the view; the key for it and the wheel make it larger.
        await View(host, "calendar").Locator("[data-view-out]").ClickAsync();
        Assert.True((await Stretch(host, 0)).To - (await Stretch(host, 0)).From > moved.To - moved.From);
        await View(host, "calendar").Locator("[data-view-earlier]").ClickAsync();
        var before = await Stretch(host, 0);
        var box = (await frame.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + 400, box.Y + 20);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + 300, box.Y + 20, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        Assert.True((await Stretch(host, 0)).From > before.From, "Pulling to the left shows what is later.");
        before = await Stretch(host, 0);
        await page.Keyboard.DownAsync("Control");
        await page.Mouse.WheelAsync(0, -200);
        await page.Keyboard.UpAsync("Control");
        await page.WaitForFunctionAsync("span => { const seen = document.querySelector('dastan-timeline').views[0].seen; return seen.to - seen.from < span; }", before.To - before.From);

        // The other views are moved by themselves, each under its own name in the address.
        var deep = await Stretch(host, 1);
        await View(host, "depth").Locator("[data-view-in]").ClickAsync();
        var deeper = await Stretch(host, 1);
        Assert.True(deeper.To - deeper.From < deep.To - deep.From);
        Assert.Contains("v1from=", page.Url, StringComparison.Ordinal);
        await View(host, "depth").Locator("[data-view-whole]").ClickAsync();
        Assert.Equal(deep.From, (await Stretch(host, 1)).From, 0.0001);
        Assert.DoesNotContain("v1from=", page.Url, StringComparison.Ordinal);

        // All of it – and nothing of all this asked the server for anything.
        await frame.FocusAsync();
        await page.Keyboard.PressAsync("0");
        Assert.Equal(whole.From, (await Stretch(host, 0)).From, 0.0001);
        Assert.DoesNotContain("from=", page.Url, StringComparison.Ordinal);
        Assert.Empty(asked);

        // An address with a stretch opens the view at it, and the still chart of the same address shows the same years.
        host = await Drawn(page, $"/en/{Page}?from=-300&to=300");
        var chosen = await Stretch(host, 0);
        Assert.Equal((1 - 300, 301), (chosen.From, chosen.To));
        await host.Locator("[data-timeline-to-still]").ClickAsync();
        await Assertions.Expect(host.Locator("[data-timeline-still]")).ToContainTextAsync("Shown: 300 BCE to 300 CE");
        await host.Locator("[data-timeline-to-live]").ClickAsync();
        await Assertions.Expect(View(host, "calendar").Locator(".tlv")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_still_chart_that_was_chosen_stays_when_one_of_its_tools_asks_for_the_page_again()
    {
        var page = await site.Visitor(1280, graph: true);
        var host = await Drawn(page, $"/en/{Page}");
        await host.Locator("[data-timeline-to-still]").ClickAsync();
        Assert.Contains("still=1", page.Url, StringComparison.Ordinal);

        // A tool of the still chart is a link that asks the server for the page again: the still chart stays.
        await host.Locator("[data-timeline-still] a[href*='view=down']").First.ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("view=down", StringComparison.Ordinal));
        host = page.Locator("dastan-timeline");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "still", new() { Timeout = 30_000 });
        await Assertions.Expect(host.Locator("[data-timeline-live]")).ToBeHiddenAsync();
        await Assertions.Expect(host.Locator("[data-timeline-to-live]")).ToBeVisibleAsync();

        // Choosing the moving chart again takes the choice out of the address and out of the tools.
        await host.Locator("[data-timeline-to-live]").ClickAsync();
        Assert.DoesNotContain("still=1", page.Url, StringComparison.Ordinal);
        Assert.DoesNotContain("still=1", await host.Locator("[data-timeline-still] a[href*='view=']").First.GetAttributeAsync("href") ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_the_script_the_still_chart_is_the_chart()
    {
        var without = await site.Visitor(360, script: false);
        await without.GotoAsync($"/en/{Page}");
        await Assertions.Expect(without.Locator("dastan-timeline [data-timeline-live]")).ToBeHiddenAsync();
        await Assertions.Expect(without.Locator("dastan-timeline [data-timeline-still] .stage:visible").First).ToBeVisibleAsync();
        await Assertions.Expect(without.Locator("#item-rock")).ToContainTextAsync("55 million years before the present");

        // A page without a timeline does not ask for its script.
        var plain = await site.Visitor(360, graph: true);
        var asked = new List<string>();
        plain.Request += (_, request) => asked.Add(request.Url);
        await plain.GotoAsync($"/en/{BrowserSite.Story}");
        await plain.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain(asked, address => Regex.IsMatch(address, "/(timeline|graph)[^/]*\\.js$"));
    }
}
