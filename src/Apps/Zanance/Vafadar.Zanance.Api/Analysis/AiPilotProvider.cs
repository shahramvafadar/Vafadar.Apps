using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Routes the isolated synthetic pilot through an operator-selected, durable provider revision.</summary>
internal sealed class AiPilotProvider(IConfiguration configuration, IHttpClientFactory clients, TimeProvider time)
{
    private const int MaxAttemptsPerProvider = 20;
    private const int MaxOutputTokens = 512;
    internal const string Client = "ZananceAnalysisPilot";
    private static readonly AiPayload Fixture = new(1, AiPurpose.PeriodExplanation, "en",
        new(new(2026, 11, 1), new(2026, 11, 30), 1, true, [new("EUR", 125000, 0, 41000, 3000)], 0), null);
    private static readonly string FixtureDigest = AiProtocol.Digest(Fixture);
    private static readonly AiProviderOutcome Unknown = new(AiProviderState.Unknown);

    /// <summary>The only accepted user payload is the original fixed synthetic fixture.</summary>
    internal static bool IsSynthetic(string digest) => digest == FixtureDigest;

    /// <summary>Serializes switches with quote admission and the global pilot quota across service instances.</summary>
    internal static Task LockAsync(ServiceDbContext db, CancellationToken ct)
        => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1854911612, 1346978644)", ct);

    /// <summary>Appends a switch audit record without making a vendor call.</summary>
    internal async Task SelectAsync(ServiceDbContext db, string provider, string actor, CancellationToken ct)
    {
        if (actor.Length is < 1 or > 256) throw new InvalidOperationException("Invalid analysis pilot operator.");
        var profile = Profile(provider);
        if (provider != "off" && profile is null) throw new InvalidOperationException("Analysis pilot provider is unavailable.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        db.Add(new AiPilotRouteRow { Provider = provider, Revision = profile?.Revision ?? "",
            Actor = actor, SelectedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Reads the current default and quote terms without egress.</summary>
    internal async Task<(AiProviderOffer Offer, long RouteId)?> OfferAsync(ServiceDbContext db, AiPurpose purpose, CancellationToken ct)
    {
        if (purpose != Fixture.Purpose) return null;
        var selected = await db.Set<AiPilotRouteRow>().OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct);
        var profile = Profile(selected?.Provider);
        return profile is not null && selected!.Revision == profile.Revision
            ? (new(profile.Provider, profile.Model, 1), selected.Id) : null;
    }

    /// <summary>Limits the fixed request and output to twenty accepted attempts per provider per pilot store.</summary>
    internal Task<bool> AtCapacityAsync(ServiceDbContext db, string provider, CancellationToken ct)
        => db.Set<AiRequestRow>().Where(r => r.State != AiRequestState.NotAccepted)
            .Join(db.Set<AiOfferRow>().Where(o => o.PilotRouteId != null && o.Provider == provider),
                r => r.OfferId, o => o.Id, (r, o) => r.Id)
            .OrderBy(id => id).Skip(MaxAttemptsPerProvider - 1).AnyAsync(ct);

    /// <summary>Submits the admitted synthetic operation once and saves the vendor ID before attempting settlement.</summary>
    internal async Task<AiProviderOutcome> BeginAsync(AiPilotRouteRow route, AiOffer offer, string operation,
        Func<string, Task> saveId, CancellationToken ct)
    {
        if (Admit(route, offer) is not { } profile) return Unknown;
        const string task = "Synthetic Zanance test. November recorded totals in EUR minor units: income 125000, expenses 41000, refunds 3000. Return JSON with net_expenses_minor = expenses minus refunds, and a one-sentence explanation in English. No other information.";
        var schema = new
        {
            type = "object",
            properties = new { net_expenses_minor = new { type = "integer" }, explanation = new { type = "string" } },
            required = new[] { "net_expenses_minor", "explanation" }, additionalProperties = false
        };
        object body = profile.Provider == "openai"
            ? new { model = profile.Model, input = task, max_output_tokens = MaxOutputTokens, background = true, store = true,
                service_tier = "default", reasoning = new { effort = "low" }, metadata = new { zanance_operation = operation },
                text = new { format = new { type = "json_schema", name = "zanance_pilot", strict = true, schema } } }
            : new { model = profile.Model, input = task, generation_config = new { max_output_tokens = MaxOutputTokens, thinking_level = "low" },
                background = true, store = true, response_format = new { type = "text", mime_type = "application/json", schema } };
        using var request = Request(profile, HttpMethod.Post, profile.Url);
        request.Content = JsonContent.Create(body);
        if (profile.Provider == "openai") request.Headers.Add("X-Client-Request-Id", operation);
        using var result = await SendAsync(request, ct);
        var id = result is null ? null : Text(result.RootElement, "id");
        if (!ValidId(id)) return Unknown;
        await saveId(id!);
        return Outcome(profile, result!.RootElement);
    }

    /// <summary>Reads only the saved original execution and its matching vendor project.</summary>
    internal async Task<AiProviderOutcome> StatusAsync(AiPilotRouteRow route, AiOffer offer, string? id, CancellationToken ct)
    {
        if (!ValidId(id) || Admit(route, offer) is not { } profile) return Unknown;
        using var request = Request(profile, HttpMethod.Get, profile.Url + "/" + id);
        using var result = await SendAsync(request, ct);
        return result is not null && Text(result.RootElement, "id") == id ? Outcome(profile, result.RootElement) : Unknown;
    }

    private async Task<JsonDocument?> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using var client = clients.CreateClient(Client);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        try { return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct); }
        catch (JsonException) { return null; }
    }

    private static AiProviderOutcome Outcome(PilotProfile profile, JsonElement response)
    {
        if (Text(response, "status") != "completed" || Text(response, "model") != profile.Model) return Unknown;
        var usage = Read(response, "usage");
        var openAi = profile.Provider == "openai";
        var input = Number(usage, openAi ? "input_tokens" : "total_input_tokens");
        var output = Number(usage, openAi ? "output_tokens" : "total_output_tokens");
        var thought = openAi ? 0 : Number(usage, "total_thought_tokens") ?? 0;
        if (input is null or < 1 or > 2048 || output is null or < 1 or > MaxOutputTokens ||
            thought is < 0 or > MaxOutputTokens || output + thought > MaxOutputTokens) return Unknown;
        var values = Array(response, openAi ? "output" : "steps")
            .Where(item => Text(item, "type") == (openAi ? "message" : "model_output"))
            .SelectMany(item => Array(item, "content"))
            .Where(item => Text(item, "type") == (openAi ? "output_text" : "text"))
            .Select(item => Text(item, "text")).ToArray();
        if (values.Length != 1 || values[0] is not { } value) return Unknown;
        try
        {
            using var parsed = JsonDocument.Parse(value);
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                Number(root, "net_expenses_minor") != 38000 || Text(root, "explanation") is not { } explanation) return Unknown;
            AiProtocol.ValidateText(explanation);
            return new(AiProviderState.Completed, 1, explanation, input, output + thought);
        }
        catch (Exception error) when (error is JsonException or ArgumentException) { return Unknown; }
    }

    private PilotProfile? Admit(AiPilotRouteRow route, AiOffer offer)
    {
        var profile = Profile(route.Provider);
        return profile is not null && profile.Revision == route.Revision && profile.Provider == offer.Provider
            && profile.Model == offer.Model && offer.Purpose == Fixture.Purpose ? profile : null;
    }

    private PilotProfile? Profile(string? provider)
    {
        if (provider is not ("openai" or "gemini")) return null;
        var settings = configuration.GetSection("Analysis:ProviderPilot");
        var key = settings[$"{provider}:ApiKey"];
        var project = settings[$"{provider}:Project"];
        if (string.IsNullOrWhiteSpace(key) || !ValidId(project)) return null;
        var openAi = provider == "openai";
        var model = openAi ? "gpt-6.2-sol" : "gemini-4-hydrogen";
        var revision = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"zanance-pilot-v1|{provider}|{model}|{project}")));
        return new(provider, model, revision, project!, key, openAi
            ? "https://api.openai.com/v1/responses" : "https://generativelanguage.googleapis.com/v1beta/interactions");
    }

    private static HttpRequestMessage Request(PilotProfile profile, HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (profile.Provider == "openai")
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.Key);
            request.Headers.Add("OpenAI-Project", profile.Project);
        }
        else
        {
            request.Headers.Add("x-goog-api-key", profile.Key);
            request.Headers.Add("Api-Revision", "2026-05-20");
        }
        return request;
    }

    private static bool ValidId(string? value) => value is { Length: > 0 and <= 250 }
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    private static JsonElement Read(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(name, out var found) ? found : default;
    private static string? Text(JsonElement value, string name) => Read(value, name) is { ValueKind: JsonValueKind.String } found
        ? found.GetString() : null;
    private static int? Number(JsonElement value, string name) => Read(value, name) is { ValueKind: JsonValueKind.Number } found
        && found.TryGetInt32(out var number) ? number : null;
    private static IEnumerable<JsonElement> Array(JsonElement value, string name) =>
        Read(value, name) is { ValueKind: JsonValueKind.Array } found ? found.EnumerateArray() : [];
    private sealed record PilotProfile(string Provider, string Model, string Revision, string Project, string Key, string Url);
}
