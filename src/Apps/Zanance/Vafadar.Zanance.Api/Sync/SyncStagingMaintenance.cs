using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Reclaims only unreachable uncommitted staging under the same actual-owner order as publication and revocation.</summary>
internal static class SyncStagingMaintenance
{
    /// <summary>Retains every committed acknowledgement and abandoned nonce while retiring a revoked device's unpublished parts.</summary>
    internal static async Task RetireDeviceAsync(ServiceDbContext db, Guid device, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Staging retirement requires the original owner transaction.");
        var pending = await db.Set<SyncRoundRow>().Where(round => round.DeviceId == device && !round.Committed && !round.Abandoned).Select(round => round.Id).ToArrayAsync(ct);
        if (pending.Length == 0) return;
        await db.Set<SyncPartRow>().Where(part => pending.Contains(part.RoundId)).ExecuteDeleteAsync(ct);
        await db.Set<SyncRoundRow>().Where(round => pending.Contains(round.Id) && !round.Committed && !round.Abandoned)
            .ExecuteUpdateAsync(update => update.SetProperty(round => round.Abandoned, true), ct);
    }

    /// <summary>Rechecks expired grants after taking each actual owner lock; a concurrent valid renewal cannot lose its staging.</summary>
    internal static async Task RunAsync(ServiceDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var owners = await db.Set<SyncDeviceRow>().Where(device =>
                !device.Revoked && !db.Sessions.Any(session => session.Id == device.SessionId && session.UserId == device.UserId && !session.Revoked && session.ExpiresAt > now) ||
                device.Revoked && db.Set<SyncRoundRow>().Any(round => round.DeviceId == device.Id && !round.Committed && !round.Abandoned))
            .Select(device => device.UserId).Distinct().OrderBy(owner => owner).Take(100).ToArrayAsync(ct);
        foreach (var owner in owners)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            var devices = await db.Set<SyncDeviceRow>().Where(device => device.UserId == owner &&
                (!device.Revoked && !db.Sessions.Any(session => session.Id == device.SessionId && session.UserId == owner && !session.Revoked && session.ExpiresAt > now) ||
                 device.Revoked && db.Set<SyncRoundRow>().Any(round => round.DeviceId == device.Id && !round.Committed && !round.Abandoned)))
                .Select(device => device.Id).ToArrayAsync(ct);
            await db.Set<SyncDeviceRow>().Where(device => devices.Contains(device.Id)).ExecuteUpdateAsync(update => update.SetProperty(device => device.Revoked, true), ct);
            foreach (var device in devices) await RetireDeviceAsync(db, device, ct);
            await transaction.CommitAsync(ct);
        }
    }
}
