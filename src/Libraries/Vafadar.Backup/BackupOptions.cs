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

    /// <summary>
    /// Gets or sets the backup set in use, read at every call, e.g. the open local profile. Backups of different sets
    /// can share one storage: file names carry the set (<c>{appId}~{set}_{time}.vbak</c>), and listing, retention and
    /// the last backup time count only the current set. <see langword="null"/> or empty is the default set, whose files
    /// keep the plain <c>{appId}_{time}.vbak</c> name. A set must not contain <c>_</c>, <c>~</c>, <c>/</c> or <c>\</c>.
    /// </summary>
    public Func<string?>? FileSet { get; set; }
}
