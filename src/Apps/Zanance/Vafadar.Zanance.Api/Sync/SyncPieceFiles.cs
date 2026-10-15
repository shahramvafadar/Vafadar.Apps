using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>One storage-publication order shared by original piece writes and orphan collection across service workers.</summary>
internal static class SyncPieceFiles
{
    /// <summary>Acquires a transaction-scoped storage namespace after any owner lock and retains it until commit/rollback.</summary>
    internal static Task LockAsync(ServiceDbContext db, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Private piece publication requires a transaction.");
        // The two-integer advisory namespace is distinct from the one-bigint identity/owner namespace.
        return db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1685220206, 1)", ct);
    }

    /// <summary>Resolves only the service's owned piece directory, refusing redirected ancestors.</summary>
    internal static string RootOf(ServiceStorage storage)
    {
        var root = Path.Combine(storage.Root, "sync-pieces");
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("The private piece directory is redirected.");
        return root;
    }
}

/// <summary>Collects only aged server-named ciphertext without a committed manifest; never financial history or active staging.</summary>
internal sealed class SyncPieceMaintenance(ServiceStorage storage, TimeProvider time)
{
    private readonly SemaphoreSlim pass = new(1, 1);
    private int scanOffset;

    /// <summary>Rotates bounded inspection batches so live first entries cannot indefinitely hide later orphan files.</summary>
    internal async Task RunAsync(ServiceDbContext db, CancellationToken ct)
    {
        await pass.WaitAsync(ct);
        try
        {
            var root = SyncPieceFiles.RootOf(storage); if (!Directory.Exists(root)) return;
            const int maximum = 100;
            var candidates = Directory.EnumerateFiles(root).Skip(scanOffset).Take(maximum).ToArray();
            scanOffset = candidates.Length < maximum ? 0 : checked(scanOffset + candidates.Length);
            if (candidates.Length == 0) return;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            // No owner lock is acquired after this gate. Original publication can complete before we decide a file has no owner.
            await SyncPieceFiles.LockAsync(db, ct);
            var cutoff = time.GetUtcNow().AddDays(-1).UtcDateTime;
            foreach (var path in candidates)
            {
                ct.ThrowIfCancellationRequested(); _ = SyncPieceFiles.RootOf(storage);
                if (!Guid.TryParseExact(Path.GetFileName(path), "N", out var id) || id == Guid.Empty) continue;
                var info = new FileInfo(path);
                if (!info.Exists || (info.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0 || info.LastWriteTimeUtc >= cutoff) continue;
                if (await db.Set<SyncBlobPieceRow>().AnyAsync(piece => piece.FileId == id, ct)) continue;
                File.Delete(path);
            }
            await transaction.CommitAsync(ct);
        }
        finally { pass.Release(); }
    }
}
