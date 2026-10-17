using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

public sealed partial class BackupViewModel
{
    private bool _publishingAutomatic;
    private AutomaticBackupCoordinator? Automatic => _services.GetService<AutomaticBackupCoordinator>();

    /// <summary>Gets the current translated interval captions independently of portable password controls.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> AutomaticIntervals { get; set; } = [];
    /// <summary>Gets the current profile's private recovery points; these rows offer restoration without sharing.</summary>
    public ObservableCollection<LocalBackup> AutomaticBackups { get; } = [];
    /// <summary>Gets whether protected native automatic-backup configuration is available.</summary>
    public bool CanChooseAutomatic => CanCreateBackup && Automatic is not null && (OperatingSystem.IsAndroid() || OperatingSystem.IsWindows());
    /// <summary>Gets the selected off/daily/weekly device preference.</summary>
    [ObservableProperty] public partial int AutomaticIntervalIndex { get; set; } = -1;
    /// <summary>Gets complete translated status, including recoverable creation failure.</summary>
    [ObservableProperty] public partial string AutomaticStatus { get; set; } = string.Empty;
    /// <summary>Gets whether the captured current profile has complete automatic copies to restore.</summary>
    [ObservableProperty] public partial bool HasAutomaticBackups { get; set; }

    partial void OnAutomaticIntervalIndexChanged(int value)
    {
        if (_publishingAutomatic || value is < 0 or > 2 || Automatic is not { } automatic) return;
        automatic.IntervalDays = value == 0 ? 0 : value == 1 ? 1 : 7;
        if (value != 0) _ = Task.Run(() => automatic.RunAsync());
    }

    /// <summary>Attaches only while the actual page is visible, avoiding retired-page subscriptions.</summary>
    internal void WatchAutomatic(bool enabled)
    {
        if (Automatic is not { } automatic) return;
        automatic.Changed -= OnAutomaticChanged;
        _translator.PropertyChanged -= OnAutomaticTranslationChanged;
        if (enabled)
        {
            automatic.Changed += OnAutomaticChanged;
            _translator.PropertyChanged += OnAutomaticTranslationChanged;
        }
    }

    private void OnAutomaticTranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => OnAutomaticChanged(sender, EventArgs.Empty);

    private async void OnAutomaticChanged(object? sender, EventArgs e)
    {
        if (_page is null || !CanUseBackupTools) return;
        await Presentation.Failures.GuardAsync(async () => { await RefreshAutomaticAsync(); await RefreshSafetyAsync(); });
    }

    private async Task RefreshAutomaticAsync()
    {
        if (Automatic is not { } automatic) return;
        var priorPublication = _publishingAutomatic;
        _publishingAutomatic = true;
        try
        {
            var captions = new[] { _translator["Backup_AutoOff"], _translator["Backup_AutoDaily"], _translator["Backup_AutoWeekly"] };
            // Android's live picker must not lose/rebuild its adapter inside its own selection callback.
            // Publish changed translations once, never Clear/Add the same choices after a status notification.
            if (!AutomaticIntervals.SequenceEqual(captions)) AutomaticIntervals = captions;
            AutomaticIntervalIndex = automatic.IntervalDays == 0 ? 0 : automatic.IntervalDays == 7 ? 2 : 1;
            AutomaticStatus = automatic.HasFailure ? _translator["Backup_AutoFailed"]
                : automatic.LastBackupAt is { } at ? _translator.Format("Backup_AutoLast", FormatTime(at)) : _translator["Backup_AutoNever"];
            OnPropertyChanged(nameof(CanChooseAutomatic));
        }
        finally { _publishingAutomatic = priorPublication; }
        // Discovery is profile-specific. Authentication and confirmation still precede actual restoration.
        var set = Vafadar.Zanance.App.Profiles.ProfileService.CurrentBackupSet();
        var files = await _safety.ListAsync();
        AutomaticBackups.Clear();
        foreach (var file in files.Where(file => AutomaticBackupFileName.Matches(file.FileName, set))
            .OrderByDescending(file => file.CreatedAt))
            AutomaticBackups.Add(new(file, _translator.Format("Backup_AutoFile", FormatTime(file.CreatedAt ?? _time.GetUtcNow())),
                file.Size is { } size ? FormatSize(size) : string.Empty, DeviceOnly: true));
        HasAutomaticBackups = AutomaticBackups.Count != 0;
    }
}
