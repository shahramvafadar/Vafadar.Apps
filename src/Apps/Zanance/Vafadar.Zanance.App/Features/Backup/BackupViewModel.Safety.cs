using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

public sealed partial class BackupViewModel
{
    /// <summary>Gets current-profile device-only pre-restore candidates, separately from automatic copies and portable exports.</summary>
    public ObservableCollection<LocalBackup> SafetyBackups { get; } = [];
    /// <summary>Gets whether pre-restore candidates are available for full verification through the original restore flow.</summary>
    [ObservableProperty] public partial bool HasSafetyBackups { get; set; }

    private async Task RefreshSafetyAsync()
    {
#if DEBUG
        if (IndependentAutomaticReview || IndependentRestoreReview) return;
#endif
        var owner = Restore.CapturePreparationOwner(); CheckCreationFrame(owner);
        IReadOnlyList<BackupFileInfo> files = [];
        if (_services.GetService<IBackupSafetyCopyDiscovery>() is { } discovery)
            files = await discovery.ListSafetyCopiesAsync(owner.Token);
        CheckCreationFrame(owner); SafetyBackups.Clear();
        foreach (var file in files)
                SafetyBackups.Add(new(file, _translator.Format("Backup_SafetyFile", FormatTime(file.CreatedAt ?? _time.GetUtcNow())),
                    file.Size is { } size ? FormatSize(size) : string.Empty, DeviceOnly: true));
        HasSafetyBackups = SafetyBackups.Count != 0;
    }
}
