using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Goal suggestions must not wrap money, lose ceiling needs or overstate a purchasable quantity.</summary>
public sealed class CapacityBoundaryTests
{
    /// <summary>A completed capacity outside stored money is refused instead of changing sign.</summary>
    [Theory]
    [InlineData(long.MaxValue, -1)]
    [InlineData(long.MinValue, 1)]
    public void An_unrepresentable_capacity_is_refused(long income, long spending) =>
        Assert.Throws<OverflowException>(() => new CapacityResult("EUR", income, spending, 0, 0, 0, true).Amount);

    /// <summary>Intermediate subtraction may cancel exactly before final stored-money narrowing.</summary>
    [Theory]
    [InlineData(long.MaxValue, -1, 1, long.MaxValue)]
    [InlineData(long.MinValue, 1, -1, long.MinValue)]
    public void A_representable_final_capacity_preserves_cancellation(long income, long spending, long nonMonthly, long expected) =>
        Assert.Equal(expected, new CapacityResult("EUR", income, spending, nonMonthly, 0, 0, true).Amount);

    /// <summary>Missing history remains unknown even when unused components would overflow.</summary>
    [Fact]
    public void Insufficient_history_remains_unknown() =>
        Assert.Null(new CapacityResult("EUR", long.MaxValue, -1, 0, 0, 0, false).Amount);

    /// <summary>Ceiling division of a supported remaining target never wraps before dividing.</summary>
    [Theory]
    [InlineData(2, 4_611_686_018_427_387_904)]
    [InlineData(3, 3_074_457_345_618_258_603)]
    [InlineData(12, 768_614_336_404_564_651)]
    public void A_large_remaining_goal_has_an_exact_positive_monthly_need(int months, long expected)
    {
        var today = new DateOnly(2026, 1, 1); var goal = new Goal { CurrencyCode = "EUR", Name = "Boundary target", TargetDate = today.AddMonths(months) };
        var need = CapacityCalculator.Need(goal, long.MaxValue, today);
        Assert.Equal(expected, need.PerMonth); Assert.Equal(goal.Id, need.GoalId); Assert.Equal(goal.TargetDate, need.TargetDate);
    }

    /// <summary>No target date keeps the original supported remaining amount.</summary>
    [Fact]
    public void An_undated_goal_keeps_the_whole_remaining_need() =>
        Assert.Equal(long.MaxValue, CapacityCalculator.Need(new Goal { CurrencyCode = "EUR", Name = "Undated target" }, long.MaxValue, new DateOnly(2026, 1, 1)).PerMonth);

    /// <summary>Decimal division must not round a quantity just below a whole base unit upward.</summary>
    [Fact]
    public void The_quantity_floor_preserves_a_remainder_near_the_range_boundary() =>
        Assert.Equal(999_999, CapacityCalculator.QuantityFor(long.MaxValue - 1, long.MaxValue));

    /// <summary>Exact full-range quantity and unsupported output retain distinct results.</summary>
    [Fact]
    public void An_exact_quantity_at_the_stored_limit_is_supported() =>
        Assert.Equal(long.MaxValue, CapacityCalculator.QuantityFor(long.MaxValue, 1_000_000));

    /// <summary>A quantity outside its stored base-unit range is refused.</summary>
    [Fact]
    public void An_unrepresentable_quantity_is_refused() =>
        Assert.Throws<OverflowException>(() => CapacityCalculator.QuantityFor(long.MaxValue, 1));

    /// <summary>Nonpositive capacity or price keeps the original no-suggestion policy.</summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void Nonpositive_quantity_inputs_remain_zero(long capacity, long price) =>
        Assert.Equal(0, CapacityCalculator.QuantityFor(capacity, price));

    /// <summary>Two supported monthly goal plans cannot wrap their combined commitment negative.</summary>
    [Fact]
    public void Combined_other_goal_commitments_outside_money_are_refused()
    {
        var first = new Goal { CurrencyCode = "EUR", Name = "First existing target", TargetAmount = long.MaxValue };
        var second = new Goal { CurrencyCode = "EUR", Name = "Second existing target", TargetAmount = 1 }; var rule = Monthly();
        var plans = new[] { new ContributionPlan { GoalId = first.Id, Method = ContributionMethod.FixedAmount, Amount = long.MaxValue, Rule = rule }, new ContributionPlan { GoalId = second.Id, Method = ContributionMethod.FixedAmount, Amount = 1, Rule = rule } };
        Assert.Throws<OverflowException>(() => CapacityCalculator.Compute([], [], [], [], [first, second], plans, "EUR", new DateOnly(2026, 10, 12), PeriodCalendar.Gregorian));
    }

    /// <summary>Supported loan installments cannot silently wrap the combined monthly principal.</summary>
    [Fact]
    public void Combined_monthly_loan_principal_outside_money_is_refused()
    {
        var loan = new Account { Name = "Outstanding test loan", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1), OpeningBalance = -long.MaxValue };
        var payment = new Account { Name = "Installment source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        var plans = new[] { new Schedule { Name = "First installment", Kind = EntryKind.Transfer, AccountId = payment.Id, ToAccountId = loan.Id, Amount = long.MaxValue, Rule = Monthly() }, new Schedule { Name = "Second installment", Kind = EntryKind.Transfer, AccountId = payment.Id, ToAccountId = loan.Id, Amount = 1, Rule = Monthly() } };
        Assert.Throws<OverflowException>(() => CapacityCalculator.Compute([payment, loan], [], plans, [], [], [], "EUR", new DateOnly(2026, 10, 12), PeriodCalendar.Gregorian));
    }

    /// <summary>The original distribution uses exact large needs and never exceeds available capacity.</summary>
    [Fact]
    public void A_large_exact_need_can_receive_the_original_capacity()
    {
        var today = new DateOnly(2026, 1, 1); var goal = new Goal { CurrencyCode = "EUR", Name = "Future target", TargetDate = today.AddMonths(2) };
        var need = CapacityCalculator.Need(goal, long.MaxValue, today); var result = Assert.Single(CapacityCalculator.Distribute(100, [need]));
        Assert.Equal(100, result.Suggested); Assert.Equal(4_611_686_018_427_387_904, result.Needed);
    }

    private static RecurrenceRule Monthly() => new() { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 1, 1) };
}
