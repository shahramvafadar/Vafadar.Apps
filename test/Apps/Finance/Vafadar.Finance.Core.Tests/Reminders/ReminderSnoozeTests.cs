using Vafadar.Finance.Core.Reminders;

namespace Vafadar.Finance.Core.Tests.Reminders;

public sealed class ReminderSnoozeTests
{
    private static readonly DateTime Now = new(2027, 3, 12, 9, 0, 0, DateTimeKind.Local);

    [Fact]
    [Trait("AT", "AT-35")]
    public void Snoozing_repeats_the_reminder_later_with_its_own_id()
    {
        const int id = 0x0000_0105;

        Assert.Equal(Now.AddHours(1), ReminderSnoozes.NotifyAt(SnoozeChoice.OneHour, Now));
        Assert.Equal(Now.AddDays(1), ReminderSnoozes.NotifyAt(SnoozeChoice.Tomorrow, Now));
        Assert.NotEqual(id, ReminderSnoozes.IdFor(id));
        Assert.NotEqual(ReminderPlanner.DueDateId(id), ReminderSnoozes.IdFor(id));
        Assert.Equal(ReminderSnoozes.IdFor(id), ReminderSnoozes.IdFor(ReminderSnoozes.IdFor(id)));
        Assert.True(ReminderSnoozes.IdFor(int.MaxValue) > 0);
    }

    [Fact]
    [Trait("AT", "AT-36")]
    public void Snoozes_end_when_they_have_fired_or_the_occurrence_is_no_longer_open()
    {
        var open = new SnoozedReminder(3, "occurrence|a|2027-03-15", "Finance", "Payment due", Now.AddHours(1));
        var settled = new SnoozedReminder(5, "occurrence|b|2027-03-15", "Finance", "Payment due", Now.AddHours(1));
        var fired = new SnoozedReminder(7, "occurrence|a|2027-03-15", "Finance", "Payment due", Now.AddMinutes(-1));

        var kept = ReminderSnoozes.Keep([open, settled, fired], Now, link => !link.Contains("|b|", StringComparison.Ordinal));

        Assert.Equal([open], kept);
    }

    [Fact]
    public void A_second_snooze_replaces_the_first_and_damaged_data_is_ignored()
    {
        var first = new SnoozedReminder(3, "plans", "Finance", "2 payments due", Now.AddHours(1));
        var again = first with { NotifyAt = Now.AddDays(1) };

        var snoozes = ReminderSnoozes.Add(ReminderSnoozes.Add([], first), again);

        Assert.Equal([again], snoozes);
        Assert.Equal(snoozes, ReminderSnoozes.Deserialize(ReminderSnoozes.Serialize(snoozes)));
        Assert.Empty(ReminderSnoozes.Deserialize("{not json"));
        Assert.Empty(ReminderSnoozes.Deserialize(null));
    }
}
