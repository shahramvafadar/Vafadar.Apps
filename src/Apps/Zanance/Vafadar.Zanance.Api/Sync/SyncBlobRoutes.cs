using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Bounded resumable attachment transport under the same current owner/device/TLS authority as finance.</summary>
internal static class SyncBlobRoutes
{
    /// <summary>Maps immutable declaration, pieces, verification and authenticated reads.</summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/profiles/{profile:guid}/blobs/{hash}", async (Guid profile, string hash, Guid deviceId, SyncBlobDeclaration input,
            ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, storage, protection, time, async (scope, space, store) =>
                Results.Ok(await store.StatusAsync(await store.DeclareAsync(scope, space, hash, input.Length, ct), ct)), ct));
        group.MapGet("/profiles/{profile:guid}/blobs/{hash}", async (Guid profile, string hash, Guid deviceId,
            ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, storage, protection, time, async (_, space, store) =>
                Results.Ok(await store.StatusAsync(await BlobAsync(db, space.Id, hash, ct), ct)), ct));
        group.MapPut("/profiles/{profile:guid}/blobs/{hash}/parts/{index:int}", PutAsync);
        group.MapPost("/profiles/{profile:guid}/blobs/{hash}/complete", async (Guid profile, string hash, Guid deviceId,
            ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, storage, protection, time, async (scope, space, store) =>
                Results.Ok(await store.CompleteAsync(scope, space, await BlobAsync(db, space.Id, hash, ct), ct)), ct));
        group.MapGet("/profiles/{profile:guid}/blobs/{hash}/parts/{index:int}", async (Guid profile, string hash, int index, Guid deviceId,
            ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, storage, protection, time, async (scope, space, store) =>
            {
                var blob = await BlobAsync(db, space.Id, hash, ct);
                if (!blob.Complete) throw new SyncProtocolRefusal(409, "IncompleteBlob");
                var piece = await db.Set<SyncBlobPieceRow>().AsNoTracking().SingleOrDefaultAsync(row => row.SpaceId == space.Id && row.BlobHash == hash && row.Index == index, ct)
                    ?? throw new SyncProtocolRefusal(404, "BlobPieceUnavailable");
                var bytes = await store.ReadAsync(scope, space, piece, ct);
                return Results.Stream(async output =>
                {
                    try { await output.WriteAsync(bytes, ct); }
                    finally { CryptographicOperations.ZeroMemory(bytes); }
                }, "application/octet-stream");
            }, ct));
    }

    private static async Task<IResult> PutAsync(Guid profile, string hash, int index, Guid deviceId, HttpContext http,
        ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection, TimeProvider time, CancellationToken ct)
    {
        if (http.Request.ContentType != "application/octet-stream" || http.Request.ContentLength is > SyncBlobStore.PieceBytes)
            return Results.BadRequest();
        // Read network bytes before taking the owner lock so a slow sender cannot hold revocation hostage.
        using var content = new MemoryStream(); var buffer = new byte[81920];
        try
        {
            int count;
            while ((count = await http.Request.Body.ReadAsync(buffer, ct)) != 0)
            {
                if (content.Length + count > SyncBlobStore.PieceBytes) return Results.StatusCode(413);
                await content.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            var bytes = content.ToArray();
            try
            {
                return await RunAsync(profile, deviceId, http.User, db, storage, protection, time, async (scope, space, store) =>
                {
                    await store.PutAsync(scope, space, await BlobAsync(db, space.Id, hash, ct), index, bytes, ct);
                    return Results.NoContent();
                }, ct);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            if (content.TryGetBuffer(out var held)) CryptographicOperations.ZeroMemory(held.AsSpan());
        }
    }

    private static async Task<IResult> RunAsync(Guid profile, Guid device, ClaimsPrincipal principal, ServiceDbContext db, ServiceStorage storage,
        IDataProtectionProvider protection, TimeProvider time, Func<SyncRequestScope, SyncSpaceRow, SyncBlobStore, Task<IResult>> action, CancellationToken ct)
    {
        await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
        if (scope is null) return Results.Unauthorized();
        if (await scope.DeviceAsync(device, ct) is null) return Results.NotFound();
        var space = await scope.SpaceAsync(profile, device, ct);
        if (space is null) return Results.NotFound();
        var result = await action(scope, space, new(db, storage, protection));
        await scope.CommitAsync(ct); return result;
    }

    private static async Task<SyncBlobRow> BlobAsync(ServiceDbContext db, Guid space, string hash, CancellationToken ct) =>
        SyncProtocolDigest.IsDigest(hash) ? await db.Set<SyncBlobRow>().SingleOrDefaultAsync(row => row.SpaceId == space && row.Hash == hash, ct)
            ?? throw new SyncProtocolRefusal(404, "BlobUnavailable") : throw new SyncProtocolRefusal(404, "BlobUnavailable");
}
