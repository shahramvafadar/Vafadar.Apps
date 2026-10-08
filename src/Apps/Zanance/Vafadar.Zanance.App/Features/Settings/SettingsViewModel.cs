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

namespace Vafadar.Zanance.App.Features.Settings;

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
    private bool _refreshing;

    public SettingsViewModel(
        ILocalizationService localization,
        Translator translator,
        IDateFormatter dates,
        TimeProvider time,
        ZananceStore store,
        ReminderService reminders,
        AppLockService appLock,
        Presentation.ThemeService theme)
    {
        _reminders = reminders;
        _lock = appLock;
        _theme = theme;
        ModeNames = [translator["Mode_Simple"], translator["Mode_Advanced"]];
        _localization = localization;
        _translator = translator;
        _dates = dates;
        _time = time;
        _store = store;

        Languages = [.. localization.SupportedLanguages];
        ReportCurrency = string.Empty;
        ReminderDaysText = "3";
        NotificationsSupported = reminders.Scheduler.IsSupported;
        Refresh();
    }

    public AppLanguage[] Languages { get; }

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

    public IReadOnlyList<string> EssentialPeriodNames => [_translator["Settings_PerDay"], _translator["Settings_PerWeek"], _translator["Settings_PerMonth"]];

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

    public IReadOnlyList<string> ModeNames { get; }

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

    // Persian digits (D-27), offered only in the Persian interface.
    [ObservableProperty]
    public partial bool IsPersian { get; set; }

    [ObservableProperty]
    public partial bool PersianDigits { get; set; }

    partial void OnPersianDigitsChanged(bool value)
    {
        if (!_refreshing && value != Presentation.DigitPreferences.PersianDigits)
        {
            Presentation.DigitPreferences.Set(value, _localization);
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

    [ObservableProperty]
    public partial string ReminderDaysText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? ReminderTime { get; set; }

    /// <summary>Loads the app settings.</summary>
    public async Task LoadAsync()
    {
        _refreshing = true;
        try
        {
            var settings = await _store.GetSettingsAsync();
            ReportCurrency = settings.ReportCurrencyCode;
            DefaultCurrency = settings.DefaultCurrencyCode;
            DefaultCurrencyChanged = false;
            ValuationEnabled = settings.ValuationCurrencyEnabled;
            IsAdvanced = settings.Shows(Feature.MoneySettings);
            var dayNames = FreshnessDays.Select(d => d == 0
                ? _translator["Settings_FreshnessNever"]
                : Vafadar.Localization.Formatting.NativeDigits.Apply(_translator.Format("Settings_FreshnessDays", d.ToString(_localization.CurrentCulture)))!);
            FreshnessNames = [.. dayNames];
            FreshnessIndex = Math.Max(0, Array.IndexOf(FreshnessDays, settings.RateFreshnessDays));
            await LoadEssentialAsync(settings);
            var accounts = (await _store.GetAccountsAsync(includeArchived: false)).Where(a => Core.Accounts.EntryAccountContract.IsValidDefault(a)).ToList();
            DefaultAccounts =
            [
                new DefaultAccountOption(null, _translator["Settings_DefaultAccountNone"]),
                .. accounts.Select(a => new DefaultAccountOption(a.Id, $"{a.Name} ({a.CurrencyCode})")),
            ];
            DefaultAccount = DefaultAccounts.FirstOrDefault(o => o.Id == settings.DefaultAccountId) ?? DefaultAccounts[0];
            ShowDetails = settings.NotificationsShowDetails;
            ModeIndex = (int)settings.Mode;
            var culture = _localization.CurrentCulture;
            StartDayNames =
            [
                _translator["Settings_MonthStartCalendar"],
                .. Enumerable.Range(2, Core.Budgets.PeriodMath.MaxStartDay - 1)
                    .Select(d => Vafadar.Localization.Formatting.NativeDigits.Apply(_translator.Format("Settings_MonthStartDay", d.ToString(culture)))!),
            ];
            StartDayIndex = Math.Clamp(settings.MonthStartDay, 1, Core.Budgets.PeriodMath.MaxStartDay) - 1;
            ThemeNames = [_translator["Theme_System"], _translator["Theme_Light"], _translator["Theme_Dark"]];
            ThemeIndex = (int)_theme.Choice;
            IsPersian = Presentation.DigitPreferences.IsPersian(_localization);
            PersianDigits = Presentation.DigitPreferences.PersianDigits;
            LockEnabled = settings.AppLockEnabled;
            LockAvailable = settings.AppLockEnabled || await _lock.Authenticator.IsAvailableAsync();
            PinEnabled = _lock.PinEnabled;
            BlockScreenshots = ScreenProtection.BlockScreenshots;
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

    // The explicit estimate of day-to-day spending (ZEX-S0606) and a suggestion: the median daily spending of the last
    // three complete months on usable accounts, without plan payments.
    private async Task LoadEssentialAsync(Core.Settings.ZananceSettings settings)
    {
        var culture = _localization.CurrentCulture;
        _essentialCurrency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        EssentialCurrencyText = _translator.Format("Settings_EssentialCurrency", _essentialCurrency);
        EssentialText = settings.EssentialEstimate is { } estimate ? MoneyText.ForInput(estimate, _essentialCurrency, culture) : string.Empty;
        EssentialPeriodIndex = (int)settings.EssentialEstimatePeriod;
        EssentialSavedText = null;
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var calendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
        _essentialSuggestion = Core.Reports.LiquidityCalculator.SuggestPerDay(await _store.GetAccountsAsync(), await _store.GetEntriesAsync(), _essentialCurrency, today, calendar, settings.MonthStartDay);
        EssentialSuggestionText = _essentialSuggestion is { } suggested and > 0
            ? _translator.Format("Settings_EssentialSuggestion", MoneyText.Format(suggested, _essentialCurrency, culture))
            : null;
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
        var settings = await _store.GetSettingsAsync();
        if (string.IsNullOrWhiteSpace(EssentialText))
        {
            settings.EssentialEstimate = null;
        }
        else if (MoneyText.TryParse(EssentialText, _essentialCurrency, _localization.CurrentCulture, out var amount) && amount > 0)
        {
            settings.EssentialEstimate = amount;
        }
        else
        {
            EssentialSavedText = _translator["Amount_Invalid"];
            return;
        }

        settings.EssentialEstimatePeriod = (Core.Settings.EstimatePeriod)Math.Clamp(EssentialPeriodIndex, 0, 2);
        settings.EssentialEstimateCurrency = _essentialCurrency;
        await _store.SaveSettingsAsync(settings);
        EssentialSavedText = _translator[settings.EssentialEstimate is null ? "Settings_EssentialCleared" : "Settings_EssentialSaved"];
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
        Refresh();
    }

    // Region and first day of the week (PR-05, §13): a region only suggests the week start; nothing else follows it.
    [ObservableProperty]
    public partial RegionOption[] Regions { get; set; } = [];

    [ObservableProperty]
    public partial RegionOption? SelectedRegion { get; set; }

    [ObservableProperty]
    public partial WeekStartOption[] WeekStarts { get; set; } = [];

    [ObservableProperty]
    public partial WeekStartOption? SelectedWeekStart { get; set; }

    partial void OnSelectedRegionChanged(RegionOption? value)
    {
        if (_refreshing || value is null || value.Code == _localization.CurrentRegion)
        {
            return;
        }

        _localization.SetRegion(value.Code);
        Refresh();
    }

    partial void OnSelectedWeekStartChanged(WeekStartOption? value)
    {
        var unchanged = value?.Day is null
            ? _localization.IsFirstDayOfWeekAutomatic
            : !_localization.IsFirstDayOfWeekAutomatic && value.Day == _localization.FirstDayOfWeek;
        if (_refreshing || value is null || unchanged)
        {
            return;
        }

        _localization.SetFirstDayOfWeek(value.Day);
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
    [MemberNotNull(nameof(Calendars), nameof(CalendarPreview))]
    private void Refresh()
    {
        _refreshing = true;
        try
        {
            Calendars =
            [
                new CalendarOption(CalendarSystem.Gregorian, _translator["Calendar_Gregorian"]),
                new CalendarOption(CalendarSystem.Persian, _translator["Calendar_Persian"]),
            new CalendarOption(CalendarSystem.Hijri, _translator["Calendar_Hijri"]),
            ];
            SelectedCalendar = Calendars.First(option => option.Calendar == _localization.CurrentCalendar);
            SelectedLanguage = _localization.CurrentLanguage;

            var culture = _localization.CurrentCulture;
            var suggested = _localization.SuggestedRegion;
            var notSet = suggested is null
                ? _translator["Settings_RegionNotSet"]
                : _translator.Format("Settings_RegionSuggested", Vafadar.Localization.Regions.DisplayName(suggested, culture));
            Regions =
            [
                new RegionOption(null, notSet),
                .. Vafadar.Localization.Regions.All
                    .Select(code => new RegionOption(code, Vafadar.Localization.Regions.DisplayName(code, culture)))
                    .OrderBy(option => option.DisplayName, StringComparer.Create(culture, ignoreCase: true)),
            ];
            SelectedRegion = Regions.FirstOrDefault(option => option.Code == _localization.CurrentRegion) ?? Regions[0];

            var automatic = Vafadar.Localization.Regions.FirstDayOfWeek(_localization.CurrentRegion, System.Globalization.CultureInfo.GetCultureInfo(_localization.CurrentLanguage.CultureName));
            WeekStarts =
            [
                new WeekStartOption(null, _translator.Format("Settings_WeekStartAutomatic", culture.DateTimeFormat.GetDayName(automatic))),
                .. new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday }.Select(day => new WeekStartOption(day, culture.DateTimeFormat.GetDayName(day))),
            ];
            SelectedWeekStart = _localization.IsFirstDayOfWeekAutomatic ? WeekStarts[0] : WeekStarts.FirstOrDefault(o => o.Day == _localization.FirstDayOfWeek) ?? WeekStarts[0];
            CalendarPreview = _dates.Format(DateOnly.FromDateTime(_time.GetLocalNow().DateTime), DateFormatStyle.Long);
        }
        finally
        {
            _refreshing = false;
        }
    }
}

/// <summary>A default-account choice; <see cref="Id"/> is <see langword="null"/> for "no default account".</summary>
public sealed record DefaultAccountOption(Guid? Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A region choice; <see cref="Code"/> is <see langword="null"/> for "not set".</summary>
public sealed record RegionOption(string? Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A first-day-of-week choice; <see cref="Day"/> is <see langword="null"/> for "automatic".</summary>
public sealed record WeekStartOption(DayOfWeek? Day, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A calendar choice with its display name in the current language.</summary>
public sealed record CalendarOption(CalendarSystem Calendar, string DisplayName)
{
    public override string ToString() => DisplayName;
}
