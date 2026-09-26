using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Forecasts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Forecast;

/// <summary>One point of the path chart (major units).</summary>
public sealed record PathPoint(DateTime Date, double Balance);

/// <summary>An item of the forecast list.</summary>
public sealed record ForecastRow(string DateText, string Name, string AmountText, Color AmountColor, string? Badge, Guid? ScheduleId, DateOnly? OriginalDate);

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
    private readonly FinanceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;

    public ForecastViewModel(FinanceStore store, PlanStore plans, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
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
        var forecasts = ForecastCalculator.Compute(accounts, entries, await _plans.GetSchedulesAsync(), await _plans.GetStatesAsync(), today, horizon);
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
                item.Effect is null ? Color.FromArgb("#8D5B00") : item.Effect < 0 ? Color.FromArgb("#B71C1C") : Color.FromArgb("#1B5E20"),
                item.Source switch
                {
                    ForecastSource.OverduePlan => _translator["Forecast_OverdueAssumed"],
                    ForecastSource.FutureEntry => _translator["Forecast_Recorded"],
                    _ => null,
                },
                item.ScheduleId,
                item.OriginalDate)).ToList();

            var warning = forecast.GoesNegative
                ? _translator.Format("Forecast_Shortfall", _dates.Format(forecast.MinimumDate, DateFormatStyle.Long))
                : null;
            Cards.Add(new ForecastCard(
                forecast.CurrencyCode,
                MoneyText.Format(forecast.EndBalance, forecast.CurrencyCode, culture),
                _translator.Format("Forecast_Lowest", MoneyText.Format(forecast.Minimum, forecast.CurrencyCode, culture), _dates.Format(forecast.MinimumDate, DateFormatStyle.Short)),
                forecast.GoesNegative ? Color.FromArgb("#B71C1C") : Color.FromArgb("#5F6368"),
                warning,
                forecast.IsIncomplete ? _translator.Format("Forecast_Incomplete", forecast.UnknownCount) : null,
                [.. forecast.Path.Select(p => new PathPoint(p.Date.ToDateTime(TimeOnly.MinValue), (double)MoneyAmount.ToDecimal(p.Balance, currency)))],
                rows));
        }
    }

    [RelayCommand]
    private Task OpenItemAsync(ForecastRow row) => row is { ScheduleId: { } plan, OriginalDate: { } date }
        ? Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = plan, ["date"] = date })
        : Task.CompletedTask;
}
