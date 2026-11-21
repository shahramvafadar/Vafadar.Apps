using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api;

/// <summary>Immutable minimal receipt markers outside the database snapshot protect restored accounts from resurrection.</summary>
public static class AccountDeletionRestoreFence
{
    private const string OwnerDomain = "zanance-account-restore-v2:";

    internal static Task RecordAsync(ServiceStorage storage, Guid receipt, string owner, CancellationToken ct) =>
        WriteMarkerAsync(storage, receipt, OwnerHash(owner), ct, allowLegacyRetry: true);

    private static async Task WriteMarkerAsync(ServiceStorage storage, Guid receipt, string? ownerHash, CancellationToken ct,
        bool allowLegacyRetry = false)
    {
        if (receipt == Guid.Empty) throw new IOException("The original account deletion receipt is required.");
        var root = RootOf(storage.Root);
        Directory.CreateDirectory(root);
        var path = Path.Combine(RootOf(storage.Root), receipt.ToString("N") + ".receipt");
        if (File.Exists(path))
        {
            var existingOwner = await ReadOwnerHashAsync(path, receipt, ct);
            if (existingOwner != ownerHash && !(allowLegacyRetry && existingOwner is null))
                throw new IOException("The account deletion marker owner differs.");
            return;
        }
        var pending = path + "." + Guid.NewGuid().ToString("N") + ".pending";
        await using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            var body = ownerHash is null ? receipt.ToString("D") : $"2\n{receipt:D}\n{ownerHash}";
            await file.WriteAsync(Encoding.ASCII.GetBytes(body), ct);
            file.Flush(flushToDisk: true);
        }
        _ = RootOf(storage.Root);
        File.Move(pending, path);
    }

    /// <summary>Requires a completion for every current marker before the service starts handling requests.</summary>
    public static async Task VerifyDatabaseAsync(ServiceDbContext db, ServiceStorage storage, CancellationToken ct)
    {
        foreach (var (path, receipt) in Markers(storage.Root))
        {
            _ = await ReadOwnerHashAsync(path, receipt, ct);
            if (!await db.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == receipt, ct))
                throw new IOException("Account deletion completion is missing. Reconcile the private restore before starting the service.");
        }
    }

    /// <summary>Reapplies retained, matched deletion requests to an isolated restored service database.</summary>
    public static async Task<int> ReconcileAsync(ServiceDbContext db, ServiceStorage storage, TimeProvider time, CancellationToken ct)
    {
        var outstanding = 0;
        foreach (var (path, receipt) in Markers(storage.Root))
        {
            var ownerHash = await ReadOwnerHashAsync(path, receipt, ct);
            if (await db.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == receipt, ct)) continue;
            if (!await BindRestoredRequestAsync(db, receipt, ownerHash, time, ct))
            {
                outstanding++;
                continue;
            }

            await AccountPrivateObjectRetirement.RunAsync(db, storage, receipt, ct);
            await AccountPersonalSyncRetirement.RunAsync(db, storage, receipt, ct);
            await AccountIdentityMailRetirement.RunAsync(db, storage, receipt, ct);
            var result = await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct);
            if (result.Status != AccountIdentityErasureStatus.Completed) outstanding++;
        }
        return outstanding;
    }

    private static async Task<bool> BindRestoredRequestAsync(ServiceDbContext db, Guid receipt, string? ownerHash,
        TimeProvider time, CancellationToken ct)
    {
        var pendingOwner = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == receipt)
            .Select(row => row.UserId).SingleOrDefaultAsync(ct);
        if (pendingOwner is not null) return ownerHash is null || OwnerHash(pendingOwner) == ownerHash;
        if (ownerHash is null) return false;

        var matches = await db.Database.SqlQuery<string>($"""
            SELECT "Id" AS "Value" FROM "AspNetUsers"
            WHERE sha256(convert_to({OwnerDomain} || "Id", 'UTF8')) = decode({ownerHash}, 'hex')
            LIMIT 2
            """).ToListAsync(ct);
        if (matches.Count != 1) return false;
        var owner = matches[0];
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await SyncRequestScope.LockOwnerAsync(db, owner, ct);
        if (!await db.Users.AnyAsync(row => row.Id == owner, ct)) return false;
        var original = await db.AccountDeletionRequests.AsNoTracking().SingleOrDefaultAsync(row => row.UserId == owner, ct);
        if (original is not null) return original.ReceiptId == receipt;
        if (await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt, ct)) return false;
        db.AccountDeletionRequests.Add(new() { UserId = owner, ReceiptId = receipt, RequestedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static string OwnerHash(string owner) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(OwnerDomain + owner)));

    /// <summary>Merges only current opaque receipt markers into a distinct restored private root.</summary>
    public static async Task<int> MergeAsync(string currentPrivateRoot, string restoredPrivateRoot, CancellationToken ct = default)
    {
        if (!Path.IsPathFullyQualified(currentPrivateRoot) || !Path.IsPathFullyQualified(restoredPrivateRoot))
            throw new IOException("Explicit private roots required.");
        var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(currentPrivateRoot));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(restoredPrivateRoot));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!Directory.Exists(source) || !Directory.Exists(target) || string.Equals(source, target, comparison) ||
            source.StartsWith(target + Path.DirectorySeparatorChar, comparison) || target.StartsWith(source + Path.DirectorySeparatorChar, comparison))
            throw new IOException("Distinct original and restored private roots required.");
        var storage = new ServiceStorage { Root = target, LocalMailbox = false };
        var merged = 0;
        foreach (var (path, receipt) in Markers(source))
        {
            var ownerHash = await ReadOwnerHashAsync(path, receipt, ct);
            await WriteMarkerAsync(storage, receipt, ownerHash, ct);
            merged++;
        }
        return merged;
    }

    private static IEnumerable<(string Path, Guid Receipt)> Markers(string storage)
    {
        var root = RootOf(storage);
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*.receipt"))
        {
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id) || id == Guid.Empty)
                throw new IOException("Invalid account deletion marker filename.");
            yield return (path, id);
        }
    }

    private static async Task<string?> ReadOwnerHashAsync(string path, Guid receipt, CancellationToken ct)
    {
        var file = new FileInfo(path);
        if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.Length is not (36 or 103))
            throw new IOException("Invalid account deletion marker.");
        var text = await File.ReadAllTextAsync(path, ct);
        var original = receipt.ToString("D");
        if (string.Equals(text, original, StringComparison.Ordinal)) return null;
        var prefix = $"2\n{original}\n";
        if (text.Length == 103 && text.StartsWith(prefix, StringComparison.Ordinal))
        {
            var ownerHash = text[prefix.Length..];
            if (ownerHash.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f')) return ownerHash;
        }
        throw new IOException("Invalid account deletion marker.");
    }

    private static string RootOf(string storage)
    {
        var root = Path.Combine(storage, "account-deletions");
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            if (directory.LinkTarget is not null || directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("The account deletion marker directory is redirected.");
        if (File.Exists(root)) throw new IOException("Invalid account deletion marker directory.");
        return root;
    }
}
