namespace Vafadar.Backup;

/// <summary>
/// Configures <see cref="BackupService"/>.
/// </summary>
public sealed class BackupOptions
{
    /// <summary>
    /// Gets or sets how many backups are kept per storage location; older ones are deleted after each backup.
    /// <c>0</c> keeps all backups. Defaults to 10.
    /// </summary>
    public int MaxBackupsToKeep { get; set; } = 10;

    /// <summary>
    /// Gets or sets how often an automatic backup is due (see <see cref="IBackupService.IsAutomaticBackupDue"/>).
    /// <see langword="null"/> disables automatic backups. Defaults to one day.
    /// </summary>
    public TimeSpan? AutomaticBackupInterval { get; set; } = TimeSpan.FromDays(1);
}
