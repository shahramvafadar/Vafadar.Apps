using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using SkiaSharp;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The site of the journeys: the content of the demonstration with times of the calendar stated about the work –
/// test data, written into a copy – so that a visitor can compare the times of two names.
/// </summary>
public sealed class JourneysBrowserSite : BrowserSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-journeys-" + Guid.NewGuid().ToString("N"), "content");

    /// <inheritdoc />
    protected override void Settings(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
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

        var path = Path.Combine(_content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var work = sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == "work-shahnameh")!;
        work["dated"] = JsonNode.Parse(
            """
            [
              {
                "key": "work-written",
                "label": "The work is written",
                "summary": "Test data of the tests.",
                "time": {
                  "kind": "period",
                  "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 977, "approximate": true } },
                  "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
                  "continuity": "notAsserted"
                },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 0,
                "reviewStatus": "proposed"
              }
            ]
            """);
        File.WriteAllText(path, sample.ToJsonString());
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
}

/// <summary>
/// The journeys of the review of phase 20, walked in a browser (step 20.12) – on a narrow screen, and with the
/// keyboard where a journey is one of looking: an unknown subject found, followed and left again; times
/// compared and their pictures found; a story read with nothing of what lies ahead in any answer of the site.
/// The journeys of two people are <see cref="TwoPeopleJourneyBrowserTests"/>.
/// </summary>
public sealed class SevenJourneysBrowserTests(JourneysBrowserSite site) : IClassFixture<JourneysBrowserSite>
{
    private const string Story = BrowserSite.Story;

    private static async Task Fits(IPage page)
    {
        Assert.False(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"), $"{page.Url} reaches beyond the screen.");
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

    [Theory]
    [InlineData("en", "Rakhsh")]
    [InlineData("fa", "رخش")]
    public async Task An_unknown_subject_is_found_followed_and_left_again_with_the_keyboard_alone_on_a_narrow_screen(string language, string name)
    {
        var page = await site.Visitor(360, readTo: 4);
        await page.GotoAsync($"/{language}/");
        var start = page.Url;

        // Found: the search is typed into and sent with the keyboard; the first result is opened with it.
        await page.Locator(".hero input[type=search]").FocusAsync();
        await page.Keyboard.TypeAsync(name);
        await page.Keyboard.PressAsync("Enter");
        await BrowserSite.Arrived(page, new Regex(@"/search\?q="));
        var found = page.Locator("main a[href*='/entity/character-rakhsh']").First;
        await found.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rakhsh$"));
        await Assertions.Expect(page.Locator(".identity h1")).ToContainTextAsync(name);
        await Fits(page);

        // Followed: the first thing to do in the head leads to the connections, and a name there leads on.
        await page.Locator(".identity-actions a.action").First.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rakhsh/connections$"));
        await Fits(page);
        var on = page.Locator("main ul.statements a[href*='/entity/character-rostam']").First;
        await on.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rostam"));

        // And back: every step is undone by going back, down to where the journey began.
        for (var steps = 0; steps < 6 && page.Url != start; steps++)
        {
            await page.GoBackAsync();
        }

        Assert.Equal(start, page.Url);
        await Assertions.Expect(page.Locator(".hero input[type=search]")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task The_times_of_two_names_are_compared_and_their_pictures_found()
    {
        var page = await site.Visitor(390, readTo: 4);
        await page.GotoAsync($"/en/{Story}/entity/character-rostam/connections?sel=work-shahnameh");

        // Something chosen that has times of its own is added to the comparison; the chart of time shows both.
        await page.Locator(".explore-chosen a", new() { HasTextString = "Add to the comparison" }).ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"with=work-shahnameh"));
        await page.Locator(".explore-compared a", new() { HasTextString = "Compare the times" }).ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rostam/timeline\?.*with=work-shahnameh"));
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("The work is written");
        await Fits(page);

        // The pictures of the name are one view away, and the comparison is not lost on the way there and back.
        await page.Locator(".view-switch a", new() { HasTextString = "Pictures" }).ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rostam/gallery\?.*with=work-shahnameh"));
        Assert.True(await page.Locator(".cards.gallery .media-card").CountAsync() > 0);
        await Fits(page);
        await page.Locator(".cards.gallery .media-card h2 a").First.ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/media/"));
        await page.Locator("header.page-header .meta a").First.ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/entity/character-rostam/gallery"));

        // The way back from the pictures returns where one was: the gallery, and before it the chart of the
        // two names – the bar of the views of Rostam offers no chart of time of his own, for nothing of him is dated.
        Assert.Equal(0, await page.Locator(".view-switch a", new() { HasTextString = "Timeline" }).CountAsync());
        for (var steps = 0; steps < 4 && !page.Url.Contains("/timeline", StringComparison.Ordinal); steps++)
        {
            await page.GoBackAsync();
        }

        Assert.Matches(@"/entity/character-rostam/timeline\?.*with=work-shahnameh", page.Url);
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("The work is written");
    }

    [Fact]
    public async Task A_story_is_read_and_no_answer_of_the_site_holds_what_lies_ahead()
    {
        // The reader is at the second stage. Sohrab is told from the third on; the fight and its end later.
        var page = await site.Visitor(390, readTo: 1, graph: true);
        var answers = new List<(string Address, string Body)>();
        page.Response += async (_, response) =>
        {
            var kind = response.Headers.GetValueOrDefault("content-type") ?? string.Empty;
            if (kind.Contains("html", StringComparison.Ordinal) || kind.Contains("json", StringComparison.Ordinal))
            {
                try
                {
                    answers.Add((response.Url, await response.TextAsync()));
                }
                catch (PlaywrightException)
                {
                    // An answer the page left before it was read has nothing to read.
                }
            }
        };

        // The story, a name, its connections and family as drawings that move, a profile in the inspector, the
        // pictures, the timeline, and the search for the very name that lies ahead.
        foreach (var address in new[]
        {
            $"/en/{Story}", $"/en/{Story}/story", $"/en/{Story}/entity/character-rostam",
            $"/en/{Story}/entity/character-rostam/gallery", $"/en/{Story}/timeline", $"/en/{Story}/gallery",
            $"/en/{Story}/search?q=Sohrab", $"/en/search?q=Sohrab",
        })
        {
            await page.GotoAsync(address);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        foreach (var drawing in new[] { "connections", "family?family=whole" })
        {
            await page.GotoAsync($"/en/{Story}/entity/character-rostam/{drawing}");
            await Assertions.Expect(page.Locator("dastan-graph")).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 30_000 });
            await page.Locator("[data-graph-canvas]").FocusAsync();
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // What is asked for by its address all the same is not there – and says nothing in saying so.
        foreach (var hidden in new[] { "entity/character-sohrab", "entity/character-sohrab/profile.json", "entity/character-sohrab/connections.json", "event/event-06/gallery" })
        {
            var answer = await page.APIRequest.GetAsync($"/en/{Story}/{hidden}");
            Assert.Equal(404, answer.Status);

            // The answer is the one for a name nobody has: it differs in nothing but the address that was asked.
            var nobody = await page.APIRequest.GetAsync($"/en/{Story}/{hidden.Replace("character-sohrab", "character-nobody", StringComparison.Ordinal).Replace("event-06", "event-99", StringComparison.Ordinal)}");
            Assert.Equal(404, nobody.Status);
            // (The token of a form is made anew for every answer and says nothing of what was asked.)
            static string Plain(string body, string asked) => Regex.Replace(body.Replace(asked, "X", StringComparison.Ordinal), "CfDJ8[A-Za-z0-9_-]+", "T");
            Assert.Equal(
                Plain(Plain(await nobody.TextAsync(), "character-nobody"), "event-99"),
                Plain(Plain(await answer.TextAsync(), "character-sohrab"), "event-06"));
        }

        Assert.True(answers.Count > 12, $"Only {answers.Count} answers were read.");
        Assert.Contains(answers, answer => answer.Address.Contains("connections.json", StringComparison.Ordinal));
        Assert.Contains(answers, answer => answer.Address.Contains("family.json", StringComparison.Ordinal));
        foreach (var (address, body) in answers)
        {
            // The tale is called "Rostam and Sohrab", and sources of the collection carry that title: the name of
            // the tale is no secret. What must not be there is the record of somebody not read to – its
            // identifier, its name as a name of the site (a link, a card, a row), a step that was not read.
            Assert.False(body.Contains("character-sohrab", StringComparison.Ordinal), $"{address} names the record of somebody not read to.");
            Assert.False(Regex.IsMatch(body, "event-0[3-8]"), $"{address} names a step that was not read.");
            Assert.False(Regex.IsMatch(body, "<bdi>Sohrab</bdi>|\"name\":\"Sohrab\"|>Sohrab</a>"), $"{address} shows somebody not read to by name.");
            foreach (var later in new[] { "Sohrab learns his lineage", "An expedition draws rival plans", "A chance to stop the fighting", "The token reveals the relationship", "The remedy does not arrive", "Mourning in Zabulistan" })
            {
                Assert.False(body.Contains(later, StringComparison.Ordinal), $"{address} tells of a step that was not read: {later}.");
            }
        }
    }
}

/// <summary>
/// The journeys of two people (step 20.12), walked on a narrow screen: two kinds of relationship asked, each
/// accepted by itself, and one ended; a place somebody was at twice, by which she is found while its dates
/// stay hers; a family tree and a photograph that the other sees and, after it is taken back, is refused.
/// One test and two accounts: a site takes five new accounts an hour from one address.
/// </summary>
public sealed class TwoPeopleJourneyBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
{
    private static async Task<(IPage Page, string Name)> Signed(AccountsBrowserSite site, string who)
    {
        var page = await site.Visitor(375);
        var (name, address) = (AccountJourneyBrowserTests.NewName(who), MailCatcher.NewAddress(who.ToLowerInvariant()));
        await AccountJourneyBrowserTests.Ask(page, "en", name, address);
        await page.GotoAsync(await AccountJourneyBrowserTests.Link(address, "Confirm"));
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));
        return (page, name);
    }

    private static Task<bool> Fits(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1");

    private static byte[] Picture()
    {
        using var bitmap = new SKBitmap(400, 300);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0x3F, 0x6C, 0x8A));
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    [Fact]
    public async Task Two_people_connect_are_found_share_a_tree_and_a_photograph_and_take_each_back()
    {
        MailCatcher.Needed();
        var (mina, minaName) = await Signed(site, "Mina");
        var (sara, saraName) = await Signed(site, "Sara");
        const string People = "/en/account/people";

        // Several relationships: asked one by one, found waiting in My World, accepted each by itself.
        foreach (var kind in new[] { "friend", "colleague" })
        {
            await mina.GotoAsync(People);
            var ask = mina.Locator("#ask form[method=post]");
            await ask.Locator("[name=name]").FillAsync(saraName);
            await ask.Locator("[name=kind]").SelectOptionAsync(kind);
            await ask.Locator("button[type=submit]").ClickAsync();
            await BrowserSite.Arrived(mina, new Regex(@"/account/people\?said=asked$"));
        }

        await sara.GotoAsync("/en/account/world");
        Assert.Equal(2, await sara.Locator("#waiting li").CountAsync());
        Assert.True(await Fits(sara));
        await sara.Locator("#waiting li a").First.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people#waiting$"));
        await sara.Locator("#waiting li button[value=accept]").First.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people\?said=accepted$"));
        Assert.Equal(1, await sara.Locator("#waiting li").CountAsync());
        Assert.Equal(1, await sara.Locator("#mine form[action$='/people/end']").CountAsync());
        await sara.Locator("#waiting li button[value=accept]").First.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people\?said=accepted$"));
        Assert.Equal(2, await sara.Locator("#mine form[action$='/people/end']").CountAsync());

        // One is ended, by itself: the other stays.
        await sara.Locator("#mine form[action$='/people/end'] button").Last.ClickAsync();
        await BrowserSite.Arrived(sara, new Regex(@"/account/people\?said="));
        Assert.Equal(1, await sara.Locator("#mine form[action$='/people/end']").CountAsync());
        await mina.GotoAsync(People);
        Assert.Equal(1, await mina.Locator("#mine form[action$='/people/end']").CountAsync());

        // A place with two stretches. Being found by it is one answer; showing its dates is another, not given.
        foreach (var (since, until) in new[] { ("1990", "1994"), ("2001", "2003") })
        {
            await sara.GotoAsync("/en/account/affiliations");
            var add = sara.Locator("#add form[method=post]");
            await add.Locator("[name=place]").FillAsync("The Made-up University of Tests");
            await add.Locator("[name=since]").FillAsync(since);
            await add.Locator("[name=until]").FillAsync(until);
            await add.Locator("button[type=submit]").ClickAsync();
            await BrowserSite.Arrived(sara, new Regex(@"/account/affiliations\?said=added#mine$"));
        }

        var shown = sara.Locator("#mine form[action$='/affiliations/show']");
        Assert.Equal(2, await shown.CountAsync());
        for (var index = 0; index < 2; index++)
        {
            var one = sara.Locator("#mine form[action$='/affiliations/show']").Nth(index);
            await one.Locator("[name=findable]").SelectOptionAsync("public");
            await one.Locator("button[type=submit]").ClickAsync();
            await BrowserSite.Arrived(sara, new Regex(@"/account/affiliations\?said=shown#mine$"));
        }

        Assert.True(await Fits(sara));
        await mina.GotoAsync("/en/account/affiliations");
        var find = mina.Locator("#find form");
        await find.Locator("[name=place]").FillAsync("The Made-up University of Tests");
        await find.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/affiliations\?place="));
        var result = await mina.Locator("#find").InnerTextAsync();
        Assert.Contains(saraName, result, StringComparison.Ordinal);
        Assert.DoesNotContain("1990", result, StringComparison.Ordinal);
        Assert.DoesNotContain("2001", result, StringComparison.Ordinal);

        // A family tree: the other may see it while she is named, and it is not there for her after.
        await mina.GotoAsync("/en/account/trees");
        await mina.Locator("#tree-name").FillAsync("The made-up family");
        await mina.Locator("form[action$='/trees/add'] button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/trees/[a-f0-9]{32}\?said=made$"));
        var tree = new Uri(mina.Url).AbsolutePath;
        Assert.Equal(404, (await sara.GotoAsync(tree))!.Status);
        await mina.Locator(".task-nav a[href$='#share']").ClickAsync();
        var member = mina.Locator("#members form[action$='/trees/member']").First;
        await member.Locator("[name=role]").SelectOptionAsync("viewer");
        await member.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"said=member-set#members$"));
        Assert.Equal(200, (await sara.GotoAsync(tree))!.Status);
        Assert.Equal(1, await sara.Locator("section.task").CountAsync());
        Assert.Equal(0, await sara.Locator("form[action*='/trees/person-add']").CountAsync());
        Assert.True(await Fits(sara));
        member = mina.Locator("#members form[action$='/trees/member']").First;
        await member.Locator("[name=role]").SelectOptionAsync("none");
        await member.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"said=member-set#members$"));
        Assert.Equal(404, (await sara.GotoAsync(tree))!.Status);

        // A photograph: shown to everybody it is delivered to the other; taken back, it is refused to her.
        await mina.GotoAsync("/en/account/photos");
        var bring = mina.Locator("form[action$='/photos/add']");
        await bring.Locator("[name=file]").SetInputFilesAsync(new FilePayload { Name = "made-up.png", MimeType = "image/png", Buffer = Picture() });
        await bring.Locator("[name=scope]").SelectOptionAsync("public");
        await bring.Locator("[name=confirmed]").CheckAsync();
        await bring.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/photos\?said="));
        var file = await mina.Locator("#mine img").First.GetAttributeAsync("src");
        Assert.Matches("^/?photo/[a-f0-9]{32}", file);
        var photo = "/" + file!.TrimStart('/');
        Assert.Equal(200, (await sara.APIRequest.GetAsync(photo)).Status);
        var share = mina.Locator("#mine form[action$='/photos/share']").First;
        await share.Locator("select").SelectOptionAsync("private");
        await share.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(mina, new Regex(@"/account/photos\?said="));
        Assert.Equal(404, (await sara.APIRequest.GetAsync(photo)).Status);
        Assert.Equal(200, (await mina.APIRequest.GetAsync(photo)).Status);
        Assert.True(await Fits(mina));
        Assert.Contains(minaName, await mina.Locator(".account-link").InnerTextAsync(), StringComparison.Ordinal);
    }
}
