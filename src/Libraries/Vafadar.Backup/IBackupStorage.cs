namespace Vafadar.Backup;

/// <summary>
/// A place backup files are stored: a local folder, the user's Google Drive, the user's OneDrive, ...
/// </summary>
/// <remarks>
/// Storage implementations only move files; they know nothing about the package format.
/// A storage location may be shared by several apps, so <see cref="BackupService"/> filters files by app id.
/// </remarks>
public interface IBackupStorage
{
    /// <summary>Gets a stable identifier of the storage kind, e.g. <c>local</c>, <c>google-drive</c>, <c>onedrive</c>.</summary>
    string Id { get; }

    /// <summary>Lists all files in the storage location.</summary>
    Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Uploads a file and returns its description.</summary>
    Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens a file for reading. The caller disposes the returned stream.</summary>
    Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file.</summary>
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}
