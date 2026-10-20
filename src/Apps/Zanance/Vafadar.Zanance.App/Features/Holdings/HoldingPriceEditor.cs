using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>The actual manual-price form: original facts, raw draft, confirmation and known acceptance.</summary>
public sealed partial class HoldingPriceEditor(HoldingStore store, HoldingText text, Translator translator,
    ILocalizationService localization, TimeProvider time, IAppInteraction interaction) : ObservableObject
{
    private HoldingPriceReview? _review, _display;
    private long _revision;
    private Guid _id;
    private bool _initialized;
    /// <summary>Reloads the original owning detail after acceptance or an explicit Refresh.</summary>
    public Func<Task>? ReloadView { get; set; }
    /// <summary>Gets whether the accepted operation was a deletion.</summary>
    public bool LastWasDeletion { get; private set; }
    /// <summary>Gets the last known accepted price identity, even if publication failed.</summary>
    public Guid? AcceptedId { get; private set; }
    /// <summary>Gets the current translated per-unit and total choices.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> ModeNames { get; set; } = [];
    /// <summary>Gets or sets the original draft's per-unit or total choice.</summary>
    [ObservableProperty] public partial int ModeIndex { get; set; }
    /// <summary>Gets or sets the exact raw price, retained after refusal.</summary>
    [ObservableProperty] public partial string Raw { get; set; } = string.Empty;
    /// <summary>Gets or sets the original valuation date without changing earlier prices.</summary>
    [ObservableProperty] public partial DateOnly Date { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    /// <summary>Gets the complete translated input label and original price currency.</summary>
    [ObservableProperty] public partial string? Label { get; set; }
    /// <summary>Gets field-local refusal or known-acceptance feedback.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets whether original authority requires an explicit read-only refresh.</summary>
    [ObservableProperty] public partial bool NeedsRefresh { get; set; }
    /// <summary>Gets whether confirmation, writing or refresh owns the form.</summary>
    [ObservableProperty] public partial bool Busy { get; set; }
    /// <summary>Gets whether original facts still permit the price form to submit.</summary>
    public bool CanSubmit => !Busy && _review is not null && !NeedsRefresh;
    /// <summary>Gets whether an explicit read-only refresh is available.</summary>
    public bool CanRefresh => !Busy;
    partial void OnBusyChanged(bool value) => PublishState();
    partial void OnNeedsRefreshChanged(bool value) => PublishState();
    private void PublishState()
    {
        OnPropertyChanged(nameof(CanSubmit)); OnPropertyChanged(nameof(CanRefresh));
        SaveCommand.NotifyCanExecuteChanged(); RefreshCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Retires a departed original review while retaining raw inputs and displayed facts.</summary>
    public void Retire()
    { Interlocked.Increment(ref _revision); _review = null; NeedsRefresh = _initialized; PublishState(); }
    /// <summary>Retires native departure; an empty ordinary form may read fresh history on return.</summary>
    public void Leave()
    {
        var readOnReturn = string.IsNullOrWhiteSpace(Raw) && !NeedsRefresh;
        Retire();
        if (readOnReturn) { _display = null; NeedsRefresh = false; }
    }
    /// <summary>Reads a coherent original detail packet without silently refreshing an unsaved or retired draft.</summary>
    public async Task<HoldingPriceInputs> LoadAsync(Guid id)
    {
        if (_id != id) { Retire(); _display = null; _id = id; Raw = string.Empty; _initialized = false; NeedsRefresh = false; }
        if (_display is null || (!Busy && !NeedsRefresh && string.IsNullOrWhiteSpace(Raw)))
        {
            var revision = _revision;
            var loaded = await store.ReadPriceReviewAsync(id);
            if (revision != _revision) throw new HoldingEventReviewChangedException("Original price load retired.");
            _review = _display = loaded; _initialized = true; PublishState();
        }
        ModeNames = [text.PerUnit(_display.Inputs.Type), translator["Holding_PriceTotal"]];
        Label = translator.Format("Holding_PriceIn", _display.Inputs.Type.PriceCurrencyCode);
        return _display.Inputs;
    }
    private string Draft() => Presentation.DraftFingerprint.Create(Raw, Date, ModeIndex);
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SaveAsync()
    {
        if (!CanSubmit || _review is not { } review) return;
        Error = null;
        var type = review.Inputs.Type;
        if (!MoneyText.TryParse(Raw, type.PriceCurrencyCode, localization.CurrentCulture, out var amount)
            || amount <= 0 || amount > long.MaxValue / 1_000 || ModeIndex is < 0 or > 1)
        { Error = translator["Amount_Invalid"]; return; }
        long price;
        try
        {
            var quantity = HoldingsLedger.Quantity(review.Inputs.Events, type.Id, Date);
            if (ModeIndex == 1 && quantity <= 0) { Error = translator["Holding_TotalNeedsQuantity"]; return; }
            price = ModeIndex == 1 ? AssetValuationService.PricePerUnitMilliOf(amount, quantity) : amount * 1_000;
            // Refuse an unrepresentable current readout before accepting a price that the detail cannot display.
            _ = AssetValuationService.ValueOf(quantity, price);
            if (price <= 0) { Error = translator["Amount_Invalid"]; return; }
        }
        catch (OverflowException) { Error = translator["Amount_Invalid"]; return; }
        var value = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = type.PriceCurrencyCode,
            Date = Date, PricePerUnitMilli = price, Source = ValuationSource.Manual };
        var draft = Draft(); var revision = _revision;
        await WriteAsync(value.Id, false, () => store.SaveReviewedValuationAsync(review, value,
            () => revision == _revision && ReferenceEquals(_review, review) && draft == Draft()));
    }
    /// <summary>Confirms the original row and refuses a changed confirmation or any changed original source.</summary>
    public async Task DeleteAsync(Guid id, string description)
    {
        if (!CanSubmit || _review is not { } review) return;
        var revision = _revision; var draft = Draft(); Busy = true;
        try
        {
            if (!await interaction.ConfirmAsync(translator["Holding_DeletePrice"], description,
                translator["Common_Delete"], translator["Common_Cancel"])) return;
            await WriteAsync(id, true, () => store.DeleteReviewedValuationAsync(review, id,
                () => revision == _revision && ReferenceEquals(_review, review) && draft == Draft()));
        }
        finally { Busy = false; }
    }
    private async Task WriteAsync(Guid id, bool deletion, Func<Task> write)
    {
        Busy = true; var accepted = false; Error = null; AcceptedId = null; LastWasDeletion = false;
        try { await write(); accepted = true; }
        catch (CommittedWriteNotificationException) { accepted = true; Error = translator["Holding_PriceSavedRefresh"]; }
        catch (HoldingEventReviewChangedException) { Retire(); Error = translator["Holding_PriceSourceChanged"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally
        {
            if (accepted)
            {
                AcceptedId = id; LastWasDeletion = deletion; Retire();
                // A deleted old row never clears a separately retained unsaved price draft.
                if (!deletion) Raw = string.Empty;
            }
            Busy = false;
        }
        if (accepted && Error is null) await RefreshAsync();
    }
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (Busy) return;
        Busy = true; Retire(); _display = null;
        try
        {
            NeedsRefresh = false;
            if (ReloadView is { } reload) await reload(); else await LoadAsync(_id);
            Error = null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Retire(); Error = AcceptedId is not null ? translator["Holding_PriceSavedRefresh"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true);
        }
        finally { Busy = false; }
    }
}
