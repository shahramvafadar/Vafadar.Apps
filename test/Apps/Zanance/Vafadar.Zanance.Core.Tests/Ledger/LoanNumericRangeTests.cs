using System.Numerics;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Exact representable loan estimate boundaries without ledger writes.</summary>
public sealed class LoanNumericRangeTests
{
    /// <summary>A representable ceiling must not overflow its temporary numerator.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(12)]
    [InlineData(600)]
    [InlineData(int.MaxValue)]
    public void Zero_interest_installment_retains_the_full_positive_money_range(int months)
    {
        var expected = (long)(((BigInteger)long.MaxValue + months - 1) / months);
        Assert.Equal(expected, LoanCalculator.Installment(long.MaxValue, 0, months));
    }

    /// <summary>The existing nonpositive-rate branch keeps the same no-interest rule.</summary>
    [Fact]
    public void Negative_rate_retains_the_same_representable_ceiling()
    {
        var expected = (long)(((BigInteger)long.MaxValue + 11) / 12);
        Assert.Equal(expected, LoanCalculator.Installment(long.MaxValue, -1, 12));
    }

    /// <summary>Every row can fit while the final public interest total does not.</summary>
    [Theory]
    [InlineData(long.MaxValue / 2, 1200, long.MaxValue / 2 + 1)]
    [InlineData(long.MaxValue / 4, 2400, long.MaxValue / 2 + 1)]
    [InlineData(long.MaxValue / 3, 3600, long.MaxValue)]
    public void Unrepresentable_schedule_interest_refuses_instead_of_wrapping(long principal, int rate, long payment)
    {
        Assert.Throws<OverflowException>(() => LoanCalculator.Schedule(principal, rate, payment, new(2026, 1, 31)));
    }

    /// <summary>Floating estimate conversion must not publish a negative payment outside the money range.</summary>
    [Theory]
    [InlineData(1200, 1)]
    [InlineData(1200, 2)]
    [InlineData(2400, 12)]
    public void Unrepresentable_annuity_payment_refuses_instead_of_converting_to_negative(int rate, int months)
    {
        Assert.Throws<OverflowException>(() => LoanCalculator.Installment(long.MaxValue, rate, months));
    }

    /// <summary>The greatest supported zero-interest payment remains valid.</summary>
    [Fact]
    public void One_zero_interest_installment_can_equal_the_money_limit() =>
        Assert.Equal(long.MaxValue, LoanCalculator.Installment(long.MaxValue, 0, 1));

    /// <summary>A divisible large amount retains its exact installment without an extra unit.</summary>
    [Fact]
    public void Divisible_large_zero_interest_amount_does_not_gain_a_minor_unit() =>
        Assert.Equal((long.MaxValue - 1) / 2, LoanCalculator.Installment(long.MaxValue - 1, 0, 2));

    /// <summary>A representable large interest/payment row must not be rejected by total validation.</summary>
    [Fact]
    public void Representable_large_interest_retains_the_original_smaller_final_payment()
    {
        var principal = long.MaxValue / 10;
        var schedule = LoanCalculator.Schedule(principal, 1200, long.MaxValue, new(2026, 1, 31));
        Assert.True(schedule.PaysOff);
        var row = Assert.Single(schedule.Installments);
        Assert.Equal(principal, row.Interest);
        Assert.Equal(principal, row.Principal);
        Assert.Equal(checked(principal * 2), row.Payment);
        Assert.Equal(0, row.Remaining);
        Assert.Equal(principal, schedule.TotalInterest);
    }
}
