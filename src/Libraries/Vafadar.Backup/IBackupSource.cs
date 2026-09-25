namespace Vafadar.Backup;

/// <summary>
/// A piece of app data that is included in backups, e.g. the SQLite database or a folder of attachments.
/// </summary>
/// <remarks>
/// Register sources in dependency injection as <see cref="IBackupSource"/>; <see cref="BackupService"/> backs up
/// all of them into one package.
/// </remarks>
public interface IBackupSource
{
    /// <summary>
    /// Gets the name of the entry inside the backup package, e.g. <c>database.sqlite</c>.
    /// Must be a plain file name, unique per app, and must never change once released.
    /// </summary>
    string Name { get; }

    /// <summary>Writes a consistent snapshot of the data to <paramref name="destination"/>.</summary>
    Task WriteAsync(Stream destination, CancellationToken cancellationToken);

    /// <summary>Replaces the current data with the snapshot read from <paramref name="source"/>.</summary>
    Task RestoreAsync(Stream source, CancellationToken cancellationToken);
}
