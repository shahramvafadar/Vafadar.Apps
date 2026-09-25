using System.Text.Json.Serialization;

namespace Vafadar.Backup.GoogleDrive;

internal sealed record DriveFile(string Id, string Name, long? Size, DateTimeOffset? CreatedTime);

internal sealed record DriveFileList(IReadOnlyList<DriveFile>? Files, string? NextPageToken);

internal sealed record DriveFileMetadata(string Name, IReadOnlyList<string> Parents);

// Drive returns 64-bit numbers (e.g. "size") as JSON strings.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DriveFile))]
[JsonSerializable(typeof(DriveFileList))]
[JsonSerializable(typeof(DriveFileMetadata))]
internal sealed partial class GoogleDriveJsonContext : JsonSerializerContext;
