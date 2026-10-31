using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original access startup stops retired secure/native continuations and preserves unstarted destinations.</summary>
public sealed class StartupAccessOwnershipTests
{
    /// <summary>A retired secure read cannot publish access readiness; an actual new startup can drain the preserved destination once.</summary>
    [Fact]
    public async Task Retired_secure_read_never_starts_access_or_drains_original_pending_work()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var host = new CountingHost(); var current = true; var navigations = 0;
        var service = new AppLockService(new AuthPlatform(), f.Store, f.Translator, f.Time, new PinLock(storage, f.Time), host);
        await service.RunWhenUnlockedAsync(() => { navigations++; return Task.CompletedTask; });
        var pending = service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken);
        await storage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        current = false; storage.ReadResult.SetResult(null);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(service.IsLocked); Assert.Equal(0, navigations); Assert.Equal(0, host.ProtectionCalls);
        current = true; await service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken);
        Assert.Equal(1, navigations); Assert.Equal(1, host.ProtectionCalls);
    }

    /// <summary>A synchronous original access event cannot transfer a native cover to another root or remove fail-closed access.</summary>
    [Fact]
    public async Task Root_departure_during_access_event_refuses_cover_and_keeps_requested_lock()
    {
        using var f = new FlowFixture(); var settings = await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken);
        settings.AppLockEnabled = true; await f.Store.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);
        var current = true; var host = new CountingHost();
        var service = new AppLockService(new AuthPlatform(), f.Store, f.Translator, f.Time, new PinLock(new PinMemory(), f.Time), host);
        service.AccessChanged += (_, _) => current = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken));
        Assert.True(service.IsLocked); Assert.Equal(0, host.CoverCreations); Assert.Equal(1, host.ProtectionCalls);
    }

    /// <summary>Departure while one queued destination runs preserves every unstarted destination without replaying the accepted first one.</summary>
    [Fact]
    public async Task Retired_startup_drain_preserves_unstarted_destinations_for_next_current_startup()
    {
        using var f = new FlowFixture(); var current = true; var first = 0; var second = 0; var host = new CountingHost();
        var service = new AppLockService(new AuthPlatform(), f.Store, f.Translator, f.Time, new PinLock(new PinMemory(), f.Time), host);
        await service.RunWhenUnlockedAsync(() => { first++; current = false; return Task.CompletedTask; });
        await service.RunWhenUnlockedAsync(() => { second++; return Task.CompletedTask; });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken));
        Assert.Equal(1, first); Assert.Equal(0, second);
        current = true; await service.StartOwnedAsync(() => current, TestContext.Current.CancellationToken);
        Assert.Equal(1, first); Assert.Equal(1, second);
    }

    private sealed class HeldStorage : IPinStorage
    {
        internal readonly TaskCompletionSource ReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<PinState?> ReadResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<PinState?> ReadAsync() { ReadStarted.TrySetResult(); return ReadResult.Task; }
        public Task WriteAsync(PinState? state) => throw new InvalidOperationException("No secure-state write belongs to this startup read.");
    }
    private sealed class CountingHost : IAppLockHost
    {
        internal int ProtectionCalls, CoverCreations;
        public void ApplyProtection() => ProtectionCalls++;
        public ILockCover? CreateCover(AppLockService service, bool prompt) { CoverCreations++; return new LockCover(); }
        public Task<bool> ConfirmPinAsync(AppLockService service, string reason) => Task.FromResult(false);
        public Task<bool> ConfirmRecoveryAsync() => Task.FromResult(false);
    }
}
