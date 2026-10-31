namespace Vafadar.Zanance.App.Tests;

/// <summary>Original access-gate destinations keep arrival order while native navigation awaits.</summary>
public sealed class UnlockedDestinationOrderTests
{
    [Fact]
    public async Task Immediate_failure_reaches_its_caller_after_later_destinations_keep_their_order()
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<int>();
        var refusal = new InvalidOperationException("Fictitious first destination refusal.");
        var first = setup.Lock.RunWhenUnlockedAsync(async () =>
        {
            order.Add(1);
            await release.Task;
            throw refusal;
        });
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(2); throw new IOException("Fictitious queued refusal."); });
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
        release.TrySetResult();
        Assert.Same(refusal, await Assert.ThrowsAsync<InvalidOperationException>(() => first));
        Assert.Equal([1, 2, 3], order);
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(4); return Task.CompletedTask; });
        Assert.Equal([1, 2, 3, 4], order);
    }

    [Fact]
    public async Task A_destination_can_enqueue_its_followup_without_deadlocking_or_overtaking_itself()
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var order = new List<int>();
        await setup.Lock.RunWhenUnlockedAsync(async () =>
        {
            order.Add(1);
            await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
            order.Add(2);
        });
        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public async Task A_second_unlocked_destination_waits_for_the_first_actual_navigation()
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();
        var first = setup.Lock.RunWhenUnlockedAsync(async () =>
        {
            order.Add("first-started");
            await release.Task;
            order.Add("first-ended");
        });
        try
        {
            await setup.Lock.RunWhenUnlockedAsync(() =>
            {
                order.Add("second");
                return Task.CompletedTask;
            });
            Assert.Equal(["first-started"], order);
        }
        finally
        {
            release.TrySetResult();
            await first;
        }
        Assert.Equal(["first-started", "first-ended", "second"], order);
    }

    [Fact]
    public async Task Covering_during_the_first_unlocked_destination_retains_the_second_until_unlock()
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        await setup.Lock.UnlockedAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();
        var first = setup.Lock.RunWhenUnlockedAsync(async () =>
        {
            order.Add("first-started");
            await release.Task;
            order.Add("first-ended");
        });
        try
        {
            await setup.Lock.RunWhenUnlockedAsync(() =>
            {
                order.Add("second");
                return Task.CompletedTask;
            });
            await setup.Lock.SleepAsync();
        }
        finally
        {
            release.TrySetResult();
            await first;
        }
        Assert.True(setup.Lock.IsLocked);
        Assert.Equal(["first-started", "first-ended"], order);
        await setup.Lock.UnlockedAsync();
        Assert.Equal(["first-started", "first-ended", "second"], order);
    }
}
