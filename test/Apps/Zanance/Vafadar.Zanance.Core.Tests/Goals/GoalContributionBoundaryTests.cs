using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Goals;

/// <summary>Original plan and observed-pace callers retain exact positive contribution counts.</summary>
public sealed class GoalContributionBoundaryTests
{
    [Theory]
    [InlineData(long.MaxValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MaxValue - 1)]
    [InlineData(long.MaxValue, long.MaxValue / 2)]
    [InlineData(long.MaxValue, 1)]
    public void Planned_period_count_uses_the_full_positive_range(long remaining, long contribution) =>
        Assert.Equal((long)(((BigInteger)remaining + contribution - 1) / contribution),
            ContributionSchedule.PeriodsNeeded(remaining, contribution));

    [Theory]
    [InlineData(long.MaxValue, long.MaxValue, 0)]
    [InlineData(long.MaxValue, long.MaxValue - 1, 1)]
    [InlineData(long.MaxValue, long.MaxValue / 2, 2)]
    public void Planned_eta_uses_the_exact_first_second_or_third_date(long remaining, long contribution, int months)
    {
        var today = new DateOnly(2026, 11, 4);
        var rule = new RecurrenceRule { Start = today, Frequency = Frequency.Monthly };
        var before = JsonSerializer.Serialize(rule);
        Assert.Equal(today.AddMonths(months), ContributionSchedule.Eta(rule, today, remaining, contribution));
        Assert.Equal(before, JsonSerializer.Serialize(rule));
    }

    [Fact]
    public void Observed_pace_keeps_the_existing_1200_period_cap_without_overflow()
    {
        var goal = new Goal { Name = "Unfunded pace", CurrencyCode = "EUR", TargetAmount = long.MaxValue };
        var allocations = Enumerable.Range(8, 3).Select(month => new GoalAllocation
        { GoalId = goal.Id, AccountId = Guid.Empty, Amount = 2, Date = new DateOnly(2026, month, 1) }).ToArray();
        var before = JsonSerializer.Serialize(allocations);
        var trend = GoalTrendService.Compute(goal, long.MaxValue, [], [], allocations, [], new DateOnly(2026, 11, 4), PeriodCalendar.Gregorian);
        Assert.Equal(TrendStatus.Ok, trend.Status);
        Assert.Equal(2, trend.Pace);
        Assert.Equal(new DateOnly(2126, 10, 31), trend.Eta);
        Assert.Equal(before, JsonSerializer.Serialize(allocations));
    }

    [Fact]
    public void Equal_large_periods_are_not_one_offs_when_the_threshold_exceeds_long_range()
    {
        var goal = new Goal { Name = "Equal pace", CurrencyCode = "EUR", TargetAmount = long.MaxValue };
        var contribution = long.MaxValue / 3 + 1;
        var allocations = Enumerable.Range(8, 3).Select(month => new GoalAllocation
        { GoalId = goal.Id, AccountId = Guid.Empty, Amount = contribution, Date = new DateOnly(2026, month, 1) })
            .Append(new GoalAllocation { GoalId = goal.Id, AccountId = Guid.Empty, Amount = -2, Date = new DateOnly(2026, 11, 1) }).ToArray();
        var before = JsonSerializer.Serialize(allocations);
        var trend = GoalTrendService.Compute(goal, 1, [], [], allocations, [], new DateOnly(2026, 11, 4), PeriodCalendar.Gregorian);
        Assert.Equal((BigInteger)long.MaxValue, allocations.Aggregate(BigInteger.Zero, (sum, allocation) => sum + allocation.Amount));
        Assert.Equal(contribution, trend.Pace);
        Assert.All(trend.Periods, period => Assert.False(period.IsOneOff));
        Assert.Equal(before, JsonSerializer.Serialize(allocations));
    }
}
