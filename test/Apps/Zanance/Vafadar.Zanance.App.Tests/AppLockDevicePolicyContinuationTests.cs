using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Device-only confirmation cannot bypass an app PIN policy acquired while native work waits.</summary>
public sealed class AppLockDevicePolicyContinuationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public async Task Awaited_device_result_rechecks_current_app_PIN_policy(int entryPoint, int transition)
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var storage = new PinMemory();
        var host = new LockPlatform();
        var auth = new HeldAuthentication();
        var access = new AppLockService(auth, fixture.Store, fixture.Translator,
            fixture.Time, new PinLock(storage, fixture.Time), host);
        await access.StartAsync();
        var pending = entryPoint switch
        {
            0 => access.AuthenticateDeviceUnlockAsync(),
            1 => access.ConfirmAsync("Fictitious sensitive operation"),
            _ => access.ConfirmStartupRecoveryAsync("Fictitious covered recovery",
                cancellationToken: TestContext.Current.CancellationToken),
        };
        await auth.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);
        if (transition == 1)
        {
            Assert.Equal(PinOutcome.Success, (await access.Pin.SetAsync("", "0137")).Outcome);
        }
        else if (transition == 2)
        {
            storage.FailReads = true;
            Assert.False(await access.ConfirmStartupRecoveryAsync("Fictitious unavailable state",
                cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(access.PinUnavailable);
        }

        auth.Completion.SetResult(AuthenticationOutcome.Success);

        Assert.Equal(transition == 0, await pending);
        Assert.True(access.IsLocked);
        Assert.Equal(1, auth.Calls);
        Assert.Equal(0, host.PinPrompts);
        Assert.Equal(0, host.Covers.Single().Closed);
        if (transition == 1) Assert.True(access.PinEnabled);
        if (transition == 2) Assert.True(access.PinUnavailable);
        if (entryPoint == 0 && transition != 0)
            Assert.Equal(AuthenticationOutcome.Failed, access.LastDeviceUnlockOutcome);
    }

    private sealed class HeldAuthentication : IDeviceAuthenticator
    {
        internal int Calls;
        internal readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<AuthenticationOutcome> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<bool> IsAvailableAsync() => Task.FromResult(true);
        public Task<AuthenticationOutcome> AuthenticateAsync(string reason)
        {
            Calls++;
            Started.TrySetResult();
            return Completion.Task;
        }
    }
}
