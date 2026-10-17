using System.Globalization;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>Formats a filtered transaction count and its actual income/spending currency totals.</summary>
internal static class TransactionSummary
{
    /// <summary>Omits the total caption when the selected entries have no income/spending currency effect.</summary>
    internal static string Format(IReadOnlyList<LedgerEntry> entries, IReadOnlyDictionary<Guid, Account> accounts,
        Translator translator, CultureInfo culture)
    {
        var net = EntrySearch.NetByCurrency(entries, accounts);
        // Transfers and asset movements have no result currency; a real currency summing to zero still has a total.
        return net.Count == 0
            ? translator.Format("Report_TagCount", entries.Count)
            : translator.Format("Tx_FilterTotal", entries.Count,
                string.Join("  ", net.Select(value => MoneyText.Format(value.Value, value.Key, culture, showPlus: true))));
    }
}
