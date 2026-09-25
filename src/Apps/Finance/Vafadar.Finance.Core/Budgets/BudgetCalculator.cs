using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Budgets;

/// <summary>Status of a budget limit (BUD-04/05).</summary>
/// <param name="Limit">Limit in minor units.</param>
/// <param name="Spent">Net eligible expense (may be negative after refunds).</param>
public sealed record BudgetStatus(long Limit, long Spent)
{
    /// <summary>Gets the remaining amount (negative when overspent).</summary>
    public long Remaining => Limit - Spent;

    /// <summary>Gets the usage in percent; <see langword="null"/> for a zero limit (no division by zero, BUD-05).</summary>
    public decimal? UsagePercent => Limit > 0 ? Spent * 100m / Limit : null;

    /// <summary>Gets a value indicating whether the limit is exceeded.</summary>
    public bool IsOver => Spent > Limit;

    /// <summary>Gets the alert level for the 80 %/100 % thresholds (BUD-06).</summary>
    public BudgetAlert Alert => IsOver ? BudgetAlert.Exceeded : Limit > 0 && Spent * 100 >= Limit * 80 ? BudgetAlert.Near : BudgetAlert.None;
}

/// <summary>Budget alert levels.</summary>
public enum BudgetAlert
{
    /// <summary>Below 80 %.</summary>
    None,

    /// <summary>80 % or more.</summary>
    Near,

    /// <summary>Above the limit.</summary>
    Exceeded,
}

/// <summary>Budget calculations (docs/02-domain-design.md §9).</summary>
public static class BudgetCalculator
{
    /// <summary>
    /// Net eligible expense: expenses minus refunds of the budget's accounts within the period, optionally limited to
    /// categories (a parent includes its children). Transfers, opening balances and adjustments never count (BUD-03).
    /// </summary>
    public static long NetExpense(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        DateOnly from,
        DateOnly to,
        string currencyCode,
        IReadOnlyCollection<Guid>? accountIds = null,
        IReadOnlyCollection<Guid>? categoryIds = null,
        IEnumerable<Category>? categories = null,
        bool confirmedOnly = false)
    {
        var scope = LedgerCalculator.InScope(accounts, accountIds is { Count: > 0 } ? accountIds : null)
            .Where(a => string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(a => a.Id);

        HashSet<Guid>? categorySet = null;
        if (categoryIds is not null)
        {
            categorySet = [.. categoryIds];
            foreach (var child in categories ?? [])
            {
                if (child.ParentId is { } parent && categoryIds.Contains(parent))
                {
                    categorySet.Add(child.Id);
                }
            }
        }

        long total = 0;
        foreach (var entry in entries)
        {
            if (entry.Date < from || entry.Date > to
                || (confirmedOnly && entry.Review != ReviewState.Confirmed)
                || !scope.TryGetValue(entry.AccountId, out var account)
                || LedgerCalculator.IsBeforeOpening(entry, account)
                || (categorySet is not null && (entry.CategoryId is not { } c || !categorySet.Contains(c))))
            {
                continue;
            }

            total += entry.Kind switch
            {
                EntryKind.Expense => entry.Amount,
                EntryKind.Refund => -entry.Amount,
                _ => 0,
            };
        }

        return total;
    }
}
