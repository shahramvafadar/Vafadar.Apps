using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Opt-in local encrypted household transport; every resource is checked under actual session then room authority.</summary>
internal static partial class HouseholdRoutes
{
    /// <summary>Public household activation remains refused until separately verified hosting/entitlement authority exists.</summary>
    internal static void Map(WebApplication app)
    {
        if (!app.Services.GetRequiredService<ServiceStorage>().LocalHouseholdQualification) return;
        var group = app.MapGroup("/households").RequireAuthorization();
        MapRecoveryArchive(group);
        MapOwnership(group);
        MapLifecycle(group);
        group.AddEndpointFilter(async (context, next) =>
        {
            try { return await next(context); }
            catch (HouseholdProtocolRefusal error) { return Results.Problem(statusCode: error.Status, title: error.Message); }
            catch (IOException) { return Results.Problem(statusCode: 503, title: "HouseholdPrivateStorageUnavailable"); }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Results.Problem(statusCode: 409, title: "HouseholdConcurrentAuthorityChanged"); }
        });
        group.MapGet("/session", async (ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(actor, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var result = new HouseholdSessionIdentity(scope.Owner, scope.Session); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/devices", async (Guid space, HouseholdEnrollDevice input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdDeviceStore(db).EnrollAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/devices/pending", async (Guid space, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await scope.RequireOwnerPendingDeviceAsync(device, ct);
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Invite, ct);
            var accepted = db.Set<HouseholdMemberRow>().Where(row => row.SpaceId == space && row.Accepted && !row.Revoked).Select(row => row.UserId);
            var bound = HouseholdLimits.DevicesPerIdentity * QuotaPolicy.SharedSpaceSeatLimit;
            var rows = await db.Set<HouseholdDeviceRow>().AsNoTracking().Where(row => row.SpaceId == space && !row.Approved && !row.Revoked &&
                row.AdmissionProof != null && accepted.Contains(row.UserId)).OrderBy(row => row.Id).Take(bound + 1).ToArrayAsync(ct);
            if (rows.Length > bound) throw new HouseholdProtocolRefusal(413, "HouseholdDeviceBound");
            var result = rows.Select(row => new HouseholdPendingDevice(row.Id, row.UserId, new(row.SigningKey, row.WrappingKey), row.AdmissionProof!)).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/devices/{device:guid}/rebind", async (Guid space, Guid device, HouseholdRebindDevice input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdDeviceStore(db).RebindAsync(scope, device, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/", async (HouseholdReserveSpace input, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await SyncRequestScope.OpenAsync(actor, db, time, ct);
            if (scope is null) return Results.Unauthorized();
            var result = await new HouseholdMembershipStore(db).ReserveAsync(scope, input, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/membership", async (Guid space, HouseholdPublishMembership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdMembershipStore(db).PublishAsync(scope, input, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/invitations", async (Guid space, HouseholdCreateInvitation input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdMembershipStore(db).InviteAsync(scope, input, ct);
            await scope.CommitAsync(ct); return Results.Ok(new HouseholdInvitationReservation(result));
        });
        group.MapGet("/{space:guid}/invitations/pending", async (Guid space, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await scope.RequireOwnerPendingDeviceAsync(device, ct);
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Invite, ct);
            var rows = await db.Set<HouseholdInvitationRow>().AsNoTracking().Where(row => row.SpaceId == space && !row.Accepted &&
                !row.Revoked && row.ExpiresAt > scope.Now && row.ClaimedUserId != null && row.ClaimedDeviceId != null)
                .OrderBy(row => row.Id).Take(Core.Commerce.QuotaPolicy.SharedSpaceSeatLimit).ToArrayAsync(ct);
            var ids = rows.Select(row => row.ClaimedDeviceId!.Value).ToArray();
            var devices = await db.Set<HouseholdDeviceRow>().AsNoTracking().Where(row => row.SpaceId == space && ids.Contains(row.Id) && !row.Revoked)
                .ToDictionaryAsync(row => row.Id, ct);
            var result = rows.Where(row => devices.TryGetValue(row.ClaimedDeviceId!.Value, out var key) && key.UserId == row.ClaimedUserId)
                .Select(row => new HouseholdPendingClaim(row.Id, row.SignedInvitation, row.SignedClaim!, row.ClaimedUserId!, row.ClaimedDeviceId!.Value,
                    new(devices[row.ClaimedDeviceId.Value].SigningKey, devices[row.ClaimedDeviceId.Value].WrappingKey))).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/invitations/{invitation:guid}/claim", async (Guid space, Guid invitation,
            HouseholdClaimInvitation input, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdMembershipStore(db).ClaimAsync(scope, invitation, input, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/checkpoint", async (Guid space, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Read, ct);
            var result = HouseholdMembershipStore.Checkpoint(scope); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/membership", async (Guid space, Guid device, long after, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.ReadAudit, ct);
            if (after < 0 || after > scope.Space.MembershipVersion) return Results.BadRequest();
            var rows = await db.Set<HouseholdMembershipEventRow>().AsNoTracking().Where(row => row.SpaceId == space && row.Version > after)
                .OrderBy(row => row.Version).Take(8).ToArrayAsync(ct);
            var result = rows.Select(row => new HouseholdMembershipProof(row.SignedEvent, row.TransferAcceptance,
                row.SignedAcceptanceClaim, row.GrantsHash, row.RecoveryEvidenceJson is null ? null : System.Text.Json.JsonSerializer.Deserialize(
                    row.RecoveryEvidenceJson, HouseholdMetadataJsonContext.Default.HouseholdRecoveryEvidence))).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/grants/{epoch:long}", async (Guid space, long epoch, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await scope.RequireAsync(device, HouseholdRemoteOperation.ReadDeviceGrant, ct);
            var result = await db.Set<HouseholdKeyGrantRow>().AsNoTracking().SingleOrDefaultAsync(row => row.SpaceId == space && row.Epoch == epoch && row.DeviceId == device, ct);
            if (result is null) return Results.NotFound();
            await scope.CommitAsync(ct); return Results.Ok(new HouseholdDeviceGrant(result.Epoch, result.DeviceId, result.MembershipVersion, result.Ciphertext, result.Digest));
        });
        group.MapPost("/{space:guid}/leave", async (Guid space, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            await new HouseholdMembershipStore(db).LeaveAsync(scope, device, ct);
            await scope.CommitAsync(ct); return Results.NoContent();
        });
        group.MapPost("/{space:guid}/rounds", async (Guid space, HouseholdOpenRound input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var store = new HouseholdRoundStore(db); var round = await store.OpenAsync(scope, input, ct);
            var result = await store.HandleAsync(round, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/rounds/abandon", async (Guid space, HouseholdOpenRound input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).AbandonAttemptAsync(scope, input, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/rounds/attempt", async (Guid space, Guid device, Guid operation, Guid attempt,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            await scope.RequireOwnAttemptObservationAsync(device, ct);
            var row = await db.Set<HouseholdRoundRow>().SingleOrDefaultAsync(value => value.SpaceId == space && value.DeviceId == device &&
                value.OperationId == operation && value.AttemptId == attempt, ct);
            if (row is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).HandleAsync(row, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/rounds/{round:guid}/parts", async (Guid space, Guid round, Guid device, int offset,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).UploadedPartsAsync(scope, await OwnedRoundAsync(db, space, round, ct), device, offset, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/rounds/{round:guid}/parts", async (Guid space, Guid round, HouseholdUploadParts input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            await new HouseholdRoundStore(db).PutBatchAsync(scope, await OwnedRoundAsync(db, space, round, ct), input, ct);
            await scope.CommitAsync(ct); return Results.Ok(true);
        });
        group.MapPut("/{space:guid}/rounds/{round:guid}/parts", async (Guid space, Guid round, HouseholdOpaquePart input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var row = await OwnedRoundAsync(db, space, round, ct);
            await new HouseholdRoundStore(db).PutAsync(scope, row, input, ct); await scope.CommitAsync(ct); return Results.NoContent();
        });
        group.MapPost("/{space:guid}/rounds/{round:guid}/commit", async (Guid space, Guid round, HouseholdCommitRound input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).CommitAsync(scope, await OwnedRoundAsync(db, space, round, ct), input, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapDelete("/{space:guid}/rounds/{round:guid}", async (Guid space, Guid round, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            await new HouseholdRoundStore(db).AbandonAsync(scope, await OwnedRoundAsync(db, space, round, ct), ct);
            await scope.CommitAsync(ct); return Results.NoContent();
        });
        group.MapGet("/{space:guid}/feed", async (Guid space, Guid device, long afterCursor, string afterRecord, long through,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).FeedAsync(scope, device, new(afterCursor, afterRecord), through, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/receipts/{round:guid}/parts", async (Guid space, Guid round, Guid device, int offset,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).ReceiptPageAsync(scope, device, round, offset, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/receipts/{round:guid}", async (Guid space, Guid round, Guid device,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRoundStore(db).ReceiptAsync(scope, device, round, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
    }

    /// <summary>Even opaque staging ids resolve through the exact room before the store checks actual caller/device authority.</summary>
    private static async Task<HouseholdRoundRow> OwnedRoundAsync(ServiceDbContext db, Guid space, Guid round, CancellationToken ct) =>
        await db.Set<HouseholdRoundRow>().SingleOrDefaultAsync(row => row.Id == round && row.SpaceId == space, ct)
        ?? throw new HouseholdProtocolRefusal(404, "HouseholdResourceUnavailable");
}
