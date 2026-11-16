using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Web.Tests;

/// <summary>A small site in English, Persian and German with the parts of the library switched on.</summary>
public sealed class SiteTests : IAsyncLifetime
{
    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSiteLanguages("en", "fa", "de");
        builder.Services.Configure<SiteLanguageOptions>(options => options.UnlocalizedPaths.Add("/preferences"));
        builder.Services.AddAntiforgery();

        _app = builder.Build();
        _app.UseSecurityHeaders();
        _app.UseSiteLanguage();
        _app.UseRouting();
        _app.UseAntiforgery();

        // Every answer says what the request looked like after the library had read it.
        static string Seen(HttpContext context)
        {
            var language = context.GetSiteLanguage();
            return $"{language.Tag}|{language.Direction}|{CultureInfo.CurrentUICulture.Name}|{context.Request.PathBase}|{context.Request.Path}";
        }

        _app.MapGet("/", Seen);
        _app.MapGet("/story/{id}", Seen);
        _app.MapGet("/slow", async context =>
        {
            await Task.Delay(30, context.RequestAborted);
            await context.Response.WriteAsync(Seen(context));
        });
        _app.MapGet("/preferences/seen", context => context.Response.WriteAsync($"{Seen(context)}|{ThemePreference.Read(context)}"));
        _app.MapGet("/preferences/token", (HttpContext context, IAntiforgery antiforgery) => antiforgery.GetAndStoreTokens(context).RequestToken);
        _app.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) => antiforgery.GetAndStoreTokens(context).RequestToken);
        _app.MapGet("/app.css", () => "body{}");
        _app.MapThemePreference();
        await _app.StartAsync();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // --- The language of a page comes from its address ---

    [Theory]
    [InlineData("/en/", "en|ltr|en|/en|/")]
    [InlineData("/fa/", "fa|rtl|fa|/fa|/")]
    [InlineData("/fa/story/rostam", "fa|rtl|fa|/fa|/story/rostam")]
    [InlineData("/de/story/rostam", "de|ltr|de|/de|/story/rostam")]
    public async Task The_language_is_read_from_the_address_and_taken_out_of_the_path(string address, string expected)
    {
        Assert.Equal(expected, await Client().GetStringAsync(address, Stop));
    }

    [Theory]
    [InlineData("/fa", "/fa/")]
    [InlineData("/FA/story/rostam?x=1", "/fa/story/rostam?x=1")]
    [InlineData("/De", "/de/")]
    public async Task A_page_has_one_address(string address, string expected)
    {
        var response = await Client().GetAsync(address, Stop);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_word_that_only_begins_like_a_language_is_not_a_language()
    {
        var response = await Client().GetAsync("/fable", Stop);

        Assert.Equal("/en/fable", response.Headers.Location!.OriginalString);
    }

    // --- An address without a language ---

    [Theory]
    [InlineData(null, "/en/")]
    [InlineData("fa-IR,fa;q=0.9,en;q=0.8", "/fa/")]
    [InlineData("fr-FR,de-AT;q=0.9,en;q=0.5", "/de/")]
    [InlineData("en;q=0.3,fa;q=0.9", "/fa/")]
    [InlineData("ja,zh;q=0.9", "/en/")]
    public async Task The_start_of_the_site_goes_to_the_first_language_of_the_browser_that_the_site_has(string? accept, string expected)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", accept);
        }

        var response = await Client().SendAsync(request, Stop);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
        Assert.Contains("Accept-Language", response.Headers.Vary);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task A_page_address_without_a_language_keeps_its_path_and_query()
    {
        var response = await Client().GetAsync("/story/rostam?view=list", Stop);

        Assert.Equal("/en/story/rostam?view=list", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task The_language_of_the_last_page_a_visitor_opened_wins_over_the_browser()
    {
        var visitor = new Visitor(Client());
        await visitor.Get("/fa/story/rostam");

        var response = await visitor.Get("/", accept: "de,en;q=0.5");

        Assert.Equal("/fa/", response.Headers.Location!.OriginalString);
        Assert.Equal("fa", visitor.Cookies["vafadar.language"]);
    }

    [Fact]
    public async Task The_remembered_language_is_a_harmless_cookie_of_the_site_only()
    {
        var response = await Client().GetAsync("/fa/", Stop);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("vafadar.language=fa;", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_language_that_is_already_remembered_is_not_written_again()
    {
        var visitor = new Visitor(Client());
        await visitor.Get("/fa/");

        var second = await visitor.Get("/fa/story/rostam");

        Assert.False(second.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("/app.css")]
    [InlineData("/fa/app.css")]
    public async Task A_file_is_served_with_and_without_a_language_and_changes_nothing(string address)
    {
        var response = await Client().GetAsync(address, Stop);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task An_endpoint_without_a_language_in_its_address_answers_in_the_visitors_language()
    {
        var visitor = new Visitor(Client());
        await visitor.Get("/fa/");

        var seen = await (await visitor.Get("/preferences/seen")).Content.ReadAsStringAsync(Stop);

        Assert.Equal("fa|rtl|fa||/preferences/seen|Auto", seen);
    }

    // --- Visitors do not affect each other ---

    [Fact]
    public async Task Requests_in_different_languages_at_the_same_time_keep_their_own_language()
    {
        var client = Client();
        var languages = new[] { "en", "fa", "de" };

        var answers = await Task.WhenAll(Enumerable.Range(0, 60).Select(async index =>
        {
            var language = languages[index % languages.Length];
            return (language, Seen: await client.GetStringAsync($"/{language}/slow", Stop));
        }));

        Assert.All(answers, answer => Assert.StartsWith($"{answer.language}|", answer.Seen, StringComparison.Ordinal));
        Assert.All(answers, answer => Assert.Contains($"|{answer.language}|/{answer.language}|/slow", answer.Seen, StringComparison.Ordinal));
    }

    // --- Direction ---

    [Theory]
    [InlineData("en", "ltr")]
    [InlineData("de", "ltr")]
    [InlineData("fa", "rtl")]
    [InlineData("ar", "rtl")]
    [InlineData("ckb", "rtl")]
    [InlineData("he", "rtl")]
    [InlineData("ur", "rtl")]
    [InlineData("fa-IR", "rtl")]
    public void The_direction_comes_from_the_language(string language, string expected)
    {
        Assert.Equal(expected, TextDirection.Of(language));
    }

    // --- Theme ---

    [Theory]
    [InlineData("dark", "Dark")]
    [InlineData("light", "Light")]
    public async Task A_visitor_chooses_a_theme_and_comes_back_to_the_page(string theme, string expected)
    {
        var visitor = new Visitor(Client());

        var response = await visitor.PostTheme(theme, "/fa/story/rostam?x=1");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/fa/story/rostam?x=1", response.Headers.Location!.OriginalString);
        Assert.Equal(theme, visitor.Cookies[ThemePreference.CookieName]);
        Assert.EndsWith($"|{expected}", await (await visitor.Get("/preferences/seen")).Content.ReadAsStringAsync(Stop), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Following_the_device_again_forgets_the_choice()
    {
        var visitor = new Visitor(Client());
        await visitor.PostTheme("dark", "/en/");

        await visitor.PostTheme("auto", "/en/");

        Assert.False(visitor.Cookies.ContainsKey(ThemePreference.CookieName));
    }

    [Fact]
    public async Task A_theme_the_site_does_not_know_is_refused()
    {
        var response = await new Visitor(Client()).PostTheme("sepia", "/en/");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_form_without_its_token_changes_nothing()
    {
        var response = await Client().PostAsync(
            "/preferences/theme",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["theme"] = "dark", ["returnUrl"] = "/en/" }),
            Stop);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    [InlineData("evil")]
    [InlineData("")]
    public async Task After_a_form_a_visitor_is_never_sent_to_another_site(string returnUrl)
    {
        var response = await new Visitor(Client()).PostTheme("dark", returnUrl);

        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/fa/", "/fa/")]
    [InlineData("/en/story/x?a=1#top", "/en/story/x?a=1#top")]
    [InlineData(null, "/")]
    [InlineData("/\tx", "/")]
    public void A_return_address_is_a_path_of_this_site(string? address, string expected)
    {
        Assert.Equal(expected, ReturnAddress.Safe(address));
    }

    [Theory]
    [InlineData(ThemeChoice.Auto, null)]
    [InlineData(ThemeChoice.Light, "light")]
    [InlineData(ThemeChoice.Dark, "dark")]
    public void The_page_carries_the_choice_and_nothing_when_the_device_decides(ThemeChoice choice, string? expected)
    {
        Assert.Equal(expected, ThemePreference.Attribute(choice));
    }

    [Fact]
    public async Task The_token_cookie_of_a_form_belongs_to_the_site_and_not_to_one_language()
    {
        // A page in a language is under /fa; its forms post to addresses without a language. A cookie scoped to
        // /fa would not be sent there, and every form of a real browser would be refused.
        var response = await Client().GetAsync("/fa/token", Stop);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.Contains("Antiforgery", StringComparison.Ordinal));
        Assert.Contains("path=/;", cookie, StringComparison.OrdinalIgnoreCase);
    }

    // --- Headers ---

    [Theory]
    [InlineData("/en/")]
    [InlineData("/")]
    [InlineData("/app.css")]
    public async Task Every_answer_tells_the_browser_what_a_page_may_do(string address)
    {
        var response = await Client().GetAsync(address, Stop);

        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", policy, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline", policy, StringComparison.Ordinal);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("camera=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_site_needs_a_language_and_knows_the_ones_it_declares()
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddSiteLanguages());
    }

    private static CancellationToken Stop => TestContext.Current.CancellationToken;

    private HttpClient Client() => _app.GetTestClient();

    /// <summary>A browser as far as these tests need one: it keeps its cookies and follows no redirect.</summary>
    private sealed class Visitor(HttpClient client)
    {
        public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);

        public Task<HttpResponseMessage> Get(string address, string? accept = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, address);
            if (accept is not null)
            {
                request.Headers.TryAddWithoutValidation("Accept-Language", accept);
            }

            return Send(request);
        }

        public async Task<HttpResponseMessage> PostTheme(string theme, string returnUrl)
        {
            var token = await (await Get("/preferences/token")).Content.ReadAsStringAsync(Stop);
            return await Send(new HttpRequestMessage(HttpMethod.Post, "/preferences/theme")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["theme"] = theme,
                    ["returnUrl"] = returnUrl,
                    ["__RequestVerificationToken"] = token.Trim('"'),
                }),
            });
        }

        private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
        {
            if (Cookies.Count > 0)
            {
                request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", Cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
            }

            var response = await client.SendAsync(request, Stop);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    var pair = cookie.Split(';')[0].Split('=', 2);
                    if (pair[1].Length == 0 || cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase))
                    {
                        Cookies.Remove(pair[0]);
                    }
                    else
                    {
                        Cookies[pair[0]] = pair[1];
                    }
                }
            }

            return response;
        }
    }
}
