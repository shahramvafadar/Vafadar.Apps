using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The chart of a comparison in a real browser (ADR 0008): what the screen decides, what scrolls, what a pointer and
/// the keyboard reach, and whether the texts have the room the layout gave them.
/// </summary>
public sealed class ChartTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Address = "compare/iran-china-rome";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    private static Task<double> PlotWidth(IPage page) =>
        page.EvaluateAsync<double>("document.querySelector('.tl-plot').getBoundingClientRect().width");

    private static Task Arrived(IPage page, Regex address) => BrowserSite.Arrived(page, address);

    [Fact]
    public async Task On_a_wide_screen_the_time_runs_across_the_page_and_scrolls_in_a_frame_of_its_own()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}");

        await Assertions.Expect(page.Locator(".tl-across")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".tl-down")).ToBeHiddenAsync();
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        // The time is longer than the frame; the names of the lanes stand still while it moves.
        var frame = page.Locator(".stage-frame:visible");
        Assert.True(await frame.EvaluateAsync<bool>("frame => frame.scrollWidth > frame.clientWidth + 100"));
        var names = await page.EvaluateAsync<double>("document.querySelector('.tl-names').getBoundingClientRect().left");
        var bar = page.Locator(".tl-across a.tl-bar[href$='#item-achaemenid']");
        var before = (await bar.BoundingBoxAsync())!.X;

        await frame.EvaluateAsync("frame => frame.scrollLeft = 100");

        Assert.Equal(before - 100, (await bar.BoundingBoxAsync())!.X, precision: 0);
        Assert.Equal(names, await page.EvaluateAsync<double>("document.querySelector('.tl-names').getBoundingClientRect().left"));
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways after the frame was scrolled.");
    }

    [Fact]
    public async Task The_keyboard_reaches_the_frame_of_the_time_and_scrolls_it()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}?view=across");
        var frame = page.Locator(".stage-frame:visible");

        for (var stops = 0; stops < 40 && !await frame.EvaluateAsync<bool>("frame => document.activeElement === frame"); stops++)
        {
            await page.Keyboard.PressAsync("Tab");
        }

        Assert.True(await frame.EvaluateAsync<bool>("frame => document.activeElement === frame"), "The keyboard does not reach the frame of the time.");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        // How far an arrow key moves a frame is the browser's own matter: 40 px in Chromium, more in the others.
        await page.WaitForFunctionAsync("() => document.querySelector('.stage-frame:not([hidden])') && [...document.querySelectorAll('.stage-frame')].some(frame => frame.scrollLeft >= 80)");
    }

    [Theory]
    [InlineData(360)]
    [InlineData(412)]
    public async Task On_a_phone_the_time_runs_down_the_page_and_the_names_of_the_lanes_stay_at_its_top(int width)
    {
        var page = await site.Visitor(width);
        await page.GotoAsync($"/en/{Address}");

        await Assertions.Expect(page.Locator(".tl-down")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".tl-across")).ToBeHiddenAsync();
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        // Every column of the chart is as high as the others: a line at one time meets the line beside it.
        var heights = await page.EvaluateAsync<double[]>("[...document.querySelectorAll('.tl-body > svg')].map(column => column.getBoundingClientRect().height)");
        Assert.Equal(5, heights.Length);
        Assert.Single(heights.Distinct());

        // The time scrolls inside the stage; the names of the lanes stay at the top of its frame.
        var frame = page.Locator(".stage-frame:visible");
        Assert.True(await frame.EvaluateAsync<bool>("frame => frame.scrollHeight > frame.clientHeight + 100"));
        await frame.EvaluateAsync("frame => frame.scrollTop = 500");

        var head = await page.EvaluateAsync<double>("document.querySelector('.tl-head').getBoundingClientRect().top - document.querySelector('.stage.only-narrow .stage-frame').getBoundingClientRect().top");
        Assert.Equal(0, head, precision: 0);
        await Assertions.Expect(page.Locator(".tl-head")).ToContainTextAsync("Events");
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways after the frame was scrolled.");
    }

    [Fact]
    public async Task A_choice_of_the_visitor_holds_on_every_screen()
    {
        var phone = await site.Visitor(width: 360);
        await phone.GotoAsync($"/en/{Address}");
        await phone.GetByRole(AriaRole.Link, new() { Name = "Across the page" }).ClickAsync();
        await Arrived(phone, new Regex("view=across$"));

        await Assertions.Expect(phone.Locator(".tl-across")).ToBeVisibleAsync();
        Assert.Equal(0, await phone.Locator(".tl-down").CountAsync());
        Assert.False(await ScrollsSideways(phone), "The page scrolls sideways.");

        var desk = await site.Visitor(width: 1280);
        await desk.GotoAsync($"/en/{Address}?view=down");

        await Assertions.Expect(desk.Locator(".tl-down")).ToBeVisibleAsync();
        Assert.Equal(0, await desk.Locator(".tl-across").CountAsync());
    }

    [Fact]
    public async Task A_bar_and_a_callout_lead_to_their_rows_in_the_table_and_Back_returns_to_the_chart()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}");

        await page.Locator(".tl-across a.tl-bar[href$='#item-achaemenid']").ClickAsync();
        await Arrived(page, new Regex("#item-achaemenid$"));
        await Assertions.Expect(page.Locator("#item-achaemenid")).ToBeInViewportAsync();
        await Assertions.Expect(page.Locator("#item-achaemenid")).ToContainTextAsync("550 BCE – 330 BCE");

        // The row that was led to is marked, by more than a colour: it has an outline.
        Assert.NotEqual("none", await page.EvaluateAsync<string>("getComputedStyle(document.querySelector('#item-achaemenid')).outlineStyle"));

        await page.GoBackAsync();
        await Arrived(page, new Regex($"{Address}$"));

        await page.Locator(".tl-across a.tl-callout[href$='#item-gaugamela']").ClickAsync();
        await Arrived(page, new Regex("#item-gaugamela$"));
        await Assertions.Expect(page.Locator("#item-gaugamela")).ToBeInViewportAsync();
    }

    [Fact]
    public async Task Opening_further_makes_the_time_longer_and_Back_undoes_it()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}?view=across");
        var before = await PlotWidth(page);

        await page.GetByRole(AriaRole.Link, new() { Name = "Open further" }).ClickAsync();
        await Arrived(page, new Regex("scale=3$"));
        var opened = await PlotWidth(page);

        // Twice the scale: twice the years, and the same room at both ends.
        Assert.True(opened > before * 1.8 && opened < before * 2, $"{before} became {opened}.");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Across the page" })).ToHaveAttributeAsync("aria-current", "true");

        await page.GoBackAsync();
        await Arrived(page, new Regex("view=across$"));
        Assert.Equal(before, await PlotWidth(page), precision: 0);
    }

    [Fact]
    public async Task Moving_to_an_earlier_time_shows_what_was_before_the_chart_began()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}?view=across");
        Assert.Equal(0, await page.Locator(".tl-across a.tl-bar[href$='#item-roman-kingdom'] .tl-year").CountAsync());

        await page.GetByRole(AriaRole.Link, new() { Name = "Everything in it" }).ClickAsync();
        await Arrived(page, new Regex("from=-770&to=1453$"));

        // The whole of the first and of the last period is there now, with the years they begin and end in.
        await Assertions.Expect(page.GetByText("Shown: 770 BCE to 1453 CE")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".tl-across a.tl-bar[href$='#item-eastern-rome']")).Not.ToHaveClassAsync(new Regex("tl-cut"));
        await Assertions.Expect(page.Locator(".tl-across a.tl-bar[href$='#item-eastern-rome'] .tl-year").Last).ToHaveTextAsync("1453 CE");
    }

    [Theory]
    [InlineData(1280, "en")]
    [InlineData(1280, "fa")]
    [InlineData(390, "en")]
    public async Task The_stage_takes_the_width_of_the_page_and_the_height_of_the_window(int width, string language)
    {
        var page = await site.Visitor(width);
        await page.GotoAsync($"/{language}/{Address}");

        var sizes = await page.EvaluateAsync<double[]>(
            """
            () => {
                const stage = [...document.querySelectorAll('.stage')].find(candidate => candidate.getClientRects().length > 0).getBoundingClientRect();
                const text = document.querySelector('main h1').getBoundingClientRect();
                return [stage.left, innerWidth - stage.right, stage.height, innerHeight, text.width];
            }
            """);

        // Not a column in the middle of the page: at most the margin of the page is left at each side, and the
        // stage is as high as the window is, less that margin. What is read keeps the width of a column of text.
        Assert.InRange(sizes[0], 0, 17);
        Assert.InRange(sizes[1], 0, 17);
        Assert.InRange(sizes[2], sizes[3] - 33 - (width <= 640 ? 60 : 0), sizes[3] - (width <= 640 ? 60 : 0));
        Assert.True(sizes[4] <= 704.5, $"The heading is {sizes[4]} px wide.");
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task Each_of_the_four_sides_of_the_stage_has_its_two_steps_where_the_chart_grows(string language)
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/{language}/{Address}?view=across");

        var sides = await page.EvaluateAsync<double[][]>(
            """
            () => {
                const frame = document.querySelector('.stage-frame').getBoundingClientRect();
                return ['top', 'left', 'right', 'bottom'].map(side => {
                    const edge = document.querySelector('.stage-' + side);
                    const box = edge.getBoundingClientRect();
                    const steps = [...edge.querySelectorAll('.stage-step')].map(step => step.getBoundingClientRect());
                    return [box.left - frame.left, box.right - frame.right, box.top - frame.top, box.bottom - frame.bottom, steps.length, Math.min(...steps.map(step => Math.min(step.width, step.height)))];
                });
            }
            """);

        // Above, left of, right of and below the frame – on a Persian page as well – each with a "−" and a "+" that
        // a finger can hit.
        Assert.True(sides[0][2] < 0);
        Assert.True(sides[1][1] < 0 && sides[1][0] < 0);
        Assert.True(sides[2][0] > 0 && sides[2][1] > 0);
        Assert.True(sides[3][3] > 0);
        Assert.All(sides, side => Assert.Equal(2, side[4]));
        Assert.All(sides, side => Assert.True(side[5] >= 43.5, $"A step is {side[5]} px."));

        // The time begins at the left in both languages: the earlier years are opened at the left side.
        var earlier = language == "fa" ? "سال‌های پیش‌تر" : "Earlier years";
        await Assertions.Expect(page.Locator(".stage-left .stage-label")).ToHaveTextAsync(earlier);
    }

    [Fact]
    public async Task A_plus_at_a_side_opens_the_time_there_and_a_minus_closes_it_and_Back_undoes_it()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}?view=across");
        var before = await PlotWidth(page);

        await page.Locator(".stage-left").GetByRole(AriaRole.Link, new() { Name = "Earlier years: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("from=-700&to=700#chart-across$"));
        await Assertions.Expect(page.GetByText("Shown: 700 BCE to 700 CE")).ToBeVisibleAsync();
        var opened = await PlotWidth(page);
        Assert.Equal(before + (100 * 0.06 * 15), opened, precision: 0);

        await page.Locator(".stage-right").GetByRole(AriaRole.Link, new() { Name = "Later years: close a step" }).ClickAsync();
        await Arrived(page, new Regex("from=-700&to=600#chart-across$"));
        Assert.Equal(before, await PlotWidth(page), precision: 0);

        await page.GoBackAsync();
        await page.GoBackAsync();
        await Arrived(page, new Regex("view=across$"));
        Assert.Equal(before, await PlotWidth(page), precision: 0);
        await Assertions.Expect(page.GetByText("Shown: 600 BCE to 700 CE")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_events_and_the_lanes_are_closed_and_opened_at_their_sides()
    {
        var page = await site.Visitor(width: 1280);
        await page.GotoAsync($"/en/{Address}?view=across");
        await Assertions.Expect(page.Locator(".tl-callout")).ToHaveCountAsync(6);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Events: close a step" }).ClickAsync();
        await Arrived(page, new Regex("events=off#chart-across$"));
        await Assertions.Expect(page.Locator(".tl-callout")).ToHaveCountAsync(0);

        await page.Locator(".stage-bottom").GetByRole(AriaRole.Link, new() { Name = "Subjects: close a step" }).ClickAsync();
        await Arrived(page, new Regex("events=off&lanes=2#chart-across$"));
        await Assertions.Expect(page.Locator(".tl-names .tl-lane-name")).ToHaveTextAsync(["China", "Iran"]);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Events: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("view=across&lanes=2#chart-across$"));

        // The event of Rome is closed with its lane.
        await Assertions.Expect(page.Locator(".tl-callout")).ToHaveCountAsync(5);
    }

    public static TheoryData<int, string, string> Views()
    {
        var views = new TheoryData<int, string, string>();
        foreach (var width in new[] { 360, 412, 1280 })
        {
            foreach (var language in new[] { "en", "fa" })
            {
                foreach (var query in new[] { string.Empty, "?view=across", "?view=down", "?view=across&scale=5&from=-300&to=300", "?view=down&scale=4" })
                {
                    views.Add(width, language, query);
                }
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task A_chart_fits_the_window_and_its_texts_have_the_room_the_layout_gave_them(int width, string language, string query)
    {
        var page = await site.Visitor(width);
        await page.GotoAsync($"/{language}/{Address}{query}");
        await page.EvaluateAsync("document.fonts.ready");

        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");
        Assert.Equal("ltr", await page.Locator(".tl").First.GetAttributeAsync("dir"));

        // Nothing of the page lies outside the window, except what scrolls inside the frame of the time.
        var outside = await page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('main *')]
                .filter(element => !element.closest('.stage-frame') && element.getClientRects().length > 0)
                .filter(element => { const box = element.getBoundingClientRect(); return box.width > 0 && (box.right > innerWidth + 1 || box.left < -1); })
                .map(element => element.tagName + '.' + element.getAttribute('class'))
            """);
        Assert.Empty(outside);

        // The name of a bar that carries one is whole, the words of a callout are whole, and no two bars or two
        // callouts of the chart that is shown lie on each other.
        var drawing = await page.EvaluateAsync<string[]>(
            """
            () => {
                const shown = [...document.querySelectorAll('.tl')].find(chart => chart.getClientRects().length > 0);
                const problems = [];
                for (const name of shown.querySelectorAll('.tl-bar .tl-name')) {
                    if (name.scrollWidth > name.clientWidth + 1 || name.scrollHeight > name.clientHeight + 1) problems.push('name cut: ' + name.textContent);
                }
                for (const bar of shown.querySelectorAll('a.tl-bar')) {
                    if (bar.scrollWidth > bar.clientWidth + 1 || bar.scrollHeight > bar.clientHeight + 1) problems.push('bar too small: ' + bar.textContent.trim());
                }
                for (const callout of shown.querySelectorAll('.tl-callout')) {
                    if (callout.scrollWidth > callout.clientWidth + 1 || callout.scrollHeight > callout.clientHeight + 1) problems.push('callout cut: ' + callout.textContent);
                }
                for (const kind of ['.tl-bar', '.tl-callout']) {
                    const frames = [...shown.querySelectorAll(kind)].map(element => [element.getAttribute('title'), element.parentElement.getBoundingClientRect()]);
                    for (let i = 0; i < frames.length; i++) {
                        for (let j = i + 1; j < frames.length; j++) {
                            const a = frames[i][1], b = frames[j][1];
                            if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) problems.push(frames[i][0] + ' lies on ' + frames[j][0]);
                        }
                    }
                }
                // A year of the axis stands at its line, and no callout lies on a year.
                const across = shown.classList.contains('tl-across');
                const years = [];
                for (const mark of shown.querySelectorAll('g')) {
                    const line = mark.querySelector('line'), year = mark.querySelector('text');
                    if (!line || !year) continue;
                    const at = line.getBoundingClientRect(), words = year.getBoundingClientRect();
                    years.push([year.textContent, words]);
                    const off = across ? Math.abs((words.left + words.right) / 2 - at.left) : Math.abs((words.top + words.bottom) / 2 - at.top);
                    if (off > 4) problems.push('year away from its line by ' + Math.round(off) + ': ' + year.textContent);
                }
                if (years.length < 2) problems.push('no years on the axis');
                for (const callout of shown.querySelectorAll('.tl-callout')) {
                    const a = callout.parentElement.getBoundingClientRect();
                    for (const [name, b] of years) {
                        if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) problems.push(callout.textContent + ' lies on the year ' + name);
                    }
                }
                return problems;
            }
            """);
        Assert.Empty(drawing);

        if (language == "fa")
        {
            Assert.True(await page.EvaluateAsync<bool>("document.fonts.check('16px Vazirmatn', 'ایران')"));
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
                views.Add(1280, language, dark, string.Empty);
                views.Add(412, language, dark, "?view=across&scale=4");
                views.Add(1280, language, dark, "?view=down&from=-300&to=300");
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Checked))]
    public async Task A_chart_breaks_no_rule_of_accessibility_that_a_tool_can_check(int width, string language, bool dark, string query)
    {
        var page = await site.Visitor(width, dark: dark);
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
    public async Task Without_a_script_the_chart_and_its_choices_work_the_same()
    {
        var page = await site.Visitor(width: 1280, script: false);
        await page.GotoAsync($"/fa/{Address}");

        await Assertions.Expect(page.Locator(".tl-across")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "عمودی" }).ClickAsync();
        await Arrived(page, new Regex("view=down$"));
        await Assertions.Expect(page.Locator(".tl-down")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".tl-down .tl-callout").First).ToContainTextAsync("۳۳۱ پ.م – نبرد گوگمل");
    }
}
