using System.Text.Json.Serialization;

namespace Vafadar.Backup.OneDrive;

internal sealed record DriveItem(
    string Id,
    string Name,
    long? Size,
    DateTimeOffset? CreatedDateTime,
    DriveItemFile? File,
    [property: JsonPropertyName("@microsoft.graph.downloadUrl")] string? DownloadUrl);

// Present only on files (not folders).
internal sealed record DriveItemFile(string? MimeType);

internal sealed record DriveItemCollection(
    IReadOnlyList<DriveItem>? Value,
    [property: JsonPropertyName("@odata.nextLink")] string? NextLink);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DriveItem))]
[JsonSerializable(typeof(DriveItemCollection))]
internal sealed partial class OneDriveJsonContext : JsonSerializerContext;
