using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Core.Hosting;
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
    private bool _refreshing;

    public SettingsViewModel(
        ILocalizationService localization,
        Translator translator,
        IDateFormatter dates,
        TimeProvider time,
        IAppEnvironment app,
        FinanceStore store)
    {
        _localization = localization;
        _translator = translator;
        _dates = dates;
        _time = time;
        _app = app;
        _store = store;

        Languages = [.. localization.SupportedLanguages];
        ReportCurrency = string.Empty;
        Refresh();
    }

    public AppLanguage[] Languages { get; }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial string ReportCurrency { get; set; }

    /// <summary>Loads the finance settings.</summary>
    public async Task LoadAsync()
    {
        _refreshing = true;
        try
        {
            ReportCurrency = (await _store.GetSettingsAsync()).ReportCurrencyCode;
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
