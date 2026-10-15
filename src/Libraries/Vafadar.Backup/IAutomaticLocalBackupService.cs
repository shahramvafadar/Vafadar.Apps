namespace Vafadar.Backup;

/// <summary>Creates device-protected local copies independently of portable backup protection and timestamps.</summary>
public interface IAutomaticLocalBackupService
{
    /// <summary>Gets the successful automatic-copy timestamp for the currently selected backup set.</summary>
    DateTimeOffset? LastAutomaticBackupAt { get; }

    /// <summary>Checks the interval again under serialization and creates at most one due protected copy.</summary>
    /// <remarks>No portable password is accepted or persisted. A null interval disables creation.
    /// Only <see cref="Storage.LocalFolderBackupStorage"/> is accepted; cloud storage is never an automatic destination.</remarks>
    Task<BackupFileInfo?> CreateWhenDueAsync(IBackupStorage storage, TimeSpan? interval, Action validate,
        CancellationToken ct = default);
}
