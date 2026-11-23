using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The family tree in a real browser (ADR 0008): what scrolls, what the links that open a generation do, how the
/// tree is drawn in both directions and both languages.
/// </summary>
public sealed class FamilyTreeBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Sohrab = $"{BrowserSite.Story}/entity/character-sohrab/family";
    private const string Zal = $"{BrowserSite.Story}/entity/character-zal/family";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    private static Task Arrived(IPage page, Regex address) => BrowserSite.Arrived(page, address);

    // Two places of the drawing are the same place when they lie less than a pixel apart. Rounding both and
    // comparing would fail where one lies a hair below a half and the other on it (seen in Firefox: 1002.4917
    // and 1002.5000, when a part above the drawing moved it by a part of a pixel).
    private static void Same(double expected, double actual) =>
        Assert.True(Math.Abs(expected - actual) < 1, $"{expected} and {actual} are a pixel or more apart.");

    // The box of a member inside the drawing, as the window shows it.
    private static async Task<(double Left, double Top, double Right, double Bottom)> Box(IPage page, string member)
    {
        var box = await page.EvaluateAsync<double[]>(
            "member => { const b = document.querySelector(`.tree a.graph-name[href$='/entity/${member}']`).closest('foreignObject').getBoundingClientRect(); return [b.left, b.top, b.right, b.bottom]; }",
            member);
        return (box[0], box[1], box[2], box[3]);
    }

    [Fact]
    public async Task On_a_phone_the_family_opened_at_every_side_stands_in_its_stage_and_the_page_does_not_scroll_sideways()
    {
        var page = await site.Visitor(width: 360, readTo: 4);
        await page.GotoAsync($"/en/{Sohrab}?up=9&down=9&beside=9");
        var frame = page.Locator(".stage-frame");

        // Since the sides beside the chart stand above it on a phone (step 10.4), the family of the story is as
        // wide as the stage: nothing is cut, and what would be larger scrolls in the frame, which the keyboard
        // reaches. Scrolling a frame by the keyboard is tested where a chart is larger: the story and the time.
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");
        Assert.True(await frame.EvaluateAsync<bool>("frame => frame.scrollWidth <= frame.clientWidth + 1"));
        Assert.Equal(5, await page.Locator("svg.graph foreignObject").CountAsync());

        for (var stops = 0; stops < 60 && !await frame.EvaluateAsync<bool>("frame => document.activeElement === frame"); stops++)
        {
            await page.Keyboard.PressAsync("Tab");
        }

        Assert.True(await frame.EvaluateAsync<bool>("frame => document.activeElement === frame"), "The keyboard does not reach the frame of the tree.");
    }

    [Fact]
    public async Task Parents_stand_above_their_child_and_partners_beside_each_other()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Sohrab}");

        var (rostam, tahmineh, sohrab) = (await Box(page, "character-rostam"), await Box(page, "character-tahmineh"), await Box(page, "character-sohrab"));
        var (zal, king) = (await Box(page, "character-zal"), await Box(page, "character-king-samangan"));

        Same(rostam.Top, tahmineh.Top);
        Assert.True(rostam.Right < tahmineh.Left);
        Assert.True(sohrab.Top > rostam.Bottom);
        Assert.True(zal.Bottom < rostam.Top && king.Bottom < tahmineh.Top);

        // The child stands below the middle of its parents, and each father above his child.
        Same((rostam.Left + tahmineh.Right) / 2, (sohrab.Left + sohrab.Right) / 2);
        Same(zal.Left, rostam.Left);
        Same(king.Left, tahmineh.Left);
        await Assertions.Expect(page.Locator(".tree .graph-focus")).ToHaveTextAsync("Sohrab");
    }

    [Fact]
    public async Task Across_the_page_generations_are_columns()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Sohrab}");
        await page.GetByRole(AriaRole.Link, new() { Name = "Across the page" }).ClickAsync();
        await Arrived(page, new Regex("view=across$"));

        var (rostam, tahmineh, sohrab, zal) = (await Box(page, "character-rostam"), await Box(page, "character-tahmineh"), await Box(page, "character-sohrab"), await Box(page, "character-zal"));

        Same(rostam.Left, tahmineh.Left);
        Assert.True(rostam.Bottom < tahmineh.Top);
        Assert.True(zal.Right < rostam.Left && rostam.Right < sohrab.Left);
        Same((rostam.Top + tahmineh.Bottom) / 2, (sohrab.Top + sohrab.Bottom) / 2);
    }

    [Fact]
    public async Task On_a_Persian_page_the_tree_begins_at_the_right()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/fa/{Sohrab}");

        var (rostam, tahmineh) = (await Box(page, "character-rostam"), await Box(page, "character-tahmineh"));
        Assert.True(tahmineh.Right < rostam.Left);

        // On a phone the frame shows the beginning of the tree, which is its right end.
        var phone = await site.Visitor(width: 360, readTo: 4);
        await phone.GotoAsync($"/fa/{Sohrab}");
        var shown = await phone.EvaluateAsync<bool>(
            """
            () => {
                const frame = document.querySelector('.stage-frame').getBoundingClientRect();
                const first = document.querySelector(".tree a.graph-name[href$='/entity/character-zal']").closest('foreignObject').getBoundingClientRect();
                return first.left >= frame.left - 1 && first.right <= frame.right + 1 && first.right > frame.right - 40;
            }
            """);
        Assert.True(shown, "The first box of the tree is not at the right end of its frame.");
        Assert.False(await ScrollsSideways(phone), "The page scrolls sideways.");
    }

    [Fact]
    public async Task A_generation_is_opened_and_closed_by_links_and_Back_undoes_it()
    {
        var page = await site.Visitor(width: 412, readTo: 4);
        await page.GotoAsync($"/en/{Zal}");
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(4);

        // Two rows have the same steps; each is named with its row.
        await page.GetByRole(AriaRole.Link, new() { Name = "Descendants: close a step" }).ClickAsync();
        await Arrived(page, new Regex("down=1#chart$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(3);
        await Assertions.Expect(page.Locator(".stage-bottom").GetByRole(AriaRole.Link, new() { Name = "Descendants: open a step further" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Descendants: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("/family#chart$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(4);

        await page.GetByRole(AriaRole.Link, new() { Name = "The whole family" }).ClickAsync();
        await Arrived(page, new Regex("family=whole$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(5);

        await page.GoBackAsync();
        await Arrived(page, new Regex("/family#chart$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(4);
    }

    [Theory]
    [InlineData(1280, "en")]
    [InlineData(390, "fa")]
    public async Task The_tree_has_the_page_and_stands_in_the_middle_of_its_stage_with_steps_at_its_four_sides(int width, string language)
    {
        var page = await site.Visitor(width, readTo: 4);
        await page.GotoAsync($"/{language}/{Zal}");

        var sizes = await page.EvaluateAsync<double[]>(
            """
            () => {
                const stage = document.querySelector('.stage').getBoundingClientRect();
                const frame = document.querySelector('.stage-frame').getBoundingClientRect();
                const tree = document.querySelector('.tree').getBoundingClientRect();
                const steps = ['top', 'left', 'right', 'bottom'].map(side => document.querySelectorAll('.stage-' + side + ' .stage-step').length);
                return [stage.left, innerWidth - stage.right, stage.height, innerHeight,
                    (tree.left + tree.right) / 2 - (frame.left + frame.right) / 2, (tree.top + tree.bottom) / 2 - (frame.top + frame.bottom) / 2,
                    tree.width - frame.width, ...steps];
            }
            """);

        Assert.InRange(sizes[0], 0, 17);
        Assert.InRange(sizes[1], 0, 17);
        Assert.InRange(sizes[2], sizes[3] - 33 - (width <= 640 ? 60 : 0), sizes[3] - (width <= 640 ? 60 : 0));
        Assert.Equal([2, 2, 2, 2], sizes[7..]);

        // A tree that is smaller than its stage stands in its middle; a larger one begins at its edge and scrolls.
        Assert.InRange(sizes[5], -2, 2);
        if (sizes[6] < 0)
        {
            Assert.InRange(sizes[4], -9, 9);
        }

        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");
    }

    [Fact]
    public async Task A_plus_above_the_tree_opens_a_generation_of_ancestors_and_a_minus_closes_it()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Sohrab}?up=1");
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(3);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Ancestors: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("/family#chart$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(5);

        // Nobody stands above the two grandfathers: the "+" is no link any more, and it says so.
        await Assertions.Expect(page.Locator(".stage-top .stage-step[aria-disabled='true']")).ToHaveTextAsync(new Regex("not possible here"));

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Ancestors: close a step" }).ClickAsync();
        await Arrived(page, new Regex("up=1#chart$"));
        await Assertions.Expect(page.Locator(".tree a.graph-name")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task A_name_of_the_tree_leads_to_its_page_and_the_keyboard_reaches_it()
    {
        // The still drawing, which is the drawing where the script of the graph does not run (ADR 0020).
        var page = await site.Visitor(width: 412, readTo: 4, script: false);
        await page.GotoAsync($"/en/{Sohrab}");
        var name = page.Locator(".tree a.graph-name[href$='/entity/character-tahmineh']");

        for (var stops = 0; stops < 80 && !await name.EvaluateAsync<bool>("link => document.activeElement === link"); stops++)
        {
            await page.Keyboard.PressAsync("Tab");
        }

        Assert.True(await name.EvaluateAsync<bool>("link => document.activeElement === link"), "The keyboard does not reach a name of the tree.");
        await Assertions.Expect(name).ToBeInViewportAsync();
        await page.Keyboard.PressAsync("Enter");
        await Arrived(page, new Regex("/entity/character-tahmineh$"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Tahmineh");
    }

    public static TheoryData<int, string, string> Views()
    {
        var views = new TheoryData<int, string, string>();
        foreach (var width in new[] { 360, 412, 1280 })
        {
            foreach (var language in new[] { "en", "fa" })
            {
                foreach (var view in new[] { Sohrab, $"{Sohrab}?view=across", $"{Zal}?family=whole", $"{Zal}?family=whole&view=across", $"{Sohrab}?up=0&down=0" })
                {
                    views.Add(width, language, view);
                }
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task A_tree_fits_the_window_and_no_box_is_cut_or_lies_on_another_or_on_a_line_that_is_not_its_own(int width, string language, string view)
    {
        var page = await site.Visitor(width, readTo: 4);
        await page.GotoAsync($"/{language}/{view}");
        await page.EvaluateAsync("document.fonts.ready");

        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        var problems = await page.EvaluateAsync<string[]>(
            """
            () => {
                const problems = [];
                const outside = [...document.querySelectorAll('main *')]
                    .filter(element => !element.closest('.stage-frame') && element.getClientRects().length > 0)
                    .filter(element => { const box = element.getBoundingClientRect(); return box.width > 0 && (box.right > innerWidth + 1 || box.left < -1); });
                for (const element of outside) problems.push('outside the window: ' + element.tagName + '.' + element.getAttribute('class'));

                const boxes = [...document.querySelectorAll('.tree foreignObject')];
                const frames = boxes.map(box => box.getBoundingClientRect());
                const names = boxes.map(box => box.textContent.trim());
                boxes.forEach((box, index) => {
                    if (box.firstElementChild.scrollHeight - frames[index].height > 1) problems.push('cut: ' + names[index]);
                    const link = box.querySelector('a').getBoundingClientRect();
                    if (link.height < 43.5) problems.push('a name lower than a finger needs: ' + names[index]);
                });
                for (let i = 0; i < frames.length; i++) {
                    for (let j = i + 1; j < frames.length; j++) {
                        const a = frames[i], b = frames[j];
                        if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) problems.push(names[i] + ' lies on ' + names[j]);
                    }
                }

                // A line never runs through a box: every part of it lies in the room between boxes.
                for (const line of document.querySelectorAll('.tree line')) {
                    const l = line.getBoundingClientRect();
                    frames.forEach((b, index) => {
                        if (l.left < b.right - 2 && b.left < l.right - 2 && l.top < b.bottom - 2 && b.top < l.bottom - 2) problems.push('a line runs through ' + names[index]);
                    });
                }

                // The drawing is as large as it says: nothing of it lies outside its own frame.
                const drawing = document.querySelector('.tree').getBoundingClientRect();
                frames.forEach((b, index) => {
                    if (b.left < drawing.left - 1 || b.right > drawing.right + 1 || b.top < drawing.top - 1 || b.bottom > drawing.bottom + 1) problems.push('outside the drawing: ' + names[index]);
                });
                return problems;
            }
            """);
        Assert.Empty(problems);
    }

    public static TheoryData<int, string, bool, string> Checked()
    {
        var views = new TheoryData<int, string, bool, string>();
        foreach (var language in new[] { "en", "fa" })
        {
            foreach (var dark in new[] { false, true })
            {
                views.Add(412, language, dark, Sohrab);
                views.Add(1280, language, dark, $"{Zal}?family=whole&view=across");
                views.Add(412, language, dark, $"{Sohrab}?up=0&down=0");
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Checked))]
    public async Task A_tree_breaks_no_rule_of_accessibility_that_a_tool_can_check(int width, string language, bool dark, string view)
    {
        var page = await site.Visitor(width, readTo: 4, dark: dark);
        await page.GotoAsync($"/{language}/{view}");
        await page.EvaluateAsync("document.fonts.ready");

        var result = await page.RunAxe(BrowserSite.PageOfDastan, new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });

        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));
    }
}
