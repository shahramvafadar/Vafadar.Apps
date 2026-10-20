using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Original holding history confirmation and file-bound short Undo across fallible native publication.</summary>
public sealed partial class HoldingHistoryEditor(HoldingStore store, HoldingText text, Translator translator,
    IAppInteraction interaction) : ObservableObject
{
    private HoldingPriceReview? _displayed;
    private HoldingGroup? _undo;
    private long _revision;
    private bool _undoInvalid;
    private AssetEventKind _deletedKind;
    private string? _errorKey;
    private int _refreshOutcome;
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
    private bool _presentationOnly;
    /// <summary>Gets only the actual original receipt for independent retained-outcome qualification.</summary>
    internal HoldingGroup? RetainedDeletion => _undo;
    /// <summary>Rehydrates a completed original caption with retired authority; it cannot submit Undo or history work.</summary>
    internal void ShowRetainedOutcome(HoldingGroup original, string key, bool showUndo)
    {
        _presentationOnly = true; Retire(); _undo = showUndo ? original : null; _undoInvalid = true;
        _deletedKind = original.Events.Single().Kind; Feedback(key); Publish();
    }
#endif
    /// <summary>Reads and publishes current detail facts through the owning page's explicit refresh.</summary>
    public Func<Task<HoldingPriceReview?>>? RefreshView { get; set; }
    /// <summary>Gets whether the latest original deletion is known to have committed.</summary>
    public bool DeleteCommitted { get; private set; }
    /// <summary>Gets whether the latest original Undo is known to have committed.</summary>
    public bool UndoCommitted { get; private set; }
    /// <summary>Gets whether native work currently owns the history actions.</summary>
    [ObservableProperty] public partial bool Busy { get; set; }
    /// <summary>Gets source-change, refusal or known-acceptance feedback without losing the original receipt.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets whether an explicit current-data publication is required.</summary>
    [ObservableProperty] public partial bool NeedsRefresh { get; set; }
    /// <summary>Gets whether original displayed rows can open a fresh, coherent action review.</summary>
    public bool CanOpen => !Busy && _displayed is not null && !NeedsRefresh;
    /// <summary>Gets whether the actual original deletion can still be restored, independently of readback failure.</summary>
    public bool CanUndo => !Busy && _undo is not null && !_undoInvalid;
    /// <summary>Gets whether the original short offer is retained, including a safely disabled obsolete receipt.</summary>
    public bool HasUndo => _undo is not null;
    /// <summary>Gets whether the footer has feedback or an original short Undo offer.</summary>
    public bool HasFeedback => HasUndo || Error is not null || NeedsRefresh;
    /// <summary>Gets whether read-only refresh can begin.</summary>
    public bool CanRefresh => !Busy;
    /// <summary>Gets the complete translated original deletion caption.</summary>
    public string? UndoText => HasUndo ? translator.Format("Holding_Deleted", text.EventKind(_deletedKind)) : null;
    partial void OnBusyChanged(bool value) => Publish();
    partial void OnErrorChanged(string? value) => Publish();
    partial void OnNeedsRefreshChanged(bool value) => Publish();
    private void Publish()
    {
        OnPropertyChanged(nameof(CanOpen)); OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(HasUndo));
        OnPropertyChanged(nameof(HasFeedback)); OnPropertyChanged(nameof(CanRefresh)); OnPropertyChanged(nameof(UndoText));
        OpenCommand.NotifyCanExecuteChanged(); UndoCommand.NotifyCanExecuteChanged(); RefreshCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Publishes exactly the packet used to render the original history, without manufacturing Undo authority.</summary>
    public void PublishDisplayed(HoldingPriceReview? review)
    {
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
        if (_presentationOnly) { if (_errorKey is { } originalKey) Error = translator[originalKey]; Publish(); return; }
#endif
        if (!ReferenceEquals(review, _displayed)) { Interlocked.Increment(ref _revision); _displayed = review; }
        if (review is not null) { NeedsRefresh = false; if (!Busy) Feedback(null); }
        if (_errorKey is { } key) Error = translator[key];
        Publish();
    }
    /// <summary>Retires pending departed actions but keeps the original file-bound Undo result for its existing short offer.</summary>
    public void Retire()
    { Interlocked.Increment(ref _revision); _displayed = null; NeedsRefresh = true; Publish(); }
    private void Feedback(string? key) { _errorKey = key; Error = key is null ? null : translator[key]; }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task OpenAsync(Guid id)
    {
        if (!CanOpen || _displayed is not { } displayed) return;
        Busy = true; Feedback(null); var revision = _revision; var accepted = false;
        try
        {
            var review = await store.ReadDeletionReviewAsync(displayed, id);
            if (revision != _revision) return;
            var original = review.Inputs.Events.Single(e => e.Id == id);
            var description = Description(review, original);
            var choice = await interaction.ChooseAsync(description, translator["Common_Cancel"], translator["Common_Edit"], translator["Common_Delete"]);
            if (revision != _revision) return;
            if (choice == translator["Common_Edit"])
            { await interaction.NavigateAsync(AppRoutes.AssetEventEditorRoute, new Dictionary<string, object> { ["id"] = id }); return; }
            if (choice != translator["Common_Delete"] || !await interaction.ConfirmAsync(translator["Holding_HistoryDeleteTitle"],
                description + Environment.NewLine + Environment.NewLine + translator["Holding_HistoryDeleteMessage"],
                translator["Common_Delete"], translator["Common_Cancel"])) return;
            if (revision != _revision) return;
            HoldingDeleteResult result;
            try { result = await store.DeleteReviewedEventAsync(review, () => revision == _revision && ReferenceEquals(displayed, _displayed)); }
            catch (CommittedWriteNotificationException<HoldingDeleteResult> known)
            { result = known.Result; Feedback("Holding_HistoryDeletedRefresh"); }
            if (result.Conflict is { } conflict)
            {
                var place = review.Inputs.Locations.Single(l => l.Id == conflict.LocationId).Name;
                Error = translator.Format("Holding_Conflict", text.Date(conflict.Date), text.Quantity(conflict.Available, review.Inputs.Type), place);
                return;
            }
            if (result.Removed is null) return;
            accepted = DeleteCommitted = true; UndoCommitted = false; _refreshOutcome = 1; _undo = result.Removed; _undoInvalid = false;
            _deletedKind = original.Kind; Retire();
        }
        catch (HoldingEventReviewChangedException)
        { Retire(); Feedback("Holding_HistorySourceChanged"); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); _errorKey = null; Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally { Busy = false; Publish(); }
        if (accepted && Error is null) await RefreshAsync();
    }
    private string Description(HoldingEventReadReview review, AssetEvent item)
    {
        var type = review.Inputs.Type;
        var location = review.Inputs.Locations.Single(l => l.Id == item.LocationId).Name;
        var lines = new List<string> { text.EventKind(item.Kind), text.Date(item.Date), text.SignedQuantity(item, type), location };
        foreach (var entry in review.Inputs.Entries)
        {
            var account = review.Inputs.Accounts.Single(a => a.Id == entry.AccountId);
            var amount = text.Money(entry.Amount, account.CurrencyCode);
            lines.Add(entry.Kind == EntryKind.Expense ? translator.Format("AssetEvent_FeeTitle", amount) + " · " + account.Name
                : account.Name + " · " + amount);
        }
        foreach (var refund in review.Inputs.Paybacks)
        {
            var account = review.Inputs.Accounts.Single(a => a.Id == refund.AccountId);
            lines.Add(translator.Format("Holding_HistoryKeptRefund", text.Money(refund.Amount, account.CurrencyCode)) + " · " + account.Name);
        }
        return string.Join(Environment.NewLine, lines);
    }
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        if (!CanUndo || _undo is not { } group) return;
        Busy = true; Feedback(null); var revision = _revision; var accepted = false;
        try
        {
            try { await store.RestoreReviewedHoldingAsync(group, () => revision == _revision && ReferenceEquals(group, _undo)); }
            catch (CommittedWriteNotificationException) { Feedback("Holding_HistoryRestoredRefresh"); }
            accepted = UndoCommitted = true; _refreshOutcome = 2; _undo = null; _undoInvalid = false; Retire();
        }
        catch (HoldingEventReviewChangedException)
        {
            // A departed uncommitted invocation must not destroy a still-valid original deletion receipt.
            if (revision != _revision) { Retire(); Feedback("Holding_HistoryUndoFailed"); }
            else { _undoInvalid = true; Retire(); Feedback("Holding_HistoryUndoChanged"); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Feedback("Holding_HistoryUndoFailed"); }
        finally { Busy = false; Publish(); }
        if (accepted && Error is null) await RefreshAsync();
    }
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (Busy) return;
        Busy = true; var revision = _revision;
        try
        {
            var review = RefreshView is { } refresh ? await refresh() : null;
            if (review is null || revision != _revision && !ReferenceEquals(review, _displayed)) throw new HoldingEventReviewChangedException("Current holding detail was not published.");
            PublishDisplayed(review); Feedback(null); _refreshOutcome = 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); Feedback(_refreshOutcome == 2 ? "Holding_HistoryRestoredRefresh" : _refreshOutcome == 1 ? "Holding_HistoryDeletedRefresh" : "Holding_HistorySourceChanged"); }
        finally { Busy = false; Publish(); }
    }
}
