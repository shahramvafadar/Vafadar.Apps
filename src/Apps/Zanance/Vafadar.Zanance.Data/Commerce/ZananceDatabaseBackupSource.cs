using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Actual-file backup/recovery rights without resource quotas or portable paid facts.</summary>
internal sealed class ZananceDatabaseBackupSource(IDbContextFactory<ZananceDbContext> contextFactory,
    ICommercialWriteAccessSource commercialAccess) : IBackupSource
{
    /// <inheritdoc />
    public string Name => SqliteDatabaseBackupSource<ZananceDbContext>.EntryName;

    /// <inheritdoc />
    public Task WriteAsync(Stream destination, CancellationToken cancellationToken) =>
        new SqliteDatabaseBackupSource<ZananceDbContext>(contextFactory).WriteAsync(destination, AccessCheck(), cancellationToken);

    /// <inheritdoc />
    public Task RestoreAsync(Stream source, CancellationToken cancellationToken) =>
        new SqliteDatabaseBackupSource<ZananceDbContext>(contextFactory).RestoreAsync(source, AccessCheck(), cancellationToken);

    /// <summary>Each backup operation captures its own file before awaits and rechecks before native copying.</summary>
    private Action<ZananceDbContext> AccessCheck()
    {
        CommercialFileAccess? access = null;
        return db =>
        {
            access ??= new(commercialAccess, db.Database.GetDbConnection().DataSource);
            // D-124: recovery preserves owned data above quota and after host expiry; membership remains separate.
            access.DemandFeature(CommercialFeature.BackupRestore);
        };
    }
}
