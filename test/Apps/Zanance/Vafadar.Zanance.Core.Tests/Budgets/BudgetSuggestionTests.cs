using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Suggestions for adjusting limits (§10.3).</summary>
public sealed class BudgetSuggestionTests
{
    private readonly Account _checking = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Category _food = new() { Kind = CategoryKind.Expense, SystemKey = "Food" };
    private readonly Category _rent = new() { Kind = CategoryKind.Expense, SystemKey = "Housing" };

    private LedgerEntry Expense(Category category, long amount, DateOnly date) =>
        new() { Kind = EntryKind.Expense, AccountId = _checking.Id, CategoryId = category.Id, Amount = amount, Date = date };

    [Fact]
    public void The_average_of_the_last_three_months_is_rounded_up_per_budget_and_category()
    {
        LedgerEntry[] entries =
        [
            Expense(_food, 400_00, new DateOnly(2026, 6, 10)), Expense(_food, 450_00, new DateOnly(2026, 7, 10)), Expense(_food, 461_60, new DateOnly(2026, 8, 10)),
            Expense(_rent, 900_00, new DateOnly(2026, 6, 1)), Expense(_rent, 900_00, new DateOnly(2026, 7, 1)), Expense(_rent, 900_00, new DateOnly(2026, 8, 1)),
            Expense(_food, 999_00, new DateOnly(2026, 9, 2)), // The month being planned does not count.
        ];

        var suggestion = BudgetSuggestions.Suggest([_checking], entries, [_food, _rent], 2026, 9, PeriodCalendar.Gregorian, 1, "EUR");

        Assert.Equal(new LimitSuggestion(437_20, 440_00, 3), suggestion.Categories[_food.Id]);
        Assert.Equal(900_00, suggestion.Categories[_rent.Id].Suggested);
        Assert.Equal(1_337_20, suggestion.Total!.Average);
        Assert.Equal(1_400_00, suggestion.Total.Suggested);
    }

    [Fact]
    public void Months_before_the_first_expense_are_not_counted_as_zero()
    {
        var suggestion = BudgetSuggestions.Suggest([_checking], [Expense(_food, 300_00, new DateOnly(2026, 8, 20))], [_food], 2026, 9, PeriodCalendar.Gregorian, 1, "EUR");

        Assert.Equal(1, suggestion.Total!.Months);
        Assert.Equal(300_00, suggestion.Total.Average);
    }

    [Fact]
    public void Without_history_nothing_is_suggested() =>
        Assert.Same(BudgetSuggestion.None, BudgetSuggestions.Suggest([_checking], [], [_food], 2026, 9, PeriodCalendar.Gregorian, 1, "EUR"));

    [Fact]
    public void Suggestions_follow_the_financial_month()
    {
        // With the 25th, the month before "September" is 25 July to 24 August: the 24 August expense counts, the 25th not.
        LedgerEntry[] entries = [Expense(_food, 100_00, new DateOnly(2026, 8, 24)), Expense(_food, 500_00, new DateOnly(2026, 8, 25))];

        var suggestion = BudgetSuggestions.Suggest([_checking], entries, [_food], 2026, 8, PeriodCalendar.Gregorian, 25, "EUR");

        Assert.Equal(100_00, suggestion.Categories[_food.Id].Average);
    }

    [Theory]
    [InlineData(437_20, 100, 440_00)]
    [InlineData(12_30, 100, 13_00)]
    [InlineData(1_234_567, 1, 1_300_000)]
    [InlineData(99, 100, 1_00)]
    [InlineData(440_00, 100, 440_00)]
    public void Amounts_are_rounded_up_to_two_significant_digits_and_whole_units(long amount, long minorFactor, long expected) =>
        Assert.Equal(expected, BudgetSuggestions.RoundUp(amount, minorFactor));
}
