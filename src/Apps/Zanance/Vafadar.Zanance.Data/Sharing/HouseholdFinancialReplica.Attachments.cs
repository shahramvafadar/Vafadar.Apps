using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Loads only one owned original piece from authenticated retained ciphertext under fresh captured native authority.</summary>
internal interface IHouseholdAttachmentArchive
{
    /// <summary>Returns a complete original2MiB piece; no global plaintext cache/connection is retained.</summary>
    Task<byte[]> ReadPieceAsync(string hash, long length, int index, CancellationToken ct);
}

/// <summary>Reads one owned128KiB original source piece without materializing all outgoing attachments.</summary>
internal interface IHouseholdAttachmentSource
{
    /// <summary>Only exact frozen original content hashes are available from this source.</summary>
    IEnumerable<string> Hashes { get; }
    /// <summary>Returns one exact owned piece; the sealer clears it after signing/encryption and verifies the complete original hash before publication.</summary>
    byte[] ReadPiece(string hash, long length, int index);
}

/// <summary>Original decoded source identity collected only after a complete publication passes financial/content verification.</summary>
internal sealed record HouseholdAttachmentLocator(string Hash, long Length, int PieceIndex, long Cursor, int EventIndex, int ElementIndex, string PieceHash);

public sealed partial class HouseholdFinancialReplica : IDisposable
{
    private const int EncryptedAttachmentPieceBytes = 128 * 1024;
    private const int OriginalAttachmentPieceBytes = 2 * 1024 * 1024;
    private Dictionary<(string Hash, int Index), StoredAttachmentPiece> attachments = [];
    private Dictionary<string, long> attachmentLengths = new(StringComparer.Ordinal);
    private bool disposed;
    /// <summary>Reference/protocol callers retain ownership of their source arrays; bounded copies feed the same sealer as protected storage.</summary>
    private sealed class MemoryAttachmentSource(IReadOnlyDictionary<string, byte[]> values) : IHouseholdAttachmentSource
    {
        private readonly IReadOnlyDictionary<string, byte[]> source = values ?? throw new ArgumentNullException(nameof(values));
        /// <inheritdoc />
        public IEnumerable<string> Hashes => source.Keys;
        /// <inheritdoc />
        public byte[] ReadPiece(string hash, long length, int index)
        {
            if (!source.TryGetValue(hash, out var bytes) || bytes is null || bytes.LongLength != length || index < 0 ||
                (long)index * EncryptedAttachmentPieceBytes >= length) throw Refusal();
            var offset = index * EncryptedAttachmentPieceBytes;
            return bytes.AsSpan(offset, (int)Math.Min(EncryptedAttachmentPieceBytes, length - offset)).ToArray();
        }
    }
    private readonly IHouseholdAttachmentArchive? archive;
    private readonly List<HouseholdAttachmentLocator> attachmentSources = [];
    /// <summary>Only accepted source locators are handed to the protected journal after full verification; they contain no attachment bytes.</summary>
    internal HouseholdAttachmentLocator[] DrainAttachmentSources()
    { var result = attachmentSources.ToArray(); attachmentSources.Clear(); return result; }
    /// <summary>Actual owned attachment-buffer footprint for scoped native qualification, not a process-memory claim.</summary>
    internal long ResidentAttachmentBytes => attachments.Values.Sum(piece => (long)piece.Bytes.Length);

    /// <summary>Reads one original piece without keeping unreferenced historical content in memory.</summary>
    public async Task<byte[]> ReadAttachmentPieceAsync(string hash, int index, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!SyncProtocolDigest.IsDigest(hash) || !attachmentLengths.TryGetValue(hash, out var length) || index < 0 ||
            (long)index * OriginalAttachmentPieceBytes >= length) throw Refusal();
        var first = index * (OriginalAttachmentPieceBytes / EncryptedAttachmentPieceBytes);
        var last = (int)((Math.Min(length, (long)(index + 1) * OriginalAttachmentPieceBytes) - 1) / EncryptedAttachmentPieceBytes);
        if (Enumerable.Range(first, last - first + 1).All(piece => attachments.ContainsKey((hash, piece)))) return ReadAttachmentPiece(hash, index);
        if (archive is null) throw Refusal();
        var result = await archive.ReadPieceAsync(hash, length, index, ct);
        if (result.Length != Math.Min(OriginalAttachmentPieceBytes, length - (long)index * OriginalAttachmentPieceBytes))
        { CryptographicOperations.ZeroMemory(result); throw Refusal(); }
        return result;
    }

    /// <summary>Owned bounded original bytes; diagnostic formatting never includes their content.</summary>
    private sealed record StoredAttachmentPiece(long Length, byte[] Bytes)
    { public override string ToString() => nameof(StoredAttachmentPiece); }

    /// <summary>Returns original complete manifest shape only for previously verified content, without publishing its digest to the service.</summary>
    public SyncBlob AttachmentManifest(string hash)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!SyncProtocolDigest.IsDigest(hash) || !attachmentLengths.TryGetValue(hash, out var length)) throw Refusal();
        var count = (int)((length + OriginalAttachmentPieceBytes - 1) / OriginalAttachmentPieceBytes);
        // Original application code checks opaque nonempty/distinct part identities, but reads pieces by
        // hash/index. These deterministic local receipt ids are not service routes, grants or financial ids.
        var parts = Enumerable.Range(0, count).Select(index =>
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(FormattableString.Invariant($"Zanance household local attachment receipt v1:{hash}:{index}"));
            return new Guid(SHA256.HashData(bytes).AsSpan(0, 16));
        }).ToArray();
        return new(hash, length, parts);
    }

    /// <summary>Reads one currently resident owned original2MiB piece; authenticated retained history uses the asynchronous reader.</summary>
    public byte[] ReadAttachmentPiece(string hash, int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!SyncProtocolDigest.IsDigest(hash) || !attachmentLengths.TryGetValue(hash, out var length) || index < 0 ||
            (long)index * OriginalAttachmentPieceBytes >= length) throw Refusal();
        var offset = (long)index * OriginalAttachmentPieceBytes;
        var result = new byte[(int)Math.Min(OriginalAttachmentPieceBytes, length - offset)];
        for (var written = 0; written < result.Length;)
        {
            var partIndex = (int)((offset + written) / EncryptedAttachmentPieceBytes);
            var pieceOffset = (int)((offset + written) % EncryptedAttachmentPieceBytes);
            if (!attachments.TryGetValue((hash, partIndex), out var part))
            { CryptographicOperations.ZeroMemory(result); throw Refusal(); }
            var count = Math.Min(result.Length - written, part.Bytes.Length - pieceOffset);
            if (count <= 0) { CryptographicOperations.ZeroMemory(result); throw Refusal(); }
            part.Bytes.AsSpan(pieceOffset, count).CopyTo(result.AsSpan(written)); written += count;
        }
        return result;
    }

    /// <summary>Splits only explicitly captured referenced bytes; their plain SHA/table never enters an outer public record address.</summary>
    private void AddOutgoingAttachments(IEnumerable<SyncRecord> projected, IHouseholdAttachmentSource sources,
        HouseholdEpochMaterial epoch, Guid attempt, Action<HouseholdFinancialPayload, string> add, Func<int> partCount, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var needs = PersonalSyncStore.BlobNeeds(projected);
        var neededHashes = needs.Select(need => need.Hash).ToHashSet(StringComparer.Ordinal);
        if (sources.Hashes.Any(hash => !neededHashes.Contains(hash))) throw Refusal();
        var small = new List<HouseholdAttachmentPiece>(); var smallBytes = 1024;
        try
        {
        foreach (var need in needs.OrderBy(need => need.Hash, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            if (attachmentLengths.TryGetValue(need.Hash, out var existing))
            { if (existing != need.Length) throw Refusal(); continue; }
            if (need.Length == 0)
            { if (need.Hash != Convert.ToHexStringLower(SHA256.HashData([]))) throw Refusal(); continue; }
            if (need.Length > SyncGraphValidation.MaximumProfileBytes) throw Refusal();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (long offset = 0; offset < need.Length; offset += EncryptedAttachmentPieceBytes)
            {
                ct.ThrowIfCancellationRequested();
                var index = (int)(offset / EncryptedAttachmentPieceBytes);
                var piece = sources.ReadPiece(need.Hash, need.Length, index);
                if (piece.Length != Math.Min(EncryptedAttachmentPieceBytes, need.Length - offset))
                { CryptographicOperations.ZeroMemory(piece); throw Refusal(); }
                hash.AppendData(piece);
                if (piece.Length <= 4096)
                {
                    var blob = new HouseholdAttachmentPiece(need.Hash, need.Length, index, piece);
                    byte[]? serialized = null;
                    try
                    {
                        serialized = JsonSerializer.SerializeToUtf8Bytes(blob, HouseholdFinancialJsonContext.Default.HouseholdAttachmentPiece);
                        if (small.Count == 512 || smallBytes + serialized.Length + 1 > 240 * 1024) FlushSmall();
                        small.Add(blob); smallBytes += serialized.Length + 1;
                    }
                    catch { CryptographicOperations.ZeroMemory(piece); throw; }
                    finally { if (serialized is not null) CryptographicOperations.ZeroMemory(serialized); }
                    continue;
                }
                try
                {
                    var address = epoch.RecordAddress("attachment-piece", FormattableString.Invariant($"{need.Hash}:{index}"));
                    add(new(1, HouseholdFinancialSchema.Hash, attempt, partCount(), HouseholdFinancialContentKind.Attachment, null,
                        new(need.Hash, need.Length, index, piece)), address);
                }
                finally { CryptographicOperations.ZeroMemory(piece); }
            }
            if (Convert.ToHexStringLower(hash.GetHashAndReset()) != need.Hash) throw Refusal();
        }
        FlushSmall();
        }
        finally { foreach (var blob in small) CryptographicOperations.ZeroMemory(blob.Bytes); }

        void FlushSmall()
        {
            if (small.Count == 0) return;
            try { add(new(1, HouseholdFinancialSchema.Hash, attempt, partCount(), HouseholdFinancialContentKind.AttachmentBatch, null, null, Blobs: small.ToArray()),
                BatchAddress(epoch, partCount())); }
            finally { foreach (var blob in small) CryptographicOperations.ZeroMemory(blob.Bytes); small.Clear(); smallBytes = 1024; }
        }
    }

    /// <summary>Copies dictionary ownership only; verified existing immutable byte arrays are neither rewritten nor rehashed for unrelated edits.</summary>
    private Dictionary<(string Hash, int Index), StoredAttachmentPiece> CopyAttachments() => new(attachments);

    /// <summary>Accepts only one exact canonical immutable encrypted piece; a later author cannot replace an old content address.</summary>
    private static void AcceptAttachment(Dictionary<(string Hash, int Index), StoredAttachmentPiece> target,
        HouseholdAttachmentPiece piece, HouseholdEpochMaterial epoch, string actualAddress)
    {
        if (piece.Bytes is null) throw Refusal();
        try
        {
            if (!SyncProtocolDigest.IsDigest(piece.Sha256) || piece.Length is < 1 or > SyncGraphValidation.MaximumProfileBytes || piece.Index < 0 ||
                (long)piece.Index * EncryptedAttachmentPieceBytes >= piece.Length ||
                piece.Bytes.Length != Math.Min(EncryptedAttachmentPieceBytes, piece.Length - (long)piece.Index * EncryptedAttachmentPieceBytes) ||
                epoch.RecordAddress("attachment-piece", FormattableString.Invariant($"{piece.Sha256}:{piece.Index}")) != actualAddress) throw Refusal();
            var key = (piece.Sha256, piece.Index);
            if (target.TryGetValue(key, out var old))
            { if (old.Length != piece.Length || !old.Bytes.AsSpan().SequenceEqual(piece.Bytes)) throw Refusal(); }
            else target.Add(key, new(piece.Length, piece.Bytes.ToArray()));
        }
        finally { CryptographicOperations.ZeroMemory(piece.Bytes); }
    }

    /// <summary>All referenced content is complete and independently SHA256 verified before the financial graph can advance.</summary>
    private Dictionary<string, long> VerifyAttachments(IEnumerable<SyncRecord> projected,
        Dictionary<(string Hash, int Index), StoredAttachmentPiece> candidates)
    {
        var needs = PersonalSyncStore.BlobNeeds(projected); var verified = new Dictionary<string, long>(attachmentLengths, StringComparer.Ordinal);
        if (needs.Sum(need => need.Length) > SyncGraphValidation.MaximumProfileBytes) throw new SyncProfileBoundException();
        if (candidates.Keys.Any(key => !attachments.ContainsKey(key) && !needs.Any(need => need.Hash == key.Hash))) throw Refusal();
        foreach (var need in needs)
        {
            if (verified.TryGetValue(need.Hash, out var oldLength)) { if (oldLength != need.Length) throw Refusal(); continue; }
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (var index = 0; (long)index * EncryptedAttachmentPieceBytes < need.Length; index++)
            {
                if (!candidates.TryGetValue((need.Hash, index), out var part) || part.Length != need.Length ||
                    part.Bytes.Length != Math.Min(EncryptedAttachmentPieceBytes, need.Length - (long)index * EncryptedAttachmentPieceBytes)) throw Refusal();
                hash.AppendData(part.Bytes);
            }
            if (Convert.ToHexStringLower(hash.GetHashAndReset()) != need.Hash) throw Refusal();
            verified.Add(need.Hash, need.Length);
        }
        return verified;
    }

    /// <summary>Only current referenced content stays resident when an actual authenticated ciphertext archive is available; historical originals remain recoverable there.</summary>
    private static (string Hash, int Index)[] UnreferencedAttachmentKeys(IEnumerable<SyncRecord> projected,
        IReadOnlyDictionary<(string Hash, int Index), StoredAttachmentPiece> candidates)
    {
        var retained = PersonalSyncStore.BlobNeeds(projected).Select(need => need.Hash).ToHashSet(StringComparer.Ordinal);
        return candidates.Keys.Where(key => !retained.Contains(key.Hash)).ToArray();
    }

    /// <summary>Only newly decoded candidate buffers are cleared after a rejected atomic transaction; prior verified originals stay unchanged.</summary>
    private void ClearRejectedAttachmentCandidates(Dictionary<(string Hash, int Index), StoredAttachmentPiece> candidates)
    {
        foreach (var (key, part) in candidates)
            if (!attachments.ContainsKey(key)) CryptographicOperations.ZeroMemory(part.Bytes);
    }

    /// <summary>Clears owned in-memory attachment buffers; no claim is made about immutable JSON strings or exported/device copies.</summary>
    public void Dispose()
    {
        if (disposed) return;
        foreach (var part in attachments.Values) CryptographicOperations.ZeroMemory(part.Bytes);
        attachments.Clear(); attachmentLengths.Clear(); attachmentSources.Clear(); current.Clear(); history.Clear(); opaqueVersions.Clear();
        disposed = true;
    }
}
