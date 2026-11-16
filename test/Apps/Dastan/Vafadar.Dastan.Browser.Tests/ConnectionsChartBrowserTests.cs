using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The connections chart in a real browser (ADR 0008): where the neighbours of a name stand, what the steps at the
/// sides of its stage do, and that no box is cut or lies on another one – in both languages and on a phone.
/// </summary>
public sealed class ConnectionsChartBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Rostam = $"{BrowserSite.Story}/entity/character-rostam/connections";
    private const string Sohrab = $"{BrowserSite.Story}/entity/character-sohrab/connections";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    private static Task Arrived(IPage page, Regex address) => BrowserSite.Arrived(page, address);

    // The first box of an entity inside the chart – the focus has a link of its own kind – as the window shows it.
    private static async Task<(double Left, double Top, double Right, double Bottom)> Box(IPage page, string entity)
    {
        var box = await page.EvaluateAsync<double[]>(
            "entity => { const b = document.querySelector(`.flow a.graph-name[href$='/entity/${entity}/connections'], .flow .graph-focus a.graph-name[href$='/entity/${entity}']`).closest('foreignObject').getBoundingClientRect(); return [b.left, b.top, b.right, b.bottom]; }",
            entity);
        return (box[0], box[1], box[2], box[3]);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task What_leads_to_the_name_stands_above_it_what_it_leads_to_below_and_a_partner_beside(string language)
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/{language}/{Rostam}");

        var (rostam, zal, rakhsh, tahmineh) = (await Box(page, "character-rostam"), await Box(page, "character-zal"), await Box(page, "character-rakhsh"), await Box(page, "character-tahmineh"));

        // Zal is the father of Rostam; Rostam rides Rakhsh; Rostam and Tahmineh are married.
        Assert.True(zal.Bottom < rostam.Top);
        Assert.True(rakhsh.Top > rostam.Bottom);
        Assert.Equal(rostam.Top, tahmineh.Top, precision: 0);

        // The chart begins where reading begins: what hangs on the focus steps away from that side, and the
        // first name beside it stands where reading goes on.
        if (language == "fa")
        {
            Assert.True(tahmineh.Right < rostam.Left);
            Assert.True(rakhsh.Right < rostam.Right);
        }
        else
        {
            Assert.True(tahmineh.Left > rostam.Right);
            Assert.True(rakhsh.Left > rostam.Left);
        }
    }

    [Fact]
    public async Task A_side_of_the_stage_opens_the_chart_a_step_further_and_closes_it_again_and_the_stage_stays_on_the_screen()
    {
        var page = await site.Visitor(width: 412, readTo: 4);
        await page.GotoAsync($"/en/{Rostam}");
        var boxes = await page.Locator("svg.flow foreignObject").CountAsync();

        // Nothing is open: no side can be closed.
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Towards Rostam: close a step" })).ToHaveCountAsync(0);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Towards Rostam: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("[?]open=character-zal#chart$"));
        Assert.True(await page.Locator("svg.flow foreignObject").CountAsync() > boxes);

        // After a step the stage is what the window shows, not the top of the page.
        var top = await page.Locator(".stage").EvaluateAsync<double>("stage => stage.getBoundingClientRect().top");
        Assert.InRange(top, -1, 40);

        // What Zal leads to hangs above him, a step further from the middle.
        var (zal, mourned) = (await Box(page, "character-zal"), await Box(page, "character-sohrab"));
        Assert.True(mourned.Bottom < zal.Top);

        await page.Locator(".stage-top").GetByRole(AriaRole.Link, new() { Name = "Towards Rostam: close a step" }).ClickAsync();
        await Arrived(page, new Regex("/connections#chart$"));
        await Assertions.Expect(page.Locator("svg.flow foreignObject")).ToHaveCountAsync(boxes);

        // Both sides beside the name carry the same steps.
        await Assertions.Expect(page.Locator(".stage-left").GetByRole(AriaRole.Link, new() { Name = "Mutual: open a step further" })).ToBeVisibleAsync();
        await page.Locator(".stage-right").GetByRole(AriaRole.Link, new() { Name = "Mutual: open a step further" }).ClickAsync();
        await Arrived(page, new Regex("[?]open=character-tahmineh#chart$"));

        await page.GoBackAsync();
        await Arrived(page, new Regex("/connections#chart$"));
        await Assertions.Expect(page.Locator("svg.flow foreignObject")).ToHaveCountAsync(boxes);
    }

    public static TheoryData<int, string, string> Charts()
    {
        var charts = new TheoryData<int, string, string>();
        foreach (var width in new[] { 360, 412, 1280 })
        {
            foreach (var language in new[] { "en", "fa" })
            {
                foreach (var chart in new[]
                {
                    Rostam,
                    $"{Rostam}?open=character-zal,character-tahmineh,character-sohrab,object-token,character-kay-kavus",
                    Sohrab,
                    $"{Sohrab}?open=character-rostam,character-tahmineh,character-zal,character-afrasiab",
                })
                {
                    charts.Add(width, language, chart);
                }
            }
        }

        return charts;
    }

    [Theory]
    [MemberData(nameof(Charts))]
    public async Task No_box_of_the_chart_is_cut_or_lies_on_another_one_and_the_chart_scrolls_in_its_frame(int width, string language, string chart)
    {
        var page = await site.Visitor(width, readTo: 4);
        await page.GotoAsync($"/{language}/{chart}");
        await page.EvaluateAsync("document.fonts.ready");

        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");
        var drawing = await page.EvaluateAsync<int[]>(
            """
            () => {
                const boxes = [...document.querySelectorAll('svg.flow foreignObject')];
                const frames = boxes.map(box => box.getBoundingClientRect());
                const clipped = boxes.filter((box, index) => box.firstElementChild.scrollHeight - frames[index].height > 1).length;
                let overlapping = 0;
                for (let i = 0; i < frames.length; i++) {
                    for (let j = i + 1; j < frames.length; j++) {
                        const a = frames[i], b = frames[j];
                        if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) overlapping++;
                    }
                }
                const frame = document.querySelector('.stage-frame');
                const svg = document.querySelector('svg.flow').getBoundingClientRect();
                const reachable = frame.scrollWidth >= Math.floor(svg.width) && frame.scrollHeight >= Math.floor(svg.height);
                const small = [...document.querySelectorAll('svg.flow a.graph-toggle')].filter(toggle => { const box = toggle.getBoundingClientRect(); return box.width < 43.5 || box.height < 43.5; }).length;
                return [boxes.length, clipped, overlapping, reachable ? 0 : 1, small];
            }
            """);

        Assert.True(drawing[0] > 1);
        Assert.Equal([0, 0, 0, 0], drawing[1..]);
    }
}
