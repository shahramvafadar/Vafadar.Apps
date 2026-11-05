using System.Globalization;
using System.Numerics;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Checks the original nominal annuity ceiling with independent exact rational inequalities.</summary>
public sealed class LoanAnnuityPrecisionTests
{
    /// <summary>Known exact monthly fractions, independently calculated from each declared decimal annual rate.</summary>
    public static TheoryData<long, decimal, int, string, string> RepresentableCases => new()
    {
        { 100L, 120000m, 600, "100", "1" },
        { 1L, 1200000m, 600, "1000", "1" },
        { 1200L, 0.0000000000000000000000000001m, 12, "1", "12000000000000000000000000000000" },
        { 1200L, 0.0000000000000000000000000001m, 600, "1", "12000000000000000000000000000000" },
        { 1200L, 0.00000000000001m, 12, "1", "120000000000000000" },
        { 1L, 1200m, 600, "1", "1" },
        { 1L, 2400m, 600, "2", "1" },
        { 1L, 3600m, 600, "3", "1" },
        { 1L, 600m, 600, "1", "2" },
        { 4611686018427387903L, 1200m, 62, "1", "1" },
        { 4611686018427387903L, 1200m, 64, "1", "1" },
        { 4611686018427387903L, 1200m, 65, "1", "1" },
        { 3L, 1200m, 2, "1", "1" },
        { 2L, 600m, 2, "1", "2" },
        { 9223372036854775806L, 0.0000000000000000000000000001m, 1, "1", "12000000000000000000000000000000" },
        { 9223372036854775807L, 0.0000000000000000000000000001m, 2, "1", "12000000000000000000000000000000" },
        { 9223372036854775807L, 0.0000000000000000000000000001m, 600, "1", "12000000000000000000000000000000" },
        { 9223372036854775807L, 1m, 600, "1", "1200" },
        { 4611686018427387903L, 12m, 600, "1", "100" },
        { 1234567890123456789L, 17.5m, 137, "7", "480" },
        { 100L, 0.1234567890123456789012345678m, 73, "205761315020576131502057613", "2000000000000000000000000000000" },
        { 1844674407370955161L, 1.2300000000000000000000000000m, 599, "41", "40000" },
        { 100L, 123456789.01234567890123456789m, 600, "4115226300411522630041152263", "40000000000000000000000" },
        { 10000L, 6m, 1, "1", "200" }
    };

    /// <summary>The published minor-unit result is the unique ceiling of the exact nominal formula.</summary>
    [Theory]
    [MemberData(nameof(RepresentableCases))]
    public void Representable_annuity_retains_the_exact_nominal_ceiling(long principal, decimal annualRate, int months, string numerator, string denominator)
    {
        var p = BigInteger.Parse(numerator, CultureInfo.InvariantCulture);
        var q = BigInteger.Parse(denominator, CultureInfo.InvariantCulture);
        var raised = BigInteger.Pow(p + q, months);
        var dividend = (BigInteger)principal * p * raised;
        var divisor = q * (raised - BigInteger.Pow(q, months));
        var actual = LoanCalculator.Installment(principal, annualRate, months);
        Assert.True(actual > 0);
        Assert.True((BigInteger)actual * divisor >= dividend);
        Assert.True(((BigInteger)actual - 1) * divisor < dividend);
    }

    /// <summary>Independent exact powers cover128 additional UI-admissible rate/large-money combinations.</summary>
    [Fact]
    public void Independent_exact_grid_retains_every_minor_unit()
    {
        for (var index = 1; index <= 128; index++)
        {
            var principal = long.MaxValue / (index % 17 + 2) + index * 1337L;
            var hundredths = index * 7919 % 10000 + 1;
            var months = 65 + index * 97 % 536;
            var p = new BigInteger(hundredths);
            var q = new BigInteger(120000);
            var raised = BigInteger.Pow(p + q, months);
            var dividend = principal * p * raised;
            var divisor = q * (raised - BigInteger.Pow(q, months));
            var actual = LoanCalculator.Installment(principal, hundredths / 100m, months);
            Assert.True(actual > 0);
            Assert.True((BigInteger)actual * divisor >= dividend);
            Assert.True(((BigInteger)actual - 1) * divisor < dividend);
        }
    }

    /// <summary>Bernoulli bounds prove the result without creating an enormous exact power.</summary>
    [Theory]
    [InlineData(long.MaxValue, 4294967299L)]
    [InlineData(1200L, 1L)]
    public void Tiny_positive_rate_and_maximum_month_count_remain_representable(long principal, long expected)
    {
        var months = int.MaxValue;
        var q = BigInteger.Parse("12000000000000000000000000000000", CultureInfo.InvariantCulture);
        // principal/months < payment <= principal/months + principal/q.
        Assert.True(((BigInteger)expected - 1) * months <= principal);
        Assert.True((BigInteger)expected * months > principal);
        Assert.True((BigInteger)expected * months * q >= (BigInteger)principal * q + (BigInteger)principal * months);
        Assert.Equal(expected, LoanCalculator.Installment(principal, 0.0000000000000000000000000001m, months));
    }

    /// <summary>Positive interest keeps the finite-period payment strictly above its integral asymptote.</summary>
    [Fact]
    public void Maximum_month_count_keeps_the_positive_integral_interest_tail()
    {
        // 100 < 100*2^m/(2^m-1) <= 101 for every m >= 7.
        Assert.Equal(101, LoanCalculator.Installment(100, 1200, int.MaxValue));
    }

    /// <summary>Exact boundary handling must retain genuine unrepresentable positive payments.</summary>
    [Fact]
    public void Positive_interest_on_maximum_single_payment_remains_unrepresentable() =>
        Assert.Throws<OverflowException>(() => LoanCalculator.Installment(long.MaxValue, 0.0000000000000000000000000001m, 1));

    /// <summary>The full96-bit decimal coefficient cannot overflow to a smaller fake rate.</summary>
    [Fact]
    public void Unrepresentable_full_decimal_rate_remains_refused() =>
        Assert.Throws<OverflowException>(() => LoanCalculator.Installment(1, decimal.MaxValue, 600));
}
