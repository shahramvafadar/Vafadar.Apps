using Microsoft.Extensions.DependencyInjection;
using Vafadar.Authentication;
using Vafadar.Backup;
using Vafadar.Backup.GoogleDrive;
using Vafadar.Backup.OneDrive;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Delegates original backup destination effects to the attached native page and registered provider services.</summary>
public sealed class MauiBackupWorkspaceRuntime(IServiceProvider services, Vafadar.Authentication.Maui.CloudSignIn cloud,
    Func<Page> originalPage) : IBackupWorkspaceRuntime
{
    /// <inheritdoc />
    public bool IsAvailable(ExternalIdentityProvider provider) => cloud.IsAvailable(provider);
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        originalPage().DisplayAlertAsync(title, message, accept, cancel);
    /// <inheritdoc />
    public async Task<string?> ChooseAsync(string title, string cancel, string restore, string delete) =>
        await originalPage().DisplayActionSheetAsync(title, cancel, null, restore, delete);
    /// <inheritdoc />
    public IExternalSignInService SignIn(ExternalIdentityProvider provider) => services.GetRequiredKeyedService<IExternalSignInService>(provider);
    /// <inheritdoc />
    public IBackupStorage Storage(ExternalIdentityProvider provider) => provider == ExternalIdentityProvider.Google
        ? services.GetRequiredService<GoogleDriveBackupStorage>() : services.GetRequiredService<OneDriveBackupStorage>();
}
