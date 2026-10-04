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

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>One day of the comparison; values are major units for the chart.</summary>
public sealed record ComparisonPoint(DateTime Date, double Saved, double Actual);

/// <summary>A day of the comparison table.</summary>
public sealed record ComparisonRow(string DateText, string SavedText, string ActualText, string DifferenceText, Color DifferenceColor);

/// <summary>
/// A saved forecast next to what happened (ZEX-S0804, ZEX-UI18): the saved path, the actual balance of the same accounts
/// from today's ledger, and the difference split into recorded later, unplanned spending and income, and plans that
/// changed – the parts always add up. The snapshot itself never changes.
/// </summary>
public sealed partial class SnapshotViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    : ViewModelBase, IQueryAttributable
{
    private Guid _id;

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
    public partial bool NotStarted { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        var snapshot = (await store.GetForecastSnapshotsAsync()).FirstOrDefault(s => s.Id == _id);
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

        var comparison = SnapshotComparer.Compare(snapshot, await store.GetAccountsAsync(), await store.GetEntriesAsync(), today);
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
        Parts.Add(new AmountLine(translator["Snapshot_UnplannedIncome"], Money(comparison.UnplannedIncome, true), false));
        Parts.Add(new AmountLine(translator["Snapshot_PlansChanged"], Money(comparison.PlansChanged, true), false, comparison.PlansChanged < 0));
        Parts.Add(new AmountLine(translator["Snapshot_Difference"], Money(comparison.Difference, true), true, comparison.Difference < 0));
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Snapshot_Delete"], translator["Snapshot_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await store.DeleteForecastSnapshotAsync(_id);
            await Shell.Current.GoToAsync("..");
        }
    }
}
