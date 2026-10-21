using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>
/// Creates or edits an asset type (ZEX-S0401): name, kind, weight or count, metal, purity (karat or fineness), unit weight
/// and divisibility of counted items, and the currency of its prices. Quantities add up only within one type, so 18 k
/// and 24 k gold are two types. After a new type the user records what they already own (an opening holding).
/// </summary>
public sealed partial class AssetTypeEditorViewModel : ViewModelBase, Presentation.IUnsavedChanges
{
    private static readonly Metal[] Metals = [Metal.Gold, Metal.Silver, Metal.Platinum, Metal.Palladium, Metal.Other, Metal.None];

    private readonly HoldingStore _holdings;
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private AssetType? _existing;
    private readonly IAppInteraction _interaction;
    private HoldingTypeReview? _review;
    private Guid _id;
    private long _revision;
    private bool _initialized, _loadRequested;
    /// <summary>Gets whether the original type is known to have been saved, even after a following effect fails.</summary>
    public bool Committed { get; private set; }
    /// <summary>Gets the stable original saved identity without creating another type.</summary>
    public Guid? AcceptedId => Committed ? _id : null;
    /// <summary>Gets whether the current original form may submit.</summary>
    public bool CanSave => !IsBusy && !Committed && _review is not null;
    /// <summary>Gets whether raw fields remain editable without changing original source authority.</summary>
    public bool CanEdit => _initialized && !IsBusy && !Committed;
    /// <summary>Gets the actual close/cancel caption for this original form.</summary>
    public string CloseText => _translator[Committed ? "Common_Close" : "Common_Cancel"];
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
    /// <summary>Displays a completed original native caption with permanently retired write authority.</summary>
    internal void ShowRetainedOutcome(string key, bool committed, bool isNew)
    { Committed = committed; RetireReview(); Title = _translator[isNew ? "AssetType_NewTitle" : "AssetType_EditTitle"]; Error = _translator[key]; }
#endif
    private void PublishState()
    {
        OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CloseText));
        OnPropertyChanged(nameof(AcceptedId)); SaveCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Retires departed continuations without replacing raw input or a known saved identity.</summary>
    public void RetireReview() { Interlocked.Increment(ref _revision); _review = null; PublishState(); }

    public AssetTypeEditorViewModel(HoldingStore holdings, ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction)
    {
        _interaction = interaction;
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

    /// <summary>Loads one original type or a stable new identity; empty native redelivery retains the current form.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (_loadRequested || IsBusy) { query.Clear(); return; }
        _loadRequested = true;
        var editing = query.TryGetValue("id", out var value) && value is Guid;
        _id = editing ? (Guid)value! : Guid.NewGuid(); query.Clear();
        var revision = ++_revision; IsBusy = true; PublishState();
        try
        {
            var currency = (await _store.GetSettingsAsync()).DefaultCurrencyCode;
            var review = await _holdings.ReadTypeReviewAsync(_id, editing);
            if (revision != _revision) return;
            _review = review; CurrencyCode = currency;
            if (review.Inputs.Type is { } type)
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
                DimensionLocked = review.Inputs.Events.Count > 0;
            }
            _initialized = true; _snapshot = Snapshot(); Error = null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            RetireReview(); Error = exception is HoldingEventReviewChangedException ? _translator["AssetType_SourceChanged"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true);
        }
        finally { IsBusy = false; PublishState(); }
    }

    partial void OnKindIndexChanged(int value)
    {
        if (_initialized) _revision++;
        var kind = (AssetKind)value;
        HasMetal = kind is AssetKind.PreciousMetal or AssetKind.CoinOrBar;
        RefreshNumericPresentation();

        // Coins and items are counted; metals are weighed. Both stay changeable until something is recorded.
        if (!DimensionLocked && _existing is null)
        {
            DimensionIndex = kind is AssetKind.CoinOrBar or AssetKind.CountableItem ? (int)AssetDimension.Count : (int)AssetDimension.Mass;
        }
    }

    partial void OnDimensionIndexChanged(int value)
    { if (_initialized) _revision++; IsCount = value == (int)AssetDimension.Count; RefreshNumericPresentation(); }

    partial void OnPurityIndexChanged(int value)
    { if (_initialized) _revision++; IsOtherPurity = value >= Purity.Common.Count; PurityError = null; RefreshNumericPresentation(); }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // A second tap while saving would create the type twice.
        if (!CanSave)
        {
            return;
        }

        IsBusy = true; PublishState();
        try
        {
            await SaveCoreAsync();
        }
        finally
        {
            IsBusy = false; PublishState();
        }
    }

    private async Task SaveCoreAsync()
    {
        Error = null;
        NameError = null; PurityError = null; UnitWeightError = null;
        var invalid = false;
        if (string.IsNullOrWhiteSpace(Name))
        {
            NameError = _translator["AssetType_NameRequired"];
            invalid = true;
        }

        int? purity = null;
        if (HasMetal && PurityIndex < Purity.Common.Count)
        {
            purity = Purity.Common[PurityIndex].Per10000;
        }
        else if (HasMetal && !string.IsNullOrWhiteSpace(PurityText))
        {
            if (!Purity.TryParse(PurityText, _purityCulture ?? _localization.CurrentCulture, out var parsed))
            {
                PurityError = _translator["AssetType_PurityInvalid"];
                invalid = true;
            }

            purity = parsed;
        }

        long? unitWeight = null;
        if (IsCount && !string.IsNullOrWhiteSpace(UnitWeightText))
        {
            var probe = new AssetType { Name = Name, PriceCurrencyCode = CurrencyCode };
            if (!Quantities.TryParse(UnitWeightText, QuantityUnit.Gram, probe, _weightCulture ?? _localization.CurrentCulture, out var weight))
            {
                UnitWeightError = _translator["AssetType_WeightInvalid"];
                invalid = true;
            }

            unitWeight = weight;
        }

        if (invalid) return;

        var review = _review ?? throw new HoldingEventReviewChangedException("Original type editor review is unavailable.");
        var revision = _revision; var originalDraft = Snapshot();
        var type = _existing is { } original ? HoldingTypeReview.Copy(original)
            : new AssetType(_id) { Name = Name.Trim(), PriceCurrencyCode = CurrencyCode };
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
        try
        {
            if (!await _holdings.SaveReviewedTypeAsync(review, type,
                () => revision == _revision && ReferenceEquals(_review, review) && originalDraft == Snapshot()))
            {
                Error = _translator["AssetType_DimensionLocked"]; return;
            }
            Committed = true;
        }
        catch (CommittedWriteNotificationException) { Committed = true; Error = _translator["AssetType_SavedNeedsReview"]; }
        catch (HoldingEventReviewChangedException) { RetireReview(); Error = _translator["AssetType_SourceChanged"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true); }
        finally
        {
            if (Committed) { _snapshot = Snapshot(); RetireReview(); }
            PublishState();
        }
        if (!Committed || Error is not null) return;
        try
        {
            // A new saved identity moves once to opening quantity; a failed return never replays its creation.
            if (!review.Editing)
                await _interaction.NavigateAsync($"../{AppRoutes.AssetEventEditorRoute}", new Dictionary<string, object>
                { ["type"] = _id, ["kind"] = nameof(AssetEventKind.Opening) });
            else await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { Error = _translator["AssetType_SavedNeedsReview"]; }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsDirty && !await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"],
            _translator["Common_Discard"], _translator["Common_KeepEditing"])) return;
        await _interaction.NavigateAsync("..");
    }

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => !Committed && _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.DraftFingerprint.Create(
        Name, KindIndex, DimensionIndex, MetalIndex, PurityIndex, PurityText, UnitWeightText, CountUnitName, Divisible, CurrencyCode, Note);
}
