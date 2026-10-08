using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reminders;

[Trait("AT", "AT-77")]
public sealed class GoalReminderTests
{
    private static readonly DateTime Now = new(2027, 1, 30, 8, 0, 0, DateTimeKind.Local);
    private static Goal Goal() => new() { Name = "Fictitious savings", CurrencyCode = "EUR", TargetAmount = 100_00 };
    private static ContributionPlan Plan(Goal goal, Frequency frequency = Frequency.Monthly) => new()
    {
        GoalId = goal.Id, ReminderEnabled = true,
        Rule = new RecurrenceRule { Frequency = frequency, Start = new DateOnly(2027, 1, 31) },
    };
    private static GoalProgress Progress(Goal goal, long current = 0, GoalNotice notice = GoalNotice.None) =>
        new(goal, current, 0, 0, null, null, null, notice);

    [Fact]
    public void Monthly_dates_keep_the_saved_anchor_and_last_valid_day()
    {
        var goal = Goal();
        var dates = GoalReminderPlanner.Plan([Progress(goal)], [Plan(goal)], Now);
        Assert.Equal(new[] { new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31) }, dates.Select(r => r.Date));
        Assert.All(dates, r => Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(r.NotifyAt)));
    }

    [Fact]
    public void Two_week_rules_use_the_saved_anchor_and_end_count()
    {
        var goal = Goal(); var plan = Plan(goal, Frequency.Weekly);
        plan.Rule.Interval = 2; plan.Rule.End = EndKind.AfterCount; plan.Rule.Count = 2;
        Assert.Equal(new[] { new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 14) }, GoalReminderPlanner.Plan([Progress(goal)], [plan], Now).Select(r => r.Date));
    }

    [Fact]
    public void Persian_rule_dates_are_not_reinterpreted_in_the_display_calendar()
    {
        var goal = Goal(); var plan = Plan(goal); plan.Rule.Calendar = PeriodCalendar.Persian;
        var actual = GoalReminderPlanner.Plan([Progress(goal)], [plan], Now).Select(r => r.Date);
        Assert.Equal(ContributionSchedule.Dates(plan.Rule, DateOnly.FromDateTime(Now), DateOnly.FromDateTime(Now.AddDays(62))), actual);
        Assert.DoesNotContain(new DateOnly(2027, 2, 28), actual);
    }

    [Theory]
    [InlineData(GoalState.Paused)] [InlineData(GoalState.Completed)] [InlineData(GoalState.Archived)]
    public void Inactive_goals_are_silent(GoalState state)
    {
        var goal = Goal(); goal.State = state;
        Assert.Empty(GoalReminderPlanner.Plan([Progress(goal)], [Plan(goal)], Now));
    }

    [Theory]
    [InlineData(GoalNotice.AccountUnavailable)] [InlineData(GoalNotice.HoldingUnavailable)]
    public void Unavailable_goal_sources_are_silent(GoalNotice notice)
    {
        var goal = Goal();
        Assert.Empty(GoalReminderPlanner.Plan([Progress(goal, notice: notice)], [Plan(goal)], Now));
    }

    [Fact]
    public void Reached_goals_resume_reminders_after_a_withdrawal()
    {
        var goal = Goal(); var plan = Plan(goal);
        Assert.Empty(GoalReminderPlanner.Plan([Progress(goal, 100_00)], [plan], Now));
        Assert.NotEmpty(GoalReminderPlanner.Plan([Progress(goal, 99_00)], [plan], Now));
        Assert.Equal(GoalState.Active, goal.State);
    }

    [Fact]
    public void Opt_out_and_missing_plans_never_generate_reminders()
    {
        var goal = Goal(); var plan = Plan(goal); plan.ReminderEnabled = false;
        Assert.Empty(GoalReminderPlanner.Plan([Progress(goal)], [plan], Now));
        Assert.Empty(GoalReminderPlanner.Plan([Progress(goal)], [], Now));
    }

    [Fact]
    public void Rebuilds_drop_missed_dates_and_keep_future_ids_without_mutating_the_plan()
    {
        var goal = Goal(); var plan = Plan(goal); var later = new DateTime(2027, 1, 31, 9, 1, 0);
        var first = GoalReminderPlanner.Plan([Progress(goal)], [plan], Now);
        var refreshed = GoalReminderPlanner.Plan([Progress(goal)], [plan], later);
        Assert.DoesNotContain(refreshed, r => r.Date == new DateOnly(2027, 1, 31));
        Assert.Equal(first[1].Id, refreshed[0].Id);
        Assert.NotEqual(ReminderPlanner.StableId(goal.Id, first[0].Date), first[0].Id);
        Assert.True(first[0].Id > 0);
        Assert.Equal(new DateOnly(2027, 1, 31), plan.Rule.Start);
        Assert.Null(plan.Amount);
    }

    [Fact]
    public void Daily_rules_are_bounded_and_sorted_across_goals()
    {
        var a = Goal(); var b = Goal();
        var items = GoalReminderPlanner.Plan([Progress(b), Progress(a)], [Plan(a, Frequency.Daily), Plan(b, Frequency.Daily)], Now);
        Assert.Equal(ReminderPlanner.MaxPending, items.Count);
        Assert.Equal(items.OrderBy(r => r.NotifyAt).ThenBy(r => r.Goal.Id), items);
        Assert.Equal(items.Count, items.Select(r => r.Id).Distinct().Count());
    }
}
