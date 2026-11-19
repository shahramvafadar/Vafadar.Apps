using System.Net;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site with a store for accounts, told a made-up provider, in an environment.</summary>
public sealed class MadeUpSite(string environment, bool invitationOnly = false) : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-made-up-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("Dastan:Content", "../content");
        builder.UseSetting("Dastan:Audience", "LocalDemonstration");
        builder.UseSetting("Dastan:Database", TestDatabase.For(Path.Combine(_folder, "people.db")));
        builder.UseSetting("Dastan:SignIn:MadeUp", "somebody-made-up");
        builder.UseSetting("Dastan:InvitationOnly", invitationOnly ? "true" : "false");
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
/// The made-up provider (step 18.2.2): a way to see the forms after a sign-in through a provider on a
/// developer's machine. It exists in development only.
/// </summary>
public sealed class MadeUpProviderTests
{
    private const string Account = "/en/account";
    private const string Forms = "/preferences/account";

    [Fact]
    public async Task In_development_the_made_up_provider_leads_to_the_form_that_chooses_a_name_and_then_to_an_account()
    {
        using var site = new MadeUpSite("Development");
        var visitor = site.Visitor();

        Assert.Contains("a made-up provider (development only)", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
        var form = await visitor.Post($"{Forms}/external", Account, [("provider", "made-up"), ("returnUrl", Account)]);
        for (var hops = 0; hops < 5 && form.Response.Headers.Location is { } next; hops++)
        {
            form = await visitor.Get(next.OriginalString);
        }

        Assert.Contains($"action=\"{Forms}/external-register\"", form.Html, StringComparison.Ordinal);

        // The form says what is kept of an account without a password, not what the form with a password says.
        var choosing = form.Main[form.Main.IndexOf("Choose a name", StringComparison.Ordinal)..form.Main.IndexOf("Make the account", StringComparison.Ordinal)];
        Assert.Contains("it opens only through a made-up provider (development only)", choosing, StringComparison.Ordinal);
        Assert.DoesNotContain("derived from the password", choosing, StringComparison.Ordinal);

        await visitor.Post($"{Forms}/external-register", Account, [("name", "Made Up Visitor"), ("age", "on"), ("terms", "on"), ("returnUrl", Account)]);
        Assert.Contains("Made Up Visitor", (await visitor.Get(Account)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_that_takes_accounts_by_invitation_only_says_so_to_who_arrives_through_a_provider_without_one()
    {
        using var site = new MadeUpSite("Development", invitationOnly: true);
        var visitor = site.Visitor();

        var arrived = await visitor.Post($"{Forms}/external", Account, [("provider", "made-up"), ("returnUrl", Account)]);
        for (var hops = 0; hops < 5 && arrived.Response.Headers.Location is { } next; hops++)
        {
            arrived = await visitor.Get(next.OriginalString);
        }

        // The form would be refused: the page says why and does not offer it.
        Assert.Contains("Choose a name", arrived.Main, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/external-register\"", arrived.Html, StringComparison.Ordinal);
        Assert.Contains("invitation", arrived.Main[arrived.Main.IndexOf("Choose a name", StringComparison.Ordinal)..], StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Outside_development_the_setting_offers_nothing_and_signs_nobody_in(string environment)
    {
        using var site = new MadeUpSite(environment);
        var visitor = site.Visitor();

        var page = await visitor.Get(Account);
        Assert.DoesNotContain("made-up", page.Html, StringComparison.Ordinal);

        // The form is refused: as one sent over plain http, or as one that names a provider the site does not have.
        var refused = await visitor.Post($"{Forms}/external", Account, [("provider", "made-up"), ("returnUrl", Account)]);
        Assert.True(
            refused.Status == HttpStatusCode.BadRequest || refused.Response.Headers.Location?.OriginalString.Contains("/refused", StringComparison.Ordinal) == true,
            $"{refused.Status} {refused.Response.Headers.Location}");
        Assert.DoesNotContain(refused.Response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [], cookie => cookie.Contains("dastan.visitor.arriving=", StringComparison.Ordinal) && !cookie.Contains("dastan.visitor.arriving=;", StringComparison.Ordinal));
        Assert.DoesNotContain($"action=\"{Forms}/external-register\"", (await visitor.Get(Account)).Html, StringComparison.Ordinal);
    }
}
