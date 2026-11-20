using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>The bounded outcome of an explicit synthetic retention action.</summary>
internal enum AiPilotDeletionResult { Refused, Unconfirmed, Confirmed }

/// <summary>Authorizes exactly one provider deletion for an original completed synthetic result.</summary>
internal sealed class AiPilotRetention(IDbContextFactory<ServiceDbContext> factory, AiPilotProvider pilot, TimeProvider time)
{
    /// <summary>Removes the stored vendor response only, preserving local outcome and credit audit.</summary>
    internal async Task<AiPilotDeletionResult> DeleteAsync(Guid request, string actor, CancellationToken ct)
    {
        if (request == Guid.Empty || string.IsNullOrWhiteSpace(actor) || actor.Length > 256)
            return AiPilotDeletionResult.Refused;

        AiPilotRouteRow route;
        AiOffer offer;
        string handle, digest;
        await using (var db = await factory.CreateDbContextAsync(ct))
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var owner = await db.Set<AiRequestRow>().Where(row => row.Id == request).Select(row => row.UserId).SingleOrDefaultAsync(ct);
            if (owner is null) return AiPilotDeletionResult.Refused;
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            var original = await db.Set<AiRequestRow>().SingleOrDefaultAsync(row => row.Id == request && row.UserId == owner, ct);
            if (original is not { State: AiRequestState.Completed, VendorOperationId: { Length: > 0 } originalHandle })
                return AiPilotDeletionResult.Refused;
            var previous = await db.Set<AiPilotDeletionRow>().SingleOrDefaultAsync(row => row.RequestId == request, ct);
            if (previous is not null)
                return previous.ConfirmedAt.HasValue ? AiPilotDeletionResult.Confirmed : AiPilotDeletionResult.Unconfirmed;

            var frozen = await db.Set<AiOfferRow>().AsNoTracking().SingleAsync(row => row.Id == original.OfferId && row.UserId == owner, ct);
            if (frozen.PilotRouteId is not { } routeId) return AiPilotDeletionResult.Refused;
            route = await db.Set<AiPilotRouteRow>().AsNoTracking().SingleAsync(row => row.Id == routeId, ct);
            offer = new(frozen.Id, frozen.Provider, frozen.Model, frozen.Purpose, frozen.MaximumUnits, frozen.ExpiresAt);
            handle = originalHandle;
            digest = original.PayloadDigest;
            if (!pilot.CanDeleteResult(route, offer, digest, handle)) return AiPilotDeletionResult.Refused;
            db.Add(new AiPilotDeletionRow { RequestId = request, StartedAt = time.GetUtcNow(), Actor = actor });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        // A lost reply can follow a successful deletion. Never replay the destructive command or infer it from absence.
        try
        {
            if (!await pilot.DeleteResultAsync(route, offer, digest, handle, ct)) return AiPilotDeletionResult.Unconfirmed;
        }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or TimeoutException)
        {
            return AiPilotDeletionResult.Unconfirmed;
        }

        await using var receipt = await factory.CreateDbContextAsync(CancellationToken.None);
        var updated = await receipt.Set<AiPilotDeletionRow>().Where(row => row.RequestId == request && row.ConfirmedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ConfirmedAt, time.GetUtcNow()), CancellationToken.None);
        return updated == 1 ? AiPilotDeletionResult.Confirmed : AiPilotDeletionResult.Unconfirmed;
    }
}
