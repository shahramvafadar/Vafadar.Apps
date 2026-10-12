using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Read-only original access facts for the actual file; no purchase or renewal facts are inferred.</summary>
public sealed class CommercialAccessSnapshot
{
    private readonly CommercialFileAccess _access;
    /// <summary>Retains the original current-file packet without copying or granting access.</summary>
    internal CommercialAccessSnapshot(CommercialFileAccess access) => _access = access;
    /// <summary>Gets verified-input capability facts, absent while commercial registration is inactive.</summary>
    public CapabilityContext? Context => _access.Context;
    /// <summary>Rejects retired facts, including an inactive-to-active change, before presentation.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
}

/// <summary>Captures cached access without opening SQLite, counting money, or manufacturing a paid entitlement.</summary>
public sealed class CommercialAccessStore(IDbContextFactory<ZananceDbContext> contextFactory,
    ICommercialWriteAccessSource commercialAccess)
{
    /// <summary>Reads the configured file identity and its existing source; construction performs no database work.</summary>
    public async Task<CommercialAccessSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var access = new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db));
        access.EnsureSnapshotCurrent();
        return new(access);
    }
}
