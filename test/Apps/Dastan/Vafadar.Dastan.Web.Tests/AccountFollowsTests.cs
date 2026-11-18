using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// What follows a visitor who signed in (step 16.2): the reading position and the list are those of the
/// account on every device, a device keeps its own for who is signed out, and invitations are links the sender
/// passes on (D-049, PRIV-006). Every test has a site of its own.
/// </summary>
public sealed class AccountFollowsTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"/en/{Story}/entity/character-rostam";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private static async Task<Page> Register(Browser visitor, string name = "Mina", string invitation = "") =>
        await visitor.Post($"{Forms}/register", Account, ("name", name), ("password", Password), ("again", Password), ("invitation", invitation), ("returnUrl", Account));

    private static async Task<Page> SignIn(Browser visitor, string name = "Mina") =>
        await visitor.Post($"{Forms}/sign-in", Account, ("name", name), ("password", Password), ("returnUrl", Account));

    private static async Task<Page> SignOut(Browser visitor) =>
        await visitor.Post($"{Forms}/sign-out", Account, ("returnUrl", Account));

    private static Task<Page> Keep(Browser visitor, string id, string act = "keep", string kind = "entity") =>
        visitor.Post(SavedList.Address, Rostam, ("collection", Story), ("kind", kind), ("id", id), ("act", act), ("returnUrl", Rostam));

    // Where the footer of a page says the visitor has read to.
    private static async Task<string> ReadTo(Browser visitor) =>
        Regex.Match((await visitor.Get($"/en/{Story}")).Text, "You are at: (.+?)\\s+Change").Groups[1].Value;

    private static async Task<string[]> Invitations(Browser visitor) =>
        [.. Regex.Matches((await visitor.Get(Account)).Html, "/en/account\\?invitation=([A-Z0-9]{20})</code>").Select(match => match.Groups[1].Value)];

    [Fact]
    public async Task How_far_a_signed_in_visitor_has_read_is_the_same_on_every_device()
    {
        var here = _site.Visitor();
        await Register(here);
        var there = _site.Visitor();
        await SignIn(there);
        var start = await ReadTo(_site.Visitor());

        await here.ReadTo(3);

        var read = await ReadTo(here);
        Assert.NotEqual(start, read);
        Assert.Equal(read, await ReadTo(there));

        // Kept with the account: the device got no cookie for it.
        Assert.DoesNotContain(ReadingPreference.CookieName(Story), here.Cookies.Keys);

        // Moving back is the same act, and it follows too.
        await there.ReadTo(0);
        Assert.Equal(start, await ReadTo(here));
    }

    [Fact]
    public async Task What_a_later_stage_holds_is_opened_on_another_device_only_through_the_account()
    {
        var later = _site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;
        var here = _site.Visitor();
        await Register(here);
        await here.ReadTo(4);
        var there = _site.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await there.Get($"/en/{Story}/entity/{later}")).Status);
        await SignIn(there);
        Assert.Equal(HttpStatusCode.OK, (await there.Get($"/en/{Story}/entity/{later}")).Status);
        await SignOut(there);
        Assert.Equal(HttpStatusCode.NotFound, (await there.Get($"/en/{Story}/entity/{later}")).Status);
    }

    [Fact]
    public async Task Signed_out_a_device_shows_what_it_holds_itself()
    {
        var visitor = _site.Visitor();
        await visitor.ReadTo(2);
        var own = await ReadTo(visitor);
        await Keep(visitor, "character-rostam");
        await SignOutAfterRegistering(visitor);

        // The account was moved on; the device was not.
        Assert.Equal(own, await ReadTo(visitor));
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", (await visitor.Get("/en/saved")).Html, StringComparison.Ordinal);

        async Task SignOutAfterRegistering(Browser browser)
        {
            await Register(browser);
            await browser.ReadTo(4);
            await Keep(browser, "character-rostam", "drop");
            Assert.NotEqual(own, await ReadTo(browser));
            await SignOut(browser);
        }
    }

    [Fact]
    public async Task A_new_account_begins_with_what_its_device_holds()
    {
        var visitor = _site.Visitor();
        await visitor.ReadTo(3);
        var read = await ReadTo(visitor);
        await Keep(visitor, "character-rostam");
        Assert.Contains("goes into the new account", (await visitor.Get(Account)).Main, StringComparison.Ordinal);

        await Register(visitor);

        var there = _site.Visitor();
        await SignIn(there);
        Assert.Equal(read, await ReadTo(there));
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", (await there.Get("/en/saved")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signing_in_takes_nothing_from_a_device_until_its_visitor_says_so()
    {
        var first = _site.Visitor();
        await Register(first);
        await first.ReadTo(1);
        var account = await ReadTo(first);
        var device = _site.Visitor();
        await device.ReadTo(4);
        await Keep(device, "character-rostam");

        await SignIn(device);

        // The account says how far its visitor has read; the list of the device is not the account's.
        Assert.Equal(account, await ReadTo(device));
        Assert.DoesNotContain("character-rostam", (await first.Get("/en/saved")).Main, StringComparison.Ordinal);
        var page = await device.Get(Account);
        Assert.Contains("This device holds a list or a reading position of its own.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/bring\"", page.Html, StringComparison.Ordinal);

        var brought = await device.Post($"{Forms}/bring", Account, ("returnUrl", Account));

        Assert.Equal("/en/account?said=brought", brought.Response.Headers.Location?.OriginalString);
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", (await first.Get("/en/saved")).Html, StringComparison.Ordinal);

        // What the account said about the story is not moved by the device.
        Assert.Equal(account, await ReadTo(first));
        Assert.DoesNotContain($"action=\"{Forms}/bring\"", (await _site.Visitor().Get(Account)).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_list_of_a_signed_in_visitor_is_kept_with_the_account()
    {
        var here = _site.Visitor();
        await Register(here);
        var there = _site.Visitor();
        await SignIn(there);

        await Keep(here, "character-rostam");

        Assert.DoesNotContain(SavedList.CookieName, here.Cookies.Keys);
        var list = await there.Get("/en/saved");
        Assert.Contains($"href=\"{Story}/entity/character-rostam\"", list.Html, StringComparison.Ordinal);
        Assert.Contains("kept with your account", list.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("in this browser only", list.Main, StringComparison.Ordinal);
        Assert.Contains("<a href=\"saved\">My list</a>", (await there.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.Contains("Take out of my list", (await there.Get(Rostam)).Main, StringComparison.Ordinal);

        await Keep(there, "character-rostam", "drop");

        Assert.Contains("Nothing is in your list.", (await here.Get("/en/saved")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<a href=\"saved\">", (await here.Get("/en/")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_signed_in_visitor_keeps_no_page_they_cannot_open()
    {
        var later = _site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;
        var visitor = _site.Visitor();
        await Register(visitor);

        var refused = await Keep(visitor, later);

        Assert.Equal(HttpStatusCode.SeeOther, refused.Status);
        Assert.Equal("/refused", refused.Response.Headers.Location?.OriginalString);
        Assert.Contains("Nothing is in your list.", (await visitor.Get("/en/saved")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_visitor_without_an_account_is_told_what_an_account_would_do_for_the_list()
    {
        var page = await _site.Visitor().Get("/en/saved");

        Assert.Contains("With an account your list follows you to every device.", page.Main, StringComparison.Ordinal);
        Assert.Contains("in this browser only", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_invitation_is_a_link_that_leads_one_person_to_an_account_and_to_nothing_else()
    {
        var mina = _site.Visitor();
        await Register(mina);

        var made = await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));

        Assert.Equal("/en/account?said=invited#invite", made.Response.Headers.Location?.OriginalString);
        var page = await mina.Get("/en/account?said=invited");
        Assert.Contains("The invitation is made.", page.Main, StringComparison.Ordinal);
        Assert.Contains("the site sends nothing to anybody", page.Main, StringComparison.Ordinal);
        var code = Assert.Single(await Invitations(mina));
        Assert.Contains($"http://localhost/en/account?invitation={code}", page.Main, StringComparison.Ordinal);
        Assert.Contains("can be used until ", page.Main, StringComparison.Ordinal);

        // Who opens the link is told they were invited, and makes an account.
        var sara = _site.Visitor();
        var invited = await sara.Get($"/en/account?invitation={code}");
        Assert.Contains("You were invited to Dastan.", invited.Main, StringComparison.Ordinal);
        Assert.Contains($"name=\"invitation\" value=\"{code}\"", invited.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Mina", invited.Main, StringComparison.Ordinal);
        Assert.Contains("said=welcome", (await Register(sara, "Sara", code)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        // It is used up, and nothing else followed: neither sees anything of the other.
        var after = await mina.Get(Account);
        Assert.Empty(await Invitations(mina));
        Assert.Contains("used", after.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", after.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Mina", (await sara.Get(Account)).Main, StringComparison.Ordinal);

        // The link opened again says so and stops nobody.
        var gone = await _site.Visitor().Get($"/en/account?invitation={code}");
        Assert.Contains("This invitation can no longer be used.", gone.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"invitation\"", gone.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Who_came_by_an_invitation_and_has_to_try_again_still_has_it()
    {
        var mina = _site.Visitor();
        await Register(mina);
        await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));
        var code = Assert.Single(await Invitations(mina));
        var sara = _site.Visitor();

        var answer = await sara.Post($"{Forms}/register", $"/en/account?invitation={code}", ("name", "Mina"), ("password", Password), ("again", Password), ("invitation", code), ("returnUrl", Account));

        Assert.Equal($"/en/account?form=register&problem=taken&invitation={code}#register", answer.Response.Headers.Location?.OriginalString);
        var page = await sara.Get(answer.Response.Headers.Location!.OriginalString);
        Assert.Contains("Somebody has this name already.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"name=\"invitation\" value=\"{code}\"", page.Html, StringComparison.Ordinal);
        Assert.Single(await Invitations(mina));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("NOSUCHCODENOSUCHCODE")]
    public async Task A_code_that_is_no_invitation_is_not_written_into_the_page(string code)
    {
        var page = await _site.Visitor().Get($"/en/account?invitation={Uri.EscapeDataString(code)}");

        Assert.Contains("This invitation can no longer be used.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"invitation\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>alert", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_invitation_is_taken_back_by_who_made_it_only()
    {
        var mina = _site.Visitor();
        await Register(mina);
        await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));
        var code = Assert.Single(await Invitations(mina));
        var sara = _site.Visitor();
        await Register(sara, "Sara");

        var other = await sara.Post($"{Forms}/revoke", Account, ("code", code), ("returnUrl", Account));
        Assert.Equal("/refused", other.Response.Headers.Location?.OriginalString);
        Assert.Contains("You were invited", (await _site.Visitor().Get($"/en/account?invitation={code}")).Main, StringComparison.Ordinal);

        var taken = await mina.Post($"{Forms}/revoke", Account, ("code", code), ("returnUrl", Account));

        Assert.Equal("/en/account?said=revoked#invite", taken.Response.Headers.Location?.OriginalString);
        var page = await mina.Get("/en/account?said=revoked");
        Assert.Contains("The invitation is taken back.", page.Main, StringComparison.Ordinal);
        Assert.Contains("taken back", page.Main, StringComparison.Ordinal);
        Assert.Empty(await Invitations(mina));
        Assert.Contains("can no longer be used", (await _site.Visitor().Get($"/en/account?invitation={code}")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_account_has_only_so_many_invitations_open()
    {
        var mina = _site.Visitor();
        await Register(mina);
        for (var number = 0; number < PeopleStore.OpenInvitations; number++)
        {
            await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));
        }

        var answer = await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));

        Assert.Equal("/en/account?form=invite&problem=invitations#invite", answer.Response.Headers.Location?.OriginalString);
        Assert.Contains("You have 10 invitations that can still be used.", (await mina.Get("/en/account?form=invite&problem=invitations")).Main, StringComparison.Ordinal);
        Assert.Equal(PeopleStore.OpenInvitations, (await Invitations(mina)).Length);
    }

    [Theory]
    [InlineData("bring")]
    [InlineData("invite")]
    [InlineData("revoke")]
    public async Task What_belongs_to_an_account_is_refused_to_who_is_not_signed_in(string form)
    {
        var answer = await _site.Visitor().Post($"{Forms}/{form}", Account, ("code", "NOSUCHCODE"), ("returnUrl", Account));

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task The_page_of_the_account_says_in_persian_what_follows_and_how_to_invite()
    {
        var visitor = _site.Visitor();
        await Register(visitor);
        await visitor.Post($"{Forms}/invite", Account, ("returnUrl", Account));

        var page = await visitor.Get("/fa/account");

        Assert.Contains("آنچه همراه شماست", page.Main, StringComparison.Ordinal);
        Assert.Contains("دعوت از دیگران", page.Main, StringComparison.Ordinal);
        Assert.Matches("تا ۲۰[۰-۹]{2}-[۰-۹]{2}-[۰-۹]{2} اعتبار دارد", page.Main);
        Assert.Contains("/fa/account?invitation=", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_without_accounts_keeps_the_reading_position_and_the_list_on_the_device_as_before()
    {
        using var site = new DemoSite();
        var visitor = site.Visitor();

        await visitor.ReadTo(3);
        await Keep(visitor, "character-rostam");

        Assert.Equal("3", visitor.Cookies[ReadingPreference.CookieName(Story)]);
        Assert.Contains(SavedList.CookieName, visitor.Cookies.Keys);
        Assert.DoesNotContain("With an account", (await visitor.Get("/en/saved")).Main, StringComparison.Ordinal);
    }
}
