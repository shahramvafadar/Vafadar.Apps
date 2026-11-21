using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>The local identity challenge stage for a pending account deletion.</summary>
public enum AccountIdentityMailRetirementResult
{
    UnknownReceipt,
    Pending,
    Retired,
}

/// <summary>Invalidates account challenges and removes their local forms without contacting a mail provider.</summary>
public static class AccountIdentityMailRetirement
{
    /// <summary>Consumes the target account's challenges before processing a bounded local retirement batch.</summary>
    public static async Task<AccountIdentityMailRetirementResult> RunAsync(ServiceDbContext db,
        ServiceStorage storage, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new ArgumentException("The original deletion receipt is required.", nameof(receipt));
        var owner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return AccountIdentityMailRetirementResult.UnknownReceipt;

        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await SyncRequestScope.LockOwnerAsync(db, owner, ct);
            if (!await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt && row.UserId == owner, ct))
                return AccountIdentityMailRetirementResult.UnknownReceipt;
            await db.Challenges.Where(row => row.UserId == owner && !row.Consumed)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Consumed, true), ct);
            await transaction.CommitAsync(ct);
        }

        await using var retirement = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        var scope = db.Challenges.Where(row => row.UserId == owner);
        // A claimed but unconfirmed provider send remains represented by its original challenge.
        var eligible = scope.Where(row => row.Consumed && (row.LeaseOwner == null || row.Delivered));
        var targets = await eligible.OrderBy(row => row.ExpiresAt).ThenBy(row => row.Id)
            .Select(row => row.Id).Take(100).ToListAsync(ct);
        foreach (var id in targets)
        {
            ct.ThrowIfCancellationRequested();
            if (RemoveLocalCopies(storage, id))
                await eligible.Where(row => row.Id == id).ExecuteDeleteAsync(ct);
        }
        var pending = await scope.AnyAsync(ct);
        await retirement.CommitAsync(ct);
        return pending ? AccountIdentityMailRetirementResult.Pending : AccountIdentityMailRetirementResult.Retired;
    }

    internal static bool RemoveLocalCopies(ServiceStorage storage, Guid id)
    {
        if (id == Guid.Empty) return false;
        var root = Path.Combine(storage.Root, "mailbox");
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) return false;
        var name = id.ToString("N");
        try
        {
            if (!RemoveFile(Path.Combine(root, name))) return false;
            // Inspect directories as well so a substituted staging path keeps the challenge pending.
            foreach (var path in Directory.EnumerateFileSystemEntries(root, name + ".*.pending"))
            {
                var suffix = Path.GetFileName(path).AsSpan(name.Length);
                if (suffix.Length == 41 && suffix[0] == '.' && suffix[33..].SequenceEqual(".pending") &&
                    Guid.TryParseExact(suffix.Slice(1, 32), "N", out _) && !RemoveFile(path)) return false;
            }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }

    private static bool RemoveFile(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) return false;
            File.Delete(path);
            return true;
        }
        catch (FileNotFoundException) { return true; }
    }
}
