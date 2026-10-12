using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Backup;
using Vafadar.Backup.Security;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Original preview/consent/safety/restore flow, with an accepted result retired before fallible follow-through.</summary>
public sealed partial class BackupRestoreSession : ViewModelBase
{
    private readonly IBackupService _backup;
    private readonly IBackupRestoreRuntime _runtime;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ILocalizationService _localization;
    private readonly Func<bool> _ownerAvailable;
    private byte[]? _package;
    private bool _opened;

    /// <summary>Uses original backup services and explicit effects; no native control or global window is required.</summary>
    public BackupRestoreSession(IBackupService backup, IBackupRestoreRuntime runtime, Translator translator,
        IDateFormatter dates, ILocalizationService localization, TimeProvider time, Func<bool>? ownerAvailable = null)
    {
        _backup = backup; _runtime = runtime; _translator = translator; _dates = dates; _localization = localization; _time = time;
        _ownerAvailable = ownerAvailable ?? (() => true);
        PropertyChanged += (_, e) => { if (e.PropertyName is nameof(IsBusy) or nameof(HasPreview) or nameof(HasCompleted) or nameof(Error) or nameof(Result)) RefreshActions(); };
    }
    /// <summary>Gets whether the selected uncommitted package requires a user-entered password.</summary>
    [ObservableProperty] public partial bool NeedsPassword { get; set; }
    /// <summary>Gets or sets only the in-memory user input, never persisted or logged.</summary>
    [ObservableProperty] public partial string Password { get; set; } = string.Empty;
    /// <summary>Gets the exact selected file's user-visible name.</summary>
    [ObservableProperty] public partial string? PickedName { get; set; }
    /// <summary>Gets the inspected original package description before replacement.</summary>
    [ObservableProperty] public partial string? PreviewText { get; set; }
    /// <summary>Gets whether explicit inspection succeeded for this current input.</summary>
    [ObservableProperty] public partial bool HasPreview { get; set; }
    /// <summary>Gets whether every original backup source completed, independently of follow-through.</summary>
    [ObservableProperty] public partial bool HasCompleted { get; set; }
    /// <summary>Gets complete translated failure or retry feedback without exception contents.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets the completed restore result without implying device/provider acceptance.</summary>
    [ObservableProperty] public partial string? Result { get; set; }
    /// <summary>Gets whether selection/cancellation can change an uncommitted review.</summary>
    public bool CanSelect => !IsBusy && !HasCompleted && _ownerAvailable();
    /// <summary>Gets whether the existing in-memory package can be explicitly inspected or re-inspected.</summary>
    public bool CanCheck => CanSelect && _package is not null;
    /// <summary>Gets whether replacement may be explicitly attempted from a current successful preview.</summary>
    public bool CanRestore => CanCheck && HasPreview;
    /// <summary>Gets whether accepted follow-through may retry without rereading/replacing data.</summary>
    public bool CanFinish => !IsBusy && HasCompleted && !_opened && _ownerAvailable();
    /// <summary>Gets whether navigation may leave an uncommitted, idle flow.</summary>
    public bool CanLeave => !IsBusy && (!HasCompleted || _opened) && _ownerAvailable();
    /// <summary>Gets whether a fresh explicit preview is required before replacement is enabled.</summary>
    public bool NeedsReview => CanCheck && !HasPreview;
    /// <summary>Gets whether pinned review, result or retry feedback needs its growing row.</summary>
    public bool HasFeedback => _package is not null || Error is not null || Result is not null;
    /// <summary>Gets or sets the first-run context without creating an extra account.</summary>
    public bool IsOnboarding { get; set; }

    partial void OnPasswordChanged(string value) { HasPreview = false; PreviewText = null; RefreshActions(); }
    /// <summary>Refreshes only affordances after the original parent busy state changes.</summary>
    public void OwnerStateChanged() => RefreshActions();
    private void RefreshActions()
    {
        OnPropertyChanged(nameof(CanSelect)); OnPropertyChanged(nameof(CanCheck)); OnPropertyChanged(nameof(CanRestore));
        OnPropertyChanged(nameof(CanFinish)); OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(HasFeedback)); OnPropertyChanged(nameof(NeedsReview));
        CheckCommand.NotifyCanExecuteChanged(); RestoreCommand.NotifyCanExecuteChanged(); FinishCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Publishes a new package only after its entire stream was read; failed reads retain the old review.</summary>
    public async Task PrepareAsync(Stream stream, string name)
    {
        if (IsBusy || HasCompleted) return;
        IsBusy = true;
        try
        {
            using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer);
            ClearSelection(); _package = buffer.ToArray(); PickedName = name; NeedsPassword = BackupEncryption.IsEncrypted(_package);
            if (!NeedsPassword) await InspectAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { Error = _translator["Backup_Error_Storage"]; }
        finally { IsBusy = false; RefreshActions(); }
    }
    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckAsync()
    {
        if (!CanCheck) return;
        IsBusy = true;
        try { await InspectAsync(); }
        finally { IsBusy = false; }
    }
    private async Task InspectAsync()
    {
        Error = null; HasPreview = false;
        try
        {
            var manifest = await _backup.InspectPackageAsync(_package!, NeedsPassword ? Password : null);
            PreviewText = Describe(manifest); HasPreview = true;
        }
        catch (BackupException exception) { Error = _translator[$"Backup_Error_{exception.Error}"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException) { Error = _translator["Backup_ReviewFailed"]; }
    }
    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync()
    {
        if (!CanRestore) return;
        IsBusy = true; var replacing = false; var safetyStarted = false;
        try
        {
            if (!await _runtime.ConfirmReplacementAsync() || !await _runtime.AuthenticateAsync()) return;
            Error = null;
            safetyStarted = true; await _runtime.CreateSafetyCopyAsync();
            replacing = true;
            await _runtime.RestorePrimaryAsync(_package!, NeedsPassword ? Password : null);
            // Every original source has returned. Never leave this accepted payload available to a fallible follow-up.
            HasCompleted = true; ClearSelection(); Result = _translator["Backup_RestoredMessage"];
            await FinishCoreAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (HasCompleted) Error = _translator["Backup_RestoredFollowUpFailed"];
            else if (replacing)
            {
                // A later source can throw BackupException after an earlier source changed data (D-167).
                HasPreview = false; Error = _translator["Backup_Error_RestoreFailed"];
            }
            else Error = _translator[safetyStarted ? "Backup_Error_SafetyCopyFailed" : "Backup_ConsentFailed"];
        }
        finally { IsBusy = false; RefreshActions(); }
    }
    [RelayCommand(CanExecute = nameof(CanFinish))]
    private async Task FinishAsync()
    {
        if (!CanFinish) return;
        IsBusy = true; Error = null;
        try { await FinishCoreAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { Error = _translator["Backup_RestoredFollowUpFailed"]; }
        finally { IsBusy = false; RefreshActions(); }
    }
    private async Task FinishCoreAsync()
    {
        await _runtime.ApplyDisplayAsync();
        Result = _translator["Backup_RestoredMessage"];
        if (IsOnboarding) await _runtime.CompleteOnboardingAsync();
        await _runtime.ReloadLockAsync();
        await _runtime.RefreshFinancialWorkAsync(DateOnly.FromDateTime(_time.GetLocalNow().DateTime));
        await _runtime.OpenRestoredProfileAsync(IsOnboarding);
        _opened = true;
    }
    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Cancel() { if (CanSelect) { ClearSelection(); Error = null; Result = null; RefreshActions(); } }
    private void ClearSelection()
    {
        _package = null; PickedName = null; Password = string.Empty; NeedsPassword = false; HasPreview = false; PreviewText = null;
    }
    private string Describe(BackupManifest manifest)
    {
        var lines = new List<string>
        {
            _translator.Format("Backup_PreviewCreated", FormatTime(manifest.CreatedAt)),
            _translator.Format("Backup_PreviewDevice", manifest.DeviceName ?? "?", manifest.AppVersion),
        };

        if (manifest.Summary is { } summary)
        {
            lines.Add(_translator.Format(
                "Backup_PreviewCounts",
                summary.GetValueOrDefault(ZananceBackupSummary.Accounts, "?"),
                summary.GetValueOrDefault(ZananceBackupSummary.Entries, "?"),
                summary.GetValueOrDefault(ZananceBackupSummary.Plans, "?")));

            // Goals and holdings are listed when the backup has them (backups of earlier versions do not; ZEX-S0409).
            if (summary.TryGetValue(ZananceBackupSummary.Goals, out var goals) && summary.TryGetValue(ZananceBackupSummary.Holdings, out var holdings))
            {
                lines.Add(_translator.Format("Backup_PreviewGoalsHoldings", goals, holdings));
            }

            if (summary.TryGetValue(ZananceBackupSummary.Snapshots, out var snapshots) && snapshots != "0")
            {
                lines.Add(_translator.Format("Backup_PreviewSnapshots", snapshots));
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string FormatTime(DateTimeOffset value) =>
        $"{_dates.Format(value, DateFormatStyle.Long)} {value.ToLocalTime().ToString("t", _localization.CurrentCulture)}";

}
