using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Completed signed category changes retain their full magnitude when ranked.</summary>
public sealed class SpendingChangeOrderBoundaryTests
{
    private static readonly DateOnly Current = new(2026, 11, 6);
    private static readonly DateOnly Previous = new(2026, 10, 6);
    private static readonly ComparisonRange Range = new(Current, Current, Previous, Previous, false);
    private static readonly Guid First = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Second = new("20000000-0000-0000-0000-000000000002");

    /// <summary>The magnitude of Int64.MinValue exceeds MaxValue without invalidating its signed change.</summary>
    [Theory]
    [InlineData(1, long.MaxValue, false)]
    [InlineData(1, long.MaxValue, true)]
    [InlineData(long.MaxValue, 1, false)]
    [InlineData(long.MaxValue, 1, true)]
    [InlineData(2, long.MaxValue - 1, false)]
    [InlineData(2, long.MaxValue - 1, true)]
    public void Minimum_signed_change_ranks_above_positive_maximum(long refund, long previousExpense, bool reverse)
    {
        var account = Account();
        LedgerEntry[] entries = [Entry(account, EntryKind.Refund, refund, Current, First),
            Entry(account, EntryKind.Expense, previousExpense, Previous, First),
            Entry(account, EntryKind.Expense, long.MaxValue, Current, Second)];
        if (reverse) Array.Reverse(entries);
        var before = JsonSerializer.Serialize(entries, SpendingOrderJson.Default.LedgerEntryArray);
        var result = KpiCatalog.SpendingChanges([account], entries, Range, "EUR", id => id);
        Assert.Equal(2, result.Count);
        Assert.Equal(First, result[0].CategoryId);
        Assert.Equal(long.MinValue, result[0].Delta);
        Assert.Equal(-refund, result[0].Current);
        Assert.Equal(previousExpense, result[0].Comparison);
        Assert.Equal(long.MaxValue, result[1].Delta);
        Assert.Equal(before, JsonSerializer.Serialize(entries, SpendingOrderJson.Default.LedgerEntryArray));
    }

    /// <summary>Widening the sort key cannot accept an unrepresentable public signed difference.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrepresentable_change_still_refuses_before_ranking(bool negative)
    {
        var account = Account();
        LedgerEntry[] entries = negative
            ? [Entry(account, EntryKind.Refund, 2, Current, First), Entry(account, EntryKind.Expense, long.MaxValue, Previous, First)]
            : [Entry(account, EntryKind.Expense, long.MaxValue, Current, First), Entry(account, EntryKind.Refund, 1, Previous, First)];
        var before = JsonSerializer.Serialize(entries, SpendingOrderJson.Default.LedgerEntryArray);
        Assert.Throws<OverflowException>(() => KpiCatalog.SpendingChanges([account], entries, Range, "EUR", id => id));
        Assert.Equal(before, JsonSerializer.Serialize(entries, SpendingOrderJson.Default.LedgerEntryArray));
    }

    /// <summary>Equal absolute changes keep the original nullable-category tie break.</summary>
    [Fact]
    public void Equal_magnitudes_keep_nullable_category_order()
    {
        var account = Account();
        LedgerEntry[] entries = [Entry(account, EntryKind.Refund, long.MaxValue, Current, Second),
            Entry(account, EntryKind.Expense, long.MaxValue, Current, First),
            Entry(account, EntryKind.Expense, long.MaxValue, Current, null)];
        var result = KpiCatalog.SpendingChanges([account], entries, Range, "EUR", id => id);
        Assert.Equal(3, result.Count);
        Assert.Null(result[0].CategoryId);
        Assert.Equal(First, result[1].CategoryId);
        Assert.Equal(Second, result[2].CategoryId);
    }

    /// <summary>Unreviewed and other-currency entries cannot enter a confirmed selected-currency ranking.</summary>
    [Fact]
    public void Ranking_preserves_original_currency_account_and_review_filters()
    {
        var account = Account();
        var foreign = Account(); foreign.CurrencyCode = "GBP";
        var excluded = Account();
        var unreviewed = Entry(account, EntryKind.Refund, 1, Current, First); unreviewed.Review = ReviewState.Unreviewed;
        LedgerEntry[] entries = [Entry(account, EntryKind.Refund, 1, Current, First),
            Entry(account, EntryKind.Expense, long.MaxValue, Previous, First), unreviewed,
            Entry(foreign, EntryKind.Expense, long.MaxValue, Current, Second),
            Entry(excluded, EntryKind.Expense, long.MaxValue, Current, Second)];
        var result = KpiCatalog.SpendingChanges([account, foreign, excluded], entries, Range, "eur", id => id, [account.Id, foreign.Id], true);
        Assert.Equal(long.MinValue, Assert.Single(result).Delta);
    }

    /// <summary>Original zero exclusion still distinguishes an absent category from a nonzero equal-period category.</summary>
    [Fact]
    public void Zero_delta_with_nonzero_periods_remains_and_zero_net_only_is_omitted()
    {
        var account = Account();
        LedgerEntry[] entries = [Entry(account, EntryKind.Expense, 7, Current, First),
            Entry(account, EntryKind.Expense, 7, Previous, First),
            Entry(account, EntryKind.Expense, 3, Current, Second),
            Entry(account, EntryKind.Refund, 3, Current, Second)];
        var result = Assert.Single(KpiCatalog.SpendingChanges([account], entries, Range, "EUR", id => id));
        Assert.Equal(First, result.CategoryId);
        Assert.Equal(0, result.Delta);
    }

    /// <summary>The original category mapper applies before signed endpoint ranking.</summary>
    [Fact]
    public void Grouped_minimum_change_retains_original_mapper_and_null_category()
    {
        var account = Account();
        LedgerEntry[] entries = [Entry(account, EntryKind.Refund, 1, Current, First),
            Entry(account, EntryKind.Expense, long.MaxValue, Previous, Second)];
        var result = Assert.Single(KpiCatalog.SpendingChanges([account], entries, Range, "EUR", _ => null));
        Assert.Null(result.CategoryId);
        Assert.Equal(long.MinValue, result.Delta);
    }

    /// <summary>Provides calculation-only original account input without a persistent setup or financial action.</summary>
    private static Account Account() => new() { Name = "Boundary", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningDate = Previous };

    /// <summary>Provides one original nonnegative gross bucket in a selected period and category.</summary>
    private static LedgerEntry Entry(Account account, EntryKind kind, long amount, DateOnly date, Guid? category)
        => new() { AccountId = account.Id, Kind = kind, Amount = amount, Date = date, CategoryId = category };
}

/// <summary>Includes complete original entry payloads in the same source comparison on trimmed Android.</summary>
[JsonSerializable(typeof(LedgerEntry[]))]
internal partial class SpendingOrderJson : JsonSerializerContext;
