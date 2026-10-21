using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>A defined display unit for the list, e.g. "1 Toman = 10 IRR".</summary>
public sealed record DisplayUnitRow(string CurrencyCode, string Text, string Example);

/// <summary>
/// Informal units in which amounts of a currency are shown and entered, e.g. the toman (FX-07). The user defines the
/// currency, the name and the factor explicitly; nothing is derived from the language or the region. Stored amounts,
/// exports and exchange rates stay in the currency itself.
/// </summary>
public sealed partial class DisplayUnitsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction) : ViewModelBase
{
    /// <summary>The factors offered: 10 to 1,000,000 major units per display unit.</summary>
    private static readonly int[] Exponents = [1, 2, 3, 4, 5, 6];

    private DisplayUnitsReview? _review;
    private bool _active;
    private long _lifetime, _revision;
    /// <summary>Edits require the original complete settings publication and no pending native consent/write.</summary>
    public bool CanEdit => _active && _review is not null && !IsBusy;
    /// <summary>Retires pending native consent and original writes when this exact page leaves.</summary>
    public void Deactivate() { _active = false; _lifetime++; OnPropertyChanged(nameof(CanEdit)); }

    public ObservableCollection<DisplayUnitRow> Units { get; } = [];

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    public IReadOnlyList<string> FactorNames { get; } = [.. Exponents.Select(e => Math.Pow(10, e).ToString("N0", localization.CurrentCulture))];

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

    partial void OnCurrencyCodeChanged(string? value) { _revision++; UpdatePreview(); }

    partial void OnNameChanged(string value) { _revision++; UpdatePreview(); }

    partial void OnFactorIndexChanged(int value) { _revision++; UpdatePreview(); }

    /// <summary>Publishes only the settings read owned by this exact visible page.</summary>
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        _active = true; var lifetime = ++_lifetime; _review = null; Error = null; OnPropertyChanged(nameof(CanEdit));
        var original = await store.ReadDisplayUnitsAsync();
        if (!_active || lifetime != _lifetime) return;
        if (original.SerializedUnits is { } serialized) DisplayUnitPublication.Apply(serialized);
        _review = original; CurrencyCode ??= original.DefaultCurrencyCode; Refresh(); OnPropertyChanged(nameof(CanEdit));
    }

    // The toman is offered as a one-tap choice, never applied by itself.
    [RelayCommand]
    private void UseToman()
    {
        if (!CanEdit) return;
        CurrencyCode = "IRR";
        Name = translator["Unit_TomanName"];
        FactorIndex = 0;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit) return;
        Error = null;
        var name = Name.Trim();
        var unit = new DisplayUnit(CurrencyCode ?? string.Empty, name, Exponents[Math.Clamp(FactorIndex, 0, Exponents.Length - 1)]);
        if (name.IndexOfAny(DisplayUnitPublication.Reserved) >= 0 || !unit.IsValid)
        {
            Error = translator["Unit_Invalid"];
            return;
        }

        await SaveUnitsAsync([.. DisplayUnits.All.Where(u => u.CurrencyCode != unit.CurrencyCode), unit], null);
    }

    [RelayCommand]
    private Task RemoveAsync(DisplayUnitRow row) => !CanEdit || !Units.Contains(row) ? Task.CompletedTask
        : SaveUnitsAsync([.. DisplayUnits.All.Where(unit => unit.CurrencyCode != row.CurrencyCode)], row);

    private async Task SaveUnitsAsync(DisplayUnit[] submitted, DisplayUnitRow? remove)
    {
        if (!CanEdit || _review is not { } original) return;
        var revision = _revision; var lifetime = _lifetime;
        bool Current() => _active && lifetime == _lifetime && revision == _revision && ReferenceEquals(_review, original);
        IsBusy = true; Error = null; OnPropertyChanged(nameof(CanEdit));
        try
        {
            if (remove is not null && !await interaction.ConfirmAsync(translator["Unit_Remove"], remove.Text,
                translator["Common_Delete"], translator["Common_Cancel"])) return;
            var saved = await DisplayUnitPublication.SaveAsync(store, submitted, original, Current);
            if (_active && lifetime == _lifetime && ReferenceEquals(_review, original))
            {
                _review = saved.Published ? saved.Review : null;
                if (revision == _revision && remove is null) Name = string.Empty;
                if (saved.NotificationFailed || !saved.Published) Error = translator["Unit_SavedFollowUpFailed"];
                Refresh();
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (_active && lifetime == _lifetime) { _review = null; Error = translator["Unit_SaveFailed"]; } }
        finally { IsBusy = false; OnPropertyChanged(nameof(CanEdit)); }
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
