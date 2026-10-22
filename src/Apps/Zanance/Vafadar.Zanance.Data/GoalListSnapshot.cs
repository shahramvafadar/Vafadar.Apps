using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>Original complete goal-list financial inputs captured by one actual-file reader without writing settings.</summary>
/// <param name="Goals">Original goal definitions, allocations, contributions and separate activity rights.</param>
/// <param name="Accounts">All original accounts in their existing display order.</param>
/// <param name="Entries">Complete original ledger history in its existing newest-first order.</param>
/// <param name="AssetTypes">Original holding metadata for quantity goals.</param>
/// <param name="AssetEvents">Complete original ordered holding events.</param>
public sealed record GoalListSnapshot(GoalWorkSnapshot Goals, List<Account> Accounts, List<LedgerEntry> Entries,
    List<AssetType> AssetTypes, List<AssetEvent> AssetEvents)
{
    /// <summary>Gets the actual captured file, independently of later profile selection.</summary>
    public string DatabasePath => Goals.DatabasePath;

    /// <summary>Refuses retired original rights before or during native list publication.</summary>
    public void EnsureCurrent()
    {
        var access = Goals.Access ?? throw new InvalidOperationException("Original goal list access is missing.");
        access.DemandFeature(CommercialFeature.History); access.EnsureSnapshotCurrent();
    }
}
