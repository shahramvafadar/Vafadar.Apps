using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>One asset type in the holdings list.</summary>
/// <param name="Id">The asset type.</param>
/// <param name="Name">Its name.</param>
/// <param name="Icon">Its icon.</param>
/// <param name="QuantityText">"20.000 g" or "3 coins".</param>
/// <param name="DetailText">Locations and purity, e.g. "Home safe · 750/1000".</param>
/// <param name="ValueText">"1,000.00 EUR · price of 2 Oct", or <see langword="null"/> without a price.</param>
/// <param name="DerivedText">The weight of counted coins, e.g. "(30.000 g)".</param>
public sealed record HoldingRow(Guid Id, string Name, Symbol Icon, string QuantityText, string? DetailText, string? ValueText, string? DerivedText)
{
    /// <summary>Gets a value indicating whether the value is unknown ("Value unknown – add a price"), never zero.</summary>
    public bool ValueUnknown => ValueText is null;

    /// <summary>Gets the row as one sentence for screen readers.</summary>
    public string Description => string.Join(", ", new[] { Name, QuantityText, DerivedText, DetailText, ValueText }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>
/// The holdings list (ZEX-S0407, design §7): every asset type with its quantity, where it is kept and its value or
/// "value unknown", the known value per currency with what is missing, and the fine-metal equivalent per metal. Holdings
/// never mix into money totals. Simple shows the list; creating types and events is in Advanced.
/// </summary>
public sealed partial class HoldingsViewModel(HoldingStore holdings, ZananceStore store, HoldingText text, Translator translator, TimeProvider time) : ViewModelBase
{
    public ObservableCollection<HoldingRow> Rows { get; } = [];

    public ObservableCollection<string> FineMetals { get; } = [];

    /// <summary>Gets the known value per currency, e.g. "1,000.00 EUR".</summary>
    [ObservableProperty]
    public partial string? KnownValueText { get; set; }

    /// <summary>Gets "Incomplete: 2 types without a price" when some value is unknown.</summary>
    [ObservableProperty]
    public partial string? IncompleteText { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    public async Task LoadAsync()
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        IsAdvanced = (await store.GetSettingsAsync()).Mode == Core.Settings.ExperienceMode.Advanced;
        var types = (await holdings.GetTypesAsync()).Where(t => !t.IsArchived).ToList();
        var events = await holdings.GetEventsAsync();
        var valuations = await holdings.GetValuationsAsync();
        var locations = (await holdings.GetLocationsAsync()).ToDictionary(l => l.Id, l => l.Name);
        var values = AssetValuationService.Values(types, events, valuations, today).ToDictionary(v => v.AssetType.Id);
        var positions = HoldingsLedger.Positions(events, today);

        Rows.Clear();
        foreach (var type in types)
        {
            var quantity = values.TryGetValue(type.Id, out var value) ? value.Quantity : 0;
            var places = positions.Where(p => p.AssetTypeId == type.Id && p.Quantity > 0).Select(p => locations.GetValueOrDefault(p.LocationId, "?"));
            var detail = string.Join(" · ", places.Append(text.Purity(type)).Where(s => s is not null));
            string? valueText = value?.Value is { } known
                ? translator.Format(value.FromPurchase ? "Holding_ValuePurchase" : "Holding_Value", text.Money(known, type.PriceCurrencyCode), text.Date(value.PriceDate!.Value))
                : null;
            Rows.Add(new HoldingRow(type.Id, type.Name, HoldingText.Icon(type), text.Quantity(quantity, type), detail.Length == 0 ? null : detail,
                quantity == 0 ? translator["Holding_None"] : valueText, text.DerivedWeight(quantity, type)));
        }

        // Known values per currency; what has no price is named, never counted as zero (ZEX-AS06).
        var held = values.Values.ToList();
        KnownValueText = held.Any(v => v.Value is not null)
            ? string.Join(" · ", held.Where(v => v.Value is not null).GroupBy(v => v.AssetType.PriceCurrencyCode).Select(g => text.Money(g.Sum(v => v.Value!.Value), g.Key)))
            : null;
        var unknown = held.Count(v => v.Value is null);
        IncompleteText = unknown > 0 ? translator.Format("Holding_Incomplete", unknown) : null;

        FineMetals.Clear();
        foreach (var fine in AssetValuationService.FineMetals(held))
        {
            FineMetals.Add(text.FineMetal(fine));
        }

        IsEmpty = Rows.Count == 0;
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.AssetTypeEditorRoute);

    [RelayCommand]
    private Task OpenAsync(HoldingRow row) => Shell.Current.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
}
