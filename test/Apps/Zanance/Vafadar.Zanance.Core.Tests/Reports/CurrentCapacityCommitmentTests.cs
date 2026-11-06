using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Current capacity subtracts only current commitments, in the units of their original methods.</summary>
public sealed class CurrentCapacityCommitmentTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    /// <summary>Past installment slices and exhausted ownership must not add a second current principal estimate.</summary>
    [Theory]
    [InlineData(0, 11000)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0)]
    public void Only_remaining_installment_dates_contribute_principal(int shape, long expected)
    {
        var payment = Payment(); var loan = Loan(); var first = Installment(payment, loan); var plans = new List<Schedule> { first };
        if (shape == 0) { var next = PlanActions.SplitFrom(first, Today); next.Amount = 12000; plans.Add(next); }
        if (shape == 1) { first.Rule.End = EndKind.OnDate; first.Rule.EndDate = Today.AddDays(-1); }
        if (shape == 2) { first.Rule.End = EndKind.AfterCount; first.Rule.Count = 2; }
        if (shape == 3) PlanActions.Pause(first, Today);
        if (shape == 4) { first.ActiveFrom = Today.AddDays(1); first.ActiveUntil = Today.AddDays(2); }
        var before = JsonSerializer.Serialize(plans);
        var result = Compute([payment, loan], [], plans, [], []);
        Assert.Equal(expected, result.LoanPrincipal); Assert.Equal(-expected, result.Amount);
        Assert.Equal(before, JsonSerializer.Serialize(plans)); Assert.Equal(-100000, loan.OpeningBalance);
    }

    /// <summary>The final allowed installment date remains inclusive, with the unchanged interest/principal split.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_last_current_installment_keeps_its_original_split(bool counted)
    {
        var payment = Payment(); var loan = Loan(); var plan = Installment(payment, loan);
        if (counted) { plan.Rule.End = EndKind.AfterCount; plan.Rule.Count = 13; }
        else { plan.Rule.End = EndKind.OnDate; plan.Rule.EndDate = Today; }
        Assert.Equal(5000, Compute([payment, loan], [], [plan], [], []).LoanPrincipal);
    }

    /// <summary>An active goal may retain a contribution plan for history after its recurrence has ended.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 3000)]
    public void Contribution_recurrence_must_still_have_a_date(int shape, long expected)
    {
        var goal = Goal(); var plan = Contribution(goal, ContributionMethod.FixedAmount, 3000);
        if (shape == 0) { plan.Rule.End = EndKind.OnDate; plan.Rule.EndDate = Today.AddDays(-1); }
        if (shape == 1) { plan.Rule.End = EndKind.AfterCount; plan.Rule.Count = 2; }
        if (shape == 2) { plan.Rule.End = EndKind.OnDate; plan.Rule.EndDate = Today; }
        var before = JsonSerializer.Serialize(plan);
        Assert.Equal(expected, Compute([Payment()], [], [], [goal], [plan]).OtherGoals);
        Assert.Equal(before, JsonSerializer.Serialize(plan));
    }

    /// <summary>A spending cut is already per financial month; reminder cadence cannot multiply or divide it.</summary>
    [Theory]
    [InlineData(Frequency.Weekly, 1)]
    [InlineData(Frequency.Yearly, 1)]
    [InlineData(Frequency.Monthly, 2)]
    public void Spending_cut_keeps_its_monthly_unit(Frequency frequency, int interval)
    {
        var goal = Goal(); var plan = Contribution(goal, ContributionMethod.SpendingCut, 12000); plan.Rule.Frequency = frequency; plan.Rule.Interval = interval;
        Assert.Equal(12000, Compute([Payment()], [], [], [goal], [plan]).OtherGoals); Assert.Equal(12000, plan.Amount);
    }

    /// <summary>Capacity uses the same nonnegative income-share suggestion as the original contribution preview.</summary>
    [Theory]
    [InlineData(-10000, 0)]
    [InlineData(0, 0)]
    [InlineData(10000, 1000)]
    public void Income_share_does_not_turn_a_deficit_into_negative_commitments(long monthlyIncome, long expected)
    {
        var account = Payment(); var goal = Goal(); var plan = Contribution(goal, ContributionMethod.ShareOfIncome, 0); plan.Percent = 10;
        var original = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 30000, Date = new DateOnly(2026, 6, 1) };
        var entries = new List<LedgerEntry> { original };
        if (monthlyIncome != 0) foreach (var month in new[] { 8, 9, 10 }) entries.Add(new LedgerEntry
        {
            AccountId = account.Id, Kind = monthlyIncome < 0 ? EntryKind.IncomeReversal : EntryKind.Income, Amount = Math.Abs(monthlyIncome),
            Date = new DateOnly(2026, month, 1), RefundOfId = monthlyIncome < 0 ? original.Id : null,
        });
        var before = JsonSerializer.Serialize(entries); var result = Compute([account], entries, [], [goal], [plan]);
        Assert.Equal(monthlyIncome, result.Income); Assert.Equal(expected, result.OtherGoals); Assert.Equal(monthlyIncome - expected, result.Amount);
        Assert.Equal(before, JsonSerializer.Serialize(entries));
    }

    /// <summary>Fixed contributions retain original per-date annualization and minor-unit rounding.</summary>
    [Theory]
    [InlineData(Frequency.Weekly, 1, 5218)]
    [InlineData(Frequency.Yearly, 1, 100)]
    [InlineData(Frequency.Monthly, 2, 600)]
    public void Fixed_amount_cadence_still_scales_per_date(Frequency frequency, int interval, long expected)
    {
        var goal = Goal(); var plan = Contribution(goal, ContributionMethod.FixedAmount, 1200); plan.Rule.Frequency = frequency; plan.Rule.Interval = interval;
        Assert.Equal(expected, Compute([Payment()], [], [], [goal], [plan]).OtherGoals);
    }

    /// <summary>Inactive, quantity and explicitly excepted goals keep their original capacity exclusions.</summary>
    [Fact]
    public void Original_goal_scope_exclusions_remain()
    {
        var account = Payment(); var paused = Goal(); paused.State = GoalState.Paused; var quantity = Goal(); quantity.Type = GoalType.HoldingQuantity; var excepted = Goal();
        var plans = new[] { paused, quantity, excepted }.Select(g => Contribution(g, ContributionMethod.FixedAmount, 3000)).ToArray();
        var result = CapacityCalculator.Compute([account], [], [], [], [paused, quantity, excepted], plans, "EUR", Today, PeriodCalendar.Gregorian, exceptGoalId: excepted.Id);
        Assert.Equal(0, result.OtherGoals);
    }

    private static CapacityResult Compute(IReadOnlyCollection<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, IReadOnlyCollection<Schedule> schedules, IReadOnlyCollection<Goal> goals, IReadOnlyCollection<ContributionPlan> plans) =>
        CapacityCalculator.Compute(accounts, entries, schedules, [], goals, plans, "EUR", Today, PeriodCalendar.Gregorian);
    private static Account Payment() => new() { Name = "Current installment source", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningDate = Today.AddYears(-3), OpeningBalance = 1000000, UsableForPayments = true, IncludeInTotals = true };
    private static Account Loan() => new() { Name = "Current loan", CurrencyCode = "EUR", Type = AccountType.Loan, OpeningDate = Today.AddYears(-3), OpeningBalance = -100000, InterestRate = 12, UsableForPayments = false, IncludeInTotals = true };
    private static Schedule Installment(Account payment, Account loan) => new() { Name = "Current installment", Kind = EntryKind.Transfer, AccountId = payment.Id, ToAccountId = loan.Id, Amount = 6000, Rule = Monthly() };
    private static Goal Goal() => new() { Name = "Current capacity goal", CurrencyCode = "EUR", TargetAmount = 100000 };
    private static ContributionPlan Contribution(Goal goal, ContributionMethod method, long amount) => new() { GoalId = goal.Id, Method = method, Amount = amount, Rule = Monthly() };
    private static RecurrenceRule Monthly() => new() { Start = Today.AddYears(-1), Frequency = Frequency.Monthly };
}
