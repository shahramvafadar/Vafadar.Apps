using System.Security.Claims;

namespace Vafadar.Zanance.Api.Sharing;

internal static partial class HouseholdRoutes
{
    /// <summary>Maps only actual room-scoped metadata and explicit owner privacy actions; none carries a readable financial payload.</summary>
    private static void MapLifecycle(RouteGroupBuilder group)
    {
        group.MapGet("/{space:guid}/deletion/checkpoint", async (Guid space, Guid device, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var checkpoint = await new HouseholdLifecycleStore(db).CheckpointAsync(scope, device, ct); await scope.CommitAsync(ct); return Results.Ok(checkpoint);
        });
        group.MapPost("/{space:guid}/deletion/review", async (Guid space, Core.Sharing.HouseholdDeleteRequest input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var review = await new HouseholdLifecycleStore(db).ReviewAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(review);
        });
        group.MapGet("/{space:guid}/lifecycle", async (Guid space, Guid device, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var status = await new HouseholdLifecycleStore(db).StatusAsync(scope, device, ct); await scope.CommitAsync(ct); return Results.Ok(status);
        });
        group.MapGet("/{space:guid}/audit", async (Guid space, Guid device, long after, long? through, int? limit, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var result = await new HouseholdAuditStore(db).ReadAsync(scope, device, after, through, ct, limit ?? 16); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/deletion", async (Guid space, Core.Sharing.HouseholdDeleteRequest input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var receipt = await new HouseholdLifecycleStore(db).DeleteAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(receipt);
        });
        group.MapGet("/{space:guid}/deletion/{operation:guid}", async (Guid space, Guid operation, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct); if (scope is null) return Results.NotFound();
            var receipt = await new HouseholdLifecycleStore(db).ObserveAsync(scope, operation, ct); await scope.CommitAsync(ct); return Results.Ok(receipt);
        });
    }
}
