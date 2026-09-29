using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Weekly and two-week budget periods (§10.3).</summary>
public sealed class BudgetPeriodTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, 2026, 9, 28)]
    [InlineData(DayOfWeek.Saturday, 2026, 9, 26)]
    [InlineData(DayOfWeek.Sunday, 2026, 9, 27)]
    public void A_week_starts_on_the_chosen_first_day(DayOfWeek first, int year, int month, int day)
    {
        // Tuesday 29 September 2026.
        Assert.Equal(new DateOnly(year, month, day), BudgetPeriods.StartOf(BudgetPeriod.Week, new DateOnly(2026, 9, 29), first));
    }

    [Fact]
    public void Two_week_periods_repeat_every_14_days_from_the_start_day_in_both_directions()
    {
        var payday = new DateOnly(2026, 9, 18);

        Assert.Equal(payday, BudgetPeriods.StartOf(BudgetPeriod.TwoWeeks, new DateOnly(2026, 10, 1), DayOfWeek.Monday, payday));
        Assert.Equal(new DateOnly(2026, 10, 2), BudgetPeriods.StartOf(BudgetPeriod.TwoWeeks, new DateOnly(2026, 10, 2), DayOfWeek.Monday, payday));
        Assert.Equal(new DateOnly(2026, 9, 4), BudgetPeriods.StartOf(BudgetPeriod.TwoWeeks, new DateOnly(2026, 9, 17), DayOfWeek.Monday, payday));
        Assert.Equal((new DateOnly(2026, 9, 18), new DateOnly(2026, 10, 1)), BudgetPeriods.Range(BudgetPeriod.TwoWeeks, payday));
    }

    [Fact]
    public void The_periods_before_a_week_are_the_complete_weeks_newest_first()
    {
        var periods = BudgetPeriods.Before(BudgetPeriod.Week, 0, 0, new DateOnly(2026, 9, 28), PeriodCalendar.Gregorian, 1, 2);

        Assert.Equal([(new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 27)), (new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 20))], periods);
    }

    [Fact]
    public void A_weekly_budget_is_copied_to_another_week_with_its_limits()
    {
        var food = Guid.NewGuid();
        var week = new Budget
        {
            Period = BudgetPeriod.Week, PeriodStart = new DateOnly(2026, 9, 28), CurrencyCode = "EUR", TotalLimit = 150_00,
            CategoryLimits = [new BudgetCategoryLimit { CategoryId = food, Limit = 80_00 }],
        };

        var next = BudgetPlanning.CopyTo(week, new DateOnly(2026, 10, 5));

        Assert.Equal((BudgetPeriod.Week, new DateOnly(2026, 10, 5), 0, 0), (next.Period, next.PeriodStart, next.Year, next.Month));
        Assert.Equal(80_00, next.CategoryLimits.Single(l => l.CategoryId == food).Limit);
        Assert.NotEqual(week.Id, next.Id);
    }

    [Fact]
    public void Weekly_suggestions_average_the_last_four_weeks()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        var food = new Category { Kind = CategoryKind.Expense, SystemKey = "Food" };
        var start = new DateOnly(2026, 9, 28);
        var entries = Enumerable.Range(1, 4)
            .Select(i => new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = food.Id, Amount = 100_00 * i, Date = start.AddDays(-7 * i) })
            .ToList();
        var periods = BudgetPeriods.Before(BudgetPeriod.Week, 0, 0, start, PeriodCalendar.Gregorian, 1, BudgetSuggestions.PeriodsFor(BudgetPeriod.Week));

        var suggestion = BudgetSuggestions.Suggest([account], entries, [food], periods, "EUR");

        Assert.Equal(new LimitSuggestion(250_00, 250_00, 4), suggestion.Total);
    }
}
