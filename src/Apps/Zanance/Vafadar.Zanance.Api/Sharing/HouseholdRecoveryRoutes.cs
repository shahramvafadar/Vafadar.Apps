using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

internal static partial class HouseholdRoutes
{
    /// <summary>Only local opt-in household hosts map these opaque archive resources; ordinary public hosting stays disabled.</summary>
    private static void MapRecoveryArchive(RouteGroupBuilder group)
    {
        group.MapPost("/{space:guid}/recovery/commit", async (Guid space, HouseholdRecoverOwner input, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).RecoverAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/recovery/membership", async (Guid space, long after, ClaimsPrincipal actor,
            ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            _ = await new HouseholdRecoveryStore(db).ReadAsync(scope, ct);
            if (after < 0 || after > scope.Space.MembershipVersion) return Results.BadRequest();
            var rows = await db.Set<HouseholdMembershipEventRow>().AsNoTracking().Where(row => row.SpaceId == space && row.Version > after)
                .OrderBy(row => row.Version).Take(8).ToArrayAsync(ct);
            var result = rows.Select(row => new HouseholdMembershipProof(row.SignedEvent, row.TransferAcceptance, row.SignedAcceptanceClaim,
                row.GrantsHash, row.RecoveryEvidenceJson is null ? null : System.Text.Json.JsonSerializer.Deserialize(row.RecoveryEvidenceJson,
                    HouseholdMetadataJsonContext.Default.HouseholdRecoveryEvidence))).ToArray();
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/owner-root-grant", async (Guid space, Guid device, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).OwnerRootAsync(scope, device, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/recovery", async (Guid space, ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).ReadAsync(scope, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/recovery/{enrollment:guid}/epochs", async (Guid space, Guid enrollment, long after, long through,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).EpochsAsync(scope, enrollment, after, through, ct);
            await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/recovery/preparations", async (Guid space, HouseholdOpenRecoverySeed input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).OpenSeedAsync(scope, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapGet("/{space:guid}/recovery/preparations/{operation:guid}", async (Guid space, Guid operation, Guid device,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).SeedAsync(scope, device, operation, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapPost("/{space:guid}/recovery/preparations/{operation:guid}/epochs", async (Guid space, Guid operation, HouseholdUploadRecoveryEpochs input,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            var result = await new HouseholdRecoveryStore(db).UploadAsync(scope, operation, input, ct); await scope.CommitAsync(ct); return Results.Ok(result);
        });
        group.MapDelete("/{space:guid}/recovery/preparations/{operation:guid}", async (Guid space, Guid operation, Guid device,
            ClaimsPrincipal actor, ServiceDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            await using var scope = await HouseholdRequestScope.OpenAsync(actor, db, time, space, ct);
            if (scope is null) return Results.NotFound();
            await new HouseholdRecoveryStore(db).CancelAsync(scope, device, operation, ct); await scope.CommitAsync(ct); return Results.NoContent();
        });
    }
}
