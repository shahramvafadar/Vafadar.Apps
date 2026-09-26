using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Plans;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Rates;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Home;

/// <summary>Income, expense and result of the period in one currency.</summary>
public sealed record PeriodSummary(string IncomeText, string ExpenseText, string ResultText, Color ResultColor, string? RefundsText);

/// <summary>A category slice of the expense chart and its row in the table below it (UX-06: charts have a table).</summary>
public sealed record CategorySlice(IReadOnlyCollection<Guid> CategoryIds, string Name, double Value, string AmountText, string PercentText, Color Color, Brush Brush);

/// <summary>
/// Home dashboard (UI-02, DASH-01..04). Every number uses one filter set – the period chosen at the top, the accounts
/// included in totals and their currencies – and every number can be tapped to see the entries behind it (AT-50).
/// </summary>
public sealed partial class HomeViewModel : ViewModelBase
{
    private const int MaxSlices = 6;

    private readonly FinanceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private string _reportCurrency = Currencies.Euro.Code;

    public HomeViewModel(FinanceStore store, PlanStore plans, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _plans = plans;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        PeriodNames = [translator["Period_ThisMonth"], translator["Period_LastMonth"]];
        ScopeText = string.Empty;
    }

    public IReadOnlyList<string> PeriodNames { get; }

    public ObservableCollection<CurrencyTotal> Balances { get; } = [];

    public ObservableCollection<PeriodSummary> Periods { get; } = [];

    public ObservableCollection<PlanRow> Upcoming { get; } = [];

    public ObservableCollection<CategorySlice> Slices { get; } = [];

    public ObservableCollection<Brush> SliceBrushes { get; } = [];

    public ObservableCollection<AccountItem> Accounts { get; } = [];

    [ObservableProperty]
    public partial int PeriodIndex { get; set; }

    [ObservableProperty]
    public partial string ScopeText { get; set; }

    [ObservableProperty]
    public partial int UnreviewedCount { get; set; }

    [ObservableProperty]
    public partial string? UnreviewedText { get; set; }

    [ObservableProperty]
    public partial int DueCount { get; set; }

    [ObservableProperty]
    public partial string? DueText { get; set; }

    [ObservableProperty]
    public partial bool HasAttention { get; set; }

    [ObservableProperty]
    public partial bool HasAccounts { get; set; }

    [ObservableProperty]
    public partial bool HasEntries { get; set; }

    [ObservableProperty]
    public partial bool HasUpcoming { get; set; }

    [ObservableProperty]
    public partial bool HasSlices { get; set; }

    [ObservableProperty]
    public partial string? ChartNote { get; set; }

    [ObservableProperty]
    public partial string? ConfirmedBalanceText { get; set; }

    [ObservableProperty]
    public partial string? CombinedText { get; set; }

    [ObservableProperty]
    public partial bool CombinedIncomplete { get; set; }

    [ObservableProperty]
    public partial string? ForecastText { get; set; }

    [ObservableProperty]
    public partial Color? ForecastColor { get; set; }

    [ObservableProperty]
    public partial bool HasBudget { get; set; }

    [ObservableProperty]
    public partial string? IncompleteText { get; set; }

    // Guidance after the first entries (ONB-04): one tip at a time, each can be dismissed for good.
    [ObservableProperty]
    public partial bool ShowPlansTip { get; set; }

    [ObservableProperty]
    public partial bool ShowBudgetTip { get; set; }

    [ObservableProperty]
    public partial string? BudgetText { get; set; }

    [ObservableProperty]
    public partial double BudgetProgress { get; set; }

    [ObservableProperty]
    public partial Color? BudgetColor { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;

    partial void OnPeriodIndexChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        var today = Today;
        var culture = _localization.CurrentCulture;
        var (from, to) = PeriodRange(today);
        var settings = await _store.GetSettingsAsync();
        _reportCurrency = settings.ReportCurrencyCode;

        var allAccounts = await _store.GetAccountsAsync();
        var accounts = allAccounts.Where(a => !a.IsArchived).ToList();
        var byId = allAccounts.ToDictionary(a => a.Id);
        var entries = await _store.GetEntriesAsync();
        var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);

        HasAccounts = accounts.Count > 0;

        // Data quality (ACC-09): an unknown opening balance makes the total incomplete; say so instead of implying a
        // complete net worth.
        var incomplete = accounts.Count(a => a.IncludeInTotals && !a.OpeningBalanceKnown);
        IncompleteText = incomplete > 0 ? _translator.Format("Home_OpeningUnknown", incomplete) : null;
        HasEntries = entries.Count > 0;
        var periodText = _dates.Format(from, DateFormatStyle.MonthYear);
        ScopeText = _translator.Format("Home_Scope", periodText, _translator["Home_AccountsInTotals"], _reportCurrency);

        // Recorded balance (FIN-13) per currency; totals always follow the accounts included in totals.
        Balances.Clear();
        foreach (var (currency, total) in LedgerCalculator.TotalBalances(allAccounts, entries, today))
        {
            Balances.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        // With several currencies, a combined total only when every rate exists (FX-02, FX-05).
        var balances = LedgerCalculator.TotalBalances(allAccounts, entries, today);
        CombinedText = null;
        CombinedIncomplete = false;
        if (balances.Count > 1)
        {
            var combined = new RateTable(await _store.GetRatesAsync()).Combine(balances, _reportCurrency, today);
            CombinedIncomplete = !combined.IsComplete;
            CombinedText = combined.IsComplete
                ? _translator.Format("Home_Combined", MoneyText.Format(combined.Total!.Value, _reportCurrency, culture), combined.OldestRateDate is { } rateDate ? _dates.Format(rateDate, DateFormatStyle.Short) : "-")
                : _translator.Format("Home_CombinedIncomplete", string.Join(", ", combined.MissingCurrencies));
        }

        var unreviewed = LedgerCalculator.Unreviewed(allAccounts, entries);
        UnreviewedCount = unreviewed.Sum(u => u.Count);

        // "Posted balance – includes unreviewed entries" with the confirmed-only value next to it (FIN-12).
        ConfirmedBalanceText = UnreviewedCount > 0
            ? _translator.Format("Home_ConfirmedOnly", UnreviewedCount, string.Join("  ", LedgerCalculator.TotalBalances(allAccounts, entries, today, confirmedOnly: true)
                .Select(b => MoneyText.Format(b.Value, b.Key, culture))))
            : null;
        UnreviewedText = UnreviewedCount > 0 ? _translator.Format("Home_Unreviewed", UnreviewedCount) : null;

        // Income and expense of the period.
        Periods.Clear();
        foreach (var totals in LedgerCalculator.Totals(allAccounts, entries, new LedgerFilter(from, to)))
        {
            var refunds = totals.Refunds > 0 ? _translator.Format("Home_Refunds", MoneyText.Format(totals.Refunds, totals.CurrencyCode, culture)) : null;
            Periods.Add(new PeriodSummary(
                MoneyText.Format(totals.NetIncome, totals.CurrencyCode, culture),
                MoneyText.Format(totals.NetExpense, totals.CurrencyCode, culture),
                MoneyText.Format(totals.Result, totals.CurrencyCode, culture, showPlus: true),
                totals.Result < 0 ? EntryPresenter.ExpenseColor : EntryPresenter.IncomeColor,
                refunds));
        }

        await LoadBudgetAsync(settings.BudgetCalendar, allAccounts, entries, today, culture);
        await LoadPlansAsync(byId, categories, today);
        await LoadTipsAsync(entries.Count);
        await LoadForecastAsync(settings.Mode, allAccounts, entries, today, culture);
        BuildSlices(allAccounts, entries, categories, from, to, culture);

        Accounts.Clear();
        foreach (var account in accounts)
        {
            var balance = LedgerCalculator.Balance(account, entries, today);
            Accounts.Add(new AccountItem(account.Id, account.Name, _translator[$"AccountType_{account.Type}"],
                Icons.Parse(account.Icon, Icons.For(account.Type)), MoneyText.Format(balance, account.CurrencyCode, culture), balance < 0, !account.IncludeInTotals, !account.OpeningBalanceKnown));
        }

        HasAttention = UnreviewedCount > 0 || DueCount > 0;
    }

    // Remaining overall budget of the month, only when a budget exists (a missing budget is not zero, BUD-01).
    private async Task LoadBudgetAsync(PeriodCalendar calendar, List<Account> accounts, List<LedgerEntry> entries, DateOnly today, System.Globalization.CultureInfo culture)
    {
        var (year, month) = PeriodMath.MonthOf(today, calendar);
        if (PeriodIndex == 1)
        {
            (year, month) = PeriodMath.Previous(year, month);
        }

        var budget = await _store.GetBudgetAsync(year, month, calendar, _reportCurrency);
        HasBudget = budget?.TotalLimit is not null;
        if (budget?.TotalLimit is not { } ownLimit)
        {
            return;
        }

        // The same limit as on the budget page, including rollover (§10.3, Q-05).
        var limit = ownLimit + (await _store.GetBudgetCarryAsync(budget)).Total;
        var (from, to) = PeriodMath.MonthRange(year, month, calendar);
        var status = new BudgetStatus(limit, BudgetCalculator.NetExpense(accounts, entries, from, to, _reportCurrency, budget.AccountIds.Count > 0 ? budget.AccountIds : null));
        BudgetText = status.IsOver
            ? _translator.Format("Budget_Over", MoneyText.Format(-status.Remaining, _reportCurrency, culture))
            : _translator.Format("Home_BudgetLeft", MoneyText.Format(status.Remaining, _reportCurrency, culture), MoneyText.Format(limit, _reportCurrency, culture));
        BudgetProgress = limit > 0 ? Math.Clamp((double)status.Spent / limit, 0, 1) : status.Spent > 0 ? 1 : 0;
        BudgetColor = status.Alert switch
        {
            BudgetAlert.Exceeded => EntryPresenter.ExpenseColor,
            BudgetAlert.Near => Color.FromArgb("#F9A825"),
            _ => Color.FromArgb("#2E7D32"),
        };
    }

    // Advanced adds the estimated end-of-month balance and its lowest point (§14, FOR-08).
    private async Task LoadForecastAsync(Core.Settings.ExperienceMode mode, List<Account> accounts, List<LedgerEntry> entries, DateOnly today, System.Globalization.CultureInfo culture)
    {
        ForecastText = null;
        if (mode != Core.Settings.ExperienceMode.Advanced)
        {
            return;
        }

        var (year, month) = PeriodMath.MonthOf(today, Calendar);
        var end = PeriodMath.MonthRange(year, month, Calendar).Last;
        var forecast = Core.Forecasts.ForecastCalculator.Compute(accounts, entries, await _plans.GetSchedulesAsync(), await _plans.GetStatesAsync(), today, end)
            .FirstOrDefault(f => string.Equals(f.CurrencyCode, _reportCurrency, StringComparison.OrdinalIgnoreCase));
        if (forecast is null)
        {
            return;
        }

        ForecastText = _translator.Format("Home_Forecast", MoneyText.Format(forecast.EndBalance, _reportCurrency, culture),
            MoneyText.Format(forecast.Minimum, _reportCurrency, culture), _dates.Format(forecast.MinimumDate, DateFormatStyle.Short))
            + (forecast.IsIncomplete ? " · " + _translator.Format("Forecast_Incomplete", forecast.UnknownCount) : string.Empty);
        ForecastColor = forecast.GoesNegative ? EntryPresenter.ExpenseColor : Color.FromArgb("#1F1F1F");
    }

    [RelayCommand]
    private Task OpenForecastAsync() => Shell.Current.GoToAsync(AppShell.ForecastRoute);

    private async Task LoadPlansAsync(Dictionary<Guid, Account> accounts, CategoryLookup categories, DateOnly today)
    {
        var schedules = (await _plans.GetSchedulesAsync()).Where(s => s.State == ScheduleState.Active).ToList();
        var states = await _plans.GetStatesAsync();
        var text = new PlanText(_translator, _dates, _localization.CurrentCulture);

        var due = schedules.SelectMany(s => Occurrences.OpenUpTo(s, states, today, s.ActiveFrom ?? s.Rule.Start)).ToList();
        DueCount = due.Count;
        DueText = DueCount > 0 ? _translator.Format("Home_Due", DueCount) : null;

        Upcoming.Clear();
        foreach (var occurrence in schedules
                     .SelectMany(s => Occurrences.Between(s, states, today.AddDays(-400), today.AddDays(14), today))
                     .Where(o => o.IsOpen)
                     .OrderBy(o => o.DueDate)
                     .Take(3))
        {
            var schedule = occurrence.Schedule;
            var color = schedule.Kind == EntryKind.Transfer ? EntryPresenter.NeutralColor : categories.Color(schedule.CategoryId);
            var overdue = occurrence.Status == OccurrenceView.Overdue;
            Upcoming.Add(new PlanRow(
                schedule.Id,
                occurrence.OriginalDate,
                schedule.Name,
                text.Date(occurrence.DueDate),
                text.Amount(occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : _reportCurrency),
                schedule.Kind == EntryKind.Income ? EntryPresenter.IncomeColor : schedule.Kind == EntryKind.Expense ? EntryPresenter.ExpenseColor : EntryPresenter.NeutralColor,
                schedule.Kind == EntryKind.Transfer ? FluentIcons.Common.Symbol.ArrowSwap : Icons.Parse(schedule.Icon, categories.Icon(schedule.CategoryId)),
                color,
                color.WithAlpha(0.12f),
                overdue ? text.Status(OccurrenceView.Overdue) : occurrence.Status == OccurrenceView.Due ? text.Status(OccurrenceView.Due) : null,
                overdue ? EntryPresenter.ExpenseColor : Color.FromArgb("#8D5B00"),
                overdue ? Color.FromArgb("#FFEBEE") : Color.FromArgb("#FFF4E0")));
        }

        HasUpcoming = Upcoming.Count > 0;
    }

    // Expense by top-level category in the report currency (or the only currency in use). Small slices are combined.
    private void BuildSlices(List<Account> accounts, List<LedgerEntry> entries, CategoryLookup categories, DateOnly from, DateOnly to, System.Globalization.CultureInfo culture)
    {
        Slices.Clear();
        SliceBrushes.Clear();
        ChartNote = null;

        Guid? TopLevel(Guid? id) => categories.Get(id)?.ParentId ?? id;
        var byCategory = LedgerCalculator.ExpenseByCategory(accounts, entries, new LedgerFilter(from, to), TopLevel);
        var currencies = byCategory.Select(c => c.CurrencyCode).Distinct().ToList();
        var currency = currencies.Contains(_reportCurrency) || currencies.Count == 0 ? _reportCurrency : currencies[0];
        if (currencies.Count > 1)
        {
            ChartNote = _translator.Format("Home_ChartCurrency", currency);
        }

        var positive = byCategory.Where(c => c.CurrencyCode == currency && c.Net > 0).ToList();
        var total = positive.Sum(c => c.Net);
        if (total <= 0)
        {
            HasSlices = false;
            return;
        }

        var shown = positive.Take(positive.Count > MaxSlices ? MaxSlices - 1 : MaxSlices).ToList();
        var rest = positive.Skip(shown.Count).ToList();
        foreach (var slice in shown)
        {
            var ids = categories.All.Where(c => c.Id == slice.CategoryId || c.ParentId == slice.CategoryId).Select(c => c.Id).ToList();
            AddSlice(ids, categories.Name(slice.CategoryId), slice.Net, total, currency, categories.Color(slice.CategoryId), culture);
        }

        if (rest.Count > 0)
        {
            var ids = rest.SelectMany(r => categories.All.Where(c => c.Id == r.CategoryId || c.ParentId == r.CategoryId)).Select(c => c.Id).ToList();
            AddSlice(ids, _translator["Home_OtherCategories"], rest.Sum(r => r.Net), total, currency, Color.FromArgb("#9E9E9E"), culture);
        }

        HasSlices = Slices.Count > 0;
    }

    private void AddSlice(IReadOnlyCollection<Guid> ids, string name, long net, long total, string currency, Color color, System.Globalization.CultureInfo culture)
    {
        var brush = new SolidColorBrush(color);
        var currencyInfo = Currencies.TryGet(currency, out var known) ? known : Currencies.Euro;
        Slices.Add(new CategorySlice(
            ids,
            name,
            (double)MoneyAmount.ToDecimal(net, currencyInfo),
            MoneyText.Format(net, currency, culture),
            ((double)net / total).ToString("P0", culture),
            color,
            brush));
        SliceBrushes.Add(brush);
    }

    private (DateOnly From, DateOnly To) PeriodRange(DateOnly today)
    {
        var (year, month) = PeriodMath.MonthOf(today, Calendar);
        if (PeriodIndex == 1)
        {
            (year, month) = PeriodMath.Previous(year, month);
        }

        return PeriodMath.MonthRange(year, month, Calendar);
    }

    private const string PlansTipKey = "home.tip.plans.dismissed";
    private const string BudgetTipKey = "home.tip.budget.dismissed";

    private async Task LoadTipsAsync(int entryCount)
    {
        var ready = HasAccounts && entryCount >= 3;
        ShowPlansTip = ready && !Preferences.Default.Get(PlansTipKey, false) && (await _plans.GetSchedulesAsync()).Count == 0;
        ShowBudgetTip = ready && !ShowPlansTip && !Preferences.Default.Get(BudgetTipKey, false) && (await _store.GetBudgetsAsync()).Count == 0;
    }

    [RelayCommand]
    private Task AddPlanAsync() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute);

    [RelayCommand]
    private Task AddBudgetAsync() => Shell.Current.GoToAsync(AppShell.BudgetRoute);

    [RelayCommand]
    private void DismissTip(string tip)
    {
        Preferences.Default.Set(tip == "plans" ? PlansTipKey : BudgetTipKey, true);
        ShowPlansTip = false;
        ShowBudgetTip = false;
    }

    [RelayCommand]
    private Task OpenAccountsAsync() => Shell.Current.GoToAsync(AppShell.AccountsRoute);

    [RelayCommand]
    private Task AddEntryAsync() => Shell.Current.GoToAsync(HasAccounts ? AppShell.EntryEditorRoute : AppShell.AccountEditorRoute);

    [RelayCommand]
    private Task OpenUnreviewedAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true });

    [RelayCommand]
    private Task OpenDueAsync() => Shell.Current.GoToAsync("//plans");

    [RelayCommand]
    private Task OpenRatesAsync() => Shell.Current.GoToAsync(AppShell.RatesRoute);

    [RelayCommand]
    private Task OpenBudgetAsync() => Shell.Current.GoToAsync(AppShell.BudgetRoute);

    [RelayCommand]
    private Task OpenOccurrenceAsync(PlanRow row) =>
        Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = row.ScheduleId, ["date"] = row.OriginalDate! });

    [RelayCommand]
    private Task OpenIncomeAsync() => Drill(KindFilter.Income, null, null);

    [RelayCommand]
    private Task OpenExpenseAsync() => Drill(KindFilter.Expenses, null, null);

    [RelayCommand]
    private Task OpenSliceAsync(CategorySlice slice) => Drill(KindFilter.Expenses, slice.CategoryIds, slice.Name);

    // Opens the entries behind a number with exactly the same filter (AT-50).
    private Task Drill(KindFilter kind, IReadOnlyCollection<Guid>? categories, string? categoryName)
    {
        var query = new Dictionary<string, object> { ["period"] = PeriodIndex, ["kind"] = kind, ["inTotals"] = true };
        if (categories is not null)
        {
            query["categories"] = categories;
            query["categoryName"] = categoryName ?? string.Empty;
        }

        return Shell.Current.GoToAsync("//transactions", query);
    }
}
