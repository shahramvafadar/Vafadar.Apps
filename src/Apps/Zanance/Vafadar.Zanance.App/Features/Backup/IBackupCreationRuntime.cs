using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Original explicit native authentication and local portable-file handoff; no provider or credentials are replaced.</summary>
public interface IBackupCreationRuntime
{
    /// <summary>Requests the original genuine app-lock confirmation.</summary>
    Task<bool> ConfirmAsync();
    /// <summary>Offers the already stored original local file to the original native share surface.</summary>
    Task ShareAsync(BackupFileInfo file);
}
