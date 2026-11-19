using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// What the owner decided after phase 16 about who makes an account (step 17.1): an account is for people of
/// 16 and older (D-061), and the site of a closed trial takes new accounts by invitation only (D-068).
/// </summary>
public sealed class ClosedTrialTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private static Task<Page> Register(Browser visitor, string name, string from = Account, string age = "on", string invitation = "") =>
        visitor.Post($"{Forms}/register", from, ("name", name), ("password", Password), ("again", Password), ("age", age), ("terms", "on"), ("invitation", invitation), ("returnUrl", Account));

    private static bool SignedIn(Page page) => page.Html.Contains($"action=\"{Forms}/sign-out\"", StringComparison.Ordinal);

    [Theory]
    [InlineData("en", "I am 16 or older.")]
    [InlineData("fa", "۱۶ سال یا بیشتر دارم.")]
    public async Task Making_an_account_asks_whether_somebody_is_sixteen_or_older(string language, string words)
    {
        var page = await _site.Visitor().Get($"/{language}/account");

        Assert.Contains(words, page.Main, StringComparison.Ordinal);
        Assert.Contains("name=\"age\" type=\"checkbox\" required", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_the_words_about_the_age_no_account_is_made()
    {
        var visitor = _site.Visitor();

        var refused = await Register(visitor, "Mina", age: string.Empty);

        Assert.Equal($"{Account}?form=register&problem=age#register", refused.Response.Headers.Location?.OriginalString);
        Assert.False(SignedIn(await visitor.Get(Account)));
        Assert.Contains("16 and older", (await visitor.Get($"{Account}?form=register&problem=age")).Main, StringComparison.Ordinal);

        await Register(visitor, "Mina");
        Assert.True(SignedIn(await visitor.Get(Account)));
    }

    [Fact]
    public async Task A_site_that_is_open_takes_an_account_without_an_invitation()
    {
        var visitor = _site.Visitor();

        Assert.DoesNotContain("by invitation only", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
        await Register(visitor, "Mina");

        Assert.True(SignedIn(await visitor.Get(Account)));
    }

    [Fact]
    public async Task A_closed_trial_takes_a_new_account_by_invitation_only()
    {
        // The first account is made before the site is closed; it invites.
        var mina = _site.Visitor();
        await Register(mina, "Mina");
        await mina.Post($"{Forms}/invite", Account, ("returnUrl", Account));
        var code = Regex.Match((await mina.Get(Account)).Html, "\\?invitation=([A-Za-z0-9]+)").Groups[1].Value;
        Assert.NotEmpty(code);

        using var closed = _site.WithWebHostBuilder(builder => builder.UseSetting("Dastan:InvitationOnly", "true"));
        Browser Visitor() => Browser.Of(closed);

        // Without an invitation there is no form, and one that is sent all the same makes nothing.
        var stranger = Visitor();
        var page = await stranger.Get(Account);
        Assert.Contains("by invitation only", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/register\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/sign-in\"", page.Html, StringComparison.Ordinal);
        Assert.Equal($"{Account}?form=register&problem=invitation-needed#register", (await Register(stranger, "Dara")).Response.Headers.Location?.OriginalString);
        Assert.Contains("problem=invitation-needed", (await Register(stranger, "Dara", invitation: "NOSUCHCODE")).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.False(SignedIn(await stranger.Get(Account)));

        // Who was invited finds the form and makes an account; the invitation is used up by it.
        var sara = Visitor();
        var invited = await sara.Get($"{Account}?invitation={code}");
        Assert.Contains($"action=\"{Forms}/register\"", invited.Html, StringComparison.Ordinal);
        await Register(sara, "Sara", from: $"{Account}?invitation={code}", invitation: code);
        Assert.True(SignedIn(await sara.Get(Account)));
        Assert.Contains("problem=invitation-needed", (await Register(Visitor(), "Dara", invitation: code)).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        // Who has an account signs in as ever.
        var again = Visitor();
        await again.Post($"{Forms}/sign-in", Account, ("name", "Mina"), ("password", Password), ("returnUrl", Account));
        Assert.True(SignedIn(await again.Get(Account)));
    }

    [Theory]
    [InlineData("en", "another kind")]
    [InlineData("fa", "گونه‌ای دیگر")]
    public async Task A_relationship_may_be_of_another_kind_than_the_ones_the_site_names(string language, string words)
    {
        var mina = _site.Visitor();
        await Register(mina, "Mina");
        await Register(_site.Visitor(), "Sara");

        Assert.Contains(words, (await mina.Get($"/{language}/account/people")).Main, StringComparison.Ordinal);
        var asked = await mina.Post("/preferences/people/ask", "/en/account/people", ("name", "Sara"), ("kind", "other"), ("returnUrl", "/en/account/people"));

        Assert.Contains("said=asked", asked.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }
}
