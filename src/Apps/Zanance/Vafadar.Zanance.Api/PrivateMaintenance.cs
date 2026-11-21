using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Vafadar.Zanance.Api;

/// <summary>Durable selected identity outbox and private deletion; external mail requires explicit independent configuration.</summary>
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
        await AccountPersonalSyncRetirement.RetireFilesAsync(db, storage, db.Set<AccountSyncFileRetirement>(), ct);
        await syncPieces.RunAsync(db, ct);
        await scope.ServiceProvider.GetRequiredService<Sharing.HouseholdRetirement>().RunOnceAsync(ct);
        await AccountPrivateObjectRetirement.RetireFilesAsync(db, storage, db.Objects, ct);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(storage.Root, "objects")).Take(1000))
        {
            if (!Guid.TryParseExact(Path.GetFileName(path), "N", out var id) || File.GetLastWriteTimeUtc(path) >= now.AddDays(-1).UtcDateTime) continue;
            if (!await db.Objects.AnyAsync(o => o.Id == id, ct)) File.Delete(path);
        }
        await db.RequestWindows.Where(w => w.StartsAt < now.AddHours(-1)).ExecuteDeleteAsync(ct);
        await Sync.SyncStagingMaintenance.RunAsync(db, now, ct);
        await db.Sessions.Where(s => s.ExpiresAt < now.AddDays(-1)).ExecuteDeleteAsync(ct);
        var cutoff = now.AddDays(-1);
        var expiredMail = await db.Challenges.Where(c => c.ExpiresAt < cutoff).OrderBy(c => c.ExpiresAt).ThenBy(c => c.Id)
            .Select(c => new { c.Id, c.UserId }).Take(100).ToListAsync(ct);
        foreach (var row in expiredMail)
        {
            await using var retirement = await db.Database.BeginTransactionAsync(ct);
            await Sync.SyncRequestScope.LockOwnerAsync(db, row.UserId, ct);
            var original = db.Challenges.Where(c => c.Id == row.Id && c.UserId == row.UserId && c.ExpiresAt < cutoff);
            if (await original.AnyAsync(ct) && AccountIdentityMailRetirement.RemoveLocalCopies(storage, row.Id))
                await original.ExecuteDeleteAsync(ct);
            await retirement.CommitAsync(ct);
        }
        var sender = scope.ServiceProvider.GetService<IIdentityMailSender>();
        if (!storage.LocalMailbox && sender is null) return;
        var lease = Guid.NewGuid();
        var candidates = await db.Challenges.Where(c => !c.Delivered && !c.Consumed && c.ExpiresAt > now &&
                (c.LeaseUntil == null || c.LeaseUntil < now) && !db.AccountDeletionRequests.Any(row => row.UserId == c.UserId))
            .OrderBy(c => c.ExpiresAt).Select(c => new { c.Id, c.UserId }).Take(20).ToListAsync(ct);
        foreach (var candidate in candidates)
        {
            var id = candidate.Id;
            await using (var claim = await db.Database.BeginTransactionAsync(ct))
            {
                await Sync.SyncRequestScope.LockOwnerAsync(db, candidate.UserId, ct);
                if (await db.AccountDeletionRequests.AnyAsync(row => row.UserId == candidate.UserId, ct)) continue;
                now = time.GetUtcNow();
                var claimed = await db.Challenges.Where(c => c.Id == id && c.UserId == candidate.UserId &&
                        !c.Delivered && !c.Consumed && c.ExpiresAt > now && (c.LeaseUntil == null || c.LeaseUntil < now))
                    .ExecuteUpdateAsync(u => u.SetProperty(c => c.LeaseOwner, lease).SetProperty(c => c.LeaseUntil, now.AddMinutes(1)), ct);
                if (claimed != 1) continue;
                await claim.CommitAsync(ct);
            }
            var row = await db.Challenges.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.LeaseOwner == lease, ct);
            if (row is null || row.Consumed || row.ExpiresAt <= time.GetUtcNow()) continue;
            if (sender is not null)
            {
                try
                {
                    var mail = IdentityMailEnvelope.Read(row, scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), sender.Authority);
                    await sender.SubmitAsync(mail, ct);
                }
                catch (Exception error) when (error is IdentityMailUnavailable or CryptographicException or JsonException)
                {
                    // Retain the claim for its bounded lease. The same challenge/key retries, never another token,
                    // recipient or financial action. Do not log protected payloads or remote exception text.
                    continue;
                }
            }
            else
            {
                // Local publication and explicit account retirement share the same original owner lock.
                // An expired/replaced lease cannot recreate a file after retirement has completed.
                await using var local = await db.Database.BeginTransactionAsync(ct);
                await Sync.SyncRequestScope.LockOwnerAsync(db, row.UserId, ct);
                if (await db.AccountDeletionRequests.AnyAsync(request => request.UserId == row.UserId, ct)) continue;
                if (!await db.Challenges.AnyAsync(c => c.Id == id && c.LeaseOwner == lease &&
                        !c.Consumed && !c.Delivered && c.ExpiresAt > time.GetUtcNow() && c.LeaseUntil > time.GetUtcNow(), ct))
                    continue;
                var path = Path.Combine(storage.Root, "mailbox", id.ToString("N"));
                var temporary = path + "." + lease.ToString("N") + ".pending";
                await File.WriteAllTextAsync(temporary, row.ProtectedMail, ct);
                File.Move(temporary, path, overwrite: true);
                await db.Challenges.Where(c => c.Id == id && c.LeaseOwner == lease)
                    .ExecuteUpdateAsync(u => u.SetProperty(c => c.Delivered, true), ct);
                await local.CommitAsync(ct);
                continue;
            }
            await db.Challenges.Where(c => c.Id == id && c.LeaseOwner == lease).ExecuteUpdateAsync(u => u.SetProperty(c => c.Delivered, true), ct);
        }
    }
}
