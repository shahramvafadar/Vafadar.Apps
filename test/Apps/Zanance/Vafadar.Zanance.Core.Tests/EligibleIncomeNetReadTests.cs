using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Completed eligible-income nets preserve supported accounts, original filters and true range refusals.</summary>
public sealed class EligibleIncomeNetReadTests
{
    private static readonly DateOnly Day = new(2026, 11, 1);

    /// <summary>A linked reversal completes a supported period net without imposing a gross-income storage-order limit.</summary>
    [Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5)]
    public void Supported_period_net_preserves_source_and_order(int permutation)
    {
        var (accounts, original) = Source();
        foreach (var day in Enumerable.Range(0, 3).Select(i => Day.AddDays(i)))
            foreach (var account in accounts) Assert.True(LedgerCalculator.Balance(account, original, day) is >= 0 and <= long.MaxValue);
        Assert.Equal(original[0].Id, original[2].RefundOfId); Assert.Equal(original[0].Amount, original[2].Amount);
        int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
        var input = orders[permutation].Select(i => original[i]).ToArray(); var before = State(accounts, input);
        var tags = input.Select(e => e.Tags).ToArray();
        var result = EligibleIncome.Of(accounts, input, "EUR", Day, Day.AddDays(2));
        Assert.Equal(long.MaxValue, result.Total); Assert.Equal("EUR", result.CurrencyCode); Assert.Equal(3, result.Entries.Count);
        for (var i = 0; i < original.Length; i++) Assert.Same(original[i], result.Entries[i]);
        Assert.Equal(before, State(accounts, input));
        for (var i = 0; i < input.Length; i++) { Assert.Same(original[orders[permutation][i]], input[i]); Assert.Same(tags[i], input[i].Tags); }
    }

    /// <summary>Positive and negative true final overflow still refuse; negative income cannot be clamped first.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Unsupported_final_period_net_still_refuses(bool negative)
    {
        var (accounts, _) = Source();
        var first = new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[0].Id, Amount = long.MaxValue, Date = Day.AddDays(-1) };
        var second = new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[1].Id, Amount = 2, Date = Day.AddDays(-1) };
        LedgerEntry[] input = negative ? [first, second,
            new() { Kind = EntryKind.IncomeReversal, AccountId = first.AccountId, Amount = first.Amount, RefundOfId = first.Id, Date = Day },
            new() { Kind = EntryKind.IncomeReversal, AccountId = second.AccountId, Amount = second.Amount, RefundOfId = second.Id, Date = Day }]
            : [first, second];
        if (!negative) { first.Date = Day; second.Date = Day; }
        var before = State(accounts, input);
        Assert.Throws<OverflowException>(() => EligibleIncome.Of(accounts, input, "EUR", Day, Day.AddDays(2)));
        Assert.Equal(before, State(accounts, input));
    }

    /// <summary>Default totals membership and an explicit selected account list keep their original distinct scopes.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Default_and_explicit_account_scope_remain(bool explicitScope)
    {
        var (accounts, input) = Source(); accounts[1].IncludeInTotals = false; var before = State(accounts, input);
        var result = EligibleIncome.Of(accounts, input, "EUR", Day, Day.AddDays(2), explicitScope ? [accounts[0].Id, accounts[1].Id] : null);
        Assert.Equal(explicitScope ? long.MaxValue : 0, result.Total); Assert.Equal(explicitScope ? 3 : 2, result.Entries.Count);
        Assert.Equal(before, State(accounts, input));
    }

    /// <summary>Excluded categories, currency, opening and date cutoffs keep offsets scoped to the original selected period.</summary>
    [Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4)]
    public void Original_period_and_identity_filters_remain(int variant)
    {
        var (accounts, input) = Source(); var excluded = Guid.NewGuid();
        if (variant == 0) input[1].CategoryId = excluded;
        if (variant == 1) accounts[1].CurrencyCode = "USD";
        if (variant == 2) accounts[1].OpeningDate = Day.AddDays(3);
        var before = State(accounts, input);
        if (variant == 4)
            Assert.Throws<OverflowException>(() => EligibleIncome.Of(accounts, input, "EUR", Day, Day.AddDays(1)));
        else
        {
            var result = EligibleIncome.Of(accounts, input, "EUR", variant == 3 ? Day.AddDays(1) : Day, Day.AddDays(2),
                excludedCategoryIds: variant == 0 ? [excluded] : null);
            Assert.Equal(0L, result.Total); Assert.Equal(2, result.Entries.Count);
        }
        Assert.Equal(before, State(accounts, input));
    }

    private static (Account[], LedgerEntry[]) Source()
    {
        var accounts = new[] { new Account { Name = "Fictitious source A", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = Day.AddDays(-2) },
            new Account { Name = "Fictitious source B", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = Day.AddDays(-2) } };
        var first = new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[0].Id, Date = Day, Amount = long.MaxValue, Tags = ["Retained income"], Note = "Fictitious complete period" };
        var second = new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[1].Id, Date = Day.AddDays(1), Amount = long.MaxValue, Tags = ["Retained independent income"], Review = ReviewState.Unreviewed };
        var reversal = new LedgerEntry { Kind = EntryKind.IncomeReversal, AccountId = accounts[0].Id, Date = Day.AddDays(2), Amount = long.MaxValue, RefundOfId = first.Id, Tags = ["Retained linked reversal"] };
        return (accounts, [first, second, reversal]);
    }

    private static string State(Account[] accounts, LedgerEntry[] entries) =>
        JsonSerializer.Serialize(accounts, EligibleIncomeReadJson.Default.AccountArray) + JsonSerializer.Serialize(entries, EligibleIncomeReadJson.Default.LedgerEntryArray);
}

/// <summary>Captures every public account and all32 ledger fields on both host and native checks.</summary>
[JsonSerializable(typeof(Account[]))]
[JsonSerializable(typeof(LedgerEntry[]))]
internal partial class EligibleIncomeReadJson : JsonSerializerContext { }
