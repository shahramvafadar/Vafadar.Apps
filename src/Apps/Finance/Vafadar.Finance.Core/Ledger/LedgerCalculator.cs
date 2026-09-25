using Vafadar.Finance.Core.Accounts;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>Filter shared by every number of one view (DASH-01, FIN-14).</summary>
/// <param name="From">First day (inclusive).</param>
/// <param name="To">Last day (inclusive).</param>
/// <param name="AccountIds">Accounts in scope; <see langword="null"/> = all accounts included in totals.</param>
/// <param name="ConfirmedOnly">Only confirmed entries (FIN-12).</param>
public sealed record LedgerFilter(DateOnly From, DateOnly To, IReadOnlyCollection<Guid>? AccountIds = null, bool ConfirmedOnly = false);

/// <summary>Income and expense of a period in one currency (§24.1).</summary>
public sealed record PeriodTotals(string CurrencyCode, long Income, long IncomeReversals, long GrossExpense, long Refunds)
{
    /// <summary>Gets income minus income reversals.</summary>
    public long NetIncome => Income - IncomeReversals;

    /// <summary>Gets gross expense minus refunds; may be negative (REF-03).</summary>
    public long NetExpense => GrossExpense - Refunds;

    /// <summary>Gets net income minus net expense.</summary>
    public long Result => NetIncome - NetExpense;
}

/// <summary>Expense of one category in one currency; <see cref="CategoryId"/> is <see langword="null"/> for entries without category.</summary>
public sealed record CategoryExpense(string CurrencyCode, Guid? CategoryId, long GrossExpense, long Refunds)
{
    /// <summary>Gets gross expense minus refunds; may be negative (REF-03).</summary>
    public long Net => GrossExpense - Refunds;
}

/// <summary>Count and sum of unreviewed entries in one currency (FIN-12).</summary>
public sealed record UnreviewedSummary(string CurrencyCode, int Count, long NetEffect);

/// <summary>
/// Pure calculations over accounts and entries. All views use these functions, so one concept always has one formula
/// (Q-05). Amounts are minor units; multi-currency results are returned per currency (FX-02).
/// </summary>
public static class LedgerCalculator
{
    /// <summary>Returns whether an entry lies before the account's opening date and is therefore not in its balance (FIN-04).</summary>
    public static bool IsBeforeOpening(LedgerEntry entry, Account account)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(account);
        return entry.Date < account.OpeningDate;
    }

    /// <summary>Balance of <paramref name="account"/> at the end of <paramref name="at"/>.</summary>
    public static long Balance(Account account, IEnumerable<LedgerEntry> entries, DateOnly at, bool confirmedOnly = false)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(entries);

        if (at < account.OpeningDate)
        {
            return 0;
        }

        var balance = account.OpeningBalance;
        foreach (var entry in entries)
        {
            if (entry.Date > at || entry.Date < account.OpeningDate || (confirmedOnly && entry.Review != ReviewState.Confirmed))
            {
                continue;
            }

            balance += entry.EffectOn(account.Id);
        }

        return balance;
    }

    /// <summary>Sum of balances per currency for the accounts in scope.</summary>
    public static IReadOnlyDictionary<string, long> TotalBalances(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        DateOnly at,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool confirmedOnly = false)
    {
        var totals = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var account in InScope(accounts, accountIds))
        {
            totals[account.CurrencyCode] = totals.GetValueOrDefault(account.CurrencyCode) + Balance(account, entries, at, confirmedOnly);
        }

        return totals;
    }

    /// <summary>Income and expense of the period per currency. Transfers and adjustments never count (FIN-02, FIN-10).</summary>
    public static IReadOnlyList<PeriodTotals> Totals(IEnumerable<Account> accounts, IEnumerable<LedgerEntry> entries, LedgerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var scope = InScope(accounts, filter.AccountIds).ToDictionary(a => a.Id);
        var sums = new SortedDictionary<string, long[]>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (!Matches(entry, filter) || !scope.TryGetValue(entry.AccountId, out var account) || IsBeforeOpening(entry, account))
            {
                continue;
            }

            var slot = entry.Kind switch
            {
                EntryKind.Income => 0,
                EntryKind.IncomeReversal => 1,
                EntryKind.Expense => 2,
                EntryKind.Refund => 3,
                _ => -1,
            };

            if (slot < 0)
            {
                continue;
            }

            if (!sums.TryGetValue(account.CurrencyCode, out var values))
            {
                sums[account.CurrencyCode] = values = new long[4];
            }

            values[slot] += entry.Amount;
        }

        return [.. sums.Select(pair => new PeriodTotals(pair.Key, pair.Value[0], pair.Value[1], pair.Value[2], pair.Value[3]))];
    }

    /// <summary>
    /// Expense per category and currency for the period: gross expense, refunds and net = gross − refunds (REF-03).
    /// <paramref name="group"/> maps a category to the one it is reported under, e.g. its parent; entries without a
    /// category are reported under <see langword="null"/>. Sorted by net expense, largest first.
    /// </summary>
    public static IReadOnlyList<CategoryExpense> ExpenseByCategory(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        LedgerFilter filter,
        Func<Guid?, Guid?> group)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(group);
        var scope = InScope(accounts, filter.AccountIds).ToDictionary(a => a.Id);
        var sums = new Dictionary<(string Currency, Guid? Category), (long Gross, long Refunds)>();

        foreach (var entry in entries)
        {
            if (entry.Kind is not (EntryKind.Expense or EntryKind.Refund)
                || !Matches(entry, filter)
                || !scope.TryGetValue(entry.AccountId, out var account)
                || IsBeforeOpening(entry, account))
            {
                continue;
            }

            var key = (account.CurrencyCode, group(entry.CategoryId));
            var (gross, refunds) = sums.GetValueOrDefault(key);
            sums[key] = entry.Kind == EntryKind.Expense ? (gross + entry.Amount, refunds) : (gross, refunds + entry.Amount);
        }

        return
        [
            .. sums
                .Select(pair => new CategoryExpense(pair.Key.Currency, pair.Key.Category, pair.Value.Gross, pair.Value.Refunds))
                .OrderBy(c => c.CurrencyCode, StringComparer.Ordinal)
                .ThenByDescending(c => c.Net),
        ];
    }

    /// <summary>Unreviewed entries per currency with their net balance effect on the accounts in scope.</summary>
    public static IReadOnlyList<UnreviewedSummary> Unreviewed(IEnumerable<Account> accounts, IEnumerable<LedgerEntry> entries, IReadOnlyCollection<Guid>? accountIds = null)
    {
        var scope = InScope(accounts, accountIds).ToList();
        var result = new SortedDictionary<string, (int Count, long Sum)>(StringComparer.Ordinal);

        foreach (var entry in entries.Where(e => e.Review == ReviewState.Unreviewed))
        {
            foreach (var account in scope.Where(a => a.Id == entry.AccountId || a.Id == entry.ToAccountId))
            {
                var current = result.GetValueOrDefault(account.CurrencyCode);
                result[account.CurrencyCode] = (current.Count + 1, current.Sum + entry.EffectOn(account.Id));
            }
        }

        return [.. result.Select(pair => new UnreviewedSummary(pair.Key, pair.Value.Count, pair.Value.Sum))];
    }

    /// <summary>Returns the entries that make up a number of a view, with the same filter (REP-01).</summary>
    public static IEnumerable<LedgerEntry> Matching(IEnumerable<LedgerEntry> entries, LedgerFilter filter, IReadOnlyCollection<Guid> scopeAccountIds, params EntryKind[] kinds)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return entries.Where(e => Matches(e, filter) && scopeAccountIds.Contains(e.AccountId) && (kinds.Length == 0 || kinds.Contains(e.Kind)));
    }

    /// <summary>Accounts in scope: the explicit list, or every account included in totals.</summary>
    public static IEnumerable<Account> InScope(IEnumerable<Account> accounts, IReadOnlyCollection<Guid>? accountIds)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        return accountIds is null ? accounts.Where(a => a.IncludeInTotals) : accounts.Where(a => accountIds.Contains(a.Id));
    }

    private static bool Matches(LedgerEntry entry, LedgerFilter filter) =>
        entry.Date >= filter.From && entry.Date <= filter.To && (!filter.ConfirmedOnly || entry.Review == ReviewState.Confirmed);
}
