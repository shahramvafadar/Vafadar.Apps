using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Publishes the known original first-run commit before fallible Changed handlers or native root replacement.</summary>
    public async Task CompleteHouseholdOnboardingAsync(HouseholdJournal journal, HouseholdJournalAuthority authority,
        Action validateOriginal, Action accepted, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(journal); ArgumentNullException.ThrowIfNull(accepted);
        await journal.CompleteOnboardingAsync(authority, validateOriginal, ct);
        accepted(); OnChanged();
    }
}
