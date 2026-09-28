using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>One point of the path chart (major units).</summary>
public sealed record PathPoint(DateTime Date, double Balance);

/// <summary>An item of the forecast list.</summary>
public sealed record ForecastRow(
    string DateText,
    string Name,
    string AmountText,
    Color AmountColor,
    string? Badge,
    Guid? ScheduleId,
    DateOnly? OriginalDate,
    DateOnly Date,
    bool IsExcluded,
    bool IsMoved,
    bool IsAmountAssumed = false,
    string? PlanCurrency = null,
    long? PlanAmount = null)
{
    /// <summary>Gets the opacity: items left out by the what-if stay visible but dimmed.</summary>
    public double Opacity => IsExcluded ? 0.45 : 1;
}

/// <summary>The forecast of one currency, ready for display.</summary>
public sealed record ForecastCard(
    string CurrencyCode,
    string EndText,
    string MinimumText,
    Color MinimumColor,
    string? Warning,
    string? IncompleteText,
    IReadOnlyList<PathPoint> Path,
    IReadOnlyList<ForecastRow> Rows);

/// <summary>
/// "Estimated balance after recorded plans" (FOR-01..09): end balance, lowest balance with its date and the items
/// behind it. Never called "safe to spend" (FOR-09); unplanned spending is not guessed (FOR-06).
/// </summary>
public sealed partial class ForecastViewModel : ViewModelBase
{
    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly ForecastScenario _scenario = new();
    private Dictionary<Guid, string> _planCurrencies = [];

    public ForecastViewModel(ZananceStore store, PlanStore plans, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _plans = plans;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        HorizonNames = [translator["Forecast_EndOfMonth"], translator["Forecast_30Days"], translator["Forecast_90Days"]];
        BasisText = string.Empty;
    }

    public IReadOnlyList<string> HorizonNames { get; }

    public ObservableCollection<ForecastCard> Cards { get; } = [];

    [ObservableProperty]
    public partial int HorizonIndex { get; set; }

    [ObservableProperty]
    public partial string BasisText { get; set; }

    [ObservableProperty]
    public partial string? UnreviewedText { get; set; }

    [ObservableProperty]
    public partial bool HasAccounts { get; set; }

    [ObservableProperty]
    public partial bool HasScenario { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    partial void OnHorizonIndexChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        var today = Today;
        var calendar = _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
        var (year, month) = PeriodMath.MonthOf(today, calendar);
        var horizon = HorizonIndex switch
        {
            1 => today.AddDays(30),
            2 => today.AddDays(90),
            _ => PeriodMath.MonthRange(year, month, calendar).Last,
        };

        var accounts = await _store.GetAccountsAsync();
        var entries = await _store.GetEntriesAsync();
        HasAccounts = accounts.Count > 0;
        var schedules = await _plans.GetSchedulesAsync();
        var accountCurrencies = accounts.ToDictionary(a => a.Id, a => a.CurrencyCode);
        _planCurrencies = schedules.Where(p => accountCurrencies.ContainsKey(p.AccountId)).ToDictionary(p => p.Id, p => accountCurrencies[p.AccountId]);
        var forecasts = ForecastCalculator.Compute(accounts, entries, schedules, await _plans.GetStatesAsync(), today, horizon, scenario: _scenario);
        HasScenario = !_scenario.IsEmpty;
        var culture = _localization.CurrentCulture;

        // The basis is always visible: accounts, base date, horizon, and whether unreviewed entries are included (FOR-09).
        BasisText = _translator.Format("Forecast_Basis", _dates.Format(today, DateFormatStyle.Long), _dates.Format(horizon, DateFormatStyle.Long));
        var unreviewed = entries.Count(e => e.Review == ReviewState.Unreviewed);
        UnreviewedText = unreviewed > 0 ? _translator.Format("Forecast_Unreviewed", unreviewed) : null;

        Cards.Clear();
        foreach (var forecast in forecasts)
        {
            var currency = Currencies.TryGet(forecast.CurrencyCode, out var known) ? known : Currencies.Euro;
            var rows = forecast.Items.Select(item => new ForecastRow(
                _dates.Format(item.Date, DateFormatStyle.Short),
                string.IsNullOrEmpty(item.Name) ? _translator["Forecast_RecordedEntry"] : item.Name,
                item.Effect is { } effect ? MoneyText.Format(effect, forecast.CurrencyCode, culture, showPlus: true, approximate: item.IsEstimate) : _translator["Plan_AmountUnknown"],
                item.Effect is null ? Palette.WarningText : item.Effect < 0 ? Palette.ExpenseText : Palette.IncomeText,
                item.IsExcluded ? _translator["Forecast_LeftOut"]
                    : item.IsMoved && item.IsAmountAssumed ? _translator["Forecast_DateAndAmountAssumed"]
                    : item.IsMoved ? _translator["Forecast_DateAssumed"]
                    : item.IsAmountAssumed ? _translator["Forecast_AmountAssumed"]
                    : item.Source switch
                    {
                        ForecastSource.OverduePlan => _translator["Forecast_OverdueAssumed"],
                        ForecastSource.FutureEntry => _translator["Forecast_Recorded"],
                        _ => null,
                    },
                item.ScheduleId,
                item.OriginalDate,
                item.Date,
                item.IsExcluded,
                item.IsMoved,
                item.IsAmountAssumed,
                item.ScheduleId is { } id ? _planCurrencies.GetValueOrDefault(id) : null,
                item.Effect is { } value && item.ScheduleId is { } plan && string.Equals(_planCurrencies.GetValueOrDefault(plan), forecast.CurrencyCode, StringComparison.OrdinalIgnoreCase) ? Math.Abs(value) : null)).ToList();

            var warning = forecast.GoesNegative
                ? _translator.Format("Forecast_Shortfall", _dates.Format(forecast.MinimumDate, DateFormatStyle.Long))
                : null;
            Cards.Add(new ForecastCard(
                forecast.CurrencyCode,
                MoneyText.Format(forecast.EndBalance, forecast.CurrencyCode, culture),
                _translator.Format("Forecast_Lowest", MoneyText.Format(forecast.Minimum, forecast.CurrencyCode, culture), _dates.Format(forecast.MinimumDate, DateFormatStyle.Short)),
                forecast.GoesNegative ? Palette.ExpenseText : Palette.SecondaryText,
                warning,
                forecast.IsIncomplete ? _translator.Format("Forecast_Incomplete", forecast.UnknownCount) : null,
                [.. forecast.Path.Select(p => new PathPoint(p.Date.ToDateTime(TimeOnly.MinValue), (double)MoneyText.ToDecimal(p.Balance, currency)))],
                rows));
        }
    }

    // FOR-04, FOR-10: a plan item can be left out, or assumed on another date or with another amount, for this view only;
    // nothing is saved.
    [RelayCommand]
    private async Task OpenItemAsync(ForecastRow row)
    {
        if (row is not { ScheduleId: { } plan, OriginalDate: { } original })
        {
            return;
        }

        var open = _translator["Forecast_OpenOccurrence"];
        var toggle = row.IsExcluded ? _translator["Forecast_IncludeAgain"] : _translator["Forecast_LeaveOut"];
        var move = _translator["Forecast_AssumeDate"];
        var restore = _translator["Forecast_RestoreDate"];
        var amount = row.IsAmountAssumed ? _translator["Forecast_RestoreAmount"] : _translator["Forecast_AssumeAmount"];
        string[] actions = row.IsExcluded ? [open, toggle] : [open, toggle, row.IsMoved ? restore : move, amount];
        var choice = await Shell.Current.DisplayActionSheetAsync(row.Name, _translator["Common_Cancel"], null, actions);
        if (choice == open)
        {
            await Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = plan, ["date"] = original });
            return;
        }

        if (choice == restore)
        {
            _scenario.SetDate(plan, original, null);
        }
        else if (choice == toggle)
        {
            _scenario.SetExcluded(plan, original, !row.IsExcluded);
        }
        else if (choice == amount && row.IsAmountAssumed)
        {
            _scenario.SetAmount(plan, original, null);
        }
        else if (choice == amount)
        {
            if (row.PlanCurrency is not { } currency)
            {
                return;
            }

            var culture = _localization.CurrentCulture;
            var text = await Shell.Current.DisplayPromptAsync(
                _translator["Forecast_AssumeAmount"], _translator.Format("Forecast_AssumeAmountMessage", MoneyText.UnitName(currency)), _translator["Common_Ok"], _translator["Common_Cancel"],
                initialValue: row.PlanAmount is { } current ? MoneyText.ForInput(current, currency, culture) : string.Empty, maxLength: 20, keyboard: Keyboard.Numeric);
            if (text is null || !MoneyText.TryParse(text, currency, culture, out var assumed) || assumed <= 0)
            {
                return;
            }

            _scenario.SetAmount(plan, original, assumed);
        }
        else if (choice == move)
        {
            var text = await Shell.Current.DisplayPromptAsync(
                _translator["Forecast_AssumeDate"], _translator["Forecast_AssumeDateMessage"], _translator["Common_Ok"], _translator["Common_Cancel"],
                initialValue: "7", maxLength: 4, keyboard: Keyboard.Numeric);
            if (!int.TryParse(Vafadar.Core.Text.Digits.ToAscii(text ?? string.Empty).Replace('−', '-'), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var days) || days == 0)
            {
                return;
            }

            _scenario.SetDate(plan, original, row.Date.AddDays(days));
        }
        else
        {
            return;
        }

        await LoadAsync();
    }

    /// <summary>Assumes another amount for a plan item of this view (FOR-10); nothing is saved.</summary>
    internal async Task AssumeAmountAsync(ForecastRow row, long amount)
    {
        if (row is { ScheduleId: { } plan, OriginalDate: { } original })
        {
            _scenario.SetAmount(plan, original, amount);
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task ResetScenarioAsync()
    {
        _scenario.Clear();
        await LoadAsync();
    }
}
