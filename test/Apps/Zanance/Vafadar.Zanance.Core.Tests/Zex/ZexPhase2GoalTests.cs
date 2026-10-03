using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>Account goals of enhancement ZEX phase 2 (golden examples G12, G13; ZEX-AT18..AT21, AT27).</summary>
public sealed class ZexPhase2GoalTests
{
    private static readonly DateOnly Today = new(2026, 10, 3);

    [Fact]
    public void G12_a_balance_goal_follows_the_account_balance_both_ways()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var goal = BalanceGoal(savings, 5_000_00);

        var before = Evaluate(ledger, goal);
        Assert.Equal((2_000_00L, 3_000_00L, 0.4), (before.Current, before.Remaining, before.Progress));

        ledger.Add(EntryKind.Expense, savings, 500m, Today);
        var after = Evaluate(ledger, goal);
        Assert.Equal((1_500_00L, 3_500_00L, 0.3), (after.Current, after.Remaining, after.Progress));
    }

    [Fact]
    public void A_transfer_from_another_own_account_is_progress_but_never_income()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 1_000m);
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var goal = BalanceGoal(savings, 5_000_00);
        ledger.Transfer(main, savings, 250m, date: Today);

        Assert.Equal(2_250_00, Evaluate(ledger, goal).Current);
        Assert.Equal(0, LedgerCalculator.Totals(ledger.Accounts, ledger.Entries, new LedgerFilter(LedgerBuilder.Day1, Today)).Sum(t => t.NetIncome));
    }

    [Fact]
    public void A_withdrawal_after_reaching_shows_the_goal_as_not_reached_again()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 2_250m, AccountType.Savings);
        var goal = BalanceGoal(savings, 2_000_00);
        var reached = Evaluate(ledger, goal);
        Assert.True(reached.IsReached);
        Assert.Equal(250_00, reached.Overshoot);

        ledger.Add(EntryKind.Expense, savings, 450m, Today);
        var now = Evaluate(ledger, goal);
        Assert.False(now.IsReached);
        Assert.Equal(200_00, now.Remaining);
    }

    [Fact]
    public void G13_a_monthly_plan_needs_twelve_dates_and_the_eta_is_the_twelfth()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var goal = BalanceGoal(savings, 5_000_00);
        var plan = new ContributionPlan { Method = ContributionMethod.FixedAmount, Amount = 250_00, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 25) } };

        var progress = Evaluate(ledger, goal, plan);

        Assert.Equal(250_00, progress.PlannedContribution);
        Assert.Equal(new DateOnly(2027, 9, 25), progress.Eta);
        Assert.Equal(12, ContributionSchedule.PeriodsNeeded(progress.Remaining, 250_00));
    }

    [Fact]
    public void A_two_week_pay_cycle_counts_its_own_dates()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Weekly, Interval = 2, Start = new DateOnly(2026, 10, 3) };

        // 3,000 / 150 = 20 dates every 14 days from 3 Oct (inclusive): the 20th is 19 × 14 days later.
        Assert.Equal(new DateOnly(2027, 6, 26), ContributionSchedule.Eta(rule, Today, 3_000_00, 150_00));
        Assert.Equal(20, ContributionSchedule.Dates(rule, Today, new DateOnly(2027, 6, 26)).Count);
    }

    [Fact]
    public void Persian_months_are_counted_in_the_persian_calendar()
    {
        // 1 Mehr 1405 = 23 September 2026; monthly on day 1 of each Persian month.
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 9, 23), Calendar = PeriodCalendar.Persian };

        var dates = ContributionSchedule.Dates(rule, Today, new DateOnly(2027, 3, 31));

        Assert.Equal(6, dates.Count);
        Assert.Equal(new DateOnly(2026, 10, 23), dates[0]);
    }

    [Fact]
    public void The_required_contribution_splits_the_rest_over_the_dates_left()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var goal = BalanceGoal(savings, 5_000_00);
        goal.TargetDate = new DateOnly(2027, 10, 25);
        var plan = new ContributionPlan { Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 25) } };

        var progress = Evaluate(ledger, goal, plan);

        Assert.Equal(13, progress.Opportunities);
        Assert.Equal(230_77, progress.Required);
    }

    [Fact]
    public void A_passed_date_means_the_rest_is_needed_now_without_dividing_by_zero()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var goal = BalanceGoal(savings, 5_000_00);
        goal.TargetDate = Today.AddDays(-1);

        var progress = Evaluate(ledger, goal);

        Assert.Equal(GoalNotice.Overdue, progress.Notice);
        Assert.Equal(0, progress.Opportunities);
        Assert.Equal(3_000_00, progress.Required);
    }

    [Fact]
    public void An_archived_account_and_a_negative_balance_are_reported()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", -120m, AccountType.Savings);
        var goal = BalanceGoal(savings, 1_000_00);

        var negative = Evaluate(ledger, goal);
        Assert.Equal((GoalNotice.NegativeBalance, 0.0, 1_000_00L), (negative.Notice, negative.Progress, negative.Remaining));

        savings.IsArchived = true;
        Assert.Equal(GoalNotice.AccountUnavailable, Evaluate(ledger, goal).Notice);
    }

    [Fact]
    public void A_paused_goal_keeps_its_numbers_and_money_set_aside()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 1_000m);
        var goal = new Goal { Name = "Trip", CurrencyCode = "EUR", TargetAmount = 2_000_00, State = GoalState.Paused };
        var allocation = new GoalAllocation { GoalId = goal.Id, AccountId = main.Id, Amount = 600_00, Date = Today };

        var progress = Assert.Single(GoalProgressService.Evaluate([goal], [allocation], ledger.Accounts, ledger.Entries, [], Today));

        Assert.Equal((600_00L, GoalNotice.Paused), (progress.Current, progress.Notice));
        Assert.Equal(600_00, GoalCalculator.Accounts([goal], [allocation], new Dictionary<Guid, long> { [main.Id] = 1_000_00 }).Single().Earmarked);
    }

    [Fact]
    public void Existing_goals_without_a_plan_keep_their_monthly_counting()
    {
        var goal = new Goal { Name = "Car", CurrencyCode = "EUR", TargetAmount = 1_200_00, TargetDate = new DateOnly(2027, 1, 2) };
        var rule = ContributionSchedule.RuleFor(goal, null, Today);

        Assert.Equal(GoalCalculator.Opportunities(ContributionFrequency.Monthly, Today, goal.TargetDate.Value), ContributionSchedule.Opportunities(rule, Today, goal.TargetDate.Value));
        goal.Frequency = ContributionFrequency.Weekly;
        Assert.Equal(GoalCalculator.Opportunities(ContributionFrequency.Weekly, Today, goal.TargetDate.Value), ContributionSchedule.Opportunities(ContributionSchedule.RuleFor(goal, null, Today), Today, goal.TargetDate.Value));
    }

    [Fact]
    public void Only_one_balance_goal_per_account()
    {
        var accountId = Guid.CreateVersion7();
        var existing = new Goal { Name = "A", CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = accountId };

        Assert.True(GoalProgressService.HasOtherBalanceGoal([existing], accountId, Guid.CreateVersion7()));
        Assert.False(GoalProgressService.HasOtherBalanceGoal([existing], accountId, existing.Id));
        existing.State = GoalState.Completed;
        Assert.False(GoalProgressService.HasOtherBalanceGoal([existing], accountId, Guid.CreateVersion7()));
    }

    [Fact]
    public void Eligible_income_leaves_out_transfers_refunds_and_excluded_categories()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 0m);
        var savings = ledger.Account("Savings", 2_000m, AccountType.Savings);
        var bonus = Guid.CreateVersion7();
        ledger.Add(EntryKind.Income, main, 3_000m, new DateOnly(2026, 10, 25));
        ledger.Add(EntryKind.Income, main, 400m, new DateOnly(2026, 10, 28), bonus);
        ledger.Transfer(savings, main, 500m, date: new DateOnly(2026, 10, 10));
        var purchase = ledger.Add(EntryKind.Expense, main, 80m, new DateOnly(2026, 10, 12));
        ledger.Refund(purchase, main, 80m, new DateOnly(2026, 10, 20));

        var income = EligibleIncome.Of(ledger.Accounts, ledger.Entries, "EUR", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), excludedCategoryIds: [bonus]);

        Assert.Equal(3_000_00, income.Total);
        Assert.Single(income.Entries);
        Assert.Equal(300_00, EligibleIncome.Suggestion(income.Total, 10m));
    }

    private static Goal BalanceGoal(Account account, long target) =>
        new() { Name = "Emergency fund", CurrencyCode = account.CurrencyCode, TargetAmount = target, Type = GoalType.AccountBalance, AccountId = account.Id };

    private static GoalProgress Evaluate(LedgerBuilder ledger, Goal goal, ContributionPlan? plan = null)
    {
        if (plan is not null)
        {
            plan.GoalId = goal.Id;
        }

        return Assert.Single(GoalProgressService.Evaluate([goal], [], ledger.Accounts, ledger.Entries, plan is null ? [] : [plan], Today));
    }
}
