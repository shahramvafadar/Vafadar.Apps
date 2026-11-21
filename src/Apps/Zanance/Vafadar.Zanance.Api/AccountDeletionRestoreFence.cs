using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api;

/// <summary>Immutable minimal receipt markers outside the database snapshot protect restored accounts from resurrection.</summary>
public static class AccountDeletionRestoreFence
{
    internal static async Task RecordAsync(ServiceStorage storage, Guid receipt, CancellationToken ct)
    {
        if (receipt == Guid.Empty) throw new IOException("The original account deletion receipt is required.");
        var root = RootOf(storage.Root);
        Directory.CreateDirectory(root);
        var path = Path.Combine(RootOf(storage.Root), receipt.ToString("N") + ".receipt");
        if (File.Exists(path))
        {
            await ValidateAsync(path, receipt, ct);
            return;
        }
        var pending = path + "." + Guid.NewGuid().ToString("N") + ".pending";
        await using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await file.WriteAsync(Encoding.ASCII.GetBytes(receipt.ToString("D")), ct);
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
            await ValidateAsync(path, receipt, ct);
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
            await ValidateAsync(path, receipt, ct);
            if (await db.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == receipt, ct)) continue;
            if (!await db.AccountDeletionRequests.AnyAsync(row => row.ReceiptId == receipt, ct))
            {
                // A receipt-only marker cannot identify an owner in a backup taken before the request.
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
            await ValidateAsync(path, receipt, ct);
            await RecordAsync(storage, receipt, ct);
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

    private static async Task ValidateAsync(string path, Guid receipt, CancellationToken ct)
    {
        var file = new FileInfo(path);
        if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.Length != 36 ||
            !string.Equals(await File.ReadAllTextAsync(path, ct), receipt.ToString("D"), StringComparison.Ordinal))
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
