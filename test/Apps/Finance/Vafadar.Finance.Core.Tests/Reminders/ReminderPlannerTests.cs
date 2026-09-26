using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Reminders;

namespace Vafadar.Finance.Core.Tests.Reminders;

public sealed class ReminderPlannerTests
{
    private static readonly DateTime Now = new(2027, 3, 10, 12, 0, 0, DateTimeKind.Local);

    [Fact]
    public void Reminders_fire_days_before_the_due_date_at_the_chosen_time()
    {
        var rent = Plan(new DateOnly(2027, 3, 15));

        var reminder = ReminderPlanner.Plan([rent], [], Now)[0];

        Assert.Equal(new DateTime(2027, 3, 12, 9, 0, 0), reminder.NotifyAt);
        Assert.Equal(new DateOnly(2027, 3, 15), reminder.Occurrence.DueDate);
    }

    [Fact]
    [Trait("AT", "AT-36")]
    public void Settled_or_skipped_occurrences_get_no_reminder()
    {
        var rent = Plan(new DateOnly(2027, 3, 15));
        var settled = new OccurrenceState { ScheduleId = rent.Id, OriginalDate = new DateOnly(2027, 3, 15), Status = OccurrenceStatus.Settled };
        var skipped = new OccurrenceState { ScheduleId = rent.Id, OriginalDate = new DateOnly(2027, 4, 15), Status = OccurrenceStatus.Skipped };

        var reminders = ReminderPlanner.Plan([rent], [settled, skipped], Now);

        Assert.DoesNotContain(reminders, r => r.Occurrence.OriginalDate == new DateOnly(2027, 3, 15));
        Assert.DoesNotContain(reminders, r => r.Occurrence.OriginalDate == new DateOnly(2027, 4, 15));
    }

    [Fact]
    [Trait("AT", "AT-35")]
    public void A_moved_due_date_moves_the_reminder_but_keeps_its_id()
    {
        var rent = Plan(new DateOnly(2027, 3, 15));
        var original = ReminderPlanner.Plan([rent], [], Now)[0];
        var moved = new OccurrenceState { ScheduleId = rent.Id, OriginalDate = new DateOnly(2027, 3, 15), DueDate = new DateOnly(2027, 3, 20) };

        var reminder = ReminderPlanner.Plan([rent], [moved], Now)[0];

        Assert.Equal(original.Id, reminder.Id);
        Assert.Equal(new DateTime(2027, 3, 17, 9, 0, 0), reminder.NotifyAt);
    }

    [Fact]
    [Trait("AT", "AT-33")]
    public void Missed_reminders_are_not_sent_later_and_the_number_pending_is_limited()
    {
        var daily = Plan(new DateOnly(2027, 1, 1));
        daily.Rule = new RecurrenceRule { Frequency = Frequency.Daily, Start = new DateOnly(2027, 1, 1) };

        var reminders = ReminderPlanner.Plan([daily], [], Now);

        Assert.All(reminders, r => Assert.True(r.NotifyAt > Now));
        Assert.Equal(ReminderPlanner.MaxPending, reminders.Count);
    }

    [Fact]
    public void Plans_without_reminders_or_not_active_are_ignored()
    {
        var silent = Plan(new DateOnly(2027, 3, 15));
        silent.ReminderEnabled = false;
        var paused = Plan(new DateOnly(2027, 3, 15));
        PlanActions.Pause(paused, new DateOnly(2027, 1, 1));

        Assert.Empty(ReminderPlanner.Plan([silent, paused], [], Now));
    }

    [Fact]
    public void Ids_are_stable_and_distinct_per_occurrence()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(ReminderPlanner.StableId(id, new DateOnly(2027, 3, 15)), ReminderPlanner.StableId(id, new DateOnly(2027, 3, 15)));
        Assert.NotEqual(ReminderPlanner.StableId(id, new DateOnly(2027, 3, 15)), ReminderPlanner.StableId(id, new DateOnly(2027, 4, 15)));
        Assert.True(ReminderPlanner.StableId(id, new DateOnly(2027, 3, 15)) > 0);
    }

    [Fact]
    public void An_optional_second_reminder_fires_on_the_due_date_with_its_own_id()
    {
        var rent = Plan(new DateOnly(2027, 3, 15));
        rent.ReminderOnDueDate = true;

        var march = ReminderPlanner.Plan([rent], [], Now).Where(r => r.Occurrence.DueDate.Month == 3).ToList();

        Assert.Equal([new DateTime(2027, 3, 12, 9, 0, 0), new DateTime(2027, 3, 15, 9, 0, 0)], march.Select(r => r.NotifyAt));
        Assert.NotEqual(march[0].Id, march[1].Id);
        Assert.True(march[1].Id > 0);

        rent.ReminderDaysBefore = 0;
        Assert.Single(ReminderPlanner.Plan([rent], [], Now), r => r.Occurrence.DueDate.Month == 3);
    }

    [Fact]
    public void Reminders_at_the_same_time_are_grouped_into_one_summary()
    {
        var rent = Plan(new DateOnly(2027, 3, 15));
        var phone = Plan(new DateOnly(2027, 3, 15));
        var gym = Plan(new DateOnly(2027, 3, 15));
        gym.ReminderTime = new TimeOnly(18, 0);

        var groups = ReminderPlanner.GroupByTime(ReminderPlanner.Plan([rent, phone, gym], [], Now).Where(r => r.Occurrence.DueDate.Month == 3));

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.Single(groups[1]);
    }

    private static Schedule Plan(DateOnly start) => new()
    {
        Name = "Rent",
        Kind = EntryKind.Expense,
        AccountId = Guid.CreateVersion7(),
        Amount = 90_000,
        Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = start },
        ReminderEnabled = true,
        ReminderDaysBefore = 3,
        ReminderTime = new TimeOnly(9, 0),
    };
}
