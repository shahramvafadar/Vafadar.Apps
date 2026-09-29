using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Budgets;

/// <summary>What was planned and what was spent in one flex group.</summary>
/// <param name="Planned">Expected amount of the month in minor units (from the plans).</param>
/// <param name="Spent">Net spending of the month in the group's categories.</param>
/// <param name="UnknownCount">Planned occurrences without a known amount (the planned total is incomplete).</param>
public sealed record FlexGroup(long Planned, long Spent, int UnknownCount = 0)
{
    /// <summary>Gets what is still expected (never below zero).</summary>
    public long Open => Math.Max(0, Planned - Spent);
}

/// <summary>A month read with the flex method (D-28).</summary>
/// <param name="Fixed">Fixed bills: expected from the plans of the month.</param>
/// <param name="NonMonthly">Non-monthly bills: the monthly share of their plans, and what was paid this month.</param>
/// <param name="Flexible">Everything else against the one flexible limit.</param>
public sealed record FlexSummary(FlexGroup Fixed, FlexGroup NonMonthly, BudgetStatus Flexible)
{
    /// <summary>Gets the month's total plan: fixed + non-monthly share + flexible limit.</summary>
    public long Total => Fixed.Planned + NonMonthly.Planned + Flexible.Limit;
}

/// <summary>
/// Flex budgeting (§10.3, D-28): expense categories are fixed, non-monthly or flexible. Fixed bills are expected from
/// the plans, non-monthly bills get their monthly share (BUD-09), and one limit covers all flexible spending. Every
/// amount is counted once: flexible spending is the total minus the fixed and the non-monthly spending (BUD-12).
/// </summary>
public static class FlexCalculator
{
    /// <summary>Returns the spending type of a category; a sub-category follows its parent.</summary>
    public static SpendingType TypeOf(Category category, IReadOnlyDictionary<Guid, Category> byId)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(byId);
        return category.ParentId is { } parent && byId.TryGetValue(parent, out var top) ? top.SpendingType : category.SpendingType;
    }

    /// <summary>Returns the expense categories of a spending type (sub-categories included).</summary>
    public static IReadOnlyList<Guid> CategoriesOf(IEnumerable<Category> categories, SpendingType type)
    {
        var list = categories.Where(c => c.Kind == CategoryKind.Expense).ToList();
        var byId = list.ToDictionary(c => c.Id);
        return [.. list.Where(c => TypeOf(c, byId) == type).Select(c => c.Id)];
    }

    /// <summary>Reads a month with the flex method.</summary>
    /// <param name="flexibleLimit">The flexible limit including any rollover.</param>
    public static FlexSummary Summarize(
        long flexibleLimit,
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        IEnumerable<Category> categories,
        DateOnly from,
        DateOnly to,
        string currencyCode,
        DateOnly today,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool confirmedOnly = false)
    {
        var accountList = accounts.ToList();
        var entryList = entries.ToList();
        var categoryList = categories.ToList();
        var stateList = states.ToList();
        var scheduleList = schedules.Where(s => s.Kind == EntryKind.Expense && s.State == ScheduleState.Active).ToList();
        var byAccount = accountList.ToDictionary(a => a.Id);
        var fixedIds = CategoriesOf(categoryList, SpendingType.Fixed);
        var nonMonthlyIds = CategoriesOf(categoryList, SpendingType.NonMonthly);

        long Spent(IReadOnlyCollection<Guid>? ids) =>
            BudgetCalculator.NetExpense(accountList, entryList, from, to, currencyCode, accountIds, ids, categoryList, confirmedOnly);

        List<Schedule> PlansIn(IReadOnlyList<Guid> ids) => [.. scheduleList.Where(s => s.CategoryId is { } c && ids.Contains(c))];

        // Fixed: the occurrences of the month, counted as they fall (BUD-10).
        var fixedPlans = BudgetPlanning.PlannedInPeriod(PlansIn(fixedIds), stateList, byAccount, from, to, currencyCode, today);

        // Non-monthly: the monthly share of each plan (a yearly 600 is 50), a monthly plan with its real occurrences.
        long nonMonthlyPlanned = 0;
        var nonMonthlyUnknown = 0;
        foreach (var schedule in PlansIn(nonMonthlyIds))
        {
            if (!byAccount.TryGetValue(schedule.AccountId, out var account)
                || !string.Equals(account.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (BudgetPlanning.MonthlyEquivalent(schedule) is { } share)
            {
                nonMonthlyPlanned += share;
                continue;
            }

            var planned = BudgetPlanning.PlannedInPeriod([schedule], stateList, byAccount, from, to, currencyCode, today);
            nonMonthlyPlanned += planned.Total;
            nonMonthlyUnknown += planned.UnknownCount;
        }

        var fixedSpent = Spent(fixedIds);
        var nonMonthlySpent = Spent(nonMonthlyIds);
        var flexibleSpent = Spent(null) - fixedSpent - nonMonthlySpent;
        return new FlexSummary(
            new FlexGroup(fixedPlans.Total, fixedSpent, fixedPlans.UnknownCount),
            new FlexGroup(nonMonthlyPlanned, nonMonthlySpent, nonMonthlyUnknown),
            new BudgetStatus(flexibleLimit, flexibleSpent));
    }

    /// <summary>
    /// The spending a budget's overall limit is measured against: everything, or with the flex method the flexible
    /// spending only. Home, alerts and rollover use the same number as the budget page (§10.3).
    /// </summary>
    public static long SpentAgainstLimit(
        Budget budget,
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        IEnumerable<Category> categories,
        DateOnly from,
        DateOnly to,
        bool confirmedOnly = false)
    {
        ArgumentNullException.ThrowIfNull(budget);
        var scope = budget.AccountIds.Count > 0 ? budget.AccountIds : null;
        return budget.Method == BudgetMethod.Flex
            ? FlexibleSpent(accounts, entries, categories, from, to, budget.CurrencyCode, scope, confirmedOnly)
            : BudgetCalculator.NetExpense(accounts, entries, from, to, budget.CurrencyCode, scope, confirmedOnly: confirmedOnly);
    }

    /// <summary>
    /// Net flexible spending of a month: all spending minus the fixed and non-monthly categories. Rollover of a flex
    /// budget carries the rest of this number (§10.3).
    /// </summary>
    public static long FlexibleSpent(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        IEnumerable<Category> categories,
        DateOnly from,
        DateOnly to,
        string currencyCode,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool confirmedOnly = false)
    {
        var accountList = accounts.ToList();
        var entryList = entries.ToList();
        var categoryList = categories.ToList();
        var excluded = CategoriesOf(categoryList, SpendingType.Fixed).Concat(CategoriesOf(categoryList, SpendingType.NonMonthly)).ToList();
        return BudgetCalculator.NetExpense(accountList, entryList, from, to, currencyCode, accountIds, null, null, confirmedOnly)
            - (excluded.Count == 0 ? 0 : BudgetCalculator.NetExpense(accountList, entryList, from, to, currencyCode, accountIds, excluded, categoryList, confirmedOnly));
    }
}
