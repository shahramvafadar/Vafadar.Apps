using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>A defined display unit for the list, e.g. "1 Toman = 10 IRR".</summary>
public sealed record DisplayUnitRow(string CurrencyCode, string Text, string Example);

/// <summary>
/// Informal units in which amounts of a currency are shown and entered, e.g. the toman (FX-07). The user defines the
/// currency, the name and the factor explicitly; nothing is derived from the language or the region. Stored amounts,
/// exports and exchange rates stay in the currency itself.
/// </summary>
public sealed partial class DisplayUnitsViewModel(ZananceStore store, Translator translator, ILocalizationService localization) : ViewModelBase
{
    /// <summary>The factors offered: 10 to 1,000,000 major units per display unit.</summary>
    private static readonly int[] Exponents = [1, 2, 3, 4, 5, 6];

    public ObservableCollection<DisplayUnitRow> Units { get; } = [];

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    public IReadOnlyList<string> FactorNames { get; } = [.. Exponents.Select(e => Math.Pow(10, e).ToString("N0", System.Globalization.CultureInfo.CurrentCulture))];

    [ObservableProperty]
    public partial string? CurrencyCode { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int FactorIndex { get; set; }

    [ObservableProperty]
    public partial string? PreviewText { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool HasUnits { get; set; }

    partial void OnCurrencyCodeChanged(string? value) => UpdatePreview();

    partial void OnNameChanged(string value) => UpdatePreview();

    partial void OnFactorIndexChanged(int value) => UpdatePreview();

    public async Task LoadAsync()
    {
        CurrencyCode ??= (await store.GetSettingsAsync()).ReportCurrencyCode;
        Refresh();
    }

    // The toman is offered as a one-tap choice, never applied by itself.
    [RelayCommand]
    private void UseToman()
    {
        CurrencyCode = "IRR";
        Name = translator["Unit_TomanName"];
        FactorIndex = 0;
    }

    [RelayCommand]
    private void Add()
    {
        Error = null;
        var name = Name.Trim();
        var unit = new DisplayUnit(CurrencyCode ?? string.Empty, name, Exponents[Math.Clamp(FactorIndex, 0, Exponents.Length - 1)]);
        if (name.IndexOfAny(DisplayUnitPreferences.Reserved) >= 0 || !unit.IsValid)
        {
            Error = translator["Unit_Invalid"];
            return;
        }

        DisplayUnitPreferences.Save([.. DisplayUnits.All.Where(u => u.CurrencyCode != unit.CurrencyCode), unit]);
        Name = string.Empty;
        Refresh();
    }

    [RelayCommand]
    private async Task RemoveAsync(DisplayUnitRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Unit_Remove"], row.Text, translator["Common_Delete"], translator["Common_Cancel"]))
        {
            DisplayUnitPreferences.Save(DisplayUnits.All.Where(u => u.CurrencyCode != row.CurrencyCode));
            Refresh();
        }
    }

    private void Refresh()
    {
        var culture = localization.CurrentCulture;
        Units.Clear();
        foreach (var unit in DisplayUnits.All)
        {
            // The same amount in the currency and in the unit; the ISO form is isolated like every amount.
            var currency = Currencies.Get(unit.CurrencyCode);
            var example = currency.MinorFactor * unit.Factor * 12_500;
            var iso = Isolated(MoneyAmount.Format(example, currency, culture), currency.Code);
            Units.Add(new DisplayUnitRow(
                unit.CurrencyCode,
                translator.Format("Unit_Definition", unit.Name, Isolated(unit.Factor.ToString("N0", culture), unit.CurrencyCode)),
                translator.Format("Unit_Example", iso, MoneyText.Format(example, unit.CurrencyCode, culture))));
        }

        HasUnits = Units.Count > 0;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var name = Name.Trim();
        PreviewText = CurrencyCode is null || name.Length == 0
            ? null
            : translator.Format("Unit_Definition", name, Isolated(FactorNames[Math.Clamp(FactorIndex, 0, FactorNames.Count - 1)], CurrencyCode));
    }

    // A number with a currency code as one left-to-right run, like every amount, so it keeps its order in Persian.
    private static string Isolated(string number, string code) => $"\u2066\u200E{number}\u00A0{code}\u200E\u2069";
}