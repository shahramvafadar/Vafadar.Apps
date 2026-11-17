using System.Globalization;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The answer to "is the site alive?" (docs/operations.md): an address for whoever runs the site – a container,
/// a proxy, a monitor – and the question itself, for a container that has no tool to ask with. The answer says
/// that the process answers and that the collection was read, which the site does before it answers anything;
/// it says nothing about the collection and nothing about a visitor.
/// </summary>
public static class SiteHealth
{
    /// <summary>The address of the answer. It has no language: nobody reads it.</summary>
    public const string Address = "/health";

    /// <summary>The answer of a site that is alive.</summary>
    public const string Alive = "ok";

    /// <summary>Adds the address to the site.</summary>
    public static IEndpointRouteBuilder MapSiteHealth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet(Address, (HttpContext context) =>
        {
            // Asked again every time, and never a page of the site for a search engine.
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            return Results.Text(Alive, "text/plain");
        });
        return endpoints;
    }

    /// <summary>
    /// Asks the site of this machine whether it is alive: 0 when it answers as a site that is, 1 otherwise. The
    /// port is the one the site was told to listen on (<c>ASPNETCORE_HTTP_PORTS</c>), 8080 when nothing was said.
    /// </summary>
    public static async Task<int> Probe()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        var address = new Uri(string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port ?? "8080"}{Address}"));
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var answer = await client.GetStringAsync(address).ConfigureAwait(false);
            return answer == Alive ? 0 : 1;
        }
        catch (Exception refused) when (refused is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
