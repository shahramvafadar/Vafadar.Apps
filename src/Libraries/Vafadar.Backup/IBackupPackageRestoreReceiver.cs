namespace Vafadar.Backup;

/// <summary>Owns whole-package restore instead of individually replacing registered sources.</summary>
/// <remarks>Registered receivers must stage all applicable sources, durably reconcile interruption and
/// publish only the complete restored state. The service validates package identity and every checksum first;
/// it supplies no global application lease or rollback guarantee. Failure never falls back to source restores.</remarks>
public interface IBackupPackageRestoreReceiver
{
    /// <summary>Restores validated package entries under the service's backup/restore gate.</summary>
    /// <remarks>Entry buffers are borrowed until this task completes and are cleared afterwards. Copy into
    /// owned protected staging before returning; do not retain them or treat absent entries as deletion.</remarks>
    Task RestoreAsync(BackupManifest manifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries,
        CancellationToken cancellationToken);
}
