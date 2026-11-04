using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Budget read aggregates preserve representable outputs and reject wrapped money.</summary>
public sealed class BudgetReadAggregationTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly Guid CategoryId = new("82900000-0000-0000-0000-000000000001");
    private static Account Account() => new() { Name = "Boundary", CurrencyCode = "EUR", OpeningDate = DateOnly.MinValue };
    private static Schedule Plan(Account account, long amount, DateOnly day, Frequency frequency = Frequency.Once) => new()
    { Name = "Boundary plan", AccountId = account.Id, CategoryId = CategoryId, Amount = amount, Rule = new() { Start = day, Frequency = frequency } };
    private static BudgetMonth Month(long limit, long spent, bool category) => new(BudgetRollover.SurplusAndDeficit,
        category ? null : limit, category ? 0 : spent, category ? new Dictionary<Guid, long> { [CategoryId] = limit } : [],
        category ? new Dictionary<Guid, long> { [CategoryId] = spent } : []);

    [Theory]
    [InlineData(4_500_000_000_000_000_000, 3)]
    [InlineData(9_007_199_254_740_993, 1)]
    [InlineData(9_007_199_254_740_993, 2)]
    [InlineData(9_007_199_254_740_994, 3)]
    public void Suggested_average_is_exact_even_when_the_cross_period_sum_is_wider(long average, int periods)
    {
        var account = Account();
        var ranges = Enumerable.Range(0, periods).Select(i => (Day.AddDays(-i), Day.AddDays(-i))).ToArray();
        var entries = ranges.Select(p => new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = average, Date = p.Item1 }).ToArray();
        var result = BudgetSuggestions.Suggest([account], entries, [], ranges, "EUR").Total!;
        Assert.Equal(average, result.Average);
        Assert.Equal(periods, result.Months);
        Assert.Equal(BudgetSuggestions.RoundUp(average, 100), result.Suggested);
        Assert.All(entries, e => Assert.Equal(average, e.Amount));
    }

    [Theory]
    [InlineData(2)] [InlineData(3)]
    public void Suggested_average_rounds_a_fraction_up_without_losing_low_digits(int periods)
    {
        const long amount = 9_007_199_254_740_993;
        var account = Account();
        var ranges = Enumerable.Range(0, periods).Select(i => (Day.AddDays(-i), Day.AddDays(-i))).ToArray();
        var entries = ranges.Select((p, i) => new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = amount + (i == 0 ? 1 : 0), Date = p.Item1 }).ToArray();
        Assert.Equal(amount + 1, BudgetSuggestions.Suggest([account], entries, [], ranges, "EUR").Total!.Average);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Envelope_parts_refuse_out_of_range_totals(bool overspent)
    {
        BudgetStatus[] statuses = overspent ? [new(0, long.MaxValue), new(0, 1)] : [new(long.MaxValue, 0), new(1, 0)];
        Assert.Throws<OverflowException>(() => EnvelopeCalculator.Summarize(0, 0, statuses));
    }

    [Fact]
    public void An_unrepresentable_overspent_magnitude_refuses() =>
        Assert.Throws<OverflowException>(() => EnvelopeCalculator.Summarize(0, 0, [new BudgetStatus(long.MinValue, 0)]));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Envelope_unassigned_refuses_an_unrepresentable_result(bool negative)
    {
        var result = negative ? new EnvelopeSummary(long.MinValue, 1, 0, 0) : new EnvelopeSummary(long.MaxValue, -1, 0, 0);
        Assert.Throws<OverflowException>(() => result.Unassigned);
    }

    [Fact]
    public void Zero_open_expense_is_clamped_before_narrowing() =>
        Assert.Equal(0, new FlexGroup(long.MinValue, long.MaxValue).Open);

    [Fact]
    public void Positive_open_expense_cannot_wrap_to_zero() =>
        Assert.Throws<OverflowException>(() => new FlexGroup(long.MaxValue, -1).Open);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Flex_total_must_fit_but_exact_cancellation_is_allowed(bool cancel)
    {
        var result = new FlexSummary(new(long.MaxValue, 0), new(1, 0), new(cancel ? -1 : 0, 0));
        if (cancel) Assert.Equal(long.MaxValue, result.Total);
        else Assert.Throws<OverflowException>(() => result.Total);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Known_planned_occurrences_do_not_wrap(bool unknownAlso)
    {
        var account = Account();
        var plans = new List<Schedule> { Plan(account, long.MaxValue, Day), Plan(account, 1, Day) };
        if (unknownAlso) { var unknown = Plan(account, 1, Day); unknown.Amount = null; unknown.AmountMode = AmountMode.Unknown; plans.Add(unknown); }
        Assert.Throws<OverflowException>(() => BudgetPlanning.PlannedInPeriod(plans, [], new Dictionary<Guid, Account> { [account.Id] = account }, Day, Day, "EUR", Day));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Flexible_spending_refuses_an_unrepresentable_difference(bool summary)
    {
        var account = Account();
        var fixedCategory = new Category { Name = "Fixed", Kind = CategoryKind.Expense, SpendingType = SpendingType.Fixed };
        LedgerEntry[] entries = [
            new() { AccountId = account.Id, Kind = EntryKind.Refund, CategoryId = fixedCategory.Id, Amount = 1, Date = Day },
            new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = long.MaxValue, Date = Day },
            new() { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1, Date = Day }];
        if (summary) Assert.Throws<OverflowException>(() => FlexCalculator.Summarize(0, [account], entries, [], [], [fixedCategory], Day, Day, "EUR", Day));
        else Assert.Throws<OverflowException>(() => FlexCalculator.FlexibleSpent([account], entries, [fixedCategory], Day, Day, "EUR"));
    }

    [Fact]
    public void Nonmonthly_shares_do_not_wrap()
    {
        var account = Account();
        var category = new Category { Name = "Nonmonthly", Kind = CategoryKind.Expense, SpendingType = SpendingType.NonMonthly };
        var plans = Enumerable.Range(0, 13).Select(_ => Plan(account, long.MaxValue, Day, Frequency.Yearly)).ToArray();
        foreach (var plan in plans) plan.CategoryId = category.Id;
        Assert.Throws<OverflowException>(() => FlexCalculator.Summarize(0, [account], [], plans, [], [category], Day, Day, "EUR", Day));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Last_year_nonmonthly_lookahead_keeps_current_occurrences(bool future)
    {
        var day = DateOnly.MaxValue.AddDays(-10); var account = Account();
        var category = new Category { Name = "Last year", Kind = CategoryKind.Expense, SpendingType = SpendingType.NonMonthly };
        var plan = Plan(account, 1200, future ? DateOnly.MaxValue : day, Frequency.Yearly);
        plan.CategoryId = category.Id;
        var result = FlexCalculator.Summarize(0, [account], [], [plan], [], [category], day, DateOnly.MaxValue, "EUR", day);
        Assert.Equal(100, result.NonMonthly.Planned);
        Assert.Equal(0, result.NonMonthly.Spent);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Carry_refuses_an_unrepresentable_surplus(bool category) =>
        Assert.Throws<OverflowException>(() => BudgetRolloverCalculator.CarryInto([Month(long.MaxValue, -1, category)], BudgetRollover.Surplus));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Carry_preserves_cancellation_in_the_complete_month(bool category)
    {
        var result = BudgetRolloverCalculator.CarryInto([Month(long.MaxValue, 0, category), Month(long.MaxValue, long.MaxValue, category)], BudgetRollover.SurplusAndDeficit);
        Assert.Equal(long.MaxValue, category ? result.For(CategoryId) : result.Total);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Surplus_mode_discards_an_unrepresentable_negative_rest_before_narrowing(bool category)
    {
        var result = BudgetRolloverCalculator.CarryInto([Month(0, long.MaxValue, category), Month(0, long.MaxValue, category)], BudgetRollover.Surplus);
        Assert.Equal(0, category ? result.For(CategoryId) : result.Total);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Deficit_mode_refuses_that_same_unrepresentable_negative_rest(bool category) =>
        Assert.Throws<OverflowException>(() => BudgetRolloverCalculator.CarryInto([Month(0, long.MaxValue, category), Month(0, long.MaxValue, category)], BudgetRollover.SurplusAndDeficit));
}
