using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Tests.Goals;

/// <summary>Valid extreme dates and minor amounts retain nonnegative original goal projections.</summary>
public sealed class GoalCalculatorBoundaryTests
{
    [Theory]
    [InlineData("9999-12-01", "9999-12-31", 1)]
    [InlineData("9999-12-31", "9999-12-31", 1)]
    [InlineData("9999-11-30", "9999-12-31", 2)]
    [InlineData("0001-01-31", "9999-12-31", 119988)]
    [InlineData("2026-11-04", "9999-12-31", 95678)]
    [InlineData("2024-01-31", "2024-02-28", 1)]
    [InlineData("2024-01-31", "2024-02-29", 2)]
    [InlineData("2024-01-31", "2024-03-30", 2)]
    public void Monthly_dates_keep_the_anchor_day_and_finish_within_the_calendar(string today, string target, int expected) =>
        Assert.Equal(expected, GoalCalculator.Opportunities(ContributionFrequency.Monthly,
            DateOnly.Parse(today, CultureInfo.InvariantCulture), DateOnly.Parse(target, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(long.MaxValue, 2)]
    [InlineData(long.MaxValue, 3)]
    [InlineData(long.MaxValue, 31)]
    [InlineData(long.MaxValue, int.MaxValue)]
    [InlineData(long.MaxValue - 1, 3)]
    [InlineData(long.MaxValue - 30, 32)]
    [InlineData(long.MaxValue, 1)]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    public void Contribution_ceiling_is_exact_without_overflow(long remaining, int opportunities)
    {
        var expected = remaining <= 0 ? 0 : (long)(((BigInteger)remaining + opportunities - 1) / opportunities);
        Assert.Equal(expected, GoalCalculator.SuggestedContribution(remaining, opportunities));
    }

    [Theory]
    [InlineData(long.MinValue, 1, 0)]
    [InlineData(long.MinValue, long.MaxValue, 0)]
    [InlineData(-2, long.MaxValue, 0)]
    [InlineData(long.MaxValue, long.MaxValue, 0)]
    [InlineData(long.MaxValue, 0, long.MaxValue)]
    public void An_earmark_never_creates_free_money_from_an_extreme_debt(long balance, long earmarked, long expected) =>
        Assert.Equal(expected, new AccountEarmark(Guid.NewGuid(), balance, earmarked).Unallocated);

    [Fact]
    public void Original_earmark_evaluation_retains_a_final_month_goal_and_its_source()
    {
        var goal = new Goal { Name = "Final month", CurrencyCode = "EUR", TargetAmount = 101,
            TargetDate = DateOnly.MaxValue, Frequency = ContributionFrequency.Monthly };
        var before = JsonSerializer.Serialize(goal);
        var status = Assert.Single(GoalCalculator.Evaluate([goal], [], new Dictionary<Guid, long>(), new DateOnly(9999, 12, 1)));
        Assert.Equal(1, status.Opportunities);
        Assert.Equal(101, status.SuggestedContribution);
        Assert.Equal(before, JsonSerializer.Serialize(goal));
    }

    [Fact]
    public void Original_preview_keeps_a_positive_exact_required_contribution()
    {
        var today = new DateOnly(2026, 11, 4);
        var goal = new Goal { Name = "Large target", CurrencyCode = "EUR", TargetAmount = long.MaxValue,
            TargetDate = today.AddMonths(1), Frequency = ContributionFrequency.Monthly };
        var before = JsonSerializer.Serialize(goal);
        var status = GoalProgressService.Scenario(goal, 0, 0, null, today);
        Assert.Equal(2, status.Opportunities);
        Assert.Equal(4611686018427387904, status.Required);
        Assert.Equal(before, JsonSerializer.Serialize(goal));
    }
}
