using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Shared Home indexing preserves original balance, scope and checked-order contracts.</summary>
public sealed class SharedBalanceIndexTests
{
    [Theory, Trait("AT", "AT-82")]
    [InlineData(EntryKind.Income), InlineData(EntryKind.Expense), InlineData(EntryKind.Transfer), InlineData(EntryKind.Refund)]
    [InlineData(EntryKind.IncomeReversal), InlineData(EntryKind.Adjustment), InlineData(EntryKind.AssetPurchase), InlineData(EntryKind.AssetSale)]
    public void Both_total_views_match_original_per_account_effects_and_dates(EntryKind kind)
    {
        var ledger = new LedgerBuilder();
        var first = ledger.Account("Fictitious EUR", 100); var second = ledger.Account("Fictitious USD", 200, currency: "USD");
        var entry = ledger.Add(kind, first, 12); entry.ToAccountId = second.Id; entry.ToAmount = 1450; entry.Direction = AdjustmentDirection.Decrease;
        ledger.Add(kind, first, 3, LedgerBuilder.Day1.AddDays(-1));
        ledger.Add(kind, first, 4, LedgerBuilder.Day1.AddDays(20));
        ledger.Add(kind, second, 5, review: ReviewState.Unreviewed);
        var index = new AccountEntryIndex(ledger.Entries);
        foreach (var confirmed in new[] { false, true })
        foreach (var day in new[] { LedgerBuilder.Day1.AddDays(-1), LedgerBuilder.Day1, LedgerBuilder.Day1.AddDays(30) })
        {
            var totals = LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, day, confirmedOnly: confirmed);
            Assert.Equal(ledger.Balance(first, day, confirmed), totals["EUR"]);
            Assert.Equal(ledger.Balance(second, day, confirmed), totals["USD"]);
        }
        Assert.Equal(4, ledger.Entries.Count);
    }

    [Fact, Trait("AT", "AT-82")]
    public void One_captured_source_serves_rows_and_both_totals_without_reenumeration()
    {
        var ledger = new LedgerBuilder(); var first = ledger.Account("First", 100); var second = ledger.Account("Second", 200);
        ledger.Transfer(first, second, 10); ledger.Add(EntryKind.Expense, first, 5, review: ReviewState.Unreviewed);
        var calls = 0;
        IEnumerable<LedgerEntry> Once()
        {
            Assert.Equal(1, ++calls);
            foreach (var entry in ledger.Entries) yield return entry;
        }
        var index = new AccountEntryIndex(Once());
        Assert.Equal(29500, LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1)["EUR"]);
        Assert.Equal(30000, LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1, confirmedOnly: true)["EUR"]);
        Assert.Equal(8500, LedgerCalculator.Balance(first, index.For(first.Id), LedgerBuilder.Day1));
        Assert.Equal(21000, LedgerCalculator.Balance(second, index.For(second.Id), LedgerBuilder.Day1));
        Assert.Equal(1, calls);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Explicit_scope_keeps_archived_and_excluded_accounts_and_sorted_currencies()
    {
        var ledger = new LedgerBuilder(); var first = ledger.Account("USD", 2, currency: "USD"); var second = ledger.Account("EUR", 3);
        first.IsArchived = true; second.IncludeInTotals = false;
        var index = new AccountEntryIndex(ledger.Entries);
        Assert.Empty(LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1));
        var totals = LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1, [first.Id, second.Id]);
        Assert.Equal(new[] { "EUR", "USD" }, totals.Keys); Assert.Equal(300, totals["EUR"]); Assert.Equal(200, totals["USD"]);
        Assert.Empty(LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1, []));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Checked_account_and_currency_addition_still_refuses_overflow()
    {
        var ledger = new LedgerBuilder(); var first = ledger.Account("First", 0); var second = ledger.Account("Second", 1);
        first.OpeningBalance = long.MaxValue;
        var index = new AccountEntryIndex(ledger.Entries);
        Assert.Throws<OverflowException>(() => LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1));
        ledger.Add(EntryKind.Income, first, 1); index = new AccountEntryIndex(ledger.Entries);
        Assert.Throws<OverflowException>(() => LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1, [first.Id]));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Legacy_self_transfer_is_still_one_original_effect_in_both_views()
    {
        var ledger = new LedgerBuilder(); var account = ledger.Account("Legacy", 100); ledger.Transfer(account, account, 10, 11);
        var index = new AccountEntryIndex(ledger.Entries);
        Assert.Equal(10100, LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1)["EUR"]);
        Assert.Equal(10100, LedgerCalculator.TotalBalancesFromIndex(ledger.Accounts, index, LedgerBuilder.Day1, confirmedOnly: true)["EUR"]);
        Assert.Same(ledger.Entries[0], Assert.Single(index.For(account.Id)));
    }
}