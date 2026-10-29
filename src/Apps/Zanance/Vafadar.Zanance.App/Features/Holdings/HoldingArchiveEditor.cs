using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Original holding archive confirmation with a terminal known outcome before fallible navigation.</summary>
public sealed partial class HoldingArchiveEditor(HoldingStore store, Translator translator, IAppInteraction interaction) : ObservableObject
{
    private HoldingPriceReview? _displayed;
    private long _revision;
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
    private bool _presentationOnly;
    private string? _captionKey;
    /// <summary>Shows only an original completed caption with retired authority, never a new archive command.</summary>
    internal void ShowRetainedOutcome(string key, bool committed)
    { _presentationOnly = true; _captionKey = key; Committed = committed; Retire(); Error = translator[key]; }
#endif
    /// <summary>Reads current original displayed facts only through explicit owner refresh.</summary>
    public Func<Task<HoldingPriceReview?>>? RefreshView { get; set; }
    /// <summary>Lets the actual owning detail review its separate inline draft before a terminal departure.</summary>
    internal Func<Task<bool>>? ConfirmDraftDeparture { get; set; }
    /// <summary>Binds the original inline draft to both native archive consents.</summary>
    internal Func<string>? DepartureStamp { get; set; }
    /// <summary>Gets whether the original archive is known to have committed; Refresh never rearms it.</summary>
    public bool Committed { get; private set; }
    /// <summary>Gets whether confirmation or persistence currently owns the original operation.</summary>
    [ObservableProperty] public partial bool Busy { get; set; }
    /// <summary>Gets refusal or accepted-result feedback without resubmission.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets whether an explicit current-data read is required.</summary>
    [ObservableProperty] public partial bool NeedsRefresh { get; set; }
    /// <summary>Gets whether a fresh original unarchived row can request confirmation.</summary>
    public bool CanArchive => (ViewReady?.Invoke() ?? true) && !Busy && !Committed && !NeedsRefresh && _displayed is { Inputs.Type.IsArchived: false };
    /// <summary>Requires complete original page publication before the native archive action.</summary>
    internal Func<bool>? ViewReady { get; set; }
    /// <summary>Refreshes native archive enablement after the owning page cover changes.</summary>
    internal void NotifyViewReady() => Publish();
    /// <summary>Gets whether read-only refresh can begin.</summary>
    public bool CanRefresh => !Busy;
    partial void OnBusyChanged(bool value) => Publish();
    partial void OnNeedsRefreshChanged(bool value) => Publish();
    private void Publish()
    {
        OnPropertyChanged(nameof(CanArchive)); OnPropertyChanged(nameof(CanRefresh));
        ArchiveCommand.NotifyCanExecuteChanged(); RefreshCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Publishes the exact original packet used for the displayed holding.</summary>
    public void PublishDisplayed(HoldingPriceReview? review)
    {
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
        if (_presentationOnly) { if (_captionKey is { } key) Error = translator[key]; return; }
#endif
        if (!ReferenceEquals(_displayed, review)) { Interlocked.Increment(ref _revision); _displayed = review; }
        if (review is not null && !Committed) NeedsRefresh = false;
        Publish();
    }
    /// <summary>Retires pending departed confirmation without discarding a known committed outcome.</summary>
    public void Retire() { Interlocked.Increment(ref _revision); _displayed = null; NeedsRefresh = true; Publish(); }
    [RelayCommand(CanExecute = nameof(CanArchive))]
    private async Task ArchiveAsync()
    {
        if (!CanArchive || _displayed is not { } review) return;
        var revision = _revision; var draft = DepartureStamp?.Invoke(); Busy = true; Error = null;
        bool Current() => revision == _revision && ReferenceEquals(_displayed, review)
            && (ViewReady?.Invoke() ?? true) && draft == DepartureStamp?.Invoke();
        try
        {
            if (ConfirmDraftDeparture is { } confirm && (!await confirm() || !Current())) return;
            if (!await interaction.ConfirmAsync(translator["Holding_Archive"], translator["Holding_ArchiveMessage"],
                translator["Holding_Archive"], translator["Common_Cancel"])) return;
            await store.ArchiveReviewedTypeAsync(review, Current);
            Committed = true;
        }
        catch (CommittedWriteNotificationException) { Committed = true; Error = translator["Holding_ArchiveSavedRefresh"]; }
        catch (HoldingEventReviewChangedException) { Retire(); Error = translator["Holding_ArchiveSourceChanged"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); Error = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally { if (Committed) Retire(); Busy = false; }
        if (Committed && Error is null && (ViewReady?.Invoke() ?? true) && draft == DepartureStamp?.Invoke())
        {
            try { await interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { Error = translator["Holding_ArchiveSavedRefresh"]; }
        }
    }
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (Busy) return;
        Busy = true; Retire();
        try
        {
            if (RefreshView is not { } refresh) throw new InvalidOperationException("Original archive refresh owner is unavailable.");
            PublishDisplayed(await refresh() ?? throw new IOException("Original current holding read did not complete."));
            Error = Committed ? translator["Holding_ArchiveSavedRefresh"] : null;
            if (Committed) NeedsRefresh = false;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Retire(); Error = Committed ? translator["Holding_ArchiveSavedRefresh"]
            : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally { Busy = false; }
    }
}
