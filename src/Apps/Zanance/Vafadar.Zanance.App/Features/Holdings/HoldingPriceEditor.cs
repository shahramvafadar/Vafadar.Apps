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
    private readonly Presentation.MoneyDraftInput _money = new();
    private long _modeRevision;
    private long _dateRevision;
    private bool _publishingDisplay;
    private string? _feedbackKey;
    private bool _acceptedNeedsRefresh;
    partial void OnDateChanged(DateOnly value) => _dateRevision++;
    /// <summary>Gets whether the inline form contains an unaccepted original price.</summary>
    public bool IsDirty => !string.IsNullOrWhiteSpace(Raw);
    /// <summary>Captures raw/date/mode and input history for one native consent.</summary>
    internal string CaptureDraft() => Draft();
    /// <summary>Requires the actual owning frame independently of a failed source cover.</summary>
    internal Func<bool>? ViewActive { get; set; }
    partial void OnModeIndexChanged(int value) { if (!_publishingDisplay) _modeRevision++; }
    /// <summary>Gets the manual price's original monetary unit until complete clearing and reentry.</summary>
    public string InputUnitName => _money.UnitName(_display?.Inputs.Type.PriceCurrencyCode ?? "EUR");
    /// <summary>Explains the manual price's original display-unit factor.</summary>
    public string? InputUnitNote => Presentation.DisplayUnitNote.ForInput(translator, _money, _display?.Inputs.Type.PriceCurrencyCode ?? "EUR");
    /// <summary>Explains the manual price's retained original numeric format.</summary>
    public string? InputFormatNote => _money.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;
    /// <summary>Gets field-local currency/unit review independently of history or write feedback.</summary>
    public string? InputContextError => _display is { } display && _money.NeedsReview(display.Inputs.Type.PriceCurrencyCode)
        ? translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(display.Inputs.Type.PriceCurrencyCode)) : null;
    partial void OnRawChanged(string value)
    {
        _money.ChangeText(value, _display?.Inputs.Type.PriceCurrencyCode ?? "EUR", localization.CurrentCulture);
        RefreshMoneyTexts();
    }
    /// <summary>Publishes display-only monetary captions without renewing the original raw price.</summary>
    public void RefreshMoneyTexts()
    {
        OnPropertyChanged(nameof(InputUnitName)); OnPropertyChanged(nameof(InputUnitNote)); OnPropertyChanged(nameof(InputFormatNote)); OnPropertyChanged(nameof(InputContextError));
    }
    private void OnMoneyDisplayChanged(object? sender, EventArgs args)
    {
        if (!(ViewActive?.Invoke() ?? true)) return;
        var selected = ModeIndex;
        _publishingDisplay = true;
        try
        {
            if (_display is { } display)
            {
                ModeNames = [text.PerUnit(display.Inputs.Type), translator["Holding_PriceTotal"]];
                ModeIndex = selected; Label = translator.Format("Holding_PriceIn", display.Inputs.Type.PriceCurrencyCode);
            }
            if (_feedbackKey is { } key) Error = translator[key];
            RefreshMoneyTexts();
        }
        finally { _publishingDisplay = false; }
    }
    private void Feedback(string? key) { _feedbackKey = key; Error = key is null ? null : translator[key]; }
    /// <summary>Tracks display-only captions while the owning detail is actually presented.</summary>
    public void SetMoneyPresentationActive(bool active)
    {
        localization.Changed -= OnMoneyDisplayChanged;
        if (active) localization.Changed += OnMoneyDisplayChanged;
        RefreshMoneyTexts();
    }
    /// <summary>Gets the exact displayed packet for reviewing an original history action in one coherent read.</summary>
    internal HoldingPriceReview? DisplayedReview => _display;
    /// <summary>Reloads the original owning detail after acceptance or an explicit Refresh.</summary>
    public Func<Task>? ReloadView { get; set; }
    /// <summary>Reads through the owning detail's coherent file/settings transaction when attached to that view.</summary>
    internal Func<Guid, Task<HoldingPriceReview>>? ReadOriginal { get; set; }
    /// <summary>Distinguishes the original explicit refresh from an ordinary appearance that must retire raw authority.</summary>
    internal bool ReadingExplicitRefresh { get; private set; }
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
    public bool CanSubmit => (ViewActive?.Invoke() ?? true) && (ViewReady?.Invoke() ?? true) && !Busy && _review is not null && !NeedsRefresh;
    /// <summary>Applies the owning detail cover without changing standalone editor authority or raw input.</summary>
    internal Func<bool>? ViewReady { get; set; }
    /// <summary>Refreshes native command enablement after the owning complete cover changes.</summary>
    internal void NotifyViewReady() { RefreshMoneyTexts(); PublishState(); }
    /// <summary>Gets whether an explicit read-only refresh is available.</summary>
    public bool CanRefresh => !Busy && (ViewActive?.Invoke() ?? true);
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
            var loaded = ReadOriginal is { } read ? await read(id) : await store.ReadPriceReviewAsync(id);
            if (revision != _revision) throw new HoldingEventReviewChangedException("Original price load retired.");
            _review = _display = loaded; _initialized = true; PublishState();
        }
        ModeNames = [text.PerUnit(_display.Inputs.Type), translator["Holding_PriceTotal"]];
        Label = translator.Format("Holding_PriceIn", _display.Inputs.Type.PriceCurrencyCode);
        RefreshMoneyTexts();
        return _display.Inputs;
    }
    private string Draft() => Presentation.DraftFingerprint.Create(Raw, Date, ModeIndex, _money.Stamp, _money.Revision, _modeRevision, _dateRevision);
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SaveAsync()
    {
        if (!CanSubmit || _review is not { } review) return;
        Feedback(null);
        var type = review.Inputs.Type;
        RefreshMoneyTexts();
        if (_money.NeedsReview(type.PriceCurrencyCode)) return;
        if (!_money.TryParse(out var amount)
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
        await WriteAsync(value.Id, false, draft, accepted => store.SaveReviewedValuationAsync(review, value,
            () => revision == _revision && ReferenceEquals(_review, review) && draft == Draft() && CanOwnWrite() && !_money.NeedsReview(type.PriceCurrencyCode), accepted: accepted));
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
            await WriteAsync(id, true, draft, accepted => store.DeleteReviewedValuationAsync(review, id,
                () => revision == _revision && ReferenceEquals(_review, review) && draft == Draft() && CanOwnWrite(), accepted: accepted));
        }
        finally { Busy = false; }
    }
    private bool CanOwnWrite() => (ViewActive?.Invoke() ?? true) && (ViewReady?.Invoke() ?? true);
    private async Task WriteAsync(Guid id, bool deletion, string submitted, Func<Action, Task> write)
    {
        Busy = true; var accepted = false; Feedback(null);
        // D-644: expose the actual receipt before fallible Changed handlers can alter the form.
        void Accept() { accepted = true; AcceptedId = id; LastWasDeletion = deletion; _acceptedNeedsRefresh = true; }
        try { await write(Accept); Accept(); }
        catch (CommittedWriteNotificationException) { accepted = true; Feedback("Holding_PriceSavedRefresh"); }
        catch (HoldingEventReviewChangedException) { Retire(); Feedback("Holding_PriceSourceChanged"); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally
        {
            if (accepted)
            {
                AcceptedId = id; LastWasDeletion = deletion; Retire();
                // A deleted old row never clears a separately retained unsaved price draft.
                if (!deletion && submitted == Draft()) Raw = string.Empty;
                if (!deletion && IsDirty) Feedback("Holding_PriceSavedDraftChanged");
            }
            Busy = false;
        }
        if (accepted && Error is null)
        {
            if (CanRefresh) await RefreshAsync();
            else Feedback("Holding_PriceSavedRefresh");
        }
    }
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (!CanRefresh) return;
        Busy = true; Retire(); _display = null; ReadingExplicitRefresh = true;
        try
        {
            NeedsRefresh = false;
            if (ReloadView is { } reload) await reload(); else await LoadAsync(_id);
            _acceptedNeedsRefresh = false; Feedback(null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Retire();
            if (_acceptedNeedsRefresh) Feedback("Holding_PriceSavedRefresh");
            else { Feedback(null); Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        }
        finally { ReadingExplicitRefresh = false; Busy = false; }
    }
}
