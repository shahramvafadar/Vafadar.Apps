namespace Vafadar.Backup;

/// <summary>
/// A file in an <see cref="IBackupStorage"/>.
/// </summary>
/// <param name="Id">The storage-specific identifier used to download or delete the file.</param>
/// <param name="FileName">The file name.</param>
/// <param name="Size">The size in bytes, when known.</param>
/// <param name="CreatedAt">When the file was created in the storage, when known.</param>
public sealed record BackupFileInfo(string Id, string FileName, long? Size, DateTimeOffset? CreatedAt);
