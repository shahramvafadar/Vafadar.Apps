namespace Vafadar.Zanance.App.Features.Backup;

public sealed partial class BackupRestoreSession
{
    private bool _active = true, _rootRetired;
    private int _frameVersion, _selectionVersion;
    private CancellationTokenSource _frame = new();
    private Func<bool>? _rootCurrent;
    private readonly Func<bool> _nativeOwnerCurrent;
    private Action? _sourceValidation;
    private bool Current => _active && !_rootRetired && !_frame.IsCancellationRequested
        && _nativeOwnerCurrent() && _rootCurrent?.Invoke() != false;
    /// <summary>Binds the actual root independently of the parent's nested busy state.</summary>
    public void BindRootOwner(Func<bool> current) => _rootCurrent = current;
    /// <summary>Activates only actual native return; an old Prepare/Check cannot reactivate the page.</summary>
    public void SetPresentationActive(bool value)
    {
        if (!value) { Deactivate(); return; }
        if (_rootRetired || _rootCurrent?.Invoke() == false) return;
        if (!_active) { _frame.Dispose(); _frame = new(); _active = true; }
        RefreshActions();
    }
    /// <summary>Retires original native consent and preview while retaining an uncommitted raw package.</summary>
    public void Deactivate()
    {
        _active = false; Interlocked.Increment(ref _frameVersion); _frame.Cancel();
        HasPreview = false; PreviewText = null; RefreshActions();
        // Do not release a primary replacement's busy lifetime before its real completion/rollback.
    }
    /// <summary>Permanently retires this actual original root without erasing a known committed receipt.</summary>
    public void RetireRoot()
    { _rootRetired = true; Deactivate(); if (!HasCompleted && !IsBusy) ClearSelection(); }
    /// <summary>One original native frame, carried across picker/download/inspection without borrowing a later return.</summary>
    public sealed class PreparationOwner
    {
        private readonly BackupRestoreSession _owner;
        private readonly int _version;
        /// <summary>Gets the original frame cancellation token for stream and service work.</summary>
        public CancellationToken Token { get; }
        /// <summary>Gets the opaque target publication captured before the native selection starts.</summary>
        internal Action SourceValidation { get; }
        /// <summary>Captures original frame and target without opening or exposing private storage.</summary>
        internal PreparationOwner(BackupRestoreSession owner, int version, CancellationToken token, Action sourceValidation)
        { _owner = owner; _version = version; Token = token; SourceValidation = sourceValidation; }
        /// <summary>Gets whether the captured original frame still owns its actual root.</summary>
        public bool IsCurrent => !Token.IsCancellationRequested && _version == Volatile.Read(ref _owner._frameVersion) && _owner.Current;
        /// <summary>Refuses an old native continuation even if the same page later reappears.</summary>
        public void EnsureCurrent()
        { if (!IsCurrent) throw new OperationCanceledException("The original restore frame retired.", Token); }
    }
    /// <summary>Captures actual native ownership before a local/cloud picker or dialog starts.</summary>
    public PreparationOwner CapturePreparationOwner()
    { var owner = new PreparationOwner(this, Volatile.Read(ref _frameVersion), _frame.Token, _runtime.CaptureSourceValidation()); owner.EnsureCurrent(); return owner; }
    private void EndOriginalOperation()
    {
        IsBusy = false;
        if (_rootRetired && !HasCompleted) ClearSelection();
        RefreshActions();
    }
}
