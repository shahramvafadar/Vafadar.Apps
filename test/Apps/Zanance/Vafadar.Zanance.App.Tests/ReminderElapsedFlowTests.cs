using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original SQLite coordinator carries the accepted elapsed anchor and original day through normal refresh.</summary>
public sealed class ReminderElapsedFlowTests
{
    [Fact]
    public async Task Normal_source_refresh_does_not_reextend_the_hour_after_real_clock_family_correction()
    {
        await using var f = new ProfileReminderFlowTests.Fixture();
        var boot=7; long elapsed=100000;
        f.Native.HourCapture=()=>new(boot,elapsed+ReminderElapsedDeadline.HourMilliseconds);
        f.Native.HourResolve=(anchor,fallback)=>anchor.Resolve(boot,elapsed,f.Clock.GetUtcNow(),fallback);
        await f.SeedAsync(); f.Data.Enable(); await f.Service.RefreshAsync();
        var original=Assert.Single(f.Native.Pending); var before=await f.RowsAsync(); var issued=f.Clock.Now;
        await f.Service.SnoozeAsync(new(original,SnoozeChoice.OneHour));
        var accepted=Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        Assert.Equal(new ReminderElapsedDeadline(7,3700000),accepted.ElapsedDeadline);
        var expiry=accepted.DeliveryExpiresAt; Assert.NotNull(expiry);
        elapsed+=120000; f.Clock.Now=issued.AddMinutes(-18);
        await f.Service.RefreshAsync();
        var pending=Assert.Single(f.Native.Pending,n=>n.Id==accepted.Id);
        Assert.Equal(f.Clock.Now.AddMinutes(58),pending.NotifyInstant);
        Assert.Equal(expiry,pending.DeliveryExpiresAt);
        Assert.Equal(accepted.ElapsedDeadline,pending.ElapsedDeadline);
        Assert.Equal(1,f.Native.HourCaptures);
        await f.Service.RefreshAsync();
        Assert.Equal(pending,Assert.Single(f.Native.Pending,n=>n.Id==accepted.Id));
        Assert.Equal(before,await f.RowsAsync()); Assert.Empty(f.Native.Shown);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expired_elapsed_interval_or_original_day_cannot_resurrect_a_snooze(bool originalDay)
    {
        await using var f=new ProfileReminderFlowTests.Fixture(); long elapsed=100000;
        f.Native.HourCapture=()=>new(7,elapsed+ReminderElapsedDeadline.HourMilliseconds);
        f.Native.HourResolve=(anchor,fallback)=>anchor.Resolve(7,elapsed,f.Clock.GetUtcNow(),fallback);
        await f.SeedAsync();f.Data.Enable();await f.Service.RefreshAsync();var original=Assert.Single(f.Native.Pending);
        var before=await f.RowsAsync();await f.Service.SnoozeAsync(new(original,SnoozeChoice.OneHour));
        var saved=Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        if(originalDay){elapsed+=120000;f.Clock.Now=new(2026,10,10,23,50,0,TimeSpan.Zero);}
        else{elapsed+=ReminderElapsedDeadline.HourMilliseconds+1;f.Clock.Now=f.Clock.Now.AddHours(-1);}
        await f.Service.RefreshAsync();
        Assert.DoesNotContain(f.Native.Pending,n=>n.Id==saved.Id);
        Assert.Empty(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        Assert.Equal(before,await f.RowsAsync());Assert.Empty(f.Native.Shown);
    }

    [Fact]
    public async Task A_new_boot_does_not_invent_a_replacement_elapsed_origin()
    {
        await using var f=new ProfileReminderFlowTests.Fixture();var boot=7;long elapsed=100000;
        f.Native.HourCapture=()=>new(boot,elapsed+ReminderElapsedDeadline.HourMilliseconds);
        f.Native.HourResolve=(anchor,fallback)=>anchor.Resolve(boot,elapsed,f.Clock.GetUtcNow(),fallback);
        await f.SeedAsync();f.Data.Enable();await f.Service.RefreshAsync();var original=Assert.Single(f.Native.Pending);
        await f.Service.SnoozeAsync(new(original,SnoozeChoice.OneHour));
        var saved=Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));var before=await f.RowsAsync();
        boot=8;elapsed=0;f.Clock.Now=f.Clock.Now.AddMinutes(20);await f.Service.RefreshAsync();
        var current=Assert.Single(f.Native.Pending,n=>n.Id==saved.Id);
        Assert.Equal(saved.NotifyInstant,current.NotifyInstant);Assert.Equal(saved.ElapsedDeadline,current.ElapsedDeadline);
        Assert.Equal(1,f.Native.HourCaptures);Assert.Equal(before,await f.RowsAsync());
    }

    [Fact]
    public async Task Historical_hour_without_anchor_is_not_silently_recreated_as_a_new_hour()
    {
        await using var f=new ProfileReminderFlowTests.Fixture();
        await f.SeedAsync();f.Data.Enable();await f.Service.RefreshAsync();var original=Assert.Single(f.Native.Pending);
        await f.Service.SnoozeAsync(new(original,SnoozeChoice.OneHour));
        var saved=Assert.Single(ReminderSnoozes.Deserialize(f.Data.Preferences.Get(ReminderSnoozes.PreferenceKey)));
        Assert.Null(saved.ElapsedDeadline);f.Native.HourCapture=()=>new(9,3700000);
        f.Clock.Now=f.Clock.Now.AddMinutes(-20);await f.Service.RefreshAsync();
        var current=Assert.Single(f.Native.Pending,n=>n.Id==saved.Id);
        Assert.Equal(saved.NotifyInstant,current.NotifyInstant);Assert.Null(current.ElapsedDeadline);Assert.Equal(1,f.Native.HourCaptures);
    }
}
