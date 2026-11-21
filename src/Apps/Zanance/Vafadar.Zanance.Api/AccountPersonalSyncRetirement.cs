using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Durable ownership of one unlinked sync piece, independent of its former deleted manifest.</summary>
public sealed class AccountSyncFileRetirement
{
    /// <summary>Original private server-generated piece filename.</summary>
    public Guid FileId { get; set; }
    /// <summary>Original pending account deletion receipt.</summary>
    public Guid ReceiptId { get; set; }
}

/// <summary>The current personal sync stage observed for an original pending account deletion.</summary>
public enum AccountPersonalSyncRetirementResult
{
    /// <summary>The receipt does not identify a pending request.</summary>
    UnknownReceipt,
    /// <summary>Sync piece retirement remains pending.</summary>
    Pending,
    /// <summary>Current personal spaces, device grants and tracked piece files have retired.</summary>
    Retired,
}

/// <summary>Retires personal sync for one account, including the bytes of attachments stored by the service.</summary>
public static class AccountPersonalSyncRetirement
{
    /// <summary>Queues exact piece IDs before cascading rows, then attempts one bounded filesystem pass.</summary>
    public static async Task<AccountPersonalSyncRetirementResult> RunAsync(ServiceDbContext db, ServiceStorage storage, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return AccountPersonalSyncRetirementResult.UnknownReceipt;

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            if (!await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt && row.UserId == owner, ct))
                return AccountPersonalSyncRetirementResult.UnknownReceipt;
            await SyncPieceFiles.LockAsync(db, ct);
            // Same transaction as the cascade: the worker has durable IDs even if the process stops at commit.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AccountSyncFileRetirements" ("FileId", "ReceiptId")
                SELECT pieces."FileId", {receipt}
                FROM "SyncBlobPieces" pieces JOIN "SyncSpaces" spaces ON spaces."Id" = pieces."SpaceId"
                WHERE spaces."UserId" = {owner}
                """, ct);
            await db.Set<SyncSpaceRow>().Where(row => row.UserId == owner).ExecuteDeleteAsync(ct);
            await db.Set<SyncDeviceRow>().Where(row => row.UserId == owner).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
        }

        var queued = db.Set<AccountSyncFileRetirement>().Where(row => row.ReceiptId == receipt);
        await RetireFilesAsync(db, storage, queued, ct);
        return await queued.AnyAsync(ct) ||
            await db.Set<SyncSpaceRow>().AnyAsync(row => row.UserId == owner, ct) ||
            await db.Set<SyncDeviceRow>().AnyAsync(row => row.UserId == owner, ct)
            ? AccountPersonalSyncRetirementResult.Pending : AccountPersonalSyncRetirementResult.Retired;
    }

    internal static async Task RetireFilesAsync(ServiceDbContext db, ServiceStorage storage,
        IQueryable<AccountSyncFileRetirement> candidates, CancellationToken ct)
    {
        if (!await candidates.AnyAsync(ct)) return;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncPieceFiles.LockAsync(db, ct);
        var ids = await candidates.OrderBy(row => row.FileId).Select(row => row.FileId).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            if (id == Guid.Empty || await db.Set<SyncBlobPieceRow>().AnyAsync(row => row.FileId == id, ct)) continue;
            var path = Path.Combine(SyncPieceFiles.RootOf(storage), id.ToString("N"));
            try
            {
                // Refuse substituted directories, links and unreadable paths. A missing file is already retired.
                try
                {
                    if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) continue;
                    File.Delete(path);
                }
                catch (FileNotFoundException) { }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
            await db.Set<AccountSyncFileRetirement>().Where(row => row.FileId == id).ExecuteDeleteAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
