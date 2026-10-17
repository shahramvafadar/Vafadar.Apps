using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

internal abstract partial class ProtectedStorageHost
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> ListSafetyCopiesAsync(CancellationToken cancellationToken = default)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken).ConfigureAwait(false);
        Session.RequireStable(owner);
        var capture = services.GetRequiredService<ProfileService>().CaptureStorage();
        void Validate() { location.Access.RequireDrainedOwner(owner); Session.RequireStable(owner); capture.Validate(); }
        Validate();
        var id = capture.OriginalPaths.Single(pair => pair.Value == capture.CurrentOriginalPath).Key;
        var identity = (await new SqliteDatabaseIdentityCatalogue(identityPath!, Validate).ReadAsync(cancellationToken).ConfigureAwait(false))
            .Single(row => row.LegacyProfileId == id);
        return await DeviceSafetyCopyDiscovery.ListAsync(new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups-safety")),
            identity.ProfileId, identity.InstallationId, Validate, cancellationToken).ConfigureAwait(false);
    }
}
