using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Authentication;
using Vafadar.Backup;
using Vafadar.Backup.GoogleDrive;
using Vafadar.Backup.OneDrive;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>A cloud storage the user can connect: their own Google Drive or OneDrive (D-35).</summary>
public sealed partial class CloudAccountRow(ExternalIdentityProvider provider, string title) : ObservableObject
{
    public ExternalIdentityProvider Provider { get; } = provider;

    public string Title { get; } = title;

    /// <summary>Gets or sets the signed-in account (e-mail), or <see langword="null"/> when not connected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    public partial string? Account { get; set; }

    public bool IsConnected => Account is not null;

    /// <summary>Gets or sets when a backup was last stored there, e.g. "Last backup: 29 September 2026 21:40" (BAK-08).</summary>
    [ObservableProperty]
    public partial string? LastBackupText { get; set; }

    /// <summary>Gets the discovered files for this destination across all local profile sets.</summary>
    public ObservableCollection<CloudBackup> Backups { get; } = [];

    /// <summary>Gets or sets the loading, empty or completed listing feedback.</summary>
    [ObservableProperty]
    public partial string? Status { get; set; }

    /// <summary>Gets or sets a destination-specific listing failure.</summary>
    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>Gets whether this destination has discovered backups.</summary>
    [ObservableProperty]
    public partial bool HasBackups { get; set; }
}

/// <summary>A backup file in a connected cloud storage.</summary>
public sealed record CloudBackup(ExternalIdentityProvider Provider, BackupFileInfo File, string Title, string Details);

/// <summary>
/// Cloud backup (D-35): an explicit choice of the user. Connecting asks for the app's own folder only
/// (<c>drive.appdata</c>, <c>Files.ReadWrite.AppFolder</c>); password protection is optional (D-62).
/// Connected destinations are listed automatically; nothing is uploaded automatically.
/// </summary>
public sealed partial class BackupViewModel
{
    private readonly Vafadar.Authentication.Maui.CloudSignIn _cloud;
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _listingGate = new(1, 1);

    public ObservableCollection<CloudAccountRow> CloudAccounts { get; } = [];

    public ObservableCollection<CloudBackup> CloudBackups { get; } = [];

    [ObservableProperty]
    public partial bool HasCloud { get; set; }

    [ObservableProperty]
    public partial bool HasCloudBackups { get; set; }

    [ObservableProperty]
    public partial string? CloudError { get; set; }

    [ObservableProperty]
    public partial string? CloudResult { get; set; }

    private async Task LoadCloudAsync()
    {
        HasCloud = _cloud.IsAnyAvailable;
        if (!HasCloud)
        {
            return;
        }

        if (CloudAccounts.Count == 0)
        foreach (var provider in new[] { ExternalIdentityProvider.Google, ExternalIdentityProvider.Microsoft }.Where(_cloud.IsAvailable))
        {
            // A damaged token cache means "not connected", never a backup page that does not open.
            ExternalAccount? account = null;
            try
            {
                account = await SignIn(provider).GetCurrentAccountAsync();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud account not read: {ex.GetType().Name}");
            }

            var row = new CloudAccountRow(provider, _translator[provider == ExternalIdentityProvider.Google ? "Cloud_GoogleDrive" : "Cloud_OneDrive"])
            {
                Account = account is null ? null : account.Email ?? account.DisplayName ?? account.Id,
            };
            ShowLastCloudBackup(row);
            CloudAccounts.Add(row);
        }

        var wasBusy = IsBusy;
        IsBusy = true;
        try
        {
            foreach (var row in CloudAccounts.Where(r => r.IsConnected))
            {
                await ListCloudAsync(row);
            }
        }
        finally
        {
            IsBusy = wasBusy;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync(CloudAccountRow row)
    {
        CloudError = null;

        // The user decides to go online: what is stored where, said before the provider's sign-in opens.
        if (!await DialogPage.DisplayAlertAsync(row.Title, _translator.Format("Cloud_ConnectMessage", row.Title), _translator["Cloud_Connect"], _translator["Common_Cancel"]))
        {
            return;
        }

        try
        {
            var account = await SignIn(row.Provider).SignInAsync(Scopes(row.Provider));
            row.Account = account.Email ?? account.DisplayName ?? account.Id;
        }
        catch (OperationCanceledException)
        {
            // The user closed the sign-in.
            return;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"Cloud sign-in failed: {ex.GetType().Name}");
            CloudError = _translator.Format("Cloud_Error_SignIn", row.Title);
            return;
        }

        // Connected; listing may still fail (e.g. offline), which is reported as such.
        IsBusy = true;
        try
        {
            await ListCloudAsync(row);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(CloudAccountRow row)
    {
        if (!await DialogPage.DisplayAlertAsync(row.Title, _translator.Format("Cloud_DisconnectMessage", row.Title), _translator["Cloud_Disconnect"], _translator["Common_Cancel"]))
        {
            return;
        }

        await SignIn(row.Provider).SignOutAsync();
        row.Account = null;
        row.LastBackupText = null;
        row.Status = null;
        row.Error = null;
        RemoveCloudBackups(row.Provider);
    }

    [RelayCommand]
    private async Task BackupToCloudAsync(CloudAccountRow row)
    {
        CloudError = null;
        CloudResult = null;

        // D-62: the same optional file protection applies to local and connected cloud destinations.
        if (UsePassword && PasswordProblem() is { } problem)
        {
            CloudError = problem;
            return;
        }

        if (!await _lock.ConfirmAsync(_translator["Lock_ConfirmBackup"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _backup.CreateBackupAsync(Storage(row.Provider), UsePassword ? Password : null);
            Password = string.Empty;
            PasswordConfirm = string.Empty;
            CloudResult = _translator.Format("Cloud_Uploaded", row.Title);
            Preferences.Default.Set(LastCloudKey(row.Provider), _time.GetUtcNow().ToUnixTimeSeconds());
            ShowLastCloudBackup(row);
            LastBackupText = _backup.LastBackupAt is { } last ? _translator.Format("Backup_LastBackup", FormatTime(last)) : LastBackupText;
            await ListCloudAsync(row);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowCloudBackupsAsync(CloudAccountRow row)
    {
        CloudError = null;
        IsBusy = true;
        try
        {
            await ListCloudAsync(row);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // A cloud backup is downloaded and then restored like a file: password, preview, confirmation (BAK-09). Deleting it
    // is a separate action from disconnecting and from deleting the data on the device (BAK-15).
    [RelayCommand]
    private async Task OpenCloudAsync(CloudBackup backup)
    {
        CloudError = null;
        var restore = _translator["Backup_Restore"];
        var delete = _translator["Common_Delete"];
        var choice = IsRestoreOnly ? restore : await DialogPage.DisplayActionSheetAsync(backup.Title, _translator["Common_Cancel"], null, restore, delete);
        if (choice == delete)
        {
            await DeleteCloudAsync(backup);
            return;
        }

        if (choice != restore)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = await Storage(backup.Provider).OpenReadAsync(backup.File.Id);
            await PrepareRestoreAsync(stream, backup.Title);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(CloudAccounts.First(a => a.Provider == backup.Provider), ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteCloudAsync(CloudBackup backup)
    {
        var row = CloudAccounts.First(a => a.Provider == backup.Provider);
        if (!await DialogPage.DisplayAlertAsync(_translator["Cloud_DeleteTitle"], _translator.Format("Cloud_DeleteMessage", backup.Title), _translator["Common_Delete"], _translator["Common_Cancel"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Storage(backup.Provider).DeleteAsync(backup.File.Id);
            CloudBackups.Remove(backup);
            row.Backups.Remove(backup);
            row.HasBackups = row.Backups.Count > 0;
            row.Status = row.Backups.Count == 0 ? _translator["Cloud_NoBackups"] : _translator.Format("Cloud_BackupsFound", row.Backups.Count);
            HasCloudBackups = CloudBackups.Count > 0;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowLastCloudBackup(CloudAccountRow row)
    {
        var seconds = Preferences.Default.Get(LastCloudKey(row.Provider), 0L);
        row.LastBackupText = seconds > 0 && row.IsConnected
            ? _translator.Format("Backup_LastBackup", FormatTime(DateTimeOffset.FromUnixTimeSeconds(seconds)))
            : null;
    }

    // Per local profile: every profile has its own backup set in the shared cloud folder (D-34).
    private static string LastCloudKey(ExternalIdentityProvider provider) =>
        Vafadar.Zanance.App.Profiles.ProfileService.CurrentBackupSet() is { } set ? $"backup.cloud.last.{provider}.{set}" : $"backup.cloud.last.{provider}";

    private async Task ListCloudAsync(CloudAccountRow row)
    {
        await _listingGate.WaitAsync();
        row.Error = null;
        row.Status = _translator["Cloud_LoadingBackups"];
        try
        {
            var files = await _backup.DiscoverBackupsAsync(Storage(row.Provider));
            ApplyCloudFiles(row, files);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            row.Status = null;
            row.Error = CloudProblem(row, ex);
        }
        finally
        {
            _listingGate.Release();
        }
    }

    /// <summary>Applies a completed listing; shared with fictitious rendered-state checks (D-67).</summary>
    internal void ApplyCloudFiles(CloudAccountRow row, IReadOnlyList<BackupFileInfo> files)
    {
        RemoveCloudBackups(row.Provider);
        foreach (var file in files)
        {
            var created = BackupFileName.TryParse(file.FileName, out var appId, out var at) ? at : file.CreatedAt ?? _time.GetUtcNow();
            var profile = _translator[appId?.Contains("~", StringComparison.Ordinal) == true ? "Cloud_OtherProfile" : "Profile_Main"];
            var backup = new CloudBackup(row.Provider, file, FormatTime(created),
                $"{profile} · {(file.Size is { } size ? FormatSize(size) : string.Empty)}");
            CloudBackups.Add(backup);
            row.Backups.Add(backup);
        }

        HasCloudBackups = CloudBackups.Count > 0;
        row.HasBackups = row.Backups.Count > 0;
        row.Status = files.Count == 0 ? _translator["Cloud_NoBackups"] : _translator.Format("Cloud_BackupsFound", files.Count);
    }

    private void RemoveCloudBackups(ExternalIdentityProvider provider)
    {
        CloudAccounts.FirstOrDefault(row => row.Provider == provider)?.Backups.Clear();
        if (CloudAccounts.FirstOrDefault(row => row.Provider == provider) is { } account)
        {
            account.HasBackups = false;
        }
        foreach (var backup in CloudBackups.Where(b => b.Provider == provider).ToList())
        {
            CloudBackups.Remove(backup);
        }

        HasCloudBackups = CloudBackups.Count > 0;
    }

    private string CloudProblem(CloudAccountRow row, Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"Cloud backup failed: {exception.GetType().Name}");
        switch (exception)
        {
            case AuthenticationRequiredException:
                row.Account = null;
                RemoveCloudBackups(row.Provider);
                return _translator.Format("Cloud_Error_SignInAgain", row.Title);
            case HttpRequestException or TaskCanceledException:
                return _translator.Format("Cloud_Error_Offline", row.Title);
            case BackupException backup:
                return _translator[$"Backup_Error_{backup.Error}"];
            default:
                return _translator["Backup_Error_Storage"];
        }
    }

    private IExternalSignInService SignIn(ExternalIdentityProvider provider) => _services.GetRequiredKeyedService<IExternalSignInService>(provider);

    private IBackupStorage Storage(ExternalIdentityProvider provider) => provider == ExternalIdentityProvider.Google
        ? _services.GetRequiredService<GoogleDriveBackupStorage>()
        : _services.GetRequiredService<OneDriveBackupStorage>();

    private static IReadOnlyCollection<string> Scopes(ExternalIdentityProvider provider) =>
        provider == ExternalIdentityProvider.Google ? GoogleDriveScopes.All : OneDriveScopes.All;
}
