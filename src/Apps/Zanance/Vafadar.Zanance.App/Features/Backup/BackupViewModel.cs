using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup.Storage;
using Vafadar.Backup;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>A backup file kept on this device.</summary>
public sealed record LocalBackup(BackupFileInfo File, string Title, string Details, bool DeviceOnly = false);

/// <summary>
/// Backup and restore (UI-13, BAK-01..12). A backup is an optionally encrypted file the user keeps wherever they like – shared to
/// Google Drive, OneDrive, e-mail or a computer. A restore replaces all data after a preview and a safety copy; it is
/// never a merge (BAK-02).
/// </summary>
public sealed partial class BackupViewModel : ViewModelBase
{
    private const int MinimumPasswordLength = 8;
    private const string PasswordProtectionKey = "backup.usePassword";

    private readonly IBackupService _backup;
    private readonly AppLockService _lock;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly LocalFolderBackupStorage _local;
    private readonly LocalFolderBackupStorage _safety;
    private Page? _page;
    private Func<Task>? _returnToOnboarding;

    private Page DialogPage => _page ?? throw new InvalidOperationException("The backup page has not been attached.");

    /// <summary>Gets whether only restoration is offered before onboarding is completed (D-62).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateBackup))]
    [NotifyPropertyChangedFor(nameof(CanChooseAutomatic))]
    public partial bool IsRestoreOnly { get; set; }

    /// <summary>Gets whether creating or uploading a backup is offered on this page.</summary>
    public bool CanCreateBackup => !IsRestoreOnly && !Restore.HasCompleted;
    /// <summary>Gets the original non-UI restore flow used by local, picked and connected-cloud packages.</summary>
    public BackupRestoreSession Restore { get; }
    /// <summary>Gets whether backup inputs remain available before an accepted restore is opened.</summary>
    public bool CanUseBackupTools => !IsBusy && !Restore.HasCompleted;
    /// <summary>Gets whether native Back/tab navigation may leave the original restore flow.</summary>
    public bool CanLeave => !IsBusy && Restore.CanLeave;
    /// <summary>Forwards storage feedback from the original local/cloud adapters into the restore flow.</summary>
    public string? RestoreError { get => Restore.Error; set => Restore.Error = value; }
    private bool _restorePriorBusy;
    private readonly bool _initializingProtection = true;
#if DEBUG
    /// <summary>Independent diagnostics skip native/cloud discovery and never display real provider identity.</summary>
    internal bool IndependentRestoreReview { get; set; }
    /// <summary>Uses independent native automatic-copy fixtures without accessing real cloud identities.</summary>
    internal bool IndependentAutomaticReview { get; set; }
#endif

    /// <summary>Sets the host for dialogs, including first run when no Shell exists.</summary>
    public void AttachPage(Page page) => _page = page;

    /// <summary>Switches to restore without creating a financial account; cancellation preserves the wizard.</summary>
    public void BeginOnboardingRestore(Func<Task> returnToOnboarding)
    {
        _returnToOnboarding = returnToOnboarding;
        IsRestoreOnly = true;
    }

    /// <summary>Returns to the same onboarding step and discards the selected package and password.</summary>
    [RelayCommand]
    private async Task BackToOnboardingAsync()
    {
        if (CanLeave)
        {
            Restore.CancelCommand.Execute(null);
            if (_returnToOnboarding is not null)
            {
                await _returnToOnboarding();
            }
        }
    }

    /// <summary>Composes original creation/discovery with the shared restore session and attached native effects.</summary>
    public BackupViewModel(
        IBackupService backup,
        AutoPostProcessor autoPost,
        ReminderService reminders,
        AppLockService appLock,
        Translator translator,
        IDateFormatter dates,
        ILocalizationService localization,
        TimeProvider time,
        Vafadar.Authentication.Maui.CloudSignIn cloud,
        IServiceProvider services,
        IBackupRestoreRuntime? restoreRuntime = null)
    {
        _cloud = cloud;
        _lock = appLock;
        _services = services;
        _backup = backup;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        _local = new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups"));
        _safety = new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups-safety"));
        Restore = new BackupRestoreSession(backup, restoreRuntime ?? new MauiBackupRestoreRuntime(backup, autoPost, reminders, appLock, localization,
            services.GetRequiredService<ZananceStore>(), _safety, () => DialogPage, () => _returnToOnboarding?.Invoke() ?? Task.CompletedTask,
                services.GetService<IBackupSafetyCopyMaintenance>()),
            translator, dates, localization, time, () => !IsBusy);
        Restore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BackupRestoreSession.IsBusy))
            {
                // Preserve an outer cloud/file read's busy lifetime while the inner restore publishes its state (D-167).
                if (Restore.IsBusy) { _restorePriorBusy = IsBusy; IsBusy = true; } else IsBusy = _restorePriorBusy;
            }
            if (e.PropertyName == nameof(BackupRestoreSession.HasCompleted))
            { OnPropertyChanged(nameof(CanCreateBackup)); OnPropertyChanged(nameof(CanChooseAutomatic)); }
            OnPropertyChanged(nameof(CanUseBackupTools)); OnPropertyChanged(nameof(CanLeave));
        };
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy)) { Restore.OwnerStateChanged(); OnPropertyChanged(nameof(CanUseBackupTools)); OnPropertyChanged(nameof(CanLeave)); }
            if (e.PropertyName == nameof(IsRestoreOnly)) Restore.IsOnboarding = IsRestoreOnly;
        };
        Password = string.Empty;
        PasswordConfirm = string.Empty;
        UsePassword = Preferences.Default.Get(PasswordProtectionKey, true);
        _initializingProtection = false;
        LastBackupText = string.Empty;
    }

    public ObservableCollection<LocalBackup> LocalBackups { get; } = [];

    [ObservableProperty]
    public partial string LastBackupText { get; set; }

    [ObservableProperty]
    public partial bool UsePassword { get; set; }

    partial void OnUsePasswordChanged(bool value)
    {
        // Remember only the choice on this device, never the password. New installations default to encryption.
        if (!_initializingProtection) Preferences.Default.Set(PasswordProtectionKey, value);
        if (!value)
        {
            Password = string.Empty;
            PasswordConfirm = string.Empty;
        }
    }

    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial string PasswordConfirm { get; set; }

    [ObservableProperty]
    public partial string? CreateError { get; set; }

    [ObservableProperty]
    public partial string? CreateResult { get; set; }

    [ObservableProperty]
    public partial bool HasLocalBackups { get; set; }

    public async Task LoadAsync()
    {
#if DEBUG
        if (IndependentRestoreReview) { LastBackupText = _translator["Backup_Never"]; return; }
#endif
        LastBackupText = _backup.LastBackupAt is { } last
            ? _translator.Format("Backup_LastPortable", FormatTime(last))
            : _translator["Backup_PortableNever"];

        LocalBackups.Clear();
        foreach (var file in await _backup.DiscoverBackupsAsync(_local))
        {
            var created = BackupFileName.TryParse(file.FileName, out _, out var at) ? at : file.CreatedAt ?? _time.GetUtcNow();
            LocalBackups.Add(new LocalBackup(file, FormatTime(created), file.Size is { } size ? FormatSize(size) : string.Empty));
        }

        HasLocalBackups = LocalBackups.Count > 0;
        await RefreshAutomaticAsync();
#if DEBUG
        if (IndependentAutomaticReview) return;
#endif
        await LoadCloudAsync();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!CanUseBackupTools || !CanCreateBackup) return;
        CreateError = null;
        CreateResult = null;
        if (UsePassword && PasswordProblem() is { } problem)
        {
            CreateError = problem;
            return;
        }

        if (!await _lock.ConfirmAsync(_translator["Lock_ConfirmBackup"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var file = await _backup.CreateBackupAsync(_local, UsePassword ? Password : null);
            Password = string.Empty;
            PasswordConfirm = string.Empty;
            CreateResult = _translator["Backup_Created"];
            await LoadAsync();
            await ShareAsync(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BackupException)
        {
            CreateError = _translator["Backup_Error_Storage"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? PasswordProblem() =>
        Password.Length < MinimumPasswordLength ? _translator.Format("Backup_PasswordTooShort", MinimumPasswordLength)
        : Password != PasswordConfirm ? _translator["Backup_PasswordMismatch"]
        : null;

    [RelayCommand]
    private async Task OpenLocalAsync(LocalBackup backup)
    {
        if (!CanUseBackupTools) return;
        var share = _translator["Backup_Share"];
        var restore = _translator["Backup_Restore"];
        var choice = IsRestoreOnly || backup.DeviceOnly ? restore : await DialogPage.DisplayActionSheetAsync(backup.Title, _translator["Common_Cancel"], null, share, restore);
        if (choice == share)
        {
            await ShareAsync(backup.File);
        }
        else if (choice == restore)
        {
            try
            {
                await using var stream = await (backup.DeviceOnly ? _safety : _local).OpenReadAsync(backup.File.Id);
                await PrepareRestoreAsync(stream, backup.Title);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BackupException)
            {
                RestoreError = _translator["Backup_Error_Storage"];
            }
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        if (!CanUseBackupTools) return;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = _translator["Backup_PickFile"] });
            if (result is null)
            {
                return;
            }

            await using var stream = await result.OpenReadAsync();
            await PrepareRestoreAsync(stream, result.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PermissionException)
        {
            RestoreError = _translator["Backup_Error_Storage"];
        }
    }

    // All local, picked and cloud streams share the one actual non-UI restore flow.
    private Task PrepareRestoreAsync(Stream stream, string name) => Restore.PrepareAsync(stream, name);

    private async Task ShareAsync(BackupFileInfo file)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "backups", file.FileName);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = _translator["Backup_ShareTitle"],
            File = new ShareFile(path, "application/octet-stream"),
        });
    }

    private string FormatTime(DateTimeOffset value) =>
        $"{_dates.Format(value, DateFormatStyle.Long)} {value.ToLocalTime().ToString("t", _localization.CurrentCulture)}";

    private string FormatSize(long bytes) => bytes < 1024 * 1024
        ? _translator.Format("Backup_SizeKb", Math.Max(1, bytes / 1024))
        : _translator.Format("Backup_SizeMb", Math.Round(bytes / 1024d / 1024d, 1));
}
