using System.Security.Claims;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

internal static partial class HouseholdRoutes
{
    /// <summary>Maps only the same explicitly enabled local household edition, retaining actual session/room transactions on every handshake step.</summary>
    private static void MapOwnership(RouteGroupBuilder group)
    {
        group.MapGet("/{space:guid}/ownership", async (Guid space, Guid device, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).CurrentAsync(scope, device, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership", async (Guid space, HouseholdOpenOwnership input, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).OpenAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership/{operation:guid}/prepare", async (Guid space, Guid operation, HouseholdPrepareOwnership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).PrepareAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership/{operation:guid}/authorize", async (Guid space, Guid operation, HouseholdAuthorizeOwnership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).AuthorizeAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership/{operation:guid}/accept", async (Guid space, Guid operation, HouseholdAcceptOwnership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).AcceptAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership/{operation:guid}/observe", async (Guid space, Guid operation, HouseholdObserveOwnership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).ObserveAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/ownership/{operation:guid}/cancel", async (Guid space, Guid operation, HouseholdCancelOwnership input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdOwnershipStore(db).CancelAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
    }
}
