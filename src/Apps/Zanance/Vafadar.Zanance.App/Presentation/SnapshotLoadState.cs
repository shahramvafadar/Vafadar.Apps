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
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady))]
    public partial bool IsLoading { get; private set; } = true;

    /// <summary>Gets whether at least one complete snapshot has been presented.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady))]
    public partial bool HasLoaded { get; private set; }

    /// <summary>Gets whether the last attempt failed and the page should offer a retry.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsReady))]
    public partial bool HasFailed { get; private set; }

    /// <summary>Gets whether the current snapshot can be shown and interacted with.</summary>
    public bool IsReady => HasLoaded && !IsLoading && !HasFailed;

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
