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
}
