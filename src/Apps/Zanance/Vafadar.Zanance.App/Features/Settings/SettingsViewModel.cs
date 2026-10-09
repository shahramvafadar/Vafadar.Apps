using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Profiles;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Editable preferences published after a complete read; display refreshes preserve unsaved inputs.</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ZananceStore _store;
    private readonly ReminderService _reminders;
    private readonly AppLockService _lock;
    private readonly Presentation.ThemeService _theme;
    private readonly ProfileService _profiles;
    private readonly EstimateDraftState _estimateDraft = new();
    private bool _refreshing;
    private SettingsSnapshot? _snapshot;

    /// <summary>Gets the first-frame/reload cover and retry state for the whole settings form.</summary>
    public Presentation.SnapshotLoadState Loading { get; } = new();

    public SettingsViewModel(
        ILocalizationService localization,
        Translator translator,
        IDateFormatter dates,
        TimeProvider time,
        ZananceStore store,
        ReminderService reminders,
        AppLockService appLock,
        Presentation.ThemeService theme,
        ProfileService profiles)
    {
        // D-82: initial reminder text uses the same setter as user input; construction must not save defaults.
        _refreshing = true;
        _reminders = reminders;
        _lock = appLock;
        _theme = theme;
        _profiles = profiles;
        _localization = localization;
        _translator = translator;
        _dates = dates;
        _time = time;
        _store = store;
        Regional = new(localization, translator, time);

        Languages = [.. localization.SupportedLanguages];
        ReportCurrency = string.Empty;
        ReminderDaysText = "3";
        NotificationsSupported = reminders.Scheduler.IsSupported;
        RefreshDisplay();
        _refreshing = false;
    }

    public AppLanguage[] Languages { get; }

    /// <summary>Gets independent regional display and holiday choices.</summary>
    public RegionalPreferencesViewModel Regional { get; }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    /// <summary>Gets or sets the valuation currency: converted totals and converted charts only (ZEX-P01).</summary>
    [ObservableProperty]
    public partial string ReportCurrency { get; set; }

    /// <summary>Gets or sets the currency preselected for new accounts, goals, budgets and rates (ZEX-P01).</summary>
    [ObservableProperty]
    public partial string DefaultCurrency { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the hint "only new items use this" is shown after a change.</summary>
    [ObservableProperty]
    public partial bool DefaultCurrencyChanged { get; set; }

    /// <summary>Gets or sets the day-to-day essential spending estimate used by the headroom (04 §3); empty = not set.</summary>
    [ObservableProperty]
    public partial string EssentialText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int EssentialPeriodIndex { get; set; }

    /// <summary>Gets refreshed estimate-period captions without changing the selected period.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> EssentialPeriodNames { get; set; } = [];

    [ObservableProperty]
    public partial string? EssentialCurrencyText { get; set; }

    /// <summary>Gets the suggestion from the last three months; Zanance only suggests, the user decides.</summary>
    [ObservableProperty]
    public partial string? EssentialSuggestionText { get; set; }

    [ObservableProperty]
    public partial string? EssentialSavedText { get; set; }

    private long? _essentialSuggestion;
    private string _essentialCurrency = Currencies.Euro.Code;

    /// <summary>Gets the choices for the default account: "none" and every money account (ZEX-S0102, S0103).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DefaultAccountOption> DefaultAccounts { get; set; } = [];

    [ObservableProperty]
    public partial DefaultAccountOption? DefaultAccount { get; set; }

    /// <summary>Gets or sets a value indicating whether converted totals are shown (Advanced).</summary>
    [ObservableProperty]
    public partial bool ValuationEnabled { get; set; } = true;

    /// <summary>Gets the choices after how many days a rate may be outdated (Advanced, ZEX-P06).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> FreshnessNames { get; set; } = [];

    [ObservableProperty]
    public partial int FreshnessIndex { get; set; }

    /// <summary>Gets a value indicating whether the Advanced settings of money are shown.</summary>
    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    private static readonly int[] FreshnessDays = [7, 30, 90, 0];

    /// <summary>Gets refreshed experience-mode captions without changing the mode.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> ModeNames { get; set; } = [];

    [ObservableProperty]
    public partial int ModeIndex { get; set; }

    // The financial month (§10.3 pay-cycle periods): day 1 is the calendar month.
    [ObservableProperty]
    public partial IReadOnlyList<string> StartDayNames { get; set; } = [];

    [ObservableProperty]
    public partial int StartDayIndex { get; set; }

    partial void OnStartDayIndexChanged(int value)
    {
        if (_refreshing || value < 0)
        {
            return;
        }

        var day = Math.Clamp(value + 1, 1, Core.Budgets.PeriodMath.MaxStartDay);
        Update(s => s.MonthStartDay = day);
    }

    // Every setting is saved through one serialized update (no change can overwrite another), and a failure is shown
    // instead of ending the app from a property-changed handler.
    private void Update(Action<Core.Settings.ZananceSettings> change) =>
        _ = Presentation.Failures.GuardAsync(() => _store.UpdateSettingsAsync(change));

    // Theme (UX-08): follow the device, light or dark.
    [ObservableProperty]
    public partial IReadOnlyList<string> ThemeNames { get; set; } = [];

    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (!_refreshing && Enum.IsDefined((Presentation.ThemeChoice)value) && (Presentation.ThemeChoice)value != _theme.Choice)
        {
            _theme.Set((Presentation.ThemeChoice)value);
        }
    }

    [ObservableProperty]
    public partial bool LockAvailable { get; set; }

    [ObservableProperty]
    public partial bool LockEnabled { get; set; }

    /// <summary>Gets or sets the device-wide Android foreground screenshot choice (D-63).</summary>
    [ObservableProperty]
    public partial bool BlockScreenshots { get; set; }

    /// <summary>Gets whether the separate app PIN is configured.</summary>
    [ObservableProperty]
    public partial bool PinEnabled { get; set; }

    partial void OnBlockScreenshotsChanged(bool value)
    {
        if (!_refreshing) { ScreenProtection.Set(value); }
    }

    [RelayCommand]
    private async Task ManagePinAsync()
    {
        if (Application.Current?.Windows.FirstOrDefault()?.Page is { } root)
        {
            var page = new PinSettingsPage(_lock, _translator);
            await root.Navigation.PushModalAsync(page);
        }
    }

    [ObservableProperty]
    public partial bool NotificationsSupported { get; set; }

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    /// <summary>Gets or sets the profile's optional financial-period review reminder.</summary>
    [ObservableProperty]
    public partial bool ReviewReminderEnabled { get; set; }

    partial void OnReviewReminderEnabledChanged(bool value)
    {
        if (!_refreshing) { _ = Presentation.Failures.GuardAsync(() => SaveReviewReminderAsync(value)); }
    }

    private async Task SaveReviewReminderAsync(bool enabled)
    {
        // Persist the choice first, so a later off toggle cannot be overwritten by a slow permission answer.
        await _store.UpdateSettingsAsync(settings => settings.ReviewReminderEnabled = enabled);
        if (enabled && ReviewReminderEnabled && NotificationsSupported)
        {
            NotificationsEnabled = await Presentation.PermissionPrompts.EnableNotificationsAsync(_reminders, _translator);
        }
        await _reminders.RefreshAsync();
    }

    [ObservableProperty]
    public partial string ReminderDaysText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? ReminderTime { get; set; }

    /// <summary>Shares a complete pending read and publishes before the form becomes interactive.</summary>
    public Task LoadAsync() => Loading.RunAsync(async () =>
        _snapshot = await SettingsSnapshot.ReadAsync(_store, _localization, _time,
            _lock.Authenticator.IsAvailableAsync, NotificationsSupported, _reminders.Scheduler.AreEnabledAsync), PresentSnapshot);

    private void PresentSnapshot()
    {
        var snapshot = _snapshot ?? throw new InvalidOperationException("A settings snapshot must be read before presentation.");
        var settings = snapshot.Settings;
        var wasRefreshing = _refreshing;
        _refreshing = true;
        try
        {
            // D-79: suppress writes only during synchronous publication, never across an asynchronous read.
            ReportCurrency = settings.ReportCurrencyCode;
            DefaultCurrency = settings.DefaultCurrencyCode;
            DefaultCurrencyChanged = false;
            ValuationEnabled = settings.ValuationCurrencyEnabled;
            IsAdvanced = settings.Shows(Feature.MoneySettings);
            FreshnessIndex = Math.Max(0, Array.IndexOf(FreshnessDays, settings.RateFreshnessDays));
            var incomingCurrency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
            var incoming = new EstimateInput(settings.EssentialEstimate is { } estimate
                ? MoneyText.ForInput(estimate, incomingCurrency, _localization.CurrentCulture) : string.Empty,
                (int)settings.EssentialEstimatePeriod, incomingCurrency);
            var presented = _estimateDraft.Publish(_profiles.Current.Id, settings.Id, incoming,
                new(EssentialText, EssentialPeriodIndex, _essentialCurrency));
            _essentialCurrency = presented.CurrencyCode;
            EssentialText = presented.Text;
            EssentialPeriodIndex = presented.PeriodIndex;
            if (presented == incoming) { EssentialSavedText = null; }
            // A suggestion calculated in a newly selected default currency cannot be relabeled as the retained draft's unit.
            _essentialSuggestion = presented.CurrencyCode == incomingCurrency ? snapshot.EssentialSuggestion : null;
            DefaultAccounts = [new DefaultAccountOption(null, _translator["Settings_DefaultAccountNone"]),
                .. snapshot.DefaultAccounts.Select(a => new DefaultAccountOption(a.Id, $"{a.Name} ({a.CurrencyCode})"))];
            DefaultAccount = DefaultAccounts.FirstOrDefault(o => o.Id == settings.DefaultAccountId) ?? DefaultAccounts[0];
            ShowDetails = settings.NotificationsShowDetails;
            ReviewReminderEnabled = settings.ReviewReminderEnabled;
            ModeIndex = (int)settings.Mode;
            StartDayIndex = Math.Clamp(settings.MonthStartDay, 1, Core.Budgets.PeriodMath.MaxStartDay) - 1;
            ThemeIndex = (int)_theme.Choice;
            LockEnabled = settings.AppLockEnabled;
            LockAvailable = snapshot.LockAvailable;
            PinEnabled = _lock.PinEnabled;
            BlockScreenshots = ScreenProtection.BlockScreenshots;
            ReminderDaysText = settings.ReminderDaysBefore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ReminderTime = settings.ReminderTime.ToTimeSpan();
            NotificationsEnabled = snapshot.NotificationsEnabled;
            RefreshDisplay();
        }
        finally { _refreshing = wasRefreshing; }
    }

    partial void OnReportCurrencyChanged(string value)
    {
        if (_refreshing || string.IsNullOrEmpty(value))
        {
            return;
        }

        Update(s => s.ReportCurrencyCode = value);
    }

    // Changing one default never changes another or any stored amount (ZEX-MC02, AT02).
    partial void OnDefaultCurrencyChanged(string value)
    {
        if (_refreshing || string.IsNullOrEmpty(value))
        {
            return;
        }

        Update(s => s.DefaultCurrencyCode = value);
        DefaultCurrencyChanged = true;
    }

    partial void OnDefaultAccountChanged(DefaultAccountOption? value)
    {
        if (_refreshing || value is null)
        {
            return;
        }

        Update(s => s.DefaultAccountId = value.Id);
    }

    partial void OnValuationEnabledChanged(bool value)
    {
        if (_refreshing)
        {
            return;
        }

        Update(s => s.ValuationCurrencyEnabled = value);
    }

    [RelayCommand]
    private void UseEssentialSuggestion()
    {
        if (_essentialSuggestion is { } suggested)
        {
            EssentialText = MoneyText.ForInput(suggested, _essentialCurrency, _localization.CurrentCulture);
            EssentialPeriodIndex = (int)Core.Settings.EstimatePeriod.Day;
        }
    }

    [RelayCommand]
    private async Task SaveEssentialAsync()
    {
        var profileId = _profiles.Current.Id;
        var settings = await _store.GetSettingsAsync();
        var submitted = new EstimateInput(EssentialText, EssentialPeriodIndex, _essentialCurrency);
        if (string.IsNullOrWhiteSpace(submitted.Text))
        {
            settings.EssentialEstimate = null;
        }
        else if (MoneyText.TryParse(submitted.Text, submitted.CurrencyCode, _localization.CurrentCulture, out var amount) && amount > 0)
        {
            settings.EssentialEstimate = amount;
        }
        else
        {
            EssentialSavedText = _translator["Amount_Invalid"];
            return;
        }

        settings.EssentialEstimatePeriod = (Core.Settings.EstimatePeriod)Math.Clamp(submitted.PeriodIndex, 0, 2);
        settings.EssentialEstimateCurrency = submitted.CurrencyCode;
        await _store.SaveSettingsAsync(settings);
        _estimateDraft.AcceptSave(profileId, settings.Id, submitted);
        EssentialSavedText = _profiles.Current.Id == profileId
            && new EstimateInput(EssentialText, EssentialPeriodIndex, _essentialCurrency) == submitted
            ? _translator[settings.EssentialEstimate is null ? "Settings_EssentialCleared" : "Settings_EssentialSaved"] : null;
    }

    partial void OnFreshnessIndexChanged(int value)
    {
        if (_refreshing || value < 0 || value >= FreshnessDays.Length)
        {
            return;
        }

        Update(s => s.RateFreshnessDays = FreshnessDays[value]);
    }

    // Simple and Advanced show the same data and calculations; switching never removes anything (UX-01, UX-02).
    partial void OnModeIndexChanged(int value)
    {
        IsAdvanced = FeaturePolicy.Shows(Feature.MoneySettings, (ExperienceMode)value);
        if (_refreshing)
        {
            return;
        }

        Update(s => s.Mode = (Core.Settings.ExperienceMode)value);
    }

    /// <summary>Turns the app lock on or off after the device owner confirmed it (SEC-01).</summary>
    public async Task SetLockAsync(bool enabled)
    {
        if (_refreshing || enabled == _lock.DeviceLockEnabled)
        {
            return;
        }

        var result = await _lock.SetEnabledAsync(enabled);
        _refreshing = true;
        LockEnabled = result;
        _refreshing = false;
    }

    partial void OnShowDetailsChanged(bool value) => _ = Presentation.Failures.GuardAsync(SaveNotificationSettingsAsync);

    partial void OnReminderDaysTextChanged(string value) => _ = Presentation.Failures.GuardAsync(SaveNotificationSettingsAsync);

    partial void OnReminderTimeChanged(TimeSpan? value) => _ = Presentation.Failures.GuardAsync(SaveNotificationSettingsAsync);

    // Reminder defaults apply to new plans; the lock-screen choice applies to all notifications (REM-01, REM-05).
    private async Task SaveNotificationSettingsAsync()
    {
        if (_refreshing)
        {
            return;
        }

        var showDetails = ShowDetails;
        int? days = int.TryParse(Vafadar.Core.Text.Digits.ToAscii(ReminderDaysText), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? Math.Clamp(parsed, 0, 60) : null;
        var time = ReminderTime;
        await _store.UpdateSettingsAsync(settings =>
        {
            settings.NotificationsShowDetails = showDetails;
            if (days is { } value)
            {
                settings.ReminderDaysBefore = value;
            }

            if (time is { } at)
            {
                settings.ReminderTime = TimeOnly.FromTimeSpan(at);
            }
        });
    }

    // SEC: deletes every record on this device after two confirmations; onboarding starts again.
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task DeleteAllDataAsync()
    {
        if (Shell.Current is null
            || !await Shell.Current.DisplayAlertAsync(_translator["Settings_DeleteAllTitle"], _translator["Settings_DeleteAllMessage"], _translator["Settings_DeleteAll"], _translator["Common_Cancel"])
            || !await _lock.ConfirmAsync(_translator["Lock_ConfirmDeleteAll"]))
        {
            return;
        }

        await _store.DeleteAllDataAsync();
        Presentation.AttachmentFiles.ClearCache();
        await Presentation.DisplayUnitPreferences.ClearAsync(_store);
        await Presentation.HomeLayoutPreferences.ClearAsync(_store);
        await _lock.SetEnabledAsync(false);
        await _reminders.RefreshAsync();
        (Application.Current as App)?.ShowOnboarding();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task EnableNotificationsAsync() =>
        NotificationsEnabled = await Presentation.PermissionPrompts.EnableNotificationsAsync(_reminders, _translator);

    [ObservableProperty]
    public partial AppLanguage? SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial CalendarOption[] Calendars { get; set; }

    [ObservableProperty]
    public partial CalendarOption? SelectedCalendar { get; set; }

    [ObservableProperty]
    public partial string CalendarPreview { get; set; }

    partial void OnSelectedLanguageChanged(AppLanguage? value)
    {
        if (_refreshing || value is null || value == _localization.CurrentLanguage)
        {
            return;
        }

        _localization.SetLanguage(value);
        RefreshDisplay();
    }

    partial void OnSelectedCalendarChanged(CalendarOption? value)
    {
        if (_refreshing || value is null || value.Calendar == _localization.CurrentCalendar)
        {
            return;
        }

        _localization.SetCalendar(value.Calendar);
        RefreshDisplay();
    }

    /// <summary>Refreshes captions/previews only; selected values and unsaved estimate/reminder input are preserved.</summary>
    [MemberNotNull(nameof(Calendars), nameof(CalendarPreview))]
    internal void RefreshDisplay()
    {
        var wasRefreshing = _refreshing;
        _refreshing = true;
        try
        {
            var labels = SettingsChoiceLabels.Create(_translator, _localization.CurrentCulture);
            // Replacing ItemsSource can clear a native selection; restore indexes while all save callbacks are suppressed.
            var mode = ModeIndex; var theme = ThemeIndex; var freshness = FreshnessIndex;
            var start = StartDayIndex; var essential = EssentialPeriodIndex;
            ModeNames = labels.Modes; ThemeNames = labels.Themes; FreshnessNames = labels.Freshness;
            StartDayNames = labels.StartDays; EssentialPeriodNames = labels.EssentialPeriods;
            ModeIndex = mode; ThemeIndex = theme; FreshnessIndex = freshness;
            StartDayIndex = start; EssentialPeriodIndex = essential;
            if (DefaultAccounts.Count > 0)
            {
                var selected = DefaultAccount?.Id;
                DefaultAccounts = [new DefaultAccountOption(null, _translator["Settings_DefaultAccountNone"]), .. DefaultAccounts.Skip(1)];
                DefaultAccount = DefaultAccounts.FirstOrDefault(a => a.Id == selected) ?? DefaultAccounts[0];
            }
            EssentialCurrencyText = _translator.Format("Settings_EssentialCurrency", _essentialCurrency);
            EssentialSuggestionText = _essentialSuggestion is { } suggested and > 0
                ? _translator.Format("Settings_EssentialSuggestion", MoneyText.Format(suggested, _essentialCurrency, _localization.CurrentCulture)) : null;
            Calendars =
            [
                new CalendarOption(CalendarSystem.Gregorian, _translator["Calendar_Gregorian"]),
                new CalendarOption(CalendarSystem.Persian, _translator["Calendar_Persian"]),
                new CalendarOption(CalendarSystem.Hijri, _translator["Calendar_Hijri"]),
            ];
            SelectedCalendar = Calendars.First(option => option.Calendar == _localization.CurrentCalendar);
            SelectedLanguage = _localization.CurrentLanguage;
            Regional.Refresh();

            CalendarPreview = _dates.Format(DateOnly.FromDateTime(_time.GetLocalNow().DateTime), DateFormatStyle.Long);
        }
        finally
        {
            _refreshing = wasRefreshing;
        }
    }
}

/// <summary>A default-account choice; <see cref="Id"/> is <see langword="null"/> for "no default account".</summary>
public sealed record DefaultAccountOption(Guid? Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}
