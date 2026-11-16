using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The website as it runs for a local demonstration (the Development settings of the project), started in memory.
/// </summary>
public class DemoSite : WebApplicationFactory<Program>
{
    private readonly Lazy<Catalogue> _catalogue;

    public DemoSite()
    {
        _catalogue = new Lazy<Catalogue>(() => PilotSample.Load(
            Path.Combine(Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "..", "content", "samples", "shahnameh-pilot.json")));
    }

    /// <summary>Gets the collection the site shows, read by the tests themselves to know what must stay hidden.</summary>
    public Catalogue Catalogue => _catalogue.Value;

    /// <summary>Returns a visitor with a browser of their own: own cookies, no automatic redirects.</summary>
    public Browser Visitor(string? acceptLanguage = null) =>
        new(CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false }), acceptLanguage);
}

/// <summary>The website as it runs for the public: the same collection, but not as a demonstration.</summary>
public sealed class PublicSite : DemoSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Dastan:Content", "../content");
    }
}

/// <summary>A page as the server answered it.</summary>
public sealed record Page(HttpStatusCode Status, string Html, HttpResponseMessage Response)
{
    /// <summary>Gets the text of the page as a visitor reads it: without markup, entities decoded.</summary>
    public string Text => WebUtility.HtmlDecode(Regex.Replace(Html, "<[^>]+>", " "));

    /// <summary>Gets the content of the <c>main</c> element.</summary>
    public string Main => WebUtility.HtmlDecode(Regex.Match(Html, "<main[^>]*>(.*)</main>", RegexOptions.Singleline).Groups[1].Value);

    /// <summary>Returns the value of an attribute of the root element.</summary>
    public string? Root(string attribute) =>
        Regex.Match(Html, $"<html[^>]*\\s{attribute}=\"([^\"]*)\"") is { Success: true } match ? match.Groups[1].Value : null;

    /// <summary>Gets the addresses the page links to, as written.</summary>
    public IEnumerable<string> Links => Regex.Matches(Html, "<a\\s[^>]*href=\"([^\"]*)\"").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value));
}

/// <summary>
/// One visitor's browser as far as the tests need it: it keeps its own cookies, sends its language and fills in
/// the forms of the site with the token the page gave it.
/// </summary>
public sealed class Browser(HttpClient client, string? acceptLanguage)
{
    private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);

    /// <summary>Gets the cookies of this visitor.</summary>
    public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);

    /// <summary>Opens an address without following a redirect.</summary>
    public Task<Page> Get(string address) => Send(new HttpRequestMessage(HttpMethod.Get, address));

    /// <summary>Opens an address and follows redirects of the site.</summary>
    public async Task<Page> Open(string address)
    {
        var page = await Get(address);
        for (var hops = 0; hops < 5 && page.Response.Headers.Location is { } next; hops++)
        {
            page = await Get(next.OriginalString);
        }

        return page;
    }

    /// <summary>Sends a form of the site, with the token of the page it stands on.</summary>
    public async Task<Page> Post(string address, string from, params (string Name, string Value)[] fields)
    {
        var form = await Open(from);
        var token = Regex.Match(form.Html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
        return await Send(new HttpRequestMessage(HttpMethod.Post, address)
        {
            Content = new FormUrlEncodedContent(
                fields.Select(field => KeyValuePair.Create(field.Name, field.Value)).Append(KeyValuePair.Create("__RequestVerificationToken", token))),
        });
    }

    /// <summary>Uses the reading control of the collection.</summary>
    public Task<Page> ReadTo(int level, string language = "en", string collection = "story-rostam-sohrab") => Post(
        "/preferences/reading",
        $"/{language}/{collection}",
        ("collection", collection),
        ("level", level.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ("returnUrl", $"/{language}/{collection}#reading"));

    private async Task<Page> Send(HttpRequestMessage request)
    {
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        // Like a browser, the visitor sends a cookie only to the part of the site the cookie was set for.
        var address = request.RequestUri!.OriginalString;
        var sent = Cookies.Where(cookie => address.StartsWith(_paths[cookie.Key], StringComparison.Ordinal)).ToList();
        if (sent.Count > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", sent.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
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
                    _paths[pair[0]] = Regex.Match(cookie, "path=([^;]+)", RegexOptions.IgnoreCase) is { Success: true } path ? path.Groups[1].Value : "/";
                }
            }
        }

        return new Page(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), response);
    }
}
