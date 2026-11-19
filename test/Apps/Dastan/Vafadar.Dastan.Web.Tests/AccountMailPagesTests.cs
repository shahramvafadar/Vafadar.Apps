using System.Net;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The journeys of an account that go through a mail (step 19.4, ADR 0016), as a visitor takes them on the
/// site: an account is one when the link that was mailed to its address was opened, a forgotten password is
/// set through a mailed link, another address counts when it was confirmed. The mail is read where the site
/// wrote it; every test has a site of its own.
/// </summary>
public sealed class AccountMailPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Other = "another long pass phrase";
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private const string Address = "mina@example.org";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private static (string, string)[] Fields(params (string, string)[] fields) => [.. fields, ("returnUrl", Account)];

    private static string? To(Page page) => page.Response.Headers.Location?.OriginalString;

    private static bool SignedIn(Page page) => page.Html.Contains($"action=\"{Forms}/sign-out\"", StringComparison.Ordinal);

    private static Task<Page> Ask(Browser visitor, string name = "Mina", string mail = Address, string account = Account) =>
        visitor.Post(Browser.Registering, account, ("name", name), ("mail", mail), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", account));

    private static Task<Page> SignIn(Browser visitor, string name, string password = Password) =>
        visitor.Post($"{Forms}/sign-in", Account, Fields(("name", name), ("password", password)));

    [Fact]
    public async Task An_account_is_one_when_the_link_that_was_mailed_to_its_address_was_opened()
    {
        var visitor = _site.Visitor();

        var asked = await Ask(visitor);

        // Nobody is signed in yet, and the page says to look at the mail.
        Assert.Equal($"{Account}?said=confirm-sent", To(asked));
        var told = await visitor.Get(To(asked)!);
        Assert.False(SignedIn(told));
        Assert.Contains("Look at your mail", told.Main, StringComparison.Ordinal);

        // The name and the password are right, and the account is none yet: said to who knows the password only.
        Assert.Equal($"{Account}?form=sign-in&problem=unconfirmed#sign-in", To(await SignIn(visitor, "Mina")));
        Assert.Equal($"{Account}?form=sign-in&problem=wrong#sign-in", To(await SignIn(visitor, "Mina", Other)));
        var waiting = await visitor.Get($"{Account}?form=sign-in&problem=unconfirmed");
        Assert.Contains("is not confirmed yet", waiting.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/resend\"", waiting.Html, StringComparison.Ordinal);

        // Opening the link changes nothing: a program that looks at the links of a mail uses none up.
        var mail = visitor.Mailed(Address, "confirm-address")!;
        Assert.Contains("Hello Mina,", mail, StringComparison.Ordinal);
        var (page, id, token) = Browser.LinkOf(mail);
        Assert.Equal("/en/account/confirm", page);
        var opened = await visitor.Get($"{page}?id={id}&token={token}");
        Assert.Equal(HttpStatusCode.OK, opened.Status);
        Assert.Contains($"action=\"{Forms}/confirm\"", opened.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"referrer\" content=\"no-referrer\"", opened.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", opened.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", opened.Own, StringComparison.Ordinal);
        Assert.Equal($"{Account}?form=sign-in&problem=unconfirmed#sign-in", To(await SignIn(visitor, "Mina")));

        // The form of the page does it: the visitor is signed in and sees the recovery codes.
        var confirmed = await visitor.Follow(mail, "confirm");
        Assert.Equal($"{Account}?said=welcome", To(confirmed));
        var welcome = await visitor.Get(To(confirmed)!);
        Assert.True(SignedIn(welcome));
        Assert.Contains("<code>", welcome.Html, StringComparison.Ordinal);
        Assert.Contains(Address, welcome.Main, StringComparison.Ordinal);

        // A link is used once.
        var again = await _site.Visitor().Follow(mail, "confirm");
        Assert.Equal("/en/account/confirm?problem=link", To(again));
        Assert.Contains("This link cannot be used", (await visitor.Get(To(again)!)).Main, StringComparison.Ordinal);

        // The address signs in as the name does.
        var elsewhere = _site.Visitor();
        Assert.Equal($"{Account}?said=signed-in", To(await SignIn(elsewhere, Address)));
    }

    [Fact]
    public async Task The_mail_is_written_in_the_language_of_the_page_the_account_was_asked_for_on()
    {
        var visitor = _site.Visitor();

        Assert.Equal("/fa/account?said=confirm-sent", To(await Ask(visitor, account: "/fa/account")));

        var mail = visitor.Mailed(Address, "confirm-address")!;
        Assert.Contains("Mina گرامی،", mail, StringComparison.Ordinal);
        Assert.Equal("/fa/account/confirm", Browser.LinkOf(mail).Page);
        var (page, id, token) = Browser.LinkOf(mail);
        var opened = await visitor.Get($"{page}?id={id}&token={token}");
        Assert.Equal("rtl", opened.Root("dir"));
        Assert.Contains("تأیید نشانی ایمیل", opened.Main, StringComparison.Ordinal);
        Assert.Equal("/fa/account?said=welcome", To(await visitor.Follow(mail, "confirm")));
    }

    [Fact]
    public async Task What_is_no_mail_address_makes_no_account_and_an_address_that_has_one_is_answered_like_any_other()
    {
        var visitor = _site.Visitor();

        Assert.Equal($"{Account}?form=register&problem=mail#register", To(await Ask(visitor, mail: "not an address")));
        Assert.Null(visitor.Mailed("not an address", "confirm-address"));

        await Ask(visitor);
        await visitor.Follow(visitor.Mailed(Address, "confirm-address")!, "confirm");

        // Somebody else names the same address: the page says what it says to everybody, and who has the address is told.
        var sara = _site.Visitor();
        Assert.Equal($"{Account}?said=confirm-sent", To(await Ask(sara, "Sara")));
        Assert.Contains("already has an account", sara.Mailed(Address, "address-in-use"), StringComparison.Ordinal);
        Assert.Equal($"{Account}?form=sign-in&problem=wrong#sign-in", To(await SignIn(sara, "Sara")));
    }

    [Fact]
    public async Task Asking_for_a_link_says_the_same_whether_there_is_an_account_or_not()
    {
        var visitor = _site.Visitor();
        await Ask(visitor);

        foreach (var name in new[] { "Mina", Address, "Nobody", "nobody@example.org" })
        {
            Assert.Equal($"{Account}?said=resend-sent", To(await visitor.Post($"{Forms}/resend", Account, Fields(("name", name)))));
            Assert.Equal($"{Account}?said=forgot-sent", To(await visitor.Post($"{Forms}/forgot", Account, Fields(("name", name)))));
        }

        Assert.Null(visitor.Mailed("nobody@example.org", "confirm-address"));
        Assert.Null(visitor.Mailed("nobody@example.org", "reset-password"));
        Assert.Contains("If there is an account", (await visitor.Get($"{Account}?said=forgot-sent")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_forgotten_password_is_set_through_the_link_that_is_mailed_to_the_address()
    {
        var visitor = _site.Visitor();
        await Ask(visitor);
        await visitor.Follow(visitor.Mailed(Address, "confirm-address")!, "confirm");
        var elsewhere = _site.Visitor();
        await SignIn(elsewhere, "Mina");
        Assert.True(SignedIn(await elsewhere.Get(Account)));

        var stranger = _site.Visitor();
        Assert.Equal($"{Account}?said=forgot-sent", To(await stranger.Post($"{Forms}/forgot", Account, Fields(("name", Address)))));
        var mail = stranger.Mailed(Address, "reset-password")!;
        var (page, id, token) = Browser.LinkOf(mail);
        Assert.Equal("/en/account/reset", page);

        // The page of the link asks for the password twice; what is wrong with it leaves the link as it was.
        var opened = await stranger.Get($"{page}?id={id}&token={token}");
        Assert.Contains($"action=\"{Forms}/reset\"", opened.Html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"new-password\"", opened.Html, StringComparison.Ordinal);
        Assert.Equal($"{page}?id={id}&token={token}&problem=password-differs", To(await stranger.Follow(mail, "reset", ("password", Other), ("again", Password))));
        Assert.Equal($"{page}?id={id}&token={token}&problem=password-short", To(await stranger.Follow(mail, "reset", ("password", "short"), ("again", "short"))));
        Assert.Contains("not the same", (await stranger.Get($"{page}?id={id}&token={token}&problem=password-differs")).Main, StringComparison.OrdinalIgnoreCase);

        var set = await stranger.Follow(mail, "reset", ("password", Other), ("again", Other));
        Assert.Equal($"{Account}?said=password", To(set));
        Assert.True(SignedIn(await stranger.Get(Account)));

        // Everybody else is signed out, the old password opens nothing, and the address is told.
        Assert.False(SignedIn(await elsewhere.Get(Account)));
        Assert.Equal($"{Account}?form=sign-in&problem=wrong#sign-in", To(await SignIn(_site.Visitor(), "Mina")));
        Assert.Equal($"{Account}?said=signed-in", To(await SignIn(_site.Visitor(), "Mina", Other)));
        Assert.NotNull(stranger.Mailed(Address, "password-changed"));
        Assert.Equal($"{page}?problem=link", To(await _site.Visitor().Follow(mail, "reset", ("password", Password), ("again", Password))));
    }

    [Fact]
    public async Task Another_address_counts_when_the_link_that_was_mailed_to_it_was_opened()
    {
        const string next = "mina@example.net";
        var visitor = _site.Visitor();
        await Ask(visitor);
        await visitor.Follow(visitor.Mailed(Address, "confirm-address")!, "confirm");
        var page = await visitor.Get(Account);
        Assert.Contains($"action=\"{Forms}/address\"", page.Html, StringComparison.Ordinal);

        Assert.Equal($"{Account}?form=address&problem=wrong-current#address", To(await visitor.Post($"{Forms}/address", Account, Fields(("mail", next), ("current", Other)))));
        Assert.Equal($"{Account}?form=address&problem=mail#address", To(await visitor.Post($"{Forms}/address", Account, Fields(("mail", "no address"), ("current", Password)))));
        Assert.Equal($"{Account}?form=address&problem=same#address", To(await visitor.Post($"{Forms}/address", Account, Fields(("mail", Address), ("current", Password)))));
        Assert.Equal(HttpStatusCode.SeeOther, (await _site.Visitor().Post($"{Forms}/address", Account, Fields(("mail", next), ("current", Password)))).Status);
        Assert.Null(visitor.Mailed(next, "confirm-new-address"));

        Assert.Equal($"{Account}?said=address-sent", To(await visitor.Post($"{Forms}/address", Account, Fields(("mail", next), ("current", Password)))));

        // Nothing changed yet; the address the account has is told that a change was asked for.
        Assert.Contains(Address, (await visitor.Get(Account)).Main, StringComparison.Ordinal);
        Assert.NotNull(visitor.Mailed(Address, "address-change-asked"));
        var mail = visitor.Mailed(next, "confirm-new-address")!;
        Assert.Equal("/en/account/address", Browser.LinkOf(mail).Page);

        var elsewhere = _site.Visitor();
        await SignIn(elsewhere, "Mina");
        Assert.Equal($"{Account}?said=address-changed", To(await visitor.Follow(mail, "address-confirm")));

        var after = await visitor.Get(Account);
        Assert.True(SignedIn(after));
        Assert.Contains(next, after.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(Address, after.Main, StringComparison.Ordinal);
        Assert.False(SignedIn(await elsewhere.Get(Account)));
        Assert.NotNull(visitor.Mailed(Address, "address-changed"));
        Assert.Equal($"{Account}?said=signed-in", To(await SignIn(_site.Visitor(), next)));
        Assert.Equal($"{Account}?form=sign-in&problem=wrong#sign-in", To(await SignIn(_site.Visitor(), Address)));
    }

    [Fact]
    public async Task Signing_out_everywhere_ends_every_other_sign_in_and_keeps_this_one()
    {
        var here = _site.Visitor();
        await Ask(here);
        await here.Follow(here.Mailed(Address, "confirm-address")!, "confirm");
        var there = _site.Visitor();
        await SignIn(there, "Mina");
        Assert.True(SignedIn(await there.Get(Account)));

        Assert.Equal(HttpStatusCode.SeeOther, (await _site.Visitor().Post($"{Forms}/sign-out-everywhere", Account, Fields())).Status);
        Assert.True(SignedIn(await there.Get(Account)));
        Assert.Equal($"{Account}?said=signed-out-everywhere", To(await here.Post($"{Forms}/sign-out-everywhere", Account, Fields())));

        Assert.True(SignedIn(await here.Get(Account)));
        Assert.False(SignedIn(await there.Get(Account)));
    }

    [Fact]
    public async Task An_invitation_is_mailed_to_an_address_and_makes_an_account_for_who_opens_its_link()
    {
        using var closed = _site.WithWebHostBuilder(builder => builder.UseSetting("Dastan:InvitationOnly", "true"));
        var mina = _site.Visitor();
        await Ask(mina);
        await mina.Follow(mina.Mailed(Address, "confirm-address")!, "confirm");
        Assert.Contains($"action=\"{Forms}/invite-by-mail\"", (await mina.Get(Account)).Html, StringComparison.Ordinal);

        Assert.Equal($"{Account}?form=invite&problem=mail#invite", To(await mina.Post($"{Forms}/invite-by-mail", Account, Fields(("mail", "no address")))));
        Assert.Equal(HttpStatusCode.SeeOther, (await _site.Visitor().Post($"{Forms}/invite-by-mail", Account, Fields(("mail", "sara@example.org")))).Status);
        Assert.Null(mina.Mailed("sara@example.org", "invitation"));

        Assert.Equal("/fa/account?said=invite-sent#invite", To(await mina.Post($"{Forms}/invite-by-mail", "/fa/account", ("mail", "sara@example.org"), ("returnUrl", "/fa/account"))));
        var mail = mina.Mailed("sara@example.org", "invitation")!;
        Assert.Contains("«Mina» در داستان حساب دارد", mail, StringComparison.Ordinal);
        var link = System.Text.RegularExpressions.Regex.Match(mail, @"https?://[^/\s]+(/fa/account\?invitation=[A-Z0-9]+)").Groups[1].Value;
        Assert.NotEmpty(link);

        // The site takes new accounts by invitation only: the link of the mail is the way in.
        var sara = Browser.Of(closed);
        Assert.Contains($"action=\"{Forms}/register\"", (await sara.Get(link)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/register\"", (await Browser.Of(closed).Get("/fa/account")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_account_turns_the_mail_about_what_others_ask_off_and_on()
    {
        var mina = _site.Visitor();
        await Ask(mina);
        await mina.Follow(mina.Mailed(Address, "confirm-address")!, "confirm");
        var sara = _site.Visitor();
        await Ask(sara, "Sara", "sara@example.org");
        await sara.Follow(sara.Mailed("sara@example.org", "confirm-address")!, "confirm");
        Assert.Contains("name=\"notices\" type=\"checkbox\" checked", (await sara.Get(Account)).Html, StringComparison.Ordinal);

        // Off: a request of somebody else is on the page and in no mail.
        Assert.Equal($"{Account}?said=notices-off#notices", To(await sara.Post($"{Forms}/notices", Account, Fields())));
        Assert.DoesNotContain("name=\"notices\" type=\"checkbox\" checked", (await sara.Get(Account)).Html, StringComparison.Ordinal);
        await mina.Post("/preferences/people/ask", "/en/account/people", ("name", "Sara"), ("kind", "friend"), ("returnUrl", "/en/account/people"));
        Assert.Contains("Mina", (await sara.Get("/en/account/people")).Main, StringComparison.Ordinal);
        Assert.Null(sara.Mailed("sara@example.org", "relationship-asked"));

        // On again: the next one is mailed.
        Assert.Equal($"{Account}?said=notices-on#notices", To(await mina.Post($"{Forms}/notices", Account, Fields(("notices", "on")))));
        Assert.Equal($"{Account}?said=notices-on#notices", To(await sara.Post($"{Forms}/notices", Account, Fields(("notices", "on")))));
        Assert.Equal(HttpStatusCode.SeeOther, (await _site.Visitor().Post($"{Forms}/notices", Account, Fields())).Status);
    }

    [Theory]
    [InlineData("/en/account/confirm")]
    [InlineData("/en/account/reset?id=abc")]
    [InlineData("/fa/account/address?id=a%20b&token=%3Cscript%3E")]
    public async Task The_page_of_a_link_without_a_complete_link_offers_no_form(string address)
    {
        var page = await _site.Visitor().Get(address);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("<form method=\"post\" action=\"/preferences/account", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", page.Html, StringComparison.Ordinal);
        Assert.Contains("href=\"account\"", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_without_accounts_has_no_page_for_a_link()
    {
        using var site = new DemoSite();

        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get("/en/account/confirm?id=a&token=b")).Status);
    }

    [Fact]
    public async Task A_provider_that_names_no_address_makes_no_account_and_an_address_that_has_one_is_linked_from_inside()
    {
        using var site = new ExternalSite();
        var mina = site.Visitor();
        await mina.Post(Browser.Registering, Account, Fields(("name", "Mina"), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password)));

        // The provider names no address it confirmed.
        site.Mail = string.Empty;
        var silent = site.Visitor();
        var refused = await silent.Open((await silent.Post($"{Forms}/external", Account, Fields(("provider", "test")))).Response.Headers.Location!.OriginalString);
        Assert.Contains("named no mail address", refused.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/external-register\"", refused.Html, StringComparison.Ordinal);

        // The provider names the address of Mina's account: no second account, and nobody is signed in.
        site.Mail = TestAccounts.MailOf("Mina");
        var arriving = site.Visitor();
        await arriving.Open((await arriving.Post($"{Forms}/external", Account, Fields(("provider", "test")))).Response.Headers.Location!.OriginalString);
        var chosen = await arriving.Post($"{Forms}/external-register", Account, Fields(("name", "Mina Again"), ("age", "on"), ("terms", "on")));
        Assert.Equal($"{Account}?form=external&problem=address-has-account#external", To(chosen));
        var told = await arriving.Get(To(chosen)!);
        Assert.False(SignedIn(told));
        Assert.Contains("already has an account", told.Main, StringComparison.Ordinal);
    }
}
