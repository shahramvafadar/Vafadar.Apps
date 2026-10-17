using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Separate bounded original-source companion endpoints under actual existing personal profile/device authority.</summary>
internal static class PersonalMarketSourceRoutes
{
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/profiles/{profile:guid}/source-documents", async (Guid profile, Guid deviceId, HttpRequest request,
            ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            if (await scope.DeviceAsync(deviceId, ct) is null) return Results.NotFound();
            var space = await scope.SpaceAsync(profile, deviceId, ct); if (space is null) return Results.NotFound();
            var input = await ReadAsync(request, ct);
            var result = await new PersonalMarketSourceStore(db, protection, storage).PutAsync(scope, space, input, ct);
            await scope.CommitAsync(ct); return Results.Json(result, MarketSourceJson.Default.MarketSourceAcknowledgment);
        });
        group.MapGet("/profiles/{profile:guid}/source-documents", async (Guid profile, Guid deviceId, long after, long? through,
            ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            if (await scope.DeviceAsync(deviceId, ct) is null) return Results.NotFound();
            var space = await scope.SpaceAsync(profile, deviceId, ct); if (space is null) return Results.NotFound();
            var result = await new PersonalMarketSourceStore(db, protection, storage).ReadAsync(scope, space, after, through, ct);
            await scope.CommitAsync(ct); return Results.Json(result, MarketSourceJson.Default.MarketSourcePage);
        });
    }
    private static async Task<MarketSourceUpload> ReadAsync(HttpRequest request, CancellationToken ct)
    {
        const int maximum = PersonalMarketSourceStore.MaximumUploadBytes;
        if (request.ContentLength > maximum) throw new SyncProtocolRefusal(413, "MarketSourceUploadBound");
        if (request.ContentType is not { } content || !content.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            throw new SyncProtocolRefusal(415, "MarketSourceJsonRequired");
        using var memory = new MemoryStream(); var buffer = new byte[8192];
        try
        {
            int count; while ((count = await request.Body.ReadAsync(buffer, ct)) != 0)
            {
                if (memory.Length + count > maximum) throw new SyncProtocolRefusal(413, "MarketSourceUploadBound");
                await memory.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            try { return StrictJsonPayload.Deserialize(memory.GetBuffer().AsMemory(0, checked((int)memory.Length)), MarketSourceJson.Default.MarketSourceUpload, maximum); }
            catch (Exception error) when (error is JsonException or ArgumentException) { throw new SyncProtocolRefusal(422, "MarketSourceUploadInvalid"); }
        }
        finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(memory.GetBuffer()); }
    }
}
