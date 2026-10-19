using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Explicit approved-source selection, independent from fictitious qualification, public hosting and paid entitlements.</summary>
internal static class MarketModule
{
    /// <summary>Registers the unavailable default and bounded original lookup/cache coordinator.</summary>
    internal static void Add(IServiceCollection services, string? source = null)
    {
        if (source is not (null or "" or "ecb")) throw new InvalidOperationException("Unapproved market source configuration.");
        if (source == "ecb") services.AddSingleton<IMarketProvider>(provider =>
            new EcbMarketProvider(EcbMarketProvider.CreateClient(), provider.GetRequiredService<TimeProvider>()));
        else services.AddSingleton<IMarketProvider, UnconfiguredMarketProvider>();
        services.AddSingleton<MarketGateway>();
    }
    /// <summary>Maps authenticated public-symbol/date lookup only; no endpoint receives an account, quantity or arbitrary URL.</summary>
    internal static void Map(WebApplication app)
    {
        var storage = app.Services.GetRequiredService<ServiceStorage>();
        if (!storage.LocalMarketQualification && !storage.ApprovedEcbReference) return;
        var group = app.MapGroup("/market").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.Request.IsHttps) return Results.StatusCode(426);
            try { return await next(context); }
            catch (MarketRefusal refusal) { return Results.Json(new { Code = refusal.Message }, statusCode: refusal.Status); }
            catch (Exception error) when (error is ArgumentException or JsonException or OverflowException)
            { return Results.Json(new { Code = "MarketRequestRefused" }, statusCode: 422); }
        });
        group.MapGet("/availability", (MarketGateway gateway) => Results.Json(gateway.Availability(), MarketJson.Default.MarketAvailability));
        group.MapPost("/quote", async (HttpRequest request, MarketGateway gateway, CancellationToken ct) =>
        {
            var original = await ReadAsync(request, MarketJson.Default.MarketRequest, ct);
            return Results.Json(await gateway.FetchAsync(original, ct), MarketJson.Default.MarketQuote);
        });
    }
    private static async Task<T> ReadAsync<T>(HttpRequest request, JsonTypeInfo<T> type, CancellationToken ct) where T : class
    {
        if (request.ContentLength > 4096) throw new MarketRefusal(413, "MarketRequestBound");
        if (request.ContentType is not { } content || !content.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            throw new MarketRefusal(415, "MarketJsonRequired");
        using var memory = new MemoryStream(); var buffer = new byte[1024];
        try
        {
            int count; while ((count = await request.Body.ReadAsync(buffer, ct)) != 0)
            {
                if (memory.Length + count > 4096) throw new MarketRefusal(413, "MarketRequestBound");
                await memory.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)), type, 4096);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
    }
}
