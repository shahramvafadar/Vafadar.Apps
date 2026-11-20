using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The connected journey of M0 in a real browser: what a visitor does with a pointer and with the keyboard, and what
/// the browser itself does with history, focus, fonts and files.
/// </summary>
public sealed class JourneyTests(BrowserSite site) : IClassFixture<BrowserSite>
{
    private const string Story = BrowserSite.Story;

    private static Task<string> Heading(IPage page) => page.Locator("h1").InnerTextAsync();

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    // --- M0-02 ---

    [Fact]
    public async Task From_the_start_to_an_entity_along_a_labelled_relation_to_another_one_and_back()
    {
        var page = await site.Visitor();

        await page.GotoAsync("/en/");
        await page.Locator("main h3 a").First.ClickAsync();
        await Arrived(page, new Regex($"/en/{Story}$"));
        Assert.Equal("Rostam and Sohrab", await Heading(page));

        await page.Locator(".cards h3 a").GetByText("Rostam", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await Arrived(page, new Regex("/entity/character-rostam$"));
        Assert.Equal("Rostam", await Heading(page));

        // The relation is a sentence with its label; the other end is the link to follow.
        var relation = page.Locator(".statements li").Filter(new LocatorFilterOptions { HasText = "Father of" });
        Assert.Contains("Zal", await relation.First.InnerTextAsync(), StringComparison.Ordinal);
        await relation.First.Locator(".statement a").ClickAsync();
        await Arrived(page, new Regex("/entity/character-zal$"));
        Assert.Equal("Zal", await Heading(page));

        await page.GoBackAsync();
        Assert.EndsWith("/entity/character-rostam", page.Url, StringComparison.Ordinal);
        Assert.Equal("Rostam", await Heading(page));
        await page.GoBackAsync();
        Assert.EndsWith($"/en/{Story}", page.Url, StringComparison.Ordinal);
        Assert.Equal("Rostam and Sohrab", await Heading(page));
    }

    // --- M0-03 and the keyboard ---

    [Fact]
    public async Task With_the_keyboard_alone_a_visitor_skips_to_the_content_and_opens_and_closes_a_row_of_the_graph()
    {
        // The still drawing, which is the drawing where the script of the graph does not run (ADR 0020).
        var page = await site.Visitor(width: 360, script: false);
        await page.GotoAsync($"/en/{Story}/entity/character-rostam/connections");
        var boxes = await page.Locator("svg.graph foreignObject").CountAsync();

        // The first stop of the keyboard is the link that skips the header.
        await page.Keyboard.PressAsync("Tab");
        Assert.Equal("skip-link", await page.EvaluateAsync<string>("document.activeElement.className"));
        await page.Keyboard.PressAsync("Enter");
        Assert.EndsWith("#content", page.Url, StringComparison.Ordinal);

        // From there the controls of the drawing are reached in order, each with a visible focus.
        await TabTo(page, "graph-toggle");
        Assert.NotEqual("none", await page.EvaluateAsync<string>("getComputedStyle(document.activeElement).outlineStyle"));
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement.getBoundingClientRect().height >= 44"));
        await page.Keyboard.PressAsync("Enter");
        await Arrived(page, new Regex("[?]open="));
        var opened = await page.Locator("svg.graph foreignObject").CountAsync();
        Assert.True(opened > boxes, $"{opened} boxes after opening, {boxes} before.");
        Assert.False(await ScrollsSideways(page));

        // The drawing and the list below it hold the same relations, opened or not.
        Assert.Equal(
            await page.Locator("svg.graph .graph-relation").CountAsync(),
            await page.Locator(".statements li").CountAsync());

        await TabTo(page, "graph-toggle");
        await page.Keyboard.PressAsync("Enter");
        await Arrived(page, new Regex("/connections#chart$"));
        Assert.Equal(boxes, await page.Locator("svg.graph foreignObject").CountAsync());
    }

    // --- M0-04 ---

    [Fact]
    public async Task On_a_phone_the_lanes_scroll_inside_their_frame_and_the_page_does_not()
    {
        var page = await site.Visitor(width: 360, readTo: 4);
        await page.GotoAsync($"/fa/{Story}/story");

        Assert.Equal("rtl", await page.EvaluateAsync<string>("document.documentElement.dir"));
        Assert.Equal(8, await page.Locator("ol.steps > li.step").CountAsync());
        Assert.Equal(8, await page.Locator(".lanes thead th").CountAsync() - 1);
        Assert.False(await ScrollsSideways(page));
        Assert.True(await page.EvaluateAsync<bool>("(e => e.scrollWidth > e.clientWidth)(document.querySelector('.lanes'))"));

        // The frame can be reached and moved with the keyboard.
        await page.Locator(".lanes").FocusAsync();
        var before = await page.EvaluateAsync<double>("document.querySelector('.lanes').scrollLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.WaitForTimeoutAsync(300);
        Assert.NotEqual(before, await page.EvaluateAsync<double>("document.querySelector('.lanes').scrollLeft"));
    }

    // --- M0-05 ---

    [Fact]
    public async Task After_moving_to_the_third_stage_the_image_is_shown_the_video_plays_and_its_subject_leads_on_and_back()
    {
        var page = await site.Visitor(width: 412);

        // Before: the gallery shows nothing – it names one work held elsewhere, without a picture.
        await page.GotoAsync($"/en/{Story}/gallery");
        Assert.Equal(0, await page.Locator(".media-card:not(.media-reference)").CountAsync());
        Assert.Equal(1, await page.Locator(".media-reference").CountAsync());
        Assert.Equal(0, await page.Locator("main img").CountAsync());

        // The visitor moves forward by their own act, on the page of the story.
        await page.GotoAsync($"/en/{Story}");
        await page.Locator("#stage-2").CheckAsync();
        await page.Locator(".reading-form button[type=submit]").ClickAsync();
        await Arrived(page, new Regex($"/en/{Story}#reading$"));

        await page.Locator("main a").GetByText("Images and videos", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await Arrived(page, new Regex("/gallery$"));
        Assert.Equal(2, await page.Locator(".media-card:not(.media-reference)").CountAsync());

        // The image arrives and is as wide as the page allows.
        await page.Locator("#media-dastan-family-illustration h2 a").ClickAsync();
        await Arrived(page, new Regex("/media/media-dastan-family-illustration$"));
        var image = page.Locator(".media-figure img");
        await Assertions.Expect(image).ToBeVisibleAsync();
        Assert.True(await image.EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0 && img.getBoundingClientRect().width <= innerWidth"));
        Assert.False(await ScrollsSideways(page));

        // The video starts when asked, runs, and shows its captions.
        await page.Locator("nav.neighbours a").First.ClickAsync();
        await Arrived(page, new Regex("/media/media-dastan-parentage-video$"));
        Assert.True(await page.EvaluateAsync<bool>("document.querySelector('video').paused"));
        var played = await page.EvaluateAsync<double[]>(
            """
            async () => {
                const video = document.querySelector('video');
                await video.play();
                await new Promise(done => setTimeout(done, 1500));
                const track = video.textTracks[0];
                const shown = track.mode === 'showing' ? 1 : 0;
                if (!shown) {
                    track.mode = 'showing';
                    await new Promise(done => setTimeout(done, 500));
                }
                return [video.currentTime, video.duration, video.videoWidth, shown, track.cues ? track.cues.length : 0, video.error ? video.error.code : 0];
            }
            """);
        Assert.Equal(0, played[5]);
        Assert.True(played[0] > 0.5, $"The video stands at {played[0]} seconds.");
        Assert.InRange(played[1], 19.5, 20.5);
        Assert.Equal(1280, played[2]);
        // Whether captions marked as the default are shown without being asked for is the visitor's setting in
        // WebKit, and the WebKit of the tests has it off; turned on, they are there.
        Assert.Equal(BrowserSite.Engine == "webkit" ? 0 : 1, played[3]);
        Assert.Equal(4, played[4]);
        Assert.Contains("Zal, also called Dastan, is Rostam's father.", await page.Locator(".transcript").InnerTextAsync(), StringComparison.Ordinal);

        // Somebody the video shows is a link; back returns to the video.
        await page.Locator(".subjects a").GetByText("Zal", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
        await Arrived(page, new Regex("/entity/character-zal$"));
        await page.GoBackAsync();
        Assert.EndsWith("/media/media-dastan-parentage-video", page.Url, StringComparison.Ordinal);

        // And the page of the video returns to its place in the gallery.
        await page.Locator(".page-header a").First.ClickAsync();
        Assert.EndsWith("/gallery#media-dastan-parentage-video", page.Url, StringComparison.Ordinal);
    }

    // --- M0-06 ---

    [Fact]
    public async Task After_moving_back_in_the_story_going_back_in_the_browser_shows_nothing_of_the_later_part()
    {
        var page = await site.Visitor(readTo: 2);
        await page.GotoAsync($"/en/{Story}/entity/character-sohrab");
        Assert.Equal("Sohrab", await Heading(page));
        await page.GotoAsync($"/en/{Story}/media/media-dastan-family-illustration");
        await Assertions.Expect(page.Locator(".media-figure img")).ToBeVisibleAsync();

        // The visitor lowers the reading position …
        await page.GotoAsync($"/en/{Story}");
        await page.Locator("#stage-0").CheckAsync();
        await page.Locator(".reading-form button[type=submit]").ClickAsync();
        await Arrived(page, new Regex("#reading$"));
        Assert.Equal(0, await page.Locator(".cards h3 a").GetByText("Sohrab", new LocatorGetByTextOptions { Exact = true }).CountAsync());

        // … and walks back through the history of the browser: the pages are asked for again and are not found.
        for (var steps = 0; steps < 4 && !page.Url.Contains("/media/", StringComparison.Ordinal); steps++)
        {
            await page.GoBackAsync();
        }

        Assert.Contains("/media/media-dastan-family-illustration", page.Url, StringComparison.Ordinal);
        Assert.Equal("Not here", await Heading(page));
        Assert.Equal(0, await page.Locator("img").CountAsync());
        Assert.DoesNotContain("Sohrab", (await page.Locator("main").InnerTextAsync()).Replace("Rostam and Sohrab", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);

        await page.GoBackAsync();
        Assert.Contains("/entity/character-sohrab", page.Url, StringComparison.Ordinal);
        Assert.Equal("Not here", await Heading(page));

        // The file itself is gone too, for the browser that had it a moment ago.
        Assert.Equal(404, await page.EvaluateAsync<int>($"fetch('/media/{Story}/media-dastan-family-illustration/file').then(answer => answer.status)"));
    }

    // --- M0-07 ---

    [Fact]
    public async Task The_sources_of_a_relation_close_back_to_the_drawing_with_what_was_open_there()
    {
        var page = await site.Visitor(width: 412);
        await page.GotoAsync($"/en/{Story}/entity/character-rostam/connections?open=character-zal");
        var boxes = await page.Locator("svg.graph foreignObject").CountAsync();

        await page.Locator(".statements .evidence-link").First.ClickAsync();
        await Arrived(page, new Regex("/evidence/relation/"));
        Assert.Equal("What supports this", await Heading(page));
        Assert.True(await page.Locator(".sources li").CountAsync() > 0);

        // A source is another site: the visitor is told before, and can stay.
        await page.Locator(".sources a.action").First.ClickAsync();
        await Arrived(page, new Regex("/source/"));
        Assert.Equal("You are leaving Dastan", await Heading(page));
        await page.Locator("a.action").GetByText("Stay on Dastan").ClickAsync();
        await Arrived(page, new Regex("/evidence/relation/"));

        await page.Locator(".page-header a").First.ClickAsync();
        await Arrived(page, new Regex("/connections[?]open=character-zal$"));
        Assert.Equal(boxes, await page.Locator("svg.graph foreignObject").CountAsync());
    }

    // --- M0-09 ---

    [Fact]
    public async Task Two_visitors_at_the_same_time_keep_their_own_language_reading_position_and_appearance()
    {
        var reader = await site.Visitor(readTo: 2, dark: true);
        var newcomer = await site.Visitor();

        await Task.WhenAll(reader.GotoAsync($"/fa/{Story}"), newcomer.GotoAsync($"/en/{Story}"));

        Assert.Equal("fa", await reader.EvaluateAsync<string>("document.documentElement.lang"));
        Assert.Equal("en", await newcomer.EvaluateAsync<string>("document.documentElement.lang"));
        Assert.Equal(1, await reader.Locator(".cards h3 a").GetByText("سهراب", new LocatorGetByTextOptions { Exact = true }).CountAsync());
        Assert.Equal(0, await newcomer.Locator(".cards h3 a").GetByText("Sohrab", new LocatorGetByTextOptions { Exact = true }).CountAsync());

        // The dark device gets dark colours, the light one light colours, without either choosing anything.
        var dark = await reader.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
        var light = await newcomer.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
        Assert.NotEqual(dark, light);
    }

    // --- M0-10 ---

    public static TheoryData<int, string, string> Views()
    {
        var views = new TheoryData<int, string, string>();
        foreach (var width in new[] { 360, 412, 1280 })
        {
            foreach (var language in new[] { "en", "fa" })
            {
                foreach (var view in new[]
                {
                    string.Empty,
                    Story,
                    $"{Story}/entity/character-rostam",
                    $"{Story}/entity/character-rostam/connections?open=character-sohrab,character-zal",
                    $"{Story}/entity/character-sohrab/family",
                    $"{Story}/story",
                    $"{Story}/gallery",
                    $"{Story}/timeline",
                    $"{Story}/media/media-dastan-parentage-video",
                    $"{Story}/evidence/relation/relation-02",
                    $"{Story}/source/src-iranica-zal",
                    $"{Story}/search?q=ro",
                    $"{Story}/entity/character-nobody",
                })
                {
                    views.Add(width, language, view);
                }
            }
        }

        return views;
    }

    [Theory]
    [MemberData(nameof(Views))]
    public async Task Every_view_fits_the_window_in_both_directions_and_clips_nothing_of_a_drawing(int width, string language, string view)
    {
        var page = await site.Visitor(width, readTo: 2);
        await page.GotoAsync($"/{language}/{view}");
        await page.EvaluateAsync("document.fonts.ready");

        Assert.Equal(language == "fa" ? "rtl" : "ltr", await page.EvaluateAsync<string>("document.documentElement.dir"));
        Assert.Equal(1, await page.Locator("h1").CountAsync());
        Assert.False(await ScrollsSideways(page), "The page scrolls sideways.");

        // Nothing of the content lies outside the window, except what scrolls inside a frame of its own.
        var outside = await page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('main *')]
                .filter(element => !element.closest('.lanes') && !element.closest('.stage-frame') && !element.closest('.visually-hidden') && !element.classList.contains('visually-hidden'))
                .filter(element => { const box = element.getBoundingClientRect(); return box.width > 0 && (box.right > innerWidth + 1 || box.left < -1); })
                .map(element => element.tagName + '.' + element.className)
            """);
        Assert.Empty(outside);

        // A box of a drawing is as high as its text, and no two boxes lie on each other.
        var drawing = await page.EvaluateAsync<int[]>(
            """
            () => {
                const boxes = [...document.querySelectorAll('svg.graph foreignObject')];
                const frames = boxes.map(box => box.getBoundingClientRect());
                const clipped = boxes.filter((box, index) => box.firstElementChild.scrollHeight - frames[index].height > 1).length;
                let overlapping = 0;
                for (let i = 0; i < frames.length; i++) {
                    for (let j = i + 1; j < frames.length; j++) {
                        const a = frames[i], b = frames[j];
                        if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) overlapping++;
                    }
                }
                return [clipped, overlapping];
            }
            """);
        Assert.Equal([0, 0], drawing);

        if (language == "fa")
        {
            // Persian text is set in the font of the site, which the browser got from the site.
            Assert.True(await page.EvaluateAsync<bool>("document.fonts.check('16px Vazirmatn', 'رستم')"));
        }
    }

    private static Task Arrived(IPage page, Regex address) => BrowserSite.Arrived(page, address);

    private static async Task TabTo(IPage page, string className)
    {
        for (var stops = 0; stops < 80; stops++)
        {
            await page.Keyboard.PressAsync("Tab");
            if (await page.EvaluateAsync<bool>($"document.activeElement.classList.contains('{className}')"))
            {
                return;
            }
        }

        Assert.Fail($"The keyboard does not reach a control of the kind \"{className}\".");
    }
}
