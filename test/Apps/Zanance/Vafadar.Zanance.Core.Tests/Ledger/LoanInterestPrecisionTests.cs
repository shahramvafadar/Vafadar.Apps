using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Preserves exact monthly half-away rounding without creating financial records.</summary>
public sealed class LoanInterestPrecisionTests
{
    /// <summary>Each declared amount is strictly below a half unit, not an exact midpoint.</summary>
    public static TheoryData<long, decimal> BelowHalfCases => new()
    {
        { 600, 0.9999999999999999999999999999m },
        { 300, 1.9999999999999999999999999999m },
        { 200, 2.9999999999999999999999999999m },
        { 150, 3.9999999999999999999999999999m },
        { 120, 4.9999999999999999999999999999m },
        { 100, 5.9999999999999999999999999999m },
        { 80, 7.4999999999999999999999999999m },
        { 400, 1.4999999999999999999999999999m },
    };

    /// <summary>For every row, principal * exact annual rate is less than600, so monthly interest is less than one half.</summary>
    [Theory]
    [MemberData(nameof(BelowHalfCases))]
    public void A_value_strictly_below_the_half_unit_does_not_round_up(long outstanding, decimal rate) =>
        Assert.Equal(0, LoanCalculator.Interest(outstanding, rate));

    /// <summary>The existing half-away rule still rounds exact positive midpoints upward.</summary>
    [Theory]
    [InlineData(600, 1, 1)]
    [InlineData(500, 6, 3)]
    public void Exact_midpoints_keep_half_away_rounding(long outstanding, int rate, long expected) =>
        Assert.Equal(expected, LoanCalculator.Interest(outstanding, rate));

    /// <summary>A positive tail above a midpoint remains on the upper side.</summary>
    [Fact]
    public void A_value_strictly_above_the_half_unit_keeps_the_rounded_unit() =>
        Assert.Equal(1, LoanCalculator.Interest(600, 1.0000000000000000000000000001m));

    /// <summary>Payment splitting must not manufacture a unit of interest before paying principal.</summary>
    [Fact]
    public void The_split_uses_the_original_exact_monthly_rounding() =>
        Assert.Equal((0L, 600L), LoanCalculator.Split(600, 0.9999999999999999999999999999m, 601));

    /// <summary>A single fully covered principal must not acquire a second installment from an intermediate midpoint.</summary>
    [Fact]
    public void The_schedule_does_not_add_a_spurious_interest_unit_or_month()
    {
        var schedule = LoanCalculator.Schedule(600, 0.9999999999999999999999999999m, 600, new(2026, 1, 31));
        Assert.True(schedule.PaysOff);
        var row = Assert.Single(schedule.Installments);
        Assert.Equal((600L, 0L, 600L, 0L), (row.Payment, row.Interest, row.Principal, row.Remaining));
        Assert.Equal(0, schedule.TotalInterest);
        Assert.Equal(new DateOnly(2026, 1, 31), schedule.LastDate);
    }
}

/// <summary>The exact rounding still narrows only the final public money amount.</summary>
public sealed class LoanInterestMoneyRangeTests
{
    /// <summary>Representable endpoint/tiny interest remain valid and a genuinely excessive rounded amount refuses.</summary>
    [Fact]
    public void The_final_rounded_interest_keeps_the_original_money_range()
    {
        Assert.Equal(long.MaxValue, LoanCalculator.Interest(long.MaxValue, 1200m));
        Assert.Equal(0, LoanCalculator.Interest(long.MaxValue, 0.0000000000000000000000000001m));
        Assert.Throws<OverflowException>(() => LoanCalculator.Interest(long.MaxValue, 1200.0000000000000001m));
    }
}
