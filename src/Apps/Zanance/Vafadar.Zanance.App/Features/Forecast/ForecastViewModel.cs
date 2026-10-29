using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>One point of the path chart (major units).</summary>
public sealed record PathPoint(DateTime Date, double Balance);

/// <summary>A saved forecast in the list (ZEX-S0803).</summary>
public sealed record SnapshotRow(Guid Id, string Name, string Detail);

/// <summary>An item of the forecast list.</summary>
public sealed record ForecastRow(
    string DateText,
    string Name,
    string AmountText,
    Color AmountColor,
    string? Badge,
    Guid? ScheduleId,
    DateOnly? OriginalDate,
    DateOnly Date,
    bool IsExcluded,
    bool IsMoved,
    bool IsAmountAssumed = false,
    string? PlanCurrency = null,
    long? PlanAmount = null)
{
    /// <summary>Gets the opacity: items left out by the what-if stay visible but dimmed.</summary>
    public double Opacity => IsExcluded ? 0.45 : 1;
}

/// <summary>The forecast of one currency, ready for display.</summary>
public sealed record ForecastCard(
    string CurrencyCode,
    string EndText,
    string MinimumText,
    Color MinimumColor,
    string? Warning,
    string? IncompleteText,
    IReadOnlyList<PathPoint> Path,
    IReadOnlyList<ForecastRow> Rows)
{
    /// <summary>Gets a value indicating whether planned items change the balance; without any the path is a flat line.</summary>
    public bool HasItems => Rows.Count > 0;
}

/// <summary>
/// "Estimated balance after recorded plans" (FOR-01..09): end balance, lowest balance with its date and the items
/// behind it. Never called "safe to spend" (FOR-09); unplanned spending is not guessed (FOR-06).
/// </summary>
public sealed partial class ForecastViewModel : ViewModelBase, Presentation.IThemeAware
{
    private readonly Translator _translator;
    private readonly IForecastInteraction _interaction;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private ForecastScenario _scenario => Workspace.Scenario;
    private Dictionary<Guid, string> _planCurrencies = [];
    private bool _active = true;
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private Func<bool>? _rootOwner;
    /// <summary>Gets whether a tab-root rebuild would discard native prompt ownership, a raw draft or known accepted work.</summary>
    public bool HasRetainedWork => IsBusy || Workspace.IsBusy || !Workspace.Scenario.IsEmpty
        || Workspace.SnapshotName.Length != 0 || Workspace.AcceptedSnapshotIds.Count != 0;
    private bool OwnsFrame => _active && (_rootOwner?.Invoke() ?? true);
    /// <summary>Binds the original root without retaining its native page.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); PublishAvailability(); }
    /// <summary>Retires reads, native prompts and row commands while preserving original drafts and accepted results.</summary>
    public void SetActive(bool active)
    {
        if (_active == active) return; _active = active;
        if (active) _localization.Changed += OnLanguageChanged; else _localization.Changed -= OnLanguageChanged;
        RetirePublication(); Workspace.SetActive(active);
    }
    private void OnLanguageChanged(object? sender, EventArgs args) { RetirePublication(); if (OwnsFrame) _ = Presentation.Failures.GuardAsync(LoadAsync); }
    private void RetirePublication()
    {
        ++_revision; var old = _lifetime; _lifetime = new(); Loading = new(); old.Cancel(); old.Dispose(); PublishAvailability();
    }
    private void PublishAvailability()
    {
        OnPropertyChanged(nameof(HasRetainedWork)); OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(CanSave));
        SaveSnapshotCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Composes the original native projection with guarded computation, draft and explicit native effects.</summary>
    public ForecastViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, ForecastWorkStore work, IForecastInteraction? interaction = null)
    {
        _translator = translator; _interaction = interaction ?? new MauiForecastInteraction();
        _dates = dates;
        _localization = localization;
        _time = time;
        HorizonNames = [translator["Forecast_EndOfMonth"], translator["Forecast_30Days"], translator["Forecast_90Days"]];
        BasisText = string.Empty;
        Workspace = new ForecastWorkspace(work, store, translator); Workspace.BindOwner(() => OwnsFrame);
        _localization.Changed += OnLanguageChanged;
        Workspace.PropertyChanged += (_, _) => PublishAvailability();
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsBusy)) PublishAvailability(); };
        Loading.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanInteract));
    }

    /// <summary>Gets the actual process-local forecast draft and original guarded writer.</summary>
    public ForecastWorkspace Workspace { get; }
    /// <summary>Gets coverage through original read and complete native projection publication.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets native input availability after complete covered publication.</summary>
    public bool CanInteract => OwnsFrame && Loading.IsReady && !Workspace.IsBusy && !IsBusy;
    /// <summary>Gets whether the reviewed original computation may save one explicit new batch.</summary>
    public bool CanSave => OwnsFrame && Loading.IsReady && Workspace.CanSave && !IsBusy;
    /// <summary>Gets current explicit Retry availability, including a covered failure.</summary>
    public bool CanReload => OwnsFrame && !Loading.IsLoading && !Workspace.IsBusy && !IsBusy;
    /// <summary>Gets the original translated month-end, thirty-day and ninety-day choices.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> HorizonNames { get; private set; } = [];

    public SnapshotCollection<ForecastCard> Cards { get; } = [];

    /// <summary>Gets the saved forecasts, newest first (ZEX-S0803); listed in both modes once they exist.</summary>
    public SnapshotCollection<SnapshotRow> Snapshots { get; } = [];

    [ObservableProperty]
    public partial bool HasSnapshots { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial int HorizonIndex { get; set; }

    [ObservableProperty]
    public partial string BasisText { get; set; }

    [ObservableProperty]
    public partial string? UnreviewedText { get; set; }

    [ObservableProperty]
    public partial bool HasAccounts { get; set; }

    [ObservableProperty]
    public partial bool HasScenario { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private bool _restoringHorizon;
    partial void OnHorizonIndexChanged(int oldValue, int newValue)
    {
        if (_restoringHorizon) return;
        if (!CanInteract || newValue is < 0 or > 2)
        { _restoringHorizon = true; try { HorizonIndex = oldValue; } finally { _restoringHorizon = false; } return; }
        Workspace.BeginNewDraft(); _ = Presentation.Failures.GuardAsync(LoadAsync);
    }
    [RelayCommand]
    private Task RefreshAccessAsync() { if (!CanReload) return Task.CompletedTask; Workspace.BeginNewDraft(); return LoadAsync(); }

    /// <summary>Computes the forecast of the chosen horizon.</summary>
    /// <remarks>Appearing, the horizon and the scenario can ask at once; loads run one after the other (as on Home, CR07-05).</remarks>
    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        if (!OwnsFrame) return; RetirePublication(); var revision = _revision; var token = _lifetime.Token; var owner = Loading;
        bool Current() => OwnsFrame && revision == _revision && ReferenceEquals(owner, Loading) && !token.IsCancellationRequested;
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("Original native forecast retired.", token); }
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args) => PublishAvailability();
        owner.PropertyChanged += Changed;
        try { await owner.RunAsync(() => LoadCoreAsync(RequireCurrent), () => RequireCurrent()); }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (Current()) Workspace.ReportAnalysisFailure(exception); }
        finally { owner.PropertyChanged -= Changed; PublishAvailability(); }
    }

    private async Task LoadCoreAsync(Action RequireCurrent)
    {
        var today = Today;
        var calendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
        await Workspace.LoadAsync(HorizonIndex, calendar, today); RequireCurrent();
        if (Workspace.Review is not { } review) return;
        review.EnsureCurrent();
        var isAdvanced = review.Settings.Shows(Feature.ForecastDetails);
        var horizon = review.Horizon(HorizonIndex, calendar, today);
        var accounts = review.Accounts; var entries = review.Entries; var schedules = review.Schedules;
        var hasAccounts = accounts.Count > 0;
        var accountCurrencies = accounts.ToDictionary(a => a.Id, a => a.CurrencyCode);
        var planCurrencies = schedules.Where(p => accountCurrencies.ContainsKey(p.AccountId)).ToDictionary(p => p.Id, p => accountCurrencies[p.AccountId]);
        var forecasts = Workspace.Forecasts;
        var snapshots = PrepareSnapshots(review); var hasScenario = !_scenario.IsEmpty;
        var culture = _localization.CurrentCulture;

        // The basis is always visible: accounts, base date, horizon, and whether unreviewed entries are included (FOR-09).
        IReadOnlyList<string> horizonNames = [_translator["Forecast_EndOfMonth"], _translator["Forecast_30Days"], _translator["Forecast_90Days"]];
        var basisText = _translator.Format("Forecast_Basis", _dates.Format(today, DateFormatStyle.Long), _dates.Format(horizon, DateFormatStyle.Long));
        var unreviewed = entries.Count(e => e.Review == ReviewState.Unreviewed);
        var unreviewedText = unreviewed > 0 ? _translator.Format("Forecast_Unreviewed", unreviewed) : null;

        var cards = new List<ForecastCard>();
        foreach (var forecast in forecasts)
        {
            var currency = Currencies.TryGet(forecast.CurrencyCode, out var known) ? known : Currencies.Euro;
            var rows = forecast.Items.Select(item => new ForecastRow(
                _dates.Format(item.Date, DateFormatStyle.DayMonth),
                string.IsNullOrEmpty(item.Name) ? _translator["Forecast_RecordedEntry"] : item.Name,
                item.Effect is { } effect ? MoneyText.Format(effect, forecast.CurrencyCode, culture, showPlus: true, approximate: item.IsEstimate) : _translator["Plan_AmountUnknown"],
                item.Effect is null ? Palette.WarningText : item.Effect < 0 ? EntryPresenter.ExpenseColor : Palette.IncomeText,
                item.IsExcluded ? _translator["Forecast_LeftOut"]
                    : item.IsMoved && item.IsAmountAssumed ? _translator["Forecast_DateAndAmountAssumed"]
                    : item.IsMoved ? _translator["Forecast_DateAssumed"]
                    : item.IsAmountAssumed ? _translator["Forecast_AmountAssumed"]
                    : item.Source switch
                    {
                        ForecastSource.OverduePlan => _translator["Forecast_OverdueAssumed"],
                        ForecastSource.FutureEntry => _translator["Forecast_Recorded"],
                        _ => null,
                    },
                item.ScheduleId,
                item.OriginalDate,
                item.Date,
                item.IsExcluded,
                item.IsMoved,
                item.IsAmountAssumed,
                item.ScheduleId is { } id ? planCurrencies.GetValueOrDefault(id) : null,
                item.Effect is { } value && item.ScheduleId is { } plan && string.Equals(planCurrencies.GetValueOrDefault(plan), forecast.CurrencyCode, StringComparison.OrdinalIgnoreCase) ? Math.Abs(value) : null)).ToList();

            var warning = forecast.GoesNegative
                ? _translator.Format("Forecast_Shortfall", _dates.Format(forecast.MinimumDate, DateFormatStyle.Long))
                : null;
            cards.Add(new ForecastCard(
                forecast.CurrencyCode,
                MoneyText.Format(forecast.EndBalance, forecast.CurrencyCode, culture),
                _translator.Format("Forecast_Lowest", MoneyText.Format(forecast.Minimum, forecast.CurrencyCode, culture), _dates.Format(forecast.MinimumDate, DateFormatStyle.DayMonth)),
                forecast.GoesNegative ? Palette.ExpenseText : Palette.SecondaryText,
                warning,
                forecast.IsIncomplete ? _translator.Format("Forecast_Incomplete", forecast.UnknownCount) : null,
                [.. forecast.Path.Select(p => new PathPoint(p.Date.ToDateTime(TimeOnly.MinValue), (double)MoneyText.ToDecimal(p.Balance, currency)))],
                rows));
        }
        await Workspace.ValidatePublicationAsync(review); RequireCurrent();
        void Publish(Action action) { RequireCurrent(); review.EnsurePublicationAccess(); action(); RequireCurrent(); }
        Publish(() => HorizonNames = horizonNames); Publish(() => IsAdvanced = isAdvanced); Publish(() => HasAccounts = hasAccounts);
        Publish(() => _planCurrencies = planCurrencies); Publish(() => HasScenario = hasScenario);
        Publish(() => BasisText = basisText); Publish(() => UnreviewedText = unreviewedText);
        Publish(() => Cards.ReplaceAll(cards)); Publish(() => Snapshots.ReplaceAll(snapshots)); Publish(() => HasSnapshots = snapshots.Length > 0);
    }

    private SnapshotRow[] PrepareSnapshots(ForecastReadSnapshot? review) => (review?.Snapshots ?? []).Select(snapshot =>
        new SnapshotRow(snapshot.Id, snapshot.Name, _translator.Format("Snapshot_Saved", _dates.Format(snapshot.BaseDate, DateFormatStyle.Short), snapshot.CurrencyCode,
            MoneyText.Format(snapshot.Minimum, snapshot.CurrencyCode, _localization.CurrentCulture), _dates.Format(snapshot.MinimumDate, DateFormatStyle.Short)))).ToArray();
    private void PublishSnapshots() { var rows = PrepareSnapshots(Workspace.Review); Snapshots.ReplaceAll(rows); HasSnapshots = rows.Length > 0; }

    // ZEX-S0803: the forecast as it is now – path, scope and assumptions – kept read-only for a later comparison.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveSnapshotAsync()
    {
        if (!CanSave || Workspace.Review is not { } reviewed)
        {
            return;
        }

        var revision = _revision;
        bool Current() => OwnsFrame && revision == _revision && ReferenceEquals(Workspace.Review, reviewed);
        IsBusy = true;
        try
        {
            await Workspace.ValidateAnalysisAsync(reviewed); if (!Current()) return;
            var name = await _interaction.PromptAsync(_translator["Snapshot_SaveTitle"], _translator["Snapshot_SaveMessage"], _translator["Common_Save"], _translator["Common_Cancel"],
                initialValue: string.IsNullOrEmpty(Workspace.SnapshotName) ? _dates.Format(Workspace.BaseDate, DateFormatStyle.Short) : Workspace.SnapshotName, maxLength: 80);
            if (!Current() || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var assumptions = string.Join("; ", Cards.SelectMany(c => c.Rows).Where(r => r.Badge is not null && (r.IsExcluded || r.IsMoved || r.IsAmountAssumed)).Select(r => $"{r.Name}: {r.Badge}"));
            await Workspace.SaveAsync(name, assumptions, AppInfo.Current.VersionString, Current);
            if (OwnsFrame && revision == _revision) PublishSnapshots();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (OwnsFrame && revision == _revision) await CoverSourceFailureAsync(exception); }
        finally { IsBusy = false; }

    }

    [RelayCommand]
    private async Task OpenSnapshotAsync(SnapshotRow row)
    {
        if (!CanInteract || !Snapshots.Any(item => ReferenceEquals(item, row)) || Workspace.Review is not { } review) return;
        var revision = _revision;
        try
        {
        await Workspace.ValidatePublicationAsync(review);
        if (OwnsFrame && revision == _revision && Snapshots.Any(item => ReferenceEquals(item, row)))
            await _interaction.NavigateAsync(AppShell.SnapshotRoute, new Dictionary<string, object> { ["id"] = row.Id });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsFrame && revision == _revision) await CoverSourceFailureAsync(exception); }
    }

    [RelayCommand]
    private Task AddPlanAsync() => CanInteract ? _interaction.NavigateAsync(AppShell.PlanEditorRoute) : Task.CompletedTask;

    // FOR-04, FOR-10: a plan item can be left out, or assumed on another date or with another amount, for this view only;
    // nothing is saved.
    [RelayCommand]
    private async Task OpenItemAsync(ForecastRow row)
    {
        if (row is not { ScheduleId: { } plan, OriginalDate: { } original })
        {
            return;
        }

        if (!CanInteract || !Cards.SelectMany(card => card.Rows).Any(item => ReferenceEquals(item, row)) || Workspace.Review is not { } reviewed) return;
        var revision = _revision;
        bool Current() => OwnsFrame && revision == _revision && ReferenceEquals(Workspace.Review, reviewed)
            && Cards.SelectMany(card => card.Rows).Any(item => ReferenceEquals(item, row));
        IsBusy = true;
        try
        {
            var open = _translator["Forecast_OpenOccurrence"];
            var toggle = row.IsExcluded ? _translator["Forecast_IncludeAgain"] : _translator["Forecast_LeaveOut"];
            var move = _translator["Forecast_AssumeDate"];
            var restore = _translator["Forecast_RestoreDate"];
            var amount = row.IsAmountAssumed ? _translator["Forecast_RestoreAmount"] : _translator["Forecast_AssumeAmount"];
            string[] actions = row.IsExcluded ? [open, toggle] : [open, toggle, row.IsMoved ? restore : move, amount];
            var choice = await _interaction.ChooseAsync(row.Name, _translator["Common_Cancel"], actions);
            if (!Current()) return;
            if (choice == open)
            {
                await Workspace.ValidatePublicationAsync(reviewed); if (!Current()) return;
                await _interaction.NavigateAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = plan, ["date"] = original });
                return;
            }

            if (choice is null || choice == _translator["Common_Cancel"]) return;
            await Workspace.ValidateAnalysisAsync(reviewed); if (!Current()) return;
            if (choice == restore)
            {
                Workspace.EditScenario(s => s.SetDate(plan, original, null));
            }
            else if (choice == toggle)
            {
                Workspace.EditScenario(s => s.SetExcluded(plan, original, !row.IsExcluded));
            }
            else if (choice == amount && row.IsAmountAssumed)
            {
                Workspace.EditScenario(s => s.SetAmount(plan, original, null));
            }
            else if (choice == amount)
            {
                if (row.PlanCurrency is not { } currency)
                {
                    return;
                }

                var culture = _localization.CurrentCulture;
                var proposal = await ForecastAmountPrompt.ReadAsync(currency, culture, row.PlanAmount, (unit, initial) => _interaction.PromptAsync(
                    _translator["Forecast_AssumeAmount"], _translator.Format("Forecast_AssumeAmountMessage", unit), _translator["Common_Ok"], _translator["Common_Cancel"],
                    initialValue: initial, maxLength: 20, numeric: true));
                if (proposal is not { } assumed)
                {
                    return;
                }

                await Workspace.ValidateAnalysisAsync(reviewed); if (!Current()) return;
                Workspace.EditScenario(s => s.SetAmount(plan, original, assumed));
            }
            else if (choice == move)
            {
                var text = await _interaction.PromptAsync(
                    _translator["Forecast_AssumeDate"], _translator["Forecast_AssumeDateMessage"], _translator["Common_Ok"], _translator["Common_Cancel"],
                    initialValue: "7", maxLength: 4, numeric: true);
                if (!int.TryParse(Vafadar.Core.Text.Digits.ToAscii(text ?? string.Empty).Replace('−', '-'), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var days) || days == 0)
                {
                    return;
                }

                await Workspace.ValidateAnalysisAsync(reviewed); if (!Current()) return;
                Workspace.EditScenario(s => s.SetDate(plan, original, row.Date.AddDays(days)));
            }
            else
            {
                return;
            }

            await LoadAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { if (OwnsFrame && revision == _revision) await CoverSourceFailureAsync(exception); }
        finally { IsBusy = false; }

    }

    /// <summary>Assumes another amount for a plan item of this view (FOR-10); nothing is saved.</summary>
    internal async Task AssumeAmountAsync(ForecastRow row, long amount)
    {
        if (CanInteract && Cards.SelectMany(card => card.Rows).Any(item => ReferenceEquals(item, row)) && row is { ScheduleId: { } plan, OriginalDate: { } original })
        {
            Workspace.EditScenario(s => s.SetAmount(plan, original, amount));
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task ResetScenarioAsync()
    {
        if (!CanInteract) return;
        Workspace.ResetScenario();
        await LoadAsync();
    }
    /// <summary>Covers stale source authority while preserving raw scenario/name and known accepted identities.</summary>
    private async Task CoverSourceFailureAsync(Exception exception)
    {
        RetirePublication(); Workspace.SetActive(false); Workspace.SetActive(true); Workspace.ReportAnalysisFailure(exception);
        var failed = Loading;
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { }
        PublishAvailability();
    }

}
