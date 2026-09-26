using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Reminders;

namespace Vafadar.Finance.Core.Tests.Reminders;

public sealed class ContractReminderTests
{
    private static readonly DateTime Now = new(2027, 3, 10, 12, 0, 0, DateTimeKind.Local);

    [Fact]
    public void A_cancellation_deadline_is_reminded_ahead_and_on_the_day_with_distinct_ids()
    {
        var streaming = Plan(deadline: new DateOnly(2027, 4, 30));

        var reminders = ContractReminderPlanner.Plan([streaming], Now);

        Assert.Equal([new DateTime(2027, 4, 16, 9, 0, 0), new DateTime(2027, 4, 30, 9, 0, 0)], reminders.Select(r => r.NotifyAt));
        Assert.All(reminders, r => Assert.Equal(ContractDateKind.Cancellation, r.Kind));
        Assert.NotEqual(reminders[0].Id, reminders[1].Id);
        Assert.All(reminders, r => Assert.True(r.Id > 0));
    }

    [Fact]
    public void Past_dates_ended_plans_and_dates_beyond_the_horizon_get_no_reminder()
    {
        var soon = Plan(deadline: new DateOnly(2027, 3, 15));
        var ended = Plan(deadline: new DateOnly(2027, 4, 30));
        ended.State = ScheduleState.Ended;
        var far = Plan(review: new DateOnly(2027, 9, 1));

        var reminders = ContractReminderPlanner.Plan([soon, ended, far], Now);

        // The lead reminder for 15 March was due on 1 March, so only the one on the day remains.
        Assert.Equal([new DateTime(2027, 3, 15, 9, 0, 0)], reminders.Select(r => r.NotifyAt));
    }

    [Fact]
    public void Upcoming_contract_dates_are_listed_for_the_next_30_days()
    {
        var gym = Plan(deadline: new DateOnly(2027, 3, 20), review: new DateOnly(2027, 5, 1));
        var phone = Plan(review: new DateOnly(2027, 4, 2));

        var upcoming = ContractReminderPlanner.Upcoming([gym, phone], DateOnly.FromDateTime(Now));

        Assert.Equal([new DateOnly(2027, 3, 20), new DateOnly(2027, 4, 2)], upcoming.Select(u => u.Date));
    }

    private static Schedule Plan(DateOnly? deadline = null, DateOnly? review = null) => new()
    {
        Name = "Subscription",
        Kind = EntryKind.Expense,
        AccountId = Guid.CreateVersion7(),
        Amount = 1_299,
        Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 1, 1) },
        ReminderTime = new TimeOnly(9, 0),
        CancellationDeadline = deadline,
        ReviewDate = review,
    };
}