using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual access policy with real profile settings and PIN verification, behind explicit native ports.</summary>
public sealed class AppLockTests
{
    [Fact, Trait("AT", "AT-80")]
    public async Task Startup_defers_links_until_secure_state_is_read_and_keeps_their_order_after_a_failed_link()
    {
        using var f = new FlowFixture(); var setup = Create(f); var order = new List<int>();
        Assert.True(setup.Lock.IsLocked);
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(1); throw new InvalidOperationException("Fictitious navigation refusal"); });
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(2); return Task.CompletedTask; });
        Assert.Empty(order); await setup.Lock.StartAsync();
        Assert.Equal([1, 2], order); Assert.False(setup.Lock.IsLocked); Assert.Empty(setup.Host.Covers);
        await setup.Lock.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
        Assert.Equal([1, 2, 3], order);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Missing_window_never_grants_access_and_resume_retries_the_cover()
    {
        using var f = new FlowFixture(); await EnableDeviceAsync(f); var setup = Create(f); setup.Host.Available = false;
        var opened = false; await setup.Lock.StartAsync();
        await setup.Lock.RunWhenUnlockedAsync(() => { opened = true; return Task.CompletedTask; });
        Assert.True(setup.Lock.IsLocked); Assert.False(opened);
        setup.Host.Available = true; await setup.Lock.ResumeAsync();
        Assert.True(Assert.Single(setup.Host.Covers).IsOnScreen); Assert.False(opened);
        await setup.Lock.UnlockedAsync(); Assert.True(opened); Assert.False(setup.Lock.IsLocked);
    }

    [Theory, Trait("AT", "AT-80")]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(-1, true)]
    public async Task Only_a_nonnegative_short_background_interval_can_use_the_grace_period(int seconds, bool remainsLocked)
    {
        using var f = new FlowFixture(); await EnableDeviceAsync(f); var setup = Create(f);
        await setup.Lock.StartAsync(); await setup.Lock.UnlockedAsync(); await setup.Lock.SleepAsync();
        var cover = setup.Host.Covers.Last(); f.Time.Now = f.Time.Now.AddSeconds(seconds);
        await setup.Lock.ResumeAsync(); Assert.Equal(remainsLocked, setup.Lock.IsLocked);
        Assert.Equal(remainsLocked ? 1 : 0, cover.Prompts);
        Assert.Equal(remainsLocked ? 0 : 1, cover.Closed);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Already_locked_app_never_uses_grace_and_a_lost_cover_is_replaced()
    {
        using var f = new FlowFixture(); await EnableDeviceAsync(f); var setup = Create(f);
        await setup.Lock.StartAsync(); await setup.Lock.SleepAsync(); await setup.Lock.ResumeAsync();
        Assert.True(setup.Lock.IsLocked); Assert.Equal(1, setup.Host.Covers[0].Prompts);
        setup.Host.Covers[0].IsOnScreen = false; await setup.Lock.ResumeAsync();
        Assert.Equal(2, setup.Host.Covers.Count); Assert.True(setup.Host.Covers[1].IsOnScreen);
        Assert.True(setup.Lock.IsLocked);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Failed_cover_removal_keeps_access_and_pending_actions_locked()
    {
        using var f = new FlowFixture(); await EnableDeviceAsync(f); var setup = Create(f); await setup.Lock.StartAsync();
        var opened = false; await setup.Lock.RunWhenUnlockedAsync(() => { opened = true; return Task.CompletedTask; });
        setup.Host.Covers[0].RefuseClose = true;
        await Assert.ThrowsAsync<InvalidOperationException>(setup.Lock.UnlockedAsync);
        Assert.True(setup.Lock.IsLocked); Assert.False(opened);
        setup.Host.Covers[0].RefuseClose = false; await setup.Lock.UnlockedAsync(); Assert.True(opened);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Unreadable_PIN_state_fails_closed_even_without_a_device_lock_or_authentication_support()
    {
        using var f = new FlowFixture(); var setup = Create(f); setup.Storage.FailReads = true;
        setup.Auth.Outcome = AuthenticationOutcome.NotAvailable; await setup.Lock.StartAsync();
        Assert.True(setup.Lock.PinUnavailable); Assert.True(setup.Lock.IsEnabled); Assert.True(setup.Lock.IsLocked);
        Assert.False(await setup.Lock.ConfirmAsync("Fictitious export")); Assert.Equal(0, setup.Auth.Calls);
    }

    [Theory, Trait("AT", "AT-80")]
    [InlineData(AuthenticationOutcome.Failed)]
    [InlineData(AuthenticationOutcome.NotAvailable)]
    public async Task Refused_device_authentication_cannot_enable_a_setting_or_confirm_a_sensitive_operation(AuthenticationOutcome outcome)
    {
        using var f = new FlowFixture(); var setup = Create(f); setup.Auth.Outcome = outcome;
        await setup.Lock.StartAsync(); Assert.False(await setup.Lock.SetEnabledAsync(true));
        Assert.False((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).AppLockEnabled);
        await EnableDeviceAsync(f); await setup.Lock.ReloadAsync();
        Assert.False(await setup.Lock.ConfirmAsync("Fictitious backup"));
        Assert.True(setup.Lock.DeviceLockEnabled); Assert.Equal(2, setup.Auth.Calls);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task PIN_confirmation_uses_the_PIN_port_and_profile_reload_cannot_disable_a_device_wide_PIN()
    {
        using var f = new FlowFixture(); var setup = Create(f);
        Assert.Equal(PinOutcome.Success, (await setup.Lock.Pin.SetAsync("", "0137")).Outcome);
        await setup.Lock.StartAsync(); Assert.True(setup.Lock.PinEnabled); Assert.True(setup.Lock.IsLocked);
        setup.Host.PinAnswer = false; Assert.False(await setup.Lock.ConfirmAsync("Fictitious export"));
        setup.Host.PinAnswer = true; Assert.True(await setup.Lock.ConfirmAsync("Fictitious export"));
        Assert.Equal(2, setup.Host.PinPrompts); Assert.Equal(0, setup.Auth.Calls);
        await setup.Lock.ReloadAsync(); Assert.False(setup.Lock.DeviceLockEnabled); Assert.True(setup.Lock.PinEnabled);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Recovery_requires_both_successful_device_authentication_and_explicit_confirmation()
    {
        using var f = new FlowFixture(); var setup = Create(f); await setup.Lock.Pin.SetAsync("", "0137");
        await setup.Lock.StartAsync(); setup.Auth.Outcome = AuthenticationOutcome.NotAvailable;
        setup.Host.RecoveryAnswer = true; Assert.False(await setup.Lock.RecoverPinAsync()); Assert.Equal(0, setup.Host.RecoveryPrompts);
        setup.Auth.Outcome = AuthenticationOutcome.Success; setup.Host.RecoveryAnswer = false;
        Assert.False(await setup.Lock.RecoverPinAsync()); Assert.True(setup.Lock.PinEnabled);
        setup.Host.RecoveryAnswer = true; Assert.True(await setup.Lock.RecoverPinAsync());
        Assert.False(setup.Lock.PinEnabled); Assert.Null(setup.Storage.State); Assert.Equal(2, setup.Host.RecoveryPrompts);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Successful_device_setting_changes_are_persisted_and_do_not_create_ledger_entries()
    {
        using var f = new FlowFixture(); var setup = Create(f); await setup.Lock.StartAsync();
        Assert.True(await setup.Lock.SetEnabledAsync(true)); Assert.True((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).AppLockEnabled);
        Assert.False(await setup.Lock.SetEnabledAsync(false)); Assert.False((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).AppLockEnabled);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    internal static (AppLockService Lock, LockPlatform Host, AuthPlatform Auth, PinMemory Storage) Create(FlowFixture f)
    {
        var storage = new PinMemory(); var host = new LockPlatform(); var auth = new AuthPlatform();
        return (new(auth, f.Store, f.Translator, f.Time, new(storage, f.Time), host), host, auth, storage);
    }

    internal static async Task EnableDeviceAsync(FlowFixture f)
    {
        var settings = await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken); settings.AppLockEnabled = true; await f.Store.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);
    }
}

/// <summary>Isolated fictitious verifier storage, never connected to platform secure storage.</summary>
internal sealed class PinMemory : IPinStorage
{
    internal PinState? State;
    internal bool FailReads;
    public Task<PinState?> ReadAsync() => FailReads ? throw new IOException("Fictitious storage failure") : Task.FromResult(State);
    public Task WriteAsync(PinState? state) { State = state; return Task.CompletedTask; }
}

/// <summary>Explicit device authentication outcome with no platform fallback.</summary>
internal sealed class AuthPlatform : IDeviceAuthenticator
{
    internal AuthenticationOutcome Outcome = AuthenticationOutcome.Success;
    internal int Calls;
    public Task<bool> IsAvailableAsync() => Task.FromResult(Outcome != AuthenticationOutcome.NotAvailable);
    public Task<AuthenticationOutcome> AuthenticateAsync(string reason) { Calls++; return Task.FromResult(Outcome); }
}

/// <summary>Window and dialog lifetimes as native ports, rather than fake MAUI pages.</summary>
internal sealed class LockPlatform : IAppLockHost
{
    internal bool Available = true, PinAnswer, RecoveryAnswer;
    internal int ProtectionApplied, PinPrompts, RecoveryPrompts;
    internal readonly List<LockCover> Covers = [];
    public void ApplyProtection() => ProtectionApplied++;
    public ILockCover? CreateCover(AppLockService service, bool prompt)
    {
        if (!Available) { return null; }
        var cover = new LockCover(); Covers.Add(cover); return cover;
    }
    public Task<bool> ConfirmPinAsync(AppLockService service, string reason) { PinPrompts++; return Task.FromResult(Available && PinAnswer); }
    public Task<bool> ConfirmRecoveryAsync() { RecoveryPrompts++; return Task.FromResult(Available && RecoveryAnswer); }
}

/// <summary>Observable cover transitions used to check gating, loss and rejected removal.</summary>
internal sealed class LockCover : ILockCover
{
    public bool IsOnScreen { get; set; }
    public bool WasLockedBeforeSleep { get; set; }
    internal bool RefuseClose;
    internal int Prompts, Closed;
    public Task ShowAsync() { IsOnScreen = true; return Task.CompletedTask; }
    public Task PromptAsync() { Prompts++; return Task.CompletedTask; }
    public Task CloseAsync()
    {
        if (RefuseClose) { throw new InvalidOperationException("Fictitious upper modal"); }
        Closed++; IsOnScreen = false; return Task.CompletedTask;
    }
}
