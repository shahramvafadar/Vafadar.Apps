using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncTransport
{
    /// <summary>Reads only provider metadata/wallet under the same original pinned rotating identity session.</summary>
    public Task<AiAvailability> AnalysisAvailabilityAsync(string expectedUser, CancellationToken ct = default) =>
        AnalysisAsync<AiOfferRequest, AiAvailability>(expectedUser, HttpMethod.Get, "analysis/availability", null, null, AiJson.Default.AiAvailability, ct);
    /// <summary>Obtains an original metadata-only cost/destination offer; no financial aggregate is sent.</summary>
    public Task<AiOffer> AnalysisOfferAsync(string expectedUser, AiPurpose purpose, CancellationToken ct = default) =>
        AnalysisAsync(expectedUser, HttpMethod.Post, "analysis/offers", new AiOfferRequest(purpose), AiJson.Default.AiOfferRequest, AiJson.Default.AiOffer, ct);
    /// <summary>Sends only a durably retained explicitly approved aggregate request, never a caller-selected owner.</summary>
    public Task<AiRequestResult> AnalysisSubmitAsync(string expectedUser, AiSubmission submission, CancellationToken ct = default) =>
        AnalysisAsync(expectedUser, HttpMethod.Post, "analysis/requests", submission, AiJson.Default.AiSubmission, AiJson.Default.AiRequestResult, ct);
    /// <summary>Queries the exact retained original id; it never sends a financial payload or starts another model request.</summary>
    public Task<AiRequestResult> AnalysisStatusAsync(string expectedUser, Guid request, CancellationToken ct = default) =>
        AnalysisAsync<AiOfferRequest, AiRequestResult>(expectedUser, HttpMethod.Post, "analysis/requests/" + request.ToString("D") + "/status", null, null, AiJson.Default.AiRequestResult, ct);
    /// <summary>Closes only an original unaccepted intent; accepted requests remain reserved until a definitive provider result.</summary>
    public Task<AiRequestResult> AnalysisCloseAsync(string expectedUser, Guid request, AiCloseRequest original, CancellationToken ct = default) =>
        AnalysisAsync(expectedUser, HttpMethod.Post, "analysis/requests/" + request.ToString("D") + "/close", original, AiJson.Default.AiCloseRequest, AiJson.Default.AiRequestResult, ct);
    private async Task<TResponse> AnalysisAsync<TBody, TResponse>(string expectedUser, HttpMethod method, string path,
        TBody? body, JsonTypeInfo<TBody>? bodyType, JsonTypeInfo<TResponse> responseType, CancellationToken ct) where TBody : class
    {
        if (string.IsNullOrWhiteSpace(expectedUser) || expectedUser.Length > 256) throw new InvalidDataException("Original analysis identity required.");
        await requests.WaitAsync(ct);
        try
        {
            var session = await SessionAsync(ct);
            if (session.UserId != expectedUser) throw new InvalidDataException("Original analysis identity changed.");
            using var content = body is null ? null : JsonContent.Create(body, bodyType!);
            return await SendCoreAsync(method, path, content, responseType, session, ct);
        }
        finally { requests.Release(); }
    }
}
