using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original PIN form continuations use only fictitious isolated verifier storage.</summary>
public sealed class PinSettingsLifetimeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class HeldStorage : IPinStorage
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Writes;
        internal bool FailRead;
        public async Task<PinState?> ReadAsync() { Entered.TrySetResult(); await Released.Task.WaitAsync(Ct); if (FailRead) throw new IOException("Fictitious storage failure"); return null; }
        public Task WriteAsync(PinState? value) { Writes++; return Task.CompletedTask; }
    }
    private static PinSettingsViewModel Create(FlowFixture f, HeldStorage storage) => new(
        new(new AuthPlatform(), f.Store, f.Translator, f.Time, new(storage, f.Time), new LockPlatform()), f.Translator)
        { NewPin = "0123", ConfirmPin = "0123" };

    /// <summary>Request-only secret text is removed before the first protected await, not retained by the editable form.</summary>
    [Fact]
    public async Task Submitted_secret_text_is_cleared_before_protected_storage_wait()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage);
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        var cleared = vm.CurrentPin.Length + vm.NewPin.Length + vm.ConfirmPin.Length == 0;
        storage.Released.SetResult(); await save;
        Assert.True(cleared); Assert.Equal(1, storage.Writes);
    }

    /// <summary>An older accepted request never clears secret text typed for a newer draft.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accepted_request_preserves_newer_secret_draft_even_when_close_fails(bool closeFails)
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage);
        vm.CloseAsync = () => closeFails ? Task.FromException(new IOException("Fictitious close failure")) : Task.CompletedTask;
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        vm.NewPin = "4567"; vm.ConfirmPin = "4567";
        storage.Released.SetResult(); await save;
        Assert.Equal("4567", vm.NewPin); Assert.Equal("4567", vm.ConfirmPin); Assert.Equal(1, storage.Writes);
    }

    /// <summary>Public feedback cannot release actual protected-operation ownership or allow a competing departure.</summary>
    [Fact]
    public async Task Mutable_busy_feedback_does_not_release_pending_original_PIN_operation()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage); var closes = 0;
        vm.CloseAsync = () => { closes++; return Task.CompletedTask; };
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        vm.IsBusy = false; await vm.BackCommand.ExecuteAsync(null); var premature = closes;
        storage.Released.SetResult(); await save;
        Assert.Equal(0, premature); Assert.Equal(1, storage.Writes);
    }

    /// <summary>An actual departed modal/root or retired-then-reappeared form cannot authorize an old pending write.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Pending_configuration_refuses_actual_original_owner_loss(int departure)
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage); var owner = true; var closes = 0;
        vm.IsCurrent = () => owner; vm.CloseAsync = () => { closes++; return Task.CompletedTask; };
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        if (departure == 0) owner = false;
        else { vm.Deactivate(); if (departure == 2) vm.Activate(); }
        storage.Released.SetResult(); await save;
        Assert.Equal(0, storage.Writes); Assert.Equal(0, closes); Assert.False(vm.HasPin);
    }

    /// <summary>A command retained from another modal cannot read or mutate protected PIN state.</summary>
    [Fact]
    public async Task Already_departed_original_form_never_reads_protected_state()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage); vm.IsCurrent = () => false;
        await vm.SaveCommand.ExecuteAsync(null); await vm.RemoveCommand.ExecuteAsync(null);
        Assert.False(storage.Entered.Task.IsCompleted); Assert.Equal(0, storage.Writes);
    }

    /// <summary>A failed close after a returned write does not submit the original security mutation again.</summary>
    [Fact]
    public async Task Known_accepted_write_retries_only_original_close()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage); var closes = 0;
        vm.CloseAsync = () => { closes++; return Task.FromException(new IOException("Fictitious navigation failure")); };
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        storage.Released.SetResult(); await save; Assert.Equal(f.Translator["ActiveItems_SavedReturnFailed"], vm.PageError);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1, storage.Writes); Assert.Equal(2, closes); Assert.True(vm.HasPin);
    }

    /// <summary>A failed older storage request preserves newer editable secrets while publishing only bounded failure feedback.</summary>
    [Fact]
    public async Task Failed_request_preserves_newer_draft_without_navigation()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage { FailRead = true }; var vm = Create(f, storage); var closes = 0;
        vm.CloseAsync = () => { closes++; return Task.CompletedTask; };
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        vm.NewPin = "4567"; vm.ConfirmPin = "4567"; storage.Released.SetResult(); await save;
        Assert.Equal("4567", vm.NewPin); Assert.Equal("4567", vm.ConfirmPin); Assert.NotNull(vm.PageError);
        Assert.Equal(0, storage.Writes); Assert.Equal(0, closes);
    }

    /// <summary>A pending retry of known accepted closing owns departure independently of original primary work.</summary>
    [Fact]
    public async Task Accepted_close_retry_does_not_allow_competing_Back()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage); var closes = 0;
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.CloseAsync = async () =>
        {
            closes++;
            if (closes == 1) throw new IOException("Fictitious initial close failure");
            closing.TrySetResult(); await release.Task.WaitAsync(Ct);
        };
        var save = vm.SaveCommand.ExecuteAsync(null); await storage.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        storage.Released.SetResult(); await save;
        var retry = vm.SaveCommand.ExecuteAsync(null); await closing.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        var back = vm.BackCommand.ExecuteAsync(null); var observed = closes;
        release.SetResult(); await Task.WhenAll(retry, back);
        Assert.Equal(2, observed); Assert.Equal(1, storage.Writes);
    }

    /// <summary>An unsubmitted Back failure is a navigation failure, never a claim that protected PIN reading or saving failed.</summary>
    [Fact]
    public async Task Unsubmitted_close_failure_does_not_claim_protected_storage_failure()
    {
        using var f = new FlowFixture(); var storage = new HeldStorage(); var vm = Create(f, storage);
        vm.CloseAsync = () => Task.FromException(new IOException("Fictitious navigation failure"));
        await vm.BackCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Error_Unexpected"], vm.PageError);
        Assert.False(storage.Entered.Task.IsCompleted); Assert.Equal(0, storage.Writes);
    }
}
