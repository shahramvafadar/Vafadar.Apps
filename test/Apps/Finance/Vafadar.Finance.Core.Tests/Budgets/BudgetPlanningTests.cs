using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Tests.Budgets;

public sealed class BudgetPlanningTests
{
    private static readonly DateOnly Today = new(2027, 1, 10);
    private readonly Account _account = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2027, 1, 1) };

    private Dictionary<Guid, Account> Accounts => new() { [_account.Id] = _account };

    [Fact]
    [Trait("AT", "AT-18")]
    public void A_month_with_three_bi_weekly_payments_sums_three_real_amounts()
    {
        var plan = Plan(new RecurrenceRule { Frequency = Frequency.Weekly, Interval = 2, Start = new DateOnly(2027, 1, 1) }, 4_000);

        var january = BudgetPlanning.PlannedInPeriod([plan], [], Accounts, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), "EUR", Today);
        var february = BudgetPlanning.PlannedInPeriod([plan], [], Accounts, new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28), "EUR", Today);

        Assert.Equal(3, january.Count);
        Assert.Equal(12_000, january.Total);
        Assert.Equal(2, february.Count);
        Assert.Equal(8_000, february.Total);
    }

    [Fact]
    public void Unknown_amounts_are_counted_as_incomplete_and_skipped_occurrences_are_left_out()
    {
        var unknown = Plan(new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 20) }, null);
        unknown.AmountMode = AmountMode.Unknown;
        var rent = Plan(new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 5) }, 90_000);
        var skipped = new OccurrenceState { ScheduleId = rent.Id, OriginalDate = new DateOnly(2027, 1, 5), Status = OccurrenceStatus.Skipped };

        var planned = BudgetPlanning.PlannedInPeriod([unknown, rent], [skipped], Accounts, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), "EUR", Today);

        Assert.Equal(0, planned.Total);
        Assert.Equal(1, planned.Count);
        Assert.Equal(1, planned.UnknownCount);
    }

    [Fact]
    [Trait("AT", "AT-41")]
    public void Monthly_equivalent_of_a_yearly_plan_is_for_comparison_only()
    {
        var insurance = Plan(new RecurrenceRule { Frequency = Frequency.Yearly, Start = new DateOnly(2027, 6, 1) }, 60_000);
        var quarterly = Plan(new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 3, Start = new DateOnly(2027, 3, 1) }, 30_000);
        var monthly = Plan(new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 3, 1) }, 30_000);

        Assert.Equal(5_000, BudgetPlanning.MonthlyEquivalent(insurance));
        Assert.Equal(10_000, BudgetPlanning.MonthlyEquivalent(quarterly));
        Assert.Null(BudgetPlanning.MonthlyEquivalent(monthly));

        // January has no insurance payment: the real planned expense of the month stays zero.
        var january = BudgetPlanning.PlannedInPeriod([insurance], [], Accounts, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), "EUR", Today);
        Assert.Equal(0, january.Total);
    }

    [Fact]
    public void Copying_a_budget_keeps_limits_and_leaves_the_source_unchanged()
    {
        var category = Guid.CreateVersion7();
        var source = new Budget { Year = 2027, Month = 1, CurrencyCode = "EUR", TotalLimit = 100_000, CategoryLimits = [new() { CategoryId = category, Limit = 20_000 }] };

        var copy = BudgetPlanning.CopyTo(source, 2027, 2);
        copy.CategoryLimits[0].Limit = 1;

        Assert.Equal(2, copy.Month);
        Assert.Equal(100_000, copy.TotalLimit);
        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal(20_000, source.CategoryLimits[0].Limit);
    }

    private Schedule Plan(RecurrenceRule rule, long? amount) => new()
    {
        Name = "Plan",
        Kind = EntryKind.Expense,
        AccountId = _account.Id,
        Amount = amount,
        Rule = rule,
    };
}
