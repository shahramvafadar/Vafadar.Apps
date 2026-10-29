using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Actual original forecast draft, covered computation and accepted Save lifetime; financial algorithms remain in Core.</summary>
public sealed partial class ForecastWorkspace(ForecastWorkStore reader, ZananceStore writer, Translator translator) : ObservableObject
{
    private bool _accepted, _acceptedFollowUpFailed, _needsRefresh, _active = true;
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private Func<bool>? _owner;
    private string? _originalFile;
    /// <summary>Gets accepted identities independently of a later read or newer draft.</summary>
    public IReadOnlyList<Guid> AcceptedSnapshotIds { get; private set; } = [];
    /// <summary>Gets covered read/retry publication state, with no work started at construction.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets the original process-local scenario, never persisted as a plan or ledger edit.</summary>
    public ForecastScenario Scenario { get; } = new();
    /// <summary>Gets the complete original reviewed source/access packet.</summary>
    public ForecastReadSnapshot? Review { get; private set; }
    /// <summary>Gets original computed currency paths, empty when computation is refused.</summary>
    public IReadOnlyList<CurrencyForecast> Forecasts { get; private set; } = [];
    /// <summary>Gets the retained original selected horizon.</summary>
    public int HorizonIndex { get; private set; }
    /// <summary>Gets the actual base date used by the original computation.</summary>
    public DateOnly BaseDate { get; private set; }
    /// <summary>Gets its original financial calendar independently of UI language.</summary>
    public PeriodCalendar Calendar { get; private set; }
    /// <summary>Gets or sets the exact unsaved name, without normalizing later raw input.</summary>
    [ObservableProperty] public partial string SnapshotName { get; set; } = string.Empty;
    /// <summary>Gets translated operation/retry feedback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets known accepted Save feedback independently of notification/history refresh.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }
    /// <summary>Gets whether an actual original Save or its follow-through is pending.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets whether this unaccepted original computation may save one explicit batch.</summary>
    public bool CanSave => OwnsFrame && Loading.IsReady && !IsBusy && !_accepted && !_needsRefresh && Forecasts.Count > 0
        && Review?.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;
    /// <summary>Gets whether current original input may use scenario tools.</summary>
    public bool CanAnalyze => OwnsFrame && Loading.IsReady && !IsBusy && !_needsRefresh
        && Review?.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;
    private bool OwnsFrame => _active && (_owner?.Invoke() ?? true);
    /// <summary>Binds native page/root authority without retaining the native page itself.</summary>
    public void BindOwner(Func<bool> owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); NotifyState(); }
    /// <summary>Retires pending source, scenario prompts and Save authority while retaining drafts and accepted receipts.</summary>
    public void SetActive(bool active) { if (_active == active) return; _active = active; RetireRead(); }
    private void RetireRead()
    {
        ++_revision; var old = _lifetime; _lifetime = new(); Loading = new(); Review = null;
        old.Cancel(); old.Dispose(); NotifyState();
    }
    /// <summary>Loads the chosen original draft from its first file and publishes only a complete current result.</summary>
    public async Task LoadAsync(int horizon, PeriodCalendar calendar, DateOnly day)
    {
        if (!OwnsFrame) return;
        RetireRead(); var revision = _revision; var token = _lifetime.Token; var owner = Loading;
        bool Current() => OwnsFrame && revision == _revision && ReferenceEquals(owner, Loading) && !token.IsCancellationRequested;
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("Original forecast computation retired.", token); }
        ForecastReadSnapshot? next = null; IReadOnlyList<CurrencyForecast> computed = []; string? error = null;
        try
        {
            await owner.RunAsync(async () =>
            {
                next = await reader.ReadAsync(token, path => { RequireCurrent(); _originalFile ??= path; return _originalFile == path; }); RequireCurrent();
                try { computed = await Task.Run(() => next.Compute(horizon, Scenario, calendar, day), token); }
                catch (CommercialWriteRejectedException rejected) { error = CommercialFailureMessage.Describe(rejected, translator) + "\n" + translator["Forecast_DraftRetained"]; }
                await reader.ValidateAsync(next, token); RequireCurrent();
            }, () =>
            {
                RequireCurrent(); next!.EnsurePublicationAccess(); Review = next; Forecasts = computed;
                if (_accepted)
                {
                    Result = translator["Forecast_Saved"];
                    if (_acceptedFollowUpFailed) Error = translator["Forecast_SavedFollowUpFailed"];
                }
                else Error = error;
                _needsRefresh = false;
                HorizonIndex = horizon; Calendar = calendar; BaseDate = day; NotifyState(); RequireCurrent();
            });
        }
        catch (OperationCanceledException) when (!Current()) { }
        finally { NotifyState(); }
    }
    /// <summary>Revalidates original source/access after a native prompt before changing the process-local draft.</summary>
    public async Task ValidateAnalysisAsync(ForecastReadSnapshot reviewed)
    { await ValidatePublicationAsync(reviewed); reviewed.Demand(CommercialFeature.AdvancedForecast); }
    /// <summary>Validates the exact current original source without granting analytical access.</summary>
    public async Task ValidatePublicationAsync(ForecastReadSnapshot reviewed)
    {
        if (!OwnsFrame || !ReferenceEquals(Review, reviewed)) throw new InvalidOperationException("Original forecast publication retired.");
        await reader.ValidateAsync(reviewed, _lifetime.Token);
        if (!OwnsFrame || !ReferenceEquals(Review, reviewed)) throw new InvalidOperationException("Original forecast publication retired.");
    }
    /// <summary>Changes only a current explicitly permitted process-local scenario.</summary>
    public void EditScenario(Action<ForecastScenario> edit)
    {
        ArgumentNullException.ThrowIfNull(edit); if (!CanAnalyze || Review is null) return;
        Review.Demand(CommercialFeature.AdvancedForecast); edit(Scenario); BeginNewDraft();
    }
    /// <summary>Explicit reset remains available for a current refused scenario to return to the Free basic forecast.</summary>
    public void ResetScenario() { if (OwnsFrame && !IsBusy && Loading.IsReady) { Scenario.Clear(); BeginNewDraft(); } }
    /// <summary>Begins a deliberate new draft without erasing historical accepted identities.</summary>
    public void BeginNewDraft() { if (OwnsFrame && !IsBusy) { _accepted = false; _acceptedFollowUpFailed = false; Result = null; Error = null; NotifyState(); } }
    /// <summary>Reports native preparation failure beside the retained original draft, without exception contents.</summary>
    public void ReportAnalysisFailure(Exception exception) => Error = CommercialFailureMessage.OrFallback(exception, translator, "Forecast_LoadFailed");
    /// <summary>Saves only the captured original computation, retaining its receipt before any Changed notification.</summary>
    public async Task SaveAsync(string name, string? assumptions, string version, Func<bool>? nativeOwner = null)
    {
        if (!CanSave || Review is not { } reviewed || string.IsNullOrWhiteSpace(name)) return;
        var revision = _revision; var token = _lifetime.Token; var forecasts = Forecasts; var day = BaseDate;
        bool Current() => OwnsFrame && revision == _revision && ReferenceEquals(Review, reviewed) && !token.IsCancellationRequested && (nativeOwner?.Invoke() ?? true);
        SnapshotName = name; IsBusy = true; Error = null; NotifyState();
        try
        {
            await ValidateAnalysisAsync(reviewed); if (!Current()) return;
            var batch = forecasts.Select(item => ForecastSnapshot.From(forecasts.Count > 1 ? $"{name.Trim()} ({item.CurrencyCode})" : name.Trim(), item,
                reviewed.Accounts.Where(account => account.IncludeInTotals && !account.IsArchived && account.UsableForPayments
                    && string.Equals(account.CurrencyCode, item.CurrencyCode, StringComparison.OrdinalIgnoreCase)).Select(account => account.Id), day, assumptions, version)).ToArray();
            try { await writer.SaveForecastSnapshotsAsync(batch, reviewed, token, Current, Accept); }
            catch (CommittedWriteNotificationException<IReadOnlyList<Guid>> accepted) { Accept(accepted.Result); _acceptedFollowUpFailed = true; Error = translator["Forecast_SavedFollowUpFailed"]; }
            if (Current())
            {
                var next = await reader.ReadAsync(token, path => _originalFile == path); next.EnsureCurrent();
                if (Current()) Review = next;
            }
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _needsRefresh = !_accepted; if (_accepted) _acceptedFollowUpFailed = true; if (Current() || _accepted) Error = _accepted ? translator["Forecast_SavedFollowUpFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Forecast_SaveFailed", draftKept: true); }
        finally { IsBusy = false; NotifyState(); }
        void Accept(IReadOnlyList<Guid> ids) { AcceptedSnapshotIds = ids.ToArray(); _accepted = true; Result = translator["Forecast_Saved"]; NotifyState(); }
    }
    private void NotifyState()
    { OnPropertyChanged(nameof(Review)); OnPropertyChanged(nameof(Forecasts)); OnPropertyChanged(nameof(AcceptedSnapshotIds)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CanAnalyze)); }
}
