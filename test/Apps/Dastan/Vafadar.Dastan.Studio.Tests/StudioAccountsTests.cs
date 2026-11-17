using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The tool with accounts (step 11.7): nothing is shown to somebody who has not signed in; an editor signs in
/// with the name and the password of an account of the store; every act is kept under that name; another
/// password or a closed account ends a sign-in at once. Every test has a tool and a store of its own.
/// </summary>
public sealed class StudioAccountsTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Relation = $"/en/{Collection}/record/relations/relation-02";

    private readonly Studio _studio = new(accounts: true);
    private readonly Desk _desk;

    public StudioAccountsTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    private Task<StudioPage> SignIn(string name = "Mina", string password = Studio.Password, string to = "") =>
        _desk.Submit($"/en/sign-in?to={Uri.EscapeDataString(to)}", "signin", ("Signing.Name", name), ("Signing.Password", password));

    // --- not signed in ---

    [Theory]
    [InlineData("/en/")]
    [InlineData($"/en/{Collection}")]
    [InlineData($"/en/{Collection}/record/relations/relation-02")]
    [InlineData($"/fa/{Collection}/waiting")]
    [InlineData("/en/a-page-that-is-not-there")]
    public async Task Who_has_not_signed_in_is_sent_to_the_page_that_signs_in_and_sees_nothing_of_the_store(string address)
    {
        var page = await _studio.Get(address);

        Assert.Equal(HttpStatusCode.Redirect, page.Status);
        Assert.Equal($"{address[..3]}/sign-in?to={Uri.EscapeDataString(address)}", page.Headers["Location"]);
        Assert.DoesNotContain("relation-02", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_form_sent_without_a_sign_in_is_refused_and_does_nothing()
    {
        var page = await _desk.Post(Relation, [("_handler", "remove"), ("Removing.By", "somebody"), ("Removing.Reason", "A try.")]);

        Assert.Equal(HttpStatusCode.Unauthorized, page.Status);
        using var store = _studio.Store();
        Assert.NotNull(store.Record(Collection, "relations", "relation-02"));
    }

    [Fact]
    public async Task The_page_that_signs_in_is_drawn_and_says_nothing_of_the_store()
    {
        var page = await _studio.Get("/en/sign-in");
        var style = Regex.Match(page.Html, "<link[^>]*href=\"([^\"]*studio[^\"]*\\.css)\"").Groups[1].Value;

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("name=\"Signing.Password\"", page.Main, StringComparison.Ordinal);
        Assert.Matches("type=\"password\"[^>]*autocomplete=\"current-password\"", page.Main);
        Assert.DoesNotContain(Collection, page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign out", page.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await _studio.Get(style.StartsWith('/') ? style : "/en/" + style)).Status);
        Assert.Contains("ورود", (await _studio.Get("/fa/sign-in")).Text, StringComparison.Ordinal);
    }

    // --- signing in ---

    [Fact]
    public async Task An_editor_signs_in_and_arrives_where_they_wanted_to_go()
    {
        var page = await SignIn(to: Relation);

        Assert.Equal(Relation, page.Arrived);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Father and teacher of", page.Text, StringComparison.Ordinal);
        Assert.Matches("<form[^>]*action=\"/sign-out\"[^>]*>.*<bdi>Mina</bdi>.*Sign out", page.Html.ReplaceLineEndings(" "));

        // The sign-in is for the whole tool, in every language.
        Assert.Equal(HttpStatusCode.OK, (await _desk.Get($"/fa/{Collection}/waiting")).Status);
    }

    [Theory]
    [InlineData("Mina", "not the password")]
    [InlineData("Nobody", Studio.Password)]
    [InlineData("", "")]
    public async Task A_wrong_name_or_password_signs_nobody_in_and_does_not_say_which_was_wrong(string name, string password)
    {
        var page = await SignIn(name, password, Relation);

        Assert.Null(page.Arrived);
        Assert.Contains("Not signed in. The name or the password is not right, or the account takes no password at the moment.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(password.Length > 0 ? password : "nothing that was typed", page.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, (await _desk.Get(Relation)).Status);
    }

    [Theory]
    [InlineData("//elsewhere.example/")]
    [InlineData("https://elsewhere.example/")]
    [InlineData("")]
    public async Task Signing_in_leads_nowhere_but_into_the_tool(string to)
    {
        var page = await SignIn(to: to);

        Assert.Equal("/en/", page.Arrived);
        Assert.Equal(HttpStatusCode.OK, page.Status);
    }

    [Fact]
    public async Task After_too_many_wrong_passwords_the_right_one_is_refused_for_a_while()
    {
        for (var attempt = 0; attempt < EditorialStore.FailuresAllowed; attempt++)
        {
            await SignIn(password: "not the password");
        }

        Assert.Null((await SignIn()).Arrived);

        _studio.Clock.Advance(EditorialStore.LockedFor + TimeSpan.FromSeconds(1));
        Assert.Equal("/en/", (await SignIn()).Arrived);
    }

    // --- signed in ---

    [Fact]
    public async Task An_act_is_kept_under_the_account_whatever_name_a_form_gives()
    {
        await SignIn();
        var page = await _desk.Get(Relation);

        Assert.DoesNotContain(".By\"", page.Main, StringComparison.Ordinal);
        Assert.Contains("Kept under your name: Mina.", page.Text, StringComparison.Ordinal);

        var done = await _desk.Submit(Relation, "review",
            ("Reviewing.Statement", "relation-02"), ("Reviewing.As", "reviewed"), ("Reviewing.On", "2026-11-17"), ("Reviewing.Note", "Read against the text."), ("Reviewing.By", "somebody else"));

        Assert.EndsWith("relation-02?done=reviewed", done.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal("Mina", store.History(Collection, "relations", "relation-02")[^1].By);
        Assert.Equal("Mina", (string?)store.Record(Collection, "relations", "relation-02")!.Body["review"]!["by"]);
    }

    [Fact]
    public async Task Two_editors_work_in_the_same_store_each_under_their_own_name()
    {
        using var other = new Desk(_studio);
        await SignIn();
        await other.Submit("/en/sign-in", "signin", ("Signing.Name", "omid"), ("Signing.Password", Studio.Password));

        await _desk.Fill(Relation, "fields", ("f.label", "Father and first teacher of"), ("Filling.Reason", "Read again."));
        await other.Fill($"/en/{Collection}/record/entities/character-rostam", "fields", ("f.summary", "A third summary."), ("Filling.Reason", "Shorter."));

        using var store = _studio.Store();
        Assert.Equal("Mina", store.History(Collection, "relations", "relation-02")[^1].By);
        Assert.Equal("Omid", store.History(Collection, "entities", "character-rostam")[^1].By);
    }

    [Fact]
    public async Task Signing_out_ends_the_sign_in_and_needs_the_token_of_the_tool()
    {
        await SignIn();

        var forged = await _desk.Post("/sign-out", [("returnUrl", "/en/")]);
        Assert.Equal(HttpStatusCode.BadRequest, forged.Status);
        Assert.Equal(HttpStatusCode.OK, (await _desk.Get(Relation)).Status);

        var page = await _desk.Get(Relation);
        var form = Regex.Match(page.Html, "<form[^>]*action=\"/sign-out\".*?</form>", RegexOptions.Singleline).Value;
        var token = WebUtility.HtmlDecode(Regex.Match(form, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value);
        var left = await _desk.Post("/sign-out", [("__RequestVerificationToken", token), ("returnUrl", "/en/")]);

        Assert.StartsWith("/en/sign-in", left.Headers["Location"], StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, (await _desk.Get(Relation)).Status);
    }

    [Fact]
    public async Task Another_password_and_a_closed_account_end_a_sign_in_at_once()
    {
        using var other = new Desk(_studio);
        await SignIn();
        await other.Submit("/en/sign-in", "signin", ("Signing.Name", "Omid"), ("Signing.Password", Studio.Password));
        using (var store = _studio.Store())
        {
            Assert.True(store.SetPassword("Mina", "another long phrase").Done);
            Assert.True(store.CloseEditor("Omid").Done);
        }

        Assert.Equal(HttpStatusCode.Redirect, (await _desk.Get(Relation)).Status);
        Assert.Equal(HttpStatusCode.Redirect, (await other.Get(Relation)).Status);
        Assert.Equal("/en/", (await SignIn(password: "another long phrase")).Arrived);
    }

    [Fact]
    public async Task Another_machine_is_answered_over_an_encrypted_connection_only()
    {
        async Task<Microsoft.AspNetCore.Http.HttpContext> From(string address, string scheme) => await _studio.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(address);
            context.Request.Scheme = scheme;
            context.Request.Method = "GET";
            context.Request.Path = "/en/sign-in";
        }, TestContext.Current.CancellationToken);

        Assert.Equal(400, (await From("203.0.113.7", "http")).Response.StatusCode);
        Assert.Equal(200, (await From("203.0.113.7", "https")).Response.StatusCode);
        Assert.Equal(200, (await From("127.0.0.1", "http")).Response.StatusCode);

        // What a stranger says about how they came is not heard: no proxy was named to the tool.
        var claimed = await _studio.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
            context.Request.Headers["X-Forwarded-Proto"] = "https";
            context.Request.Method = "GET";
            context.Request.Path = "/en/sign-in";
        }, TestContext.Current.CancellationToken);
        Assert.Equal(400, claimed.Response.StatusCode);
    }

    // --- a tool without accounts ---

    [Fact]
    public async Task A_tool_without_accounts_has_no_page_that_signs_in_and_asks_for_a_name()
    {
        using var studio = new Studio();

        Assert.Equal(HttpStatusCode.NotFound, (await studio.Get("/en/sign-in")).Status);
        var page = await studio.Get(Relation);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("name=\"Reviewing.By\"", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("/sign-out", page.Html, StringComparison.Ordinal);
    }
}
