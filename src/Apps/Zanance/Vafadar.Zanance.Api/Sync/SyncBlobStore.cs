using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Verified immutable private transfer pieces; only encrypted bytes touch the service filesystem.</summary>
internal sealed class SyncBlobStore(ServiceDbContext db, ServiceStorage storage, IDataProtectionProvider protection)
{
    /// <summary>Bounds both transport and decryption allocations independently of attachment length.</summary>
    internal const int PieceBytes = 2 * 1024 * 1024;

    /// <summary>Idempotently declares the exact space-owned content address and byte count.</summary>
    internal async Task<SyncBlobRow> DeclareAsync(SyncRequestScope scope, SyncSpaceRow space, string hash, long length, CancellationToken ct)
    {
        RequireAuthority(scope, space, hash);
        if (length is < 1 or > Core.Ledger.EntryAttachment.MaxBytes) throw new SyncProtocolRefusal(413, "BlobBound");
        var blob = await db.Set<SyncBlobRow>().SingleOrDefaultAsync(row => row.SpaceId == space.Id && row.Hash == hash, ct);
        if (blob is not null)
        {
            if (blob.Length != length) throw new SyncProtocolRefusal(409, "AlteredBlob");
            return blob;
        }
        var total = await db.Set<SyncBlobRow>().Where(row => row.SpaceId == space.Id).SumAsync(row => row.Length, ct);
        if (total + length > SyncGraphValidation.MaximumProfileBytes || await db.Set<SyncBlobRow>().CountAsync(row => row.SpaceId == space.Id, ct) >= 10000)
            throw new SyncProtocolRefusal(413, "BlobStorageBound");
        var ownerBytes = await (from retained in db.Set<SyncBlobRow>() join owned in db.Set<SyncSpaceRow>() on retained.SpaceId equals owned.Id
            where owned.UserId == scope.Owner select retained.Length).SumAsync(ct);
        if (ownerBytes + length > storage.SyncLimits.BlobBytesPerOwner) throw new SyncProtocolRefusal(413, "OwnerBlobStorageBound");
        blob = new() { SpaceId = space.Id, Hash = hash, Length = length, Parts = (int)((length + PieceBytes - 1) / PieceBytes) };
        db.Add(blob); return blob;
    }

    /// <summary>Publishes encrypted immutable bytes before the transactional manifest; a lost transaction leaves only an owned orphan.</summary>
    internal async Task PutAsync(SyncRequestScope scope, SyncSpaceRow space, SyncBlobRow blob, int index, byte[] bytes, CancellationToken ct)
    {
        RequireAuthority(scope, space, blob.Hash);
        if (blob.SpaceId != space.Id || index < 0 || index >= blob.Parts || bytes.Length != ExpectedLength(blob, index))
            throw new SyncProtocolRefusal(400, "InvalidBlobPiece");
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var previous = await db.Set<SyncBlobPieceRow>().SingleOrDefaultAsync(row => row.SpaceId == space.Id && row.BlobHash == blob.Hash && row.Index == index, ct);
        if (previous is not null)
        {
            if (previous.Hash != digest || previous.Length != bytes.Length) throw new SyncProtocolRefusal(409, "AlteredBlobPiece");
            var retained = await ReadAsync(scope, space, previous, ct);
            CryptographicOperations.ZeroMemory(retained); return;
        }
        if (blob.Complete) throw new SyncProtocolRefusal(409, "CompletedBlob");
        var piece = new SyncBlobPieceRow { SpaceId = space.Id, BlobHash = blob.Hash, Index = index,
            FileId = Guid.NewGuid(), Length = bytes.Length, Hash = digest };
        // Hold through the caller's manifest commit. Collection cannot mistake a live uncommitted publication for an orphan.
        await SyncPieceFiles.LockAsync(db, ct);
        var path = PathOf(piece.FileId);
        var encrypted = Protector(scope, space.Id, blob.Hash, index).Protect(bytes);
        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
        { await file.WriteAsync(encrypted, ct); file.Flush(flushToDisk: true); }
        db.Add(piece);
    }

    /// <summary>Reopens all actual ciphertext pieces and verifies the complete concatenated digest before marking ready.</summary>
    internal async Task<SyncBlob> CompleteAsync(SyncRequestScope scope, SyncSpaceRow space, SyncBlobRow blob, CancellationToken ct)
    {
        RequireAuthority(scope, space, blob.Hash);
        var pieces = await PiecesAsync(space.Id, blob.Hash, ct);
        if (pieces.Length != blob.Parts || pieces.Where((piece, index) => piece.Index != index || piece.Length != ExpectedLength(blob, index)).Any())
            throw new SyncProtocolRefusal(409, "IncompleteBlob");
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long length = 0;
        foreach (var piece in pieces)
        {
            var bytes = await ReadAsync(scope, space, piece, ct);
            try { digest.AppendData(bytes); length += bytes.Length; }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        if (length != blob.Length || Convert.ToHexStringLower(digest.GetHashAndReset()) != blob.Hash)
            throw new SyncProtocolRefusal(409, "BlobDigestMismatch");
        blob.Complete = true;
        return new(blob.Hash, blob.Length, pieces.Select(piece => piece.FileId).ToArray());
    }

    /// <summary>Reopens an owned piece, refusing absent, redirected, oversized or tampered storage before publication.</summary>
    internal async Task<byte[]> ReadAsync(SyncRequestScope scope, SyncSpaceRow space, SyncBlobPieceRow piece, CancellationToken ct)
    {
        RequireAuthority(scope, space, piece.BlobHash);
        if (piece.SpaceId != space.Id || piece.Length is < 1 or > PieceBytes) throw new SyncProtocolRefusal(503, "BlobStorageUnavailable");
        var path = PathOf(piece.FileId); var info = new FileInfo(path);
        if (!info.Exists || info.Length > PieceBytes + 65536 || (info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new SyncProtocolRefusal(503, "BlobStorageUnavailable");
        var bytes = Protector(scope, space.Id, piece.BlobHash, piece.Index).Unprotect(await File.ReadAllBytesAsync(path, ct));
        if (bytes.Length != piece.Length || Convert.ToHexStringLower(SHA256.HashData(bytes)) != piece.Hash)
        { CryptographicOperations.ZeroMemory(bytes); throw new SyncProtocolRefusal(503, "BlobStorageUnavailable"); }
        return bytes;
    }

    /// <summary>Returns the confirmed persisted piece prefix without revealing unverified bytes.</summary>
    internal async Task<SyncBlobStatus> StatusAsync(SyncBlobRow blob, CancellationToken ct) => new(blob.Hash, blob.Length, blob.Parts,
        await db.Set<SyncBlobPieceRow>().AsNoTracking().Where(row => row.SpaceId == blob.SpaceId && row.BlobHash == blob.Hash)
            .OrderBy(row => row.Index).Select(row => row.Index).ToArrayAsync(ct), blob.Complete);

    private Task<SyncBlobPieceRow[]> PiecesAsync(Guid space, string hash, CancellationToken ct) => db.Set<SyncBlobPieceRow>().AsNoTracking()
        .Where(row => row.SpaceId == space && row.BlobHash == hash).OrderBy(row => row.Index).ToArrayAsync(ct);

    private static int ExpectedLength(SyncBlobRow blob, int index) => (int)Math.Min(PieceBytes, blob.Length - (long)index * PieceBytes);

    private string PathOf(Guid id)
    {
        if (id == Guid.Empty) throw new SyncProtocolRefusal(503, "BlobStorageUnavailable");
        var root = SyncPieceFiles.RootOf(storage);
        Directory.CreateDirectory(root);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new SyncProtocolRefusal(503, "BlobStorageUnavailable");
        return Path.Combine(root, id.ToString("N"));
    }

    private IDataProtector Protector(SyncRequestScope scope, Guid space, string hash, int index) => protection.CreateProtector(
        "personal-sync-blob-piece-v1", scope.Owner, space.ToString("N"), hash, index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static void RequireAuthority(SyncRequestScope scope, SyncSpaceRow space, string hash)
    {
        if (scope.AuthorizedDeviceId is not { } device || space.UserId != scope.Owner || !SyncProtocolDigest.IsDigest(hash) ||
            space.Initializing && space.BootstrapDeviceId != device) throw new SyncProtocolRefusal(404, "BlobUnavailable");
    }
}
