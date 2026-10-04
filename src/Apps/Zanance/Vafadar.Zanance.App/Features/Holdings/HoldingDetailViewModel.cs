using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>One change of a holding in the history.</summary>
public sealed record HoldingEventRow(Guid Id, string Title, string DateText, string QuantityText, string? Detail, Color QuantityColor)
{
    public string Description => string.Join(", ", new[] { Title, DateText, QuantityText, Detail }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>One dated price of the type.</summary>
public sealed record ValuationRow(Guid Id, string PriceText, string DateText, string? SourceText)
{
    public string Description => string.Join(", ", new[] { PriceText, DateText, SourceText }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>
/// One asset type (ZEX-S0407, design §7.5): the quantity in total and per location, the value from the latest price or
/// "value unknown", the average cost and the unrealised result (both labelled as estimates), the history of events with
/// edit and delete (Undo), and the prices. Buying, selling, moving, adding and removing are Advanced; a correction is
/// also offered in Simple.
/// </summary>
public sealed partial class HoldingDetailViewModel(
    HoldingStore holdings,
    ZananceStore store,
    HoldingText text,
    Translator translator,
    ILocalizationService localization,
    TimeProvider time) : ViewModelBase, IQueryAttributable
{
    private Guid _id;
    private AssetType? _type;
    private HoldingGroup? _deleted;

    public ObservableCollection<string> PerLocation { get; } = [];

    public ObservableCollection<HoldingEventRow> Events { get; } = [];

    public ObservableCollection<ValuationRow> Valuations { get; } = [];

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = Symbol.Diamond;

    [ObservableProperty]
    public partial string? QuantityText { get; set; }

    [ObservableProperty]
    public partial string? DerivedText { get; set; }

    /// <summary>Gets the purity and the fine-metal amount, e.g. "750/1000 · ≈ 15.000 g fine gold".</summary>
    [ObservableProperty]
    public partial string? PurityText { get; set; }

    [ObservableProperty]
    public partial string? ValueText { get; set; }

    [ObservableProperty]
    public partial bool ValueUnknown { get; set; }

    [ObservableProperty]
    public partial string? AverageCostText { get; set; }

    [ObservableProperty]
    public partial string? UnrealisedText { get; set; }

    [ObservableProperty]
    public partial string? RealisedText { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial bool HasStock { get; set; }

    [ObservableProperty]
    public partial string? UndoText { get; set; }

    // The price form: per gram or unit, or a total value of the current quantity.
    [ObservableProperty]
    public partial IReadOnlyList<string> PriceModeNames { get; set; } = [];

    [ObservableProperty]
    public partial int PriceModeIndex { get; set; }

    [ObservableProperty]
    public partial string PriceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly PriceDate { get; set; }

    [ObservableProperty]
    public partial string? PriceLabel { get; set; }

    [ObservableProperty]
    public partial string? PriceError { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
            PriceDate = Today;
        }
    }

    public async Task LoadAsync()
    {
        _type = (await holdings.GetTypesAsync()).FirstOrDefault(t => t.Id == _id);
        NotFound = _type is null;
        if (_type is not { } type)
        {
            return;
        }

        var today = Today;
        IsAdvanced = (await store.GetSettingsAsync()).Mode == Core.Settings.ExperienceMode.Advanced;
        var events = await holdings.GetEventsAsync(type.Id);
        var valuations = await holdings.GetValuationsAsync(type.Id);
        var locations = (await holdings.GetLocationsAsync()).ToDictionary(l => l.Id, l => l.Name);
        var value = AssetValuationService.Values([type], events, valuations, today).FirstOrDefault();
        var quantity = value?.Quantity ?? 0;

        Name = type.Name;
        Icon = HoldingText.Icon(type);
        QuantityText = text.Quantity(quantity, type);
        DerivedText = text.DerivedWeight(quantity, type);
        HasStock = quantity > 0;
        var fine = Quantities.FineMassMg(quantity, type) is { } mg && type.Metal != Metal.None && quantity > 0
            ? text.FineMetal(new FineMetal(type.Metal, mg, 1, 0), withCount: false)
            : null;
        PurityText = string.Join(" · ", new[] { text.Purity(type), fine }.Where(s => s is not null));
        PurityText = PurityText.Length == 0 ? null : PurityText;

        PerLocation.Clear();
        foreach (var position in HoldingsLedger.Positions(events, today).Where(p => p.Quantity > 0))
        {
            PerLocation.Add(translator.Format("AssetEvent_At", text.Quantity(position.Quantity, type), locations.GetValueOrDefault(position.LocationId, "?")));
        }

        // A value needs a price; without one it is "unknown", never zero (ZEX-AS06).
        ValueUnknown = quantity > 0 && value?.Value is null;
        ValueText = value?.Value is { } known
            ? translator.Format(value.FromPurchase ? "Holding_ValuePurchase" : "Holding_Value", text.Money(known, type.PriceCurrencyCode), text.Date(value.PriceDate!.Value))
            : null;

        // Average cost and results are estimates from the recorded prices (ZEX-AS09).
        var basis = HoldingsLedger.Basis(events, type.Id);
        AverageCostText = basis.PerUnitMilli is { } perUnit && basis.Quantity > 0 ? translator.Format("Holding_AverageCost", text.Price(perUnit, type)) : null;
        UnrealisedText = basis.Basis is { } cost && value?.Value is { } current && basis.Quantity > 0
            ? translator.Format("Holding_Unrealised", text.Money(current - cost, type.PriceCurrencyCode, showPlus: true))
            : null;
        var realised = basis.Sales.Where(s => s.Result is not null).ToList();
        RealisedText = realised.Count > 0 ? translator.Format("Holding_Realised", text.Money(realised.Sum(s => s.Result!.Value), type.PriceCurrencyCode, showPlus: true)) : null;

        Events.Clear();
        foreach (var assetEvent in events.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt))
        {
            var place = locations.GetValueOrDefault(assetEvent.LocationId, "?");
            var detail = assetEvent.Kind == AssetEventKind.LocationTransfer
                ? translator.Format("Holding_Moved", place, locations.GetValueOrDefault(assetEvent.ToLocationId ?? Guid.Empty, "?"))
                : string.Join(" · ", new[] { place, assetEvent.Reason }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var money = assetEvent.Kind == AssetEventKind.Sale ? assetEvent.ProceedsAmount : assetEvent.BasisAmount;
            if (money is { } amount)
            {
                detail += " · " + text.Money(amount, type.PriceCurrencyCode);
            }

            var effect = assetEvent.TotalEffect;
            Events.Add(new HoldingEventRow(assetEvent.Id, text.EventKind(assetEvent.Kind), text.Date(assetEvent.Date), text.SignedQuantity(assetEvent, type), detail,
                effect > 0 ? Palette.IncomeText : effect < 0 ? Palette.ExpenseText : Palette.SecondaryText));
        }

        Valuations.Clear();
        foreach (var valuation in valuations.OrderByDescending(v => v.Date))
        {
            Valuations.Add(new ValuationRow(valuation.Id, text.Price(valuation.PricePerUnitMilli, type), text.Date(valuation.Date),
                valuation.Source == ValuationSource.Purchase ? translator["Holding_FromPurchase"] : valuation.Note));
        }

        PriceModeNames = [text.PerUnit(type), translator["Holding_PriceTotal"]];
        PriceLabel = translator.Format("Holding_PriceIn", type.PriceCurrencyCode);
    }

    [RelayCommand]
    private Task RecordAsync(string kind) =>
        Shell.Current.GoToAsync(AppShell.AssetEventEditorRoute, new Dictionary<string, object> { ["type"] = _id, ["kind"] = kind });

    [RelayCommand]
    private Task EditTypeAsync() => Shell.Current.GoToAsync(AppShell.AssetTypeEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private async Task OpenEventAsync(HoldingEventRow row)
    {
        var edit = translator["Common_Edit"];
        var delete = translator["Common_Delete"];
        var choice = await Shell.Current.DisplayActionSheetAsync(row.Description, translator["Common_Cancel"], delete, edit);
        if (choice == edit)
        {
            await Shell.Current.GoToAsync(AppShell.AssetEventEditorRoute, new Dictionary<string, object> { ["id"] = row.Id });
        }
        else if (choice == delete)
        {
            await DeleteEventAsync(row);
        }
    }

    // A purchase or sale is deleted with its money entry and fee; a later sale that needs it blocks the delete (AT17).
    private async Task DeleteEventAsync(HoldingEventRow row)
    {
        var (removed, conflict) = await holdings.DeleteEventAsync(row.Id);
        if (conflict is not null && _type is { } type)
        {
            var place = (await holdings.GetLocationsAsync()).FirstOrDefault(l => l.Id == conflict.LocationId)?.Name ?? "?";
            await Shell.Current.DisplayAlertAsync(translator["Holding_CannotDelete"],
                translator.Format("Holding_Conflict", text.Date(conflict.Date), text.Quantity(conflict.Available, type), place), translator["Common_Ok"]);
            return;
        }

        _deleted = removed;
        UndoText = removed is null ? null : translator.Format("Holding_Deleted", row.Title);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_deleted is { } group)
        {
            _deleted = null;
            UndoText = null;
            await holdings.RestoreAsync(group);
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task SavePriceAsync()
    {
        if (_type is not { } type)
        {
            return;
        }

        PriceError = null;
        if (!MoneyText.TryParse(PriceText, type.PriceCurrencyCode, localization.CurrentCulture, out var amount) || amount <= 0)
        {
            PriceError = translator["Amount_Invalid"];
            return;
        }

        // A total is turned into a price per gram or unit of the quantity held on that day.
        long perUnitMilli = amount * 1_000;
        if (PriceModeIndex == 1)
        {
            var held = HoldingsLedger.Quantity(await holdings.GetEventsAsync(type.Id), type.Id, PriceDate);
            if (held <= 0)
            {
                PriceError = translator["Holding_TotalNeedsQuantity"];
                return;
            }

            perUnitMilli = AssetValuationService.PricePerUnitMilliOf(amount, held);
        }

        await holdings.SaveValuationAsync(new AssetValuation
        {
            AssetTypeId = type.Id,
            CurrencyCode = type.PriceCurrencyCode,
            Date = PriceDate,
            PricePerUnitMilli = perUnitMilli,
            Source = ValuationSource.Manual,
        });
        PriceText = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeletePriceAsync(ValuationRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Holding_DeletePrice"], row.Description, translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await holdings.DeleteValuationAsync(row.Id);
            await LoadAsync();
        }
    }

    // Archiving hides the type from lists and Home; its history stays.
    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (_type is { } type && await Shell.Current.DisplayAlertAsync(translator["Holding_Archive"], translator["Holding_ArchiveMessage"], translator["Holding_Archive"], translator["Common_Cancel"]))
        {
            type.IsArchived = true;
            await holdings.SaveTypeAsync(type);
            await Shell.Current.GoToAsync("..");
        }
    }
}
