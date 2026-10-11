using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One eligible original current-period alert, calculated from complete original financial inputs.</summary>
/// <param name="BudgetId">Original persisted period identity, also used for device-only deduplication.</param>
/// <param name="CurrencyCode">Original budget currency.</param>
/// <param name="Status">Original limit, carry and net spending; never recalculated from a selected account subset.</param>
public sealed record BudgetAlertCandidate(Guid BudgetId, string CurrencyCode, BudgetStatus Status);

/// <summary>A read-only actual-file alert batch, with separate activity rights and guarded financial/privacy inputs.</summary>
public sealed class BudgetAlertSnapshot
{
    /// <summary>Retains independent immutable candidate packets and the exact guarded definition/financial review.</summary>
    internal BudgetAlertSnapshot(BudgetChoiceSnapshot choice, IReadOnlyList<BudgetAlertCandidate> candidates,
        DateOnly? from, DateOnly? to, string stamp, bool showDetails)
    {
        Choice = choice; Candidates = Array.AsReadOnly(candidates.ToArray()); From = from; To = to;
        InputStamp = stamp; ShowDetails = showDetails;
    }
    /// <summary>Gets only eligible current alert candidates, retaining historical/unselected budgets in the underlying review.</summary>
    public IReadOnlyList<BudgetAlertCandidate> Candidates { get; }
    /// <summary>Gets the reviewed local date for stable notification identities.</summary>
    public DateOnly Today => Choice.Today;
    /// <summary>Gets the current explicit notification privacy choice.</summary>
    public bool ShowDetails { get; }
    /// <summary>Gets the complete original definition review, private to the data boundary.</summary>
    internal BudgetChoiceSnapshot Choice { get; }
    /// <summary>Gets the bounded start of all current/carry calculation inputs.</summary>
    internal DateOnly? From { get; }
    /// <summary>Gets the bounded end of all current/carry calculation inputs.</summary>
    internal DateOnly? To { get; }
    /// <summary>Gets the explicit financial-input stamp; not a portable grant or credential.</summary>
    internal string InputStamp { get; }
}
