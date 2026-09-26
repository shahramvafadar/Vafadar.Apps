using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Text;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Core.Rates;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Rates;

/// <summary>A stored rate for the list.</summary>
public sealed record RateRow(Guid Id, string Text, string DateText, bool IsEstimate);

/// <summary>
/// Manual exchange rates (FX-02): 1 unit of one currency in another on a date, actual or estimated. Rates only value
/// totals in the report currency; recorded amounts, budgets and history are never converted (FX-04..06).
/// </summary>
public sealed partial class RatesViewModel(FinanceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time) : ViewModelBase
{
    public ObservableCollection<RateRow> Rates { get; } = [];

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial string? FromCurrency { get; set; }

    [ObservableProperty]
    public partial string? ToCurrency { get; set; }

    [ObservableProperty]
    public partial string RateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly Date { get; set; }

    [ObservableProperty]
    public partial bool IsEstimate { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial string? MissingText { get; set; }

    [ObservableProperty]
    public partial bool HasRates { get; set; }

    public async Task LoadAsync()
    {
        var settings = await store.GetSettingsAsync();
        ToCurrency ??= settings.ReportCurrencyCode;
        if (Date == default)
        {
            Date = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        }

        var culture = localization.CurrentCulture;
        var rates = await store.GetRatesAsync();
        Rates.Clear();
        foreach (var rate in rates)
        {
            Rates.Add(new RateRow(
                rate.Id,
                string.Create(culture, $"1 {rate.FromCurrencyCode} = {rate.Rate.ToString("0.######", culture)} {rate.ToCurrencyCode}"),
                dates.Format(rate.Date, DateFormatStyle.Long),
                rate.IsEstimate));
        }

        HasRates = Rates.Count > 0;

        // Currencies of the accounts that cannot be valued in the report currency today.
        var table = new RateTable(rates);
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var missing = (await store.GetAccountsAsync(includeArchived: false))
            .Select(a => a.CurrencyCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => !table.TryGetRate(c, settings.ReportCurrencyCode, today, out _, out _))
            .ToList();
        MissingText = missing.Count > 0 ? translator.Format("Rates_Missing", string.Join(", ", missing), settings.ReportCurrencyCode) : null;
        FromCurrency ??= missing.FirstOrDefault() ?? CurrencyCodes.FirstOrDefault(c => c != ToCurrency);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        Error = null;
        if (FromCurrency is null || ToCurrency is null || FromCurrency == ToCurrency)
        {
            Error = translator["Rates_SameCurrency"];
            return;
        }

        // A rate is a plain positive number with the culture's or a Latin decimal point, in any digit script.
        var text = Digits.ToAscii(RateText.Trim()).Replace(localization.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".", StringComparison.Ordinal);
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
        {
            Error = translator["Rates_Invalid"];
            return;
        }

        await store.SaveRateAsync(new ExchangeRate { FromCurrencyCode = FromCurrency, ToCurrencyCode = ToCurrency, Rate = rate, Date = Date, IsEstimate = IsEstimate });
        RateText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(RateRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Rates_Delete"], row.Text, translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await store.DeleteRateAsync(row.Id);
            await LoadAsync();
        }
    }
}
