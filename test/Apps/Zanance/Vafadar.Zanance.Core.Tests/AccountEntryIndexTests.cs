using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>The performance index routes original entries; the established balance calculation remains the oracle.</summary>
public sealed class AccountEntryIndexTests
{
    [Theory, Trait("AT", "AT-82")]
    [InlineData(EntryKind.Income), InlineData(EntryKind.Expense), InlineData(EntryKind.Transfer), InlineData(EntryKind.Refund)]
    [InlineData(EntryKind.IncomeReversal), InlineData(EntryKind.Adjustment), InlineData(EntryKind.AssetPurchase), InlineData(EntryKind.AssetSale)]
    public void Indexed_balances_keep_every_kind_date_review_and_currency_effect(EntryKind kind)
    {
        var ledger = new LedgerBuilder();
        var from = ledger.Account("EUR", 100); var to = ledger.Account("USD", 200, currency: "USD");
        var entry = ledger.Add(kind, from, 12, LedgerBuilder.Day1);
        entry.ToAccountId = to.Id; entry.ToAmount = 1450; entry.Direction = AdjustmentDirection.Decrease;
        ledger.Add(kind, from, 3, LedgerBuilder.Day1.AddDays(-1));
        ledger.Add(kind, from, 4, LedgerBuilder.Day1.AddDays(20));
        ledger.Add(kind, to, 5, LedgerBuilder.Day1, review: ReviewState.Unreviewed);
        var index = new AccountEntryIndex(ledger.Entries);
        foreach (var account in ledger.Accounts)
        {
            foreach (var confirmed in new[] { false, true })
            {
                foreach (var at in new[] { LedgerBuilder.Day1.AddDays(-1), LedgerBuilder.Day1, LedgerBuilder.Day1.AddDays(30) })
                {
                    Assert.Equal(LedgerCalculator.Balance(account, ledger.Entries, at, confirmed),
                        LedgerCalculator.Balance(account, index.For(account.Id), at, confirmed));
                }
            }
        }
        Assert.Equal(4, ledger.Entries.Count);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Transfer_is_one_original_entry_in_each_endpoint_and_keeps_the_order()
    {
        var ledger = new LedgerBuilder(); var first = ledger.Account("First", 0); var second = ledger.Account("Second", 0);
        var expense = ledger.Add(EntryKind.Expense, first, 1);
        var transfer = ledger.Transfer(first, second, 2);
        var income = ledger.Add(EntryKind.Income, second, 3);
        var index = new AccountEntryIndex(ledger.Entries);
        Assert.Equal(new[] { expense, transfer }, index.For(first.Id));
        Assert.Equal(new[] { transfer, income }, index.For(second.Id));
        Assert.Same(transfer, index.For(second.Id).First()); Assert.Equal(3, ledger.Entries.Count);
        Assert.Equal(-300, LedgerCalculator.Balance(first, index.For(first.Id), LedgerBuilder.Day1));
        Assert.Equal(500, LedgerCalculator.Balance(second, index.For(second.Id), LedgerBuilder.Day1));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Legacy_self_transfer_is_not_counted_twice()
    {
        var ledger = new LedgerBuilder(); var account = ledger.Account("Legacy", 100);
        ledger.Transfer(account, account, 10, 11);
        var index = new AccountEntryIndex(ledger.Entries);
        Assert.Single(index.For(account.Id));
        Assert.Equal(10100, LedgerCalculator.Balance(account, index.For(account.Id), LedgerBuilder.Day1));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Source_is_enumerated_once_and_repeated_reads_keep_the_original_identity()
    {
        var entry = new LedgerEntry(); var enumerations = 0;
        IEnumerable<LedgerEntry> Once() { Assert.Equal(1, ++enumerations); yield return entry; }
        var index = new AccountEntryIndex(Once());
        Assert.Same(entry, Assert.Single(index.For(entry.AccountId)));
        Assert.Same(entry, Assert.Single(index.For(entry.AccountId)));
        Assert.Empty(index.For(Guid.NewGuid()));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Scope_exclusion_and_checked_overflow_stay_with_the_balance_calculator()
    {
        var ledger = new LedgerBuilder(); var included = ledger.Account("Included", 1); var excluded = ledger.Account("Excluded", 0);
        excluded.OpeningBalance = long.MaxValue; excluded.IncludeInTotals = false;
        ledger.Add(EntryKind.Income, excluded, 1);
        Assert.Equal(100, LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, LedgerBuilder.Day1)["EUR"]);
        Assert.Throws<OverflowException>(() => LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, LedgerBuilder.Day1, [excluded.Id]));
        included.IsArchived = true;
        Assert.Empty(LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, LedgerBuilder.Day1));
        Assert.Equal(100, LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, LedgerBuilder.Day1, [included.Id])["EUR"]);
    }
}
