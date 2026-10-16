using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Minimal permanent anti-resurrection intent outside a database snapshot; no email, financial content or private key.</summary>
public sealed record HouseholdDeletionMarker(int Format, Guid SpaceId, Guid OperationId, string ActorHash, string RequestHash,
    long DeclaredAtUnixSeconds, HouseholdDeletionReason Reason);

/// <summary>A bounded raw directory page; unknown names count toward cursor progress but never supply room authority.</summary>
public sealed record HouseholdDeletionInspection(int Scanned, IReadOnlyList<Guid> Spaces);

/// <summary>Original private immutable-file effects; a recorded deletion remains authoritative after an older database restore.</summary>
public interface IHouseholdDeletionLedger
{
    /// <summary>Reads one exact finite marker; corrupt/redirected state fails closed rather than appearing absent.</summary>
    Task<HouseholdDeletionMarker?> ReadAsync(Guid space, CancellationToken ct);
    /// <summary>Publishes one immutable original intent; retries cannot replace another operation or actor.</summary>
    Task<HouseholdDeletionMarker> RecordAsync(HouseholdDeletionMarker marker, CancellationToken ct);
    /// <summary>Bounded raw discovery including unknown-name cursor progress; recognized ids still require exact verified reads.</summary>
    HouseholdDeletionInspection Inspect(int offset, int maximum);
}

/// <summary>Server-owned write-through file publication; database commit failure cannot make an already accepted owner deletion disappear.</summary>
public sealed class HouseholdDeletionLedger(ServiceStorage storage) : IHouseholdDeletionLedger
{
    private const int MaximumBytes = 4096;

    /// <inheritdoc />
    public async Task<HouseholdDeletionMarker?> ReadAsync(Guid space, CancellationToken ct)
    {
        var path = PathOf(space);
        if (new FileInfo(path).LinkTarget is not null) throw new IOException("Private deletion marker is redirected.");
        if (!File.Exists(path))
        { if (Directory.Exists(path)) throw new IOException("Private deletion marker is not a regular file."); return null; }
        var info = new FileInfo(path);
        if ((info.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0 || info.Length is < 1 or > MaximumBytes)
            throw new IOException("Private deletion marker is invalid.");
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var marker = Parse(bytes);
        Validate(marker); if (marker.SpaceId != space) throw new IOException("Private deletion marker scope changed."); return marker;
    }

    /// <inheritdoc />
    public async Task<HouseholdDeletionMarker> RecordAsync(HouseholdDeletionMarker marker, CancellationToken ct)
    {
        Validate(marker); var existing = await ReadAsync(marker.SpaceId, ct);
        if (existing is not null) return RequireOriginal(existing, marker);
        var root = Root(); Directory.CreateDirectory(root); _ = Root();
        var path = PathOf(marker.SpaceId); var pending = path + "." + marker.OperationId.ToString("N") + ".pending";
        if (new FileInfo(pending).LinkTarget is not null) throw new IOException("Private deletion staging is redirected.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(marker, HouseholdDeletionJsonContext.Default.HouseholdDeletionMarker);
        if (bytes.Length > MaximumBytes) throw new IOException("Private deletion marker bound exceeded.");
        // A crash before the atomic rename leaves only a non-authoritative pending file.
        // Resume that exact original staging only; never truncate a different operation.
        if (File.Exists(pending))
        {
            var info = new FileInfo(pending);
            if ((info.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0 || info.Length > MaximumBytes)
                throw new IOException("Private deletion staging is invalid.");
            var originalBytes = await File.ReadAllBytesAsync(pending, ct);
            HouseholdDeletionMarker? original = null;
            try
            {
                original = Parse(originalBytes);
            }
            catch (IOException)
            {
                // A torn incomplete stage is not a published deletion. Preserve it in the
                // same owned directory and retry this currently authorized literal request;
                // no committed marker, room, financial file or key is ever replaced.
                File.Move(pending, pending + ".incomplete-" + Guid.NewGuid().ToString("N"));
                await using var resumed = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None, MaximumBytes, FileOptions.WriteThrough);
                await resumed.WriteAsync(bytes, ct); resumed.Flush(flushToDisk: true);
            }
            if (original is not null)
            {
                _ = RequireOriginal(original, marker);
                marker = original; // The original server declaration time survives response/process loss.
            }
        }
        else
        {
            await using var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None, MaximumBytes, FileOptions.WriteThrough);
            await file.WriteAsync(bytes, ct); file.Flush(flushToDisk: true);
        }
        _ = Root(); File.Move(pending, path); return marker;
    }

    /// <inheritdoc />
    public HouseholdDeletionInspection Inspect(int offset, int maximum)
    {
        if (offset < 0 || maximum is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(maximum));
        var root = Root(); if (!Directory.Exists(root)) return new(0, []);
        // Preserve every unknown/corrupt entry. Discovery is not authorization: the
        // later exact ReadAsync must still validate each known room before mutation.
        // Count raw entries so unknown names cannot pin the cursor to its first page.
        var paths = Directory.EnumerateFileSystemEntries(root, "*.json").Skip(offset).Take(maximum).ToArray();
        var spaces = paths.Select(path => Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).ToArray();
        return new(paths.Length, spaces);
    }

    /// <summary>Opaque comparison only; no identity address or credential enters permanent privacy intent.</summary>
    public static string Actor(string identity) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("household-delete-actor-v1:" + identity)));
    /// <summary>Only the already recorded original operation may acknowledge its existing intent.</summary>
    private static HouseholdDeletionMarker RequireOriginal(HouseholdDeletionMarker existing, HouseholdDeletionMarker requested) =>
        existing.SpaceId == requested.SpaceId && existing.OperationId == requested.OperationId && existing.ActorHash == requested.ActorHash &&
        existing.RequestHash == requested.RequestHash && existing.Reason == requested.Reason ? existing : throw new IOException("Another original deletion is already recorded.");
    /// <summary>Finite strict public metadata, independent of a client clock or financial calendar.</summary>
    private static void Validate(HouseholdDeletionMarker marker)
    {
        if (marker.Format != 1 || marker.SpaceId == Guid.Empty || marker.OperationId == Guid.Empty || !Enum.IsDefined(marker.Reason) ||
            !HouseholdProtectionContext.IsFingerprint(marker.ActorHash) || !HouseholdProtectionContext.IsFingerprint(marker.RequestHash) ||
            marker.DeclaredAtUnixSeconds <= 0 || marker.DeclaredAtUnixSeconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            throw new IOException("Private deletion declaration is invalid.");
    }
    /// <summary>Malformed private metadata is unavailable storage, never an absent marker or an owner-provided new authority.</summary>
    private static HouseholdDeletionMarker Parse(byte[] bytes)
    {
        try
        {
            var marker = StrictJsonPayload.Deserialize(bytes, HouseholdDeletionJsonContext.Default.HouseholdDeletionMarker, MaximumBytes);
            Validate(marker); return marker;
        }
        catch (Exception error) when (error is JsonException or CryptographicException or InvalidDataException)
        { throw new IOException("Private deletion marker cannot be verified.", error); }
    }
    /// <summary>Only a fixed server-generated room filename in the existing owned private root.</summary>
    private string PathOf(Guid space) => space != Guid.Empty ? Path.Combine(Root(), space.ToString("N") + ".json") : throw new IOException("Private room identity required.");
    /// <summary>Every read/write rechecks ancestors; a redirected private directory cannot be treated as an empty ledger.</summary>
    private string Root()
    {
        var root = Path.Combine(storage.Root, "household-deletions");
        if (File.Exists(root) || new DirectoryInfo(root).LinkTarget is not null) throw new IOException("Private deletion directory is invalid.");
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Private deletion directory is redirected.");
        return root;
    }
}

/// <summary>Finite source-generated privacy marker metadata; no reflection serializer or credential field.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HouseholdDeletionMarker))]
internal partial class HouseholdDeletionJsonContext : JsonSerializerContext;
