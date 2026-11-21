using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api;

/// <summary>Minimal proof of completed deletion from the active service, with no original subject or email.</summary>
public sealed class AccountDeletionCompletion
{
    /// <summary>The original opaque pending receipt.</summary>
    public Guid ReceiptId { get; set; }
    /// <summary>UTC instant the service committed the identity deletion.</summary>
    public DateTimeOffset CompletedAt { get; set; }
}

/// <summary>Operator result for an explicitly reviewed active-service identity deletion.</summary>
public enum AccountIdentityErasureStatus
{
    UnknownReceipt,
    DependenciesRemain,
    Completed,
}

/// <summary>Content-free status and any remaining active service dependencies.</summary>
public sealed record AccountIdentityErasureResult(AccountIdentityErasureStatus Status,
    AccountDeletionDependencies Dependencies = AccountDeletionDependencies.None);

/// <summary>Deletes the original identity when all non-cascading effects have been cleared.</summary>
public static class AccountIdentityErasure
{
    /// <summary>Reconciles completed receipt retries and refuses unresolved dependencies under the owner lock.</summary>
    public static async Task<AccountIdentityErasureResult> CompleteAsync(
        ServiceDbContext db, ServiceStorage storage, TimeProvider time, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return await KnownResultAsync(db, receipt, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        if (!await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt && row.UserId == owner, ct))
            return await KnownResultAsync(db, receipt, ct);

        // Sessions and already-retired private manifests are handled by this transaction. Other groups need
        // their own erasure or retention decision, including identity mail with a potentially external effect.
        var present = await AccountDeletionInventory.ReadOwnerAsync(db, owner, receipt, ct);
        var blocked = present & ~(AccountDeletionDependencies.Sessions | AccountDeletionDependencies.PrivateObjects);
        if ((present & AccountDeletionDependencies.PrivateObjects) != 0 &&
            !await PrivateObjectsRetiredAsync(db, storage, owner, ct))
            blocked |= AccountDeletionDependencies.PrivateObjects;
        if (blocked == AccountDeletionDependencies.Household)
        {
            await RemoveUnpublishedHouseholdReservationAsync(db, owner, ct);
            await RemoveRetiredHouseholdReferencesAsync(db, owner, ct);
            if (!await AccountDeletionInventory.HasHouseholdAsync(db, owner, ct))
                blocked = AccountDeletionDependencies.None;
        }
        if (blocked != AccountDeletionDependencies.None)
            return new(AccountIdentityErasureStatus.DependenciesRemain, blocked);

        // Publish the independent restore authority before removing the original account from PostgreSQL.
        await AccountDeletionRestoreFence.RecordAsync(storage, receipt, owner, ct);
        await db.Objects.Where(row => row.UserId == owner).ExecuteDeleteAsync(ct);
        if (await db.Users.Where(row => row.Id == owner).ExecuteDeleteAsync(ct) != 1)
            throw new InvalidOperationException("The original account could not be retired.");
        // Identity rows, sessions and the original pending request cascade with the account.
        db.Set<AccountDeletionCompletion>().Add(new() { ReceiptId = receipt, CompletedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(AccountIdentityErasureStatus.Completed);
    }

    private static async Task<AccountIdentityErasureResult> KnownResultAsync(ServiceDbContext db, Guid receipt, CancellationToken ct) =>
        new(await db.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == receipt, ct)
            ? AccountIdentityErasureStatus.Completed : AccountIdentityErasureStatus.UnknownReceipt);

    private static async Task RemoveUnpublishedHouseholdReservationAsync(ServiceDbContext db, string owner, CancellationToken ct)
    {
        var emptyHead = new string('0', 64);
        var candidates = db.Set<HouseholdSpaceRow>().Where(row => row.OwnerId == owner && !row.Deleted &&
            !row.StorageRetired && !row.RotationRequired && row.DeletionOperationId == null && row.HostExpiresAt == null &&
            row.MembershipVersion == 0 && row.KeyEpoch == 0 && row.Cursor == 0 && row.AuditBytes == 0 &&
            row.MembershipHead == emptyHead && row.PublicationHead == emptyHead);
        var id = await candidates.Select(row => (Guid?)row.Id).SingleOrDefaultAsync(ct);
        if (id is not { } space) return;
        await HouseholdRequestScope.LockRoomAsync(db, space, ct);
        if (!await candidates.AnyAsync(row => row.Id == space, ct) || await db.HouseholdDeletions.ReadAsync(space, ct) is not null)
            return;

        var members = db.Set<HouseholdMemberRow>().Where(row => row.SpaceId == space);
        var devices = db.Set<HouseholdDeviceRow>().Where(row => row.SpaceId == space);
        // Reserve runs before genesis and creates only one pending owner/member/device.
        if (await members.CountAsync(ct) != 1 || !await members.AnyAsync(row => row.UserId == owner &&
                row.Role == HouseholdRole.Owner && !row.Accepted && !row.Revoked && row.JoinedVersion == 0, ct) ||
            await devices.CountAsync(ct) != 1 || !await devices.AnyAsync(row => row.UserId == owner && !row.Approved &&
                !row.Revoked && row.AdmissionProof == null && row.AdmissionOperationId == null &&
                row.SessionProof == null && row.SessionOperationId == null, ct) ||
            await HasHouseholdHistoryAsync(db, space, ct))
            return;

        await devices.ExecuteDeleteAsync(ct);
        await members.ExecuteDeleteAsync(ct);
        await candidates.Where(row => row.Id == space).ExecuteDeleteAsync(ct);
        // The account restore marker is written before this transaction can commit.
    }

    private static async Task<bool> HasHouseholdHistoryAsync(ServiceDbContext db, Guid space, CancellationToken ct) =>
        await db.Set<HouseholdInvitationRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdMembershipEventRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRoundRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRecordRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdEventRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdOwnershipRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRecoveryRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRecoveryEpochRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRecoverySeedRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdRecoverySeedEpochRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdOwnerRootGrantRow>().AnyAsync(row => row.SpaceId == space, ct) ||
        await db.Set<HouseholdAuditRow>().AnyAsync(row => row.SpaceId == space, ct);

    private static async Task RemoveRetiredHouseholdReferencesAsync(ServiceDbContext db, string owner, CancellationToken ct)
    {
        var retired = db.Set<HouseholdSpaceRow>().Where(row => row.OwnerId == owner && row.Deleted &&
            row.StorageRetired && row.DeletionOperationId != null);
        var rooms = await retired.OrderBy(row => row.Id).Select(row => row.Id).ToListAsync(ct);
        foreach (var id in rooms)
        {
            await HouseholdRequestScope.LockRoomAsync(db, id, ct);
            var room = await retired.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, ct);
            if (room is null || room.OwnerSigningKey.Length != 0 || room.OwnerWrappingKey.Length != 0 ||
                await db.Set<HouseholdOwnershipRow>().AnyAsync(row => row.SpaceId == id, ct)) continue;
            var marker = await db.HouseholdDeletions.ReadAsync(id, ct);
            if (marker is null) continue;
            // Verify the independent marker against the exact recorded room declaration.
            HouseholdLifecycleStore.Apply(room, marker);
            // The room's child foreign keys still refuse if the ordinary retirement did not clear them.
            // Dropping the owner's SQL tombstone does not remove the independent restore marker.
            await retired.Where(row => row.Id == id).ExecuteDeleteAsync(ct);
        }
    }

    private static async Task<bool> PrivateObjectsRetiredAsync(ServiceDbContext db, ServiceStorage storage, string owner, CancellationToken ct)
    {
        if (await db.Objects.AnyAsync(row => row.UserId == owner && (!row.Deleted || !row.StorageRetired), ct)) return false;
        await foreach (var id in db.Objects.Where(row => row.UserId == owner).Select(row => row.Id).AsAsyncEnumerable().WithCancellation(ct))
        {
            // A restored file or an inaccessible path cannot be accepted as physical retirement.
            try { _ = File.GetAttributes(storage.ObjectPath(id)); return false; }
            catch (FileNotFoundException) { }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
        }
        return true;
    }
}
