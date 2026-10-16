using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api;

/// <summary>Durable local outbox and private deletion; no external mail is sent by this delivery.</summary>
public sealed class PrivateMaintenance(IServiceScopeFactory scopes, ServiceStorage storage, TimeProvider time, IHostApplicationLifetime lifetime) : BackgroundService
{
    private readonly Sync.SyncPieceMaintenance syncPieces = new(storage, time);
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5), time);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (File.Exists(Path.Combine(storage.Root, "shutdown.request"))) { lifetime.StopApplication(); break; }
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception e) when (e is IOException or Npgsql.NpgsqlException or DbUpdateException) { /* Retry durable work; never log protected mail or object bytes. */ }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }

    /// <summary>Idempotent worker pass; leased rows recover after a terminated worker.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = time.GetUtcNow();
        await syncPieces.RunAsync(db, ct);
        await scope.ServiceProvider.GetRequiredService<Sharing.HouseholdRetirement>().RunOnceAsync(ct);
        foreach (var id in await db.Objects.Where(o => o.Deleted && !o.StorageRetired).OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).Select(o => o.Id).Take(100).ToListAsync(ct))
        {
            File.Delete(storage.ObjectPath(id));
            await db.Objects.Where(o => o.Id == id && o.Deleted).ExecuteUpdateAsync(u => u.SetProperty(o => o.StorageRetired, true), ct);
        }
        foreach (var path in Directory.EnumerateFiles(Path.Combine(storage.Root, "objects")).Take(1000))
        {
            if (!Guid.TryParseExact(Path.GetFileName(path), "N", out var id) || File.GetLastWriteTimeUtc(path) >= now.AddDays(-1).UtcDateTime) continue;
            if (!await db.Objects.AnyAsync(o => o.Id == id, ct)) File.Delete(path);
        }
        await db.RequestWindows.Where(w => w.StartsAt < now.AddHours(-1)).ExecuteDeleteAsync(ct);
        await Sync.SyncStagingMaintenance.RunAsync(db, now, ct);
        await db.Sessions.Where(s => s.ExpiresAt < now.AddDays(-1)).ExecuteDeleteAsync(ct);
        var expiredMail = await db.Challenges.Where(c => c.ExpiresAt < now.AddDays(-1)).OrderBy(c => c.ExpiresAt).ThenBy(c => c.Id).Select(c => c.Id).Take(100).ToListAsync(ct);
        foreach (var id in expiredMail)
        {
            File.Delete(Path.Combine(storage.Root, "mailbox", id.ToString("N")));
            await db.Challenges.Where(c => c.Id == id && c.ExpiresAt < now.AddDays(-1)).ExecuteDeleteAsync(ct);
        }
        if (!storage.LocalMailbox) return;
        var lease = Guid.NewGuid();
        var ids = await db.Challenges.Where(c => !c.Delivered && !c.Consumed && c.ExpiresAt > now && (c.LeaseUntil == null || c.LeaseUntil < now))
            .OrderBy(c => c.ExpiresAt).Select(c => c.Id).Take(20).ToListAsync(ct);
        foreach (var id in ids)
        {
            var claimed = await db.Challenges.Where(c => c.Id == id && !c.Delivered && !c.Consumed && c.ExpiresAt > now && (c.LeaseUntil == null || c.LeaseUntil < now))
                .ExecuteUpdateAsync(u => u.SetProperty(c => c.LeaseOwner, lease).SetProperty(c => c.LeaseUntil, now.AddMinutes(1)), ct);
            if (claimed != 1) continue;
            var row = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == id, ct);
            var path = Path.Combine(storage.Root, "mailbox", id.ToString("N"));
            var temporary = path + "." + lease.ToString("N") + ".pending";
            await File.WriteAllTextAsync(temporary, row.ProtectedMail, ct);
            File.Move(temporary, path, overwrite: true);
            await db.Challenges.Where(c => c.Id == id && c.LeaseOwner == lease).ExecuteUpdateAsync(u => u.SetProperty(c => c.Delivered, true), ct);
        }
    }
}
