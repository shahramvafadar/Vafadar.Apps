using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A category row of the expense table: gross, refunds and net (REP-02).</summary>
public sealed record CategoryReportRow(IReadOnlyCollection<Guid> CategoryIds, string Name, Color Color, string GrossText, string RefundsText, string NetText, Color NetColor);

/// <summary>A labelled amount, e.g. one line of an account movement.</summary>
public sealed record AmountLine(string Label, string Amount, bool IsTotal);

/// <summary>An account with its movement lines.</summary>
public sealed record AccountReport(string Name, IReadOnlyList<AmountLine> Lines);

/// <summary>One month of the trend chart; amounts are major units for the chart axis.</summary>
public sealed record TrendPoint(string Label, double Income, double Expense, string IncomeText, string ExpenseText, string ResultText);

/// <summary>A plan with planned and recorded amounts.</summary>
/// <summary>Spending of one tag.</summary>
public sealed record TagReportRow(string Tag, string Name, string AmountText, string Details);

public sealed record PlanReportRow(string Name, string PlannedText, string ActualText, string Details);

/// <summary>
/// Reports (UI-11, REP-01..06): expenses by category, income and expense, trend, account movement and plan versus
/// actual. Every number uses the period at the top and the accounts included in totals; tapping a number lists the
/// entries behind it (REP-01, AT-50).
/// </summary>
public sealed partial class ReportsViewModel : ViewModelBase, IQueryAttributable
{
    private const int TrendMonths = 6;

    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private int _year;
    private int _month;
    private DateOnly _from;
    private DateOnly _to;
    private string _currency = Currencies.Euro.Code;

    public ReportsViewModel(ZananceStore store, PlanStore plans, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, Security.AppLockService appLock)
    {
        _lock = appLock;
        _store = store;
        _plans = plans;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        PeriodKindNames = [translator["Report_Month"], translator["Report_Year"]];
        ReportNames = [translator["Report_Expenses"], translator["Report_IncomeExpense"], translator["Report_Trend"], translator["Report_Accounts"], translator["Report_Plans"], translator["Report_Tags"]];
        PeriodText = string.Empty;
    }

    public IReadOnlyList<string> PeriodKindNames { get; }

    public IReadOnlyList<string> ReportNames { get; }

    private readonly Security.AppLockService _lock;

    // PDF of the period (REP-07): every report of the screen in one file, clearly marked as not official.
    [RelayCommand]
    private async Task SharePdfAsync()
    {
        if (IsBusy || !await _lock.ConfirmAsync(_translator["Lock_ConfirmExport"]))
        {
            return;
        }

        IsBusy = true;
        var shown = ReportIndex;
        try
        {
            var pdf = await CreatePdfAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, string.Create(CultureInfo.InvariantCulture, $"zanance-report-{_from:yyyy-MM}.pdf"));
            await File.WriteAllBytesAsync(path, pdf);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = _translator["Report_SharePdf"], File = new ShareFile(path, "application/pdf") });
        }
        finally
        {
            IsBusy = false;
            ReportIndex = shown;
            await LoadAsync();
        }
    }

    /// <summary>Creates the PDF of the current period with all reports; the screen must be reloaded afterwards.</summary>
    public async Task<byte[]> CreatePdfAsync()
    {
        {
            var accounts = await _store.GetAccountsAsync();
            var entries = await _store.GetEntriesAsync();
            var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            var culture = _localization.CurrentCulture;
            BuildExpenses(accounts, entries, categories, culture);
            BuildIncomeExpense(accounts, entries, culture);
            BuildAccounts(accounts, entries, culture);
            await BuildPlansAsync(accounts, entries, culture);
            BuildTags(accounts, entries, culture);

            var tables = new List<Vafadar.Zanance.Reports.ReportTable>();
            if (CategoryRows.Count > 0)
            {
                tables.Add(new(_translator["Report_GrossByCategory"], [_translator["Entry_Category"], _translator["Report_Gross"], _translator["Report_Refunds"], _translator["Report_Net"]],
                    [.. CategoryRows.Select(r => (IReadOnlyList<string>)[r.Name, r.GrossText, r.RefundsText, r.NetText])], new HashSet<int> { 1, 2, 3 }));
            }

            if (Accounts.Count > 0)
            {
                tables.Add(new(_translator["Report_Accounts"], [_translator["Entry_Account"], _translator["Report_Opening"], _translator["Report_Closing"]],
                    [.. Accounts.Select(a => (IReadOnlyList<string>)[a.Name, a.Lines.FirstOrDefault()?.Amount ?? string.Empty, a.Lines.LastOrDefault()?.Amount ?? string.Empty])], new HashSet<int> { 1, 2 }));
            }

            if (PlanRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Plans"], [_translator["Plan_Name"], _translator["Report_Planned"], _translator["Report_Actual"]],
                    [.. PlanRows.Select(p => (IReadOnlyList<string>)[p.Name, p.PlannedText, p.ActualText])], new HashSet<int> { 1, 2 }));
            }

            if (TagRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Tags"], [_translator["Entry_Tags"], _translator["Report_Net"]],
                    [.. TagRows.Select(t => (IReadOnlyList<string>)["#" + t.Tag, t.AmountText])], new HashSet<int> { 1 }));
            }

            var document = new Vafadar.Zanance.Reports.ReportDocument(
                _translator.Format("Report_PdfTitle", _translator["App_Name"]),
                PeriodText,
                _translator.Format("Report_PdfScope", _currency),
                _translator.Format("Report_PdfCreated", _dates.Format(Today, DateFormatStyle.Long)),
                _translator["Report_PdfDisclaimer"],
                _localization.IsRightToLeft,
                [.. IncomeExpenseLines.Select(l => new Vafadar.Zanance.Reports.ReportLine(l.Label, l.Amount))],
                tables);
            return await Task.Run(() => Vafadar.Zanance.Reports.PdfReport.Write(document));
        }
    }

    public ObservableCollection<CategorySlice> Slices { get; } = [];

    public ObservableCollection<Brush> SliceBrushes { get; } = [];

    public ObservableCollection<CategoryReportRow> CategoryRows { get; } = [];

    public ObservableCollection<AmountLine> IncomeExpenseLines { get; } = [];

    public ObservableCollection<TrendPoint> Trend { get; } = [];

    public ObservableCollection<AccountReport> Accounts { get; } = [];

    public ObservableCollection<PlanReportRow> PlanRows { get; } = [];

    [ObservableProperty]
    public partial int PeriodKind { get; set; }

    [ObservableProperty]
    public partial int ReportIndex { get; set; }

    [ObservableProperty]
    public partial string PeriodText { get; set; }

    [ObservableProperty]
    public partial string? RefundsText { get; set; }

    [ObservableProperty]
    public partial string? CurrencyNote { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasSlices { get; set; }

    [ObservableProperty]
    public partial bool HasPlanRows { get; set; }

    [ObservableProperty]
    public partial Symbol PreviousIcon { get; set; }

    [ObservableProperty]
    public partial Symbol NextIcon { get; set; }

    public bool ShowExpenses => ReportIndex == 0;

    public bool ShowIncomeExpense => ReportIndex == 1;

    public bool ShowTrend => ReportIndex == 2;

    public bool ShowAccounts => ReportIndex == 3;

    public bool ShowPlans => ReportIndex == 4;

    public bool ShowTags => ReportIndex == 5;

    // Spending per tag (F2-TX-04, REP-08). An entry with several tags appears under each, so tags do not add up.
    public ObservableCollection<TagReportRow> TagRows { get; } = [];

    [ObservableProperty]
    public partial bool HasTagRows { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;

    partial void OnPeriodKindChanged(int value) => _ = LoadAsync();

    partial void OnReportIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowExpenses));
        OnPropertyChanged(nameof(ShowIncomeExpense));
        OnPropertyChanged(nameof(ShowTrend));
        OnPropertyChanged(nameof(ShowAccounts));
        OnPropertyChanged(nameof(ShowPlans));
        OnPropertyChanged(nameof(ShowTags));
        _ = LoadAsync();
    }

    /// <summary>Query: <c>report</c> (index of the report to show).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("report", out var report) && report is int index)
        {
            ReportIndex = index;
        }
    }

    public async Task LoadAsync()
    {
        if (_year == 0)
        {
            (_year, _month) = PeriodMath.MonthOf(Today, Calendar);
        }

        PreviousIcon = _localization.IsRightToLeft ? Symbol.ChevronRight : Symbol.ChevronLeft;
        NextIcon = _localization.IsRightToLeft ? Symbol.ChevronLeft : Symbol.ChevronRight;

        (_from, _to) = PeriodKind == 0 ? PeriodMath.MonthRange(_year, _month, Calendar) : (PeriodMath.MonthRange(_year, 1, Calendar).First, PeriodMath.MonthRange(_year, 12, Calendar).Last);
        PeriodText = PeriodKind == 0 ? _dates.Format(_from, DateFormatStyle.MonthYear) : _year.ToString(CultureInfo.InvariantCulture);
        if (_to >= Today && _from <= Today)
        {
            // A running period is labelled as such, so it is not compared as if it were complete (REP-05).
            PeriodText += " · " + _translator["Report_SoFar"];
        }

        _currency = (await _store.GetSettingsAsync()).ReportCurrencyCode;
        var accounts = await _store.GetAccountsAsync();
        var entries = await _store.GetEntriesAsync();
        var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
        var culture = _localization.CurrentCulture;
        IsEmpty = entries.Count == 0;

        switch (ReportIndex)
        {
            case 0:
                BuildExpenses(accounts, entries, categories, culture);
                break;
            case 1:
                BuildIncomeExpense(accounts, entries, culture);
                break;
            case 2:
                BuildTrend(accounts, entries, culture);
                break;
            case 3:
                BuildAccounts(accounts, entries, culture);
                break;
            case 4:
                await BuildPlansAsync(accounts, entries, culture);
                break;
            default:
                BuildTags(accounts, entries, culture);
                break;
        }
    }

    // Gross expense chart (never negative), refunds card, net table (REP-02, AT-13).
    private void BuildExpenses(List<Account> accounts, List<LedgerEntry> entries, CategoryLookup categories, CultureInfo culture)
    {
        Slices.Clear();
        SliceBrushes.Clear();
        CategoryRows.Clear();
        Guid? TopLevel(Guid? id) => categories.Get(id)?.ParentId ?? id;
        var rows = LedgerCalculator.ExpenseByCategory(accounts, entries, new LedgerFilter(_from, _to), TopLevel)
            .Where(r => string.Equals(r.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.GrossExpense)
            .ToList();
        var otherCurrencies = LedgerCalculator.ExpenseByCategory(accounts, entries, new LedgerFilter(_from, _to), TopLevel)
            .Any(r => !string.Equals(r.CurrencyCode, _currency, StringComparison.OrdinalIgnoreCase));
        CurrencyNote = otherCurrencies ? _translator.Format("Home_ChartCurrency", _currency) : null;

        var gross = rows.Sum(r => r.GrossExpense);
        var currency = Currencies.TryGet(_currency, out var known) ? known : Currencies.Euro;
        foreach (var row in rows)
        {
            var ids = categories.All.Where(c => c.Id == row.CategoryId || c.ParentId == row.CategoryId).Select(c => c.Id).ToList();
            var color = categories.Color(row.CategoryId);
            var name = categories.Name(row.CategoryId);
            if (row.GrossExpense > 0)
            {
                var brush = new SolidColorBrush(color);
                Slices.Add(new CategorySlice(ids, name, (double)MoneyText.ToDecimal(row.GrossExpense, currency), MoneyText.Format(row.GrossExpense, _currency, culture),
                    ((double)row.GrossExpense / gross).ToString("P0", culture), color, brush));
                SliceBrushes.Add(brush);
            }

            CategoryRows.Add(new CategoryReportRow(
                ids,
                name,
                color,
                MoneyText.Format(row.GrossExpense, _currency, culture),
                row.Refunds > 0 ? MoneyText.Format(-row.Refunds, _currency, culture) : "–",
                MoneyText.Format(row.Net, _currency, culture),
                row.Net < 0 ? EntryPresenter.IncomeColor : EntryPresenter.ExpenseColor));
        }

        var refunds = rows.Sum(r => r.Refunds);
        RefundsText = refunds > 0 ? _translator.Format("Report_RefundsTotal", MoneyText.Format(refunds, _currency, culture)) : null;
        HasSlices = Slices.Count > 0;
    }

    // Income, net expense and result per currency (REP-03, REP-04).
    private void BuildIncomeExpense(List<Account> accounts, List<LedgerEntry> entries, CultureInfo culture)
    {
        IncomeExpenseLines.Clear();
        var totals = LedgerCalculator.Totals(accounts, entries, new LedgerFilter(_from, _to));
        if (totals.Count == 0)
        {
            IncomeExpenseLines.Add(new AmountLine(_translator["Report_NoData"], string.Empty, false));
            return;
        }

        foreach (var total in totals)
        {
            IncomeExpenseLines.Add(new AmountLine(_translator["KindFilter_Income"], MoneyText.Format(total.NetIncome, total.CurrencyCode, culture), false));
            IncomeExpenseLines.Add(new AmountLine(_translator["Report_NetExpense"], MoneyText.Format(total.NetExpense, total.CurrencyCode, culture), false));
            if (total.Refunds > 0)
            {
                IncomeExpenseLines.Add(new AmountLine(_translator["Report_OfWhichRefunds"], MoneyText.Format(total.Refunds, total.CurrencyCode, culture), false));
            }

            IncomeExpenseLines.Add(new AmountLine(_translator["Home_Result"], MoneyText.Format(total.Result, total.CurrencyCode, culture, showPlus: true), true));
            if (total.NetIncome > 0)
            {
                var rate = (double)total.Result / total.NetIncome;
                IncomeExpenseLines.Add(new AmountLine(_translator["Report_SavingsRate"], rate.ToString("P0", culture), false));
            }
        }
    }

    private void BuildTrend(List<Account> accounts, List<LedgerEntry> entries, CultureInfo culture)
    {
        Trend.Clear();
        var currency = Currencies.TryGet(_currency, out var known) ? known : Currencies.Euro;
        var end = _to < Today ? _to : Today;
        foreach (var month in ReportCalculator.MonthlyTrend(accounts, entries, end, PeriodKind == 0 ? TrendMonths : 12, Calendar, _currency))
        {
            var label = _dates.Format(month.From, DateFormatStyle.MonthYear) + (month.IsPartial ? " *" : string.Empty);
            Trend.Add(new TrendPoint(
                label,
                (double)MoneyText.ToDecimal(month.NetIncome, currency),
                (double)MoneyText.ToDecimal(Math.Max(0, month.NetExpense), currency),
                MoneyText.Format(month.NetIncome, _currency, culture),
                MoneyText.Format(month.NetExpense, _currency, culture),
                MoneyText.Format(month.Result, _currency, culture, showPlus: true)));
        }
    }

    // Opening → movements → closing for each account (REP-03).
    private void BuildAccounts(List<Account> accounts, List<LedgerEntry> entries, CultureInfo culture)
    {
        Accounts.Clear();
        foreach (var movement in ReportCalculator.AccountMovements(accounts.Where(a => !a.IsArchived || entries.Any(e => e.AccountId == a.Id && e.Date >= _from && e.Date <= _to)), entries, _from, _to))
        {
            var account = accounts.First(a => a.Id == movement.AccountId);
            string Format(long value, bool plus = false) => MoneyText.Format(value, movement.CurrencyCode, culture, showPlus: plus);
            var lines = new List<AmountLine> { new(_translator["Report_Opening"], Format(movement.Opening), true) };
            void Add(string key, long value, bool negative = false)
            {
                if (value != 0)
                {
                    lines.Add(new AmountLine(_translator[key], Format(negative ? -value : value, plus: true), false));
                }
            }

            Add("Report_OpeningBalance", movement.OpeningBalanceAdded);
            Add("KindFilter_Income", movement.Income);
            Add("Report_Refunds", movement.Refunds);
            Add("KindFilter_Expenses", movement.Expense, negative: true);
            Add("EntryKind_IncomeReversal", movement.IncomeReversals, negative: true);
            Add("Report_TransfersIn", movement.TransfersIn);
            Add("Report_TransfersOut", movement.TransfersOut, negative: true);
            Add("Report_Adjustments", movement.Adjustments);
            lines.Add(new AmountLine(_translator["Report_Closing"], Format(movement.Closing), true));
            Accounts.Add(new AccountReport(account.Name, lines));
        }
    }

    private void BuildTags(List<Account> accounts, List<LedgerEntry> entries, CultureInfo culture)
    {
        TagRows.Clear();
        var inPeriod = entries.Where(e => e.Date >= _from && e.Date <= _to && e.Tags.Count > 0).ToList();
        foreach (var tag in EntryTags.InUse(inPeriod))
        {
            var tagged = inPeriod.Where(e => e.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)).ToList();
            var spent = Core.Budgets.BudgetCalculator.NetExpense(accounts, tagged, _from, _to, _currency);
            if (spent != 0)
            {
                TagRows.Add(new TagReportRow(tag, EntryTags.Display(tag), MoneyText.Format(spent, _currency, culture), _translator.Format("Report_TagCount", tagged.Count)));
            }
        }

        HasTagRows = TagRows.Count > 0;
    }

    [RelayCommand]
    private Task OpenTagAsync(TagReportRow row) =>
        Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["from"] = _from, ["to"] = _to, ["kind"] = KindFilter.Expenses, ["inTotals"] = true, ["search"] = "#" + row.Tag });

    private async Task BuildPlansAsync(List<Account> accounts, List<LedgerEntry> entries, CultureInfo culture)
    {
        PlanRows.Clear();
        var byId = accounts.ToDictionary(a => a.Id);
        var rows = ReportCalculator.PlanVsActual(await _plans.GetSchedulesAsync(), await _plans.GetStatesAsync(), entries, _from, _to, Today);
        foreach (var row in rows)
        {
            var currency = byId.TryGetValue(row.Schedule.AccountId, out var account) ? account.CurrencyCode : _currency;
            var details = _translator.Format("Report_PlanDetails", row.SettledCount, row.PlannedCount)
                + (row.UnknownCount > 0 ? " · " + _translator.Format("Budget_PlannedUnknown", row.UnknownCount) : string.Empty)
                + (row.OpenCount > 0 ? " · " + _translator.Format("Report_PlanOpen", row.OpenCount) : string.Empty)
                + (row.Variance is { } variance and not 0 ? " · " + _translator.Format("Report_PlanVariance", MoneyText.Format(variance, currency, culture, showPlus: true)) : string.Empty)
                + (BudgetPlanning.MonthlyEquivalent(row.Schedule) is { } monthly ? " · " + _translator.Format("Report_PlanMonthly", MoneyText.Format(monthly, currency, culture, approximate: true)) : string.Empty);
            PlanRows.Add(new PlanReportRow(row.Schedule.Name, MoneyText.Format(row.Planned, currency, culture), MoneyText.Format(row.Actual, currency, culture), details));
        }

        HasPlanRows = PlanRows.Count > 0;
    }

    [RelayCommand]
    private Task PreviousAsync()
    {
        if (PeriodKind == 0)
        {
            (_year, _month) = PeriodMath.Previous(_year, _month);
        }
        else
        {
            _year--;
        }

        return LoadAsync();
    }

    [RelayCommand]
    private Task NextAsync()
    {
        if (PeriodKind == 0)
        {
            (_year, _month) = PeriodMath.Next(_year, _month);
        }
        else
        {
            _year++;
        }

        return LoadAsync();
    }

    [RelayCommand]
    private Task OpenCategoryAsync(CategoryReportRow row) => Drill(KindFilter.Expenses, row.CategoryIds, row.Name);

    [RelayCommand]
    private Task OpenSliceAsync(CategorySlice slice) => Drill(KindFilter.Expenses, slice.CategoryIds, slice.Name);

    [RelayCommand]
    private Task OpenExpensesAsync() => Drill(KindFilter.Expenses, null, null);

    [RelayCommand]
    private Task OpenIncomeAsync() => Drill(KindFilter.Income, null, null);

    private Task Drill(KindFilter kind, IReadOnlyCollection<Guid>? categories, string? name)
    {
        var query = new Dictionary<string, object> { ["from"] = _from, ["to"] = _to, ["kind"] = kind, ["inTotals"] = true };
        if (categories is not null)
        {
            query["categories"] = categories;
            query["categoryName"] = name ?? string.Empty;
        }

        return Shell.Current.GoToAsync("//transactions", query);
    }
}
