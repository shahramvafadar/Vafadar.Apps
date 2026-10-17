using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Explicit local prerequisites only; no real model, pricing, purchase grant or public activation.</summary>
internal static class AiModule
{
    /// <summary>Registers lazy closed provider and independently persisted credit/request coordination.</summary>
    internal static void Add(IServiceCollection services)
    { services.AddSingleton<IAiProvider, UnconfiguredAiProvider>(); services.AddSingleton<AiRequestStore>(); }
    /// <summary>Maps identity-scoped local qualification; no grant or arbitrary provider/url endpoint exists.</summary>
    internal static void Map(WebApplication app)
    {
        if (!app.Services.GetRequiredService<ServiceStorage>().LocalAnalysisQualification) return;
        var group = app.MapGroup("/analysis").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.Request.IsHttps) return Results.StatusCode(426);
            try { return await next(context); }
            catch (AiRefusal refusal) { return Results.Json(new { Code = refusal.Message }, statusCode: refusal.Status); }
            catch (Exception error) when (error is ArgumentException or FormatException or JsonException or OverflowException or CryptographicException)
            { return Results.Json(new { Code = "AiRequestRefused" }, statusCode: 422); }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Results.Json(new { Code = "AiConcurrentStateChanged" }, statusCode: 409); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { return Results.Json(new { Code = "AiOriginalStateUnavailable" }, statusCode: 503); }
        });
        group.MapGet("/availability", async (ClaimsPrincipal actor, AiRequestStore store, CancellationToken ct) =>
            Results.Json(await store.AvailabilityAsync(actor, ct), AiJson.Default.AiAvailability));
        group.MapPost("/offers", async (ClaimsPrincipal actor, HttpRequest request, AiRequestStore store, CancellationToken ct) =>
        {
            var body = await ReadAsync(request, AiJson.Default.AiOfferRequest, ct);
            return Results.Json(await store.OfferAsync(actor, body.Purpose, ct), AiJson.Default.AiOffer);
        });
        group.MapPost("/requests", async (ClaimsPrincipal actor, HttpRequest request, AiRequestStore store, CancellationToken ct) =>
        {
            var body = await ReadAsync(request, AiJson.Default.AiSubmission, ct);
            return Results.Json(await store.SubmitAsync(actor, body, ct), AiJson.Default.AiRequestResult);
        });
        group.MapPost("/requests/{id:guid}/close", async (Guid id, ClaimsPrincipal actor, HttpRequest request, AiRequestStore store, CancellationToken ct) =>
        {
            var body = await ReadAsync(request, AiJson.Default.AiCloseRequest, ct);
            return Results.Json(await store.CloseAsync(actor, id, body, ct), AiJson.Default.AiRequestResult);
        });
        group.MapPost("/requests/{id:guid}/status", async (Guid id, ClaimsPrincipal actor, AiRequestStore store, CancellationToken ct) =>
            Results.Json(await store.StatusAsync(actor, id, ct), AiJson.Default.AiRequestResult));
    }
    private static async Task<T> ReadAsync<T>(HttpRequest request, JsonTypeInfo<T> type, CancellationToken ct) where T : class
    {
        if (request.ContentLength > 65536) throw new AiRefusal(413, "AiRequestBound");
        if (request.ContentType is not { } content || !content.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            throw new AiRefusal(415, "AiJsonRequired");
        using var memory = new MemoryStream(); var buffer = new byte[4096];
        try
        {
            int count; while ((count = await request.Body.ReadAsync(buffer, ct)) != 0)
            {
                if (memory.Length + count > 65536) throw new AiRefusal(413, "AiRequestBound");
                await memory.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)), type, 65536);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
    }
}
