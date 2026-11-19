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
    internal const string Client = "ZananceAnalysisPilot";
    internal static readonly AiPayload ReportFixture = new(1, AiPurpose.PeriodExplanation, "en",
        new(new(2026, 11, 1), new(2026, 11, 30), 1, true, [new("EUR", 125000, 0, 41000, 3000)], 0), null);
    internal static readonly AiPayload ComparisonFixture = new(1, AiPurpose.PeriodComparison, "en",
        new(new(2026, 11, 1), new(2026, 11, 30), 2, true, [new("EUR", 125000, 5000, 41000, 3000), new("USD", 60000, 0, 17000, 2000)], 0),
        new(new(2026, 10, 1), new(2026, 10, 31), 2, true, [new("EUR", 119000, 0, 45000, 1000), new("USD", 59000, 1000, 12000, 2000)], 0));
    private static readonly string ReportDigest = AiProtocol.Digest(ReportFixture);
    private static readonly string ComparisonDigest = AiProtocol.Digest(ComparisonFixture);
    private static readonly AiProviderOutcome Unknown = new(AiProviderState.Unknown);

    /// <summary>Accepts only the fixed synthetic payload for the requested purpose.</summary>
    internal static bool IsSynthetic(AiPurpose purpose, string digest) => purpose switch
    {
        AiPurpose.PeriodExplanation => digest == ReportDigest,
        AiPurpose.PeriodComparison => digest == ComparisonDigest,
        _ => false,
    };

    /// <summary>Lists fixed model choices for each independent operator setting.</summary>
    internal static string[] Models(AiPurpose purpose) => purpose switch
    {
        AiPurpose.PeriodExplanation => ["gpt-6-luna", "gemini-3.5-flash-lite"],
        AiPurpose.PeriodComparison => ["gpt-6.2-sol", "gemini-4-hydrogen"],
        _ => [],
    };

    /// <summary>Serializes switches with quote admission and the global pilot quota across service instances.</summary>
    internal static Task LockAsync(ServiceDbContext db, CancellationToken ct)
        => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1854911612, 1346978644)", ct);

    /// <summary>Preserves the original operator command for the legacy report profile.</summary>
    internal Task SelectAsync(ServiceDbContext db, string provider, string actor, CancellationToken ct)
        => SaveSelectionAsync(db, AiPurpose.PeriodExplanation, provider, null, actor, ct);

    /// <summary>Selects an allowlisted model or disables just the selected purpose.</summary>
    internal Task SelectModelAsync(ServiceDbContext db, AiPurpose purpose, string model, string actor, CancellationToken ct)
    {
        var models = Models(purpose);
        if (models.Length == 0 || (model != "off" && !models.Contains(model, StringComparer.Ordinal)))
            throw new InvalidOperationException("Analysis pilot model is not allowed for this purpose.");
        return SaveSelectionAsync(db, purpose, model == "off" ? "off" : ModelProvider(model),
            model == "off" ? null : model, actor, ct);
    }

    private async Task SaveSelectionAsync(ServiceDbContext db, AiPurpose purpose, string provider, string? model, string actor, CancellationToken ct)
    {
        if (actor.Length is < 1 or > 256) throw new InvalidOperationException("Invalid analysis pilot operator.");
        var profile = Profile(purpose, provider, model);
        if (provider != "off" && profile is null) throw new InvalidOperationException("Analysis pilot provider is unavailable.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        db.Add(new AiPilotRouteRow { Purpose = purpose, Provider = provider, Model = model, Revision = profile?.Revision ?? "",
            Actor = actor, SelectedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Reads the current default and quote terms without egress.</summary>
    internal async Task<(AiProviderOffer Offer, long RouteId)?> OfferAsync(ServiceDbContext db, AiPurpose purpose, CancellationToken ct)
    {
        var selected = await db.Set<AiPilotRouteRow>().Where(r => r.Purpose == purpose).OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct);
        var profile = selected is null ? null : Profile(purpose, selected.Provider, selected.Model);
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
        var comparison = offer.Purpose == AiPurpose.PeriodComparison;
        var task = comparison
            ? "Synthetic Zanance test. Compare recorded October and November results, all amounts in minor units. EUR October: income 119000, income reversals 0, expenses 45000, refunds 1000. EUR November: income 125000, income reversals 5000, expenses 41000, refunds 3000. USD October: income 59000, income reversals 1000, expenses 12000, refunds 2000. USD November: income 60000, income reversals 0, expenses 17000, refunds 2000. Net spending is expenses minus refunds. Net cash flow is income minus income reversals minus net spending. Give November minus October changes in net spending and net cash flow separately by currency. No exchange rate is supplied; set can_combine to whether one converted total can be computed. Explain the changes briefly in English without advice."
            : "Synthetic Zanance test. November recorded totals in EUR minor units: income 125000, expenses 41000, refunds 3000. Give net_expenses_minor = expenses minus refunds, and a one-sentence explanation in English. No other information.";
        var expected = Expected(offer.Purpose);
        var properties = expected.ToDictionary(f => f.Key, f => new { type = f.Value is bool ? "boolean" : "integer" });
        properties.Add("explanation", new { type = "string" });
        var schema = new
        {
            type = "object",
            properties, required = properties.Keys.ToArray(), additionalProperties = false
        };
        object body = profile.Provider == "openai"
            ? new { model = profile.Model, input = task, max_output_tokens = profile.OutputLimit, background = true, store = true,
                service_tier = "default", reasoning = new { effort = profile.Reasoning }, metadata = new { zanance_operation = operation },
                text = new { format = new { type = "json_schema", name = "zanance_pilot", strict = true, schema } } }
            : new { model = profile.Model, input = task, generation_config = new { max_output_tokens = profile.OutputLimit, thinking_level = profile.Reasoning },
                background = true, store = true, response_format = new { type = "text", mime_type = "application/json", schema } };
        using var request = Request(profile, HttpMethod.Post, profile.Url);
        request.Content = JsonContent.Create(body);
        if (profile.Provider == "openai") request.Headers.Add("X-Client-Request-Id", operation);
        using var result = await SendAsync(request, ct);
        var id = result is null ? null : Text(result.RootElement, "id");
        if (!ValidId(id)) return Unknown;
        await saveId(id!);
        return Outcome(profile, offer.Purpose, result!.RootElement);
    }

    /// <summary>Reads only the saved original execution and its matching vendor project.</summary>
    internal async Task<AiProviderOutcome> StatusAsync(AiPilotRouteRow route, AiOffer offer, string? id, CancellationToken ct)
    {
        if (!ValidId(id) || Admit(route, offer) is not { } profile) return Unknown;
        using var request = Request(profile, HttpMethod.Get, profile.Url + "/" + id);
        using var result = await SendAsync(request, ct);
        return result is not null && Text(result.RootElement, "id") == id ? Outcome(profile, offer.Purpose, result.RootElement) : Unknown;
    }

    private async Task<JsonDocument?> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using var client = clients.CreateClient(Client);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        try { return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct); }
        catch (JsonException) { return null; }
    }

    private static AiProviderOutcome Outcome(PilotProfile profile, AiPurpose purpose, JsonElement response)
    {
        if (Text(response, "status") != "completed" || Text(response, "model") != profile.Model) return Unknown;
        var usage = Read(response, "usage");
        var openAi = profile.Provider == "openai";
        var input = Number(usage, openAi ? "input_tokens" : "total_input_tokens");
        var output = Number(usage, openAi ? "output_tokens" : "total_output_tokens");
        var thought = openAi ? 0 : Number(usage, "total_thought_tokens") ?? 0;
        if (input is null or < 1 or > 2048 || output is null or < 1 || output > profile.OutputLimit ||
            thought < 0 || thought > profile.OutputLimit || output + thought > profile.OutputLimit) return Unknown;
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
            var expected = Expected(purpose);
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != expected.Count + 1 ||
                !expected.All(pair => pair.Value is int number ? Number(root, pair.Key) == number
                    : Read(root, pair.Key).ValueKind == JsonValueKind.False) ||
                Text(root, "explanation") is not { } explanation) return Unknown;
            AiProtocol.ValidateText(explanation);
            return new(AiProviderState.Completed, 1, explanation, input, output + thought);
        }
        catch (Exception error) when (error is JsonException or ArgumentException) { return Unknown; }
    }

    private PilotProfile? Admit(AiPilotRouteRow route, AiOffer offer)
    {
        var profile = Profile(route.Purpose, route.Provider, route.Model);
        return profile is not null && profile.Revision == route.Revision && profile.Provider == offer.Provider
            && profile.Model == offer.Model && offer.Purpose == route.Purpose ? profile : null;
    }

    private static Dictionary<string, object> Expected(AiPurpose purpose) => purpose == AiPurpose.PeriodComparison
        ? new() { ["eur_spending_change"] = -6000, ["usd_spending_change"] = 5000,
            ["eur_cash_flow_change"] = 7000, ["usd_cash_flow_change"] = -3000, ["can_combine"] = false }
        : new() { ["net_expenses_minor"] = 38000 };

    private PilotProfile? Profile(AiPurpose purpose, string provider, string? selectedModel)
    {
        if (provider is not ("openai" or "gemini")) return null;
        if (selectedModel is null ? purpose != AiPurpose.PeriodExplanation
            : !Models(purpose).Contains(selectedModel, StringComparer.Ordinal) || ModelProvider(selectedModel) != provider) return null;
        var settings = configuration.GetSection("Analysis:ProviderPilot");
        var key = settings[$"{provider}:ApiKey"];
        var project = settings[$"{provider}:Project"];
        if (string.IsNullOrWhiteSpace(key) || !ValidId(project)) return null;
        var openAi = provider == "openai";
        var comparison = purpose == AiPurpose.PeriodComparison;
        var model = selectedModel ?? (openAi ? "gpt-6.2-sol" : "gemini-4-hydrogen");
        var reasoning = comparison ? "medium" : "low";
        // Hydrogen bills thinking against the same limit. The 1024-token trial exhausted it before emitting JSON.
        var outputLimit = comparison ? (openAi ? 1024 : 4096) : 512;
        var sendHeader = settings.GetValue("openai:SendProjectHeader", true);
        var revisionInput = selectedModel is null && (!openAi || sendHeader)
            ? $"zanance-pilot-v1|{provider}|{model}|{project}"
            : $"zanance-pilot-v2|{purpose}|{provider}|{model}|{project}|{reasoning}|{outputLimit}|{openAi && sendHeader}";
        var revision = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(revisionInput)));
        return new(provider, model, revision, openAi && sendHeader ? project : null, key, reasoning, outputLimit, openAi
            ? "https://api.openai.com/v1/responses" : "https://generativelanguage.googleapis.com/v1beta/interactions");
    }

    private static string ModelProvider(string model) => model.StartsWith("gpt-", StringComparison.Ordinal) ? "openai" : "gemini";

    private static HttpRequestMessage Request(PilotProfile profile, HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (profile.Provider == "openai")
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.Key);
            if (profile.ProjectHeader is { } project) request.Headers.Add("OpenAI-Project", project);
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
    private sealed record PilotProfile(string Provider, string Model, string Revision, string? ProjectHeader, string Key,
        string Reasoning, int OutputLimit, string Url);
}
