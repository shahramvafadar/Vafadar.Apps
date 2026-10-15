namespace Vafadar.Zanance.Api.Sync;

/// <summary>Operator-configurable technical storage bounds, independent of commercial plans and subscription retention.</summary>
public sealed class SyncStorageLimits
{
    /// <summary>Maximum concurrent uncommitted operations across one actual owner's spaces.</summary>
    public int PendingRoundsPerOwner { get; }
    /// <summary>Maximum original serialized proposal bytes across that owner's active operations.</summary>
    public long StagedBytesPerOwner { get; }
    /// <summary>Maximum declared attachment bytes across that owner's spaces, including retained historical content.</summary>
    public long BlobBytesPerOwner { get; }

    /// <summary>Validates finite operational bounds; no customer plan or financial-history expiry follows from them.</summary>
    public SyncStorageLimits(int pendingRoundsPerOwner = 64, long stagedBytesPerOwner = 512L * 1024 * 1024,
        long blobBytesPerOwner = 1024L * 1024 * 1024)
    {
        if (pendingRoundsPerOwner is < 1 or > 1024 || stagedBytesPerOwner is < 1024 or > 64L * 1024 * 1024 * 1024 ||
            blobBytesPerOwner is < 1024 or > 64L * 1024 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(pendingRoundsPerOwner), "Finite personal synchronization storage bounds are required.");
        PendingRoundsPerOwner = pendingRoundsPerOwner; StagedBytesPerOwner = stagedBytesPerOwner; BlobBytesPerOwner = blobBytesPerOwner;
    }

    /// <summary>Reads only bounded nonsecret operator settings before the service starts.</summary>
    internal static SyncStorageLimits Read(IConfiguration configuration) => new(
        configuration.GetValue<int?>("Service:Sync:PendingRoundsPerOwner") ?? 64,
        configuration.GetValue<long?>("Service:Sync:StagedBytesPerOwner") ?? 512L * 1024 * 1024,
        configuration.GetValue<long?>("Service:Sync:BlobBytesPerOwner") ?? 1024L * 1024 * 1024);
}
