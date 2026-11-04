using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Representable weekly ranges, explicit fortnight anchors and period steps (D-812).</summary>
public sealed class FixedPeriodEndpointTests
{
    [Theory]
    [InlineData(BudgetPeriod.Week, 0)]
    [InlineData(BudgetPeriod.Week, 2)]
    [InlineData(BudgetPeriod.TwoWeeks, 0)]
    [InlineData(BudgetPeriod.TwoWeeks, 2)]
    public void A_valid_final_start_has_only_representable_remaining_days(BudgetPeriod period, int daysBeforeEnd)
    {
        var start = DateOnly.MaxValue.AddDays(-daysBeforeEnd);

        Assert.Equal((start, DateOnly.MaxValue), BudgetPeriods.Range(period, start));
    }

    [Theory]
    [InlineData(BudgetPeriod.Week)]
    [InlineData(BudgetPeriod.TwoWeeks)]
    public void A_complete_final_period_keeps_its_original_length(BudgetPeriod period)
    {
        var start = DateOnly.MaxValue.AddDays(1 - BudgetPeriods.Days(period));

        Assert.Equal((start, DateOnly.MaxValue), BudgetPeriods.Range(period, start));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Wednesday)]
    public void An_explicit_fortnight_anchor_does_not_require_an_earlier_week_start(DayOfWeek firstDay)
    {
        Assert.Equal(DateOnly.MinValue, BudgetPeriods.StartOf(BudgetPeriod.TwoWeeks, DateOnly.MinValue, firstDay, DateOnly.MinValue));
    }

    [Theory]
    [InlineData(BudgetPeriod.Week, int.MaxValue)]
    [InlineData(BudgetPeriod.Week, 613566757)]
    [InlineData(BudgetPeriod.TwoWeeks, int.MaxValue)]
    [InlineData(BudgetPeriod.TwoWeeks, int.MinValue)]
    [InlineData(BudgetPeriod.TwoWeeks, 306783379)]
    public void A_step_outside_the_date_domain_cannot_wrap_to_an_unrelated_valid_date(BudgetPeriod period, int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetPeriods.Step(period, new DateOnly(2026, 11, 4), count));
    }

    [Theory]
    [InlineData(BudgetPeriod.Week, 1)]
    [InlineData(BudgetPeriod.Week, -1)]
    [InlineData(BudgetPeriod.TwoWeeks, 1)]
    [InlineData(BudgetPeriod.TwoWeeks, -1)]
    public void An_exact_step_to_either_endpoint_remains_valid(BudgetPeriod period, int count)
    {
        var expected = count > 0 ? DateOnly.MaxValue : DateOnly.MinValue;
        var start = expected.AddDays(-count * BudgetPeriods.Days(period));

        Assert.Equal(expected, BudgetPeriods.Step(period, start, count));
    }
}
