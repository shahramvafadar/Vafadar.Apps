using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
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
public class BrowserSite : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The name of the story in addresses.</summary>
    public const string Story = "story-rostam-sohrab";

    /// <summary>
    /// Gets what a check of accessibility looks at on a page of Dastan: all of it but the notice the control
    /// of the graph (ADR 0020) puts over a page while the site has no key of its licence. The notice is no
    /// part of Dastan and is not there on the site of the owner; it alone is left out.
    /// </summary>
    public static Deque.AxeCore.Commons.AxeRunContext PageOfDastan => new() { Exclude = [new Deque.AxeCore.Commons.AxeSelector(".sf-license-validation-banner")] };

    // More windows than any one test opens.
    private const int WindowsKept = 4;

    private readonly System.Collections.Concurrent.ConcurrentQueue<IBrowserContext> _windows = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _address = string.Empty;

    /// <summary>
    /// Gets the engine of the browser the tests run in: <c>chromium</c> unless the environment variable
    /// <c>DASTAN_BROWSER_ENGINE</c> names <c>firefox</c> or <c>webkit</c>. The container has all three.
    /// </summary>
    public static string Engine => Environment.GetEnvironmentVariable("DASTAN_BROWSER_ENGINE") is { Length: > 0 } engine ? engine.ToLowerInvariant() : "chromium";

    /// <summary>Gets the address of the browser the tests connect to, or <see langword="null"/> when none is named.</summary>
    public static string? Endpoint => Environment.GetEnvironmentVariable("DASTAN_BROWSER") is { Length: > 0 } endpoint ? endpoint : null;

    /// <summary>
    /// Gets or sets a value indicating whether the site answers with a newer service worker than the one in its
    /// files: the same worker, keeping its files under another name. It stands for a later release of the site.
    /// </summary>
    public bool NewerWorker { get; set; }

    /// <summary>Gets the port the site listens on; 0 lets the system choose one.</summary>
    protected virtual int Port => 0;

    /// <summary>Gets the address the browser reaches the site at, once the site listens.</summary>
    public string Address => _address;

    /// <summary>Tells the site more than the demonstration needs: a site of its own kind says its settings here.</summary>
    protected virtual void Settings(IWebHostBuilder builder)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new LaterRelease(this)));
        Settings(builder);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (Endpoint is not { } endpoint)
        {
            return;
        }

        // The site listens on this machine only, on a port the system chooses. The browser lives in a container;
        // the connection to it carries the addresses of this machine with it, so the browser opens the site as
        // "localhost" – which is also what a browser needs to treat a site without a certificate as safe enough
        // for a service worker.
        UseKestrel(options => options.Listen(IPAddress.Loopback, Port));
        StartServer();
        var port = new Uri(Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;
        _address = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";

        _playwright = await Playwright.CreateAsync();
        var type = Engine switch
        {
            "chromium" => _playwright.Chromium,
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => throw new InvalidOperationException($"DASTAN_BROWSER_ENGINE is \"{Engine}\"; the engines are chromium, firefox and webkit."),
        };
        _browser = await type.ConnectAsync(endpoint, new BrowserTypeConnectOptions { ExposeNetwork = "<loopback>" });
    }

    /// <summary>
    /// Opens a window of a visitor of their own: own cookies, a width, a language of the browser. A test without a
    /// browser to connect to is skipped here.
    /// </summary>
    /// <param name="width">The width of the window in CSS pixels.</param>
    /// <param name="readTo">The reading stage the visitor has already chosen, if any (0 is the first).</param>
    /// <param name="dark">Whether the device of the visitor asks for the dark appearance.</param>
    /// <param name="script">Whether the browser of the visitor runs scripts.</param>
    /// <param name="offline">Whether the visitor turned reading without a connection on.</param>
    /// <param name="graph">
    /// Whether the scripts that draw a graph (ADR 0020) and a timeline (ADR 0021) reach the visitor. A test of the still drawing – which
    /// is what a page first answers with, and what stays where that script does not arrive – says nothing and
    /// gets the still drawing for as long as it looks; a test of the drawing that moves asks for it.
    /// </param>
    public async Task<IPage> Visitor(int width = 360, int? readTo = null, bool dark = false, bool script = true, bool offline = false, bool graph = false)
    {
        Assert.SkipWhen(_browser is null, "No browser: start the container of src/Apps/Dastan/compose.yaml and set DASTAN_BROWSER=ws://127.0.0.1:53333/.");

        // The tests of a class run one after the other and none has more than a few visitors at a time: the
        // windows of earlier tests are closed. Left open, each keeps processes of its own in Firefox and WebKit,
        // and a class of sixty tests takes gigabytes.
        while (_windows.Count >= WindowsKept && _windows.TryDequeue(out var earlier))
        {
            await earlier.CloseAsync();
        }

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _address,
            ViewportSize = new ViewportSize { Width = width, Height = 800 },
            Locale = "en-US",
            ColorScheme = dark ? ColorScheme.Dark : ColorScheme.Light,
            JavaScriptEnabled = script,
        });
        _windows.Enqueue(context);
        if (!graph)
        {
            await context.RouteAsync(new System.Text.RegularExpressions.Regex(@"/(graph|timeline)(\.[a-z0-9]+)?\.js$"), route => route.AbortAsync());
        }

        if (offline)
        {
            // Somebody who turned reading without a connection on (row 18.8.2): only then the site has a worker.
            await context.AddCookiesAsync([new Cookie { Name = "dastan.offline", Value = "on", Url = _address + "/" }]);
        }

        if (readTo is { } level)
        {
            await context.AddCookiesAsync(
            [
                new Cookie { Name = $"dastan.reading.{Story}", Value = level.ToString(CultureInfo.InvariantCulture), Url = _address + "/" },
            ]);
        }

        var page = await context.NewPageAsync();
        if (dark && Engine == "firefox")
        {
            // Firefox, driven from here, forgets the appearance of the device when a window opens its first
            // page. The window opens a file of the site first and is told the appearance again; from then on
            // it keeps it.
            await page.GotoAsync("/favicon.svg");
            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        }

        return page;
    }

    /// <summary>
    /// Clicks what sends a form and waits until the page it leads to is there. Where that page has the address
    /// the browser is already at – a second "accept" after the first –, <see cref="Arrived"/> alone found the
    /// old page and the test read it (WebKit on the build server, 2026-11-23): this waits for a new document
    /// of that address first. It asks no script of the page, so it also waits in a window without scripts.
    /// </summary>
    public static async Task Sent(IPage page, ILocator button, System.Text.RegularExpressions.Regex address)
    {
        var moved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Navigated(object? sender, IFrame frame)
        {
            if (frame == page.MainFrame && address.IsMatch(frame.Url))
            {
                moved.TrySetResult();
            }
        }

        page.FrameNavigated += Navigated;
        try
        {
            await button.ClickAsync();
            await moved.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            page.FrameNavigated -= Navigated;
        }

        await Arrived(page, address);
    }

    /// <summary>
    /// Waits until the window shows an address and the page there has been loaded. A link that is chosen while
    /// its page is still loading is not always followed in WebKit; a visitor is slower than a test.
    /// <para>
    /// The wait for the event of the load missed it now and then, in every engine and most on a slow machine
    /// (the runner of CI, 2026-11-20): it ended only with its time, and its log said the network had long been
    /// idle. Why the event was missed is not known. So the wait is short and asked again, and between two
    /// waits the document itself is asked whether it is complete; a document that really never completes still
    /// fails here, after the same thirty seconds. (Asking the document alone would not do: a window without
    /// scripts does not answer in every engine.)
    /// </para>
    /// </summary>
    public static async Task Arrived(IPage page, System.Text.RegularExpressions.Regex address)
    {
        await Assertions.Expect(page).ToHaveURLAsync(address);
        var until = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try
            {
                await page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 2_000 });
                return;
            }
            catch (TimeoutException)
            {
                // Asked again below.
            }

            try
            {
                var state = await page.EvaluateAsync<string>("document.readyState").WaitAsync(TimeSpan.FromSeconds(2));
                if (state == "complete")
                {
                    return;
                }
            }
            catch (Exception stopped) when (stopped is PlaywrightException or TimeoutException)
            {
                // The window went on to another document while it was asked, or does not answer without scripts.
            }

            if (DateTime.UtcNow > until)
            {
                throw new TimeoutException($"The page at {page.Url} was not loaded within thirty seconds.");
            }
        }
    }

    // Stands in front of everything the site maps and answers the address of the worker when a test asks for it.
    private sealed class LaterRelease(BrowserSite site) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, following) =>
            {
                if (!site.NewerWorker || context.Request.Path != "/service-worker.js")
                {
                    await following(context);
                    return;
                }

                var files = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                var worker = await File.ReadAllTextAsync(Path.Combine(files, "service-worker.js"), context.RequestAborted);
                context.Response.ContentType = "text/javascript";
                context.Response.Headers.CacheControl = "no-cache";
                await context.Response.WriteAsync(worker.Replace("'dastan-shell-2'", "'dastan-shell-3'", StringComparison.Ordinal), context.RequestAborted);
            });
            next(app);
        };
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
