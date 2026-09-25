namespace Vafadar.Backup;

/// <summary>
/// Creates, lists and restores backups of all registered <see cref="IBackupSource"/>s.
/// </summary>
public interface IBackupService
{
    /// <summary>Gets when the last successful backup was created on this device, if ever.</summary>
    DateTimeOffset? LastBackupAt { get; }

    /// <summary>Returns whether an automatic backup should run now, according to <see cref="BackupOptions.AutomaticBackupInterval"/>.</summary>
    bool IsAutomaticBackupDue();

    /// <summary>Creates a backup package, uploads it and applies the retention policy.</summary>
    /// <param name="storage">Where to store the backup.</param>
    /// <param name="password">Encrypts the backup when given. The password is not stored anywhere.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task<BackupFileInfo> CreateBackupAsync(IBackupStorage storage, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a backup package in memory, e.g. to export or share it as a file.</summary>
    Task<byte[]> CreatePackageAsync(string? password = null, CancellationToken cancellationToken = default);

    /// <summary>Lists this app's backups in <paramref name="storage"/>, newest first.</summary>
    Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(IBackupStorage storage, CancellationToken cancellationToken = default);

    /// <summary>Downloads and restores a backup, replacing the current data.</summary>
    /// <exception cref="BackupException">The backup cannot be restored (wrong password, other app, damaged, ...).</exception>
    Task<BackupManifest> RestoreAsync(IBackupStorage storage, BackupFileInfo file, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts and validates a package (password, app, version, checksums) and returns its manifest without
    /// changing any data – for a preview before a restore (BAK-09).
    /// </summary>
    /// <exception cref="BackupException">The backup cannot be restored (wrong password, other app, damaged, ...).</exception>
    Task<BackupManifest> InspectPackageAsync(byte[] package, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>Restores a backup package (e.g. an imported file), replacing the current data.</summary>
    /// <exception cref="BackupException">The backup cannot be restored (wrong password, other app, damaged, ...).</exception>
    Task<BackupManifest> RestorePackageAsync(byte[] package, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes backups beyond <see cref="BackupOptions.MaxBackupsToKeep"/>; returns how many were deleted.</summary>
    Task<int> ApplyRetentionAsync(IBackupStorage storage, CancellationToken cancellationToken = default);
}
