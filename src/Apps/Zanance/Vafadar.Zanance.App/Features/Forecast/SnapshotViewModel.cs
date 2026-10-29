using System.Globalization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>One day of the comparison; values are major units for the chart.</summary>
public sealed record ComparisonPoint(DateTime Date, double Saved, double Actual);

/// <summary>A day of the comparison table.</summary>
public sealed record ComparisonRow(string DateText, string SavedText, string ActualText, string DifferenceText, Color DifferenceColor);

/// <summary>
/// A saved forecast next to what happened (ZEX-S0804, ZEX-UI18): the saved path, the actual balance of the same accounts
/// from today's ledger, and the difference split into recorded later, net unplanned spending/income, other account
/// movements and residual changes to plans or earlier data. The parts add up; the snapshot itself never changes.
/// </summary>
public sealed partial class SnapshotViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, ForecastWorkStore work, IForecastInteraction? interaction = null)
    : ViewModelBase, Presentation.IThemeAware
{
    private readonly IForecastInteraction _interaction = interaction ?? new MauiForecastInteraction();
    private Guid _id;
    private Guid? _deletedId;
    private Guid _publishedId;
    private bool _active = true;
    private int _revision;
    private string? _originalFile;
    private Func<bool>? _rootOwner;
    private CancellationTokenSource _lifetime = new();
    /// <summary>Gets the actual most recent accepted deletion, independently of another requested identity.</summary>
    public Guid? DeletedSnapshotId => _deletedId;
    /// <summary>Gets whether this actual frame can close outside an outstanding native deletion.</summary>
    public bool CanLeave => _active && (_rootOwner?.Invoke() ?? true) && !IsBusy;
    /// <summary>Gets whether an explicit source read is available for the current original frame.</summary>
    public bool CanReload => _active && (_rootOwner?.Invoke() ?? true) && !IsBusy && !Loading.IsLoading;
    /// <summary>Binds the weak native root without retaining a completed Shell.</summary>
    internal void BindRootOwner(Func<bool> owner) => _rootOwner = owner;
    /// <summary>Retires original reads, row authority and native consent on actual page/tab/root departure.</summary>
    public void SetActive(bool active)
    {
        if (_active != active) { _active = active; RetireRead(); }
        localization.Changed -= OnDisplayChanged;
        if (active) localization.Changed += OnDisplayChanged;
        PublishAvailability();
    }
    private void OnDisplayChanged(object? sender, EventArgs args)
    { RetireRead(); if (_active) _ = LoadAsync(); }
    private void RetireRead()
    {
        ++_revision; _review = null; _publishedId = Guid.Empty;
        var old = _lifetime; _lifetime = new(); Loading = new(); old.Cancel(); old.Dispose(); PublishAvailability();
    }
    private void PublishAvailability()
    {
        OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(DeletedSnapshotId)); DeleteCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
    }
    private ForecastReadSnapshot? _review;
    /// <summary>Gets covered current-file publication state.</summary>
    [ObservableProperty] public partial Presentation.SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets translated read feedback beside the original covered Retry.</summary>
    [ObservableProperty] public partial string? LoadError { get; private set; }
    /// <summary>Gets retained saved points independently of paid new comparison.</summary>
    public Presentation.SnapshotCollection<ComparisonRow> SavedRows { get; } = [];
    /// <summary>Gets full translated access or accepted-delete follow-through feedback.</summary>
    [ObservableProperty] public partial string? AccessError { get; set; }
    /// <summary>Gets whether a new analytical comparison was permitted by the original current facts.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNotStarted))] public partial bool CanCompare { get; set; }
    /// <summary>Gets the original not-started comparison explanation only after an authorized comparison.</summary>
    public bool ShowNotStarted => CanCompare && NotStarted;
    /// <summary>Gets whether original saved data can still be explicitly deleted.</summary>
    public bool CanDelete => _active && (_rootOwner?.Invoke() ?? true) && Loading.IsReady && !IsBusy
        && _publishedId == _id && _deletedId != _id && !NotFound && _review is not null;
    [RelayCommand(CanExecute = nameof(CanReload))] private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;

    public Presentation.SnapshotCollection<ComparisonPoint> Points { get; } = [];

    public Presentation.SnapshotCollection<ComparisonRow> Rows { get; } = [];

    public Presentation.SnapshotCollection<AmountLine> Parts { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? SavedText { get; set; }

    [ObservableProperty]
    public partial string? AssumptionsText { get; set; }

    [ObservableProperty]
    public partial string? ComparedText { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNotStarted))]
    public partial bool NotStarted { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            if (_id != id) { RetireRead(); _id = id; Name = null; AccessError = null; }
            query.Clear();
        }
    }

    // Display changes retire the original frame before rebuilding detached text and semantic colors.
    Task Presentation.IThemeAware.RefreshThemeAsync() { RetireRead(); return _active ? LoadAsync() : Task.CompletedTask; }

    /// <summary>Reads, computes and validates complete original comparison input before any native publication.</summary>
    public async Task LoadAsync()
    {
        if (!_active || _rootOwner?.Invoke() == false) return;
        var owner = Loading; var revision = _revision; var id = _id; var token = _lifetime.Token;
        bool Current() => _active && (_rootOwner?.Invoke() ?? true) && revision == _revision && id == _id
            && ReferenceEquals(owner, Loading) && !token.IsCancellationRequested;
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("Original saved comparison retired.", token); }
        ForecastReadSnapshot? reviewed = null; Prepared? prepared = null;
        void Availability(object? sender, System.ComponentModel.PropertyChangedEventArgs args) => PublishAvailability();
        owner.PropertyChanged += Availability;
        try
        {
            await owner.RunAsync(async () =>
            {
                RequireCurrent(); _review = null; _publishedId = Guid.Empty; LoadError = null;
                reviewed = await work.ReadAsync(token, path =>
                {
                    RequireCurrent(); _originalFile ??= path; return _originalFile == path;
                });
                RequireCurrent(); var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                var culture = CultureInfo.ReadOnly((CultureInfo)localization.CurrentCulture.Clone());
                var secondary = Presentation.Palette.SecondaryText; var danger = Presentation.EntryPresenter.DangerColor;
                prepared = await Task.Run(() => Prepare(reviewed, id, today, culture, secondary, danger), token); RequireCurrent();
                await work.ValidateAsync(reviewed, token); RequireCurrent();
            }, () =>
            {
                var result = prepared ?? throw new InvalidOperationException("Original complete comparison missing.");
                var source = reviewed ?? throw new InvalidOperationException("Original comparison source missing.");
                void Publish(Action action) { RequireCurrent(); source.EnsurePublicationAccess(); action(); RequireCurrent(); source.EnsurePublicationAccess(); }
                Publish(() => Name = result.Name); Publish(() => NotFound = result.NotFound);
                Publish(() => SavedText = result.SavedText); Publish(() => AssumptionsText = result.AssumptionsText);
                Publish(() => ComparedText = result.ComparedText); Publish(() => CanCompare = result.CanCompare);
                Publish(() => NotStarted = result.NotStarted); Publish(() => AccessError = _deletedId == id ? translator["Forecast_DeletedFollowUpFailed"] : result.AccessError);
                Publish(() => SavedRows.ReplaceAll(result.SavedRows)); Publish(() => Points.ReplaceAll(result.Points));
                Publish(() => Rows.ReplaceAll(result.Rows)); Publish(() => Parts.ReplaceAll(result.Parts));
                _review = source; _publishedId = id;
            });
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) LoadError = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Forecast_LoadFailed"); }
        finally { owner.PropertyChanged -= Availability; PublishAvailability(); }
    }

    private Prepared Prepare(ForecastReadSnapshot reviewed, Guid id, DateOnly today, CultureInfo culture, Color secondary, Color danger)
    {
        reviewed.EnsureCurrent(); var result = new Prepared();
        var snapshot = reviewed.Snapshots.FirstOrDefault(s => s.Id == id);
        result.NotFound = snapshot is null;
        if (snapshot is null)
        {
            return result;
        }

        string Money(long minor, bool plus = false) => MoneyText.Format(minor, snapshot.CurrencyCode, culture, showPlus: plus);
        result.Name = snapshot.Name;
        result.SavedText = translator.Format("Snapshot_Detail", dates.Format(snapshot.BaseDate, DateFormatStyle.Long), dates.Format(snapshot.Horizon, DateFormatStyle.Long),
            Money(snapshot.Minimum), dates.Format(snapshot.MinimumDate, DateFormatStyle.Short))
            + (snapshot.UnknownCount > 0 ? " · " + translator.Format("Forecast_Incomplete", snapshot.UnknownCount) : string.Empty);
        result.AssumptionsText = snapshot.Assumptions is { } assumptions ? translator.Format("Snapshot_Assumptions", assumptions) : null;

        result.Points.Clear(); result.Rows.Clear(); result.Parts.Clear(); result.SavedRows.Clear(); result.ComparedText = null;
        foreach (var point in snapshot.Points())
            result.SavedRows.Add(new ComparisonRow(dates.Format(point.Date, DateFormatStyle.DayMonth), Money(point.Balance), string.Empty, string.Empty, secondary));
        result.CanCompare = reviewed.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;
        result.AccessError = result.CanCompare ? null : Presentation.CommercialFailureMessage.Describe(
            new CommercialWriteRejectedException(CommercialFeature.AdvancedForecast, reviewed.Permission(CommercialFeature.AdvancedForecast)), translator);
        if (!result.CanCompare) { result.NotStarted = true; return result; }
        var comparison = reviewed.Compare(snapshot, today);
        var currency = Currencies.TryGet(snapshot.CurrencyCode, out var known) ? known : Currencies.Euro;
        result.Points.Clear();
        result.Rows.Clear();
        foreach (var day in comparison.Days)
        {
            result.Points.Add(new ComparisonPoint(day.Date.ToDateTime(TimeOnly.MinValue), (double)MoneyText.ToDecimal(day.Saved, currency), (double)MoneyText.ToDecimal(day.Actual, currency)));
        }

        // The table shows every seventh day and the last one, so it stays readable at 90 days.
        foreach (var day in comparison.Days.Where((_, i) => i % 7 == 0 || i == comparison.Days.Count - 1))
        {
            result.Rows.Add(new ComparisonRow(dates.Format(day.Date, DateFormatStyle.DayMonth), Money(day.Saved), Money(day.Actual), Money(day.Difference, plus: true),
                day.Difference < 0 ? danger : secondary));
        }

        result.NotStarted = comparison.Days.Count <= 1;
        result.ComparedText = comparison.Days.Count == 0 ? null : translator.Format("Snapshot_Compared", dates.Format(comparison.Days[^1].Date, DateFormatStyle.Short));
        result.Parts.Clear();
        result.Parts.Add(new AmountLine(translator["Snapshot_RecordedLater"], Money(comparison.RecordedLater, true), false, comparison.RecordedLater < 0));
        result.Parts.Add(new AmountLine(translator["Snapshot_UnplannedSpending"], Money(comparison.UnplannedSpending, true), false, comparison.UnplannedSpending < 0));
        result.Parts.Add(new AmountLine(translator["Snapshot_UnplannedIncome"], Money(comparison.UnplannedIncome, true), false, comparison.UnplannedIncome < 0));
        if (comparison.OtherMovements != 0)
            result.Parts.Add(new AmountLine(translator["Snapshot_OtherMovements"], Money(comparison.OtherMovements, true), false));
        result.Parts.Add(new AmountLine(translator["Snapshot_PlansChanged"], Money(comparison.PlansChanged, true), false, comparison.PlansChanged < 0));
        result.Parts.Add(new AmountLine(translator["Snapshot_Difference"], Money(comparison.Difference, true), true, comparison.Difference < 0));
        reviewed.EnsureCurrent(); return result;
    }

    private sealed class Prepared
    {
        internal string? Name, SavedText, AssumptionsText, ComparedText, AccessError;
        internal bool NotFound, CanCompare, NotStarted;
        internal readonly List<ComparisonRow> SavedRows = [], Rows = [];
        internal readonly List<ComparisonPoint> Points = [];
        internal readonly List<AmountLine> Parts = [];
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (!CanDelete || _review is not { } reviewed) return;
        var id = _id; var revision = _revision; var token = _lifetime.Token;
        bool Current() => _active && (_rootOwner?.Invoke() ?? true) && revision == _revision && id == _id
            && _publishedId == id && ReferenceEquals(_review, reviewed) && !token.IsCancellationRequested;
        IsBusy = true; PublishAvailability();
        void Accept() { _deletedId = id; PublishAvailability(); }
        try
        {
            if (!await _interaction.ConfirmAsync(translator["Snapshot_Delete"], translator["Snapshot_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"]) || !Current()) return;
            await work.ValidateAsync(reviewed, token);
            if (!Current()) return;
            await store.DeleteReviewedForecastSnapshotAsync(id, reviewed, token, Current, Accept);
            if (Current()) await _interaction.NavigateAsync("..");
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (CommittedWriteNotificationException)
        { Accept(); if (Current()) AccessError = translator["Forecast_DeletedFollowUpFailed"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current())
            {
                if (_deletedId == id) AccessError = translator["Forecast_DeletedFollowUpFailed"];
                else await CoverSourceFailureAsync(exception);
            }
        }
        finally { IsBusy = false; PublishAvailability(); }
    }
    /// <summary>Retires untrusted displayed authority after refusal while keeping explicit current-frame Retry available.</summary>
    private async Task CoverSourceFailureAsync(Exception exception)
    {
        RetireRead(); AccessError = null; var failed = Loading;
        LoadError = Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Forecast_LoadFailed");
        try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { }
        PublishAvailability();
    }
}
