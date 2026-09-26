using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Backup;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Finance.App.Reminders;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Backup;

/// <summary>A backup file kept on this device.</summary>
public sealed record LocalBackup(BackupFileInfo File, string Title, string Details);

/// <summary>
/// Backup and restore (UI-13, BAK-01..12). A backup is an encrypted file the user keeps wherever they like – shared to
/// Google Drive, OneDrive, e-mail or a computer. A restore replaces all data after a preview and a safety copy; it is
/// never a merge (BAK-02).
/// </summary>
public sealed partial class BackupViewModel : ViewModelBase
{
    private const int MinimumPasswordLength = 8;

    private readonly IBackupService _backup;
    private readonly AutoPostProcessor _autoPost;
    private readonly ReminderService _reminders;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly LocalFolderBackupStorage _local;
    private readonly LocalFolderBackupStorage _safety;
    private byte[]? _package;

    public BackupViewModel(IBackupService backup, AutoPostProcessor autoPost, ReminderService reminders, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _reminders = reminders;
        _backup = backup;
        _autoPost = autoPost;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        _local = new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups"));
        _safety = new LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups-safety"));
        Password = string.Empty;
        PasswordConfirm = string.Empty;
        RestorePassword = string.Empty;
        UsePassword = true;
        LastBackupText = string.Empty;
    }

    public ObservableCollection<LocalBackup> LocalBackups { get; } = [];

    [ObservableProperty]
    public partial string LastBackupText { get; set; }

    [ObservableProperty]
    public partial bool UsePassword { get; set; }

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

    // Restore flow: pick → (password) → preview → confirm.
    [ObservableProperty]
    public partial bool NeedsRestorePassword { get; set; }

    [ObservableProperty]
    public partial string RestorePassword { get; set; }

    [ObservableProperty]
    public partial string? RestoreError { get; set; }

    [ObservableProperty]
    public partial string? PreviewText { get; set; }

    [ObservableProperty]
    public partial bool HasPreview { get; set; }

    [ObservableProperty]
    public partial string? PickedName { get; set; }

    public async Task LoadAsync()
    {
        LastBackupText = _backup.LastBackupAt is { } last
            ? _translator.Format("Backup_LastBackup", FormatTime(last))
            : _translator["Backup_Never"];

        LocalBackups.Clear();
        foreach (var file in await _backup.ListBackupsAsync(_local))
        {
            var created = BackupFileName.TryParse(file.FileName, out _, out var at) ? at : file.CreatedAt ?? _time.GetUtcNow();
            LocalBackups.Add(new LocalBackup(file, FormatTime(created), file.Size is { } size ? FormatSize(size) : string.Empty));
        }

        HasLocalBackups = LocalBackups.Count > 0;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        CreateError = null;
        CreateResult = null;
        if (UsePassword)
        {
            if (Password.Length < MinimumPasswordLength)
            {
                CreateError = _translator.Format("Backup_PasswordTooShort", MinimumPasswordLength);
                return;
            }

            if (Password != PasswordConfirm)
            {
                CreateError = _translator["Backup_PasswordMismatch"];
                return;
            }
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

    [RelayCommand]
    private async Task OpenLocalAsync(LocalBackup backup)
    {
        var share = _translator["Backup_Share"];
        var restore = _translator["Backup_Restore"];
        var choice = await Shell.Current.DisplayActionSheetAsync(backup.Title, _translator["Common_Cancel"], null, share, restore);
        if (choice == share)
        {
            await ShareAsync(backup.File);
        }
        else if (choice == restore)
        {
            await using var stream = await _local.OpenReadAsync(backup.File.Id);
            await PrepareRestoreAsync(stream, backup.Title);
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
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

    // Reads the file and shows the password field or the preview; nothing is changed yet (BAK-09, BAK-10).
    private async Task PrepareRestoreAsync(Stream stream, string name)
    {
        ResetRestore();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        _package = buffer.ToArray();
        PickedName = name;
        NeedsRestorePassword = BackupEncryption.IsEncrypted(_package);
        if (!NeedsRestorePassword)
        {
            await CheckAsync();
        }
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (_package is null)
        {
            return;
        }

        RestoreError = null;
        IsBusy = true;
        try
        {
            var manifest = await _backup.InspectPackageAsync(_package, NeedsRestorePassword ? RestorePassword : null);
            PreviewText = Describe(manifest);
            HasPreview = true;
        }
        catch (BackupException ex)
        {
            RestoreError = _translator[$"Backup_Error_{ex.Error}"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (_package is null || !HasPreview)
        {
            return;
        }

        if (!await Shell.Current.DisplayAlertAsync(_translator["Backup_ReplaceTitle"], _translator["Backup_ReplaceMessage"], _translator["Backup_Replace"], _translator["Common_Cancel"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Keep a safety copy of the current data first (BAK-02, BAK-09).
            await _backup.CreateBackupAsync(_safety);
            await _backup.RestorePackageAsync(_package, NeedsRestorePassword ? RestorePassword : null);

            // Due occurrences are processed again; settled ones are not recreated (BAK-11).
            await _autoPost.RunAsync(DateOnly.FromDateTime(_time.GetLocalNow().DateTime));
            await _reminders.RefreshAsync();
            ResetRestore();
            await Shell.Current.DisplayAlertAsync(_translator["Backup_Restored"], _translator["Backup_RestoredMessage"], _translator["Common_Ok"]);
            (Application.Current as App)?.ShowMainShell();
        }
        catch (BackupException ex)
        {
            RestoreError = _translator[$"Backup_Error_{ex.Error}"];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RestoreError = _translator["Backup_Error_Storage"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelRestore() => ResetRestore();

    private void ResetRestore()
    {
        _package = null;
        PickedName = null;
        NeedsRestorePassword = false;
        RestorePassword = string.Empty;
        RestoreError = null;
        PreviewText = null;
        HasPreview = false;
    }

    private async Task ShareAsync(BackupFileInfo file)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "backups", file.FileName);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = _translator["Backup_ShareTitle"],
            File = new ShareFile(path, "application/octet-stream"),
        });
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
                summary.GetValueOrDefault(FinanceBackupSummary.Accounts, "?"),
                summary.GetValueOrDefault(FinanceBackupSummary.Entries, "?"),
                summary.GetValueOrDefault(FinanceBackupSummary.Plans, "?")));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string FormatTime(DateTimeOffset value) =>
        $"{_dates.Format(value, DateFormatStyle.Long)} {value.ToLocalTime().ToString("t", _localization.CurrentCulture)}";

    private string FormatSize(long bytes) => bytes < 1024 * 1024
        ? _translator.Format("Backup_SizeKb", Math.Max(1, bytes / 1024))
        : _translator.Format("Backup_SizeMb", Math.Round(bytes / 1024d / 1024d, 1));
}
