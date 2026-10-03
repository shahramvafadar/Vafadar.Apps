using System.Net;
using System.Net.Sockets;
using Vafadar.Authentication.OAuth;

namespace Vafadar.Authentication.Tests;

public sealed class LoopbackAuthorizationBrowserTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listens_on_a_free_loopback_port()
    {
        var browser = new LoopbackAuthorizationBrowser((_, _) => Task.CompletedTask);
        await using var first = await browser.StartAsync(Ct);
        await using var second = await browser.StartAsync(Ct);

        Assert.Equal("http", first.RedirectUri.Scheme);
        Assert.Equal("127.0.0.1", first.RedirectUri.Host);
        Assert.Equal("/", first.RedirectUri.AbsolutePath);
        Assert.NotEqual(first.RedirectUri.Port, second.RedirectUri.Port);
    }

    [Fact]
    public async Task Returns_the_redirect_parameters_and_ignores_idle_sockets_and_other_requests()
    {
        string? page = null;
        HttpStatusCode? faviconStatus = null;
        var browser = new LoopbackAuthorizationBrowser(async (uri, ct) =>
        {
            // What a browser does: an idle preconnect socket, a favicon request, then the redirect.
            var redirect = new Uri(Uri.UnescapeDataString(uri.Query["?redirect_uri=".Length..]));
            using var idle = new TcpClient();
            await idle.ConnectAsync(IPAddress.Loopback, redirect.Port, ct);
            using var http = new HttpClient();
            faviconStatus = (await http.GetAsync(new Uri(redirect, "/favicon.ico"), ct)).StatusCode;
            page = await http.GetStringAsync(new Uri(redirect, "/?code=4%2F0Ab-c_d&state=s1&scope=a+b"), ct);
        })
        {
            CompletionMessage = "Done <here>",
            RequestTimeout = TimeSpan.FromMilliseconds(500),
        };

        await using var session = await browser.StartAsync(Ct);
        var answer = await session.AuthorizeAsync(new Uri("https://accounts.example/auth?redirect_uri=" + Uri.EscapeDataString(session.RedirectUri.ToString())), Ct);

        Assert.Equal("4/0Ab-c_d", answer["code"]);
        Assert.Equal("s1", answer["state"]);
        Assert.Equal("a b", answer["scope"]);
        Assert.Equal(HttpStatusCode.NotFound, faviconStatus);
        Assert.Contains("Done &lt;here&gt;", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_error_redirect_is_returned_too()
    {
        var browser = new LoopbackAuthorizationBrowser(async (uri, ct) =>
        {
            var redirect = new Uri(Uri.UnescapeDataString(uri.Query["?redirect_uri=".Length..]));
            using var http = new HttpClient();
            await http.GetStringAsync(new Uri(redirect, "/?error=access_denied&state=s1"), ct);
        });

        await using var session = await browser.StartAsync(Ct);
        var answer = await session.AuthorizeAsync(new Uri("https://accounts.example/auth?redirect_uri=" + Uri.EscapeDataString(session.RedirectUri.ToString())), Ct);

        Assert.Equal("access_denied", answer["error"]);
    }

    [Fact]
    public async Task An_abandoned_sign_in_times_out_as_cancelled()
    {
        var browser = new LoopbackAuthorizationBrowser((_, _) => Task.CompletedTask) { Timeout = TimeSpan.FromMilliseconds(200) };
        await using var session = await browser.StartAsync(Ct);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.AuthorizeAsync(new Uri("https://accounts.example/auth"), Ct));
    }

    [Fact]
    public async Task The_listener_stops_with_the_session()
    {
        var browser = new LoopbackAuthorizationBrowser((_, _) => Task.CompletedTask);
        var session = await browser.StartAsync(Ct);
        var port = session.RedirectUri.Port;
        await session.DisposeAsync();

        using var client = new TcpClient();
        await Assert.ThrowsAnyAsync<SocketException>(() => client.ConnectAsync(IPAddress.Loopback, port, Ct).AsTask());
    }

    [Theory]
    [InlineData("/?code=a%20b&state=x", "/", "a b")]
    [InlineData("/?state=x&code=first&code=second", "/", "first")]
    public void Parses_and_decodes_the_redirect(string target, string path, string code)
    {
        var parameters = LoopbackAuthorizationBrowser.ParseRedirectTarget(target, path);

        Assert.NotNull(parameters);
        Assert.Equal(code, parameters["code"]);
        Assert.Equal("x", parameters["state"]);
    }

    [Fact]
    public void Another_path_is_not_the_redirect()
    {
        Assert.Null(LoopbackAuthorizationBrowser.ParseRedirectTarget("/favicon.ico", "/"));
        Assert.Null(LoopbackAuthorizationBrowser.ParseRedirectTarget("/other?code=1", "/"));
    }
}
