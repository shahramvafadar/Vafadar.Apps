using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The stage of a chart in a real browser, where it had two limits (ADR 0008, step 10.4): a chart drawn around a
/// name keeps that name in sight when much is opened above it, and on a phone the sides beside the chart do not
/// take its width.
/// </summary>
public sealed class StageBrowserTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    // Sohrab with what leads to him opened on every side: the name lies far below the top of the drawing.
    private const string Sohrab = $"{BrowserSite.Story}/entity/character-sohrab/connections?open=character-rostam,character-tahmineh,character-zal,character-afrasiab";
    private const string Family = $"{BrowserSite.Story}/entity/character-sohrab/family";

    // The frame of the stage that is shown, and inside it the box of the name the chart is drawn around.
    private const string Places =
        """
        () => {
            const frame = [...document.querySelectorAll('.stage-frame')].find(candidate => candidate.clientHeight > 0);
            const name = frame.querySelector('.graph-focus').closest('foreignObject').getBoundingClientRect();
            const shown = frame.getBoundingClientRect();
            return [name.left - shown.left, name.top - shown.top, shown.right - name.right, shown.bottom - name.bottom, frame.scrollTop, frame.scrollHeight - frame.clientHeight];
        }
        """;

    [Theory]
    [InlineData("en", 360)]
    [InlineData("fa", 360)]
    [InlineData("en", 1280)]
    [InlineData("fa", 1280)]
    public async Task A_chart_with_much_opened_above_its_name_shows_the_name(string language, int width)
    {
        var page = await site.Visitor(width: width, readTo: 4);
        await page.GotoAsync($"/{language}/{Sohrab}");
        await page.WaitForLoadStateAsync(LoadState.Load);

        var places = await page.EvaluateAsync<double[]>(Places);

        // The whole box of the name is inside what the stage shows.
        Assert.All(places[..4], distance => Assert.True(distance >= -1, $"The name is outside the stage by {distance} px."));
    }

    [Fact]
    public async Task A_chart_whose_name_is_in_sight_is_not_moved()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{BrowserSite.Story}/entity/character-rakhsh/connections");
        await page.WaitForLoadStateAsync(LoadState.Load);

        var places = await page.EvaluateAsync<double[]>(Places);

        Assert.Equal(0, places[4]);
    }

    [Fact]
    public async Task The_family_drawing_shows_its_name_too()
    {
        var page = await site.Visitor(width: 360, readTo: 4);
        await page.GotoAsync($"/en/{Family}?up=9&down=9&beside=9");
        await page.WaitForLoadStateAsync(LoadState.Load);

        var places = await page.EvaluateAsync<double[]>(Places);

        Assert.All(places[..4], distance => Assert.True(distance >= -1, $"The name is outside the stage by {distance} px."));
    }

    [Fact]
    public async Task Without_the_script_the_chart_is_whole_and_the_name_is_reached_by_scrolling_the_stage()
    {
        var page = await site.Visitor(width: 360, readTo: 4, script: false);
        await page.GotoAsync($"/en/{Sohrab}");

        var places = await page.EvaluateAsync<double[]>(Places);

        // Nothing moved the stage; the name lies below what it shows and within what it can be scrolled to.
        Assert.Equal(0, places[4]);
        Assert.True(places[3] < 0, "The name is in sight without the script: the test does not test anything.");
        Assert.True(places[5] > 0);
        await Assertions.Expect(page.Locator(".stage-frame .graph-focus")).ToHaveCountAsync(1);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task On_a_phone_the_chart_has_the_whole_width_and_the_steps_of_the_sides_stand_above_it(string language)
    {
        var page = await site.Visitor(width: 360, readTo: 4);
        await page.GotoAsync($"/{language}/{Sohrab}");

        var frame = (await page.Locator(".stage-frame").BoundingBoxAsync())!;
        var left = (await page.Locator(".stage-left").BoundingBoxAsync())!;
        var right = (await page.Locator(".stage-right").BoundingBoxAsync())!;
        var top = (await page.Locator(".stage-top").BoundingBoxAsync())!;

        Assert.True(frame.Width >= 358, $"The chart has {frame.Width} of 360 px.");

        // The left side is still at the left and the right one at the right, between the upper side and the chart.
        Assert.True(left.X + left.Width <= right.X + 1, $"left {left.X}+{left.Width}, right {right.X}");
        Assert.True(left.Y >= top.Y + top.Height - 1 && left.Y + left.Height <= frame.Y + 1, $"top {top.Y}+{top.Height}, left {left.Y}+{left.Height}, frame {frame.Y}");
        Assert.True(right.Y + right.Height <= frame.Y + 1, $"right {right.Y}+{right.Height}, frame {frame.Y}");

        // A step is as large as a finger needs, and the page does not scroll sideways.
        foreach (var step in await page.Locator(".stage-step").AllAsync())
        {
            var box = (await step.BoundingBoxAsync())!;
            Assert.True(box.Width >= 43.9 && box.Height >= 43.9, $"a step of {box.Width} x {box.Height}");
            Assert.True(box.X >= 0 && box.X + box.Width <= 360.5, $"a step at {box.X}+{box.Width}");
        }

        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"));
    }

    [Fact]
    public async Task On_a_wide_screen_the_sides_stand_beside_the_chart()
    {
        var page = await site.Visitor(width: 1280, readTo: 4);
        await page.GotoAsync($"/en/{Sohrab}");

        var frame = (await page.Locator(".stage-frame").BoundingBoxAsync())!;
        var left = (await page.Locator(".stage-left").BoundingBoxAsync())!;
        var right = (await page.Locator(".stage-right").BoundingBoxAsync())!;

        Assert.True(left.X + left.Width <= frame.X + 1);
        Assert.True(right.X >= frame.X + frame.Width - 1);
    }
}
