namespace Vafadar.Zanance.Core.Holdings;

/// <summary>
/// Routes one unchanged event snapshot to its original asset-type slices, preserving identity and order.
/// Quantities still use HoldingsLedger; this index never combines units or copies financial formulas.
/// </summary>
public sealed class AssetEventIndex
{
    private readonly ILookup<Guid, AssetEvent> _events;

    /// <summary>Enumerates the snapshot once; location transfers remain one original event of the same type.</summary>
    public AssetEventIndex(IEnumerable<AssetEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events.ToLookup(item => item.AssetTypeId);
    }

    /// <summary>Returns original events in original order, or an empty slice for an unused asset type.</summary>
    public IEnumerable<AssetEvent> For(Guid assetTypeId) => _events[assetTypeId];
}
