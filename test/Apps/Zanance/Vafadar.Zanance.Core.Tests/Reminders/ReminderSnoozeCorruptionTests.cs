using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reminders;

/// <summary>Damaged local snooze elements cannot interrupt valid reminder reconstruction.</summary>
public sealed class ReminderSnoozeCorruptionTests
{
    [Theory]
    [InlineData("[null]")]
    [InlineData("[null,null]")]
    public void Null_cache_elements_do_not_become_snoozes(string json)
        => Assert.Empty(ReminderSnoozes.Deserialize(json));

    [Fact]
    public void Valid_snooze_between_null_elements_keeps_its_original_fields_and_expiry()
    {
        var now = new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Local);
        var original = new SnoozedReminder(17, "original-plan-link", "Original title", "Original body", now.AddHours(1));
        var serialized = ReminderSnoozes.Serialize([original]);
        var damaged = "[null," + serialized[1..^1] + ",null]";

        var kept = ReminderSnoozes.Keep(ReminderSnoozes.Deserialize(damaged), now, link => link == original.Link);

        Assert.Equal(original, Assert.Single(kept));
        Assert.Equal(serialized, ReminderSnoozes.Serialize(kept));
        Assert.Empty(ReminderSnoozes.Keep(kept, original.NotifyAt, _ => true));
    }
}
