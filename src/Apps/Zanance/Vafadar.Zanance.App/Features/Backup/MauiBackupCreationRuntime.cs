using Vafadar.Backup;
using Vafadar.Localization;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Native original portable creation effects, invoked only under the captured request's current authority.</summary>
public sealed class MauiBackupCreationRuntime(AppLockService appLock, Translator translator) : IBackupCreationRuntime
{
    /// <inheritdoc />
    public Task<bool> ConfirmAsync() => appLock.ConfirmAsync(translator["Lock_ConfirmBackup"]);
    /// <inheritdoc />
    public Task ShareAsync(BackupFileInfo file) => Share.Default.RequestAsync(new ShareFileRequest
    {
        Title = translator["Backup_ShareTitle"],
        File = new ShareFile(Path.Combine(FileSystem.AppDataDirectory, "backups", file.FileName), "application/octet-stream"),
    });
}
