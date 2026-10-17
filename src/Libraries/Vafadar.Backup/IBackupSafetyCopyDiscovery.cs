namespace Vafadar.Backup;

/// <summary>Lists device-local safety candidates for the captured current profile without opening keys or plaintext.</summary>
public interface IBackupSafetyCopyDiscovery
{
    /// <summary>Returns only current-context candidates; selection still requires complete authenticated package review.</summary>
    Task<IReadOnlyList<BackupFileInfo>> ListSafetyCopiesAsync(CancellationToken cancellationToken = default);
}
