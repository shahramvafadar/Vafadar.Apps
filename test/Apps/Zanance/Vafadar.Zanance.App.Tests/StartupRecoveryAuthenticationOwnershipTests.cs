using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original secure-state/native confirmation ports preserve startup ownership without replacing authentication policy.</summary>
public sealed class StartupRecoveryAuthenticationOwnershipTests
{
    /// <summary>An already retired startup request never reads secure state or asks a native confirmation.</summary>
    [Fact]
    public async Task Departed_startup_refuses_before_secure_read()
    {
        using var f = new FlowFixture(); var storage = new HeldPinStorage(); var host = new HeldPinHost(); var auth = new AuthPlatform();
        var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new PinLock(storage, f.Time), host);
        Assert.False(await service.ConfirmStartupRecoveryAsync("Fictitious recovery", () => false, TestContext.Current.CancellationToken));
        Assert.Equal(0, storage.Reads); Assert.Equal(0, host.PinCalls); Assert.Equal(0, auth.Calls);
    }

    /// <summary>A real delayed secure read cannot proceed to a replacement startup's native prompt.</summary>
    [Fact]
    public async Task Retirement_during_secure_read_refuses_native_continuation()
    {
        using var f = new FlowFixture(); var storage = new HeldPinStorage { HoldRead = true }; var host = new HeldPinHost(); var auth = new AuthPlatform();
        var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new PinLock(storage, f.Time), host); var current = true;
        var pending = service.ConfirmStartupRecoveryAsync("Fictitious recovery", () => current, TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted); current = false; storage.ReadResult.SetResult(null);
        Assert.False(await pending); Assert.Equal(0, host.PinCalls); Assert.Equal(0, auth.Calls); Assert.False(service.PinUnavailable);
    }

    /// <summary>Cancellation after secure input begins cannot silently become no-PIN confirmation success.</summary>
    [Fact]
    public async Task Cancelled_secure_read_request_cannot_return_implicit_confirmation()
    {
        using var f = new FlowFixture(); using var ct = new CancellationTokenSource();
        var storage = new HeldPinStorage { HoldRead = true }; var host = new HeldPinHost(); var auth = new AuthPlatform();
        var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new PinLock(storage, f.Time), host);
        var pending = service.ConfirmStartupRecoveryAsync("Fictitious recovery", () => true, ct.Token);
        ct.Cancel(); storage.ReadResult.SetResult(null); Assert.False(await pending);
        Assert.Equal(0, host.PinCalls); Assert.Equal(0, auth.Calls);
    }

    /// <summary>A departed read failure cannot overwrite the current caller's secure-unavailability feedback.</summary>
    [Fact]
    public async Task Retired_secure_failure_does_not_publish_old_feedback()
    {
        using var f = new FlowFixture(); var storage = new HeldPinStorage { HoldRead = true }; var host = new HeldPinHost(); var auth = new AuthPlatform();
        var service = new AppLockService(auth, f.Store, f.Translator, f.Time, new PinLock(storage, f.Time), host); var current = true;
        var pending = service.ConfirmStartupRecoveryAsync("Fictitious recovery", () => current, TestContext.Current.CancellationToken);
        current = false; storage.ReadResult.SetException(new IOException("Fictitious original read failure."));
        Assert.False(await pending); Assert.False(service.PinUnavailable); Assert.Equal(0, host.PinCalls); Assert.Equal(0, auth.Calls);
    }

    /// <summary>The original configured PIN branch must refuse a native answer whose presentation has retired.</summary>
    [Fact]
    public async Task Late_native_PIN_result_cannot_confirm_replacement_startup()
    {
        using var f = new FlowFixture(); var storage = new HeldPinStorage(); var host = new HeldPinHost(); var auth = new AuthPlatform();
        var pin = new PinLock(storage, f.Time); Assert.Equal(PinOutcome.Success, (await pin.SetAsync("", "0137")).Outcome);
        var service = new AppLockService(auth, f.Store, f.Translator, f.Time, pin, host); var current = true;
        var pending = service.ConfirmStartupRecoveryAsync("Fictitious recovery", () => current, TestContext.Current.CancellationToken);
        Assert.Equal(1, host.PinCalls); Assert.False(pending.IsCompleted); current = false; host.PinResult.SetResult(true);
        Assert.False(await pending); Assert.Equal(0, auth.Calls); Assert.NotNull(storage.State); Assert.Equal(1, storage.Writes);
    }

    private sealed class HeldPinStorage : IPinStorage
    {
        internal bool HoldRead { get; init; }
        internal int Reads, Writes;
        internal PinState? State;
        internal readonly TaskCompletionSource<PinState?> ReadResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<PinState?> ReadAsync() { Reads++; return HoldRead ? ReadResult.Task : Task.FromResult(State); }
        public Task WriteAsync(PinState? state) { Writes++; State = state; return Task.CompletedTask; }
    }
    private sealed class HeldPinHost : IAppLockHost
    {
        internal int PinCalls;
        internal readonly TaskCompletionSource<bool> PinResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ApplyProtection() { }
        public ILockCover? CreateCover(AppLockService service, bool prompt) => null;
        public Task<bool> ConfirmPinAsync(AppLockService service, string reason) { PinCalls++; return PinResult.Task; }
        public Task<bool> ConfirmRecoveryAsync() => Task.FromResult(false);
    }
}
