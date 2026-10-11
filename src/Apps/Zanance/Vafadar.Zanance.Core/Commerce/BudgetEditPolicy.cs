using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>One shared feature rule for retained corrections versus new budget configuration.</summary>
public static class BudgetEditPolicy
{
    /// <summary>Describes the exact saved/current footprint; selection and net capacity remain separate writer checks.</summary>
    public static CommercialFeature Feature(Budget target, Budget? original, QuotaScope scope, DateOnly today, int monthStartDay)
    {
        ArgumentNullException.ThrowIfNull(target);
        var current = BudgetSelectionPolicy.IsCurrent(target, today, monthStartDay);
        var same = original is not null && BudgetDefinitionKey.From(scope, original) == BudgetDefinitionKey.From(scope, target);
        var newUse = original is null || !same || current && !BudgetSelectionPolicy.IsCurrent(original, today, monthStartDay);
        var addsAdvanced = (newUse || current) &&
            (target.Method != BudgetMethod.Limits && (newUse || original!.Method == BudgetMethod.Limits)
            || target.Rollover != BudgetRollover.None && (newUse || original!.Rollover == BudgetRollover.None)
            || target.Period != BudgetPeriod.Month && (newUse || original!.Period == BudgetPeriod.Month));
        return addsAdvanced ? CommercialFeature.AdvancedBudgets : newUse ? CommercialFeature.BasicBudgets : CommercialFeature.Corrections;
    }
}
