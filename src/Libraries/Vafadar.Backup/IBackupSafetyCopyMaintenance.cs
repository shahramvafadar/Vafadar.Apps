namespace Vafadar.Backup;

/// <summary>Native post-upload durability and reference-aware maintenance for internal safety copies.</summary>
public interface IBackupSafetyCopyMaintenance
{
    /// <summary>Qualifies the actual uploaded local copy and retires only durably authorized obsolete references.</summary>
    Task AfterUploadAsync(BackupFileInfo file, CancellationToken ct = default);
}
