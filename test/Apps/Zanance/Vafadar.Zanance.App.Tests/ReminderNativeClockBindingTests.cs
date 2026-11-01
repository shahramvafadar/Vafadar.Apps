using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exact accepted request identity refuses stale/missing clock metadata rather than inferring from source preferences.</summary>
public sealed class ReminderNativeClockBindingTests
{
    private static readonly DateTimeOffset Scheduled = new(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Exact_accepted_binding_round_trips_independently_of_mutable_snooze_cache(bool local)
    {
        var original = new ReminderNativeClockBinding(7, Scheduled, local);
        Assert.Equal(original, ReminderNativeClockBinding.Match(7, original.Encode(), Scheduled));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1:2")]
    [InlineData("1:2:3:4")]
    [InlineData("bad:2:1")]
    [InlineData("1:bad:1")]
    public void Unbound_or_damaged_request_is_not_reinterpreted(string? data) => Assert.Null(ReminderNativeClockBinding.Match(7, data, Scheduled));

    [Fact]
    public void Same_instant_with_different_wall_or_offset_is_not_the_accepted_request()
    {
        var binding = new ReminderNativeClockBinding(7, Scheduled, true);
        var different = Scheduled.ToOffset(TimeSpan.FromHours(9));
        Assert.Equal(Scheduled, different);
        Assert.Null(ReminderNativeClockBinding.Match(7, binding.Encode(), different));
        Assert.Null(ReminderNativeClockBinding.Match(7, binding.Encode(), Scheduled.AddTicks(1)));
    }

    [Fact]
    public void Invalid_kind_aliases_and_oversized_value_refuse()
    {
        var good = new ReminderNativeClockBinding(7, Scheduled, true).Encode();
        Assert.Null(ReminderNativeClockBinding.Match(7, good[..^1]+"2", Scheduled));
        Assert.Null(ReminderNativeClockBinding.Match(7, "0"+good, Scheduled));
        Assert.Null(ReminderNativeClockBinding.Match(7, new string('1',65), Scheduled));
    }
}
