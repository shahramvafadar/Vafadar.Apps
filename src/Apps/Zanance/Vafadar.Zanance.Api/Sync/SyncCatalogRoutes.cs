using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Optional personal spaces and revocable devices; no household roles, paid hosting or purchase grant.</summary>
internal static class SyncCatalogRoutes
{
    /// <summary>Maps owner-checked catalog operations using the actual maintained identity sessions.</summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/identity", async (ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            await scope.CommitAsync(ct); return Results.Ok(new SyncIdentity(scope.Owner));
        });
        group.MapGet("/current-device", async (ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var row = await db.Set<SyncDeviceRow>().SingleOrDefaultAsync(row => row.UserId == scope.Owner && row.SessionId == scope.Session && !row.Revoked, ct);
            if (row is null) return Results.NotFound();
            var result = new SyncDevice(row.Id, SyncLabelProtection.For(protection, scope.Owner, "device", row.Id).Unprotect(row.ProtectedName), row.LastSeenAt, false);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/devices", async (ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var rows = await db.Set<SyncDeviceRow>().AsNoTracking().Where(row => row.UserId == scope.Owner)
                .OrderBy(row => row.Revoked).ThenByDescending(row => row.LastSeenAt).Take(100).ToArrayAsync(ct);
            var result = rows.Select(row => new SyncDevice(row.Id,
                SyncLabelProtection.For(protection, scope.Owner, "device", row.Id).Unprotect(row.ProtectedName), row.LastSeenAt, row.Revoked)).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/devices", async (SyncNameInput input, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            if (!ValidName(input.Name)) return Results.BadRequest();
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var device = await db.Set<SyncDeviceRow>().SingleOrDefaultAsync(row => row.UserId == scope.Owner && row.SessionId == scope.Session, ct);
            if (device is not null)
            {
                if (device.Revoked) return Results.Unauthorized();
                device.ProtectedName = SyncLabelProtection.For(protection, scope.Owner, "device", device.Id).Protect(input.Name.Trim()); device.LastSeenAt = scope.Now;
            }
            else
            {
                if (await db.Set<SyncDeviceRow>().CountAsync(row => row.UserId == scope.Owner && !row.Revoked &&
                    db.Sessions.Any(session => session.Id == row.SessionId && !session.Revoked && session.ExpiresAt > scope.Now), ct) >= 100)
                    return Results.StatusCode(413);
                device = new() { Id = Guid.NewGuid(), UserId = scope.Owner, SessionId = scope.Session, LastSeenAt = scope.Now };
                device.ProtectedName = SyncLabelProtection.For(protection, scope.Owner, "device", device.Id).Protect(input.Name.Trim());
                db.Add(device);
            }
            await scope.CommitAsync(ct); return Results.Ok(new SyncDevice(device.Id, input.Name.Trim(), device.LastSeenAt, false));
        });
        group.MapDelete("/devices/{device:guid}", async (Guid device, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var target = await db.Set<SyncDeviceRow>().SingleOrDefaultAsync(row => row.Id == device && row.UserId == scope.Owner, ct);
            if (target is null) return Results.NotFound();
            target.Revoked = true;
            // Revoke its session too: an old bearer must not register a replacement device under another label.
            var session = await db.Sessions.SingleOrDefaultAsync(row => row.Id == target.SessionId && row.UserId == scope.Owner, ct);
            if (session is not null) session.Revoked = true;
            await SyncStagingMaintenance.RetireDeviceAsync(db, target.Id, ct);
            await scope.CommitAsync(ct); return Results.NoContent();
        });
        group.MapGet("/profiles", async (Guid deviceId, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            if (await scope.DeviceAsync(deviceId, ct) is null) return Results.NotFound();
            var rows = await db.Set<SyncSpaceRow>().AsNoTracking().Where(row => row.UserId == scope.Owner)
                .OrderBy(row => row.Id).Take(100).ToArrayAsync(ct);
            var result = rows.Select(row => new SyncProfile(row.Id,
                SyncLabelProtection.For(protection, scope.Owner, "space", row.Id).Unprotect(row.ProtectedName), row.SchemaHash, row.Cursor, row.Initializing,
                row.Initializing ? row.BootstrapDeviceId : Guid.Empty)).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/profiles", async (SyncCreateProfile input, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            if (!ValidName(input.Name) || input.OperationId == Guid.Empty || input.SchemaHash != SyncSchema.Hash) return Results.BadRequest();
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            if (await scope.DeviceAsync(input.DeviceId, ct) is null) return Results.NotFound();
            var space = await db.Set<SyncSpaceRow>().SingleOrDefaultAsync(row => row.UserId == scope.Owner && row.CreationOperationId == input.OperationId, ct);
            if (space is not null)
            {
                if (space.BootstrapDeviceId != input.DeviceId ||
                    SyncLabelProtection.For(protection, scope.Owner, "space", space.Id).Unprotect(space.ProtectedName) != input.Name.Trim() || space.SchemaHash != input.SchemaHash)
                    return Results.Conflict();
            }
            else
            {
                if (await db.Set<SyncSpaceRow>().CountAsync(row => row.UserId == scope.Owner, ct) >= 100) return Results.StatusCode(413);
                space = new() { Id = Guid.NewGuid(), UserId = scope.Owner, SchemaHash = input.SchemaHash,
                    BootstrapDeviceId = input.DeviceId, CreationOperationId = input.OperationId };
                space.ProtectedName = SyncLabelProtection.For(protection, scope.Owner, "space", space.Id).Protect(input.Name.Trim());
                db.Add(space);
            }
            await scope.CommitAsync(ct); return Results.Ok(new SyncProfile(space.Id, input.Name.Trim(), space.SchemaHash, space.Cursor, space.Initializing,
                space.Initializing ? space.BootstrapDeviceId : Guid.Empty));
        });
        group.MapPost("/profiles/{profile:guid}/initialization", async (Guid profile, SyncClaimInitialization input, ClaimsPrincipal principal,
            ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, CancellationToken ct) =>
        {
            if (input.DeviceId == Guid.Empty || input.ExpectedBootstrapDeviceId == Guid.Empty) return Results.BadRequest();
            await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            if (await scope.DeviceAsync(input.DeviceId, ct) is null) return Results.NotFound();
            // This owner lookup intentionally precedes bootstrap-device filtering: only this explicit
            // reviewed endpoint may transfer an unpublished initialization to another actual device.
            var space = await db.Set<SyncSpaceRow>().SingleOrDefaultAsync(row => row.Id == profile && row.UserId == scope.Owner, ct);
            if (space is null) return Results.NotFound();
            if (!space.Initializing || space.Cursor != 0 || space.SchemaHash != SyncSchema.Hash ||
                await db.Set<SyncRecordRow>().AnyAsync(row => row.SpaceId == profile, ct) ||
                await db.Set<SyncEventRow>().AnyAsync(row => row.SpaceId == profile, ct)) return Results.Conflict();
            // A lost response can be retried by the same actual device without erasing its new staging.
            if (space.BootstrapDeviceId != input.DeviceId)
            {
                if (space.BootstrapDeviceId != input.ExpectedBootstrapDeviceId) return Results.Conflict();
                await db.Set<SyncRoundRow>().Where(row => row.SpaceId == profile && !row.Committed && !row.Abandoned)
                    .ExecuteUpdateAsync(set => set.SetProperty(row => row.Abandoned, true), ct);
                space.BootstrapDeviceId = input.DeviceId;
            }
            var result = new SyncProfile(space.Id, SyncLabelProtection.For(protection, scope.Owner, "space", space.Id).Unprotect(space.ProtectedName),
                space.SchemaHash, space.Cursor, true, space.BootstrapDeviceId);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
    }

    private static bool ValidName(string? name) => name is not null && name.Trim().Length is > 0 and <= 100 && !name.Any(char.IsControl);
}
