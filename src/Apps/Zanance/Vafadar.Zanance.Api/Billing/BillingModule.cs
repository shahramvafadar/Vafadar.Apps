using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Explicit local verification composition; commercial activation is not inferred from a test environment.</summary>
internal static class BillingModule
{
    /// <summary>Registers lazy credential/signing ports without reading keys, identities, receipts or financial data.</summary>
    internal static void Add(IServiceCollection services)
    {
        // The catalogue intentionally keeps its constructor internal; the container selects public constructors only.
        services.AddSingleton(service => new BillingCatalog(service.GetRequiredService<IConfiguration>())); services.AddSingleton<BillingAuthority>();
        services.AddSingleton<BillingRegistry>(); services.AddSingleton<BillingProviderHttp>();
        services.AddSingleton<BillingPurchaseCoordinator>(); services.AddSingleton<BillingCommandStore>();
        services.AddSingleton<IGoogleBillingCredentials, GoogleBillingCredentials>();
        services.AddSingleton<IBillingProvider, GooglePlayPurchaseProvider>();
        services.AddSingleton<AppleBillingSignatures>(); services.AddSingleton<IAppleBillingCredentials, AppleBillingCredentials>();
        services.AddSingleton<IBillingProvider, ApplePurchaseProvider>();
        services.AddSingleton<BillingNotifications>(); services.AddSingleton<BillingReconciliation>(); services.AddHostedService<BillingReconciliationWorker>();
    }

    /// <summary>Maps only the explicitly allowed local billing principal/time boundary; unsupported public activation stays closed.</summary>
    internal static void Map(WebApplication app)
    {
        if (!app.Services.GetRequiredService<ServiceStorage>().LocalBillingQualification) return;
        var group = app.MapGroup("/billing");
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.Request.IsHttps) return Results.StatusCode(426);
            try { return await next(context); }
            catch (BillingRefusal refusal) { return Results.Json(new { Code = refusal.Message }, statusCode: refusal.Status); }
            catch (Exception error) when (error is CryptographicException or JsonException or FormatException or KeyNotFoundException or ArgumentException)
            { return Results.Json(new { Code = "BillingProofRefused" }, statusCode: 422); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { return Results.Json(new { Code = "OriginalBillingStateUnavailable" }, statusCode: 503); }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Results.Json(new { Code = "BillingConcurrentStateChanged" }, statusCode: 409); }
        });
        group.MapPost("/notifications/google/{environment}", async (BillingEnvironment environment, HttpRequest request,
            BillingNotifications notifications, CancellationToken ct) =>
        {
            await notifications.GoogleAsync(request, environment, ct); return Results.NoContent();
        });
        group.MapPost("/notifications/apple/{environment}", async (BillingEnvironment environment, HttpRequest request,
            BillingNotifications notifications, AppleBillingSignatures signatures, CancellationToken ct) =>
        {
            await notifications.AppleAsync(request, environment, signatures, ct); return Results.NoContent();
        });
        group.MapGet("/time/{nonce:guid}", async (Guid nonce, BillingAuthority authority, TimeProvider time, CancellationToken ct) =>
        {
            using var signer = await authority.OpenAsync(ct);
            var now = time.GetUtcNow(); var proof = new BillingTimeProof(nonce, now, now.AddMinutes(2));
            return Results.Json(new BillingTimeResult(BillingAuthority.Public(signer), BillingAuthority.SignTime(proof, signer)), BillingJson.Default.BillingTimeResult);
        });
        group.MapPost("/devices", async (HttpRequest request, ClaimsPrincipal actor, BillingRegistry registry, CancellationToken ct) =>
        {
            var body = await ReadAsync(request, BillingJson.Default.BillingRegistrationEnvelope, ct);
            return Results.Json(await registry.RegisterAsync(body, actor, ct), BillingJson.Default.BillingRegistrationResult);
        });
        group.MapPost("/devices/{customer:guid}/commands", async (Guid customer, HttpRequest request, ClaimsPrincipal actor,
            BillingCommandStore commands, CancellationToken ct) =>
        {
            var body = await ReadAsync(request, BillingJson.Default.BillingSignedRequest, ct);
            return Results.Json(await commands.ExecuteAsync(customer, body.Signed, actor, ct), BillingJson.Default.BillingCommandResult);
        });
    }

    /// <summary>Strict bounded request parsing, without logging request bodies or provider tokens.</summary>
    internal static async Task<T> ReadAsync<T>(HttpRequest request, JsonTypeInfo<T> type, CancellationToken ct) where T : class
    {
        if (request.ContentLength > 65536) throw new BillingRefusal(413, "BillingRequestBound");
        if (request.ContentType is not { } content || !content.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            throw new BillingRefusal(415, "BillingJsonRequired");
        using var memory = new MemoryStream(); var buffer = new byte[4096];
        try
        {
            int read;
            while ((read = await request.Body.ReadAsync(buffer, ct)) != 0)
            {
                if (memory.Length + read > 65536) throw new BillingRefusal(413, "BillingRequestBound");
                await memory.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)), type, 65536);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
    }
}
