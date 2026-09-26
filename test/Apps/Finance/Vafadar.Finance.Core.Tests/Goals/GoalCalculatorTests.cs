using Vafadar.Finance.Core.Goals;

namespace Vafadar.Finance.Core.Tests.Goals;

public sealed class GoalCalculatorTests
{
    private static readonly DateOnly Today = new(2027, 3, 10);
    private static readonly Guid Savings = Guid.CreateVersion7();
    private static readonly Guid Checking = Guid.CreateVersion7();

    [Fact]
    [Trait("AT", "AT-65")]
    public void The_same_money_never_funds_two_goals()
    {
        var emergency = Goal("Emergency fund", 1_000_00, priority: GoalPriority.High);
        var travel = Goal("Travel", 1_000_00);
        var allocations = new[] { Allocate(emergency, Savings, 800_00), Allocate(travel, Savings, 600_00) };

        var status = GoalCalculator.Evaluate([emergency, travel], allocations, Balances(savings: 1_000_00), Today);

        var e = status.Single(s => s.Goal == emergency);
        var t = status.Single(s => s.Goal == travel);
        Assert.Equal(800_00, e.Funded);
        Assert.Equal(200_00, t.Funded);
        Assert.Equal(400_00, t.Unfunded);
        Assert.Equal(1_000_00, status.Sum(s => s.Funded));

        var account = Assert.Single(GoalCalculator.Accounts([emergency, travel], allocations, Balances(savings: 1_000_00)));
        Assert.Equal(1_400_00, account.Earmarked);
        Assert.Equal(400_00, account.Shortfall);
        Assert.Equal(0, account.Unallocated);
    }

    [Fact]
    public void Allocating_and_releasing_never_change_balances_and_a_falling_balance_shows_the_shortfall()
    {
        var goal = Goal("Laptop", 1_500_00);
        var allocations = new[] { Allocate(goal, Savings, 900_00), Allocate(goal, Savings, -200_00), Allocate(goal, Checking, 100_00) };

        var covered = GoalCalculator.Evaluate([goal], allocations, Balances(savings: 2_000_00, checking: 500_00), Today).Single();
        Assert.Equal(800_00, covered.Allocated);
        Assert.Equal(800_00, covered.Funded);
        Assert.Equal(700_00, covered.Remaining);

        // The savings balance dropped after a purchase: only 300 of the 700 earmarked there is still covered (F2-GOAL-05).
        var afterSpending = GoalCalculator.Evaluate([goal], allocations, Balances(savings: 300_00, checking: 500_00), Today).Single();
        Assert.Equal(400_00, afterSpending.Funded);
        Assert.Equal(400_00, afterSpending.Unfunded);
        Assert.Equal(1_100_00, afterSpending.Remaining);
    }

    [Fact]
    public void Suggested_contribution_uses_the_opportunities_left_not_the_monthly_average()
    {
        // §10.2: yearly insurance of 720, 180 already set aside, three saving opportunities left → 180 each, not 60.
        var insurance = Goal("Insurance", 720_00, target: Today.AddMonths(2));
        var status = GoalCalculator.Evaluate([insurance], [Allocate(insurance, Savings, 180_00)], Balances(savings: 5_000_00), Today).Single();

        Assert.Equal(3, status.Opportunities);
        Assert.Equal(180_00, status.SuggestedContribution);
    }

    [Theory]
    [InlineData(ContributionFrequency.Monthly, "2027-03-10", "2027-03-10", 1)]
    [InlineData(ContributionFrequency.Monthly, "2027-03-10", "2027-06-09", 3)]
    [InlineData(ContributionFrequency.Monthly, "2027-03-10", "2027-06-10", 4)]
    [InlineData(ContributionFrequency.Weekly, "2027-03-10", "2027-03-30", 3)]
    [InlineData(ContributionFrequency.Monthly, "2027-03-10", "2027-03-09", 0)]
    public void Opportunities_count_the_current_period(ContributionFrequency frequency, string today, string target, int expected) =>
        Assert.Equal(expected, GoalCalculator.Opportunities(frequency, DateOnly.Parse(today), DateOnly.Parse(target)));

    [Fact]
    public void Suggestions_round_up_are_due_at_once_when_late_and_absent_without_a_date()
    {
        Assert.Equal(334, GoalCalculator.SuggestedContribution(1_000, 3));
        Assert.Equal(500, GoalCalculator.SuggestedContribution(500, 0));
        Assert.Equal(0, GoalCalculator.SuggestedContribution(0, 4));

        var open = Goal("Someday", 100_00);
        Assert.Null(GoalCalculator.Evaluate([open], [], Balances(), Today).Single().SuggestedContribution);

        var late = Goal("Late", 100_00, target: Today.AddDays(-1));
        Assert.True(GoalCalculator.Evaluate([late], [], Balances(), Today).Single().IsOverdue(Today));
    }

    [Fact]
    public void Completed_and_archived_goals_release_their_earmark()
    {
        var done = Goal("Done", 100_00);
        done.State = GoalState.Completed;
        var allocations = new[] { Allocate(done, Savings, 100_00) };

        Assert.Empty(GoalCalculator.Evaluate([done], allocations, Balances(savings: 100_00), Today));
        Assert.Empty(GoalCalculator.Accounts([done], allocations, Balances(savings: 100_00)));
    }

    private static Goal Goal(string name, long amount, GoalPriority priority = GoalPriority.Normal, DateOnly? target = null) => new()
    {
        Name = name,
        TargetAmount = amount,
        CurrencyCode = "EUR",
        Priority = priority,
        TargetDate = target,
    };

    private static GoalAllocation Allocate(Goal goal, Guid account, long amount) => new() { GoalId = goal.Id, AccountId = account, Amount = amount, Date = Today };

    private static Dictionary<Guid, long> Balances(long savings = 0, long checking = 0) => new() { [Savings] = savings, [Checking] = checking };
}
