using System.Globalization;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.DataFiles;

/// <summary>
/// Exports entries as CSV (IO-01..06). Dates are ISO (<c>yyyy-MM-dd</c>, Gregorian) and amounts invariant decimals, so
/// the file works in any tool and language. A transfer stays one row with both accounts, so a re-import cannot turn
/// it into an income and an expense (IO-03). CSV is not a backup: plans, budgets and settings are not included (IO-04).
/// </summary>
public static class CsvExport
{
    /// <summary>Column names of the app's own format; the importer recognises files by them.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        "id", "date", "kind", "amount", "currency", "account", "to_account", "to_amount", "to_currency",
        "category", "title", "payee", "note", "original_amount", "original_currency", "review", "refund_of", "group_id",
    ];

    /// <summary>Returns the CSV text.</summary>
    /// <param name="entries">Entries to export (already filtered by period and accounts).</param>
    /// <param name="accounts">All accounts by id.</param>
    /// <param name="categoryName">Display name of a category id.</param>
    /// <param name="includeNotes">Whether notes and payees are included (IO-05).</param>
    public static string Write(IEnumerable<LedgerEntry> entries, IReadOnlyDictionary<Guid, Account> accounts, Func<Guid?, string> categoryName, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(categoryName);
        var rows = new List<IReadOnlyList<string>> { Columns };

        foreach (var entry in entries.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt))
        {
            accounts.TryGetValue(entry.AccountId, out var account);
            Account? destination = null;
            if (entry.ToAccountId is { } to)
            {
                accounts.TryGetValue(to, out destination);
            }

            var currency = account?.CurrencyCode ?? string.Empty;
            rows.Add(
            [
                entry.Id.ToString("D", CultureInfo.InvariantCulture),
                entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                entry.Kind.ToString(),
                Amount(entry.Amount, currency),
                currency,
                Csv.Text(account?.Name),
                Csv.Text(destination?.Name),
                entry.Kind == EntryKind.Transfer && destination is not null ? Amount(entry.ToAmount ?? entry.Amount, destination.CurrencyCode) : string.Empty,
                destination?.CurrencyCode ?? string.Empty,
                entry.CategoryId is null ? string.Empty : Csv.Text(categoryName(entry.CategoryId)),
                Csv.Text(entry.Title),
                includeNotes ? Csv.Text(entry.Payee) : string.Empty,
                includeNotes ? Csv.Text(entry.Note) : string.Empty,
                entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency ? Amount(original, originalCurrency) : string.Empty,
                entry.OriginalCurrencyCode ?? string.Empty,
                entry.Review.ToString(),
                entry.RefundOfId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.GroupId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty,
            ]);
        }

        return Csv.Write(rows);
    }

    private static string Amount(long minor, string currencyCode)
    {
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        return MoneyAmount.ToDecimal(minor, currency).ToString("F" + currency.MinorDigits, CultureInfo.InvariantCulture);
    }
}
