using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Budgets;

/// <summary>A suggested limit and what it is based on.</summary>
/// <param name="Average">The average net spending per month in minor units.</param>
/// <param name="Suggested">The average rounded up to a round amount.</param>
/// <param name="Months">The number of past periods (months or weeks) the average is taken over.</param>
public sealed record LimitSuggestion(long Average, long Suggested, int Months);

/// <summary>Suggested limits of a budget month.</summary>
/// <param name="Total">The overall limit (with the flex method: the flexible spending); <see langword="null"/> without history.</param>
/// <param name="Categories">Per expense category (sub-categories included) with spending in the past months.</param>
public sealed record BudgetSuggestion(LimitSuggestion? Total, IReadOnlyDictionary<Guid, LimitSuggestion> Categories)
{
    /// <summary>Gets an empty suggestion (no history yet).</summary>
    public static BudgetSuggestion None { get; } = new(null, new Dictionary<Guid, LimitSuggestion>());
}

/// <summary>
/// Suggestions for adjusting limits (§10.3): the average net spending of the last complete periods (months, weeks), rounded
/// up. A suggestion is only shown; nothing changes until the user takes it over and saves.
/// </summary>
public static class BudgetSuggestions
{
    /// <summary>The number of past months looked at.</summary>
    public const int Months = 3;

    /// <summary>Returns how many past periods are looked at: three months, four weeks or three two-week periods.</summary>
    public static int PeriodsFor(BudgetPeriod period) => period == BudgetPeriod.Week ? 4 : Months;

    /// <summary>Suggests limits for the budget month (year, month) from the months before it.</summary>
    /// <param name="accountIds">The budget scope; empty or <see langword="null"/> = all accounts in totals.</param>
    /// <param name="flexibleOnly">With the flex method the overall limit covers flexible spending only (D-28).</param>
    public static BudgetSuggestion Suggest(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        IEnumerable<Category> categories,
        int year,
        int month,
        PeriodCalendar calendar,
        int startDay,
        string currencyCode,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool flexibleOnly = false) =>
        Suggest(accounts, entries, categories, BudgetPeriods.Before(BudgetPeriod.Month, year, month, default, calendar, startDay, Months), currencyCode, accountIds, flexibleOnly);

    /// <summary>Suggests limits from the given past periods (newest first), e.g. the four weeks before a weekly budget.</summary>
    public static BudgetSuggestion Suggest(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        IEnumerable<Category> categories,
        IReadOnlyList<(DateOnly First, DateOnly Last)> pastPeriods,
        string currencyCode,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool flexibleOnly = false)
    {
        ArgumentNullException.ThrowIfNull(pastPeriods);
        var accountList = accounts.ToList();
        var entryList = entries.ToList();
        var categoryList = categories.ToList();
        var scope = accountIds is { Count: > 0 } ? accountIds : null;

        // Only months after the first recorded expense count: a new user's empty months are no "zero spending".
        var inScope = LedgerCalculator.InScope(accountList, scope)
            .Where(a => string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Id)
            .ToHashSet();
        var firstExpense = entryList.Where(e => e.Kind == EntryKind.Expense && inScope.Contains(e.AccountId)).Select(e => (DateOnly?)e.Date).Min();
        if (firstExpense is null)
        {
            return BudgetSuggestion.None;
        }

        var periods = pastPeriods.TakeWhile(p => p.Last >= firstExpense).Select(p => (From: p.First, To: p.Last)).ToList();

        if (periods.Count == 0)
        {
            return BudgetSuggestion.None;
        }

        var factor = Currencies.TryGet(currencyCode, out var currency) ? currency.MinorFactor : 100;
        LimitSuggestion? Average(Func<(DateOnly From, DateOnly To), long> spent)
        {
            var total = periods.Sum(p => Math.Max(0, spent(p)));
            var average = (long)Math.Ceiling(total / (double)periods.Count);
            return average > 0 ? new LimitSuggestion(average, RoundUp(average, factor), periods.Count) : null;
        }

        var overall = Average(p => flexibleOnly
            ? FlexCalculator.FlexibleSpent(accountList, entryList, categoryList, p.From, p.To, currencyCode, scope)
            : BudgetCalculator.NetExpense(accountList, entryList, p.From, p.To, currencyCode, scope));

        var byCategory = new Dictionary<Guid, LimitSuggestion>();
        foreach (var category in categoryList.Where(c => c.Kind == CategoryKind.Expense && c.ParentId is null))
        {
            if (Average(p => BudgetCalculator.NetExpense(accountList, entryList, p.From, p.To, currencyCode, scope, [category.Id], categoryList)) is { } suggestion)
            {
                byCategory[category.Id] = suggestion;
            }
        }

        return new BudgetSuggestion(overall, byCategory);
    }

    /// <summary>
    /// Rounds an amount up to two significant digits, at least to whole currency units: 437.20 becomes 440 and
    /// 1,234,567 becomes 1,300,000.
    /// </summary>
    /// <param name="amount">The amount in minor units.</param>
    /// <param name="minorFactor">Minor units per currency unit (100 for the euro, 1 for the rial).</param>
    public static long RoundUp(long amount, long minorFactor)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var step = Math.Max(Math.Max(1, minorFactor), (long)Math.Pow(10, Math.Max(0, (int)Math.Floor(Math.Log10(amount)) - 1)));
        return (amount + step - 1) / step * step;
    }
}
