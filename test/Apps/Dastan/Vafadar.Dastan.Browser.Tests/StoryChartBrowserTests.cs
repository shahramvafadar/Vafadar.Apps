using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The story as a chart in a real browser (ADR 0008, D-040): the stage and its sides, what scrolls and what stays,
/// whether names and marks have the room they need.
/// </summary>
public sealed class StoryChartBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Address = $"{BrowserSite.Story}/story";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    private static Task Arrived(IPage page, Regex address) => BrowserSite.Arrived(page, address);

    [Theory]
    [InlineData(1280, "en")]
    [InlineData(390, "fa")]
    public async Task The_chart_has_the_page_and_two_steps_at_each_of_its_four_sides(int width, string language)
    {
        var page = await site.Visitor(width, readTo: 4);
        await page.GotoAsync($"/{language}/{Address}");

        var sizes = await page.EvaluateAsync<double[]>(
            """
            () => {
                const stage = document.querySelector('.stage').getBoundingClientRect();
                const steps = ['top', 'left', 'right', 'bottom'].map(side => document.querySelectorAll('.stage-' + side + ' .stage-step').length);
                const small = Math.min(...[...document.querySelectorAll('.stage-step')].map(step => { const box = step.getBoundingClientRect(); return Math.min(box.width, box.height); }));
                return [stage.left, innerWidth - stage.right, stage.height, innerHeight, small, ...steps];
            }
            """);

        Assert.InRange(sizes[0], 0, 17);
        Assert.InRange(sizes[1], 0, 17);
        Assert.InRange(sizes[2], sizes[3] - 33, sizes[3]);
        Assert.True(sizes[4] >= 43.5, $"A step is {sizes[4]} px.");
        Assert.Equal([2, 2, 2, 2], sizes[5..]);
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        // The telling begins where reading begins: the earlier steps are closed at that side.
        var earlier = language == "fa" ? "گام‌های پیش‌تر" : "Earlier steps";
        await Assertions.Expect(page.Locator(language == "fa" ? ".stage-right .stage-label" : ".stage-left .stage-label")).ToHaveTextAsync(earlier);
    }

    [Fact]
    public async Task On_a_phone_the_steps_scroll_in_the_stage_and_the_names_stay_in_sight()
    {
        var page = await site.Visitor(width: 390, readTo: 4);
        await page.GotoAsync($"/en/{Address}");
        var frame = page.Locator(".stage-frame");

        Assert.True(await frame.EvaluateAsync<bool>("frame => frame.scrollWidth > frame.clientWidth + 100"));
        var names = await page.EvaluateAsync<double>("document.querySelector('.sc-names').getBoundingClientRect().left");
        var head = page.Locator(".sc-head").First;
        var before = (await head.BoundingBoxAsync())!.X;

        await frame.EvaluateAsync("frame => frame.scrollLeft = 150");

        Assert.Equal(before - 150, (await head.BoundingBoxAsync())!.X, precision: 0);
        Assert.Equal(names, await page.EvaluateAsync<double>("document.querySelector('.sc-names').getBoundingClientRect().left"), precision: 0);
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways after the frame was scrolled.");

        // On a Persian page the chart begins at the right: the first step is in sight there, beside the names.
        var persian = await site.Visitor(width: 390, readTo: 4);
        await persian.GotoAsync($"/fa/{Address}");
        var first = await persian.EvaluateAsync<bool>(
            """
            () => {
                const frame = document.querySelector('.stage-frame').getBoundingClientRect();
                const names = document.querySelector('.sc-names').getBoundingClientRect();
                const head = document.querySelector(".sc-head[href$='#step-1']").getBoundingClientRect();
                // On a phone the frame can be a little narrower than the names and one step: most of the step is in sight.
                return names.right <= frame.right + 1 && names.right > frame.right - 12 && head.right <= names.left + 1 && head.left >= frame.left - head.width / 2;
            }
            """);
        Assert.True(first, "The first step of a Persian chart is not beside the names at the right.");
    }

    [Fact]
    public async Task A_minus_at_a_side_closes_a_step_and_the_chart_stays_on_the_screen_and_Back_undoes_it()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Address}");
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(8);

        await page.Locator(".stage-right").GetByRole(AriaRole.Link, new() { Name = "Later steps: close a step" }).ClickAsync();
        await Arrived(page, new Regex("story\\?to=7#chart$"));
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(7);

        // The step led to the stage, not to the top of the page.
        await Assertions.Expect(page.Locator(".stage")).ToBeInViewportAsync(new() { Ratio = 0.9f });

        await page.Locator(".stage-left").GetByRole(AriaRole.Link, new() { Name = "Earlier steps: close a step" }).ClickAsync();
        await Arrived(page, new Regex("story\\?from=2&to=7#chart$"));
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(6);

        await page.Locator(".stage-left").GetByRole(AriaRole.Link, new() { Name = "Earlier steps: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("story\\?to=7#chart$"));
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(7);

        await page.GoBackAsync();
        await Arrived(page, new Regex("story\\?from=2&to=7#chart$"));
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(6);
    }

    [Fact]
    public async Task The_names_of_the_steps_are_closed_to_their_numbers_and_the_rows_from_the_last_one()
    {
        var page = await site.Visitor(width: 412, readTo: 4);
        await page.GotoAsync($"/en/{Address}");
        await Assertions.Expect(page.Locator(".sc-title")).ToHaveCountAsync(8);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Names of the steps: close a step" }).ClickAsync();
        await Arrived(page, new Regex("heads=numbers#chart$"));
        await Assertions.Expect(page.Locator(".sc-title")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(8);

        await page.Locator(".stage-bottom").GetByRole(AriaRole.Link, new() { Name = "Names: close a step" }).ClickAsync();
        await Arrived(page, new Regex("lanes=11&heads=numbers#chart$"));
        await Assertions.Expect(page.Locator(".sc-name")).ToHaveCountAsync(11);
    }

    [Fact]
    public async Task A_mark_leads_to_its_step_of_the_story()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Address}");

        await page.Locator(".sc-mark[title^='Tahmineh: takes part – step 3']").ClickAsync();
        await Arrived(page, new Regex("story#step-3$"));
        await Assertions.Expect(page.Locator("#step-3")).ToBeInViewportAsync();
        await Assertions.Expect(page.Locator("#step-3 h2")).ToHaveTextAsync("Sohrab learns his lineage");
    }

    public static TheoryData<int, string, string> Views()
    {
        var views = new TheoryData<int, string, string>();
        foreach (var width in new[] { 360, 412, 1280 })
        {
            foreach (var language in new[] { "en", "fa" })
            {
                foreach (var query in new[] { string.Empty, "?heads=numbers", "?from=2&to=5&lanes=4" })
                {
                    views.Add(width, language, query);
                }
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task The_chart_fits_the_window_and_its_names_and_marks_have_the_room_they_need(int width, string language, string query)
    {
        var page = await site.Visitor(width, readTo: 4);
        await page.GotoAsync($"/{language}/{Address}{query}");
        await page.EvaluateAsync("document.fonts.ready");

        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        var problems = await page.EvaluateAsync<string[]>(
            """
            () => {
                const problems = [];
                for (const element of document.querySelectorAll('main *')) {
                    if (element.closest('.stage-frame') || element.closest('.lanes') || element.closest('.visually-hidden') || element.classList.contains('visually-hidden') || element.getClientRects().length === 0) continue;
                    const box = element.getBoundingClientRect();
                    if (box.width > 0 && (box.right > innerWidth + 1 || box.left < -1)) problems.push('outside the window: ' + element.tagName + '.' + element.getAttribute('class'));
                }

                // A name of a row and a head of a column are whole inside their places.
                for (const kind of ['.sc-name', '.sc-head']) {
                    for (const text of document.querySelectorAll(kind)) {
                        const place = text.parentElement.getBoundingClientRect();
                        if (text.scrollHeight > place.height + 1 || text.scrollWidth > place.width + 1) problems.push('cut: ' + text.textContent.trim());
                    }
                }

                // A mark, a name and a head are as large as a finger needs.
                for (const target of document.querySelectorAll('.sc-mark, .sc-name, .sc-head')) {
                    const box = target.getBoundingClientRect();
                    if (box.width < 43.5 || box.height < 43.5) problems.push('too small: ' + target.textContent.trim());
                }

                // A mark stands in the column of its step and in the row of its name.
                const heads = [...document.querySelectorAll('.sc-head')].map(head => [head.getAttribute('href').split('#')[1], head.getBoundingClientRect()]);
                for (const mark of document.querySelectorAll('.sc-mark')) {
                    const box = mark.getBoundingClientRect();
                    const head = heads.find(([step]) => step === mark.getAttribute('href').split('#')[1]);
                    if (!head || Math.abs(head[1].left - box.left) > 1 || Math.abs(head[1].right - box.right) > 1) problems.push('not in its column: ' + mark.getAttribute('title'));
                }
                return problems;
            }
            """);
        Assert.Empty(problems);

        if (language == "fa")
        {
            Assert.True(await page.EvaluateAsync<bool>("document.fonts.check('16px Vazirmatn', 'رستم')"));
        }
    }

    public static TheoryData<int, string, bool, string> Checked()
    {
        var views = new TheoryData<int, string, bool, string>();
        foreach (var language in new[] { "en", "fa" })
        {
            foreach (var dark in new[] { false, true })
            {
                views.Add(412, language, dark, string.Empty);
                views.Add(1280, language, dark, "?heads=numbers&from=2");
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Checked))]
    public async Task The_chart_breaks_no_rule_of_accessibility_that_a_tool_can_check(int width, string language, bool dark, string query)
    {
        var page = await site.Visitor(width, readTo: 4, dark: dark);
        await page.GotoAsync($"/{language}/{Address}{query}");
        await page.EvaluateAsync("document.fonts.ready");

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
    public async Task Without_a_script_the_chart_and_its_sides_work_the_same()
    {
        var page = await site.Visitor(width: 1280, readTo: 4, script: false);
        await page.GotoAsync($"/fa/{Address}");

        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(8);
        await page.Locator(".stage-left").GetByRole(AriaRole.Link, new() { Name = "گام‌های پس‌تر: یک گام بسته‌تر" }).ClickAsync();
        await Arrived(page, new Regex("to=7#chart$"));
        await Assertions.Expect(page.Locator(".sc-head")).ToHaveCountAsync(7);
    }
}
