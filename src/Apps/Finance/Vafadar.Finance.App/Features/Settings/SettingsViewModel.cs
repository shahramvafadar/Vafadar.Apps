using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Core.Hosting;
using Vafadar.Finance.App.Reminders;
using Vafadar.Finance.App.Security;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Settings;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly IAppEnvironment _app;
    private readonly FinanceStore _store;
    private readonly ReminderService _reminders;
    private readonly AppLockService _lock;
    private bool _refreshing;

    public SettingsViewModel(
        ILocalizationService localization,
        Translator translator,
        IDateFormatter dates,
        TimeProvider time,
        IAppEnvironment app,
        FinanceStore store,
        ReminderService reminders,
        AppLockService appLock)
    {
        _reminders = reminders;
        _lock = appLock;
        ModeNames = [translator["Mode_Simple"], translator["Mode_Advanced"]];
        _localization = localization;
        _translator = translator;
        _dates = dates;
        _time = time;
        _app = app;
        _store = store;

        Languages = [.. localization.SupportedLanguages];
        ReportCurrency = string.Empty;
        ReminderDaysText = "3";
        NotificationsSupported = reminders.Scheduler.IsSupported;
        Refresh();
    }

    public AppLanguage[] Languages { get; }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial string ReportCurrency { get; set; }

    public IReadOnlyList<string> ModeNames { get; }

    [ObservableProperty]
    public partial int ModeIndex { get; set; }

    [ObservableProperty]
    public partial bool LockAvailable { get; set; }

    [ObservableProperty]
    public partial bool LockEnabled { get; set; }

    [ObservableProperty]
    public partial bool NotificationsSupported { get; set; }

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    [ObservableProperty]
    public partial string ReminderDaysText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? ReminderTime { get; set; }

    /// <summary>Loads the finance settings.</summary>
    public async Task LoadAsync()
    {
        _refreshing = true;
        try
        {
            var settings = await _store.GetSettingsAsync();
            ReportCurrency = settings.ReportCurrencyCode;
            ShowDetails = settings.NotificationsShowDetails;
            ModeIndex = (int)settings.Mode;
            LockEnabled = settings.AppLockEnabled;
            LockAvailable = settings.AppLockEnabled || await _lock.Authenticator.IsAvailableAsync();
            ReminderDaysText = settings.ReminderDaysBefore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ReminderTime = settings.ReminderTime.ToTimeSpan();
            NotificationsEnabled = NotificationsSupported && await _reminders.Scheduler.AreEnabledAsync();
        }
        finally
        {
            _refreshing = false;
        }

        Refresh();
    }

    async partial void OnReportCurrencyChanged(string value)
    {
        if (_refreshing || string.IsNullOrEmpty(value))
        {
            return;
        }

        var settings = await _store.GetSettingsAsync();
        if (settings.ReportCurrencyCode != value)
        {
            settings.ReportCurrencyCode = value;
            await _store.SaveSettingsAsync(settings);
        }
    }

    // Simple and Advanced show the same data and calculations; switching never removes anything (UX-01, UX-02).
    async partial void OnModeIndexChanged(int value)
    {
        if (_refreshing)
        {
            return;
        }

        var settings = await _store.GetSettingsAsync();
        settings.Mode = (Core.Settings.ExperienceMode)value;
        await _store.SaveSettingsAsync(settings);
    }

    /// <summary>Turns the app lock on or off after the device owner confirmed it (SEC-01).</summary>
    public async Task SetLockAsync(bool enabled)
    {
        if (_refreshing || enabled == _lock.IsEnabled)
        {
            return;
        }

        var result = await _lock.SetEnabledAsync(enabled);
        _refreshing = true;
        LockEnabled = result;
        _refreshing = false;
    }

    partial void OnShowDetailsChanged(bool value) => _ = SaveNotificationSettingsAsync();

    partial void OnReminderDaysTextChanged(string value) => _ = SaveNotificationSettingsAsync();

    partial void OnReminderTimeChanged(TimeSpan? value) => _ = SaveNotificationSettingsAsync();

    // Reminder defaults apply to new plans; the lock-screen choice applies to all notifications (REM-01, REM-05).
    private async Task SaveNotificationSettingsAsync()
    {
        if (_refreshing)
        {
            return;
        }

        var settings = await _store.GetSettingsAsync();
        settings.NotificationsShowDetails = ShowDetails;
        if (int.TryParse(Vafadar.Core.Text.Digits.ToAscii(ReminderDaysText), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var days))
        {
            settings.ReminderDaysBefore = Math.Clamp(days, 0, 60);
        }

        if (ReminderTime is { } time)
        {
            settings.ReminderTime = TimeOnly.FromTimeSpan(time);
        }

        await _store.SaveSettingsAsync(settings);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task EnableNotificationsAsync() => NotificationsEnabled = await _reminders.EnsurePermissionAsync();

    [ObservableProperty]
    public partial AppLanguage? SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial CalendarOption[] Calendars { get; set; }

    [ObservableProperty]
    public partial CalendarOption? SelectedCalendar { get; set; }

    [ObservableProperty]
    public partial string CalendarPreview { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; }

    partial void OnSelectedLanguageChanged(AppLanguage? value)
    {
        if (_refreshing || value is null || value == _localization.CurrentLanguage)
        {
            return;
        }

        _localization.SetLanguage(value);
        Refresh();
    }

    partial void OnSelectedCalendarChanged(CalendarOption? value)
    {
        if (_refreshing || value is null || value.Calendar == _localization.CurrentCalendar)
        {
            return;
        }

        _localization.SetCalendar(value.Calendar);
        Refresh();
    }

    // Rebuilds everything that contains translated or formatted text for the current language and calendar.
    [MemberNotNull(nameof(Calendars), nameof(CalendarPreview), nameof(VersionText))]
    private void Refresh()
    {
        _refreshing = true;
        try
        {
            Calendars =
            [
                new CalendarOption(CalendarSystem.Gregorian, _translator["Calendar_Gregorian"]),
                new CalendarOption(CalendarSystem.Persian, _translator["Calendar_Persian"]),
            ];
            SelectedCalendar = Calendars.First(option => option.Calendar == _localization.CurrentCalendar);
            SelectedLanguage = _localization.CurrentLanguage;
            CalendarPreview = _dates.Format(DateOnly.FromDateTime(_time.GetLocalNow().DateTime), DateFormatStyle.Long);
            VersionText = _translator.Format("Settings_Version", _app.Version);
        }
        finally
        {
            _refreshing = false;
        }
    }
}

/// <summary>A calendar choice with its display name in the current language.</summary>
public sealed record CalendarOption(CalendarSystem Calendar, string DisplayName)
{
    public override string ToString() => DisplayName;
}
