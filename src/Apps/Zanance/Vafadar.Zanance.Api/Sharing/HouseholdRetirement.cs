using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Bounded durable room retirement and deletion-ledger reconciliation; it never takes an identity lock after a room lock.</summary>
public sealed class HouseholdRetirement(IDbContextFactory<ServiceDbContext> factory, IHouseholdDeletionLedger ledger, TimeProvider time, ILogger<HouseholdRetirement>? logger = null)
{
    private readonly SemaphoreSlim passes = new(1, 1);
    private int markerOffset;
    private int roomOffset;

    /// <summary>Rechecks actual expiry under each original room lock, permanently declares120-day expiry, then retires only that room's records.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        await passes.WaitAsync(ct);
        try
        {
            var now = time.GetUtcNow(); var cutoff = now >= DateTimeOffset.MinValue + HouseholdAccessPolicy.TotalRetentionInterval
                ? now - HouseholdAccessPolicy.TotalRetentionInterval : DateTimeOffset.MinValue;
            await using var candidates = await factory.CreateDbContextAsync(ct);
            var rooms = await candidates.Set<HouseholdSpaceRow>().AsNoTracking().Where(row => !row.StorageRetired && (row.Deleted || row.HostExpiresAt <= cutoff))
                .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Select(row => row.Id).Skip(roomOffset).Take(20).ToArrayAsync(ct);
            // Rotate pending failures as well as successes: twenty unverifiable rooms
            // must not indefinitely starve a later unrelated expiry (D-294).
            roomOffset = NextOffset(roomOffset, rooms.Length, 20);
            var markers = ledger.Inspect(markerOffset, 20);
            markerOffset = NextOffset(markerOffset, markers.Scanned, 20);
            var unavailable = markers.Scanned - markers.Spaces.Count;
            foreach (var id in rooms.Concat(markers.Spaces).Distinct())
            {
                try
                {
                    await using var db = await factory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
                    await HouseholdRequestScope.LockRoomAsync(db, id, ct);
                    var room = await db.Set<HouseholdSpaceRow>().SingleOrDefaultAsync(row => row.Id == id, ct);
                    if (room is null) continue;
                    var marker = await ledger.ReadAsync(id, ct); now = time.GetUtcNow();
                    if (marker is null && HouseholdAccessPolicy.Retention(now, room.HostExpiresAt, false) == HouseholdRetentionState.DeletionDue)
                    {
                        var request = "household-retention-ended-v1:" + id.ToString("N") + ":" + room.HostExpiresAt!.Value.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                        marker = await ledger.RecordAsync(new(1, id, Guid.NewGuid(), HouseholdDeletionLedger.Actor(room.OwnerId),
                            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(request))), now.ToUnixTimeSeconds(), HouseholdDeletionReason.RetentionEnded), ct);
                    }
                    if (marker is null)
                    {
                        if (room.Deleted) throw new IOException("Original private deletion marker is missing.");
                        continue; // A verified renewal that won this lock before permanent intent remains active.
                    }
                    HouseholdLifecycleStore.Apply(room, marker);
                    if (!room.StorageRetired)
                    {
                        // Exact room predicates and FK order retain every unrelated member's own
                        // personal spaces/objects and other household records. PostgreSQL commits
                        // this metadata/ciphertext retirement atomically; interrupted passes resume.
                        await db.Set<HouseholdEventRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRecordRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdPartRow>().Where(row => db.Set<HouseholdRoundRow>().Any(round => round.Id == row.RoundId && round.SpaceId == id)).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRoundRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdKeyGrantRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdOwnerRootGrantRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRecoverySeedEpochRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRecoverySeedRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRecoveryEpochRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdRecoveryRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdOwnershipRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdAuditRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdMembershipEventRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdInvitationRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdDeviceRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        await db.Set<HouseholdMemberRow>().Where(row => row.SpaceId == id).ExecuteDeleteAsync(ct);
                        room.OwnerSigningKey = room.OwnerWrappingKey = ""; room.AuditBytes = 0; room.StorageRetired = true;
                    }
                    await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    // The exact transaction is disposed/rolled back before continuing.
                    // Unreadable intent never becomes absence, replacement or new consent.
                    unavailable++;
                }
            }
            if (unavailable != 0) logger?.LogWarning("Household retirement preserved {Count} unverifiable private entries; unrelated rooms continue.", unavailable);
        }
        finally { passes.Release(); }
    }

    /// <summary>Finite raw-entry cursor advancement with an overflow-safe wrap.</summary>
    private static int NextOffset(int offset, int count, int maximum) => count < maximum || offset > int.MaxValue - maximum ? 0 : offset + count;
}
