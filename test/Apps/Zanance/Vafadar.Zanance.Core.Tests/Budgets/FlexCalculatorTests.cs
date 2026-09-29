using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Budgets;

public sealed class FlexCalculatorTests
{
    private static readonly DateOnly From = new(2027, 3, 1);
    private static readonly DateOnly To = new(2027, 3, 31);
    private static readonly DateOnly Today = new(2027, 3, 20);

    private readonly Account _account = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2027, 1, 1) };
    private readonly Category _housing = new() { Kind = CategoryKind.Expense, SystemKey = "Housing", SpendingType = SpendingType.Fixed };
    private readonly Category _insurance = new() { Kind = CategoryKind.Expense, SystemKey = "Insurance", SpendingType = SpendingType.NonMonthly };
    private readonly Category _food = new() { Kind = CategoryKind.Expense, SystemKey = "Food" };
    private readonly Category _other = new() { Kind = CategoryKind.Expense, SystemKey = "Other" };

    [Fact]
    public void Fixed_bills_come_from_the_plans_non_monthly_bills_get_a_monthly_share_and_the_rest_is_flexible()
    {
        var rent = Plan(_housing, Frequency.Monthly, new DateOnly(2027, 1, 1), 90_000);
        var insurance = Plan(_insurance, Frequency.Yearly, new DateOnly(2026, 11, 1), 60_000);
        var entries = new[]
        {
            Expense(_housing, 90_000, new DateOnly(2027, 3, 1)),
            Expense(_food, 25_000, new DateOnly(2027, 3, 4)),
            Expense(_other, 7_000, new DateOnly(2027, 3, 9)),
        };

        var flex = FlexCalculator.Summarize(40_000, [_account], entries, [rent, insurance], [], [_housing, _insurance, _food, _other], From, To, "EUR", Today);

        Assert.Equal(new FlexGroup(90_000, 90_000), flex.Fixed);
        Assert.Equal(new FlexGroup(5_000, 0), flex.NonMonthly);
        Assert.Equal(new BudgetStatus(40_000, 32_000), flex.Flexible);
        Assert.Equal(135_000, flex.Total);
    }

    [Fact]
    public void Every_amount_counts_once_and_refunds_reduce_their_group()
    {
        var entries = new[]
        {
            Expense(_insurance, 60_000, new DateOnly(2027, 3, 2)),
            Expense(_food, 10_000, new DateOnly(2027, 3, 3)),
            new LedgerEntry { Kind = EntryKind.Refund, AccountId = _account.Id, CategoryId = _food.Id, Amount = 2_000, Date = new DateOnly(2027, 3, 5) },
        };

        var flex = FlexCalculator.Summarize(20_000, [_account], entries, [], [], [_housing, _insurance, _food, _other], From, To, "EUR", Today);

        Assert.Equal(60_000, flex.NonMonthly.Spent);
        Assert.Equal(8_000, flex.Flexible.Spent);
        Assert.Equal(8_000, FlexCalculator.FlexibleSpent([_account], entries, [_housing, _insurance, _food, _other], From, To, "EUR"));
    }

    [Fact]
    public void A_sub_category_follows_its_parent()
    {
        var electricity = new Category { Kind = CategoryKind.Expense, ParentId = _housing.Id, SpendingType = SpendingType.Flexible };

        Assert.Contains(electricity.Id, FlexCalculator.CategoriesOf([_housing, electricity, _food], SpendingType.Fixed));
        Assert.DoesNotContain(electricity.Id, FlexCalculator.CategoriesOf([_housing, electricity, _food], SpendingType.Flexible));
    }

    [Fact]
    public void Default_bills_start_as_fixed_or_non_monthly()
    {
        Assert.Equal(SpendingType.Fixed, DefaultCategories.SpendingTypeOf("Housing"));
        Assert.Equal(SpendingType.NonMonthly, DefaultCategories.SpendingTypeOf("Insurance"));
        Assert.Equal(SpendingType.Flexible, DefaultCategories.SpendingTypeOf("Food"));
    }

    private Schedule Plan(Category category, Frequency frequency, DateOnly start, long amount) => new()
    {
        Name = category.SystemKey!,
        Kind = EntryKind.Expense,
        AccountId = _account.Id,
        CategoryId = category.Id,
        Amount = amount,
        Rule = new RecurrenceRule { Frequency = frequency, Start = start },
    };

    private LedgerEntry Expense(Category category, long amount, DateOnly date) =>
        new() { Kind = EntryKind.Expense, AccountId = _account.Id, CategoryId = category.Id, Amount = amount, Date = date };
}
