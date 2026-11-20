using System.Net;
using Vafadar.Dastan.Tests;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Signing in through a provider that names an address without proving it, on the site (step 20.3, ADR 0016):
/// the visitor writes the address, nobody is signed in before it is confirmed by the mail of the site –
/// neither by coming back through the provider nor by any page of an account – and nothing says whether an
/// address has an account.
/// </summary>
public sealed class UnprovenAddressPagesTests : IDisposable
{
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private readonly ExternalSite _site = new() { Proves = false };

    public void Dispose() => _site.Dispose();

    private static (string, string)[] Fields(params (string, string)[] fields) => [.. fields, ("returnUrl", Account)];

    private static async Task<Page> Follow(Browser visitor, Page page)
    {
        for (var hops = 0; hops < 5 && page.Response.Headers.Location is { } next; hops++)
        {
            page = await visitor.Get(next.OriginalString);
        }

        return page;
    }

    private static async Task<Page> Through(Browser visitor) =>
        await Follow(visitor, await visitor.Post($"{Forms}/external", Account, Fields(("provider", "test"))));

    private static async Task<Page> Choose(Browser visitor, string name, string mail) =>
        await Follow(visitor, await visitor.Post($"{Forms}/external-register", Account, Fields(("name", name), ("mail", mail), ("age", "on"), ("terms", "on"), ("website", string.Empty))));

    private static bool SignedIn(Page page) => page.Html.Contains($"action=\"{Forms}/sign-out\"", StringComparison.Ordinal);

    [Theory]
    [InlineData("mina@corp.example", "mina@corp.example")] // what the provider calls a mailbox, or a name to sign in with that looks like one
    [InlineData("", "")]                                    // the provider names nothing
    public async Task The_visitor_writes_the_address_and_is_nobody_until_the_mail_of_the_site_was_opened(string named, string proposed)
    {
        _site.Mail = named;
        var visitor = _site.Visitor();

        var asked = await Through(visitor);

        // The form asks for the address; what the provider named is a proposal in it and nothing more.
        Assert.False(SignedIn(asked));
        Assert.Contains("Choose a name for your account", asked.Main, StringComparison.Ordinal);
        Assert.Contains($"name=\"mail\" type=\"email\" required", asked.Html, StringComparison.Ordinal);
        Assert.Contains($"value=\"{proposed}\"", asked.Html, StringComparison.Ordinal);
        Assert.Contains("does not prove that an address is yours", asked.Main, StringComparison.Ordinal);

        var made = await Choose(visitor, "Mina", "mina@example.org");

        // Asked for, and nothing more: no sign-in, no codes, and the page of an account is the one of a stranger.
        Assert.False(SignedIn(made));
        Assert.DoesNotContain("<code>", made.Html, StringComparison.Ordinal);
        Assert.Contains("mailed", made.Main, StringComparison.Ordinal);
        Assert.False(SignedIn(await visitor.Get(Account)));
        Assert.Equal(HttpStatusCode.Redirect, (await visitor.Get("/en/account/trees")).Status);

        // Coming back through the provider, any number of times, signs nobody in and makes nothing new.
        for (var time = 0; time < 2; time++)
        {
            var back = await Through(visitor);
            Assert.False(SignedIn(back));
            Assert.Contains("Confirm your mail address", back.Main, StringComparison.Ordinal);
            Assert.DoesNotContain($"action=\"{Forms}/external-register\"", back.Html, StringComparison.Ordinal);
        }

        // The link of the mail makes the account one, with its codes; from then on the provider signs in.
        var mail = visitor.Mailed("mina@example.org", "confirm-address")!;
        var confirmed = await Follow(visitor, await visitor.Follow(mail, "confirm"));
        Assert.True(SignedIn(confirmed));
        Assert.Contains("<code>", confirmed.Html, StringComparison.Ordinal);

        var later = _site.Visitor();
        Assert.True(SignedIn(await Through(later)));
    }

    [Fact]
    public async Task An_address_that_has_an_account_is_answered_like_any_other_and_its_holder_is_told()
    {
        // Mina has an account with her address, made through a provider that verifies.
        _site.Proves = true;
        (_site.Subject, _site.Mail) = ("the-identifier-of-mina", "mina@example.org");
        var mina = _site.Visitor();
        await Through(mina);
        Assert.True(SignedIn(await Follow(mina, await mina.Post($"{Forms}/external-register", Account, Fields(("name", "Mina"), ("age", "on"), ("terms", "on"), ("website", string.Empty))))));

        // Somebody else comes through a provider that proves nothing and writes her address.
        _site.Proves = false;
        (_site.Subject, _site.Mail) = ("the-identifier-of-somebody", string.Empty);
        var somebody = _site.Visitor();
        await Through(somebody);
        var answer = await Choose(somebody, "Somebody", "mina@example.org");
        var other = _site.Visitor();
        (_site.Subject, _site.Mail) = ("the-identifier-of-another", string.Empty);
        await Through(other);
        var free = await Choose(other, "Another", "free@example.org");

        // The same words whether the address has an account or not.
        static string Said(Page page) => System.Text.RegularExpressions.Regex.Match(page.Html, "<p class=\"note\" role=\"status\">(.*?)</p>").Groups[1].Value;
        Assert.NotEqual(string.Empty, Said(answer));
        Assert.Equal(Said(free), Said(answer));
        Assert.Equal(free.Response.RequestMessage!.RequestUri!.PathAndQuery, answer.Response.RequestMessage!.RequestUri!.PathAndQuery);
        Assert.False(SignedIn(answer));
        Assert.DoesNotContain("has an account", answer.Main, StringComparison.OrdinalIgnoreCase);

        // Mina is told that somebody tried; no link went out for her address, and nothing waits for the stranger.
        Assert.NotNull(mina.Mailed("mina@example.org", "address-in-use"));
        Assert.Null(mina.Mailed("mina@example.org", "confirm-address"));
        (_site.Subject, _site.Mail) = ("the-identifier-of-somebody", string.Empty);
        Assert.Contains("Choose a name for your account", (await Through(_site.Visitor())).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Who_waits_corrects_the_address_and_only_the_new_link_opens_the_account()
    {
        _site.Mail = string.Empty;
        var visitor = _site.Visitor();
        await Through(visitor);
        await Choose(visitor, "Mina", "mian@example.org");
        var mistyped = visitor.Mailed("mian@example.org", "confirm-address")!;

        // Something that is no address is said; the form for it is the one of who waits.
        await Through(visitor);
        var wrong = await Follow(visitor, await visitor.Post($"{Forms}/external-waiting", Account, Fields(("mail", "no address"))));
        Assert.Contains("Confirm your mail address", wrong.Main, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", wrong.Html, StringComparison.Ordinal);

        // Without having come through the provider nobody sends this form for somebody else.
        var stranger = _site.Visitor();
        await stranger.Get(Account);
        Assert.Contains("external-failed", (await stranger.Post($"{Forms}/external-waiting", Account, Fields(("mail", "stranger@example.org")))).Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Null(stranger.Mailed("stranger@example.org", "confirm-address"));

        // The store sends a new link a few minutes after the one before at the earliest; the site of the test
        // has the clock of the machine, so the first mail is made old here.
        Age("mian@example.org");
        var sent = await Follow(visitor, await visitor.Post($"{Forms}/external-waiting", Account, Fields(("mail", "mina@example.org"))));
        Assert.False(SignedIn(sent));
        var mail = visitor.Mailed("mina@example.org", "confirm-address")!;

        // The link that went to the mistyped address opens nothing any more.
        Assert.False(SignedIn(await Follow(visitor, await _site.Visitor().Follow(mistyped, "confirm"))));
        Assert.False(SignedIn(await Through(_site.Visitor())));

        Assert.True(SignedIn(await Follow(visitor, await visitor.Follow(mail, "confirm"))));
    }

    [Fact]
    public async Task The_link_that_opens_the_account_really_arrives_by_mail()
    {
        MailCatcher.Needed();
        using var site = new ExternalSite { Proves = false, Mail = string.Empty, MailPort = MailCatcher.Port };
        var to = MailCatcher.NewAddress("mina");
        var visitor = site.Visitor();
        await Through(visitor);
        Assert.False(SignedIn(await Choose(visitor, "Mina", to)));

        // The mail left the site and was caught: it is read there, not in the database.
        var caught = Assert.Single(await MailCatcher.For(to));
        var confirmed = await Follow(visitor, await visitor.Follow((string)caught["Text"]!, "confirm"));

        Assert.True(SignedIn(confirmed));
        Assert.True(SignedIn(await Through(site.Visitor())));
    }

    // Makes the token of the mail that waits for an address as old as the store wants it before it sends another.
    private void Age(string address)
    {
        using var connection = new Npgsql.NpgsqlConnection(_site.Services.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) is Microsoft.Extensions.Configuration.IConfiguration settings ? settings["Dastan:Database"] : null);
        connection.Open();
        using var command = connection.CreateCommand();

        // The value of a token is "hash|ends|sent|carries" (VisitorAccounts.NewToken): the third part is moved back.
        command.CommandText = """
            update identity."VisitorTokens" as token
            set "Value" = split_part("Value", '|', 1) || '|' || split_part("Value", '|', 2) || '|' || (split_part("Value", '|', 3)::bigint - 600)::text || '|' || split_part("Value", '|', 4)
            from identity."VisitorAccounts" as account
            where account."Id" = token."UserId" and lower(account."Email") = lower(@address)
            """;
        command.Parameters.AddWithValue("address", address);
        Assert.Equal(1, command.ExecuteNonQuery());
    }
}
