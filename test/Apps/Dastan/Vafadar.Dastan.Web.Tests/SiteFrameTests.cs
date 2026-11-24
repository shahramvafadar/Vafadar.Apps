using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The frame of every page (step 20.5, ADR 0024): the main ways – Discover, the private space, what was kept,
/// the search –, the account, and the language and the appearance behind one control. And the page of the
/// parts, which exists on a machine of development only.
/// </summary>
public sealed class SiteFrameTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Story = "story-rostam-sohrab";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private static string Nav(Page page) => Regex.Match(page.Html, "<nav class=\"site-nav\".*?</nav>", RegexOptions.Singleline).Value;

    private static string[] Ways(Page page) => [.. Regex.Matches(Nav(page), "<a href=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];

    private static string? Current(Page page) => Regex.Match(Nav(page), "<a href=\"([^\"]*)\" aria-current=\"page\"").Groups[1] is { Success: true } way ? way.Value : null;

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    [Theory]
    [InlineData("en", "Discover Side by side My World Saved Search")]
    [InlineData("fa", "کاوش کنار هم دنیای من ذخیره‌ها جست‌وجو")]
    public async Task The_main_ways_are_one_list_and_what_was_kept_has_its_place_while_it_is_empty(string language, string names)
    {
        var page = await _site.Visitor().Get($"/{language}/");

        Assert.Equal(["./", "compare", "account", "saved", "search"], Ways(page));
        Assert.Equal(names, Words(Nav(page)));
        Assert.Equal("./", Current(page));

        // Each way carries its drawing as decoration: the words name it.
        Assert.Equal(5, Regex.Matches(Nav(page), "<svg class=\"icon\"[^>]*aria-hidden=\"true\"").Count);

        // The place of what was kept says that it is empty, and how it is filled.
        var saved = await _site.Visitor().Get($"/{language}/saved");
        Assert.Equal(HttpStatusCode.OK, saved.Status);
        Assert.Equal("saved", Current(saved));
    }

    [Fact]
    public async Task The_way_a_page_lies_on_is_marked_and_a_page_of_a_collection_lies_on_none()
    {
        var visitor = _site.Visitor();

        Assert.Equal("search", Current(await visitor.Get("/en/search?q=rostam")));
        Assert.Null(Current(await visitor.Get($"/en/{Story}/entity/character-rostam")));
        Assert.Null(Current(await visitor.Get("/en/privacy")));

        // The page of the account is the account's, not the private space.
        var account = await visitor.Get("/en/account");
        Assert.Null(Current(account));
        Assert.Matches("<a class=\"account-link\" href=\"account\" aria-current=\"page\">", account.Html);
    }

    [Fact]
    public async Task Who_signed_in_is_led_to_their_people_and_shown_their_name_and_how_many_wait()
    {
        async Task<Browser> Signed(string name)
        {
            var visitor = _site.Visitor();
            await visitor.Post("/preferences/account/register", "/en/account", ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", "/en/account"));
            return visitor;
        }

        var (mina, sara) = (await Signed("Mina"), await Signed("Sara"));
        var start = await mina.Get("/en/");
        Assert.Equal(["./", "compare", "account/world", "saved", "search"], Ways(start));
        Assert.Contains("<bdi class=\"name\">Mina</bdi>", start.Html, StringComparison.Ordinal);
        Assert.Equal("account/world", Current(await mina.Get("/en/account/trees")));

        // Somebody asks: the way to the private space leads to who waits and says how many.
        await mina.Post("/preferences/people/ask", "/en/account/people", ("name", "Sara"), ("kind", "friend"), ("returnUrl", "/en/account/people"));
        var asked = await sara.Get("/en/");
        Assert.Equal(["./", "compare", "account/world#waiting", "saved", "search"], Ways(asked));
        Assert.Equal("Discover Side by side My World 1 – 1 waiting Saved Search", Words(Nav(asked)));
        Assert.Contains("<span class=\"count\" aria-hidden=\"true\">1</span>", Nav(asked), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_without_accounts_has_no_private_space_and_no_account()
    {
        using var site = new DemoSite();
        var page = await site.Visitor().Get("/en/");

        Assert.Equal(["./", "compare", "saved", "search"], Ways(page));
        Assert.DoesNotContain("account-link", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Language and appearance", "/fa/")]
    [InlineData("fa", "زبان و ظاهر", "/en/")]
    public async Task The_language_and_the_appearance_stand_together_behind_one_control_that_needs_no_script(string language, string name, string other)
    {
        var page = await _site.Visitor().Get($"/{language}/");
        var settings = Regex.Match(page.Html, "<details class=\"site-settings\">.*?</details>", RegexOptions.Singleline).Value;

        Assert.StartsWith(name, Words(Regex.Match(settings, "<summary>.*?</summary>", RegexOptions.Singleline).Value), StringComparison.Ordinal);
        Assert.Contains($"href=\"{other}\"", settings, StringComparison.Ordinal);
        Assert.Contains("action=\"/preferences/theme\"", settings, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Matches(settings, "name=\"theme\" value=\"(auto|light|dark)\"").Count);

        // They stand nowhere else: the ways are ways, and these are settings.
        Assert.DoesNotContain("hreflang", Nav(page), StringComparison.Ordinal);
        Assert.Single(Regex.Matches(page.Html, "action=\"/preferences/theme\""));
    }

    [Fact]
    public async Task The_page_of_the_parts_shows_every_part_on_a_machine_of_development()
    {
        var page = await _site.Visitor().Get("/en/parts?chip=2");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("noindex", page.Html, StringComparison.Ordinal);
        foreach (var part in new[] { "class=\"identity is-", "class=\"identity-mark mark-record\"", "class=\"view-bar\"", "class=\"node-card tall\"", "class=\"chips\"", "class=\"state state-waiting\"", "class=\"state state-nothing\"", "class=\"state state-failed\"", "id=\"parts-audience-choice\"" })
        {
            Assert.Contains(part, page.Html, StringComparison.Ordinal);
        }

        // A state that went wrong is said at once; a filter that is set says so without its colour.
        Assert.Matches("<div class=\"state state-failed\" role=\"alert\">", page.Html);
        Assert.Single(Regex.Matches(page.Html, "<a class=\"chip\" href=\"[^\"]*\" aria-current=\"true\">"));

        // The view that is shown is marked, and the choice of an audience shows what is chosen.
        Assert.Single(Regex.Matches(Regex.Match(page.Html, "<nav class=\"view-bar\".*?</nav>", RegexOptions.Singleline).Value, "aria-current=\"page\""));
        Assert.Matches("<option value=\"circle-2\" selected", page.Html);

        // The page carries no style of its own: the policy of the site would not let it.
        Assert.DoesNotContain(" style=\"", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_that_runs_for_anybody_has_no_page_of_the_parts()
    {
        using var site = new PublicSite();

        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get("/en/parts")).Status);
    }
}
