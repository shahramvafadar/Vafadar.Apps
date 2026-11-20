using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// Relationships in a real browser (step 19.6, ADR 0018): two people make accounts; one asks the other for two
/// kinds of relationship – a friend, and a colleague at a place since a year – the other accepts both, and
/// both are shown side by side. Then a place somebody was at, the search through it, and the own page seen
/// as everybody sees it. On a narrow and on a wide screen, in both languages and appearances. The tests share
/// a site, which takes five new accounts an hour from one address: this class makes four.
/// </summary>
public sealed class RelationshipsBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
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

    private static async Task Fits(IPage page)
    {
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"), "Nothing reaches beyond the screen.");
        await BreaksNoRule(page);
    }

    // Somebody makes an account as a person does: the form, the mail, the link.
    private async Task<(IPage Page, string Name)> Signed(int width, bool dark, string language, string who)
    {
        var page = await site.Visitor(width, dark: dark);
        var (name, address) = (AccountJourneyBrowserTests.NewName(who), MailCatcher.NewAddress(who.ToLowerInvariant()));
        await AccountJourneyBrowserTests.Ask(page, language, name, address);
        await page.GotoAsync(await AccountJourneyBrowserTests.Link(address, language == "fa" ? "تأیید" : "Confirm"));
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));
        return (page, name);
    }

    [Theory]
    [InlineData(375, "en", false, "friend", "colleague")]
    [InlineData(1280, "fa", true, "دوست", "همکار")]
    public async Task Two_people_show_two_kinds_of_relationship_side_by_side(int width, string language, bool dark, string friend, string colleague)
    {
        MailCatcher.Needed();
        var (mina, minaName) = await Signed(width, dark, language, "Mina");
        var (sara, saraName) = await Signed(width, dark, language, "Sara");
        var people = $"/{language}/account/people";

        // Mina asks twice: a friend – and a colleague, with where and since when.
        await mina.GotoAsync(people);
        var ask = mina.Locator("#ask form[method=post]");
        await ask.Locator("[name=name]").FillAsync(saraName);
        await ask.Locator("[name=kind]").SelectOptionAsync("friend");
        await ask.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/people\?said=asked$"));
        ask = mina.Locator("#ask form[method=post]");
        await ask.Locator("[name=name]").FillAsync(saraName);
        await ask.Locator("[name=kind]").SelectOptionAsync("colleague");
        await ask.Locator("[name=place]").FillAsync("نشر زابل");
        await ask.Locator("details.finer > summary").ClickAsync();
        await ask.Locator("[name=since]").FillAsync("1389");
        await ask.Locator("[name=calendar]").SelectOptionAsync("solar-hijri");
        await Fits(mina);
        await ask.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/people\?said=asked$"));

        // Sara finds both and accepts each by itself.
        await sara.GotoAsync(people);
        Assert.Equal(2, await sara.Locator("#waiting li").CountAsync());
        await Fits(sara);
        await sara.Locator("#waiting li button[value=accept]").First.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people\?said=accepted$"));
        await sara.Locator("#waiting li button[value=accept]").First.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people\?said=accepted$"));

        // Both are there, side by side, each with what it says – and each with its own audience and its own end.
        var mine = await sara.Locator("#mine > ul.people").InnerTextAsync();
        Assert.Contains(friend, mine, StringComparison.Ordinal);
        Assert.Contains(colleague, mine, StringComparison.Ordinal);
        Assert.Contains("نشر زابل", mine, StringComparison.Ordinal);
        Assert.Contains(language == "fa" ? "۱۳۸۹" : "1389", mine, StringComparison.Ordinal);
        Assert.Equal(2, await sara.Locator("#mine form[action$='/people/end']").CountAsync());
        foreach (var more in await sara.Locator("#mine details > summary").AllAsync())
        {
            await more.ClickAsync();
        }

        await Fits(sara);

        // Both let everybody see the friendship; the page about Sara shows it to a stranger – and Sara sees
        // that by looking at her page as everybody.
        foreach (var page in new[] { sara, mina })
        {
            await page.GotoAsync(people);
            // The friendship stands first: the kinds of one person are listed in the order of the form.
            var shown = page.Locator("#mine > ul.people > li").First.Locator("form[action$='/people/show']");
            Assert.Contains(friend, await page.Locator("#mine > ul.people > li").First.Locator("span.meta").First.InnerTextAsync(), StringComparison.Ordinal);
            await shown.Locator("select").SelectOptionAsync("public");
            await shown.Locator("button[type=submit]").ClickAsync();
            await BrowserSite.Arrived(page, new Regex(@"/account/people\?said=shown$"));
        }

        await sara.Locator("header.page-header a[href^='people/']").ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/people/[a-f0-9]{32}$"));
        await sara.Locator("#see-as-section select").SelectOptionAsync("public");
        await sara.Locator("#see-as-section button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/people/[a-f0-9]{32}\?as=public$"));
        var seen = await sara.Locator("main").InnerTextAsync();
        Assert.Contains(minaName, seen, StringComparison.Ordinal);
        Assert.Contains(friend, seen, StringComparison.Ordinal);
        Assert.DoesNotContain("نشر زابل", await sara.Locator("section[aria-labelledby=person-relationships]").InnerTextAsync(), StringComparison.Ordinal);
        await Fits(sara);

        // A place, shown to everybody, and found through by the other.
        await sara.GotoAsync($"/{language}/account/affiliations");
        var add = sara.Locator("#add form[method=post]");
        await add.Locator("[name=place]").FillAsync("مدرسهٔ کابل");
        await add.Locator("[name=since]").FillAsync("1377");
        await add.Locator("[name=until]").FillAsync("1381");
        await add.Locator("[name=calendar]").SelectOptionAsync("solar-hijri");
        await add.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/affiliations\?said=added#mine$"));
        var show = sara.Locator("#mine form[action$='/affiliations/show']");
        await show.Locator("[name=findable]").SelectOptionAsync("public");
        await show.Locator("[name=dates]").SelectOptionAsync("public");
        await show.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/affiliations\?said=shown#mine$"));
        await sara.Locator("#mine details > summary").ClickAsync();
        await Fits(sara);

        await mina.GotoAsync($"/{language}/account/affiliations");
        var find = mina.Locator("#find form");
        await find.Locator("[name=place]").FillAsync("مدرسهٔ کابل");
        await find.Locator("[name=year]").FillAsync("1379");
        await find.Locator("[name=calendar]").SelectOptionAsync("solar-hijri");
        await find.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/affiliations\?place="));
        Assert.Contains(saraName, await mina.Locator("#find").InnerTextAsync(), StringComparison.Ordinal);
        await Fits(mina);
    }
}
