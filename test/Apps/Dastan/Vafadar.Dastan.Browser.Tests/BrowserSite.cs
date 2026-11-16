using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Cookie = Microsoft.Playwright.Cookie;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The website as it runs for a local demonstration, listening on this machine, and the browser of the Compose
/// project "test" that visits it. The browser is named by the environment variable <c>DASTAN_BROWSER</c>
/// (<c>ws://127.0.0.1:53333/</c> for the container of <c>src/Apps/Dastan/compose.yaml</c>).
/// </summary>
public sealed class BrowserSite : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The name of the story in addresses.</summary>
    public const string Story = "story-rostam-sohrab";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _address = string.Empty;

    /// <summary>Gets the address of the browser the tests connect to, or <see langword="null"/> when none is named.</summary>
    public static string? Endpoint => Environment.GetEnvironmentVariable("DASTAN_BROWSER") is { Length: > 0 } endpoint ? endpoint : null;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (Endpoint is not { } endpoint)
        {
            return;
        }

        // The site listens on this machine only, on a port the system chooses. The browser lives in a container
        // and reaches the machine by the name Docker gives it.
        UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        StartServer();
        var port = new Uri(Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;
        _address = $"http://host.docker.internal:{port.ToString(CultureInfo.InvariantCulture)}";

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.ConnectAsync(endpoint);
    }

    /// <summary>
    /// Opens a window of a visitor of their own: own cookies, a width, a language of the browser. A test without a
    /// browser to connect to is skipped here.
    /// </summary>
    /// <param name="width">The width of the window in CSS pixels.</param>
    /// <param name="readTo">The reading stage the visitor has already chosen, if any (0 is the first).</param>
    /// <param name="dark">Whether the device of the visitor asks for the dark appearance.</param>
    public async Task<IPage> Visitor(int width = 360, int? readTo = null, bool dark = false)
    {
        Assert.SkipWhen(_browser is null, "No browser: start the container of src/Apps/Dastan/compose.yaml and set DASTAN_BROWSER=ws://127.0.0.1:53333/.");

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _address,
            ViewportSize = new ViewportSize { Width = width, Height = 800 },
            Locale = "en-US",
            ColorScheme = dark ? ColorScheme.Dark : ColorScheme.Light,
        });
        if (readTo is { } level)
        {
            await context.AddCookiesAsync(
            [
                new Cookie { Name = $"dastan.reading.{Story}", Value = level.ToString(CultureInfo.InvariantCulture), Url = _address + "/" },
            ]);
        }

        return await context.NewPageAsync();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        await base.DisposeAsync();
    }
}
