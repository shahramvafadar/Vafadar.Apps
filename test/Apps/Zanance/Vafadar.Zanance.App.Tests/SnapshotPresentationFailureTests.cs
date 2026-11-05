using Vafadar.Zanance.App.Presentation;
namespace Vafadar.Zanance.App.Tests;

/// <summary>A failed retained filter covers its results without disturbing source-read ownership or replaying a write.</summary>
public sealed class SnapshotPresentationFailureTests
{
    /// <summary>Retains the completed source while hiding an unrepresentable filter and allowing the original retry.</summary>
    [Fact]
    public async Task Filter_failure_covers_results_and_next_original_read_recovers()
    {
        var state = new SnapshotLoadState(); var reads = 0; var presents = 0;
        await state.RunAsync(() => { reads++; return Task.CompletedTask; }, () => presents++);
        state.CoverFailedPresentation();
        Assert.True(state.HasLoaded); Assert.True(state.HasFailed); Assert.False(state.IsReady); Assert.False(state.IsLoading);
        Assert.Equal(1, reads); Assert.Equal(1, presents);
        await state.RunAsync(() => { reads++; return Task.CompletedTask; }, () => presents++);
        Assert.True(state.IsReady); Assert.False(state.HasFailed); Assert.Equal(2, reads); Assert.Equal(2, presents);
    }

    /// <summary>A pending original read keeps its own task and cover when another caller cannot publish.</summary>
    [Fact]
    public async Task Filter_failure_cannot_replace_a_pending_original_read()
    {
        var state = new SnapshotLoadState(); await state.RunAsync(() => Task.CompletedTask, () => { });
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pending = state.RunAsync(() => held.Task, () => { });
        Assert.Throws<InvalidOperationException>(state.CoverFailedPresentation);
        Assert.True(state.IsLoading); Assert.False(state.HasFailed); Assert.False(pending.IsCompleted);
        held.SetResult(); await pending; Assert.True(state.IsReady);
    }

    /// <summary>An uninitialized snapshot cannot be misrepresented as retained source after a filter failure.</summary>
    [Fact]
    public void Filter_failure_requires_a_completed_original_source()
    {
        var state = new SnapshotLoadState(); Assert.Throws<InvalidOperationException>(state.CoverFailedPresentation);
        Assert.False(state.HasLoaded); Assert.False(state.HasFailed); Assert.False(state.IsReady);
    }
}
