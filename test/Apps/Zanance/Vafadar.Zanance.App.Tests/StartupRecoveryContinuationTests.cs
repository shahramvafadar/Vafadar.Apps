using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual asynchronous native boundaries cannot hand results to a replaced startup source or presentation.</summary>
public sealed class StartupRecoveryContinuationTests
{
    /// <summary>A current original result retains its exact value and starts only one native operation.</summary>
    [Fact]
    public async Task Current_original_result_is_returned_once()
    {
        using var source = new RecoverySetupAuthority(() => { }); var calls = 0;
        Assert.Equal(7, await StartupRecoveryContinuation.AwaitAsync(() => { calls++; return Task.FromResult(7); }, source, () => true));
        Assert.Equal(1, calls);
    }

    /// <summary>An actual native refusal remains a refusal rather than a fabricated successful recovery.</summary>
    [Fact]
    public async Task Original_native_false_is_not_changed_to_acceptance()
    {
        using var source = new RecoverySetupAuthority(() => { });
        Assert.False(await StartupRecoveryContinuation.AwaitAsync(() => Task.FromResult(false), source, () => true));
    }

    /// <summary>An already departed frame cannot begin even the first native input or confirmation.</summary>
    [Fact]
    public async Task Retired_presentation_refuses_before_invoking_native_factory()
    {
        using var source = new RecoverySetupAuthority(() => { }); var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StartupRecoveryContinuation.AwaitAsync(() => { calls++; return Task.FromResult(true); }, source, () => false));
        Assert.Equal(0, calls);
    }

    /// <summary>Root departure ends the original waiter even when the native operation itself has not returned yet.</summary>
    [Fact]
    public async Task Retirement_cancels_pending_native_wait_and_late_result_never_revives_it()
    {
        using var source = new RecoverySetupAuthority(() => { });
        var native = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = StartupRecoveryContinuation.AwaitAsync(() => native.Task, source, () => true);
        Assert.False(pending.IsCompleted); source.Retire();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        native.SetResult(true); Assert.True(pending.IsCanceled);
    }

    /// <summary>Actual native completion cannot transfer authority to a newly presented startup frame.</summary>
    [Fact]
    public async Task Replaced_presentation_after_native_completion_refuses_handoff()
    {
        using var source = new RecoverySetupAuthority(() => { }); var current = true;
        var native = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = StartupRecoveryContinuation.AwaitAsync(() => native.Task, source, () => current);
        current = false; native.SetResult(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    /// <summary>A changed original catalogue refuses the native result without capturing its replacement as new authority.</summary>
    [Fact]
    public async Task Changed_original_source_while_native_work_waits_refuses_handoff()
    {
        var current = true; using var source = new RecoverySetupAuthority(() => { if (!current) throw new IOException("Original source changed."); });
        var native = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = StartupRecoveryContinuation.AwaitAsync(() => native.Task, source, () => true);
        current = false; native.SetResult(true); await Assert.ThrowsAsync<IOException>(() => pending);
    }

    /// <summary>A native failure is still a failure; this boundary does not create an accepted-operation receipt.</summary>
    [Fact]
    public async Task Original_native_failure_propagates_without_an_acceptance_value()
    {
        using var source = new RecoverySetupAuthority(() => { });
        await Assert.ThrowsAsync<IOException>(() => StartupRecoveryContinuation.AwaitAsync(() => Task.FromException<bool>(new IOException("Original native failure.")), source, () => true));
    }

    /// <summary>Retirement during a current-owner check cannot start the next native operation.</summary>
    [Fact]
    public async Task Retirement_during_presentation_check_refuses_before_native_factory()
    {
        using var source = new RecoverySetupAuthority(() => { }); var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StartupRecoveryContinuation.AwaitAsync(() => { calls++; return Task.FromResult(true); }, source, () => { source.Retire(); return true; }));
        Assert.Equal(0, calls);
    }
}
