using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Wealth corrections cancel exactly while retaining completed end-of-day balance limits.</summary>
public sealed class WealthAdjustmentCancellationTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);

    /// <summary>Every balance and completed correction fits even when the adjustment-only prefix does not.</summary>
    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 20)]
    [InlineData(true, 2)]
    [InlineData(true, 20)]
    public void Representable_adjustments_cancel_before_publication(bool negative, long offset)
    {
        var sign = negative ? -1 : 1;
        var account = Account(-sign * offset);
        LedgerEntry[] entries = [Adjustment(account, sign * long.MaxValue, 1),
            Adjustment(account, sign * offset, 2), Adjustment(account, -sign * offset, 3)];
        var change = Assert.Single(WealthHistory.Explain([account], entries, [], [], [], Day, Day.AddDays(3)));
        Assert.Equal(sign * long.MaxValue, change.Corrections);
        Assert.Equal(sign * long.MaxValue, change.Change);
        Assert.Equal(0, change.Flows);
        Assert.Equal(0, change.Remainder);
        Assert.Equal(-sign * offset, account.OpeningBalance);
        Assert.Equal(sign * offset, entries[1].EffectOn(account.Id));
        Assert.Equal(-sign * offset, entries[2].EffectOn(account.Id));
    }

    /// <summary>A completed unsupported correction still refuses even when the ending balance fits.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrepresentable_completed_corrections_still_refuse(bool negative)
    {
        var sign = negative ? -1 : 1;
        var account = Account(-sign * 20);
        LedgerEntry[] entries = [Adjustment(account, sign * long.MaxValue, 1), Adjustment(account, sign * 20, 2)];
        Assert.Throws<OverflowException>(() => WealthHistory.Explain([account], entries, [], [], [], Day, Day.AddDays(3)));
    }

    /// <summary>The original exclusive start, inclusive end and account ownership are unchanged.</summary>
    [Fact]
    public void Corrections_keep_the_original_range_and_account_scope()
    {
        var account = Account(0);
        var other = Account(0);
        LedgerEntry[] entries = [Adjustment(account, 7, 0), Adjustment(account, 11, 1),
            Adjustment(account, -3, 3), Adjustment(account, 13, 4), Adjustment(other, 17, 1)];
        var change = Assert.Single(WealthHistory.Explain([account], entries, [], [], [], Day, Day.AddDays(3)));
        Assert.Equal(8, change.Corrections);
        Assert.Equal(7, change.Start);
        Assert.Equal(15, change.End);
        Assert.Equal(0, change.Remainder);
    }

    /// <summary>D-875 permits a completed net, while an unsupported selected end-of-day balance still refuses.</summary>
    [Fact]
    public void Completed_balance_cancellation_keeps_the_selected_day_limit()
    {
        var account = Account(0);
        LedgerEntry[] entries = [Adjustment(account, long.MaxValue, 1), Adjustment(account, 20, 2), Adjustment(account, -20, 3)];
        Assert.Throws<OverflowException>(() => WealthHistory.Explain([account], entries, [], [], [], Day, Day.AddDays(2)));
        var change = Assert.Single(WealthHistory.Explain([account], entries, [], [], [], Day, Day.AddDays(3)));
        Assert.Equal(long.MaxValue, change.End);
        Assert.Equal(long.MaxValue, change.Corrections);
        Assert.Equal(0, change.Remainder);
        Assert.Equal(0, account.OpeningBalance);
        Assert.Equal(new[] { long.MaxValue, 20, -20 }, entries.Select(entry => entry.EffectOn(account.Id)).ToArray());
    }

    private static Account Account(long opening) => new()
    {
        Name = "Wealth correction boundary", CurrencyCode = "EUR", Type = AccountType.Checking,
        OpeningDate = Day, OpeningBalance = opening,
    };

    private static LedgerEntry Adjustment(Account account, long effect, int day) => new()
    {
        AccountId = account.Id, Kind = EntryKind.Adjustment, Amount = Math.Abs(effect), Date = Day.AddDays(day),
        Direction = effect < 0 ? AdjustmentDirection.Decrease : AdjustmentDirection.Increase,
    };
}
