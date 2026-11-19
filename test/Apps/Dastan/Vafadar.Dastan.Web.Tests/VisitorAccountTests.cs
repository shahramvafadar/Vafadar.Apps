using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Site;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site, told a file for the store of its visitors.</summary>
public sealed class AccountsSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-people-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the file of the store.</summary>
    public string File => Path.Combine(_folder, "people.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseSetting("Dastan:Database", TestDatabase.For(File));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}

/// <summary>
/// The account of a visitor on the site (step 16.1, D-048 to D-050 and D-055): making one, signing in and out,
/// recovery codes, another password, closing. Every test has a site of its own, so that one test's accounts and
/// attempts are not another's.
/// </summary>
public sealed class VisitorAccountTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private static (string, string)[] Fields(params (string, string)[] fields) => [.. fields, ("returnUrl", Account)];

    private static async Task<Page> Register(Browser visitor, string name = "Mina", string password = Password, string? again = null, string website = "") =>
        await visitor.Post($"{Forms}/register", Account, Fields(("name", name), ("age", "on"), ("terms", "on"), ("password", password), ("again", again ?? password), ("website", website)));

    private static async Task<Page> SignIn(Browser visitor, string name = "Mina", string password = Password) =>
        await visitor.Post($"{Forms}/sign-in", Account, Fields(("name", name), ("password", password)));

    private static string[] Codes(Page page) =>
        [.. Regex.Matches(page.Html, "<code>([A-Z0-9]{4}(?:-[A-Z0-9]{4}){3})</code>").Select(match => match.Groups[1].Value)];

    [Fact]
    public async Task A_site_that_was_told_no_store_has_no_account_and_no_page_for_one()
    {
        using var site = new DemoSite();
        var visitor = site.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get(Account)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Register(visitor)).Status);
        Assert.DoesNotContain("href=\"account\"", (await visitor.Get("/en/")).Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Sign in", "New here? Make an account", "a name, a mail address and a password")]
    [InlineData("fa", "ورود", "تازه آمده‌اید؟ حساب بسازید", "یک نام، یک نشانی ایمیل و یک گذرواژه")]
    public async Task The_page_offers_signing_in_and_making_an_account_in_the_language_of_the_page(string language, string signIn, string register, string privacy)
    {
        var page = await _site.Visitor().Get($"/{language}/account");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(signIn, page.Main, StringComparison.Ordinal);
        Assert.Contains(register, page.Main, StringComparison.Ordinal);
        Assert.Contains(privacy, page.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/register\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"name=\"returnUrl\" value=\"/{language}/account\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"account\">{signIn}</a>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_visitor_makes_an_account_is_signed_in_and_sees_the_recovery_codes_once()
    {
        var visitor = _site.Visitor();

        var answer = await Register(visitor, " Mina Rad ");

        Assert.Equal(HttpStatusCode.Redirect, answer.Status);
        Assert.Equal("/en/account?said=welcome", answer.Response.Headers.Location?.OriginalString);
        var session = Assert.Single(answer.Response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(AccountDesk.SessionCookie + "=", StringComparison.Ordinal));
        Assert.Contains("httponly", session, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", session, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", session, StringComparison.OrdinalIgnoreCase);

        var page = await visitor.Get("/en/account?said=welcome");
        Assert.Contains("You are signed in as Mina Rad.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Your account is made", page.Main, StringComparison.Ordinal);
        var codes = Codes(page);
        Assert.Equal(PeopleStore.RecoveryCodeCount, codes.Length);
        Assert.DoesNotContain(AccountDesk.CodesCookie, visitor.Cookies.Keys);

        // Shown once: the page asked for again has them no more.
        Assert.Empty(Codes(await visitor.Get(Account)));

        // The name stands in the header of every page.
        Assert.Contains("<a href=\"account\">Mina Rad</a>", (await visitor.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Mina Rad", (await _site.Visitor().Get("/en/")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_the_browser_carries_of_the_recovery_codes_cannot_be_read()
    {
        var visitor = _site.Visitor();

        var answer = await Register(visitor);

        var carried = visitor.Cookies[AccountDesk.CodesCookie];
        Assert.Contains(answer.Response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(AccountDesk.CodesCookie + "=", StringComparison.Ordinal) && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("max-age=300", StringComparison.OrdinalIgnoreCase));
        var codes = Codes(await visitor.Get(Account));
        Assert.NotEmpty(codes);
        Assert.DoesNotContain(codes, code => carried.Contains(code, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Mina", "short", "short", "A password has at least 12 characters.")]
    [InlineData("Mina", Password, "another long pass phrase", "The two passwords are not the same.")]
    [InlineData("ab", Password, Password, "A name has 3 to 40 characters.")]
    [InlineData("mina@example.org", Password, Password, "A name is made of letters")]
    [InlineData("Sara", Password, Password, "Somebody has this name already.")]
    public async Task An_account_that_cannot_be_made_says_why_and_signs_nobody_in(string name, string password, string again, string said)
    {
        await Register(_site.Visitor(), "Sara");
        var visitor = _site.Visitor();

        var answer = await Register(visitor, name, password, again);

        Assert.StartsWith("/en/account?form=register&problem=", answer.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
        var page = await visitor.Get(answer.Response.Headers.Location!.OriginalString);
        Assert.Contains(said, page.Main, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_program_that_fills_in_every_field_gets_no_account()
    {
        var visitor = _site.Visitor();

        await Register(visitor, website: "https://example.org");

        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
        Assert.Contains("form=sign-in&problem=wrong", (await SignIn(visitor)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_visitor_signs_out_and_in_again()
    {
        var visitor = _site.Visitor();
        await Register(visitor);

        var gone = await visitor.Post($"{Forms}/sign-out", Account, Fields());
        Assert.Equal("/en/account?said=signed-out", gone.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
        Assert.Contains("You are signed out.", (await visitor.Get("/en/account?said=signed-out")).Main, StringComparison.Ordinal);

        var back = await SignIn(visitor, "mina");
        Assert.Equal("/en/account?said=signed-in", back.Response.Headers.Location?.OriginalString);
        Assert.Contains("You are signed in as Mina.", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Mina", "a wrong pass phrase")]
    [InlineData("Nobody", Password)]
    public async Task A_wrong_name_and_a_wrong_password_are_answered_alike(string name, string password)
    {
        await Register(_site.Visitor());
        var visitor = _site.Visitor();

        var answer = await SignIn(visitor, name, password);

        Assert.Equal("/en/account?form=sign-in&problem=wrong#sign-in", answer.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
        Assert.Contains("The name or the password is not right", (await visitor.Get("/en/account?form=sign-in&problem=wrong")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_password_signs_every_other_browser_out()
    {
        var here = _site.Visitor();
        await Register(here);
        var there = _site.Visitor();
        await SignIn(there);

        var wrong = await here.Post($"{Forms}/password", Account, Fields(("current", "a wrong pass phrase"), ("password", "another long pass phrase"), ("again", "another long pass phrase")));
        Assert.Contains("problem=wrong-current", wrong.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Contains("You are signed in as Mina.", (await there.Get(Account)).Main, StringComparison.Ordinal);

        var changed = await here.Post($"{Forms}/password", Account, Fields(("current", Password), ("password", "another long pass phrase"), ("again", "another long pass phrase")));

        Assert.Equal("/en/account?said=password", changed.Response.Headers.Location?.OriginalString);
        Assert.Contains("You are signed in as Mina.", (await here.Get(Account)).Main, StringComparison.Ordinal);
        var other = await there.Get(Account);
        Assert.DoesNotContain("You are signed in", other.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(AccountDesk.SessionCookie, there.Cookies.Keys);
        Assert.Contains("problem=wrong", (await SignIn(there)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Contains("said=signed-in", (await SignIn(there, password: "another long pass phrase")).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_recovery_code_sets_a_new_password_once()
    {
        var first = _site.Visitor();
        await Register(first);
        var code = Codes(await first.Get(Account))[0];
        var visitor = _site.Visitor();
        (string, string)[] form = Fields(("name", "Mina"), ("code", code), ("password", "another long pass phrase"), ("again", "another long pass phrase"));

        var recovered = await visitor.Post($"{Forms}/recover", Account, form);

        Assert.Equal("/en/account?said=recovered", recovered.Response.Headers.Location?.OriginalString);
        var page = await visitor.Get(Account);
        Assert.Contains("You are signed in as Mina.", page.Main, StringComparison.Ordinal);
        Assert.Contains($"Recovery codes you have not used yet: {PeopleStore.RecoveryCodeCount - 1}.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("You are signed in", (await first.Get(Account)).Main, StringComparison.Ordinal);

        var again = await _site.Visitor().Post($"{Forms}/recover", Account, form);
        Assert.Equal("/en/account?form=recover&problem=wrong-code#recover", again.Response.Headers.Location?.OriginalString);
        var said = await _site.Visitor().Get("/en/account?form=recover&problem=wrong-code");
        Assert.Contains("The name or the recovery code is not right", said.Main, StringComparison.Ordinal);
        Assert.Contains("<details open", said.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task New_recovery_codes_are_shown_once_and_take_the_place_of_the_old()
    {
        var visitor = _site.Visitor();
        await Register(visitor);
        var old = Codes(await visitor.Get(Account));

        var refused = await visitor.Post($"{Forms}/codes", Account, Fields(("current", "a wrong pass phrase")));
        Assert.Contains("form=codes&problem=wrong-current", refused.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        var renewed = await visitor.Post($"{Forms}/codes", Account, Fields(("current", Password)));

        Assert.Equal("/en/account?said=codes", renewed.Response.Headers.Location?.OriginalString);
        var codes = Codes(await visitor.Get("/en/account?said=codes"));
        Assert.Equal(PeopleStore.RecoveryCodeCount, codes.Length);
        Assert.Empty(codes.Intersect(old));
        Assert.Empty(Codes(await visitor.Get(Account)));
    }

    [Fact]
    public async Task A_closed_account_is_signed_out_at_once_and_opened_again_with_its_password()
    {
        var visitor = _site.Visitor();
        await Register(visitor);
        var other = _site.Visitor();
        await SignIn(other);

        var closed = await visitor.Post($"{Forms}/close", Account, Fields(("current", Password)));

        Assert.Equal("/en/account?said=closed", closed.Response.Headers.Location?.OriginalString);
        Assert.Contains("It will be deleted for good in 30 days.", (await visitor.Get("/en/account?said=closed")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("You are signed in", (await other.Get(Account)).Main, StringComparison.Ordinal);
        Assert.Contains("problem=wrong", (await SignIn(visitor)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var wrong = await visitor.Post($"{Forms}/reopen", Account, Fields(("name", "Mina"), ("password", "a wrong pass phrase")));
        Assert.Equal("/en/account?form=reopen&problem=wrong-closed#reopen", wrong.Response.Headers.Location?.OriginalString);
        var again = await visitor.Post($"{Forms}/reopen", Account, Fields(("name", "Mina"), ("password", Password)));
        Assert.Equal("/en/account?said=reopened", again.Response.Headers.Location?.OriginalString);
        Assert.Contains("You are signed in as Mina.", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("codes")]
    [InlineData("close")]
    public async Task What_is_for_a_signed_in_visitor_is_refused_to_everybody_else(string form)
    {
        var answer = await _site.Visitor().Post($"{Forms}/{form}", Account, Fields(("current", Password), ("password", Password), ("again", Password)));

        Assert.Equal(HttpStatusCode.SeeOther, answer.Status);
        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task A_form_without_the_token_of_its_page_is_refused()
    {
        var visitor = _site.Visitor();
        await visitor.Get(Account);

        var answer = await visitor.Post($"{Forms}/register", "/health", Fields(("name", "Mina"), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password)));

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
    }

    [Fact]
    public async Task An_address_asks_for_only_so_many_accounts_in_an_hour()
    {
        for (var number = 0; number < AccountDesk.RegistrationsPerHour; number++)
        {
            Assert.Contains("said=welcome", (await Register(_site.Visitor(), $"Visitor {number}")).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        }

        var visitor = _site.Visitor();
        var answer = await Register(visitor, "One more");

        Assert.Equal("/en/account?form=register&problem=later#register", answer.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
        Assert.Contains("try again in an hour", (await visitor.Get("/en/account?form=register&problem=later")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_address_tries_only_so_many_passwords_in_an_hour_over_all_accounts()
    {
        await Register(_site.Visitor());
        var visitor = _site.Visitor();
        for (var attempt = 0; attempt < AccountDesk.AttemptsPerHour; attempt++)
        {
            await SignIn(visitor, $"Name {attempt}", "a wrong pass phrase");
        }

        var answer = await SignIn(visitor);

        Assert.Equal("/en/account?form=sign-in&problem=later#sign-in", answer.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(AccountDesk.SessionCookie, visitor.Cookies.Keys);
    }

    [Theory]
    [InlineData("https://elsewhere.example/account")]
    [InlineData("//elsewhere.example/account")]
    [InlineData("/en/story-rostam-sohrab")]
    [InlineData("/en/account/../../elsewhere/account")]
    public async Task A_form_leads_back_to_the_page_of_the_account_and_nowhere_else(string returnUrl)
    {
        var visitor = _site.Visitor();

        var answer = await visitor.Post($"{Forms}/sign-in", Account, ("name", "Mina"), ("password", Password), ("returnUrl", returnUrl));

        Assert.Equal("/account?form=sign-in&problem=wrong#sign-in", answer.Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task What_an_address_says_is_not_written_into_the_page()
    {
        var page = await _site.Visitor().Get("/en/account?said=<b>hello</b>&form=register&problem=<i>x</i>,taken,nonsense");

        Assert.DoesNotContain("hello", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>hello", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("nonsense", page.Main, StringComparison.Ordinal);
        Assert.Contains("Somebody has this name already.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_sign_in_that_this_site_did_not_seal_signs_nobody_in()
    {
        var visitor = _site.Visitor();
        await Register(visitor);
        visitor.Cookies[AccountDesk.SessionCookie] = "forged";

        Assert.DoesNotContain("You are signed in", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_a_signed_in_visitor_is_kept_by_nobody()
    {
        var visitor = _site.Visitor();
        await Register(visitor);

        var page = await visitor.Get(Account);

        Assert.True(page.Response.Headers.CacheControl?.NoStore);
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData("127.0.0.1", false, true)]
    [InlineData("::1", false, true)]
    [InlineData("203.0.113.7", true, true)]
    [InlineData("203.0.113.7", false, false)]
    public void A_password_travels_encrypted_or_on_this_machine_only(string? address, bool https, bool allowed)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address is null ? null : IPAddress.Parse(address);
        context.Request.Scheme = https ? "https" : "http";

        Assert.Equal(allowed, AccountDesk.IsPrivate(context));
    }

    [Fact]
    public async Task A_visitor_gets_a_copy_of_their_own_data_as_a_file()
    {
        var visitor = _site.Visitor();
        await Register(visitor, "مینا");
        Assert.Contains("A copy of your data", (await visitor.Get(Account)).Main, StringComparison.Ordinal);

        var file = await visitor.Post($"{Forms}/export", Account, Fields(("current", Password)));

        Assert.Equal(HttpStatusCode.OK, file.Status);
        Assert.Equal("application/json", file.Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment; filename=\"dastan-account.json\"", file.Response.Content.Headers.ContentDisposition?.ToString());
        Assert.True(file.Response.Headers.CacheControl?.NoStore);
        var copy = System.Text.Json.Nodes.JsonNode.Parse(file.Html)!;
        Assert.Equal(("dastan-account", "مینا"), ((string?)copy["format"], (string?)copy["account"]!["name"]));

        // Written for a person to read: the name stands in its own letters, each thing on a line of its own.
        Assert.Contains("\"name\": \"مینا\"", file.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, file.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("pbkdf2", file.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_copy_is_handed_over_for_the_password_only()
    {
        var visitor = _site.Visitor();
        await Register(visitor);

        var wrong = await visitor.Post($"{Forms}/export", Account, Fields(("current", "a wrong pass phrase")));

        Assert.Equal("/en/account?form=export&problem=wrong-current#export", wrong.Response.Headers.Location?.OriginalString);
        Assert.Contains("The present password is not right.", (await visitor.Get("/en/account?form=export&problem=wrong-current")).Main, StringComparison.Ordinal);
        Assert.Equal("/refused", (await _site.Visitor().Post($"{Forms}/export", Account, Fields(("current", Password)))).Response.Headers.Location?.OriginalString);
    }
}
