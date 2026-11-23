using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Tests;

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
    public Browser Visitor(string? acceptLanguage = null) => Browser.Of(this, acceptLanguage);
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
    /// <summary>Gets what the server sent, as bytes: the content of a file.</summary>
    public Task<byte[]> Bytes() => Response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

    /// <summary>
    /// Gets the markup of the page without the one script the site adds to every page – the file that registers the
    /// service worker. What is left must not contain a script.
    /// </summary>
    public string Own => Regex.Replace(Html, "<script src=\"app[^\"]*\\.js\" defer></script>", string.Empty);

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
public sealed class Browser(HttpClient client, string? acceptLanguage, Func<string?>? database = null)
{
    /// <summary>The address the form that makes an account is sent to.</summary>
    public const string Registering = "/preferences/account/register";

    /// <summary>Returns a visitor of a site with a browser of their own: own cookies, no automatic redirects.</summary>
    public static Browser Of(WebApplicationFactory<Program> site, string? acceptLanguage = null)
    {
        ArgumentNullException.ThrowIfNull(site);
        return new(
            site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false }),
            acceptLanguage,
            () => site.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Dastan:Database"]);
    }

    /// <summary>
    /// Returns the last mail of a kind the site wrote to an address, as the catcher of a developer would show
    /// it: its text, or nothing when there is none. The kinds are those of <c>AccountMail</c>.
    /// </summary>
    public string? Mailed(string to, string kind)
    {
        using var connection = new Npgsql.NpgsqlConnection(database?.Invoke() ?? throw new InvalidOperationException("The site of this visitor has no database."));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Text" from delivery."Mail" where lower("To") = lower(@to) and "Kind" = @kind order by "CreatedAt" desc, "Key" desc limit 1""";
        command.Parameters.AddWithValue("to", to);
        command.Parameters.AddWithValue("kind", kind);
        return command.ExecuteScalar() as string;
    }

    /// <summary>Returns the link of a mail: the page it opens, with the language, and the identifier and the token it carries.</summary>
    public static (string Page, string Id, string Token) LinkOf(string mail)
    {
        var link = Regex.Match(mail, @"https?://[^/\s]+(?<page>/(?:en|fa)/account/\w+)\?id=(?<id>[^&\s]+)&token=(?<token>\S+)");
        Assert.True(link.Success, mail);
        return (link.Groups["page"].Value, link.Groups["id"].Value, link.Groups["token"].Value);
    }

    /// <summary>Opens the link of a mail and sends the form of the page it leads to, with more fields when the form has some.</summary>
    public Task<Page> Follow(string mail, string action, params (string Name, string Value)[] fields)
    {
        var (page, id, token) = LinkOf(mail);
        return Post($"/preferences/account/{action}", $"{page}?id={id}&token={token}", [("id", id), ("token", token), ("returnUrl", page[..page.LastIndexOf('/')]), .. fields]);
    }

    private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);

    // A visitor may ask for several pages at the same time; their cookies are kept and sent one request at a time.
    private readonly Lock _jar = new();

    /// <summary>Gets or sets the page the visitor says a form stood on, when it is another than the one the token was taken from.</summary>
    public string? CameFrom { get; set; }

    /// <summary>Gets the cookies of this visitor.</summary>
    public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);

    /// <summary>Opens an address without following a redirect.</summary>
    public Task<Page> Get(string address) => Send(new HttpRequestMessage(HttpMethod.Get, address));

    /// <summary>Asks for a part of a file, as a player does for a video.</summary>
    public Task<Page> GetRange(string address, string range)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.TryAddWithoutValidation("Range", range);
        return Send(request);
    }

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

    /// <summary>
    /// Sends a form of the site, with the token of the page it stands on. The form that makes an account is
    /// sent as a person would see it through when the test names no mail address – a test that is not about
    /// the address: the visitor gives the made-up address of the name (<see cref="TestAccounts.MailOf"/>),
    /// opens the link that was mailed to it and confirms, and the answer is the one of that last step. A test
    /// that names an address does every step itself.
    /// </summary>
    public async Task<Page> Post(string address, string from, params (string Name, string Value)[] fields)
    {
        if (address == Registering && !fields.Any(field => field.Name == "mail"))
        {
            var to = TestAccounts.MailOf(fields.FirstOrDefault(field => field.Name == "name").Value ?? string.Empty);
            var asked = await Post(address, from, [.. fields, ("mail", to)]);
            return asked.Response.Headers.Location?.OriginalString.Contains("said=confirm-sent", StringComparison.Ordinal) == true && Mailed(to, "confirm-address") is { } mail
                ? await Follow(mail, "confirm")
                : asked;
        }

        var form = await Open(from);
        var token = Regex.Match(form.Html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
        var request = new HttpRequestMessage(HttpMethod.Post, address)
        {
            Content = new FormUrlEncodedContent(
                fields.Select(field => KeyValuePair.Create(field.Name, field.Value)).Append(KeyValuePair.Create("__RequestVerificationToken", token))),
        };

        // Like a browser, the visitor says which page the form stood on.
        request.Headers.Referrer = new Uri(new Uri("http://localhost"), CameFrom ?? from);
        return await Send(request);
    }

    /// <summary>Sends a form of the site that carries a file, with the token of the page it stands on.</summary>
    public async Task<Page> Upload(string address, string from, string fileName, byte[] file, params (string Name, string Value)[] fields)
    {
        var form = await Open(from);
        var token = Regex.Match(form.Html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
        var content = new MultipartFormDataContent();
        foreach (var (name, value) in fields.Append(("__RequestVerificationToken", token)))
        {
            content.Add(new StringContent(value, System.Text.Encoding.UTF8), name);
        }

        var sent = new ByteArrayContent(file);
        sent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(sent, "file", fileName);
        return await Send(new HttpRequestMessage(HttpMethod.Post, address) { Content = content });
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
        if (CameFrom is not null && request.Headers.Referrer is null)
        {
            request.Headers.Referrer = new Uri(new Uri("http://localhost"), CameFrom);
        }

        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        // Like a browser, the visitor sends a cookie only to the part of the site the cookie was set for.
        var address = request.RequestUri!.OriginalString;
        List<KeyValuePair<string, string>> sent;
        lock (_jar)
        {
            sent = [.. Cookies.Where(cookie => address.StartsWith(_paths[cookie.Key], StringComparison.Ordinal))];
        }

        if (sent.Count > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", sent.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            lock (_jar)
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
                        _paths[pair[0]] = Regex.Match(cookie, "path=([^;]+)", RegexOptions.IgnoreCase) is { Success: true } path ? path.Groups[1].Value : "/";
                        Cookies[pair[0]] = pair[1];
                    }
                }
            }
        }

        return new Page(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), response);
    }
}

/// <summary>
/// The hour of the clock, for the tests of what the site counts in an hour. The site counts by the hour of the
/// clock; a test that begins a moment before the hour turns would count its first requests in one hour and its
/// last in the next, and find the limit not reached (seen on the build server on 2026-11-23). Such a test waits
/// for the hour to turn first – at most a minute and a half, about once in forty runs.
/// </summary>
public static class TestHour
{
    /// <summary>Waits, when the hour is about to turn, until it has.</summary>
    public static async Task NotAboutToTurn()
    {
        var now = DateTimeOffset.UtcNow;
        var left = TimeSpan.FromHours(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerHour);
        if (left < TimeSpan.FromSeconds(90))
        {
            await Task.Delay(left + TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        }
    }
}
