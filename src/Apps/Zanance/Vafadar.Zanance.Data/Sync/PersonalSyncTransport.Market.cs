using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncTransport
{
    /// <summary>Explicit fixed-origin source metadata only; no financial request or automatic sign-in.</summary>
    public Task<MarketAvailability> MarketAvailabilityAsync(string expectedUser, CancellationToken ct = default) =>
        MarketAsync<MarketRequest, MarketAvailability>(expectedUser, HttpMethod.Get, "market/availability", null, null, MarketJson.Default.MarketAvailability, ct);
    /// <summary>One original approved public symbol/date lookup; no account/holding name, quantity, URL or credential parameter.</summary>
    public Task<MarketQuote> MarketQuoteAsync(string expectedUser, MarketRequest request, CancellationToken ct = default) =>
        MarketAsync(expectedUser, HttpMethod.Post, "market/quote", request, MarketJson.Default.MarketRequest, MarketJson.Default.MarketQuote, ct);
    private async Task<TResponse> MarketAsync<TBody, TResponse>(string expectedUser, HttpMethod method, string path,
        TBody? body, JsonTypeInfo<TBody>? bodyType, JsonTypeInfo<TResponse> responseType, CancellationToken ct) where TBody : class
    {
        if (string.IsNullOrWhiteSpace(expectedUser) || expectedUser.Length > 256) throw new InvalidDataException("Original market identity required.");
        await requests.WaitAsync(ct);
        try
        {
            var session = await SessionAsync(ct);
            if (session.UserId != expectedUser) throw new InvalidDataException("Original market identity changed.");
            using var content = body is null ? null : JsonContent.Create(body, bodyType!);
            return await SendCoreAsync(method, path, content, responseType, session, ct);
        }
        finally { requests.Release(); }
    }
}
