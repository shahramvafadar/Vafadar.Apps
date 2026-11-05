using Vafadar.Zanance.App.Presentation;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Retained query recovery must never replay a source read or take a pending reader's ownership.</summary>
public sealed class RetainedPresentationRecoveryTests
{
    /// <summary>Correcting a failed presentation reuses its completed source and publishes only after success.</summary>
    [Fact]
    public async Task A_valid_correction_recovers_without_another_source_read()
    {
        var s = new SnapshotLoadState(); var reads = 0; var presents = 0;
        await s.RunAsync(() => { reads++; return Task.CompletedTask; }, () => presents++); s.CoverFailedPresentation();
        Assert.True(s.CanPresentRetained); Assert.False(s.IsReady);
        await s.RunPresentationAsync(() => { Assert.False(s.IsReady); Assert.False(s.CanPresentRetained); presents++; });
        Assert.True(s.IsReady); Assert.True(s.CanPresentRetained); Assert.False(s.HasFailed); Assert.Equal(1, reads); Assert.Equal(2, presents);
    }

    /// <summary>A failed correction remains covered and may be corrected again without a source delegate.</summary>
    [Fact]
    public async Task Another_invalid_query_stays_covered_until_a_valid_correction()
    {
        var s = new SnapshotLoadState(); var reads = 0; await s.RunAsync(() => { reads++; return Task.CompletedTask; }, () => { });
        await Assert.ThrowsAsync<OverflowException>(() => s.RunPresentationAsync(() => throw new OverflowException()));
        Assert.True(s.HasLoaded); Assert.True(s.HasFailed); Assert.False(s.IsReady); Assert.True(s.CanPresentRetained);
        await s.RunPresentationAsync(() => { }); Assert.True(s.IsReady); Assert.Equal(1, reads);
    }

    /// <summary>No retained publication is fabricated before a source has completed.</summary>
    [Fact]
    public async Task An_uninitialized_source_cannot_be_represented()
    {
        var s = new SnapshotLoadState(); var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.RunPresentationAsync(() => calls++));
        Assert.False(s.CanPresentRetained); Assert.False(s.HasLoaded); Assert.Equal(0, calls);
    }

    /// <summary>A pending source keeps its actual task and cannot be replaced by filter recovery.</summary>
    [Fact]
    public async Task A_pending_original_reader_keeps_ownership()
    {
        var s = new SnapshotLoadState(); await s.RunAsync(() => Task.CompletedTask, () => { });
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var task = s.RunAsync(() => held.Task, () => { });
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.RunPresentationAsync(() => { })); Assert.False(s.CanPresentRetained); Assert.False(task.IsCompleted);
        held.SetResult(); await task; Assert.True(s.IsReady);
    }

    /// <summary>Reentrant source callers share the active presentation owner instead of replaying a source read.</summary>
    [Fact]
    public async Task Reentrant_callers_share_the_registered_owner()
    {
        var s = new SnapshotLoadState(); var reads = 0; var nestedPresents = 0; Task? nested = null;
        await s.RunAsync(() => { reads++; return Task.CompletedTask; }, () => { });
        var task = s.RunPresentationAsync(() => nested = s.RunAsync(() => { reads++; return Task.CompletedTask; }, () => nestedPresents++));
        await task; Assert.Same(task, nested); Assert.Equal(1, reads); Assert.Equal(0, nestedPresents);
    }

    /// <summary>Readiness notifications never expose rows before the replacement presentation finishes.</summary>
    [Fact]
    public async Task Readiness_stays_covered_until_the_callback_completes()
    {
        var s = new SnapshotLoadState(); await s.RunAsync(() => Task.CompletedTask, () => { }); s.CoverFailedPresentation();
        var finished = false; var unsafeReady = false; var states = new List<bool>();
        s.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(s.CanPresentRetained)) states.Add(s.CanPresentRetained); if (s.IsReady && !finished) unsafeReady = true; };
        await s.RunPresentationAsync(() => { Assert.False(s.IsReady); finished = true; });
        Assert.False(unsafeReady); Assert.Contains(false, states); Assert.True(states.Last());
    }
}
