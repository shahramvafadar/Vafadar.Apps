using System.Net;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The list of a visitor (step 15.1, B-016): pages kept in a cookie of their own device, without an account.
/// The list never shows more than its pages do, and the site keeps nothing of it.
/// </summary>
public sealed class SavedListTests(DemoSite site, TwoCollectionsSite two) : IClassFixture<DemoSite>, IClassFixture<TwoCollectionsSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"/en/{Story}/entity/character-rostam";

    // The list as the browser holds it: the cookie, as the site wrote it.
    private static string Kept(Browser visitor) => Uri.UnescapeDataString(visitor.Cookies[SavedList.CookieName]);

    // Writes another list into the cookie the browser already has – as somebody could who edits their cookies.
    private static async Task Rewrite(Browser visitor, string written)
    {
        await Send(visitor, Rostam, "entity", "character-rostam", "keep");
        visitor.Cookies[SavedList.CookieName] = written;
    }

    private static Task<Page> Send(Browser visitor, string from, string kind, string id, string act, string collection = Story) =>
        visitor.Post(SavedList.Address, from, ("collection", collection), ("kind", kind), ("id", id), ("act", act), ("returnUrl", from));

    [Theory]
    [InlineData("en", "Keep in my list", "Take out of my list", "My list")]
    [InlineData("fa", "نگه‌داشتن در فهرست من", "برداشتن از فهرست من", "فهرست من")]
    public async Task A_page_is_kept_with_one_button_and_then_stands_in_the_list(string language, string keep, string drop, string list)
    {
        var visitor = site.Visitor();
        var address = $"/{language}/{Story}/entity/character-rostam";
        var before = await visitor.Get(address);

        Assert.Contains(keep, before.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"saved\"", before.Html, StringComparison.Ordinal);

        var kept = await Send(visitor, address, "entity", "character-rostam", "keep");
        Assert.Equal(HttpStatusCode.Redirect, kept.Status);
        Assert.Equal(address, kept.Response.Headers.Location!.OriginalString);
        Assert.Equal($"{Story}/entity/character-rostam", Kept(visitor));

        var after = await visitor.Get(address);
        Assert.Contains(drop, after.Main, StringComparison.Ordinal);
        Assert.Contains("href=\"saved\"", after.Html, StringComparison.Ordinal);

        var mine = await visitor.Get($"/{language}/saved");
        Assert.Equal(HttpStatusCode.OK, mine.Status);
        Assert.Contains(list, mine.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", mine.Html, StringComparison.Ordinal);
        Assert.Contains("content=\"noindex, nofollow\"", mine.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", mine.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_cookie_of_the_list_cannot_be_read_by_a_script_and_holds_nothing_but_the_pages()
    {
        var visitor = site.Visitor();
        var kept = await Send(visitor, Rostam, "entity", "character-rostam", "keep");

        var cookie = Assert.Single(kept.Response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(SavedList.CookieName + "=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("en", "Nothing is in your list.", "in this browser only")]
    [InlineData("fa", "چیزی در فهرست شما نیست.", "فقط در همین مرورگر")]
    public async Task Without_a_list_the_page_says_so_and_where_a_list_would_be_kept(string language, string empty, string where)
    {
        var page = await site.Visitor().Get($"/{language}/saved");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(empty, page.Main, StringComparison.Ordinal);
        Assert.Contains(where, page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Taking_the_last_page_out_takes_the_cookie_away()
    {
        var visitor = site.Visitor();
        await Send(visitor, Rostam, "entity", "character-rostam", "keep");
        await Send(visitor, Rostam, "entity", "character-tahmineh", "drop");
        Assert.Equal($"{Story}/entity/character-rostam", Kept(visitor));

        await Send(visitor, Rostam, "entity", "character-rostam", "drop");

        Assert.False(visitor.Cookies.TryGetValue(SavedList.CookieName, out var left) && left.Length > 0);
        Assert.Contains("Keep in my list", (await visitor.Get(Rostam)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_page_the_visitor_cannot_open_is_not_kept()
    {
        var visitor = site.Visitor();
        var later = site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;

        var refused = await Send(visitor, Rostam, "entity", later, "keep");
        var nobody = await Send(visitor, Rostam, "entity", "nobody-here", "keep");
        var elsewhere = await Send(visitor, Rostam, "entity", "character-rostam", "keep", collection: "another-story");

        Assert.Equal(HttpStatusCode.SeeOther, refused.Status);
        Assert.Equal(HttpStatusCode.SeeOther, nobody.Status);
        Assert.Equal(HttpStatusCode.SeeOther, elsewhere.Status);
        Assert.False(visitor.Cookies.ContainsKey(SavedList.CookieName));
    }

    [Fact]
    public async Task A_page_that_was_kept_and_is_not_reached_any_more_is_neither_shown_nor_counted()
    {
        var visitor = site.Visitor();
        var later = site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;
        await visitor.ReadTo(4);
        await Send(visitor, Rostam, "entity", later, "keep");
        await Send(visitor, Rostam, "media", "media-dastan-parentage-video", "keep");
        await Send(visitor, Rostam, "entity", "character-rostam", "keep");
        Assert.Contains($"href=\"{Story}/entity/{later}\"", (await visitor.Get("/en/saved")).Html, StringComparison.Ordinal);

        // The visitor moves the reading position back: the list shows what can be opened now, and no gap.
        await visitor.ReadTo(0);
        var page = await visitor.Get("/en/saved");

        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(later, page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("media-dastan-parentage-video", page.Html, StringComparison.Ordinal);
        Assert.Single(page.Html.Split("name=\"act\" value=\"drop\"").Skip(1));
    }

    [Theory]
    [InlineData("entity", "character-rostam", "burn")]
    [InlineData("person", "character-rostam", "keep")]
    [InlineData("entity", "../../etc", "keep")]
    [InlineData("entity", "", "keep")]
    public async Task What_is_no_act_of_the_list_is_refused(string kind, string id, string act)
    {
        var visitor = site.Visitor();

        Assert.Equal(HttpStatusCode.SeeOther, (await Send(visitor, Rostam, kind, id, act)).Status);
        Assert.False(visitor.Cookies.ContainsKey(SavedList.CookieName));
    }

    [Theory]
    [InlineData("%3Cscript%3Ealert(1)%3C/script%3E")]
    [InlineData("a/b/c/d|x")]
    [InlineData("story-rostam-sohrab/person/character-rostam")]
    public async Task A_cookie_that_is_no_list_is_an_empty_list(string written)
    {
        var visitor = site.Visitor();
        await Rewrite(visitor, written);

        var page = await visitor.Get("/en/saved");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Nothing is in your list.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("alert(1)", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_full_list_lets_its_oldest_page_go()
    {
        var visitor = site.Visitor();
        await Rewrite(visitor, Uri.EscapeDataString(string.Join('|', Enumerable.Range(0, SavedList.Most).Select(number => $"{Story}/entity/kept-{number}"))));

        await Send(visitor, Rostam, "entity", "character-rostam", "keep");

        var entries = Kept(visitor).Split('|');
        Assert.Equal(SavedList.Most, entries.Length);
        Assert.Equal($"{Story}/entity/character-rostam", entries[^1]);
        Assert.DoesNotContain($"{Story}/entity/kept-0", entries);
        Assert.Equal($"{Story}/entity/kept-1", entries[0]);
    }

    [Fact]
    public async Task The_list_holds_pages_of_every_collection_each_under_its_own()
    {
        var visitor = two.Visitor();
        var club = TwoCollectionsSite.Club;
        await Send(visitor, Rostam, "entity", "character-rostam", "keep");
        await Send(visitor, $"/en/{club}", "entity", club, "keep", collection: club);

        var page = await visitor.Get("/en/saved");

        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{club}/entity/{club}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"id=\"saved-{club}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"id=\"saved-{Story}\"", page.Html, StringComparison.Ordinal);

        // A page of one collection is not kept under the name of the other.
        Assert.Equal(HttpStatusCode.SeeOther, (await Send(visitor, Rostam, "entity", "character-rostam", "keep", collection: club)).Status);
    }
}
