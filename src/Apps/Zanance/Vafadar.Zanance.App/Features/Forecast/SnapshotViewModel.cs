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
    : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private readonly IForecastInteraction _interaction = interaction ?? new MauiForecastInteraction();
    private Guid _id;
    private bool _deleted;
    private ForecastReadSnapshot? _review;
    /// <summary>Gets covered current-file publication state.</summary>
    public Presentation.SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets retained saved points independently of paid new comparison.</summary>
    public ObservableCollection<ComparisonRow> SavedRows { get; } = [];
    /// <summary>Gets full translated access or accepted-delete follow-through feedback.</summary>
    [ObservableProperty] public partial string? AccessError { get; set; }
    /// <summary>Gets whether a new analytical comparison was permitted by the original current facts.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNotStarted))] public partial bool CanCompare { get; set; }
    /// <summary>Gets the original not-started comparison explanation only after an authorized comparison.</summary>
    public bool ShowNotStarted => CanCompare && NotStarted;
    /// <summary>Gets whether original saved data can still be explicitly deleted.</summary>
    public bool CanDelete => Loading.IsReady && !IsBusy && !_deleted && !NotFound;
    [RelayCommand] private Task ReloadAsync() => LoadAsync();

    public ObservableCollection<ComparisonPoint> Points { get; } = [];

    public ObservableCollection<ComparisonRow> Rows { get; } = [];

    public ObservableCollection<AmountLine> Parts { get; } = [];

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
            _id = id;
        }
    }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        ForecastReadSnapshot? reviewed = null;
        await Loading.RunAsync(async () => reviewed = await work.ReadAsync(), () => Publish(reviewed!));
        DeleteCommand.NotifyCanExecuteChanged();
    }

    private void Publish(ForecastReadSnapshot reviewed)
    {
        reviewed.EnsureCurrent(); _review = reviewed;
        var snapshot = reviewed.Snapshots.FirstOrDefault(s => s.Id == _id);
        NotFound = snapshot is null;
        if (snapshot is null)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        string Money(long minor, bool plus = false) => MoneyText.Format(minor, snapshot.CurrencyCode, culture, showPlus: plus);
        Name = snapshot.Name;
        SavedText = translator.Format("Snapshot_Detail", dates.Format(snapshot.BaseDate, DateFormatStyle.Long), dates.Format(snapshot.Horizon, DateFormatStyle.Long),
            Money(snapshot.Minimum), dates.Format(snapshot.MinimumDate, DateFormatStyle.Short))
            + (snapshot.UnknownCount > 0 ? " · " + translator.Format("Forecast_Incomplete", snapshot.UnknownCount) : string.Empty);
        AssumptionsText = snapshot.Assumptions is { } assumptions ? translator.Format("Snapshot_Assumptions", assumptions) : null;

        Points.Clear(); Rows.Clear(); Parts.Clear(); SavedRows.Clear(); ComparedText = null;
        foreach (var point in snapshot.Points())
            SavedRows.Add(new ComparisonRow(dates.Format(point.Date, DateFormatStyle.DayMonth), Money(point.Balance), string.Empty, string.Empty, Presentation.Palette.SecondaryText));
        CanCompare = reviewed.Permission(CommercialFeature.AdvancedForecast) == FeaturePermission.Allowed;
        AccessError = CanCompare ? null : Presentation.CommercialFailureMessage.Describe(
            new CommercialWriteRejectedException(CommercialFeature.AdvancedForecast, reviewed.Permission(CommercialFeature.AdvancedForecast)), translator);
        if (!CanCompare) { NotStarted = true; return; }
        var comparison = reviewed.Compare(snapshot, today);
        var currency = Currencies.TryGet(snapshot.CurrencyCode, out var known) ? known : Currencies.Euro;
        Points.Clear();
        Rows.Clear();
        foreach (var day in comparison.Days)
        {
            Points.Add(new ComparisonPoint(day.Date.ToDateTime(TimeOnly.MinValue), (double)MoneyText.ToDecimal(day.Saved, currency), (double)MoneyText.ToDecimal(day.Actual, currency)));
        }

        // The table shows every seventh day and the last one, so it stays readable at 90 days.
        foreach (var day in comparison.Days.Where((_, i) => i % 7 == 0 || i == comparison.Days.Count - 1))
        {
            Rows.Add(new ComparisonRow(dates.Format(day.Date, DateFormatStyle.DayMonth), Money(day.Saved), Money(day.Actual), Money(day.Difference, plus: true),
                day.Difference < 0 ? Presentation.EntryPresenter.DangerColor : Presentation.Palette.SecondaryText));
        }

        NotStarted = comparison.Days.Count <= 1;
        ComparedText = comparison.Days.Count == 0 ? null : translator.Format("Snapshot_Compared", dates.Format(comparison.Days[^1].Date, DateFormatStyle.Short));
        Parts.Clear();
        Parts.Add(new AmountLine(translator["Snapshot_RecordedLater"], Money(comparison.RecordedLater, true), false, comparison.RecordedLater < 0));
        Parts.Add(new AmountLine(translator["Snapshot_UnplannedSpending"], Money(comparison.UnplannedSpending, true), false, comparison.UnplannedSpending < 0));
        Parts.Add(new AmountLine(translator["Snapshot_UnplannedIncome"], Money(comparison.UnplannedIncome, true), false, comparison.UnplannedIncome < 0));
        if (comparison.OtherMovements != 0)
            Parts.Add(new AmountLine(translator["Snapshot_OtherMovements"], Money(comparison.OtherMovements, true), false));
        Parts.Add(new AmountLine(translator["Snapshot_PlansChanged"], Money(comparison.PlansChanged, true), false, comparison.PlansChanged < 0));
        Parts.Add(new AmountLine(translator["Snapshot_Difference"], Money(comparison.Difference, true), true, comparison.Difference < 0));
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (!CanDelete || _review is not { } reviewed) return;
        IsBusy = true; DeleteCommand.NotifyCanExecuteChanged();
        try
        {
            if (!await _interaction.ConfirmAsync(translator["Snapshot_Delete"], translator["Snapshot_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"])) return;
            try { await work.ValidateAsync(reviewed); await store.DeleteReviewedForecastSnapshotAsync(_id, reviewed); }
            catch (CommittedWriteNotificationException) { _deleted = true; AccessError = translator["Forecast_DeletedFollowUpFailed"]; }
            _deleted = true; await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { AccessError = _deleted ? translator["Forecast_DeletedFollowUpFailed"] : Presentation.CommercialFailureMessage.OrFallback(exception, translator, "Forecast_DeleteFailed"); }
        finally { IsBusy = false; DeleteCommand.NotifyCanExecuteChanged(); }
    }
}
