using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Unreviewed scope routing preserves original currency counts, order and legacy inputs.</summary>
[Trait("AT", "AT-373")]
public sealed class UnreviewedScopeIndexTests
{
    /// <summary>A transfer has one count per touched currency even with both accounts in the scope.</summary>
    [Theory, InlineData("EUR", 1), InlineData("USD", 2)]
    public void Transfer_counts_each_currency_once_and_preserves_destination_amount(string destinationCurrency, int currencies)
    {
        var source = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var destination = new Account { Name = "Fictitious", CurrencyCode = destinationCurrency };
        var entry = Transfer(source, destination, 100, 120);
        var rows = LedgerCalculator.Unreviewed([destination, source], [entry]);
        Assert.Equal(currencies, rows.Count);
        if (currencies == 1) Assert.Equal(new UnreviewedSummary("EUR", 1, 20), rows[0]);
        else Assert.Equal(new[] { new UnreviewedSummary("EUR", 1, -100), new UnreviewedSummary("USD", 1, 120) }, rows);
    }

    /// <summary>Duplicate source records retain global scope order and the checked currency sum's overflow boundary.</summary>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Repeated_accounts_keep_original_checked_sum_order(int destinationPosition)
    {
        var source = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var destination = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var accounts = new List<Account> { source, source };
        accounts.Insert(destinationPosition, destination);
        var entry = Transfer(source, destination, long.MaxValue, long.MaxValue);
        if (destinationPosition == 2)
            Assert.Throws<OverflowException>(() => LedgerCalculator.Unreviewed(accounts, [entry]));
        else Assert.Equal(new UnreviewedSummary("EUR", 1, -long.MaxValue), Assert.Single(LedgerCalculator.Unreviewed(accounts, [entry])));
    }

    /// <summary>Explicit selection includes an archived excluded account once; review counts do not filter by opening date.</summary>
    [Fact]
    public void Explicit_scope_keeps_archived_excluded_account_and_preopening_review()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR", IsArchived = true, IncludeInTotals = false, OpeningDate = new(2026, 10, 20) };
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 1), Amount = 17, Kind = EntryKind.Expense, Review = ReviewState.Unreviewed };
        Assert.Empty(LedgerCalculator.Unreviewed([account], [entry]));
        Assert.Equal(new UnreviewedSummary("EUR", 1, -17), Assert.Single(LedgerCalculator.Unreviewed([account], [entry], [account.Id, account.Id])));
    }

    /// <summary>Legacy same-account transfers are routed once, including duplicate account records and optional destination amount.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Self_transfer_does_not_duplicate_source_and_destination_groups(bool converted)
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var entry = Transfer(account, account, 50, converted ? 75 : null);
        Assert.Equal(new UnreviewedSummary("EUR", 1, converted ? 50 : 0), Assert.Single(LedgerCalculator.Unreviewed([account, account], [entry])));
    }

    /// <summary>Historic destination metadata on a nontransfer still produces its original zero-effect review group.</summary>
    [Fact]
    public void Nontransfer_destination_metadata_keeps_its_zero_effect_currency_count()
    {
        var source = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var destination = new Account { Name = "Fictitious", CurrencyCode = "USD" };
        var entry = Transfer(source, destination, 100, 120);
        entry.Kind = EntryKind.Expense;
        Assert.Equal(new[] { new UnreviewedSummary("EUR", 1, -100), new UnreviewedSummary("USD", 1, 0) },
            LedgerCalculator.Unreviewed([source, destination], [entry]));
    }

    /// <summary>Unmatched and confirmed entries do not create currency summaries or alter caller-owned records.</summary>
    [Fact]
    public void Irrelevant_rows_are_ignored_and_input_records_remain_unchanged()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var missing = new LedgerEntry { AccountId = Guid.NewGuid(), Kind = EntryKind.Income, Amount = 100, Review = ReviewState.Unreviewed, Tags = ["original"] };
        var confirmed = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 20, Review = ReviewState.Confirmed };
        var before = JsonSerializer.Serialize(new { account, missing, confirmed });
        Assert.Empty(LedgerCalculator.Unreviewed([account], [missing, confirmed]));
        Assert.Equal(before, JsonSerializer.Serialize(new { account, missing, confirmed }));
    }

    /// <summary>Currency keys retain ordinal case-sensitive grouping and ordering.</summary>
    [Fact]
    public void Currency_groups_remain_ordinal_and_case_sensitive()
    {
        var lower = new Account { Name = "Fictitious", CurrencyCode = "eur" };
        var upper = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var dollars = new Account { Name = "Fictitious", CurrencyCode = "USD" };
        LedgerEntry Income(Account account, long amount) => new() { AccountId = account.Id, Kind = EntryKind.Income, Amount = amount, Review = ReviewState.Unreviewed };
        Assert.Equal(new[] { new UnreviewedSummary("EUR", 1, 20), new UnreviewedSummary("USD", 1, 30), new UnreviewedSummary("eur", 1, 10) },
            LedgerCalculator.Unreviewed([lower, dollars, upper], [Income(lower, 10), Income(upper, 20), Income(dollars, 30)]));
    }

    /// <summary>Routing materializes scope once and reads the original entry sequence once.</summary>
    [Fact]
    public void Account_and_entry_sequences_are_each_enumerated_once()
    {
        var account = new Account { Name = "Fictitious", CurrencyCode = "EUR" };
        var entry = new LedgerEntry { AccountId = account.Id, Amount = 10, Kind = EntryKind.Income, Review = ReviewState.Unreviewed };
        var accountReads = 0; var entryReads = 0;
        IEnumerable<Account> Accounts() { Assert.Equal(1, ++accountReads); yield return account; }
        IEnumerable<LedgerEntry> Entries() { Assert.Equal(1, ++entryReads); yield return entry; }
        Assert.Equal(new UnreviewedSummary("EUR", 1, 10), Assert.Single(LedgerCalculator.Unreviewed(Accounts(), Entries())));
        Assert.Equal(1, accountReads); Assert.Equal(1, entryReads);
    }

    /// <summary>Accumulating individually valid effects must refuse a total outside signed minor-unit bounds.</summary>
    [Theory, InlineData(EntryKind.Income), InlineData(EntryKind.Expense)]
    public void Cross_entry_currency_total_refuses_overflow(EntryKind kind)
    {
        var account = new Account { Name = "Boundary", CurrencyCode = "EUR" };
        var first = new LedgerEntry { AccountId = account.Id, Amount = long.MaxValue, Kind = kind, Review = ReviewState.Unreviewed };
        var second = new LedgerEntry { AccountId = account.Id, Amount = 2, Kind = kind, Review = ReviewState.Unreviewed };
        Assert.Throws<OverflowException>(() => LedgerCalculator.Unreviewed([account], [first, second]));
    }

    /// <summary>Builds only a fictitious original transfer input, without calculating its result.</summary>
    private static LedgerEntry Transfer(Account source, Account destination, long amount, long? destinationAmount) => new()
    {
        AccountId = source.Id, ToAccountId = destination.Id, Kind = EntryKind.Transfer,
        Amount = amount, ToAmount = destinationAmount, Review = ReviewState.Unreviewed,
    };
}
