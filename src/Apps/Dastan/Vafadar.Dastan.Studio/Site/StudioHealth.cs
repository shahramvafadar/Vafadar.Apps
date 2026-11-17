using System.Globalization;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// The answer to "is the tool alive?" (docs/operations.md), as the site has one: an address for whoever runs the
/// tool, and the question itself for a container that has no tool to ask with. The answer says that the process
/// answers and that the store was opened, which the tool does before it answers anything; it says nothing of the
/// store and needs no sign-in.
/// </summary>
public static class StudioHealth
{
    /// <summary>The address of the answer. It has no language: nobody reads it.</summary>
    public const string Address = "/health";

    /// <summary>The answer of a tool that is alive.</summary>
    public const string Alive = "ok";

    /// <summary>Adds the address to the tool.</summary>
    public static IEndpointRouteBuilder MapStudioHealth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet(Address, () => Results.Text(Alive, "text/plain"));
        return endpoints;
    }

    /// <summary>
    /// Asks the tool of this machine whether it is alive: 0 when it answers as a tool that is, 1 otherwise. The
    /// port is the one the tool was told to listen on (<c>ASPNETCORE_HTTP_PORTS</c>), 8080 when nothing was said.
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
