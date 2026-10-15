using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Personal whole-round publication and immutable feed, with current device authority on every endpoint.</summary>
internal static class SyncRoundRoutes
{
    /// <summary>Maps under the common authenticated TLS-only personal group.</summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/profiles/{profile:guid}/rounds", async (Guid profile, SyncOpenRound input, ClaimsPrincipal principal,
            ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, input.DeviceId, principal, db, time, protection, storage, async (scope, space, store) =>
            {
                var round = await store.OpenAsync(scope, space, input, ct);
                return Results.Ok(await HandleAsync(db, round, ct));
            }, ct));
        group.MapGet("/profiles/{profile:guid}/rounds/{roundId:guid}", async (Guid profile, Guid roundId, Guid deviceId, ClaimsPrincipal principal,
            ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, time, protection, storage, async (_, space, _) =>
            {
                var round = await RoundAsync(db, space.Id, deviceId, roundId, ct);
                return Results.Ok(await HandleAsync(db, round, ct));
            }, ct));
        group.MapPut("/profiles/{profile:guid}/rounds/{roundId:guid}/parts", async (Guid profile, Guid roundId, Guid deviceId,
            SyncRoundPart input, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, time, protection, storage, async (scope, space, store) =>
            {
                var round = await RoundAsync(db, space.Id, deviceId, roundId, ct);
                await store.PutPartAsync(scope, space, round, input, ct); return Results.NoContent();
            }, ct));
        group.MapPost("/profiles/{profile:guid}/rounds/{roundId:guid}/commit", async (Guid profile, Guid roundId, Guid deviceId,
            SyncCommitRound input, ClaimsPrincipal principal, ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, time, protection, storage, async (scope, space, store) =>
            {
                var round = await RoundAsync(db, space.Id, deviceId, roundId, ct);
                return Results.Ok(await store.CommitAsync(scope, space, round, input, ct));
            }, ct));
        group.MapDelete("/profiles/{profile:guid}/rounds/{roundId:guid}", async (Guid profile, Guid roundId, Guid deviceId, ClaimsPrincipal principal,
            ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, time, protection, storage, async (_, space, _) =>
            {
                var round = await RoundAsync(db, space.Id, deviceId, roundId, ct);
                if (round.Committed) throw new SyncProtocolRefusal(409, "CommittedOperation");
                round.Abandoned = true;
                await db.Set<SyncPartRow>().Where(row => row.RoundId == round.Id).ExecuteDeleteAsync(ct);
                return Results.NoContent();
            }, ct));
        group.MapGet("/profiles/{profile:guid}/feed", async (Guid profile, Guid deviceId, long after, long through, ClaimsPrincipal principal,
            ServiceDbContext db, TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, CancellationToken ct) =>
            await RunAsync(profile, deviceId, principal, db, time, protection, storage, async (scope, space, store) =>
                Results.Ok(await store.FeedAsync(scope, space, after, through, ct)), ct));
    }

    private static async Task<IResult> RunAsync(Guid profile, Guid device, ClaimsPrincipal principal, ServiceDbContext db,
        TimeProvider time, IDataProtectionProvider protection, ServiceStorage storage, Func<SyncRequestScope, SyncSpaceRow, SyncRoundStore, Task<IResult>> action, CancellationToken ct)
    {
        await using var scope = await SyncRequestScope.OpenAsync(principal, db, time, ct);
        if (scope is null) return Results.Unauthorized();
        if (await scope.DeviceAsync(device, ct) is null) return Results.NotFound();
        var space = await scope.SpaceAsync(profile, device, ct);
        if (space is null) return Results.NotFound();
        var result = await action(scope, space, new(db, protection, storage));
        await scope.CommitAsync(ct); return result;
    }

    private static async Task<SyncRoundRow> RoundAsync(ServiceDbContext db, Guid space, Guid device, Guid round, CancellationToken ct) =>
        await db.Set<SyncRoundRow>().SingleOrDefaultAsync(row => row.Id == round && row.SpaceId == space && row.DeviceId == device, ct)
            ?? throw new SyncProtocolRefusal(404, "RoundUnavailable");

    private static async Task<SyncRoundHandle> HandleAsync(ServiceDbContext db, SyncRoundRow round, CancellationToken ct)
    {
        if (round.Abandoned) throw new SyncProtocolRefusal(410, "AbandonedOperation");
        var parts = await db.Set<SyncPartRow>().AsNoTracking().Where(row => row.RoundId == round.Id)
            .GroupBy(row => row.RoundId).Select(group => new { Count = group.Count(), Rows = group.Sum(row => (long)row.Rows) }).SingleOrDefaultAsync(ct);
        return new(round.Id, round.Committed, round.CommitCursor, parts?.Count ?? 0, parts?.Rows ?? 0);
    }
}
