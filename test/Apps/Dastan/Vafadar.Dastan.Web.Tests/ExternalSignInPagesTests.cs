using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The demonstration site with a store for its visitors and a made-up provider to sign in through. The provider
/// answers at once, with the identifier the test set – as Google or Microsoft would after their own pages.
/// </summary>
public sealed class ExternalSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-external-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the file of the store.</summary>
    public string File => Path.Combine(_folder, "people.db");

    /// <summary>Gets or sets who the provider says the visitor is. Nothing: the sign-in did not come through.</summary>
    public string? Subject { get; set; } = "the-identifier-of-mina";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:People", File);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(this);
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, MadeUpProvider>("test", null);
            services.Configure<ExternalSignInOptions>(options => options.Providers.Add("test"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private sealed class MadeUpProvider(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ExternalSite site)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (site.Subject is { } subject)
            {
                // A name and a mail address, as a provider may send them along: the site takes neither.
                var said = new ClaimsIdentity([new(ClaimTypes.NameIdentifier, subject), new(ClaimTypes.Name, "Name At The Provider"), new(ClaimTypes.Email, "mina@example.org")], "test");
                await Context.SignInAsync(ExternalSignIn.Arriving, new ClaimsPrincipal(said), properties);
            }

            Response.Redirect(properties.RedirectUri!);
        }
    }
}

/// <summary>
/// Signing in through a provider on the site (step 16.8, D-048): offered only where the owner entered a client,
/// the visitor chooses the name of the account, and an account keeps a way in.
/// </summary>
public sealed class ExternalSignInPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private readonly ExternalSite _site = new();

    public void Dispose() => _site.Dispose();

    private static (string, string)[] Fields(params (string, string)[] fields) => [.. fields, ("returnUrl", Account)];

    // Follows where a form sends the visitor, as a browser does.
    private static async Task<Page> Follow(Browser visitor, Page page)
    {
        for (var hops = 0; hops < 5 && page.Response.Headers.Location is { } next; hops++)
        {
            page = await visitor.Get(next.OriginalString);
        }

        return page;
    }

    private static async Task<Page> Through(Browser visitor, string provider = "test") =>
        await Follow(visitor, await visitor.Post($"{Forms}/external", Account, Fields(("provider", provider))));

    private static async Task<Page> Choose(Browser visitor, string name, string website = "") =>
        await Follow(visitor, await visitor.Post($"{Forms}/external-register", Account, Fields(("name", name), ("age", "on"), ("terms", "on"), ("website", website))));

    private static async Task<Page> Send(Browser visitor, string form, params (string, string)[] fields) =>
        await Follow(visitor, await visitor.Post($"{Forms}/{form}", Account, Fields(fields)));

    private static bool SignedIn(Page page) => page.Html.Contains($"action=\"{Forms}/sign-out\"", StringComparison.Ordinal);

    [Fact]
    public async Task A_site_without_a_client_of_a_provider_offers_none()
    {
        using var site = new AccountsSite();
        var visitor = site.Visitor();

        var page = await visitor.Get(Account);

        Assert.DoesNotContain($"{Forms}/external", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign in through", page.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.SeeOther, (await visitor.Post($"{Forms}/external", Account, Fields(("provider", "google")))).Status);
    }

    [Theory]
    [InlineData("Google", "google", "accounts.google.com", "signin-google")]
    [InlineData("Microsoft", "microsoft", "login.microsoftonline.com", "signin-microsoft")]
    public async Task A_site_with_a_client_of_a_provider_sends_the_visitor_there(string name, string provider, string host, string back)
    {
        using var site = new AccountsSite().WithWebHostBuilder(builder => builder
            .UseSetting($"Dastan:SignIn:{name}:ClientId", "a-made-up-client")
            .UseSetting($"Dastan:SignIn:{name}:ClientSecret", "a-made-up-value-no-provider-knows"));
        var visitor = new Browser(site.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false }), null);

        var page = await visitor.Get(Account);
        var sent = await visitor.Post($"{Forms}/external", Account, Fields(("provider", provider)));

        Assert.Contains($"Sign in through {name}", page.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, sent.Status);
        var address = sent.Response.Headers.Location!;
        Assert.Equal(host, address.Host);
        Assert.Contains("client_id=a-made-up-client", address.Query, StringComparison.Ordinal);
        Assert.Contains(back, Uri.UnescapeDataString(address.Query), StringComparison.Ordinal);

        // The value the owner entered goes to the provider behind the visitor's back only – never into an address.
        Assert.DoesNotContain("a-made-up-value", address.Query, StringComparison.Ordinal);
        if (provider == "google")
        {
            // Asked for who it is, and for nothing about them.
            Assert.Contains("scope=openid&", address.Query + "&", StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Somebody_new_chooses_a_name_and_has_an_account_without_a_password()
    {
        var visitor = _site.Visitor();

        var asked = await Through(visitor);

        Assert.False(SignedIn(asked));
        Assert.Contains("Choose a name for your account", asked.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/external-register\"", asked.Html, StringComparison.Ordinal);

        var made = await Choose(visitor, "Mina");

        Assert.True(SignedIn(made));
        Assert.Contains("Mina", made.Main, StringComparison.Ordinal);
        Assert.Contains("<code>", made.Html, StringComparison.Ordinal);
        Assert.Contains("Set a password", made.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/set-password\"", made.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/close\"", made.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(ExternalSignIn.PendingCookie + "=ey", string.Join(';', visitor.Cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")), StringComparison.Ordinal);

        // Nothing the provider said about the person is kept or shown.
        Assert.DoesNotContain("Name At The Provider", made.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("mina@example.org", made.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(_site.Subject!, made.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Somebody_who_has_an_account_is_signed_in_at_once_the_next_time()
    {
        var first = _site.Visitor();
        await Through(first);
        await Choose(first, "Mina");

        var again = await Through(_site.Visitor());

        Assert.True(SignedIn(again));
        Assert.Contains("Mina", again.Main, StringComparison.Ordinal);

        // Somebody else at the same provider is somebody else.
        _site.Subject = "the-identifier-of-sara";
        Assert.Contains("Choose a name for your account", (await Through(_site.Visitor())).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_somebody_has_is_said_and_the_visitor_chooses_another_without_signing_in_again()
    {
        var mina = _site.Visitor();
        await mina.Post($"{Forms}/register", Account, Fields(("name", "Mina"), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("website", string.Empty)));
        var visitor = _site.Visitor();
        await Through(visitor);

        var refused = await Choose(visitor, "mina");

        Assert.False(SignedIn(refused));
        Assert.Contains("role=\"alert\"", refused.Html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/external-register\"", refused.Html, StringComparison.Ordinal);
        Assert.True(SignedIn(await Choose(visitor, "Mina Two")));
    }

    [Fact]
    public async Task A_name_is_chosen_only_after_a_sign_in_through_a_provider_and_by_a_person()
    {
        var nobody = _site.Visitor();

        Assert.False(SignedIn(await Choose(nobody, "Mina")));

        var program = _site.Visitor();
        await Through(program);
        Assert.False(SignedIn(await Choose(program, "Mina", website: "https://example.org")));

        using var store = PeopleStore.Open(_site.File, TimeProvider.System);
        Assert.Null(store.SignInWith("test", _site.Subject!));
    }

    [Fact]
    public async Task A_sign_in_that_did_not_come_through_makes_nothing()
    {
        _site.Subject = null;
        var visitor = _site.Visitor();

        var page = await Through(visitor);

        Assert.False(SignedIn(page));
        Assert.Contains("did not come through", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose a name", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_account_with_a_password_adds_a_provider_and_takes_it_away_again()
    {
        var mina = _site.Visitor();
        await mina.Post($"{Forms}/register", Account, Fields(("name", "Mina"), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("website", string.Empty)));

        var before = await mina.Get(Account);
        var linked = await Through(mina);

        Assert.Contains("Add test", before.Main, StringComparison.Ordinal);
        Assert.Contains("can be signed in that way", linked.Main, StringComparison.Ordinal);
        Assert.Contains("Remove test", linked.Main, StringComparison.Ordinal);
        Assert.True(SignedIn(await Through(_site.Visitor())));

        // The same sign-in is not added to a second account.
        var sara = _site.Visitor();
        await sara.Post($"{Forms}/register", Account, Fields(("name", "Sara"), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("website", string.Empty)));
        Assert.Contains("belongs to another account", (await Through(sara)).Main, StringComparison.Ordinal);

        var removed = await Send(mina, "unlink", ("provider", "test"));

        Assert.Contains("was taken away", removed.Main, StringComparison.Ordinal);
        Assert.Contains("Choose a name", (await Through(_site.Visitor())).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_only_way_into_an_account_stays_until_a_password_is_set()
    {
        var mina = _site.Visitor();
        await Through(mina);
        await Choose(mina, "Mina");

        var kept = await Send(mina, "unlink", ("provider", "test"));

        Assert.Contains("only way into the account", kept.Main, StringComparison.Ordinal);

        Assert.Contains("role=\"alert\"", (await Send(mina, "set-password", ("password", Password), ("again", "another long pass phrase"))).Html, StringComparison.Ordinal);
        var set = await Send(mina, "set-password", ("password", Password), ("again", Password));

        Assert.True(SignedIn(set));
        Assert.Contains($"action=\"{Forms}/close\"", set.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/set-password\"", set.Html, StringComparison.Ordinal);
        Assert.Contains("was taken away", (await Send(mina, "unlink", ("provider", "test"))).Main, StringComparison.Ordinal);

        // The account is signed in by its name from now on.
        var other = _site.Visitor();
        Assert.True(SignedIn(await Follow(other, await other.Post($"{Forms}/sign-in", Account, Fields(("name", "Mina"), ("password", Password))))));
    }

    [Fact]
    public async Task The_forms_are_for_somebody_who_is_signed_in_and_the_words_are_in_the_language_of_the_page()
    {
        var visitor = _site.Visitor();

        Assert.Equal(HttpStatusCode.SeeOther, (await visitor.Post($"{Forms}/unlink", Account, Fields(("provider", "test")))).Status);
        Assert.Equal(HttpStatusCode.SeeOther, (await visitor.Post($"{Forms}/set-password", Account, Fields(("password", Password), ("again", Password)))).Status);
        Assert.Equal(HttpStatusCode.SeeOther, (await visitor.Post($"{Forms}/external", Account, Fields(("provider", "another")))).Status);

        var page = await visitor.Get("/fa/account");
        Assert.Contains("ورود از راه سرویسی دیگر", page.Main, StringComparison.Ordinal);
        Assert.Contains("ورود با test", page.Main, StringComparison.Ordinal);
    }
}
