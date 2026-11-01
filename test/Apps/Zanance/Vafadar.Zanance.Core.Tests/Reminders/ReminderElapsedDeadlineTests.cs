using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reminders;

/// <summary>Actual manual-clock failure family: same-boot intervals survive UTC correction without inventing reboot timing.</summary>
public sealed class ReminderElapsedDeadlineTests
{
    [Theory]
    [InlineData(-20)]
    [InlineData(20)]
    public void Wall_correction_does_not_extend_or_shorten_the_remaining_elapsed_interval(int correction)
    {
        var anchor = new ReminderElapsedDeadline(7, 3_700_000);
        var utc = new DateTimeOffset(2026, 11, 1, 8, 2, 0, TimeSpan.Zero).AddMinutes(correction);
        var actual = anchor.Resolve(7, 220_000, utc, utc.AddHours(3));
        Assert.Equal(utc.AddMinutes(58), actual);
        Assert.Equal(3_700_000L, 220_000 + (long)(actual - utc).TotalMilliseconds);
    }

    [Theory]
    [InlineData(8, 100000)]
    [InlineData(-1, 100000)]
    [InlineData(7, -1)]
    [InlineData(7, 0)]
    public void Other_boot_unknown_clock_and_impossible_future_anchor_keep_original_fallback(int boot, long elapsed)
    {
        var anchor = new ReminderElapsedDeadline(7, 3_700_000);
        var now = DateTimeOffset.UnixEpoch; var fallback = now.AddMinutes(40);
        Assert.False(anchor.IsCurrent(boot, elapsed));
        Assert.Equal(fallback, anchor.Resolve(boot, elapsed, now, fallback));
    }

    [Fact]
    public void Expired_same_boot_anchor_does_not_become_future_after_a_wall_clock_rewind()
    {
        var now = new DateTimeOffset(2026, 11, 1, 6, 0, 0, TimeSpan.Zero);
        var anchor = new ReminderElapsedDeadline(7, 100000);
        Assert.True(anchor.Resolve(7, 3_800_000, now, now.AddHours(3)) < now);
    }

    [Theory]
    [InlineData(-1, 100000)]
    [InlineData(7, -1)]
    public void Damaged_stored_origin_does_not_gain_a_new_boot_or_hour(int boot, long target)
    {
        var now = DateTimeOffset.UnixEpoch; var fallback = now.AddHours(1);
        Assert.Equal(fallback, new ReminderElapsedDeadline(boot,target).Resolve(7,1,now,fallback));
    }
}
