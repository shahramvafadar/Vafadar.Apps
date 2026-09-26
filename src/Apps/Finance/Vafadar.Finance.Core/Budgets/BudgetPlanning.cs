using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Budgets;

/// <summary>Planned expenses of a period from plan occurrences; unknown amounts are counted, never treated as zero.</summary>
/// <param name="Total">Sum of the known amounts in minor units.</param>
/// <param name="Count">Number of occurrences.</param>
/// <param name="UnknownCount">Occurrences whose amount is not known (the total is incomplete).</param>
public sealed record PlannedExpense(long Total, int Count, int UnknownCount);

/// <summary>Budget helpers that look at plans and at the next period (BUD-07, BUD-09, BUD-10).</summary>
public static class BudgetPlanning
{
    /// <summary>
    /// Sums the planned expenses of the period by counting the real occurrences in it – three bi-weekly payments in a
    /// month are three amounts, not an average (BUD-10, AT-18). Settled and open occurrences count, skipped ones do not.
    /// </summary>
    public static PlannedExpense PlannedInPeriod(
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        IReadOnlyDictionary<Guid, Account> accounts,
        DateOnly from,
        DateOnly to,
        string currencyCode,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(accounts);
        var stateList = states.ToList();
        long total = 0;
        var count = 0;
        var unknown = 0;

        foreach (var schedule in schedules.Where(s => s.Kind == EntryKind.Expense))
        {
            if (!accounts.TryGetValue(schedule.AccountId, out var account)
                || !string.Equals(account.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var occurrence in Occurrences.Between(schedule, stateList, from, to, today).Where(o => o.Status != OccurrenceView.Skipped))
            {
                count++;
                if (occurrence.Amount is { } amount)
                {
                    total += amount;
                }
                else
                {
                    unknown++;
                }
            }
        }

        return new PlannedExpense(total, count, unknown);
    }

    /// <summary>
    /// Returns the approximate monthly equivalent of a non-monthly plan (amount × occurrences per year ÷ 12), e.g. a
    /// yearly insurance of 600 → 50 per month. For comparison only: it creates no entry and no budget spending
    /// (BUD-09, AT-41). Monthly and one-time plans and unknown amounts return <see langword="null"/>.
    /// </summary>
    public static long? MonthlyEquivalent(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var rule = schedule.Rule;
        if (schedule.Amount is not { } amount
            || schedule.AmountMode == AmountMode.Unknown
            || rule.Frequency == Frequency.Once
            || (rule.Frequency == Frequency.Monthly && rule.Interval == 1))
        {
            return null;
        }

        return (long)Math.Round(amount * Recurrence.PerYear(rule) / 12m, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Copies a budget to another month (BUD-07): same currency, calendar, scope and limits. The source budget is not
    /// changed, so closed months keep their history.
    /// </summary>
    public static Budget CopyTo(Budget source, int year, int month)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new Budget
        {
            Year = year,
            Month = month,
            Calendar = source.Calendar,
            CurrencyCode = source.CurrencyCode,
            TotalLimit = source.TotalLimit,
            AccountIds = [.. source.AccountIds],
            CategoryLimits = [.. source.CategoryLimits.Select(l => new BudgetCategoryLimit { CategoryId = l.CategoryId, Limit = l.Limit })],
            AlertsEnabled = source.AlertsEnabled,
            Rollover = source.Rollover,
        };
    }
}
