using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Common optional personal service boundary; never exposes financial routes through cleartext HTTP.</summary>
internal static class SyncServiceRoutes
{
    /// <summary>Registers only owner-authorized TLS operations; private diagnostics never become an error response.</summary>
    internal static void Map(WebApplication app)
    {
        var group = app.MapGroup("/sync").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.Request.IsHttps) return Results.StatusCode(426);
            try { return await next(context); }
            catch (SyncProtocolRefusal error) { return Results.Json(new { Code = error.Message }, statusCode: error.Status); }
            catch (SyncProfileBoundException) { return Results.Json(new { Code = "ProfileBound" }, statusCode: 413); }
            catch (Exception error) when (error is CryptographicException or JsonException)
            { return Results.Json(new { Code = "ProtectedSyncStateUnavailable" }, statusCode: 503); }
            catch (IOException) { return Results.Json(new { Code = "PrivateSyncStorageUnavailable" }, statusCode: 503); }
        });
        SyncCatalogRoutes.Map(group);
        SyncRoundRoutes.Map(group);
        SyncBlobRoutes.Map(group);
    }
}
