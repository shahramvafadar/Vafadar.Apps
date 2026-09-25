namespace Vafadar.Backup.Storage;

/// <summary>
/// Stores backups as files in a local folder. Useful for tests, for "export to device" and as a staging area.
/// </summary>
/// <remarks>A local folder does not protect against losing the device; prefer a cloud storage for real backups.</remarks>
public sealed class LocalFolderBackupStorage : IBackupStorage
{
    private readonly string _directory;

    /// <summary>Creates a storage for <paramref name="directory"/>, which is created when needed.</summary>
    public LocalFolderBackupStorage(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = Path.GetFullPath(directory);
    }

    /// <inheritdoc />
    public string Id => "local";

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BackupFileInfo> files = Directory.Exists(_directory)
            ? [.. new DirectoryInfo(_directory).EnumerateFiles().Select(ToInfo)]
            : [];

        return Task.FromResult(files);
    }

    /// <inheritdoc />
    public async Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var path = GetPath(fileName);
        Directory.CreateDirectory(_directory);

        // Write to a temporary file first so that an interrupted write never leaves a half-written backup.
        var temporaryPath = path + ".tmp";
        await using (var file = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
        return ToInfo(new FileInfo(path));
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new FileStream(GetPath(fileId), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    /// <inheritdoc />
    public Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        File.Delete(GetPath(fileId));
        return Task.CompletedTask;
    }

    private string GetPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException("Only plain file names are allowed.", nameof(fileName));
        }

        return Path.Combine(_directory, fileName);
    }

    private static BackupFileInfo ToInfo(FileInfo file) =>
        new(file.Name, file.Name, file.Length, new DateTimeOffset(file.CreationTimeUtc, TimeSpan.Zero));
}
