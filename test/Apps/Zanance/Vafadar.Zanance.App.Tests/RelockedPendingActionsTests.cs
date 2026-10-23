namespace Vafadar.Zanance.App.Tests;

/// <summary>Original pending destinations must retain access ordering after a native background return.</summary>
public sealed class RelockedPendingActionsTests
{
    [Fact, Trait("AT", "AT-80")]
    public async Task New_unlocked_input_does_not_overtake_destinations_waiting_for_native_navigation()
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<int>();
        await setup.Lock.RunWhenUnlockedAsync(async () => { order.Add(1); entered.SetResult(); await release.Task; });
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(2); return Task.CompletedTask; });
        var unlocking = setup.Lock.UnlockedAsync();
        try
        {
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
            Assert.Equal([1], order);
        }
        finally { release.TrySetResult(); }
        await unlocking;
        Assert.Equal([1, 2, 3], order);
    }

    [Theory, Trait("AT", "AT-80")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Relocking_during_the_first_destination_retains_remaining_actions_before_new_covered_input(bool windowAvailable)
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var order = new List<int>();
        await setup.Lock.RunWhenUnlockedAsync(async () =>
        {
            order.Add(1);
            setup.Host.Available = windowAvailable;
            await setup.Lock.SleepAsync();
            await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
        });
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(2); return Task.CompletedTask; });

        await setup.Lock.UnlockedAsync();

        Assert.True(setup.Lock.IsLocked);
        Assert.Equal([1], order);
        setup.Host.Available = true;
        await setup.Lock.ResumeAsync();
        await setup.Lock.UnlockedAsync();
        Assert.Equal([1, 2, 3], order);
        Assert.False(setup.Lock.IsLocked);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
