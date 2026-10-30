using Vafadar.Authentication;
using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>The original native disclosure and configured destination adapters, without credentials or a backup algorithm.</summary>
public interface IBackupWorkspaceRuntime
{
    /// <summary>Gets whether this original build has the specified destination configured.</summary>
    bool IsAvailable(ExternalIdentityProvider provider);
    /// <summary>Uses the original read-only service and cancellation token for local or connected discovery.</summary>
    Task<IReadOnlyList<BackupFileInfo>> DiscoverAsync(IBackupService backup, IBackupStorage storage, CancellationToken cancellationToken) =>
        backup.DiscoverBackupsAsync(storage, cancellationToken);
    /// <summary>Offers the original explicit destination disclosure.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
    /// <summary>Offers only the original selected backup's restore/delete actions.</summary>
    Task<string?> ChooseAsync(string title, string cancel, string restore, string delete);
    /// <summary>Gets the already configured original native provider service.</summary>
    IExternalSignInService SignIn(ExternalIdentityProvider provider);
    /// <summary>Gets the already configured original storage adapter.</summary>
    IBackupStorage Storage(ExternalIdentityProvider provider);
}
