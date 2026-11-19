using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Tests;

/// <summary>
/// The catcher of mail the tests send to (ADR 0016): the service <c>mail</c> of
/// <c>src/Apps/Dastan/compose.yaml</c> on a developer's machine, a container of the job in CI. It takes every
/// message by SMTP and sends none on; a test reads what arrived for an address of its own. Without a catcher
/// the tests that need one are skipped; where <c>DASTAN_POSTGRES_REQUIRED</c> is set – in CI – they fail
/// instead, as the tests that need the database do.
/// </summary>
internal static class MailCatcher
{
    private static readonly HttpClient Api = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>Gets the name of the machine the catcher takes SMTP on.</summary>
    public static string Host => Smtp.Host;

    /// <summary>Gets the port the catcher takes SMTP on.</summary>
    public static int Port => Smtp.Port;

    private static (string Host, int Port) Smtp
    {
        get
        {
            var told = Environment.GetEnvironmentVariable("DASTAN_SMTP") is { Length: > 0 } smtp ? smtp : "127.0.0.1:53425";
            var colon = told.LastIndexOf(':');
            return (told[..colon], int.Parse(told[(colon + 1)..], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static string Address => Environment.GetEnvironmentVariable("DASTAN_MAIL_API") is { Length: > 0 } api ? api.TrimEnd('/') : "http://127.0.0.1:53480";

    /// <summary>Makes sure the catcher answers; a test without one is skipped, or fails where one is required.</summary>
    public static void Needed()
    {
        try
        {
            using var answer = Api.GetAsync(new Uri($"{Address}/api/v1/info")).GetAwaiter().GetResult();
            answer.EnsureSuccessStatusCode();
        }
        catch (Exception missing) when (missing is HttpRequestException or TaskCanceledException)
        {
            var why = $"No catcher of mail answers at {Address}: start it with \"docker compose -f src/Apps/Dastan/compose.yaml up -d mail\".";
            Assert.SkipWhen(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DASTAN_POSTGRES_REQUIRED")), why);
            Assert.Fail(why);
        }
    }

    /// <summary>Returns a mail address nobody else writes to: what a test sends there is its own.</summary>
    public static string NewAddress(string who = "somebody") => $"{who}-{Guid.NewGuid():N}@dastan.test";

    /// <summary>
    /// Returns the messages the catcher holds for an address, the oldest first, each with its head and both
    /// bodies as the catcher writes them (<c>Subject</c>, <c>MessageID</c>, <c>Text</c>, <c>HTML</c>, <c>To</c>).
    /// It waits a moment for the number that is expected, because a message is caught after it is sent.
    /// </summary>
    public static async Task<List<JsonObject>> For(string address, int expected = 1)
    {
        var found = new List<JsonObject>();
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var list = await Api.GetFromJsonAsync<JsonObject>(new Uri($"{Address}/api/v1/search?query={Uri.EscapeDataString("to:" + address)}"));
            var messages = list!["messages"]!.AsArray();
            if (messages.Count >= expected)
            {
                found.Clear();
                foreach (var message in messages.Reverse())
                {
                    found.Add((await Api.GetFromJsonAsync<JsonObject>(new Uri($"{Address}/api/v1/message/{(string)message!["ID"]!}")))!);
                }

                return found;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return found;
    }
}
