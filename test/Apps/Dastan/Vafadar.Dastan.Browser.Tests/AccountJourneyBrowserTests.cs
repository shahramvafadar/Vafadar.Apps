using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The site with accounts as a visitor reaches it: a database of its own, and mail that really leaves – by
/// SMTP to the catcher of the Compose project, from where a test reads it as a person reads their mailbox.
/// The site listens on a port that is known before it starts, because a link in a mail begins with the
/// address of the site.
/// </summary>
public sealed class AccountsBrowserSite : BrowserSite
{
    private readonly int _port = FreePort();
    private readonly string _photos = Path.Combine(Path.GetTempPath(), "dastan-browser-photos-" + Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    protected override int Port => _port;

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(_photos))
        {
            Directory.Delete(_photos, recursive: true);
        }
    }

    /// <inheritdoc />
    protected override void Settings(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (Endpoint is null)
        {
            // No browser, no test: nothing of a database or a catcher is asked for.
            return;
        }

        builder.UseSetting("Dastan:Database", TestDatabase.For("browser-accounts-" + _port.ToString(CultureInfo.InvariantCulture)));
        builder.UseSetting("Dastan:Photos", _photos);
        builder.UseSetting("Dastan:Operator:Site", $"http://localhost:{_port.ToString(CultureInfo.InvariantCulture)}");
        builder.UseSetting("Dastan:Mail:Host", MailCatcher.Host);
        builder.UseSetting("Dastan:Mail:Port", MailCatcher.Port.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Dastan:Mail:Security", "None");
        builder.UseSetting("Dastan:Mail:From", "accounts@dastan.test");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

/// <summary>
/// The journeys of an account in a real browser (step 19.4, part seven; ADR 0016): what a visitor types and
/// presses, the mail the site really sends, and the link of that mail opened as it was written. The tests of
/// a class share a site, and a site takes five new accounts an hour from one address: a class makes no more.
/// </summary>
public sealed partial class AccountJourneyBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
{
    private const string Password = "a long pass phrase of a test";
    private const string Other = "another pass phrase of a test";

    private static Task<bool> ScrollsSideways(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");

    internal static Task<bool> SignedIn(IPage page) => page.Locator("form[action$='/sign-out']").IsVisibleAsync();

    // The link of the newest mail of a kind the catcher holds for an address – as its plain text writes it.
    internal static async Task<string> Link(string address, string subject, int expected = 1)
    {
        var mail = (await MailCatcher.For(address, expected)).Last(message => ((string?)message["Subject"])!.Contains(subject, StringComparison.Ordinal));
        var link = LinkForm().Match((string)mail["Text"]!);
        Assert.True(link.Success, (string?)mail["Text"]);
        return link.Value;
    }

    [GeneratedRegex(@"http://localhost:\d+/(?:en|fa)/account/\w+\?id=[0-9a-f]+&token=[0-9a-f]+")]
    private static partial Regex LinkForm();

    // Fills in the form that makes an account, as a person does, and sends it.
    internal static async Task Ask(IPage page, string language, string name, string address)
    {
        await page.GotoAsync($"/{language}/account");
        var form = page.Locator("form[action$='/register']");
        await form.Locator("[name=name]").FillAsync(name);
        await form.Locator("[name=mail]").FillAsync(address);
        await form.Locator("[name=password]").FillAsync(Password);
        await form.Locator("[name=again]").FillAsync(Password);
        await form.Locator("[name=age]").CheckAsync();
        await form.Locator("[name=terms]").CheckAsync();
        await form.Locator("button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=confirm-sent$"));
    }

    internal static string NewName(string who) => $"{who} {Guid.NewGuid().ToString("N")[..8]}";

    [Theory]
    [InlineData("en", "Confirm your mail address for Dastan", "Confirm and open the account", true)]
    [InlineData("fa", "نشانی ایمیل خود را در داستان تأیید کنید", "تأیید و باز کردن حساب", true)]
    [InlineData("en", "Confirm your mail address for Dastan", "Confirm and open the account", false)]
    public async Task A_visitor_makes_an_account_reads_the_mail_and_opens_it_with_the_link(string language, string subject, string button, bool script)
    {
        MailCatcher.Needed();
        var page = await site.Visitor(width: 375, script: script);
        var address = MailCatcher.NewAddress("mina");
        var name = NewName("Mina");

        await Ask(page, language, name, address);

        // Told to look at the mail, and not signed in.
        Assert.False(await SignedIn(page));
        Assert.Equal("status", await page.Locator("main p.note").First.GetAttributeAsync("role"));
        Assert.False(await ScrollsSideways(page));

        // The mail really arrived, in the language of the page, with a link to this site.
        var link = await Link(address, subject);
        Assert.StartsWith($"{site.Address}/{language}/account/confirm?", link, StringComparison.Ordinal);

        // Opening the link changes nothing: the page shows one button.
        await page.GotoAsync(link);
        Assert.Equal(language == "fa" ? "rtl" : "ltr", await page.Locator("html").GetAttributeAsync("dir"));
        Assert.Equal("no-referrer", await page.Locator("meta[name=referrer]").GetAttributeAsync("content"));
        Assert.False(await SignedIn(page));
        Assert.False(await ScrollsSideways(page));
        var confirm = page.Locator("main form button[type=submit]");
        Assert.Equal(button, (await confirm.InnerTextAsync()).Trim());

        // The button is reached with the keyboard and pressed with it.
        await confirm.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));

        Assert.True(await SignedIn(page));
        Assert.Equal(8, await page.Locator("#codes-shown code").CountAsync());
        Assert.Contains(address, await page.Locator("#address").InnerTextAsync(), StringComparison.Ordinal);
        Assert.Contains(name, await page.Locator(".page-header").InnerTextAsync(), StringComparison.Ordinal);
        Assert.False(await ScrollsSideways(page));

        // The link is used up: opened again it leads to no form that could be sent twice.
        await page.GotoAsync(link);
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account/confirm\?problem=link$"));
        Assert.Equal("alert", await page.Locator("main .problems").GetAttributeAsync("role"));
        Assert.Equal(0, await page.Locator("main form").CountAsync());
    }

    [Fact]
    public async Task The_page_of_a_link_tells_no_other_page_where_its_visitor_came_from()
    {
        MailCatcher.Needed();
        var page = await site.Visitor(width: 375);
        var address = MailCatcher.NewAddress("sara");
        await Ask(page, "en", NewName("Sara"), address);
        var link = await Link(address, "Confirm your mail address");
        await page.GotoAsync(link);

        // The address of this page holds the token. A visitor who goes on from it – to the page of the account
        // – takes the address along in no header.
        var asked = await page.RunAndWaitForRequestAsync(
            () => page.Locator("header a[href='account'], nav a[href='account']").First.ClickAsync(),
            request => request.IsNavigationRequest && request.Url.EndsWith("/en/account", StringComparison.Ordinal));

        Assert.DoesNotContain("token", await asked.HeaderValueAsync("referer") ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_forgotten_password_and_another_address_go_through_the_mail_and_back()
    {
        MailCatcher.Needed();
        var page = await site.Visitor(width: 375);
        var address = MailCatcher.NewAddress("dara");
        var next = MailCatcher.NewAddress("dara-new");
        var name = NewName("Dara");
        await Ask(page, "en", name, address);
        await page.GotoAsync(await Link(address, "Confirm your mail address"));
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));

        // Signed out, the password forgotten: the link is asked for by the address.
        await page.Locator("form[action$='/sign-out'] button").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=signed-out$"));
        await page.Locator("#forgot summary").ClickAsync();
        await page.Locator("form[action$='/forgot'] [name=name]").FillAsync(address);
        await page.Locator("form[action$='/forgot'] button").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=forgot-sent$"));

        var reset = await Link(address, "A new password", expected: 2);
        Assert.StartsWith($"{site.Address}/en/account/reset?", reset, StringComparison.Ordinal);
        await page.GotoAsync(reset);
        Assert.Equal("new-password", await page.Locator("#reset-password").GetAttributeAsync("autocomplete"));

        // Two passwords that differ: the page says so beside the form, and the link still works.
        await page.Locator("#reset-password").FillAsync(Other);
        await page.Locator("#reset-again").FillAsync(Password);
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account/reset\?id=.*problem=password-differs$"));
        Assert.Equal("alert", await page.Locator("main .problems").GetAttributeAsync("role"));
        Assert.False(await ScrollsSideways(page));

        await page.Locator("#reset-password").FillAsync(Other);
        await page.Locator("#reset-again").FillAsync(Other);
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=password$"));
        Assert.True(await SignedIn(page));

        // Another address: asked for with the password, confirmed at the new address.
        await page.Locator("#address summary").ClickAsync();
        await page.Locator("#address-new").FillAsync(next);
        await page.Locator("#address-current").FillAsync(Other);
        await page.Locator("form[action$='/address'] button").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=address-sent$"));
        Assert.Contains(address, await page.Locator("#address").InnerTextAsync(), StringComparison.Ordinal);

        var change = await Link(next, "Confirm");
        Assert.StartsWith($"{site.Address}/en/account/address?", change, StringComparison.Ordinal);
        await page.GotoAsync(change);
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=address-changed$"));
        Assert.Contains(next, await page.Locator("#address").InnerTextAsync(), StringComparison.Ordinal);

        // The address the account had was told of every step: the link, the new password, the request, the change.
        Assert.True((await MailCatcher.For(address, expected: 5)).Count >= 5);
    }
}

/// <summary>
/// The pages of an account against the rules of accessibility a tool can decide (WCAG 2.1 A and AA, and what
/// axe calls best practice) – with a site of its own, because a site takes five new accounts an hour from one
/// address.
/// </summary>
public sealed class AccountAccessibilityBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
{
    // What a tool can decide about accessibility, said with the markup that breaks a rule.
    private static async Task BreaksNoRule(IPage page)
    {
        await page.EvaluateAsync("document.fonts.ready");
        var result = await page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "best-practice"] },
        });

        Assert.True(
            result.Violations.Length == 0,
            string.Join(Environment.NewLine, result.Violations.Select(violation =>
                $"{violation.Id} ({violation.Impact}): {violation.Help} – {string.Join(" | ", violation.Nodes.Select(node => node.Html))}")));
    }

    [Theory]
    [InlineData("en", false)]
    [InlineData("fa", false)]
    [InlineData("en", true)]
    [InlineData("fa", true)]
    public async Task The_pages_of_an_account_break_no_rule_of_accessibility_that_a_tool_can_check(string language, bool dark)
    {
        MailCatcher.Needed();
        var page = await site.Visitor(width: 375, dark: dark);
        var address = MailCatcher.NewAddress("axe");

        // Before an account: the forms that sign in, ask for a link and make an account – all of them open.
        await page.GotoAsync($"/{language}/account?form=sign-in&problem=unconfirmed");
        await page.Locator("#forgot summary").ClickAsync();
        await BreaksNoRule(page);

        // The page of a link, and the page of the account with everything it offers open.
        await AccountJourneyBrowserTests.Ask(page, language, AccountJourneyBrowserTests.NewName("Axe"), address);
        await BreaksNoRule(page);
        await page.GotoAsync(await AccountJourneyBrowserTests.Link(address, language == "fa" ? "تأیید" : "Confirm"));
        await BreaksNoRule(page);
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));
        foreach (var more in await page.Locator("main details > summary").AllAsync())
        {
            await more.ClickAsync();
        }

        await BreaksNoRule(page);

        // The page of a link that cannot be used, and the form for a new password with what was wrong.
        await page.GotoAsync($"/{language}/account/confirm?problem=link");
        await BreaksNoRule(page);
        await page.GotoAsync($"/{language}/account/reset?id=abc&token=def&problem=password-differs");
        await BreaksNoRule(page);
    }
}
