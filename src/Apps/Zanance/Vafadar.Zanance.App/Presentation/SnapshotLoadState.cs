using CommunityToolkit.Mvvm.ComponentModel;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps a page covered until its snapshot has been read and presented, with one outstanding load and retry after
/// failure. Call from the presentation thread; await retains that context for publishing and state changes (D-76).
/// </summary>
public sealed partial class SnapshotLoadState : ObservableObject
{
    private Task? _pending;

    /// <summary>Gets whether initial loading or a reload is in progress; the first frame starts covered.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady)), NotifyPropertyChangedFor(nameof(CanPresentRetained))]
    public partial bool IsLoading { get; private set; } = true;

    /// <summary>Gets whether at least one complete snapshot has been presented.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady)), NotifyPropertyChangedFor(nameof(CanPresentRetained))]
    public partial bool HasLoaded { get; private set; }

    /// <summary>Gets whether the last attempt failed and the page should offer a retry.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady))]
    public partial bool HasFailed { get; private set; }

    /// <summary>Gets whether the current snapshot can be shown and interacted with.</summary>
    public bool IsReady => HasLoaded && !IsLoading && !HasFailed;

    /// <summary>Gets whether a completed original source may be presented again without a new read.</summary>
    public bool CanPresentRetained => HasLoaded && !IsLoading;

    /// <summary>Re-presents only a completed source under the same cover and pending-task ownership as a read.</summary>
    public Task RunPresentationAsync(Action present)
    {
        ArgumentNullException.ThrowIfNull(present);
        if (!CanPresentRetained) throw new InvalidOperationException("A completed original source is required for presentation recovery.");
        // D-855: no new source delegate runs; the original owner still covers errors and coalesces reentrant callers.
        return RunAsync(static () => Task.CompletedTask, present);
    }

    /// <summary>Shares a pending read, presents before uncovering the page, and propagates failure to its caller.</summary>
    public Task RunAsync(Func<Task> read, Action present)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(present);
        if (_pending is { } pending) { return pending; }

        // Register before starting: a synchronously completed read must not leave a cached completed task that
        // suppresses every later reload, and a native callback must not start a second read during presentation.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = completion.Task;
        IsLoading = true;
        // Cover a failed reload before clearing its error: otherwise the retained prior snapshot briefly appears ready.
        HasFailed = false;
        _ = ExecuteAsync(read, present, completion);
        return completion.Task;
    }

    /// <summary>Covers a completed snapshot when its current read-only filter cannot be presented safely.</summary>
    public void CoverFailedPresentation()
    {
        // A filter failure must not replace the ownership or completion of an already pending source read (D-854).
        if (!HasLoaded || IsLoading) throw new InvalidOperationException("Only a completed original snapshot may fail presentation.");
        HasFailed = true;
    }

    private async Task ExecuteAsync(Func<Task> read, Action present, TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            await read();
            present();
            HasLoaded = true;
        }
        catch (Exception ex)
        {
            HasFailed = true;
            failure = ex;
        }
        finally
        {
            _pending = null;
            IsLoading = false;
        }

        if (failure is OperationCanceledException cancelled) { completion.SetCanceled(cancelled.CancellationToken); }
        else if (failure is not null) { completion.SetException(failure); }
        else { completion.SetResult(); }
    }
}
