using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Settings;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Onboarding;

/// <summary>
/// Three steps: language · report currency and calendar · first account (ONB-01..03). Nothing requires sign-in,
/// network or permissions.
/// </summary>
public sealed partial class OnboardingViewModel : ViewModelBase
{
    public const int StepCount = 3;

    private readonly FinanceStore _store;
    private readonly ILocalizationService _localization;
    private readonly Translator _translator;
    private bool _refreshing;

    public OnboardingViewModel(FinanceStore store, ILocalizationService localization, Translator translator, TimeProvider time)
    {
        _store = store;
        _localization = localization;
        _translator = translator;
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

    public IReadOnlyList<string> CurrencyCodes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsLastStep), nameof(CanGoBack))]
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

    public bool IsStep1 => Step == 1;

    public bool IsStep2 => Step == 2;

    public bool IsStep3 => Step == 3;

    public bool IsLastStep => Step == StepCount;

    public bool CanGoBack => Step > 1;

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
        var account = new Account { Name = string.Empty, CurrencyCode = ReportCurrency };
        if (IsBusy || !Account.TryApply(account, _localization.CurrentCulture))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _store.EnsureDefaultCategoriesAsync();
            await _store.SaveAccountAsync(account);

            var settings = await _store.GetSettingsAsync();
            settings.ReportCurrencyCode = ReportCurrency;
            settings.DefaultAccountId = account.Id;

            // Budget months follow the calendar chosen here; later changes apply to future budgets only (BUD-08).
            settings.BudgetCalendar = _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
            settings.OnboardingCompleted = true;
            await _store.SaveSettingsAsync(settings);

            ((App)Application.Current!).ShowMainShell();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Refresh()
    {
        _refreshing = true;
        try
        {
            SelectedLanguage = _localization.CurrentLanguage;
            Calendars =
            [
                new CalendarOption(CalendarSystem.Gregorian, _translator["Calendar_Gregorian"]),
                new CalendarOption(CalendarSystem.Persian, _translator["Calendar_Persian"]),
            ];
            SelectedCalendar = Calendars.First(c => c.Calendar == _localization.CurrentCalendar);
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
