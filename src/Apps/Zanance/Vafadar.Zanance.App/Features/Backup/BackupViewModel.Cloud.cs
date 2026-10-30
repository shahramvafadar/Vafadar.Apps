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
    /// <summary>Gets the original provider identity independently of its mutable display label.</summary>
    internal ExternalAccount? Identity { get; set; }
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
    [NotifyPropertyChangedFor(nameof(HasCoveredPortableFeedback), nameof(HasBackupFeedback))]
    public partial string? CloudResult { get; set; }

    private async Task LoadCloudAsync()
    {
        var owner = Restore.CapturePreparationOwner(); CheckCreationFrame(owner);
        HasCloud = new[] { ExternalIdentityProvider.Google, ExternalIdentityProvider.Microsoft }.Any(_workspaceRuntime.IsAvailable);
        if (!HasCloud)
        {
            CloudAccounts.Clear(); CloudBackups.Clear(); HasCloudBackups = false; return;
        }

        var rows = new List<CloudAccountRow>();
        foreach (var provider in new[] { ExternalIdentityProvider.Google, ExternalIdentityProvider.Microsoft }.Where(_workspaceRuntime.IsAvailable))
        {
            // A damaged token cache means "not connected", never a backup page that does not open.
            ExternalAccount? account = null;
            try
            {
                account = await SignIn(provider).GetCurrentAccountAsync(owner.Token);
                CheckCreationFrame(owner);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Cloud account not read: {ex.GetType().Name}");
            }

            CheckCreationFrame(owner);
            var row = new CloudAccountRow(provider, _translator[provider == ExternalIdentityProvider.Google ? "Cloud_GoogleDrive" : "Cloud_OneDrive"])
            {
                Identity = account,
                Account = account is null ? null : account.Email ?? account.DisplayName ?? account.Id,
            };
            ShowLastCloudBackup(row);
            rows.Add(row);
        }

        CheckCreationFrame(owner); CloudBackups.Clear(); CloudAccounts.Clear();
        foreach (var row in rows) CloudAccounts.Add(row);
        HasCloudBackups = false;
        try
        {
            foreach (var row in CloudAccounts.Where(r => r.IsConnected).ToArray())
            {
                CheckCreationFrame(owner);
                await ListCloudAsync(row);
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException && !IsOriginalBackupFrameCurrent(owner)) { }
    }

    [RelayCommand]
    private async Task ConnectAsync(CloudAccountRow row)
    {
        if (!CanUseBackupTools || row.IsConnected || !CloudAccounts.Any(original => ReferenceEquals(original, row))) return;
        var (owner, current) = CaptureDestination(row);
        var request = new BackupDestinationRequest(SignIn(row.Provider), Storage(row.Provider), current, owner.Token, row.Identity);
        CloudError = null; BeginWorkspaceCommand();
        try
        {
            // The original disclosure precedes the provider's genuine native sign-in; credentials never pass through this page.
            var account = await request.ConnectAsync(() => _workspaceRuntime.ConfirmAsync(row.Title,
                _translator.Format("Cloud_ConnectMessage", row.Title), _translator["Cloud_Connect"], _translator["Common_Cancel"]), Scopes(row.Provider));
            if (account is null) return;
            await request.EnsurePublicationCurrentAsync();
            current();
            row.Identity = account; row.Account = account.Email ?? account.DisplayName ?? account.Id;
            // Accepted sign-in is not repeated if its subsequent listing fails.
            await ListCloudAsync(row);
        }
        catch (BackupDestinationChangedException ex) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException ex) when (request.ConnectedAccount is not null) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not OutOfMemoryException && !IsOriginalBackupFrameCurrent(owner)) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (request.ConnectedAccount is null)
            {
                if (IsOriginalDestinationCurrent(current)) CloudError = _translator.Format("Cloud_Error_SignIn", row.Title);
            }
            else if (CloudAccounts.Any(original => ReferenceEquals(original, row))) CloudError = CloudProblem(row, ex);
        }
        finally { EndWorkspaceCommand(); }
    }

    [RelayCommand]
    private async Task DisconnectAsync(CloudAccountRow row)
    {
        if (!CanUseBackupTools || !row.IsConnected || !CloudAccounts.Any(original => ReferenceEquals(original, row))) return;
        var (owner, current) = CaptureDestination(row);
        var request = new BackupDestinationRequest(SignIn(row.Provider), Storage(row.Provider), current, owner.Token, row.Identity);
        CloudError = null; BeginWorkspaceCommand();
        try
        {
            if (!await request.DisconnectAsync(() => _workspaceRuntime.ConfirmAsync(row.Title,
                _translator.Format("Cloud_DisconnectMessage", row.Title), _translator["Cloud_Disconnect"], _translator["Common_Cancel"]))) return;
            await request.EnsurePublicationCurrentAsync();
            current(); row.Identity = null; row.Account = null; row.LastBackupText = null; row.Status = null; row.Error = null;
            RemoveCloudBackups(row.Provider);
        }
        catch (BackupDestinationChangedException ex) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException ex) when (request.Disconnected) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not OutOfMemoryException && !IsOriginalDestinationCurrent(current)) { }
        catch (Exception ex) when (ex is not OutOfMemoryException) { CloudError = CloudProblem(row, ex); }
        finally { EndWorkspaceCommand(); }
    }

    [RelayCommand]
    private async Task BackupToCloudAsync(CloudAccountRow row)
    {
        if (!CanUseBackupTools || !CanCreateBackup || !CloudAccounts.Any(item => ReferenceEquals(item, row)) || !row.IsConnected) return;
        CloudError = null; CloudResult = null;
        if (UsePassword && PasswordProblem() is { } problem) { CloudError = problem; return; }
        var owner = Restore.CapturePreparationOwner(); var input = CaptureCreationInput();
        var account = row.Account; var originalLastKey = LastCloudKey(row.Provider);
        void CheckDestination()
        {
            if (!CloudAccounts.Any(item => ReferenceEquals(item, row)) || row.Account != account || !row.IsConnected)
                throw new OperationCanceledException("The original portable destination changed.", owner.Token);
        }
        var request = CreationRequest(BoundStorage(row, CheckDestination), input, owner, CheckDestination);
        BeginPortableCreation();
        try
        {
            var file = await request.RunAsync(ConfirmCreationAsync);
            if (file is null) return;
            ConsumeOnlyOriginalCreationInput(input);
            CloudResult = _translator.Format("Cloud_Uploaded", row.Title);
            // Original profile metadata cannot borrow a different profile after an accepted upload.
            Preferences.Default.Set(originalLastKey, _time.GetUtcNow().ToUnixTimeSeconds());
            if (!owner.IsCurrent) return;
            CheckCreationFrame(owner); CheckDestination(); ShowLastCloudBackup(row);
            LastBackupText = _backup.LastBackupAt is { } last ? _translator.Format("Backup_LastBackup", FormatTime(last)) : LastBackupText;
            await ListCloudAsync(row);
        }
        catch (BackupDestinationChangedException ex) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not OutOfMemoryException && !owner.IsCurrent) { }
        catch (Exception ex) when (ex is not OutOfMemoryException) { CloudError = CloudProblem(row, ex); }
        finally { EndPortableCreation(); }
    }

    [RelayCommand]
    private async Task ShowCloudBackupsAsync(CloudAccountRow row)
    {
        if (!CanUseBackupTools || !row.IsConnected || !CloudAccounts.Any(original => ReferenceEquals(original, row))) return;
        var (_, current) = CaptureDestination(row);
        CloudError = null; BeginWorkspaceCommand();
        try
        {
            await ListCloudAsync(row);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && !IsOriginalDestinationCurrent(current)) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally { EndWorkspaceCommand(); }
    }

    // A cloud backup is downloaded and then restored like a file: password, preview, confirmation (BAK-09). Deleting it
    // is a separate action from disconnecting and from deleting the data on the device (BAK-15).
    [RelayCommand]
    private async Task OpenCloudAsync(CloudBackup backup)
    {
        if (!CanUseBackupTools || !CloudBackups.Any(original => ReferenceEquals(original, backup))) return;
        var row = CloudAccounts.FirstOrDefault(original => original.Provider == backup.Provider && original.IsConnected);
        if (row is null) return;
        var (owner, current) = CaptureDestination(row, backup);
        CloudError = null; BeginWorkspaceCommand();
        var restore = _translator["Backup_Restore"];
        var delete = _translator["Common_Delete"];
        try
        {
            var choice = IsRestoreOnly ? restore : await _workspaceRuntime.ChooseAsync(backup.Title, _translator["Common_Cancel"], restore, delete);
            current();
            if (choice == delete)
            {
                await DeleteCloudAsync(backup, row, owner, current); return;
            }
            if (choice != restore) return;
            await using var stream = await BoundStorage(row, current).OpenReadAsync(backup.File.Id, owner.Token);
            current(); await Restore.PrepareAsync(stream, backup.Title, owner);
        }
        catch (BackupDestinationChangedException ex) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is not OutOfMemoryException && !IsOriginalDestinationCurrent(current)) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            CloudError = CloudProblem(row, ex);
        }
        finally { EndWorkspaceCommand(); }
    }

    private async Task DeleteCloudAsync(CloudBackup backup, CloudAccountRow row,
        BackupRestoreSession.PreparationOwner owner, Action current)
    {
        HasDeletedBackupReceipt = false;
        var request = new BackupDestinationRequest(SignIn(row.Provider), BoundStorage(row, current), current, owner.Token, row.Identity);
        try
        {
            if (!await request.DeleteAsync(backup.File, () => _workspaceRuntime.ConfirmAsync(_translator["Cloud_DeleteTitle"],
                _translator.Format("Cloud_DeleteMessage", backup.Title), _translator["Common_Delete"], _translator["Common_Cancel"]))) return;
            HasDeletedBackupReceipt = true;
            await request.EnsurePublicationCurrentAsync();
            current(); CloudBackups.Remove(backup); row.Backups.Remove(backup);
            row.HasBackups = row.Backups.Count > 0;
            row.Status = row.Backups.Count == 0 ? _translator["Cloud_NoBackups"] : _translator.Format("Cloud_BackupsFound", row.Backups.Count);
            HasCloudBackups = CloudBackups.Count > 0;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && request.DeletedFile is not null)
        { await RequireWorkspaceReloadAsync(owner, ex); }
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
        var owner = Restore.CapturePreparationOwner(); var account = row.Account; var identity = row.Identity;
        void Current()
        {
            CheckCreationFrame(owner);
            if (!CloudAccounts.Any(original => ReferenceEquals(original, row)) || row.Account != account || row.Identity != identity || !row.IsConnected)
                throw new OperationCanceledException("The original backup discovery destination changed.", owner.Token);
        }
        Current(); await _listingGate.WaitAsync(owner.Token);
        try
        {
            Current(); row.Error = null; row.Status = _translator["Cloud_LoadingBackups"];
            var files = await _workspaceRuntime.DiscoverAsync(_backup, BoundStorage(row, Current), owner.Token); Current();
            ApplyCloudFiles(row, files);
        }
        catch (BackupDestinationChangedException ex) { await RequireWorkspaceReloadAsync(owner, ex); }
        catch (OperationCanceledException) when (!IsOriginalDestinationCurrent(Current) || owner.Token.IsCancellationRequested) { }
        catch (Exception error) when (error is not OutOfMemoryException && !IsOriginalBackupFrameCurrent(owner)) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Current();
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
                row.Identity = null;
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

    private IExternalSignInService SignIn(ExternalIdentityProvider provider) => _workspaceRuntime.SignIn(provider);

    private IBackupStorage Storage(ExternalIdentityProvider provider) => _workspaceRuntime.Storage(provider);

    private static IReadOnlyCollection<string> Scopes(ExternalIdentityProvider provider) =>
        provider == ExternalIdentityProvider.Google ? GoogleDriveScopes.All : OneDriveScopes.All;
}
