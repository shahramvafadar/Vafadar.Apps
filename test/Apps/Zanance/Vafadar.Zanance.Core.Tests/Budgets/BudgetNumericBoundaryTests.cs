using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Budget warnings and rounded suggestions keep exact integer money boundaries.</summary>
public sealed class BudgetNumericBoundaryTests
{
    [Theory]
    [InlineData(1_000_000_000_000_000_000, 800_000_000_000_000_000, BudgetAlert.Near)]
    [InlineData(long.MaxValue, long.MaxValue, BudgetAlert.Near)]
    [InlineData(long.MaxValue, 0, BudgetAlert.None)]
    [InlineData(long.MaxValue, -1, BudgetAlert.None)]
    [InlineData(long.MaxValue, 7_378_697_629_483_820_646, BudgetAlert.Near)]
    [InlineData(long.MaxValue, 7_378_697_629_483_820_645, BudgetAlert.None)]
    [InlineData(100, 80, BudgetAlert.Near)]
    [InlineData(100, 79, BudgetAlert.None)]
    [InlineData(100, 101, BudgetAlert.Exceeded)]
    [InlineData(0, 0, BudgetAlert.None)]
    public void Alert_comparison_has_no_integer_product_wrap(long limit, long spent, BudgetAlert expected) =>
        Assert.Equal(expected, new BudgetStatus(limit, spent).Alert);

    [Theory]
    [InlineData(long.MaxValue, -1)]
    [InlineData(long.MinValue, 1)]
    public void Unrepresentable_remaining_refuses(long limit, long spent) =>
        Assert.Throws<OverflowException>(() => new BudgetStatus(limit, spent).Remaining);

    [Theory]
    [InlineData(9_200_000_000_000_000_000, 1, 9_200_000_000_000_000_000)]
    [InlineData(1, long.MaxValue, long.MaxValue)]
    [InlineData(99_000_000_000_000_001, 1, 100_000_000_000_000_000)]
    [InlineData(99_000_000_000_000_000, 1, 99_000_000_000_000_000)]
    [InlineData(43720, 100, 44000)]
    [InlineData(0, 100, 0)]
    public void Representable_rounding_uses_exact_integer_magnitude(long amount, long factor, long expected) =>
        Assert.Equal(expected, BudgetSuggestions.RoundUp(amount, factor));

    [Theory]
    [InlineData(long.MaxValue, 1)]
    [InlineData(long.MaxValue, long.MaxValue - 1)]
    public void Unrepresentable_rounded_suggestion_refuses(long amount, long factor) =>
        Assert.Throws<OverflowException>(() => BudgetSuggestions.RoundUp(amount, factor));
}
