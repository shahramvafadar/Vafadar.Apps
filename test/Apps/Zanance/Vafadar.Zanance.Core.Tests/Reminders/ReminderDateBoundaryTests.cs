using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reminders;

/// <summary>Checks that representable reminders survive the calendar endpoints.</summary>
public sealed class ReminderDateBoundaryTests
{
    [Fact]
    public void Expired_first_day_contract_does_not_drop_another_contracts_review()
    {
        var now = new DateTime(2027, 1, 15, 8, 0, 0);
        var obsolete = new Schedule { Name = "Archived contract date", CancellationDeadline = DateOnly.MinValue };
        var current = new Schedule { Name = "Current contract", ReviewDate = DateOnly.FromDateTime(now), ReminderTime = new TimeOnly(9, 0) };

        var review = Assert.Single(ContractReminderPlanner.Plan([obsolete, current], now));
        Assert.Same(current, review.Schedule);
        Assert.Equal(new DateTime(2027, 1, 15, 9, 0, 0), review.NotifyAt);
    }

    [Fact]
    public void First_day_plan_keeps_its_due_day_reminder_when_the_lead_date_is_unrepresentable()
    {
        var plan = new Schedule
        {
            Name = "First day plan",
            Rule = new RecurrenceRule { Start = DateOnly.MinValue, Frequency = Frequency.Once },
            ReminderEnabled = true, ReminderDaysBefore = 1, ReminderOnDueDate = true,
            ReminderTime = new TimeOnly(9, 0),
        };

        var reminder = Assert.Single(ReminderPlanner.Plan([plan], [], new DateTime(1, 1, 1, 8, 0, 0)));
        Assert.Equal(new DateTime(1, 1, 1, 9, 0, 0), reminder.NotifyAt);
        Assert.Equal(DateOnly.MinValue, reminder.Occurrence.DueDate);
        Assert.Equal(ReminderPlanner.DueDateId(ReminderPlanner.StableId(plan.Id, DateOnly.MinValue)), reminder.Id);
    }

    [Theory]
    [InlineData("plan")]
    [InlineData("contract")]
    [InlineData("goal")]
    public void Final_day_keeps_future_reminders(string kind)
    {
        var last = DateOnly.MaxValue;
        var now = last.ToDateTime(new TimeOnly(8, 0));
        var rule = new RecurrenceRule { Start = last, Frequency = Frequency.Once };
        var schedule = new Schedule
        {
            Name = "Final day plan", Rule = rule, ReviewDate = last, ReminderEnabled = true, ReminderDaysBefore = 0,
            ReminderTime = new TimeOnly(9, 0),
        };
        var goal = new Goal { Name = "Final day goal", CurrencyCode = "EUR", TargetAmount = 100 };
        var goalPlan = new ContributionPlan { GoalId = goal.Id, ReminderEnabled = true, Rule = rule };
        var progress = new GoalProgress(goal, 0, 0, 0, null, null, null, GoalNotice.None);
        var expected = last.ToDateTime(new TimeOnly(9, 0));

        var notifyAt = kind switch
        {
            "plan" => Assert.Single(ReminderPlanner.Plan([schedule], [], now)).NotifyAt,
            "contract" => Assert.Single(ContractReminderPlanner.Plan([schedule], now)).NotifyAt,
            "goal" => Assert.Single(GoalReminderPlanner.Plan([progress], [goalPlan], now)).NotifyAt,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        Assert.Equal(expected, notifyAt);
    }

    [Theory]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void Final_partial_month_keeps_the_last_real_review_boundary(PeriodCalendar calendar)
    {
        var (year, month) = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var boundary = PeriodMath.ToDate(year, month, 1, calendar);
        var previous = PeriodMath.Previous(year, month);
        var firstData = PeriodMath.ToDate(previous.Year, previous.Month, 1, calendar);
        var now = boundary.ToDateTime(new TimeOnly(8, 0));

        var reminder = Assert.Single(ReviewReminderPlanner.Plan(true, null, now, calendar, 1, firstData));
        Assert.Equal(boundary.ToDateTime(new TimeOnly(9, 0)), reminder.NotifyAt);
        Assert.Equal(firstData, reminder.PeriodFirst);
    }
}
