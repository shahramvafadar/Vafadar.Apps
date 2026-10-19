using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Global review counts represent transactions rather than their currency effects.</summary>
[Trait("AT", "AT-374")]
public sealed class UnreviewedEntryCountTests
{
    /// <summary>One transfer is one item even when both different-currency endpoints are included.</summary>
    [Theory, InlineData("EUR"), InlineData("USD")]
    public void Transfer_counts_once_across_currency_groups(string currency)
    {
        var source = new Account { Name = "Fictitious source", CurrencyCode = "EUR" };
        var destination = new Account { Name = "Fictitious destination", CurrencyCode = currency };
        var entry = new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id,
            Kind = EntryKind.Transfer, Amount = 100, ToAmount = 120, Review = ReviewState.Unreviewed };
        Assert.Equal(1, LedgerCalculator.UnreviewedEntryCount([source, destination], [entry]));
    }

    /// <summary>Explicit and default selection retain existing account scope, including destination-only transfers.</summary>
    [Fact]
    public void Scope_keeps_destination_only_entries_and_ignores_unrelated_or_confirmed_rows()
    {
        var source = new Account { Name = "Excluded", CurrencyCode = "EUR", IncludeInTotals = false };
        var destination = new Account { Name = "Included", CurrencyCode = "EUR", IsArchived = true };
        var transfer = new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id,
            Kind = EntryKind.Transfer, Amount = 100, Review = ReviewState.Unreviewed };
        var excluded = new LedgerEntry { AccountId = source.Id, Amount = 5, Review = ReviewState.Unreviewed };
        var confirmed = new LedgerEntry { AccountId = destination.Id, Amount = 7, Review = ReviewState.Confirmed };
        Assert.Equal(1, LedgerCalculator.UnreviewedEntryCount([source, destination], [transfer, excluded, confirmed]));
        Assert.Equal(2, LedgerCalculator.UnreviewedEntryCount([source, destination], [transfer, excluded, confirmed], [source.Id, source.Id]));
        Assert.Equal(0, LedgerCalculator.UnreviewedEntryCount([source, destination], [transfer, excluded, confirmed], []));
    }

    /// <summary>Counting does not add money, so individually valid review items remain countable beyond long totals.</summary>
    [Fact]
    public void Count_does_not_require_summing_monetary_effects()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var first = new LedgerEntry { AccountId = account.Id, Amount = long.MaxValue, Kind = EntryKind.Income, Review = ReviewState.Unreviewed };
        var second = new LedgerEntry { AccountId = account.Id, Amount = 2, Kind = EntryKind.Income, Review = ReviewState.Unreviewed };
        Assert.Equal(2, LedgerCalculator.UnreviewedEntryCount([account], [first, second]));
    }

    /// <summary>Repeated accounts do not multiply a transaction; entry occurrences are not silently deduplicated by id.</summary>
    [Fact]
    public void Duplicate_account_records_do_not_multiply_original_entry_occurrences()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var entry = new LedgerEntry { AccountId = account.Id, ToAccountId = account.Id,
            Kind = EntryKind.Transfer, Review = ReviewState.Unreviewed };
        Assert.Equal(2, LedgerCalculator.UnreviewedEntryCount([account, account], [entry, entry]));
    }

    /// <summary>Unmatched entries do not count and a historical destination reference preserves the prior scope predicate.</summary>
    [Fact]
    public void Historic_destination_metadata_preserves_scope_without_currency_effects()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var missing = new LedgerEntry { AccountId = Guid.NewGuid(), Review = ReviewState.Unreviewed };
        var legacy = new LedgerEntry { AccountId = Guid.NewGuid(), ToAccountId = account.Id,
            Kind = EntryKind.Expense, Review = ReviewState.Unreviewed };
        Assert.Equal(1, LedgerCalculator.UnreviewedEntryCount([account], [missing, legacy]));
    }

    /// <summary>Counting consumes each caller sequence once and does not depend on transaction date.</summary>
    [Fact]
    public void Sources_are_enumerated_once_and_review_is_not_date_filtered()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 20) };
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2020, 1, 1), Review = ReviewState.Unreviewed };
        var accountsRead = 0; var entriesRead = 0;
        IEnumerable<Account> Accounts() { Assert.Equal(1, ++accountsRead); yield return account; }
        IEnumerable<LedgerEntry> Entries() { Assert.Equal(1, ++entriesRead); yield return entry; }
        Assert.Equal(1, LedgerCalculator.UnreviewedEntryCount(Accounts(), Entries()));
        Assert.Equal(1, accountsRead); Assert.Equal(1, entriesRead);
    }
}
