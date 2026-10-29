using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Original report read/publication and command authority, independent of native controls and financial calculators.</summary>
public sealed partial class ReportWorkspace(ReportWorkStore store) : ObservableObject
{
    private bool _active = true, _retired;
    private long _revision;
    private Func<bool>? _rootOwner;
    private string? _originalFile;
    private readonly CancellationTokenSource _root = new();
    private CancellationTokenSource _read = new();
    /// <summary>Gets the covered current read, replaced whenever its original publication retires.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets only the fully validated original report packet.</summary>
    public ReportReadReview? Review { get; private set; }
    /// <summary>Gets the original current read failure for translated native feedback.</summary>
    public Exception? Failure { get; private set; }
    /// <summary>Gets whether current original source may own native report commands.</summary>
    public bool CanInteract => OwnsRoot && _active && Loading.IsReady && Review is not null;
    private bool OwnsRoot => !_retired && (_rootOwner?.Invoke() ?? true);
    /// <summary>Binds the actual original root through a caller-supplied weak owner check.</summary>
    public void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); Changed(); }
    /// <summary>Retires pending reads and parent commands on actual native page/tab departure, retaining scope values outside this owner.</summary>
    public void SetActive(bool active) { if (_active == active) return; _active = active; RetireRead(); }
    /// <summary>Permanently retires this root, including any source-bound child handoff.</summary>
    public void RetireRoot() { if (_retired) return; _retired = true; _root.Cancel(); SetActive(false); Changed(); }
    private void RetireRead()
    {
        ++_revision; var old = _read; _read = CancellationTokenSource.CreateLinkedTokenSource(_root.Token);
        Loading = new(); Review = null; Failure = null; old.Cancel(); old.Dispose(); Changed();
    }
    /// <summary>Reads and validates one original file around preparation, and uncovers only a complete current publication.</summary>
    public async Task PublishAsync(Func<ReportReadReview, CancellationToken, Task> prepare, Action<ReportReadReview> present)
    {
        ArgumentNullException.ThrowIfNull(prepare); ArgumentNullException.ThrowIfNull(present); if (!OwnsRoot || !_active) return;
        RetireRead(); var revision = _revision; var token = _read.Token; var owner = Loading; ReportReadReview? next = null;
        bool Current() => OwnsRoot && _active && revision == _revision && ReferenceEquals(owner, Loading) && !token.IsCancellationRequested;
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("Original report publication retired.", token); }
        void StateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args) { if (Current()) Changed(); }
        owner.PropertyChanged += StateChanged;
        try
        {
            await owner.RunAsync(async () =>
            {
                next = await store.ReadAsync(token, path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }); RequireCurrent();
                await prepare(next, token); RequireCurrent(); await store.ValidateAsync(next, token); RequireCurrent();
            }, () => { RequireCurrent(); next!.EnsureCurrent(); present(next); RequireCurrent(); Review = next; });
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) Failure = exception; }
        finally { owner.PropertyChanged -= StateChanged; Changed(); }
    }
    /// <summary>Covers refused original source authority while keeping an explicit current-page Retry possible.</summary>
    public async Task RefuseAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception); if (!OwnsRoot || !_active) return;
        RetireRead(); var failed = Loading;
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { Failure = exception; }
        Changed();
    }
    /// <summary>Captures exact source, parent generation, scope and optional original row membership for one native command.</summary>
    public ReportCommandLease? CaptureCommand(Func<string> scope, Func<bool>? rowCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(scope); if (!CanInteract || Review is not { } review || rowCurrent?.Invoke() == false) return null;
        var revision = _revision; var stamp = scope(); var token = _read.Token;
        return new(store, review, token, () => CanInteract && revision == _revision && ReferenceEquals(Review, review)
            && stamp == scope() && rowCurrent?.Invoke() != false && !token.IsCancellationRequested,
            () => OwnsRoot && stamp == scope(), _root.Token);
    }
    private void Changed()
    { OnPropertyChanged(nameof(Loading)); OnPropertyChanged(nameof(Review)); OnPropertyChanged(nameof(Failure)); OnPropertyChanged(nameof(CanInteract)); }
}

/// <summary>One original native report command; source and current ownership are rechecked around every external await.</summary>
public sealed class ReportCommandLease
{
    private readonly ReportWorkStore _store;
    private readonly Func<bool> _current, _rootCurrent;
    private readonly CancellationToken _rootToken;
    /// <summary>Retains only the captured original input/access packet.</summary>
    public ReportReadReview Review { get; }
    /// <summary>Gets cancellation bound to this original parent read/frame.</summary>
    public CancellationToken Token { get; }
    /// <summary>Gets current original row/scope/frame authority without granting financial access.</summary>
    public bool IsCurrent => !Token.IsCancellationRequested && _current();
    internal ReportCommandLease(ReportWorkStore store, ReportReadReview review, CancellationToken token, Func<bool> current, Func<bool> rootCurrent, CancellationToken rootToken)
    { _store = store; Review = review; Token = token; _current = current; _rootCurrent = rootCurrent; _rootToken = rootToken; }
    /// <summary>Rejects retired parent authority before and after validating full original data/file/day/access.</summary>
    public async Task ValidateAsync()
    {
        RequireCurrent(); await _store.ValidateAsync(Review, Token); RequireCurrent();
    }
    /// <summary>Rejects a stale original parent command before a native side effect or actual writer commit.</summary>
    public void RequireCurrent() { Token.ThrowIfCancellationRequested(); if (!IsCurrent) throw new InvalidOperationException("Original report command retired."); }
    /// <summary>Captures a source/root/scope-bound child handoff independently of the parent's expected disappearance.</summary>
    public ReportSourceLease CaptureChildSource()
    { RequireCurrent(); return new(_store, Review, _rootCurrent, _rootToken); }
}

/// <summary>Frozen original report source authority for an owned explanation returning to its original parent.</summary>
public sealed class ReportSourceLease(ReportWorkStore store, ReportReadReview review, Func<bool> current, CancellationToken rootToken)
{
    /// <summary>Gets exact original root/scope availability; parent publication may be covered during the normal return.</summary>
    public bool IsCurrent => !rootToken.IsCancellationRequested && current();
    /// <summary>Validates the immutable original packet before and after native child-close handoff.</summary>
    public async Task ValidateAsync()
    {
        RequireCurrent(); await store.ValidateAsync(review, rootToken); RequireCurrent();
    }
    private void RequireCurrent() { rootToken.ThrowIfCancellationRequested(); if (!IsCurrent) throw new InvalidOperationException("Original report explanation source retired."); }
}
