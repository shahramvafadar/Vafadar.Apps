using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>
/// Creates or edits an asset type (ZEX-S0401): name, kind, weight or count, metal, purity (karat or fineness), unit weight
/// and divisibility of counted items, and the currency of its prices. Quantities add up only within one type, so 18 k
/// and 24 k gold are two types. After a new type the user records what they already own (an opening holding).
/// </summary>
public sealed partial class AssetTypeEditorViewModel : ViewModelBase, IQueryAttributable
{
    private static readonly Metal[] Metals = [Metal.Gold, Metal.Silver, Metal.Platinum, Metal.Palladium, Metal.Other, Metal.None];

    private readonly HoldingStore _holdings;
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private AssetType? _existing;

    public AssetTypeEditorViewModel(HoldingStore holdings, ZananceStore store, Translator translator, ILocalizationService localization)
    {
        _holdings = holdings;
        _store = store;
        _translator = translator;
        _localization = localization;
        Title = translator["AssetType_NewTitle"];
        KindNames = [.. Enum.GetValues<AssetKind>().Select(k => translator[$"AssetKind_{k}"])];
        DimensionNames = [translator["AssetType_ByWeight"], translator["AssetType_ByCount"]];
        MetalNames = [.. Metals.Select(m => translator[$"Metal_{m}"])];
        PurityNames = [.. Purity.Common.Select(p => p.Label), translator["AssetType_PurityOther"]];
        PurityIndex = Purity.Common.Count;
    }

    public IReadOnlyList<string> KindNames { get; }

    public IReadOnlyList<string> DimensionNames { get; }

    public IReadOnlyList<string> MetalNames { get; }

    public IReadOnlyList<string> PurityNames { get; }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial int DimensionIndex { get; set; }

    [ObservableProperty]
    public partial bool IsCount { get; set; }

    [ObservableProperty]
    public partial bool DimensionLocked { get; set; }

    [ObservableProperty]
    public partial bool HasMetal { get; set; } = true;

    [ObservableProperty]
    public partial int MetalIndex { get; set; }

    /// <summary>Gets or sets the common purity chosen (24, 22, 21, 18, 14 k) or the last index for free input.</summary>
    [ObservableProperty]
    public partial int PurityIndex { get; set; }

    /// <summary>Gets or sets a free purity: karat ("18") or fineness ("750", "999.9"); empty = unknown.</summary>
    [ObservableProperty]
    public partial string PurityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsOtherPurity { get; set; } = true;

    [ObservableProperty]
    public partial string UnitWeightText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountUnitName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Divisible { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; } = Currencies.Euro.Code;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        CurrencyCode = (await _store.GetSettingsAsync()).DefaultCurrencyCode;
        if (query.TryGetValue("id", out var value) && value is Guid id && (await _holdings.GetTypesAsync()).FirstOrDefault(t => t.Id == id) is { } type)
        {
            _existing = type;
            Title = _translator["AssetType_EditTitle"];
            Name = type.Name;
            KindIndex = (int)type.Kind;
            DimensionIndex = (int)type.Dimension;
            MetalIndex = Math.Max(0, Array.IndexOf(Metals, type.Metal));
            var common = Purity.Common.ToList().FindIndex(p => p.Per10000 == type.PurityPer10000);
            PurityIndex = common >= 0 ? common : Purity.Common.Count;
            PurityText = common < 0 && type.PurityPer10000 is { } purity ? (purity / 10m).ToString("0.#", _localization.CurrentCulture) : string.Empty;
            UnitWeightText = type.UnitWeightMg is { } weight ? (weight / 1000m).ToString("0.###", _localization.CurrentCulture) : string.Empty;
            CountUnitName = type.CountUnitName ?? string.Empty;
            Divisible = type.Divisible;
            CurrencyCode = type.PriceCurrencyCode;
            Note = type.Note ?? string.Empty;
            DimensionLocked = (await _holdings.GetEventsAsync(type.Id)).Count > 0;
        }

        query.Clear();
    }

    partial void OnKindIndexChanged(int value)
    {
        var kind = (AssetKind)value;
        HasMetal = kind is AssetKind.PreciousMetal or AssetKind.CoinOrBar;

        // Coins and items are counted; metals are weighed. Both stay changeable until something is recorded.
        if (!DimensionLocked && _existing is null)
        {
            DimensionIndex = kind is AssetKind.CoinOrBar or AssetKind.CountableItem ? (int)AssetDimension.Count : (int)AssetDimension.Mass;
        }
    }

    partial void OnDimensionIndexChanged(int value) => IsCount = value == (int)AssetDimension.Count;

    partial void OnPurityIndexChanged(int value) => IsOtherPurity = value >= Purity.Common.Count;

    [RelayCommand]
    private async Task SaveAsync()
    {
        Error = null;
        var culture = _localization.CurrentCulture;
        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = _translator["AssetType_NameRequired"];
            return;
        }

        int? purity = null;
        if (HasMetal && PurityIndex < Purity.Common.Count)
        {
            purity = Purity.Common[PurityIndex].Per10000;
        }
        else if (HasMetal && !string.IsNullOrWhiteSpace(PurityText))
        {
            if (!Purity.TryParse(PurityText, culture, out var parsed))
            {
                Error = _translator["AssetType_PurityInvalid"];
                return;
            }

            purity = parsed;
        }

        long? unitWeight = null;
        if (IsCount && !string.IsNullOrWhiteSpace(UnitWeightText))
        {
            var probe = new AssetType { Name = Name, PriceCurrencyCode = CurrencyCode };
            if (!Quantities.TryParse(UnitWeightText, QuantityUnit.Gram, probe, culture, out var weight))
            {
                Error = _translator["AssetType_WeightInvalid"];
                return;
            }

            unitWeight = weight;
        }

        var type = _existing ?? new AssetType { Name = Name.Trim(), PriceCurrencyCode = CurrencyCode };
        type.Name = Name.Trim();
        type.Kind = (AssetKind)KindIndex;
        type.Dimension = (AssetDimension)DimensionIndex;
        type.Metal = HasMetal ? Metals[Math.Clamp(MetalIndex, 0, Metals.Length - 1)] : Metal.None;
        type.PurityPer10000 = purity;
        type.UnitWeightMg = IsCount ? unitWeight : null;
        type.CountUnitName = IsCount && !string.IsNullOrWhiteSpace(CountUnitName) ? CountUnitName.Trim() : null;
        type.Divisible = IsCount && Divisible;
        type.PriceCurrencyCode = CurrencyCode;
        type.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        if (!await _holdings.SaveTypeAsync(type))
        {
            Error = _translator["AssetType_DimensionLocked"];
            return;
        }

        // A new type: record what is already owned next – no money is taken from an account (AT39).
        if (_existing is null)
        {
            await Shell.Current.GoToAsync($"../{AppShell.AssetEventEditorRoute}", new Dictionary<string, object>
            {
                ["type"] = type.Id,
                ["kind"] = nameof(AssetEventKind.Opening),
            });
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
