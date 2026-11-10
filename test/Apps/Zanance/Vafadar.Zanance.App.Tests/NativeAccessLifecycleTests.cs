using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original lifetime and access services keep their native handoff despite synchronous or dispatched observer faults.</summary>
public sealed class NativeAccessLifecycleTests
{
    /// <summary>Only completed publication/dispatch permits foreground work; queued native cover errors stay contained.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Original_access_transition_survives_observer_failures(int scenario)
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        await AppLockTests.EnableDeviceAsync(fixture);
        await setup.Lock.ReloadAsync();
        var location = fixture.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var profiles = new ProfileService(location, fixture.Store, fixture.Preferences,
            new NativeLifetimeCancellationTests.UnusedRuntime(), fixture.Translator);
        using var lifetime = new PersonalSyncLifetime(setup.Lock, profiles, location);
        lifetime.Resume();
        var original = lifetime.CaptureAnalysis(false);
        using var cancellation = original.Cancellation.Register(() =>
        {
            if (scenario == 0) throw new IOException("Fictitious original cancellation failure.");
        });
        if (scenario == 1)
            setup.Lock.AccessChanged += (_, _) => throw new IOException("Fictitious dispatched observer failure.");
        var queued = new List<Func<Task>>();

        var published = NativeAccessLifecycle.Queue(lifetime.Suspend, action =>
        {
            if (scenario == 3) return false;
            queued.Add(action); return true;
        }, setup.Lock.SleepAsync);

        Assert.Equal(scenario is 1 or 2, published);
        Assert.True(original.Cancellation.IsCancellationRequested);
        Assert.ThrowsAny<OperationCanceledException>(original.Validate);
        Assert.ThrowsAny<OperationCanceledException>(() => lifetime.CaptureAnalysis(false));
        if (scenario == 3)
        {
            Assert.Empty(queued); Assert.Empty(setup.Host.Covers);
        }
        else
        {
            await Assert.Single(queued)();
            Assert.True(setup.Lock.IsLocked);
            Assert.True(Assert.Single(setup.Host.Covers).IsOnScreen);
            var opened = 0;
            await setup.Lock.RunWhenUnlockedAsync(() => { opened++; return Task.CompletedTask; });
            Assert.Equal(0, opened);
        }
        Assert.Equal(0, setup.Auth.Calls);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
