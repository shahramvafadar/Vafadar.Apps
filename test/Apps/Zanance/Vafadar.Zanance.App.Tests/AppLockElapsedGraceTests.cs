using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original30second background grace under independently changing UTC and elapsed clocks.</summary>
public sealed class AppLockElapsedGraceTests
{
    [Theory]
    [InlineData(31, 1, true)]
    [InlineData(3600, 1, true)]
    [InlineData(30, 1, true)]
    [InlineData(31, 29, true)]
    [InlineData(29, 29, false)]
    [InlineData(1, -1, true)]
    public async Task Wall_clock_correction_cannot_extend_the_original_background_grace(int actualSeconds, int wallSeconds, bool expectedLocked)
    {
        using var f = new FlowFixture(); await AppLockTests.EnableDeviceAsync(f);
        var clock = new IndependentClock(); var host = new LockPlatform(); var auth = new AuthPlatform();
        var access = new AppLockService(auth, f.Store, f.Translator, clock, new PinLock(new PinMemory(), clock), host);
        await access.StartAsync(); await access.UnlockedAsync(); await access.SleepAsync();
        var cover = host.Covers.Last(); var originalUtc = clock.Now;
        var opened = false; await access.RunWhenUnlockedAsync(() => { opened = true; return Task.CompletedTask; });
        clock.Stamp += TimeSpan.FromSeconds(actualSeconds).Ticks; clock.Now = originalUtc.AddSeconds(wallSeconds);
        await access.ResumeAsync();
        Assert.Equal(expectedLocked, access.IsLocked); Assert.Equal(!expectedLocked, opened);
        Assert.Equal(expectedLocked ? 1 : 0, cover.Prompts); Assert.Equal(expectedLocked ? 0 : 1, cover.Closed);
        Assert.Equal(0, auth.Calls);
        Assert.True((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).AppLockEnabled);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(29999L, false)]
    [InlineData(30000L, true)]
    [InlineData(3600000L, true)]
    [InlineData(-1L, true)]
    [InlineData(null, true)]
    public async Task Android_sleep_inclusive_elapsed_clock_cannot_be_hidden_by_paused_stopwatch_or_lost_counter(long? actualMilliseconds, bool expectedLocked)
    {
        using var f = new FlowFixture(); await AppLockTests.EnableDeviceAsync(f);
        var clock = new IndependentClock(); var host = new LockPlatform { BackgroundElapsedMilliseconds = 100000 };
        var auth = new AuthPlatform(); var access = new AppLockService(auth, f.Store, f.Translator, clock, new PinLock(new PinMemory(), clock), host);
        await access.StartAsync(); await access.UnlockedAsync(); await access.SleepAsync();
        clock.Stamp += TimeSpan.TicksPerSecond; clock.Now = clock.Now.AddSeconds(1);
        host.BackgroundElapsedMilliseconds = actualMilliseconds is { } value ? 100000 + value : null;
        await access.ResumeAsync();
        Assert.Equal(expectedLocked, access.IsLocked); Assert.Equal(expectedLocked ? 1 : 0, host.Covers.Last().Prompts);
        Assert.Equal(0, auth.Calls); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Independent clocks never change machine time or create an authentication receipt.</summary>
    private sealed class IndependentClock : TimeProvider
    {
        internal DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        internal long Stamp = TimeSpan.FromDays(2).Ticks;
        public override DateTimeOffset GetUtcNow() => Now;
        public override long GetTimestamp() => Stamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    }
}
