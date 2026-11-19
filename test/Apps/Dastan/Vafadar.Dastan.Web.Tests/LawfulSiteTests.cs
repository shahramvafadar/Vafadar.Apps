using System.Net;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site with accounts and a folder for the notices about content.</summary>
public sealed class NoticesSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-notices-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the file of the store of the visitors.</summary>
    public string People => Path.Combine(_folder, "people.db");

    /// <summary>Gets the folder the notices lie in.</summary>
    public string Notices => Path.Combine(_folder, "notices");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Database", TestDatabase.For(People));
        builder.UseSetting("Dastan:Notices", Notices);
        builder.UseSetting("Dastan:Announcement", "The site moves on 1 December. <b>");
    }

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
/// What the law asks of the site beyond its texts (row 18.8): it stores on a device only what a visitor asked
/// for and says so once; pages are kept for reading without a connection only with a yes; an account is made
/// by somebody who read the terms; anybody can give notice of content; and what the operator restricts is
/// told to the person concerned with its reasons.
/// </summary>
public sealed class LawfulSiteTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private readonly NoticesSite _site = new();

    public void Dispose() => _site.Dispose();

    private static string? Kept(Page page) => page.Response.Headers.TryGetValues("Dastan-Keep", out var values) ? string.Join(',', values) : null;

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        return visitor;
    }

    [Fact]
    public async Task Opening_a_page_puts_no_choice_on_the_device_and_the_note_about_storage_is_shown_until_it_was_read()
    {
        var visitor = _site.Visitor();

        var first = await visitor.Get("/fa/");
        Assert.Contains("class=\"storage-notice\"", first.Html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"cookies\">", first.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("vafadar.language", visitor.Cookies.Keys);
        Assert.DoesNotContain("dastan.offline", visitor.Cookies.Keys);
        Assert.DoesNotContain("dastan.notice", visitor.Cookies.Keys);

        // Changing the language on the site is a choice, and that is remembered.
        visitor.CameFrom = "/fa/";
        await visitor.Get("/en/");
        Assert.Equal("en", visitor.Cookies["vafadar.language"]);
        visitor.CameFrom = null;

        await visitor.Post("/preferences/storage-note", "/fa/", ("returnUrl", "/fa/"));
        Assert.Equal("1", visitor.Cookies["dastan.notice"]);
        Assert.DoesNotContain("class=\"storage-notice\"", (await visitor.Get("/fa/")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_form_answers_in_the_language_of_the_page_it_stood_on_without_anything_kept_for_it()
    {
        var visitor = _site.Visitor();

        var refused = await visitor.Post("/preferences/report-content", "/fa/notice", ("language", "fa"), ("address", ""), ("kind", ""), ("explanation", ""));
        Assert.StartsWith("/fa/notice?problem=", refused.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain("vafadar.language", visitor.Cookies.Keys);
    }

    [Fact]
    public async Task Pages_are_kept_for_reading_without_a_connection_only_for_who_said_yes()
    {
        var visitor = _site.Visitor();

        var before = await visitor.Get("/en/");
        Assert.Null(Kept(before));
        Assert.DoesNotContain("data-offline", before.Html, StringComparison.Ordinal);
        Assert.Contains("action=\"/preferences/offline\"", (await visitor.Get("/en/cookies")).Html, StringComparison.Ordinal);

        await visitor.Post("/preferences/offline", "/en/cookies", ("answer", "on"), ("returnUrl", "/en/cookies"));
        var after = await visitor.Get("/en/");
        Assert.Equal("page", Kept(after));
        Assert.Contains("data-offline=\"on\"", after.Html, StringComparison.Ordinal);

        await visitor.Post("/preferences/offline", "/en/cookies", ("answer", "off"), ("returnUrl", "/en/cookies"));
        var taken = await visitor.Get("/en/");
        Assert.Null(Kept(taken));
        Assert.DoesNotContain("data-offline", taken.Html, StringComparison.Ordinal);
        await visitor.Post("/preferences/offline", "/en/cookies", ("answer", "perhaps"));
        Assert.DoesNotContain("dastan.offline", visitor.Cookies.Keys);
    }

    [Fact]
    public async Task What_the_operator_has_to_tell_everybody_stands_on_every_page_as_text()
    {
        var page = await _site.Visitor().Get("/fa/story-rostam-sohrab");

        Assert.Contains("class=\"announcement\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("The site moves on 1 December. &lt;b&gt;", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_account_is_made_without_the_words_about_the_terms_and_an_account_notes_which_it_read()
    {
        var visitor = _site.Visitor();
        var form = await visitor.Get(Account);
        Assert.Contains("name=\"terms\" type=\"checkbox\" required", form.Html, StringComparison.Ordinal);

        var refused = await visitor.Post("/preferences/account/register", Account, ("name", "Mina"), ("age", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("problem=terms", refused.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Contains("Please read the terms of use", (await visitor.Open(refused.Response.Headers.Location!.OriginalString)).Main, StringComparison.Ordinal);

        var mina = await Signed("Mina");
        Assert.DoesNotContain("id=\"terms\"", (await mina.Get(Account)).Html, StringComparison.Ordinal);
        using var store = PeopleStore.Open(TestDatabase.For(_site.People));
        Assert.Null(store.SignIn("mina", "another pass phrase"));
        Assert.Equal(Site.AccountDesk.TermsVersion, store.TermsOf(store.SignIn("Mina", Password)!.Id));
    }

    [Fact]
    public async Task An_account_that_never_said_it_read_the_terms_is_asked_and_its_yes_is_noted()
    {
        string id;
        using (var store = PeopleStore.Open(TestDatabase.For(_site.People)))
        {
            id = store.Register("Sara", Password).Session!.Id;
            Assert.Null(store.TermsOf(id));
        }

        var sara = _site.Visitor();
        await sara.Post("/preferences/account/sign-in", Account, ("name", "Sara"), ("password", Password), ("returnUrl", Account));
        Assert.Contains("id=\"terms\"", (await sara.Get(Account)).Html, StringComparison.Ordinal);

        await sara.Post("/preferences/account/terms", Account, ("terms", "on"), ("returnUrl", Account));
        Assert.DoesNotContain("id=\"terms\"", (await sara.Get(Account)).Html, StringComparison.Ordinal);
        using var after = PeopleStore.Open(TestDatabase.For(_site.People));
        Assert.Equal(Site.AccountDesk.TermsVersion, after.TermsOf(id));
    }

    [Fact]
    public async Task Anybody_gives_notice_of_content_and_is_told_a_reference_at_once()
    {
        var visitor = _site.Visitor();
        var page = await visitor.Get("/en/notice?about=https%3A%2F%2Fdastan.online%2Fen%2Ftree%2Fabc");
        Assert.Contains("value=\"https://dastan.online/en/tree/abc\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("mailto:info@dastan.online", page.Html, StringComparison.Ordinal);

        var sent = await visitor.Post(
            "/preferences/report-content",
            "/en/notice",
            ("language", "en"),
            ("address", "https://dastan.online/en/tree/abc"),
            ("kind", "image"),
            ("explanation", "The photograph on this page shows me and I never agreed."),
            ("name", "A Person"),
            ("mail", "person@example.org"),
            ("good-faith", "on"));

        var location = sent.Response.Headers.Location!.OriginalString;
        Assert.Matches("^/en/notice\\?received=\\d{8}-[A-Z0-9]{8}#received$", location);
        var reference = location[(location.IndexOf('=', StringComparison.Ordinal) + 1)..location.IndexOf('#', StringComparison.Ordinal)];
        Assert.Contains(reference, (await visitor.Get(location)).Main, StringComparison.Ordinal);

        var notice = Assert.Single(new NoticeBox(_site.Notices).Waiting());
        Assert.Equal((reference, "en", "https://dastan.online/en/tree/abc", "image", "A Person", "person@example.org", true), (notice.Id, notice.Language, notice.Address, notice.Kind, notice.Name, notice.Mail, notice.GoodFaith));
    }

    [Theory]
    [InlineData("", "image", "A long enough explanation of the matter.", "on", "", "address")]
    [InlineData("https://dastan.online/en/x", "nothing", "A long enough explanation of the matter.", "on", "", "kind")]
    [InlineData("https://dastan.online/en/x", "image", "too short", "on", "", "explanation")]
    [InlineData("https://dastan.online/en/x", "image", "A long enough explanation of the matter.", "", "", "good-faith")]
    [InlineData("https://dastan.online/en/x", "image", "A long enough explanation of the matter.", "on", "no address", "mail")]
    public async Task A_notice_that_lacks_something_is_not_kept_and_the_page_says_what(string address, string kind, string explanation, string goodFaith, string mail, string problem)
    {
        var visitor = _site.Visitor();

        var sent = await visitor.Post("/preferences/report-content", "/fa/notice", ("language", "fa"), ("address", address), ("kind", kind), ("explanation", explanation), ("good-faith", goodFaith), ("mail", mail));

        Assert.Equal($"/fa/notice?problem={problem}#form", sent.Response.Headers.Location!.OriginalString);
        Assert.Contains("class=\"problems\"", (await visitor.Get($"/fa/notice?problem={problem}")).Html, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_site.Notices) && new NoticeBox(_site.Notices).Waiting().Count > 0);
    }

    [Fact]
    public async Task A_program_that_fills_in_the_hidden_field_leaves_no_notice()
    {
        var sent = await _site.Visitor().Post("/preferences/report-content", "/en/notice", ("language", "en"), ("address", "https://dastan.online/en/x"), ("kind", "image"), ("explanation", "A long enough explanation of the matter."), ("good-faith", "on"), ("website", "https://spam.example"));

        Assert.Contains("problem=later", sent.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_site.Notices) && new NoticeBox(_site.Notices).Waiting().Count > 0);
    }

    [Fact]
    public async Task What_the_operator_restricts_is_told_to_its_owner_with_the_reasons()
    {
        var mina = await Signed("Mina");
        await mina.Post("/preferences/trees/add", "/en/account/trees", ("name", "The Rad family"), ("returnUrl", "/en/account/trees"));
        using (var store = PeopleStore.Open(TestDatabase.For(_site.People)))
        {
            var id = store.SignIn("Mina", Password)!.Id;
            var tree = Assert.Single(store.Trees(id)).Id;
            Assert.NotNull(store.Restrict(RestrictionKind.Tree, tree, "The tree names a living person who objected.", "Section 7 of the terms of use; Article 21 GDPR", noticed: true));
        }

        var account = await mina.Get(Account);
        Assert.Contains("id=\"restrictions\"", account.Html, StringComparison.Ordinal);
        Assert.Contains("The tree names a living person who objected.", account.Main, StringComparison.Ordinal);
        Assert.Contains("Section 7 of the terms of use; Article 21 GDPR", account.Main, StringComparison.Ordinal);
        Assert.Contains("Somebody gave notice of the content", account.Main, StringComparison.Ordinal);
        Assert.Contains("No automated means were used", account.Main, StringComparison.Ordinal);
        Assert.Contains("If the terms are broken again", account.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_account_the_operator_closed_tells_its_owner_why_and_nobody_else()
    {
        var mina = await Signed("Mina");
        long number;
        using (var store = PeopleStore.Open(TestDatabase.For(_site.People)))
        {
            number = store.Restrict(RestrictionKind.Account, "Mina", "The account stored pictures of strangers after two decisions.", "Sections 6 and 10 of the terms of use", noticed: false)!.Value;
        }

        // Signed out everywhere, and the page of the account says nothing to somebody who only knows the name.
        Assert.DoesNotContain("id=\"restrictions\"", (await mina.Get(Account)).Html, StringComparison.Ordinal);
        var stranger = _site.Visitor();
        var wrong = await stranger.Post("/preferences/account/sign-in", Account, ("name", "Mina"), ("password", "not her pass phrase"), ("returnUrl", Account));
        Assert.Contains("problem=wrong", wrong.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain("pictures of strangers", (await stranger.Open(wrong.Response.Headers.Location!.OriginalString)).Html, StringComparison.Ordinal);

        // Who gives the name and the password reads the statement of reasons, and is not signed in.
        var owner = _site.Visitor();
        var told = await owner.Post("/preferences/account/sign-in", Account, ("name", "Mina"), ("password", Password), ("returnUrl", Account));
        Assert.Contains("problem=closed-by-operator", told.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        var page = await owner.Open(told.Response.Headers.Location!.OriginalString);
        Assert.Contains("id=\"closed\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("The account stored pictures of strangers after two decisions.", page.Main, StringComparison.Ordinal);
        Assert.Contains("A person who runs the site looked at the content on their own initiative.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign out", page.Main, StringComparison.Ordinal);

        // Lifted, the account opens with its password again.
        using (var store = PeopleStore.Open(TestDatabase.For(_site.People)))
        {
            Assert.True(store.Lift(number));
        }

        var again = await owner.Post("/preferences/account/sign-in", Account, ("name", "Mina"), ("password", Password), ("returnUrl", Account));
        Assert.Contains("said=signed-in", again.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }
}
