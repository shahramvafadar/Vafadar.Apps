using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original goal inputs and a separate new-work policy for the actual opened profile file.</summary>
/// <param name="Goals">Every original goal, including unselected and historical identities.</param>
/// <param name="ContributionPlans">Complete saved rules, retained without paid-tool or selection rewriting.</param>
/// <param name="Allocations">All original earmarks and releases needed by complete funding/protection calculations.</param>
/// <param name="Work">Independent eligibility for new activity; never pass it into financial progress calculations.</param>
public sealed record GoalWorkSnapshot(List<Goal> Goals, List<ContributionPlan> ContributionPlans,
    List<GoalAllocation> Allocations, GoalWorkPolicy Work)
{
    /// <summary>Gets the actual file's opaque local notification identity, never an authorization token.</summary>
    public string ReminderScope { get; internal init; } = string.Empty;
    /// <summary>Gets the actual captured file, retained only inside the data boundary.</summary>
    internal string DatabasePath { get; init; } = string.Empty;
    /// <summary>Gets the persisted choice revision, with absence distinct from an explicit empty choice.</summary>
    internal Guid? SelectionRevision { get; init; }
    /// <summary>Gets the complete raw original choice packet, including unchanged-revision mutations.</summary>
    internal string SelectionStamp { get; init; } = string.Empty;
    /// <summary>Gets a private original-input stamp, including state/rules/funding changes without a choice change.</summary>
    internal string InputStamp { get; init; } = string.Empty;
    /// <summary>Gets the referenced account/holding eligibility and price-currency stamp for reviewed detail writes.</summary>
    internal string ReferenceStamp { get; init; } = string.Empty;
    /// <summary>Gets the immutable cached-rights guard captured by the actual read.</summary>
    internal Commerce.CommercialFileAccess? Access { get; init; }

    /// <summary>Compares original actual-file inputs/revision when an editor explicitly reloads retained drafts.</summary>
    public bool HasSameOriginalInputs(GoalWorkSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return DatabasePath == other.DatabasePath && SelectionRevision == other.SelectionRevision
            && SelectionStamp == other.SelectionStamp && InputStamp == other.InputStamp && ReferenceStamp == other.ReferenceStamp;
    }

    /// <summary>Captures original account/holding units and availability on which the goal form relies.</summary>
    internal static string CaptureReferences(IEnumerable<Core.Accounts.Account> accounts, IEnumerable<Core.Holdings.AssetType> types)
    {
        var values = accounts.OrderBy(account => account.Id).Select(account => FormattableString.Invariant(
            $"account|{account.Id:N}|{account.Type}|{account.IsArchived}|{account.CurrencyCode}"))
            .Concat(types.OrderBy(type => type.Id).Select(type => FormattableString.Invariant(
                $"type|{type.Id:N}|{type.Dimension}|{type.PriceCurrencyCode}|{type.IsArchived}|{type.Divisible}|{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(type.CountUnitName ?? string.Empty))}")));
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join('\n', values))));
    }

    /// <summary>Captures native preparation inputs without reflection serialization, portable state or schema changes.</summary>
    internal static string CaptureInputs(IEnumerable<Goal> goals, IEnumerable<ContributionPlan> plans, IEnumerable<GoalAllocation> allocations)
    {
        static string Text(string? value) => value is null ? "-" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        var values = goals.OrderBy(goal => goal.Id).Select(goal => FormattableString.Invariant(
            $"goal|{goal.Id:N}|{Text(goal.Name)}|{goal.State}|{goal.Type}|{goal.TargetAmount}|{goal.TargetDate?.DayNumber}|{Text(goal.CurrencyCode)}|{goal.AccountId:N}|{goal.AssetTypeId:N}|{goal.LocationId:N}|{goal.Priority}|{goal.Protect}|{goal.UpdatedAt.UtcTicks}|{goal.CreatedAt.UtcTicks}|{Text(goal.Icon)}|{Text(goal.Note)}|{goal.HomePin}|{goal.Frequency}|{goal.PausedAt?.UtcTicks}|{goal.CompletedAt?.UtcTicks}"))
            .Concat(plans.OrderBy(plan => plan.Id).Select(plan => FormattableString.Invariant(
                $"plan|{plan.Id:N}|{plan.GoalId:N}|{plan.ReminderEnabled}|{plan.Method}|{plan.Amount}|{plan.Percent}|{plan.AssumedPricePerUnitMilli}|{string.Join(',', plan.CategoryIds)}|{plan.UpdatedAt.UtcTicks}|{plan.CreatedAt.UtcTicks}|{plan.Rule.Frequency}|{plan.Rule.Interval}|{plan.Rule.Start.DayNumber}|{plan.Rule.Calendar}|{plan.Rule.DayRule}|{plan.Rule.MissingDay}|{plan.Rule.SecondDay}|{plan.Rule.End}|{plan.Rule.EndDate?.DayNumber}|{plan.Rule.Count}|{plan.Rule.WeekendShift}|{plan.Rule.WeekendDays}|{Text(plan.Rule.HolidayRegion)}")))
            .Concat(allocations.OrderBy(allocation => allocation.Id).Select(allocation => FormattableString.Invariant(
                $"allocation|{allocation.Id:N}|{allocation.GoalId:N}|{allocation.AccountId:N}|{allocation.Amount}|{allocation.Date.DayNumber}|{Text(allocation.Note)}|{allocation.CreatedAt.UtcTicks}|{allocation.UpdatedAt.UtcTicks}")));
        // Names/region are length-safe encodings; canonical order and invariant numbers survive live display changes.
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join('\n', values))));
    }
}
