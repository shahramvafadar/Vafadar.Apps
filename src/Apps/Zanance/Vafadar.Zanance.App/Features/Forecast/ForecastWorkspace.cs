using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Actual non-UI forecast draft, original computation and accepted Save lifetime; no monetary algorithms are copied.</summary>
public sealed partial class ForecastWorkspace(ForecastWorkStore reader, ZananceStore writer, Translator translator) : ObservableObject
{
    private bool _accepted;
    private bool _needsRefresh;
    /// <summary>Gets covered read/retry publication state, with no work started at construction.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets the original process-local scenario, never persisted as a plan or ledger edit.</summary>
    public ForecastScenario Scenario { get; } = new();
    /// <summary>Gets the actual original reviewed input/access packet.</summary>
    public ForecastReadSnapshot? Review { get; private set; }
    /// <summary>Gets original computed currency paths, empty when the requested computation is refused.</summary>
    public IReadOnlyList<CurrencyForecast> Forecasts { get; private set; } = [];
    /// <summary>Gets the retained selected horizon; refusal never silently chooses a different one.</summary>
    public int HorizonIndex { get; private set; }
    /// <summary>Gets the exact date used by the original calculation and any accepted saved path.</summary>
    public DateOnly BaseDate { get; private set; }
    /// <summary>Gets the calendar used by the last explicit read, independent of app language.</summary>
    public PeriodCalendar Calendar { get; private set; }
    /// <summary>Gets or sets the unsaved original name, retained across refused writes and native prompt cancellation.</summary>
    [ObservableProperty] public partial string SnapshotName { get; set; } = string.Empty;
    /// <summary>Gets full translated operation/retry feedback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets an accepted saved result, distinct from notification/history refresh success.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }
    /// <summary>Gets whether one Save or original follow-through is pending.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets whether an explicit new snapshot can save this unaccepted computation.</summary>
    public bool CanSave => Loading.IsReady && !IsBusy && !_accepted && !_needsRefresh && Forecasts.Count > 0
        && Review?.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;
    /// <summary>Gets whether new scenario tools may use the current original access.</summary>
    public bool CanAnalyze => Loading.IsReady && !IsBusy && !_needsRefresh
        && Review?.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;

    /// <summary>Loads the chosen draft without saving settings or replacing blocked horizon/scenario inputs.</summary>
    public async Task LoadAsync(int horizon, PeriodCalendar calendar, DateOnly day)
    {
        ForecastReadSnapshot? next = null; IReadOnlyList<CurrencyForecast> computed = []; string? error = null;
        try
        {
            await Loading.RunAsync(async () =>
            {
                next = await reader.ReadAsync();
                try { computed = next.Compute(horizon, Scenario, calendar, day); }
                catch (CommercialWriteRejectedException rejected)
                { error = CommercialFailureMessage.Describe(rejected, translator) + "\n" + translator["Forecast_DraftRetained"]; }
            }, () =>
            {
                next!.EnsureCurrent(); Review = next; Forecasts = computed; Error = error; _needsRefresh = false;
                HorizonIndex = horizon; Calendar = calendar; BaseDate = day; NotifyState();
            });
        }
        finally { NotifyState(); }
    }
    /// <summary>Revalidates original current file/access after a native prompt before touching the process-local draft.</summary>
    public async Task ValidateAnalysisAsync(ForecastReadSnapshot reviewed)
    { await reader.ValidateAsync(reviewed); reviewed.Demand(CommercialFeature.AdvancedForecast); }
    /// <summary>Applies only an explicitly permitted original scenario change; the caller then loads its new projection.</summary>
    public void EditScenario(Action<ForecastScenario> edit)
    {
        ArgumentNullException.ThrowIfNull(edit); if (IsBusy || !Loading.IsReady || Review is null) return;
        if (_needsRefresh) return;
        Review.Demand(CommercialFeature.AdvancedForecast); edit(Scenario); BeginNewDraft();
    }
    /// <summary>Explicit reset always remains available so a refused scenario can return to the Free basic forecast.</summary>
    public void ResetScenario() { if (!IsBusy && Loading.IsReady) { Scenario.Clear(); BeginNewDraft(); } }
    /// <summary>Retires an accepted Save only after an explicit new draft/refresh, never on an automatic follow-through.</summary>
    public void BeginNewDraft() { if (!IsBusy) { _accepted = false; Result = null; Error = null; NotifyState(); } }
    /// <summary>Reports native preparation failure beside the retained original draft, without exposing exception contents.</summary>
    public void ReportAnalysisFailure(Exception exception) =>
        Error = CommercialFailureMessage.OrFallback(exception, translator, "Forecast_LoadFailed");
    /// <summary>Saves all original currency paths in one writer, retaining the name and accepted outcome across failure.</summary>
    public async Task SaveAsync(string name, string? assumptions, string version)
    {
        if (!CanSave || Review is not { } reviewed || string.IsNullOrWhiteSpace(name)) return;
        SnapshotName = name; IsBusy = true; Error = null; NotifyState();
        try
        {
            await ValidateAnalysisAsync(reviewed);
            var batch = Forecasts.Select(item => ForecastSnapshot.From(Forecasts.Count > 1 ? $"{name.Trim()} ({item.CurrencyCode})" : name.Trim(),
                item, reviewed.Accounts.Where(account => account.IncludeInTotals && !account.IsArchived && account.UsableForPayments
                    && string.Equals(account.CurrencyCode, item.CurrencyCode, StringComparison.OrdinalIgnoreCase)).Select(account => account.Id),
                BaseDate, assumptions, version)).ToArray();
            try { await writer.SaveForecastSnapshotsAsync(batch, reviewed); }
            catch (CommittedWriteNotificationException<IReadOnlyList<Guid>>) { Accept(); Error = translator["Forecast_SavedFollowUpFailed"]; }
            if (!_accepted) Accept();
            // Reload only retained history. A completed write never makes its original Save payload available again.
            var next = await reader.ReadAsync(); next.EnsureCurrent(); Review = next;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _needsRefresh = !_accepted; Error = _accepted ? translator["Forecast_SavedFollowUpFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Forecast_SaveFailed", draftKept: true); }
        finally { IsBusy = false; NotifyState(); }
        void Accept() { _accepted = true; Result = translator["Forecast_Saved"]; }
    }
    /// <summary>Publishes only changing affordances; original raw inputs are never reformatted here.</summary>
    private void NotifyState()
    { OnPropertyChanged(nameof(Review)); OnPropertyChanged(nameof(Forecasts)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CanAnalyze)); }
}
