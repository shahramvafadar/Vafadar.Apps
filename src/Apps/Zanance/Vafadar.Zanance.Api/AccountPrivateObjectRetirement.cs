using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Content-free progress of the private-object stage of an original account deletion.</summary>
public enum AccountPrivateObjectRetirementResult
{
    /// <summary>No pending request matched the receipt.</summary>
    UnknownReceipt,
    /// <summary>Additional object files remain for the retirement worker.</summary>
    Pending,
    /// <summary>All currently registered private objects for this account have been retired.</summary>
    Retired,
}

/// <summary>Retires one pending account's private objects through the same durable queue used by ordinary deletion.</summary>
public static class AccountPrivateObjectRetirement
{
    /// <summary>Hides currently registered objects atomically, then processes one bounded batch of their files.</summary>
    public static async Task<AccountPrivateObjectRetirementResult> RunAsync(ServiceDbContext db, ServiceStorage storage, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return AccountPrivateObjectRetirementResult.UnknownReceipt;

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            if (!await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt && row.UserId == owner, ct))
                return AccountPrivateObjectRetirementResult.UnknownReceipt;
            await db.Objects.Where(row => row.UserId == owner && !row.Deleted)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Deleted, true), ct);
            await transaction.CommitAsync(ct);
        }

        var originals = db.Objects.Where(row => row.UserId == owner);
        await RetireFilesAsync(db, storage, originals, ct);
        return await originals.AnyAsync(row => !row.Deleted || !row.StorageRetired, ct)
            ? AccountPrivateObjectRetirementResult.Pending
            : AccountPrivateObjectRetirementResult.Retired;
    }

    internal static async Task RetireFilesAsync(ServiceDbContext db, ServiceStorage storage, IQueryable<PrivateObject> candidates, CancellationToken ct)
    {
        var ids = await candidates.Where(row => row.Deleted && !row.StorageRetired)
            .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Select(row => row.Id).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            // An absent file is already retired. Persist only after the filesystem operation returns.
            try { File.Delete(storage.ObjectPath(id)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Keep this committed tombstone pending. Other files in the batch can still retire.
                continue;
            }
            await db.Objects.Where(row => row.Id == id && row.Deleted && !row.StorageRetired)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.StorageRetired, true), ct);
        }
    }
}
