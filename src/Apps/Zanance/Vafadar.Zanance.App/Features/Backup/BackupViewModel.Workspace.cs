using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Backup;

public sealed partial class BackupViewModel
{
    private readonly IBackupWorkspaceRuntime _workspaceRuntime;
    private bool _workspaceCommandActive;
    private Task? _pendingWorkspaceRead;
    private BackupRestoreSession.PreparationOwner? _pendingWorkspaceOwner;
    private bool WorkspaceSourceCurrent => _pendingWorkspaceOwner is { } original && IsOriginalBackupFrameCurrent(original);
    /// <summary>Keeps original backup lists and commands covered until a complete current read has published.</summary>
    public SnapshotLoadState WorkspaceLoading { get; private set; } = new();
    /// <summary>Gets current inline discovery guidance without borrowing a retired page for native failure dialogs.</summary>
    [ObservableProperty] public partial string? WorkspaceError { get; set; }
    /// <summary>Retains the actual confirmed deletion independently of any subsequent discovery or display failure.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCoveredPortableFeedback), nameof(HasBackupFeedback))]
    public partial bool HasDeletedBackupReceipt { get; private set; }
    /// <summary>Gets whether a known completed destination/creation result must remain visible above a covered read failure.</summary>
    public bool HasCoveredPortableFeedback => WorkspaceLoading.HasFailed
        && (HasDeletedBackupReceipt || CreateResult is not null || CloudResult is not null);
    /// <summary>Gets whether the reserved feedback row contains original restore or known portable/destination feedback.</summary>
    public bool HasBackupFeedback => Restore.HasFeedback || HasCoveredPortableFeedback;
    /// <summary>Gets whether the actual original failed workspace can be explicitly reloaded.</summary>
    public bool CanReloadWorkspace => _presentationActive && !_workspaceCommandActive && !_portableCreationActive
        && !IsBusy && !WorkspaceLoading.IsLoading;
    [RelayCommand(CanExecute = nameof(CanReloadWorkspace))]
    private Task ReloadWorkspaceAsync() => LoadAsync();

    private void NotifyWorkspaceAvailability() => NotifyPortableAvailability();

    private void BeginWorkspaceCommand()
    { _workspaceCommandActive = true; IsBusy = true; NotifyWorkspaceAvailability(); }
    private void EndWorkspaceCommand()
    { _workspaceCommandActive = false; IsBusy = false; NotifyWorkspaceAvailability(); }

    private async Task RequireWorkspaceReloadAsync(BackupRestoreSession.PreparationOwner owner, Exception failure)
    {
        if (!IsOriginalBackupFrameCurrent(owner)) return;
        WorkspaceLoading = new(); OnPropertyChanged(nameof(WorkspaceLoading));
        try { await WorkspaceLoading.RunAsync(() => Task.FromException(failure), () => { }); }
        catch (Exception error) when (error is not OutOfMemoryException) { WorkspaceError = _translator["Backup_ReadFailed"]; }
        NotifyWorkspaceAvailability();
    }

    private Vafadar.Backup.IBackupStorage BoundStorage(CloudAccountRow row, Action current) =>
        new OriginalBackupDestinationStorage(Storage(row.Provider), SignIn(row.Provider),
            row.Identity ?? throw new OperationCanceledException("The original backup account is not connected."), current);

    /// <summary>Retains the original row/account/disclosure, never a same-provider row from a later appearance.</summary>
    private (BackupRestoreSession.PreparationOwner Owner, Action Check) CaptureDestination(CloudAccountRow row, CloudBackup? backup = null)
    {
        var sourceOwner = _pendingWorkspaceOwner ?? throw new OperationCanceledException("The original backup workspace is unavailable.");
        CheckCreationFrame(sourceOwner);
        var owner = Restore.CapturePreparationOwner(); var account = row.Account; var identity = row.Identity;
        var language = _localization.CurrentLanguage.CultureName;
        void Check()
        {
            CheckCreationFrame(owner);
            CheckCreationFrame(sourceOwner);
            if (!ReferenceEquals(sourceOwner, _pendingWorkspaceOwner)
                || !CloudAccounts.Any(original => ReferenceEquals(original, row)) || row.Account != account || row.Identity != identity
                || _localization.CurrentLanguage.CultureName != language
                || backup is not null && (!CloudBackups.Any(original => ReferenceEquals(original, backup))
                    || !row.Backups.Any(original => ReferenceEquals(original, backup)) || backup.Provider != row.Provider))
                throw new OperationCanceledException("The original backup destination or disclosure changed.", owner.Token);
        }
        Check(); return (owner, Check);
    }

    /// <summary>Suppresses stale destination feedback without suppressing failures on the actual original current row.</summary>
    private static bool IsOriginalDestinationCurrent(Action check)
    {
        try { check(); return true; }
        catch (Exception error) when (error is not OutOfMemoryException) { return false; }
    }

    /// <summary>Checks the actual captured page/root/file without borrowing a later profile for error feedback.</summary>
    private static bool IsOriginalBackupFrameCurrent(BackupRestoreSession.PreparationOwner owner)
    {
        try { CheckCreationFrame(owner); return true; }
        catch (Exception error) when (error is not OutOfMemoryException) { return false; }
    }
}
