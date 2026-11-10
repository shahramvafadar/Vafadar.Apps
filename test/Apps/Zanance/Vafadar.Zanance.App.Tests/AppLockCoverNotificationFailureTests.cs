using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Access observers cannot prevent an owned native cover or transfer it to retired startup.</summary>
public sealed class AppLockCoverNotificationFailureTests
{
    /// <summary>Callback failures remain visible to the caller after the current cover is secured.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Notification_failure_does_not_prevent_the_current_cover(int scenario)
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var host = new LockPlatform(); var auth = new AuthPlatform();
        var service = new AppLockService(auth, fixture.Store, fixture.Translator,
            fixture.Time, new PinLock(new PinMemory(), fixture.Time), host);
        var current = true; var opened = 0;
        if (scenario is 1 or 2 or 3)
        {
            host.Available = scenario != 2;
            await service.StartAsync();
            if (scenario == 1) await service.UnlockedAsync();
            host.Available = true;
        }

        var marker = new IOException("Fictitious access observer failure.");
        using var cancelled = new CancellationTokenSource();
        using var registration = cancelled.Token.Register(() => throw marker);
        service.AccessChanged += (_, _) =>
        {
            if (scenario == 4) current = false;
            if (scenario == 2) cancelled.Cancel();
            else throw marker;
        };
        Func<Task> request = scenario switch
        {
            1 or 3 => service.SleepAsync,
            2 => service.ResumeAsync,
            _ => () => service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken),
        };
        if (scenario == 4)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(request);
        else if (scenario == 2)
        {
            var failure = await Assert.ThrowsAsync<AggregateException>(request);
            Assert.Same(marker, Assert.Single(failure.InnerExceptions));
        }
        else Assert.Same(marker, await Assert.ThrowsAsync<IOException>(request));

        Assert.True(service.IsLocked);
        if (scenario == 4) Assert.Empty(host.Covers);
        else Assert.True(host.Covers.Last().IsOnScreen);
        Assert.Equal(scenario == 1 ? 2 : scenario == 4 ? 0 : 1, host.Covers.Count);
        await service.RunWhenUnlockedAsync(() => { opened++; return Task.CompletedTask; });
        Assert.Equal(0, opened); Assert.Equal(0, auth.Calls);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
