using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A change of spending in one category against the comparison period (ZEX-K11).</summary>
public sealed record ChangeRow(IReadOnlyCollection<Guid>? CategoryIds, string Name, string DeltaText, string Detail, Color DeltaColor);

/// <summary>R1 – period overview: surplus, rate, spending changes, separate lines, categories, trend and tags.</summary>
public sealed partial class ReportsViewModel
{
    private SurplusResult? _surplus;
    private ComparisonRange? _range;

    public ObservableCollection<CategorySlice> Slices { get; } = [];

    public ObservableCollection<Brush> SliceBrushes { get; } = [];

    public ObservableCollection<CategoryReportRow> CategoryRows { get; } = [];

    /// <summary>Gets income, spending (expenses − refunds) and the surplus (ZEX-K05).</summary>
    public ObservableCollection<AmountLine> OverviewLines { get; } = [];

    /// <summary>Gets the movements that are neither income nor spending: transfers and capital movements (ZEX-R1).</summary>
    public ObservableCollection<AmountLine> SeparateLines { get; } = [];

    public ObservableCollection<ChangeRow> Changes { get; } = [];

    public ObservableCollection<TrendPoint> Trend { get; } = [];

    // Spending per tag (F2-TX-04, REP-08). An entry with several tags appears under each, so tags do not add up.
    public ObservableCollection<TagReportRow> TagRows { get; } = [];

    [ObservableProperty]
    public partial string? SurplusText { get; set; }

    [ObservableProperty]
    public partial Color? SurplusColor { get; set; }

    [ObservableProperty]
    public partial string? SurplusDetail { get; set; }

    /// <summary>Gets the surplus rate (ZEX-K06), or "not available" without income.</summary>
    [ObservableProperty]
    public partial string? RateText { get; set; }

    /// <summary>Gets the surplus of the other currencies, never added to this one.</summary>
    [ObservableProperty]
    public partial string? OtherCurrenciesText { get; set; }

    /// <summary>Gets the converted surplus of all currencies (Advanced, valuation currency on), with its rate status.</summary>
    [ObservableProperty]
    public partial string? ConvertedText { get; set; }

    /// <summary>Gets what the spending changes compare, e.g. "Oct 1–12 vs Sep 1–12".</summary>
    [ObservableProperty]
    public partial string? ComparisonText { get; set; }

    [ObservableProperty]
    public partial bool HasChanges { get; set; }

    [ObservableProperty]
    public partial bool HasSeparateLines { get; set; }

    [ObservableProperty]
    public partial string? RefundsText { get; set; }

    // The total in the middle of the doughnut, so the chart answers "how much in total" at a glance.
    [ObservableProperty]
    public partial string? SliceTotalText { get; set; }

    [ObservableProperty]
    public partial bool HasSlices { get; set; }

    [ObservableProperty]
    public partial bool HasTagRows { get; set; }

    private async Task BuildOverviewAsync(IReadOnlyCollection<LedgerEntry> entries)
    {
        var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
        var filter = Filter(_from, _to);
        var all = KpiCatalog.Surplus(_accounts, entries, filter);
        _surplus = all.FirstOrDefault(s => string.Equals(s.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));

        // K05/K06 of the chosen currency; other currencies are named, never added.
        var surplus = _surplus ?? new SurplusResult(_currency, 0, 0, 0, 0, 0, 0, 0);
        SurplusText = Money(surplus.Surplus, showPlus: true);
        SurplusColor = surplus.Surplus < 0 ? EntryPresenter.DangerColor : surplus.Surplus > 0 ? EntryPresenter.IncomeColor : Palette.AmountText;
        SurplusDetail = _translator.Format("Report_SurplusDetail", Money(surplus.EligibleIncome), Money(surplus.Consumption));
        RateText = surplus.RatePercent is { } rate
            ? _translator.Format("Report_Rate", rate.ToString("0.0", Culture) + " %")
            : _translator.Format("Report_Rate", _translator["Report_NotAvailable"]);
        var others = all.Where(s => !string.Equals(s.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase) && s.Surplus != 0).ToList();
        OtherCurrenciesText = others.Count == 0 ? null : _translator.Format("Report_OtherCurrencies", string.Join(" · ", others.Select(s => Money(s.Surplus, s.CurrencyCode, showPlus: true))));
        ConvertedText = null;
        if (IsAdvanced && _settings.ValuationCurrencyEnabled && all.Count > 1)
        {
            var combined = new Core.Rates.RateTable(await _store.GetRatesAsync()).Combine(all.ToDictionary(s => s.CurrencyCode, s => s.Surplus), _settings.ReportCurrencyCode, _to < Today ? _to : Today, new Core.Rates.RateFreshness(_settings.RateFreshnessDays));
            ConvertedText = combined.IsComplete
                ? _translator.Format("Report_Converted", Money(combined.Total!.Value, _settings.ReportCurrencyCode, showPlus: true))
                : _translator.Format("Home_CombinedIncomplete", string.Join(", ", combined.MissingCurrencies));
        }

        OverviewLines.Clear();
        OverviewLines.Add(new AmountLine(_translator["KindFilter_Income"], Money(surplus.EligibleIncome, showPlus: true), false));
        OverviewLines.Add(new AmountLine(_translator.Format("Report_SpendingLine", Money(surplus.Expense), Money(surplus.Refunds)), Money(-surplus.Consumption), false));
        OverviewLines.Add(new AmountLine(_translator["Report_Surplus"], Money(surplus.Surplus, showPlus: true), true, surplus.Surplus < 0, surplus.Surplus > 0));

        // Money moved between accounts or exchanged for holdings: shown, but never income or spending (ZEX-R1).
        SeparateLines.Clear();
        if (surplus.Transfers > 0)
        {
            SeparateLines.Add(new AmountLine(_translator["Report_TransfersBetween"], Money(surplus.Transfers), false));
        }

        if (surplus.CapitalPurchases > 0)
        {
            SeparateLines.Add(new AmountLine(_translator["EntryKind_AssetPurchase"], Money(surplus.CapitalPurchases), false));
        }

        if (surplus.CapitalSales > 0)
        {
            SeparateLines.Add(new AmountLine(_translator["EntryKind_AssetSale"], Money(surplus.CapitalSales), false));
        }

        HasSeparateLines = SeparateLines.Count > 0;
        BuildChanges(entries, categories);
        BuildExpenses(entries, categories);
        if (IsAdvanced)
        {
            BuildTrend(entries);
            BuildTags(entries);
        }

        await BuildStatusAsync(entries, full: false);
    }

    // K11: the comparison period has the same length while the period runs (AT30); Simple shows the top three.
    private void BuildChanges(IReadOnlyCollection<LedgerEntry> entries, CategoryLookup categories)
    {
        Changes.Clear();
        var (previousFrom, previousTo) = PeriodKind == 0
            ? PeriodMath.MonthRange(PeriodMath.Previous(_year, _month).Year, PeriodMath.Previous(_year, _month).Month, Calendar, _startDay)
            : (PeriodMath.MonthRange(_year - 1, 1, Calendar).First, PeriodMath.MonthRange(_year - 1, 12, Calendar).Last);
        _range = KpiCatalog.Compare(_from, _to, previousFrom, previousTo, Today);
        ComparisonText = _translator.Format("Report_Comparison",
            $"{_dates.Format(_range.From, DateFormatStyle.Short)} – {_dates.Format(_range.To, DateFormatStyle.Short)}",
            $"{_dates.Format(_range.CompareFrom, DateFormatStyle.Short)} – {_dates.Format(_range.CompareTo, DateFormatStyle.Short)}");
        Guid? TopLevel(Guid? id) => categories.Get(id)?.ParentId ?? id;
        var changes = KpiCatalog.SpendingChanges(_accounts, entries, _range, _currency, TopLevel, ScopeIds(), ConfirmedOnly);
        foreach (var change in changes.Where(c => c.Delta != 0).Take(IsAdvanced ? 12 : 3))
        {
            var ids = change.CategoryId is { } id ? (IReadOnlyCollection<Guid>)[.. categories.All.Where(c => c.Id == id || c.ParentId == id).Select(c => c.Id)] : null;
            var percent = change.DeltaPercent is { } p ? $" ({(p > 0 ? "+" : string.Empty)}{p.ToString("0", Culture)} %)" : change.IsNew ? $" ({_translator["Report_New"]})" : string.Empty;
            Changes.Add(new ChangeRow(
                ids,
                categories.Name(change.CategoryId),
                Money(change.Delta, showPlus: true) + percent,
                _translator.Format("Report_ChangeDetail", Money(change.Current), Money(change.Comparison)),
                change.Delta > 0 ? EntryPresenter.DangerColor : EntryPresenter.IncomeColor));
        }

        HasChanges = Changes.Count > 0;
    }

    // Gross expense chart (never negative), refunds card, net table (REP-02, AT-13).
    private void BuildExpenses(IReadOnlyCollection<LedgerEntry> entries, CategoryLookup categories)
    {
        Slices.Clear();
        SliceBrushes.Clear();
        CategoryRows.Clear();
        Guid? TopLevel(Guid? id) => categories.Get(id)?.ParentId ?? id;
        var rows = LedgerCalculator.ExpenseByCategory(_accounts, entries, Filter(_from, _to), TopLevel)
            .Where(r => string.Equals(r.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.GrossExpense)
            .ToList();
        var gross = rows.Sum(r => r.GrossExpense);
        SliceTotalText = gross > 0 ? Money(gross) : null;
        var currency = Currencies.TryGet(_currency, out var known) ? known : Currencies.Euro;
        foreach (var row in rows)
        {
            var ids = categories.All.Where(c => c.Id == row.CategoryId || c.ParentId == row.CategoryId).Select(c => c.Id).ToList();
            var color = categories.Color(row.CategoryId);
            var name = categories.Name(row.CategoryId);
            if (row.GrossExpense > 0)
            {
                var brush = new SolidColorBrush(color);
                Slices.Add(new CategorySlice(ids, name, (double)MoneyText.ToDecimal(row.GrossExpense, currency), Money(row.GrossExpense),
                    ((double)row.GrossExpense / gross).ToString("P0", Culture), color, brush));
                SliceBrushes.Add(brush);
            }

            CategoryRows.Add(new CategoryReportRow(
                ids,
                name,
                color,
                Money(row.GrossExpense),
                row.Refunds > 0 ? Money(-row.Refunds) : "–",
                Money(row.Net),
                row.Net < 0 ? EntryPresenter.IncomeColor : EntryPresenter.ExpenseColor,
                $"{_translator["Report_Gross"]} {Money(row.GrossExpense)}"
                    + (row.Refunds > 0 ? $" · {_translator["Report_Refunds"]} {Money(-row.Refunds)}" : string.Empty)));
        }

        var refunds = rows.Sum(r => r.Refunds);
        RefundsText = refunds > 0 ? _translator.Format("Report_RefundsTotal", Money(refunds)) : null;
        HasSlices = Slices.Count > 0;
    }

    private void BuildTrend(IReadOnlyCollection<LedgerEntry> entries)
    {
        Trend.Clear();
        var currency = Currencies.TryGet(_currency, out var known) ? known : Currencies.Euro;
        var end = _to < Today ? _to : Today;
        var scope = ScopedAccounts().ToList();
        var scoped = ConfirmedOnly ? [.. entries.Where(e => e.Review == ReviewState.Confirmed)] : entries;
        foreach (var month in ReportCalculator.MonthlyTrend(scope, scoped, end, PeriodKind == 0 ? TrendMonths : 12, Calendar, _currency, PeriodKind == 0 ? _startDay : 1))
        {
            var label = _dates.Format(month.From, DateFormatStyle.MonthYear) + (month.IsPartial ? " *" : string.Empty);
            Trend.Add(new TrendPoint(
                label,
                (double)MoneyText.ToDecimal(month.NetIncome, currency),
                (double)MoneyText.ToDecimal(Math.Max(0, month.NetExpense), currency),
                Money(month.NetIncome),
                Money(month.NetExpense),
                Money(month.Result, showPlus: true),
                month.Result < 0 ? EntryPresenter.DangerColor : month.Result > 0 ? EntryPresenter.IncomeColor : Palette.AmountText));
        }
    }

    // Each tag counts an entry once; an entry with two tags is under both, so the tag table never adds up (AT29).
    private void BuildTags(IReadOnlyCollection<LedgerEntry> entries)
    {
        TagRows.Clear();
        var scope = ScopedAccounts().ToList();
        var inPeriod = entries.Where(e => e.Date >= _from && e.Date <= _to && e.Tags.Count > 0 && (!ConfirmedOnly || e.Review == ReviewState.Confirmed)).ToList();
        foreach (var tag in EntryTags.InUse(inPeriod))
        {
            var tagged = inPeriod.Where(e => e.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)).ToList();
            var spent = BudgetCalculator.NetExpense(scope, tagged, _from, _to, _currency);
            if (spent != 0)
            {
                var shared = tagged.Count(e => e.Tags.Count > 1);
                var details = _translator.Format("Report_TagCount", tagged.Count) + (shared > 0 ? " · " + _translator.Format("Report_TagShared", shared) : string.Empty);
                TagRows.Add(new TagReportRow(tag, EntryTags.Display(tag), Money(spent), details));
            }
        }

        HasTagRows = TagRows.Count > 0;
    }

    [RelayCommand]
    private Task OpenTagAsync(TagReportRow row) =>
        Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["from"] = _from, ["to"] = _to, ["kind"] = KindFilter.Expenses, ["inTotals"] = AccountScopeIndex == 0, ["currency"] = _currency, ["search"] = "#" + row.Tag, ["scope"] = ScopeText });

    [RelayCommand]
    private Task OpenCategoryAsync(CategoryReportRow row) => Drill(KindFilter.Expenses, row.CategoryIds, row.Name);

    [RelayCommand]
    private Task OpenSliceAsync(CategorySlice slice) => Drill(KindFilter.Expenses, slice.CategoryIds, slice.Name);

    [RelayCommand]
    private Task OpenChangeAsync(ChangeRow row) => Drill(KindFilter.Expenses, row.CategoryIds, row.Name, _range?.From, _range?.To);

    [RelayCommand]
    private Task OpenExpensesAsync() => Drill(KindFilter.Expenses, null, null);

    [RelayCommand]
    private Task OpenIncomeAsync() => Drill(KindFilter.Income, null, null);

    [RelayCommand]
    private Task OpenSurplusEntriesAsync() => Drill(KindFilter.All, null, null);

    [RelayCommand]
    private Task ExplainSurplusAsync()
    {
        var s = _surplus ?? new SurplusResult(_currency, 0, 0, 0, 0, 0, 0, 0);
        return ExplainAsync("K05", SurplusText ?? string.Empty,
        [
            new(_translator["KindFilter_Income"], Money(s.EligibleIncome), false),
            new("− " + _translator["KindFilter_Expenses"], Money(s.Expense), false),
            new("+ " + _translator["Report_Refunds"], Money(s.Refunds), false),
            new("= " + _translator["Report_Surplus"], SurplusText ?? string.Empty, true),
        ], () => Drill(KindFilter.All, null, null));
    }

    [RelayCommand]
    private Task ExplainRateAsync() => ExplainAsync("K06", RateText ?? string.Empty,
        [new(_translator["Report_Surplus"], SurplusText ?? string.Empty, false), new(_translator["KindFilter_Income"], Money(_surplus?.EligibleIncome ?? 0), false)],
        () => Drill(KindFilter.All, null, null));

    [RelayCommand]
    private Task ExplainChangesAsync() => ExplainAsync("K11", ComparisonText ?? string.Empty, [.. Changes.Select(c => new AmountLine(c.Name, c.DeltaText, false))],
        () => Drill(KindFilter.Expenses, null, null, _range?.From, _range?.To));
}
