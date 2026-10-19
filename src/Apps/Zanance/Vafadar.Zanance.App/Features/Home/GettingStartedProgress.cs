using Vafadar.Zanance.Core.Budgets;
using BudgetModel = Vafadar.Zanance.Core.Budgets.Budget;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>The saved facts required by Home's three named getting-started steps.</summary>
internal sealed record GettingStartedProgress(bool FirstExpenseDone, bool RecurringPlanDone, bool MonthlyBudgetDone)
{
    /// <summary>Gets whether every named step has been completed.</summary>
    internal bool IsComplete => FirstExpenseDone && RecurringPlanDone && MonthlyBudgetDone;

    /// <summary>Reads saved kinds without treating a transfer, one-off plan or weekly budget as another task.</summary>
    internal static GettingStartedProgress From(IEnumerable<LedgerEntry> entries, IEnumerable<Schedule> schedules, IEnumerable<BudgetModel> budgets)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(budgets);
        return new(entries.Any(entry => entry.Kind == EntryKind.Expense),
            schedules.Any(plan => plan.Rule.Frequency is Frequency.Daily or Frequency.Weekly or Frequency.Monthly or Frequency.Yearly),
            budgets.Any(budget => budget.Period == BudgetPeriod.Month));
    }
}
