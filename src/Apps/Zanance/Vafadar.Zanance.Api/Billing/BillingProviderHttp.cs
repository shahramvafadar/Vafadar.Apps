using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Cryptography;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Bounded official-provider HTTP without automatic URL logging, redirection or arbitrary destinations.</summary>
internal sealed class BillingProviderHttp : IDisposable
{
    private readonly HttpClient _client;
    /// <summary>The optional handler is an explicit qualification effect port; normal composition always uses fixed HTTPS transport.</summary>
    public BillingProviderHttp(HttpMessageHandler? handler = null)
        => _client = new(handler ?? new SocketsHttpHandler
        { AllowAutoRedirect = false, ConnectTimeout = TimeSpan.FromSeconds(15), AutomaticDecompression = DecompressionMethods.None })
        { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Uses a fixed provider host and returns only a bounded duplicate-free JSON document.</summary>
    internal async Task<JsonDocument> SendAsync(Uri endpoint, string bearer, bool post, CancellationToken ct)
    {
        if (endpoint.Scheme != "https" || endpoint.Port != 443 || endpoint.UserInfo.Length != 0 || endpoint.Fragment.Length != 0 ||
            endpoint.Host is not ("androidpublisher.googleapis.com" or "api.storekit.apple.com" or "api.storekit-sandbox.apple.com"))
            throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
        try
        {
            using var request = new HttpRequestMessage(post ? HttpMethod.Post : HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            request.Headers.Accept.Add(new("application/json"));
            if (post) request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new BillingRefusal(503, "BillingProviderConfigurationUnavailable");
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                throw new BillingRefusal(422, "StorePurchaseUnavailable");
            if (!response.IsSuccessStatusCode) throw new BillingRefusal(503, "BillingProviderUnavailable");
            if (post && response.Content.Headers.ContentLength is 0) return JsonDocument.Parse("{}");
            if (response.Content.Headers.ContentLength > 512 * 1024) throw new BillingRefusal(503, "StoreResponseRefused");
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var memory = new MemoryStream(); var buffer = new byte[8192];
            try
            {
                int read;
                while ((read = await stream.ReadAsync(buffer, ct)) != 0)
                {
                    if (memory.Length + read > 512 * 1024) throw new BillingRefusal(503, "StoreResponseRefused");
                    await memory.WriteAsync(buffer.AsMemory(0, read), ct);
                }
                if (post && memory.Length == 0) return JsonDocument.Parse("{}");
                return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)),
                    BillingProviderJson.Default.JsonDocument, 512 * 1024);
            }
            finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new BillingRefusal(503, "BillingProviderUnavailable"); }
        catch (Exception error) when (error is HttpRequestException or JsonException or IOException)
        { throw new BillingRefusal(503, "StoreResponseRefused"); }
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();
}

/// <summary>Reflection-free provider JSON; unknown additive provider properties remain harmless.</summary>
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(PublicSigningRecipientIdentity))]
[JsonSerializable(typeof(BillingPurchaseWork))]
[JsonSerializable(typeof(AppleNotificationEnvelope))]
internal partial class BillingProviderJson : JsonSerializerContext;

/// <summary>Bounded provider field readers; missing/unknown authority is refused, never given a default grant.</summary>
internal static class BillingProviderFields
{
    /// <summary>Reads one required bounded original string.</summary>
    internal static string Text(JsonElement row, string name, int maximum = 256)
        => OptionalText(row, name, maximum) ?? throw new BillingRefusal(422, "StoreResponseRefused");
    /// <summary>Reads only actual optional strings, without coercion or diagnostic output.</summary>
    internal static string? OptionalText(JsonElement row, string name, int maximum = 256)
    {
        if (!row.TryGetProperty(name, out var field) || field.ValueKind == JsonValueKind.Null) return null;
        if (field.ValueKind != JsonValueKind.String || field.GetString() is not { Length: > 0 } text || text.Length > maximum || text.Any(char.IsControl))
            throw new BillingRefusal(422, "StoreResponseRefused");
        return text;
    }
    /// <summary>Reads an RFC3339 provider instant independently of device locale.</summary>
    internal static DateTimeOffset Instant(JsonElement row, string name)
    {
        if (!DateTimeOffset.TryParse(Text(row, name, 64), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value))
            throw new BillingRefusal(422, "StoreResponseRefused");
        return value;
    }
    /// <summary>Reads one actual object without silently accepting a scalar.</summary>
    internal static JsonElement Object(JsonElement row, string name)
        => row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : throw new BillingRefusal(422, "StoreResponseRefused");
}
