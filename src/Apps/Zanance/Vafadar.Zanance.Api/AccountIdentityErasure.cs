using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

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
        if (blocked != AccountDeletionDependencies.None)
            return new(AccountIdentityErasureStatus.DependenciesRemain, blocked);

        // Publish the independent restore authority before removing the original account from PostgreSQL.
        await AccountDeletionRestoreFence.RecordAsync(storage, receipt, ct);
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
