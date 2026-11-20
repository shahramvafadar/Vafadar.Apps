using System.Buffers.Binary;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The installable app (ADR 0007): the manifest and its icons, the service worker and the page for "no connection",
/// the one script of the pages, and that a demonstration is not for search engines.
/// </summary>
public sealed class InstallableAppTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    // --- The manifest ---

    [Fact]
    public async Task The_manifest_names_the_app_its_start_and_its_scope_and_every_icon_it_lists_is_what_it_says()
    {
        var visitor = site.Visitor();
        var answer = await visitor.Get("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("application/manifest+json", answer.Response.Content.Headers.ContentType!.MediaType);
        using var manifest = JsonDocument.Parse(answer.Html);
        var root = manifest.RootElement;
        Assert.Equal("Dastan", root.GetProperty("name").GetString());
        Assert.Equal("Dastan", root.GetProperty("short_name").GetString());
        Assert.Equal("/", root.GetProperty("id").GetString());
        Assert.Equal("/", root.GetProperty("start_url").GetString());
        Assert.Equal("/", root.GetProperty("scope").GetString());
        Assert.Equal("standalone", root.GetProperty("display").GetString());

        var icons = root.GetProperty("icons").EnumerateArray().ToList();
        Assert.Contains(icons, icon => icon.GetProperty("sizes").GetString() == "192x192" && icon.GetProperty("purpose").GetString() == "any");
        Assert.Contains(icons, icon => icon.GetProperty("sizes").GetString() == "512x512" && icon.GetProperty("purpose").GetString() == "any");
        Assert.Contains(icons, icon => icon.GetProperty("sizes").GetString() == "512x512" && icon.GetProperty("purpose").GetString() == "maskable");
        foreach (var icon in icons)
        {
            var file = await visitor.Get(icon.GetProperty("src").GetString()!);
            Assert.Equal(HttpStatusCode.OK, file.Status);
            Assert.Equal(icon.GetProperty("type").GetString(), file.Response.Content.Headers.ContentType!.MediaType);
            if (icon.GetProperty("type").GetString() == "image/png")
            {
                // A PNG states its size after its signature and the name of its first part.
                var bytes = await file.Bytes();
                Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], bytes[..4]);
                var size = $"{BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16))}x{BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20))}";
                Assert.Equal(icon.GetProperty("sizes").GetString(), size);
            }
        }
    }

    [Theory]
    [InlineData(null, "/en/")]
    [InlineData("fa,en;q=0.5", "/fa/")]
    public async Task The_start_of_the_app_is_the_start_page_in_the_language_of_the_visitor(string? languages, string expected)
    {
        var visitor = site.Visitor(languages);
        var first = await visitor.Get("/");
        var page = await visitor.Open("/");

        Assert.Equal(expected, first.Response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Single(Regex.Matches(page.Html, "<h1"));
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab/entity/character-rostam")]
    [InlineData("/en/story-rostam-sohrab/entity/character-nobody")]
    public async Task Every_page_names_the_manifest_the_icon_for_a_home_screen_and_the_colour_around_it(string address)
    {
        var page = await site.Visitor().Get(address);

        // One app for both languages: these addresses carry no language.
        Assert.Matches("<link rel=\"manifest\" href=\"/manifest.webmanifest\" ?/?>", page.Html);
        Assert.Matches("<link rel=\"apple-touch-icon\" href=\"/icons/apple-touch-icon.png\" ?/?>", page.Html);
        Assert.Matches("<meta name=\"theme-color\" content=\"#fbfaf7\" media=\"[(]prefers-color-scheme: light[)]\" ?/?>", page.Html);
        Assert.Matches("<meta name=\"theme-color\" content=\"#14132a\" media=\"[(]prefers-color-scheme: dark[)]\" ?/?>", page.Html);
    }

    [Fact]
    public async Task A_visitor_who_chose_an_appearance_gets_its_colour_around_the_page()
    {
        var visitor = site.Visitor();
        await visitor.Post("/preferences/theme", "/en/", ("theme", "dark"), ("returnUrl", "/en/"));

        var page = await visitor.Get("/en/");

        Assert.Single(Regex.Matches(page.Html, "<meta name=\"theme-color\""));
        Assert.Matches("<meta name=\"theme-color\" content=\"#14132a\" ?/?>", page.Html);
    }

    // --- The one script, the service worker and the page for "no connection" ---

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab")]
    [InlineData("/en/story-rostam-sohrab/story")]
    [InlineData("/en/story-rostam-sohrab/search?q=rostam")]
    [InlineData("/en/story-rostam-sohrab/entity/character-nobody")]
    public async Task A_page_has_one_script_a_file_of_the_site_and_nothing_written_into_the_page(string address)
    {
        var visitor = site.Visitor();
        var page = await visitor.Get(address);

        var script = Assert.Single(Regex.Matches(page.Html, "<script[^>]*>(.*?)</script>", RegexOptions.Singleline));
        Assert.Matches("^<script src=\"app[^\"/:]*\\.js\" defer></script>$", script.Value);
        Assert.DoesNotMatch("\\son[a-z]+=\"", page.Html);
        Assert.DoesNotContain("javascript:", page.Html, StringComparison.OrdinalIgnoreCase);

        // The address of the file is written from the start of the language, like every address of a page.
        var file = await visitor.Get($"/{address.Split('/')[1]}/{Regex.Match(script.Value, "src=\"([^\"]+)\"").Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, file.Status);
        Assert.Equal("text/javascript", file.Response.Content.Headers.ContentType!.MediaType);
        // The file is built from TypeScript and made small (ADR 0019): it still registers the worker of the site.
        Assert.Contains("navigator.serviceWorker.register(\"/service-worker.js\",{scope:\"/\"})", file.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_service_worker_keeps_the_page_for_no_connection_and_only_what_the_site_lets_it_keep()
    {
        var visitor = site.Visitor();
        var worker = await visitor.Get("/service-worker.js");

        Assert.Equal(HttpStatusCode.OK, worker.Status);
        Assert.Equal("text/javascript", worker.Response.Content.Headers.ContentType!.MediaType);

        // The browser asks for the worker again each time, so that a new one is found.
        Assert.True(worker.Response.Headers.CacheControl is { NoCache: true });

        var kept = Regex.Match(worker.Html, "const FILES = \\[(.*?)\\];", RegexOptions.Singleline).Groups[1].Value;
        var files = Regex.Matches(kept, "'([^']+)'").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(["/offline.html", "/app.css", "/_content/Vafadar.Web/vafadar.css", "/fonts/vazirmatn-nl-wght.woff2", "/icon.svg"], files);
        foreach (var file in files)
        {
            Assert.Equal(HttpStatusCode.OK, (await visitor.Get(file)).Status);
        }

        // It keeps a page only when the site says that the page may be kept (D-064), in a store of its own
        // that a form of the preferences empties; it names no collection, and never a photograph of a visitor.
        Assert.Contains("answer.headers.get('Dastan-Keep') === 'page'", worker.Html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(worker.Html, "read\\.put\\("));
        Assert.Contains("address.pathname.startsWith('/preferences/')", worker.Html, StringComparison.Ordinal);
        Assert.Contains("caches.delete(READ)", worker.Html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(worker.Html, "addAll"));
        Assert.DoesNotContain(Story, worker.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("/photo", worker.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("/account", worker.Html, StringComparison.Ordinal);

        // A page that is shown from what was kept says so, in the language of its address.
        Assert.Contains("kept ? marked(kept, address.pathname) : caches.match('/offline.html')", worker.Html, StringComparison.Ordinal);
        Assert.Contains("class=\"note kept-copy\" role=\"status\" lang=\"fa\" dir=\"rtl\"", worker.Html, StringComparison.Ordinal);
        Assert.Contains("class=\"note kept-copy\" role=\"status\" lang=\"en\" dir=\"ltr\"", worker.Html, StringComparison.Ordinal);

        // With a connection every page comes from the network: what was kept is asked for only in a catch.
        Assert.All(Regex.Matches(worker.Html, "read\\.match\\(").Select(found => worker.Html[..found.Index]), before => Assert.Contains(".catch(() => caches.open(READ)", before[^120..], StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_page_for_no_connection_says_so_in_both_languages_and_holds_nothing_of_a_collection()
    {
        var page = await site.Visitor().Get("/offline.html");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>No connection</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("اتصال برقرار نیست", page.Html, StringComparison.Ordinal);
        Assert.Matches("<section class=\"page-header\" lang=\"fa\" dir=\"rtl\">", page.Html);

        // The way on is the same address again; there is no other link, no script and no name of a story.
        Assert.Equal([string.Empty, string.Empty], page.Links);
        Assert.DoesNotContain("<script", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rostam", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("رستم", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(Story, page.Html, StringComparison.Ordinal);
    }

    // --- Not for search engines ---

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab/story")]
    [InlineData("/manifest.webmanifest")]
    [InlineData("/media/story-rostam-sohrab/media-nothing/file")]
    public async Task A_demonstration_asks_not_to_be_indexed_with_every_answer(string address)
    {
        var answer = await site.Visitor().Get(address);

        Assert.Equal("noindex, nofollow", Assert.Single(answer.Response.Headers.GetValues("X-Robots-Tag")));
        if (answer.Html.Contains("<html", StringComparison.Ordinal))
        {
            Assert.Matches("<meta name=\"robots\" content=\"noindex, nofollow\" ?/?>", answer.Html);
        }
    }
}

/// <summary>The site for the public is an ordinary site for search engines.</summary>
public sealed class PublicIndexingTests(PublicSite site) : IClassFixture<PublicSite>
{
    [Fact]
    public async Task The_site_for_the_public_does_not_ask_to_be_left_out()
    {
        var page = await site.Visitor().Get("/en/");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.False(page.Response.Headers.Contains("X-Robots-Tag"));
        Assert.DoesNotContain("name=\"robots\"", page.Html, StringComparison.Ordinal);
        Assert.Matches("<link rel=\"manifest\" href=\"/manifest.webmanifest\" ?/?>", page.Html);
    }
}
