using System.Globalization;
using Vafadar.Core.Text;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>Kind groups offered as filter chips in the transaction list (UI-04).</summary>
public enum KindFilter
{
    /// <summary>Every entry.</summary>
    All = 0,

    /// <summary>Expenses and their refunds.</summary>
    Expenses = 1,

    /// <summary>Income and income reversals.</summary>
    Income = 2,

    /// <summary>Transfers and balance adjustments – movements that are neither income nor expense.</summary>
    Transfers = 3,
}

/// <summary>Filter of the transaction list. <see langword="null"/> values do not filter.</summary>
public sealed record EntryFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    KindFilter Kind = KindFilter.All,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    bool UnreviewedOnly = false,
    string? Text = null);

/// <summary>The entries of one day with their net income/expense effect per currency (transfers excluded).</summary>
public sealed record EntryDay(DateOnly Date, IReadOnlyList<LedgerEntry> Entries, IReadOnlyDictionary<string, long> Net);

/// <summary>Search, filter and grouping of entries for the transaction list (UI-04).</summary>
public static class EntrySearch
{
    /// <summary>Returns whether <paramref name="kind"/> belongs to <paramref name="filter"/>.</summary>
    public static bool InKindGroup(EntryKind kind, KindFilter filter) => filter switch
    {
        KindFilter.Expenses => kind is EntryKind.Expense or EntryKind.Refund,
        KindFilter.Income => kind is EntryKind.Income or EntryKind.IncomeReversal,
        KindFilter.Transfers => kind is EntryKind.Transfer or EntryKind.Adjustment,
        _ => true,
    };

    /// <summary>Applies <paramref name="filter"/>; the order of <paramref name="entries"/> is kept.</summary>
    /// <param name="entries">The entries to filter.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="categoryName">Returns the display name of a category id (used by the text search).</param>
    /// <param name="accounts">All accounts by id, used to match amounts in their currency.</param>
    public static IEnumerable<LedgerEntry> Apply(
        IEnumerable<LedgerEntry> entries,
        EntryFilter filter,
        Func<Guid, string?> categoryName,
        IReadOnlyDictionary<Guid, Account> accounts)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(categoryName);
        ArgumentNullException.ThrowIfNull(accounts);

        var text = Normalize(filter.Text);
        return entries.Where(e =>
            (filter.From is not { } from || e.Date >= from)
            && (filter.To is not { } to || e.Date <= to)
            && InKindGroup(e.Kind, filter.Kind)
            && (filter.AccountId is not { } account || e.AccountId == account || e.ToAccountId == account)
            && (filter.CategoryId is not { } category || e.CategoryId == category)
            && (!filter.UnreviewedOnly || e.Review == ReviewState.Unreviewed)
            && (text.Length == 0 || MatchesText(e, text, categoryName, accounts)));
    }

    /// <summary>Groups entries by date (newest first) and sums the income/expense effect of each day per currency.</summary>
    public static IReadOnlyList<EntryDay> ByDay(IEnumerable<LedgerEntry> entries, IReadOnlyDictionary<Guid, Account> accounts)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(accounts);

        return
        [
            .. entries
                .GroupBy(e => e.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new EntryDay(g.Key, [.. g], DayNet(g, accounts))),
        ];
    }

    /// <summary>Signed income/expense effect of an entry: positive for income and refunds, negative for expenses.</summary>
    public static long SignedResult(LedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Kind switch
        {
            EntryKind.Income or EntryKind.Refund => entry.Amount,
            EntryKind.Expense or EntryKind.IncomeReversal => -entry.Amount,
            _ => 0,
        };
    }

    private static SortedDictionary<string, long> DayNet(IEnumerable<LedgerEntry> entries, IReadOnlyDictionary<Guid, Account> accounts)
    {
        var net = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var result = SignedResult(entry);
            if (result != 0 && accounts.TryGetValue(entry.AccountId, out var account))
            {
                net[account.CurrencyCode] = net.GetValueOrDefault(account.CurrencyCode) + result;
            }
        }

        return net;
    }

    private static bool MatchesText(LedgerEntry entry, string text, Func<Guid, string?> categoryName, IReadOnlyDictionary<Guid, Account> accounts)
    {
        if (Contains(entry.Title, text) || Contains(entry.Payee, text) || Contains(entry.Note, text)
            || (entry.CategoryId is { } category && Contains(categoryName(category), text)))
        {
            return true;
        }

        // Amounts match by their plain value, e.g. "12.5" or "12.50" finds 12.50 EUR (digits in any script).
        if (accounts.TryGetValue(entry.AccountId, out var account)
            && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var searched))
        {
            var currency = Currencies.TryGet(account.CurrencyCode, out var known) ? known : new Currency(account.CurrencyCode, 2);
            return MoneyAmount.ToDecimal(entry.Amount, currency) == searched;
        }

        return false;
    }

    private static bool Contains(string? value, string text) =>
        value is not null && Normalize(value).Contains(text, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value) => value is null ? string.Empty : Digits.ToAscii(value.Trim()).Replace('ي', 'ی').Replace('ك', 'ک');
}
