using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-648: original app-lock setting uses an explicit logical device outcome, never native authentication acceptance.</summary>
public sealed class SettingsLockBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class PendingDevice : IDeviceAuthenticator
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<AuthenticationOutcome> Answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Calls;
        public Task<bool> IsAvailableAsync() => Task.FromResult(true);
        public Task<AuthenticationOutcome> AuthenticateAsync(string reason) { Calls++; Entered.TrySetResult(); return Answer.Task.WaitAsync(Ct); }
    }
    /// <summary>A retired root never starts the original device-authentication request.</summary>
    [Fact]
    public async Task Already_retired_settings_owner_does_not_request_device_authentication()
    {
        using var f = new FlowFixture(); var auth = new PendingDevice(); var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new(new PinMemory(), f.Time), new LockPlatform());
        await service.StartAsync(); var review = await f.Store.ReadSettingsWorkSnapshotAsync(PeriodCalendar.Gregorian, cancellationToken: Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEnabledAsync(true, review, () => false, Ct));
        Assert.Equal(0, auth.Calls); Assert.False((await f.Store.GetSettingsAsync(Ct)).AppLockEnabled);
    }
    /// <summary>A later successful outcome cannot authorize another owner or settings replaced during the original wait.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pending_original_device_setting_refuses_lost_owner_or_changed_actual_source(bool changedSource)
    {
        using var f = new FlowFixture(); var auth = new PendingDevice(); var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new(new PinMemory(), f.Time), new LockPlatform());
        await service.StartAsync(); var review = await f.Store.ReadSettingsWorkSnapshotAsync(PeriodCalendar.Gregorian, cancellationToken: Ct); var owner = true;
        var pending = service.SetEnabledAsync(true, review, () => owner, Ct); await auth.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        if (changedSource) await f.Store.UpdateSettingsAsync(settings => settings.MonthStartDay = 17, Ct); else owner = false;
        auth.Answer.SetResult(AuthenticationOutcome.Success); await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal(1, auth.Calls); var current = await f.Store.GetSettingsAsync(Ct); Assert.False(current.AppLockEnabled);
        Assert.Equal(changedSource ? 17 : review.Settings.MonthStartDay, current.MonthStartDay); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
