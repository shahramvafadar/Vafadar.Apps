using System.Text.Json.Serialization;

namespace Vafadar.Backup;

/// <summary>
/// Describes the content of a backup package. Stored as <c>manifest.json</c> inside the package.
/// </summary>
public sealed record BackupManifest
{
    /// <summary>The package format version written by this library.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Gets the package format version.</summary>
    public required int FormatVersion { get; init; }

    /// <summary>Gets the id of the app that created the backup.</summary>
    public required string AppId { get; init; }

    /// <summary>Gets the version of the app that created the backup.</summary>
    public required string AppVersion { get; init; }

    /// <summary>Gets when the backup was created (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the name of the device that created the backup.</summary>
    public string? DeviceName { get; init; }

    /// <summary>Gets the platform that created the backup.</summary>
    public string? Platform { get; init; }

    /// <summary>Gets the data entries in the package.</summary>
    public required IReadOnlyList<BackupManifestEntry> Entries { get; init; }
}

/// <summary>
/// One data entry of a backup package.
/// </summary>
/// <param name="Name">The <see cref="IBackupSource.Name"/> of the source that produced the entry.</param>
/// <param name="Length">The uncompressed length in bytes.</param>
/// <param name="Sha256">The lowercase hex SHA-256 hash of the uncompressed content.</param>
public sealed record BackupManifestEntry(string Name, long Length, string Sha256);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BackupManifest))]
internal sealed partial class BackupJsonContext : JsonSerializerContext;
