using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.Core.Tests.Security;

/// <summary>Original security mutations retain their owner across exclusive waiting and protected reads.</summary>
public sealed class PinConfigurationOwnerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class HeldStorage : IPinStorage
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Reads, Writes;
        public async Task<PinState?> ReadAsync() { Reads++; Entered.TrySetResult(); await Released.Task.WaitAsync(Ct); return null; }
        public Task WriteAsync(PinState? state) { Writes++; return Task.CompletedTask; }
    }
    /// <summary>Neither setting nor removal may continue after the actual protected read outlives its original owner.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configuration_rechecks_original_owner_after_actual_protected_read(bool remove)
    {
        var storage = new HeldStorage(); var gate = new PinLock(storage, TimeProvider.System); var current = true;
        void Owner() { if (!current) throw new InvalidOperationException("Original owner retired."); }
        var request = remove ? gate.RemoveAsync("0123", Owner) : gate.SetAsync("", "0123", Owner);
        await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); current = false; storage.Released.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => request); Assert.Equal(0, storage.Writes); Assert.False(gate.IsEnabled);
    }
    /// <summary>A queued request checks ownership only after it has acquired actual exclusive access, before protected reading.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Queued_configuration_refuses_before_read_after_original_owner_loss(bool remove)
    {
        var storage = new HeldStorage(); var gate = new PinLock(storage, TimeProvider.System); var current = true;
        var read = gate.LoadAsync(); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        void Owner() { if (!current) throw new InvalidOperationException("Original owner retired."); }
        var request = remove ? gate.RemoveAsync("0123", Owner) : gate.SetAsync("", "0123", Owner);
        current = false; storage.Released.SetResult(); await read;
        await Assert.ThrowsAsync<InvalidOperationException>(() => request); Assert.Equal(1, storage.Reads); Assert.Equal(0, storage.Writes);
    }
}
