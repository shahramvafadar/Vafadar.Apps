using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Onboarding;

/// <summary>
/// Three steps: language, display and financial preferences, then a first account; an existing backup can be restored
/// instead (D-62). Creating a profile requires no sign-in, network or permissions.
/// </summary>
public sealed partial class OnboardingViewModel : ViewModelBase
{
    public const int StepCount = 3;

    private readonly ZananceStore _store;
    private readonly ILocalizationService _localization;
    private readonly Translator _translator;
    private readonly Presentation.ThemeService _theme;
    private bool _refreshing;
    private Account? _firstAccount;

    public OnboardingViewModel(ZananceStore store, ILocalizationService localization, Translator translator, TimeProvider time,
        Presentation.ThemeService theme)
    {
        _store = store;
        _localization = localization;
        _translator = translator;
        _theme = theme;
        Regional = new(localization, translator, time);
        ThemeIndex = (int)theme.Choice;
        Account = new AccountFormModel(translator, time);
        Languages = [.. localization.SupportedLanguages];
        CurrencyCodes = [.. Currencies.All.Select(c => c.Code)];
        Step = 1;
        ReportCurrency = SuggestCurrency();
        Calendars = [];
        StepText = string.Empty;
        Refresh();
    }

    public AccountFormModel Account { get; }

    public AppLanguage[] Languages { get; }

    /// <summary>Gets independent regional choices before creating an account.</summary>
    public RegionalPreferencesViewModel Regional { get; }

    public IReadOnlyList<string> CurrencyCodes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsLastStep), nameof(CanGoBack), nameof(ReachedStep2), nameof(ReachedStep3))]
    public partial int Step { get; set; }

    [ObservableProperty]
    public partial string StepText { get; set; }

    [ObservableProperty]
    public partial AppLanguage? SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial string ReportCurrency { get; set; }

    [ObservableProperty]
    public partial CalendarOption[] Calendars { get; set; }

    [ObservableProperty]
    public partial CalendarOption? SelectedCalendar { get; set; }

    /// <summary>Gets the translated theme choices in ThemeChoice order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> ThemeNames { get; set; } = [];

    /// <summary>Gets or sets the device-wide theme, applied immediately for preview.</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    /// <summary>Gets the translated experience choices in ExperienceMode order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> ModeNames { get; set; } = [];

    /// <summary>Gets or sets the experience of a new profile; Simple remains the default.</summary>
    [ObservableProperty]
    public partial int ModeIndex { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (!_refreshing && Enum.IsDefined((Presentation.ThemeChoice)value))
        {
            _theme.Set((Presentation.ThemeChoice)value);
        }
    }

    /// <summary>Opens restore without creating an account or saving the draft profile preferences.</summary>
    [RelayCommand]
    private Task RestoreBackupAsync() => Application.Current is App app ? app.ShowOnboardingRestoreAsync() : Task.CompletedTask;

    public bool IsStep1 => Step == 1;

    public bool IsStep2 => Step == 2;

    public bool IsStep3 => Step == 3;

    public bool IsLastStep => Step == StepCount;

    public bool CanGoBack => Step > 1;

    /// <summary>Gets a value indicating whether the second segment of the progress bar is filled.</summary>
    public bool ReachedStep2 => Step >= 2;

    /// <summary>Gets a value indicating whether the third segment of the progress bar is filled.</summary>
    public bool ReachedStep3 => Step >= 3;

    partial void OnSelectedLanguageChanged(AppLanguage? value)
    {
        if (!_refreshing && value is not null && value != _localization.CurrentLanguage)
        {
            _localization.SetLanguage(value);
            Refresh();
        }
    }

    partial void OnSelectedCalendarChanged(CalendarOption? value)
    {
        if (!_refreshing && value is not null && value.Calendar != _localization.CurrentCalendar)
        {
            _localization.SetCalendar(value.Calendar);
            Regional.Refresh();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (Step > 1)
        {
            Step--;
            Refresh();
        }
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (Step == 2)
        {
            Account.CurrencyCode = ReportCurrency;
            if (string.IsNullOrWhiteSpace(Account.Name))
            {
                Account.Name = _translator["Account_DefaultName"];
            }
        }

        if (Step < StepCount)
        {
            Step++;
            Refresh();
            return;
        }

        await FinishAsync();
    }

    private async Task FinishAsync()
    {
        // The account of a finish that failed half-way is reused, so trying again never adds a second first account.
        var account = _firstAccount ??= new Account { Name = string.Empty, CurrencyCode = ReportCurrency };
        if (IsBusy || !Account.TryApply(account, _localization.CurrentCulture, await _store.GetAccountsAsync()))
        {
            return;
        }

        IsBusy = true;
        var completed = false;
        try
        {
            await _store.EnsureDefaultCategoriesAsync();
            await _store.SaveAccountAsync(account);

            var settings = await _store.GetSettingsAsync();
            // One currency starts all three defaults (ZEX-P01): new items, the default account and converted totals.
            settings.ReportCurrencyCode = ReportCurrency;
            settings.DefaultCurrencyCode = ReportCurrency;
            settings.DefaultAccountId = account.Id;

            // Budget months follow the calendar chosen here; later changes apply to future budgets only (BUD-08).
            settings.BudgetCalendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
            // D-62: the user's explicit choice overrides the Simple suggestion for a new profile only.
            settings.Mode = (Core.Settings.ExperienceMode)ModeIndex;
            settings.OnboardingCompleted = true;
            await _store.SaveSettingsAsync(settings);

            completed = true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The input stays; Finish can be tapped again.
            await Presentation.Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (completed && Application.Current is App app)
        {
            // Finish the form's layout updates before Windows disconnects its visual tree (D-62).
            app.Dispatcher.Dispatch(app.ShowMainShell);
        }
    }

    private void Refresh()
    {
        _refreshing = true;
        try
        {
            SelectedLanguage = _localization.CurrentLanguage;
            // Replacing the translated choice lists can reset selection on some platforms; retain both choices.
            var themeIndex = ThemeIndex;
            var modeIndex = ModeIndex;
            ThemeNames = [_translator["Theme_System"], _translator["Theme_Light"], _translator["Theme_Dark"]];
            ModeNames = [_translator["Mode_Simple"], _translator["Mode_Advanced"]];
            ThemeIndex = themeIndex;
            ModeIndex = modeIndex;
            Calendars =
            [
                new CalendarOption(CalendarSystem.Gregorian, _translator["Calendar_Gregorian"]),
                new CalendarOption(CalendarSystem.Persian, _translator["Calendar_Persian"]),
                new CalendarOption(CalendarSystem.Hijri, _translator["Calendar_Hijri"]),
            ];
            SelectedCalendar = Calendars.First(c => c.Calendar == _localization.CurrentCalendar);
            Regional.Refresh();
            StepText = _translator.Format("Onb_Step", Step, StepCount);
            Account.RefreshTexts();
        }
        finally
        {
            _refreshing = false;
        }
    }

    // A suggestion from the device region; the user confirms or changes it (§13.1). No location access (LOC-05).
    private static string SuggestCurrency()
    {
        try
        {
            var code = new System.Globalization.RegionInfo(System.Globalization.CultureInfo.CurrentCulture.Name).ISOCurrencySymbol;
            return Currencies.TryGet(code, out var currency) ? currency.Code : Currencies.Euro.Code;
        }
        catch (ArgumentException)
        {
            return Currencies.Euro.Code;
        }
    }
}
