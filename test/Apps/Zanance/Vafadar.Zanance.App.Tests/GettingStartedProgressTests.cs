using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exercises the original guidance policy rather than substituting record counts for named tasks.</summary>
public sealed class GettingStartedProgressTests
{
    [Theory, Trait("AT", "AT-376")]
    [InlineData(EntryKind.Income, false)]
    [InlineData(EntryKind.Transfer, false)]
    [InlineData(EntryKind.Refund, false)]
    [InlineData(EntryKind.Expense, true)]
    public void Only_a_saved_expense_completes_the_first_expense(EntryKind kind, bool expected)
    {
        var entry = new LedgerEntry { AccountId = Guid.NewGuid(), Kind = kind, Date = new(2026, 10, 1), Amount = 100 };
        Assert.Equal(expected, GettingStartedProgress.From([entry], [], []).FirstExpenseDone);
    }

    [Theory, Trait("AT", "AT-376")]
    [InlineData(Frequency.Once, false)]
    [InlineData(Frequency.Daily, true)]
    [InlineData(Frequency.Weekly, true)]
    [InlineData(Frequency.Monthly, true)]
    [InlineData(Frequency.Yearly, true)]
    [InlineData((Frequency)999, false)]
    public void A_one_off_plan_does_not_complete_the_regular_payment(Frequency frequency, bool expected)
    {
        var plan = new Schedule { Name = "Fictitious plan", Rule = new() { Frequency = frequency, Start = new(2026, 10, 1) } };
        Assert.Equal(expected, GettingStartedProgress.From([], [plan], []).RecurringPlanDone);
    }

    [Theory, Trait("AT", "AT-376")]
    [InlineData(BudgetPeriod.Month, true)]
    [InlineData(BudgetPeriod.Week, false)]
    [InlineData(BudgetPeriod.TwoWeeks, false)]
    public void Weekly_budgets_do_not_complete_the_monthly_budget(BudgetPeriod period, bool expected)
    {
        Assert.Equal(expected, GettingStartedProgress.From([], [], [new Budget { CurrencyCode = "EUR", Period = period }]).MonthlyBudgetDone);
    }

    [Fact, Trait("AT", "AT-376")]
    public void Each_step_is_independent_and_completion_reverses_when_its_saved_record_is_removed()
    {
        var entry = new LedgerEntry { AccountId = Guid.NewGuid(), Kind = EntryKind.Expense, Date = new(2026, 10, 1), Amount = 100 };
        var plan = new Schedule { Name = "Fictitious recurring plan", Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 10, 1) } };
        var budget = new Budget { CurrencyCode = "EUR", Period = BudgetPeriod.Month };
        Assert.True(GettingStartedProgress.From([entry], [plan], [budget]).IsComplete);
        Assert.False(GettingStartedProgress.From([], [plan], [budget]).IsComplete);
        Assert.False(GettingStartedProgress.From([entry], [], [budget]).IsComplete);
        Assert.False(GettingStartedProgress.From([entry], [plan], []).IsComplete);
        Assert.Equal(new GettingStartedProgress(false, false, false), GettingStartedProgress.From([], [], []));
    }
}
