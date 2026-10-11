using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Identifies saved contribution work consistently for reviewed goal affordances and actual writers.</summary>
public static class GoalContributionPolicy
{
    /// <summary>Checks contribution dates beyond the basic weekly/monthly scaffold.</summary>
    public static bool HasAdvancedDates(ContributionPlan plan) => plan.Rule.Frequency is not (Frequency.Weekly or Frequency.Monthly)
        || plan.Rule.DayRule.IsWeekday() || plan.Rule.SecondDay is not null || plan.Rule.WeekendShift != WeekendShift.None;

    /// <summary>Checks whether the retained plan would restart paid work when a historical goal is reopened.</summary>
    public static bool HasWork(ContributionPlan? plan) => plan is not null && (plan.Method != ContributionMethod.FixedAmount
        || plan.Amount is not null || plan.Percent is not null || plan.CategoryIds.Count > 0
        || plan.AssumedPricePerUnitMilli is not null || HasAdvancedDates(plan));

    /// <summary>Classifies paid work consistently before an editor preview and under the actual paired writer.</summary>
    public static bool StartsWork(ContributionPlan? plan, ContributionPlan? existing, Goal? target, Goal? original)
    {
        var counted = target?.State is GoalState.Active or GoalState.Paused;
        var reopening = counted && original is not null && original.State is not (GoalState.Active or GoalState.Paused);
        return plan is not null && HasWork(plan) && (!HasWork(existing) || reopening
            || (counted && existing is not null && (plan.Method != existing.Method
                || (HasAdvancedDates(plan) && !HasAdvancedDates(existing)))));
    }

    /// <summary>Checks an independently paid newly enabled reminder; retained opt-in/disabling are corrections.</summary>
    public static bool StartsReminder(ContributionPlan? plan, ContributionPlan? existing, Goal? target, Goal? original) =>
        plan?.ReminderEnabled == true && (existing?.ReminderEnabled != true
            || (target?.State is GoalState.Active or GoalState.Paused && original is not null
                && original.State is not (GoalState.Active or GoalState.Paused)));
}
