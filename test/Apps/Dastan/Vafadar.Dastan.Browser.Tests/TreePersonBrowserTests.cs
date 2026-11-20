using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using SkiaSharp;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// A person of a family tree in a real browser (step 19.5, part four; ADR 0017): somebody makes an account and
/// a tree, writes down a person with times of the Solar Hijri calendar as the form asks for them, opens the
/// page of the person, brings a photograph in and makes it the picture – on a narrow and on a wide screen, in
/// both languages and both appearances. The tests share a site, which takes five new accounts an hour from
/// one address: this class makes four.
/// </summary>
public sealed class TreePersonBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
{
    private static async Task BreaksNoRule(IPage page)
    {
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

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    // A picture a browser can show: taller than wide, like a portrait, so that the square of the profile cuts it.
    private static byte[] Portrait()
    {
        using var bitmap = new SKBitmap(600, 900);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0x3F, 0x6C, 0x8A));
        using var paint = new SKPaint { Color = new SKColor(0xE9, 0xD8, 0xB4), IsAntialias = true };
        canvas.DrawCircle(300, 330, 150, paint);
        canvas.DrawOval(300, 800, 260, 240, paint);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    [Theory]
    [InlineData(375, "en", false)]
    [InlineData(375, "fa", true)]
    [InlineData(1280, "en", true)]
    [InlineData(1280, "fa", false)]
    public async Task Somebody_writes_down_a_person_and_gives_them_a_picture(int width, string language, bool dark)
    {
        MailCatcher.Needed();
        var page = await site.Visitor(width, dark: dark);
        var address = MailCatcher.NewAddress("tree");
        await AccountJourneyBrowserTests.Ask(page, language, AccountJourneyBrowserTests.NewName("Tree"), address);
        await page.GotoAsync(await AccountJourneyBrowserTests.Link(address, language == "fa" ? "تأیید" : "Confirm"));
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));

        // A tree, and in it somebody who died: born about 1250 and died in the spring of 1329, as a Persian
        // family counts its years.
        await page.GotoAsync($"/{language}/account/trees");
        await page.Locator("#tree-name").FillAsync("خاندان راد");
        await page.Locator("form[action$='/trees/add'] button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account/trees/[a-f0-9]{32}\?said=made$"));

        var form = page.Locator("#person-add form");
        await form.Locator("[name=name]").FillAsync("زال");
        await form.Locator("[name=born]").FillAsync("1250");
        await form.Locator("details.finer > summary").First.ClickAsync();
        await form.Locator("[name=born-calendar]").SelectOptionAsync("solar-hijri");
        await form.Locator("[name=born-about]").CheckAsync();
        await form.Locator("[name=died]").FillAsync("1329");
        await form.Locator("details.finer > summary").Last.ClickAsync();
        await form.Locator("[name=died-month]").FillAsync("1");
        await form.Locator("[name=died-day]").FillAsync("12");
        await form.Locator("[name=died-calendar]").SelectOptionAsync("solar-hijri");
        await form.Locator("[name=deceased]").CheckAsync();

        // The form with its finer fields open fits the screen and breaks no rule.
        Assert.False(await ScrollsSideways(page));
        await BreaksNoRule(page);
        await form.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"\?said=person-added#people$"));

        // The page of the person: the drawing and the facts, beside each other where there is room.
        await page.Locator("#people a[href*='/person/']").First.ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/person/[a-f0-9]{32}$"));
        var person = page.Url;
        Assert.Equal("زال", await page.Locator("main h1").InnerTextAsync());
        Assert.Equal(1, await page.Locator(".profile .kind-drawing").CountAsync());
        var facts = await page.Locator(".profile .profile-facts").InnerTextAsync();
        Assert.Contains(language == "fa" ? "۱۲۵۰" : "1250", facts, StringComparison.Ordinal);
        // The day as it was written, with the name of its month – and, on a page of another calendar, converted beside it.
        Assert.Contains(language == "fa" ? "۱۲ فروردین ۱۳۲۹" : "12 Farvardin 1329 SH (converted: 1 April 1950 CE)", facts, StringComparison.Ordinal);
        Assert.Contains(language == "fa" ? "دانسته نیست" : "Not known", facts, StringComparison.Ordinal);
        await Fits(page, width);
        await BreaksNoRule(page);

        // A photograph is brought in – for its owner alone – and made the picture, with a place and a few words.
        await page.GotoAsync($"/{language}/account/photos");
        var add = page.Locator("form[action$='/photos/add']");
        await add.Locator("[name=file]").SetInputFilesAsync(new FilePayload { Name = "zal.png", MimeType = "image/png", Buffer = Portrait() });
        await add.Locator("[name=caption]").FillAsync("زال در باغ");
        await add.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account/photos\?said=added#mine$"));

        await page.GotoAsync(person);
        var describe = page.Locator("#describe form");
        await describe.Locator("[name=bornAt]").FillAsync("زابل");
        await describe.Locator("[name=about]").FillAsync("رستم را پرورد.\nموی سپید داشت.");
        await describe.Locator("[name=picture]").SelectOptionAsync(new SelectOptionValue { Label = "زال در باغ" });
        await BreaksNoRule(page);
        await describe.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"\?said=described#profile$"));

        // The picture is really drawn – a square cut out of the portrait – and the words keep their two lines.
        var picture = page.Locator(".profile .profile-picture img");
        await picture.EvaluateAsync("image => image.complete || new Promise(done => image.addEventListener('load', done, { once: true }))");
        Assert.True(await picture.EvaluateAsync<int>("image => image.naturalWidth") > 0, "The picture is drawn.");
        Assert.Equal(0, await page.Locator(".profile .kind-drawing").CountAsync());
        Assert.Contains("زابل", await page.Locator(".profile .profile-facts").InnerTextAsync(), StringComparison.Ordinal);
        var about = await page.Locator(".person-about").BoundingBoxAsync();
        Assert.NotNull(about);
        Assert.True(about.Height > 40, "The words about the person stand in two lines.");
        await Fits(page, width);
        await BreaksNoRule(page);
    }

    // Nothing reaches beyond the screen; the picture is a square; the facts stand beside it or under it.
    private static async Task Fits(IPage page, int width)
    {
        Assert.False(await ScrollsSideways(page));
        var picture = await page.Locator(".profile .profile-picture").BoundingBoxAsync();
        var facts = await page.Locator(".profile .profile-facts").BoundingBoxAsync();
        Assert.NotNull(picture);
        Assert.NotNull(facts);
        Assert.InRange(picture.Width, 100, 240);
        Assert.Equal(picture.Width, picture.Height, 1.0);
        if (width >= 1000)
        {
            Assert.True(Math.Abs(facts.Y - picture.Y) < 4 + 24, "The facts stand beside the picture.");
        }
        else
        {
            Assert.True(facts.Y > picture.Y + picture.Height, "The facts stand under the picture.");
        }
    }
}
