namespace Vafadar.Backup;

/// <summary>Protects and durably qualifies unattended local copies under their captured profile authority.</summary>
public interface IAutomaticLocalBackupProtection
{
    /// <summary>Rechecks captured authority under native ownership before protecting a complete in-memory package.</summary>
    Task<byte[]> ProtectAutomaticAsync(ReadOnlyMemory<byte> package, Action validate, CancellationToken ct);

    /// <summary>Authenticates the uploaded copy and applies independent automatic-copy retention under captured authority.</summary>
    Task CompleteAutomaticAsync(BackupFileInfo file, Action validate, CancellationToken ct);
}
