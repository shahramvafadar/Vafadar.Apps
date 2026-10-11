using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Read-only original budget notification resolution; historical viewing never grants new-work rights.</summary>
public sealed partial class ZananceStore
{
    /// <summary>Resolves the exact saved period in the current actual file, even after it becomes historical or unselected.</summary>
    public async Task<BudgetChoiceItem?> ResolveBudgetReminderAsync(BudgetReminderLink link, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(link);
        var choices = new BudgetChoiceStore(contextFactory, commercialAccess, time);
        var snapshot = await choices.ReadAsync(cancellationToken);
        if (snapshot.ReminderScope != link.Scope) return null;
        var target = snapshot.Items.SingleOrDefault(item => item.Id == link.BudgetId);
        if (target is null) return null;
        // Resolve again after unlock and at the actual page load; a restored Guid cannot identify another file.
        await choices.ValidateAsync(snapshot, cancellationToken);
        return target;
    }
}
