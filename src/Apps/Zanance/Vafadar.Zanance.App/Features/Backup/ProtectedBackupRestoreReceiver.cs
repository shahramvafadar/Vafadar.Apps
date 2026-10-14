using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Connects validated whole-package backup delivery to the original protected application storage host.</summary>
/// <remarks>BackupService validates ZIP, app/version and checksums before this callback. No restore intent is
/// reserved during DI construction, preview, password failure or corrupt-package rejection. Native adoption
/// registers this receiver exclusively; its errors never invoke the legacy sequential source adapter.</remarks>
public sealed class ProtectedBackupRestoreReceiver(LocalDatabaseLocation<ZananceDbContext> location,
    ProtectedProfileStorage storage) : IBackupPackageRestoreReceiver
{
    /// <inheritdoc />
    public async Task RestoreAsync(BackupManifest manifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries,
        CancellationToken cancellationToken)
    {
        using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken).ConfigureAwait(false);
        await storage.RestorePackageAsync(manifest, entries, owner, cancellationToken).ConfigureAwait(false);
    }
}
