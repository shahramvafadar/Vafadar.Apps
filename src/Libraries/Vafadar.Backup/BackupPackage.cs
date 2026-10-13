using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Vafadar.Backup;

/// <summary>
/// Reads and writes the (unencrypted) backup package: a ZIP archive with <c>manifest.json</c> and one entry per
/// <see cref="IBackupSource"/> under <c>data/</c>.
/// </summary>
internal static class BackupPackage
{
    public const string ManifestEntryName = "manifest.json";
    public const string DataFolder = "data/";

    public static async Task<byte[]> CreateAsync(
        IReadOnlyList<IBackupSource> sources,
        Func<IReadOnlyList<BackupManifestEntry>, BackupManifest> createManifest,
        CancellationToken cancellationToken, int? maximumBytes = null)
    {
        ValidateSourceNames(sources);

        using var buffer = new BoundedPackageBuffer(maximumBytes ?? int.MaxValue);
        long totalBytes = 0;
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entries = new List<BackupManifestEntry>(sources.Count);
            foreach (var source in sources)
            {
                using var content = new BoundedPackageBuffer((maximumBytes ?? int.MaxValue) - totalBytes);
                await source.WriteAsync(content, cancellationToken);
                totalBytes = checked(totalBytes + content.Length);

                content.Position = 0;
                var hash = Convert.ToHexStringLower(SHA256.HashData(content));

                content.Position = 0;
                var entry = archive.CreateEntry(DataFolder + source.Name, CompressionLevel.Optimal);
                await using (var entryStream = entry.Open())
                {
                    await content.CopyToAsync(entryStream, cancellationToken);
                }

                entries.Add(new BackupManifestEntry(source.Name, content.Length, hash));
            }

            var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
            await using var manifestStream = manifestEntry.Open();
            await JsonSerializer.SerializeAsync(manifestStream, createManifest(entries), BackupJsonContext.Default.BackupManifest, cancellationToken);
        }

        return buffer.ToArray();
    }

    public static async Task<(BackupManifest Manifest, IReadOnlyDictionary<string, byte[]> Entries)> ReadAsync(
        byte[] package,
        CancellationToken cancellationToken, int? maximumBytes = null)
    {
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var completed = false;
        try
        {
            using var archive = new ZipArchive(new MemoryStream(package, writable: false), ZipArchiveMode.Read);

            var manifestEntry = archive.GetEntry(ManifestEntryName)
                ?? throw new BackupException(BackupError.InvalidFormat, "The backup package has no manifest.");
            if (manifestEntry.Length > (maximumBytes ?? int.MaxValue))
                throw new BackupException(BackupError.InvalidFormat, "The backup manifest exceeds its bound.");

            BackupManifest manifest;
            await using (var manifestStream = manifestEntry.Open())
            {
                manifest = await JsonSerializer.DeserializeAsync(manifestStream, BackupJsonContext.Default.BackupManifest, cancellationToken)
                    ?? throw new BackupException(BackupError.InvalidFormat, "The backup manifest is empty.");
            }

            if (manifest.FormatVersion > BackupManifest.CurrentFormatVersion)
            {
                throw new BackupException(
                    BackupError.UnsupportedFormatVersion,
                    $"The backup uses package format {manifest.FormatVersion}; this app supports up to {BackupManifest.CurrentFormatVersion}. Update the app.");
            }

            if (manifest.Entries is null || manifest.Entries.Any(e => e is null || string.IsNullOrEmpty(e.Name) || e.Sha256 is null))
            {
                throw new BackupException(BackupError.InvalidFormat, "The backup manifest is incomplete.");
            }

            long totalBytes = 0;
            foreach (var described in manifest.Entries)
            {
                var entry = archive.GetEntry(DataFolder + described.Name)
                    ?? throw new BackupException(BackupError.Corrupted, $"The backup entry '{described.Name}' is missing.");

                // The size is checked before reading, so a damaged or hostile package cannot fill the memory.
                if (entry.Length != described.Length || described.Length < 0
                    || described.Length > (maximumBytes ?? int.MaxValue) - totalBytes)
                {
                    throw new BackupException(BackupError.Corrupted, $"The backup entry '{described.Name}' is damaged.");
                }

                totalBytes = checked(totalBytes + described.Length);
                using var content = new BoundedPackageBuffer(described.Length);
                await using (var entryStream = entry.Open())
                {
                    await entryStream.CopyToAsync(content, cancellationToken);
                }

                var bytes = content.ToArray();
                if (bytes.LongLength != described.Length
                    || !string.Equals(Convert.ToHexStringLower(SHA256.HashData(bytes)), described.Sha256, StringComparison.Ordinal))
                {
                    CryptographicOperations.ZeroMemory(bytes);
                    throw new BackupException(BackupError.Corrupted, $"The backup entry '{described.Name}' is damaged.");
                }

                if (!entries.TryAdd(described.Name, bytes))
                {
                    CryptographicOperations.ZeroMemory(bytes);
                    throw new BackupException(BackupError.InvalidFormat, "Duplicate backup entry.");
                }
            }

            completed = true;
            return (manifest, entries);
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            throw new BackupException(BackupError.InvalidFormat, "The file is not a valid backup package.", ex);
        }
        finally
        {
            if (!completed) foreach (var content in entries.Values) CryptographicOperations.ZeroMemory(content);
        }
    }

    private static void ValidateSourceNames(IReadOnlyList<IBackupSource> sources)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            var name = source.Name;
            if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name || name.Contains('/') || name.Contains('\\'))
            {
                throw new InvalidOperationException($"Backup source name '{name}' must be a plain file name.");
            }

            if (!names.Add(name))
            {
                throw new InvalidOperationException($"More than one backup source is named '{name}'.");
            }
        }
    }
}
