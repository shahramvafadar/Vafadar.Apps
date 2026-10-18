using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Home;

/// <summary>Income, expense and result of the period in one currency.</summary>
public sealed record PeriodSummary(string IncomeText, string ExpenseText, string ResultText, Color ResultColor, string? RefundsText);

/// <summary>A category slice of the expense chart and its row in the table below it (UX-06: charts have a table).</summary>
public sealed record CategorySlice(IReadOnlyCollection<Guid> CategoryIds, string Name, double Value, string AmountText, string PercentText, Color Color, Brush Brush);

/// <summary>A quick template on Home (TX-04): one tap opens a new entry filled from it, nothing is saved yet.</summary>
public sealed record QuickTemplate(Guid Id, string Name, FluentIcons.Common.Symbol Icon, Color IconColor, string Description)
{
    /// <summary>Gets separate original activity enablement; every retained definition remains in its source review.</summary>
    public bool CanUse { get; init; }
}

/// <summary>
/// Home dashboard (UI-02, DASH-01..04). Every number uses one filter set – the period chosen at the top, the accounts
/// included in totals and their currencies – and every number can be tapped to see the entries behind it (AT-50).
/// </summary>
public sealed partial class HomeViewModel : ViewModelBase, Presentation.IThemeAware
{
    private const int MaxSlices = 6;

    private readonly ZananceStore _store;
    /// <summary>Gets the actual reviewed template navigation source, independent of financial presentation.</summary>
    public TemplateNavigationViewModel TemplateNavigation { get; }
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private string _reportCurrency = Currencies.Euro.Code;

    // The currency of the budget, forecast and chart on Home: the default account's currency or the one picked on the
    // Budget page (ZEX-P02). The valuation currency only names the converted total.
    private string _homeCurrency = Currencies.Euro.Code;
    private int _startDay = 1;
    private readonly Vafadar.Zanance.App.Profiles.ProfileService _profiles;
    private readonly Receipts.ReceiptReadingSession _receipts;
    private readonly Goals.GoalPresenter _goalPresenter;
    private readonly Holdings.HoldingText _holdingText;
    private readonly Vafadar.Backup.IBackupService _backup;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public HomeViewModel(ZananceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time, Vafadar.Zanance.App.Profiles.ProfileService profiles, Receipts.ReceiptReadingSession receipts, Goals.GoalPresenter goalPresenter, Holdings.HoldingText holdingText, Vafadar.Backup.IBackupService backup, Interaction.IAppInteraction interaction)
    {
        _backup = backup;
        TemplateNavigation = new(store, translator, interaction, LoadAsync);
        _holdingText = holdingText;
        _goalPresenter = goalPresenter;
        _store = store;
        _receipts = receipts;
        _profiles = profiles;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        PeriodNames = [translator["Period_ThisMonth"], translator["Period_LastMonth"]];
        ScopeText = string.Empty;
    }

    public IReadOnlyList<string> PeriodNames { get; }

    /// <summary>Gets complete currency totals without disconnecting every native amount row during a reload.</summary>
    public SnapshotCollection<CurrencyTotal> Balances { get; } = [];

    public ObservableCollection<PeriodSummary> Periods { get; } = [];

    public ObservableCollection<PlanRow> Upcoming { get; } = [];

    public ObservableCollection<CategorySlice> Slices { get; } = [];

    public ObservableCollection<Brush> SliceBrushes { get; } = [];

    /// <summary>Gets the complete account snapshot, rebound without rebuilding every native row.</summary>
    public SnapshotCollection<AccountItem> Accounts { get; } = [];

    /// <summary>Gets the holdings line, e.g. "Holdings: 18k gold 50.000 g · Coins 3 coins" – per type, never summed (ZEX-AS05).</summary>
    [ObservableProperty]
    public partial string? HoldingsText { get; set; }

    /// <summary>Gets the goals pinned to Home, at most two (ZEX-GO06).</summary>
    public ObservableCollection<Goals.GoalRow> PinnedGoals { get; } = [];

    [ObservableProperty]
    public partial bool HasPinnedGoals { get; set; }

    /// <summary>Gets the attention line for an overdue goal when no goal is pinned (ZEX-GO07).</summary>
    [ObservableProperty]
    public partial string? GoalAttentionText { get; set; }

    /// <summary>Gets the quick templates shown under the quick add buttons (at most six, in the user's order).</summary>
    public SnapshotCollection<QuickTemplate> QuickTemplates { get; } = [];

    [ObservableProperty]
    public partial bool HasQuickTemplates { get; set; }

    /// <summary>Gets the latest recorded entries (D-37), newest first, dated today at the latest.</summary>
    public ObservableCollection<EntryRow> RecentEntries { get; } = [];

    [ObservableProperty]
    public partial bool HasRecent { get; set; }

    [ObservableProperty]
    public partial string? ForecastEndText { get; set; }

    [ObservableProperty]
    public partial string? ForecastLowText { get; set; }

    [ObservableProperty]
    public partial Color? ForecastLowColor { get; set; }

    [ObservableProperty]
    public partial string? ForecastNote { get; set; }

    /// <summary>Gets what can be spent per day for the rest of the budget period, e.g. "≈ 38.90 EUR a day for 30 days".</summary>
    [ObservableProperty]
    public partial string? BudgetDailyText { get; set; }

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

    // The next contract date within 30 days, e.g. the last day to cancel a subscription (F2-CON-01).
    [ObservableProperty]
    public partial string? ContractText { get; set; }

    private Guid? _contractPlanId;

    // Money others still owe for reimbursable expenses (F2-TX-03).
    [ObservableProperty]
    public partial string? ReimbursementText { get; set; }

    /// <summary>Gets "Review September" when the last financial month ended and its review is not finished (ZEX-S0610).</summary>
    [ObservableProperty]
    public partial string? ReviewText { get; set; }

    /// <summary>Gets the payments of the next 30 days in the Home currency, e.g. "Next 30 days: 1,200.00 EUR · ≈ 30.00 EUR · unknown: 1" (ZEX-K04).</summary>
    [ObservableProperty]
    public partial string? Next30Text { get; set; }

    /// <summary>Gets the backup item of the data status (ZEX-S0608): shown once on Home when the last backup is old or missing.</summary>
    [ObservableProperty]
    public partial string? BackupText { get; set; }

    [ObservableProperty]
    public partial bool HasAttention { get; set; }

    /// <summary>Gets the warning that the balance may fall below zero this month, or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? LowBalanceText { get; set; }

    /// <summary>Gets the line under quick add: "to Main · EUR", or a request to choose an account (ZEX-S0103).</summary>
    [ObservableProperty]
    public partial string? QuickAddTargetText { get; set; }

    [ObservableProperty]
    public partial bool NeedsDefaultAccount { get; set; }

    /// <summary>Gets the colours of the due items row: red when something is overdue, violet otherwise (D-27).</summary>
    [ObservableProperty]
    public partial Color? DueTileText { get; set; }

    [ObservableProperty]
    public partial Color? DueTileBackground { get; set; }

    [ObservableProperty]
    public partial Color? DueTileLine { get; set; }

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
    public partial bool HasBudget { get; set; }

    [ObservableProperty]
    public partial string? IncompleteText { get; set; }

    // Getting started (ONB-04, D-41): the three steps that make Home useful, ticked off as they are done; hidden when all
    // are done or when the user hides it.
    [ObservableProperty]
    public partial bool ShowGettingStarted { get; set; }

    [ObservableProperty]
    public partial bool FirstEntryDone { get; set; }

    [ObservableProperty]
    public partial bool PlanDone { get; set; }

    [ObservableProperty]
    public partial bool BudgetDone { get; set; }

    [ObservableProperty]
    public partial string? BudgetText { get; set; }

    [ObservableProperty]
    public partial double BudgetProgress { get; set; }

    // The total in the middle of the doughnut, so the chart answers "how much in total" at a glance.
    [ObservableProperty]
    public partial string? SliceTotalText { get; set; }

    [ObservableProperty]
    public partial Color? BudgetColor { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);

    partial void OnPeriodIndexChanged(int value) => _ = Presentation.Failures.GuardAsync(LoadAsync);

    /// <summary>Loads everything shown on Home.</summary>
    /// <remarks>
    /// Appearing and a change of the period can ask at the same moment; two loads filling the same lists would mix their
    /// rows, so loads run one after the other and the later one shows the latest state.
    /// </remarks>
    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    private bool _publicationRetired;

    /// <summary>Gets the profile layout from the same validated snapshot as the original financial sections.</summary>
    internal string? CapturedHomeLayout { get; private set; }

    /// <summary>Applies original native section ordering while complete publication is still covered.</summary>
    internal event Action? PresentingSnapshot;

    /// <summary>Keeps the original dashboard and its actions covered until the complete load has finished.</summary>
    public SnapshotLoadState Loading { get; } = new();

    /// <summary>Retries the complete original snapshot without creating an account or posting money.</summary>
    [RelayCommand]
    private Task RetryLoadAsync() => Presentation.Failures.GuardAsync(LoadAsync);

    /// <summary>Invalidates only this original Home lifetime before its native controls are disconnected.</summary>
    internal void RetirePublication() => _publicationRetired = true;

    private void RequireCurrentPublication()
    {
        if (_publicationRetired) throw new OperationCanceledException("Home publication is retired.");
    }

    public async Task LoadAsync()
    {
        await _loading.WaitAsync();
        try
        {
            if (_publicationRetired) return;
            TemplateNavigation.BeginLoad();
            try { await Loading.RunAsync(LoadCoreAsync, RequireCurrentPublication); }
            catch (OperationCanceledException) when (_publicationRetired) { /* The retired native graph receives no further state publication. */ }
            catch { TemplateNavigation.FailLoad(); throw; }
        }
        finally
        {
            _loading.Release();
        }
    }

    private async Task LoadCoreAsync()
    {
        var today = Today;
        var culture = _localization.CurrentCulture;
        var snapshot = await _store.GetHomeSnapshotAsync(today, PeriodIndex);
        // A period change while the worker reads is queued separately; never project its old budget as the new period.
        while (snapshot.PeriodIndex != PeriodIndex)
            snapshot = await _store.GetHomeSnapshotAsync(today, PeriodIndex);
        var templateReview = snapshot.QuickResources;
        RequireCurrentPublication();
        var settings = templateReview.Settings ?? new ZananceSettings();
        _reportCurrency = settings.ReportCurrencyCode;
        _startDay = settings.MonthStartDay;
        var (from, to) = PeriodRange(today);

        var allAccounts = templateReview.Accounts.OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => TemplateNavigationViewModel.StoredNameOrderKey(a.Name), StringComparer.Ordinal).ToList();
        var accounts = allAccounts.Where(a => !a.IsArchived).ToList();
        _homeCurrency = BudgetCurrency.Resolve(settings, allAccounts);

        // Where quick add puts an expense (ZEX-S0103): the valid default account, or a request to choose one.
        var defaultAccount = accounts.FirstOrDefault(a => a.Id == settings.DefaultAccountId);
        NeedsDefaultAccount = !EntryAccountContract.IsValidDefault(defaultAccount);
        QuickAddTargetText = NeedsDefaultAccount
            ? _translator["Home_QuickAddChoose"]
            : _translator.Format("Home_QuickAddTarget", defaultAccount!.Name, MoneyText.UnitName(defaultAccount.CurrencyCode));
        var byId = allAccounts.ToDictionary(a => a.Id);
        var entries = snapshot.Entries;
        RequireCurrentPublication();
        // D-285: scan the complete captured ledger off the window thread; publish only its original results.
        var facts = await Task.Run(() =>
        {
            var index = new AccountEntryIndex(entries);
            var unreviewedCount = LedgerCalculator.Unreviewed(allAccounts, entries).Sum(value => value.Count);
            return new
            {
                Balances = LedgerCalculator.TotalBalances(allAccounts, entries, today),
                UnreviewedCount = unreviewedCount,
                Confirmed = unreviewedCount > 0 ? LedgerCalculator.TotalBalances(allAccounts, entries, today, confirmedOnly: true) : null,
                Totals = LedgerCalculator.Totals(allAccounts, entries, new LedgerFilter(from, to)),
                AccountBalances = accounts.ToDictionary(account => account.Id, account => LedgerCalculator.Balance(account, index.For(account.Id), today)),
                OpenReimbursementCount = EntryActions.OpenReimbursements(entries).Count,
            };
        });
        RequireCurrentPublication();
        var categoryList = templateReview.Categories.OrderBy(c => c.Kind).ThenBy(c => c.SortOrder).ToList();
        var categories = new CategoryLookup(categoryList, _translator);

        // Plans are read once for the due items, the forecast and getting started.
        var planSnapshot = snapshot.Plans;
        RequireCurrentPublication();
        var schedules = planSnapshot.Schedules;
        var states = planSnapshot.States;

        HasAccounts = accounts.Count > 0;

        // Data quality (ACC-09): an unknown opening balance makes the total incomplete; say so instead of implying a
        // complete net worth.
        var incomplete = accounts.Count(a => a.IncludeInTotals && !a.OpeningBalanceKnown);
        IncompleteText = incomplete > 0 ? _translator.Format("Home_OpeningUnknown", incomplete) : null;
        HasEntries = entries.Count > 0;
        // A month with its own start day (pay cycle) shows its exact range (DASH-01).
        var periodText = _startDay > 1
            ? $"{_dates.Format(from, DateFormatStyle.Short)} – {_dates.Format(to, DateFormatStyle.Short)}"
            : _dates.Format(from, DateFormatStyle.MonthYear);
        ScopeText = _translator.Format("Home_Scope", periodText, _translator["Home_AccountsInTotals"], _homeCurrency);
        if (_profiles.HasSeveral)
        {
            // With several local profiles the open one is named first (§3).
            ScopeText = $"{_profiles.NameOf(_profiles.Current)} · {ScopeText}";
        }

        // Recorded balance (FIN-13) per currency; totals always follow the accounts included in totals.
        var balances = facts.Balances;
        // D-267: clearing first disconnects ContentView handlers while Windows reopens covered Home pages.
        // Publish the complete snapshot once so BindableLayout rebinds existing amount rows.
        Balances.ReplaceAll(balances.Select(value => new CurrencyTotal(value.Key, MoneyText.Format(value.Value, value.Key, culture))));

        // With several currencies, a combined total only when every rate exists (FX-02, FX-05).
        CombinedText = null;
        CombinedIncomplete = false;
        if (balances.Count > 1 && settings.ValuationCurrencyEnabled)
        {
            // Secondary, with the date of the oldest rate and a reminder when a rate may be outdated (ZEX-S0106).
            var (monthYear, monthNumber) = PeriodMath.MonthOf(today, settings.BudgetCalendar, _startDay);
            var freshness = new RateFreshness(settings.RateFreshnessDays, PeriodMath.MonthRange(monthYear, monthNumber, settings.BudgetCalendar, _startDay).First);
            var combined = new RateTable(snapshot.Rates).Combine(balances, _reportCurrency, today, freshness);
            RequireCurrentPublication();
            CombinedIncomplete = !combined.IsComplete || combined.IsOutdated;
            var rateDate = combined.OldestRateDate is { } oldest ? _dates.Format(oldest, DateFormatStyle.Short) : "-";
            CombinedText = !combined.IsComplete
                ? _translator.Format("Home_CombinedIncomplete", string.Join(", ", combined.MissingCurrencies))
                : _translator.Format(combined.IsOutdated ? "Home_CombinedOutdated" : "Home_Combined", MoneyText.Format(combined.Total!.Value, _reportCurrency, culture), rateDate)
                    + (combined.HasEstimate ? " · " + _translator["Home_RateEstimate"] : string.Empty);
        }

        UnreviewedCount = facts.UnreviewedCount;

        // "Posted balance – includes unreviewed entries" with the confirmed-only value next to it (FIN-12).
        ConfirmedBalanceText = UnreviewedCount > 0
            ? _translator.Format("Home_ConfirmedOnly", UnreviewedCount, string.Join("  ", (facts.Confirmed ?? throw new InvalidOperationException("Confirmed Home balances are missing."))
                .Select(b => MoneyText.Format(b.Value, b.Key, culture))))
            : null;
        UnreviewedText = UnreviewedCount > 0 ? _translator.Format("Home_Unreviewed", UnreviewedCount) : null;

        // Income and expense of the period.
        Periods.Clear();
        foreach (var totals in facts.Totals)
        {
            var refunds = totals.Refunds > 0 ? _translator.Format("Home_Refunds", MoneyText.Format(totals.Refunds, totals.CurrencyCode, culture)) : null;
            Periods.Add(new PeriodSummary(
                MoneyText.Format(totals.NetIncome, totals.CurrencyCode, culture),
                MoneyText.Format(totals.NetExpense, totals.CurrencyCode, culture),
                MoneyText.Format(totals.Result, totals.CurrencyCode, culture, showPlus: true),
                totals.Result < 0 ? EntryPresenter.DangerColor : EntryPresenter.IncomeColor,
                refunds));
        }

        await LoadBudgetAsync(snapshot, settings.BudgetCalendar, allAccounts, entries, categoryList, today, culture);
        RequireCurrentPublication();
        await LoadPlansAsync(byId, categories, schedules, states, today, planSnapshot.Work);
        LoadGettingStarted(entries.Count, schedules.Count, snapshot.Budgets.Count);
        RequireCurrentPublication();
        await LoadForecastAsync(settings.Shows(Feature.ForecastDetails), allAccounts, entries, schedules, states, today, culture);
        await BuildSlicesAsync(allAccounts, entries, categories, from, to, culture);

        var accountRows = new List<AccountItem>(accounts.Count);
        foreach (var account in accounts)
        {
            var balance = facts.AccountBalances[account.Id];
            accountRows.Add(new AccountItem(account.Id, account.Name, _translator[$"AccountType_{account.Type}"],
                Icons.Parse(account.Icon, Icons.For(account.Type)), MoneyText.Format(balance, account.CurrencyCode, culture), balance < 0, !account.IncludeInTotals, !account.OpeningBalanceKnown) { Type = account.Type });
        }
        // D-92: keep the existing native rows and publish the complete, freshly formatted snapshot once.
        Accounts.ReplaceAll(accountRows);

        // The last entries show at a glance what was recorded and that a quick add worked.
        var presenter = new EntryPresenter(byId, categories, _translator, culture);
        RecentEntries.Clear();
        foreach (var entry in entries.Where(e => e.Date <= today).OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).Take(3))
        {
            var row = presenter.Row(entry);
            var day = entry.Date == today ? _translator["Common_Today"]
                : entry.Date == today.AddDays(-1) ? _translator["Home_Yesterday"]
                : _dates.Format(entry.Date, DateFormatStyle.DayMonth);
            RecentEntries.Add(row with { Subtitle = $"{day} · {row.Subtitle}" });
        }

        HasRecent = RecentEntries.Count > 0;

        ReimbursementText = facts.OpenReimbursementCount == 0 ? null : _translator.Format("Home_Reimbursements", facts.OpenReimbursementCount);
        LoadDataAttention(settings, allAccounts, entries, today);
        LoadGoals(snapshot, today);
        RequireCurrentPublication();
        LoadHoldings(snapshot, today);
        RequireCurrentPublication();
        HasAttention = UnreviewedCount > 0 || DueCount > 0 || ContractText is not null || ReimbursementText is not null || LowBalanceText is not null || GoalAttentionText is not null || ReviewText is not null || BackupText is not null;
        await _store.ValidateHomeSnapshotAsync(snapshot);
        RequireCurrentPublication();
        if (snapshot.PeriodIndex != PeriodIndex) { await LoadCoreAsync(); return; }
        TemplateNavigation.Publish(templateReview);
        // Publish complete rows together: clearing first makes MAUI disconnect unchanged native template controls (D-353).
        var quickTemplates = TemplateNavigation.Items.Select(template =>
        {
            var transfer = template.Kind == EntryKind.Transfer;
            return new QuickTemplate(template.Id, template.Name,
                transfer ? FluentIcons.Common.Symbol.ArrowSwap : Icons.Parse(template.Icon, categories.Icon(template.CategoryId)),
                transfer ? Palette.TransferText : categories.Color(template.CategoryId),
                _translator.Format("Home_TemplateDescription", template.Name)) { CanUse = templateReview.Work.CanApplyTemplate(template.Id) };
        }).ToArray();
        QuickTemplates.ReplaceAll(quickTemplates);
        HasQuickTemplates = QuickTemplates.Count > 0;
        CapturedHomeLayout = settings.HomeLayout;
        PresentingSnapshot?.Invoke();
    }

    // Remaining overall budget of the month, only when a budget exists (a missing budget is not zero, BUD-01).
    private async Task LoadBudgetAsync(HomeReadSnapshot snapshot, PeriodCalendar calendar, List<Account> accounts, List<LedgerEntry> entries, List<Core.Categories.Category> categories, DateOnly today, System.Globalization.CultureInfo culture)
    {
        var (year, month) = PeriodMath.MonthOf(today, calendar, _startDay);
        if (PeriodIndex == 1)
        {
            (year, month) = PeriodMath.Previous(year, month);
        }

        var budget = snapshot.MonthBudget;
        RequireCurrentPublication();
        BudgetDailyText = null;
        HasBudget = budget?.TotalLimit is not null;
        if (budget?.TotalLimit is not { } ownLimit)
        {
            return;
        }

        // The same limit as on the budget page, including rollover (§10.3, Q-05).
        var limit = ownLimit + snapshot.Carry.Total;
        RequireCurrentPublication();
        var (from, to) = PeriodMath.MonthRange(year, month, calendar, _startDay);
        var spent = await Task.Run(() => FlexCalculator.SpentAgainstLimit(budget, accounts, entries, categories, from, to));
        RequireCurrentPublication();
        var status = new BudgetStatus(limit, spent);
        BudgetText = status.IsOver
            ? _translator.Format("Budget_Over", MoneyText.Format(-status.Remaining, _homeCurrency, culture))
            : _translator.Format("Home_BudgetLeft", MoneyText.Format(status.Remaining, _homeCurrency, culture), MoneyText.Format(limit, _homeCurrency, culture));
        BudgetProgress = limit > 0 ? Math.Clamp((double)status.Spent / limit, 0, 1) : status.Spent > 0 ? 1 : 0;

        // What is left per day of the running period answers "can I still spend today?" at a glance.
        var daysLeft = to.DayNumber - today.DayNumber + 1;
        if (PeriodIndex == 0 && !status.IsOver && status.Remaining > 0 && daysLeft > 0)
        {
            var perDay = MoneyText.Format(status.Remaining / daysLeft, _homeCurrency, culture);
            BudgetDailyText = daysLeft == 1
                ? _translator.Format("Home_BudgetLastDay", perDay)
                : _translator.Format("Home_BudgetPerDay", perDay, daysLeft);
        }

        BudgetColor = status.Alert switch
        {
            BudgetAlert.Exceeded => EntryPresenter.DangerColor,
            BudgetAlert.Near => Palette.NearLimit,
            _ => Palette.IncomeText,
        };
    }

    // Advanced adds the estimated end-of-month balance and its lowest point (§14, FOR-08); a balance that may fall below
    // zero is a warning in "Needs attention" in both modes (ZEX-P19).
    private async Task LoadForecastAsync(bool showDetails, List<Account> accounts, List<LedgerEntry> entries, List<Schedule> schedules, List<OccurrenceState> states, DateOnly today, System.Globalization.CultureInfo culture)
    {
        ForecastEndText = null;
        LowBalanceText = null;

        var (year, month) = PeriodMath.MonthOf(today, Calendar, _startDay);
        var end = PeriodMath.MonthRange(year, month, Calendar, _startDay).Last;
        var currency = _homeCurrency;
        var forecast = await Task.Run(() => Core.Forecasts.ForecastCalculator.Compute(accounts, entries, schedules, states, today, end)
            .FirstOrDefault(f => string.Equals(f.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)));
        RequireCurrentPublication();
        if (forecast is null)
        {
            return;
        }

        if (forecast.GoesNegative)
        {
            LowBalanceText = _translator.Format("Home_BalanceBelowZero", _dates.Format(forecast.MinimumDate, DateFormatStyle.DayMonth));
        }

        if (!showDetails)
        {
            return;
        }

        // The end balance and the lowest point are two facts; only a balance below zero is a problem (red, D-27).
        ForecastEndText = _translator.Format("Home_ForecastEnd", MoneyText.Format(forecast.EndBalance, _homeCurrency, culture));
        ForecastLowText = _translator.Format("Home_ForecastLow", MoneyText.Format(forecast.Minimum, _homeCurrency, culture), _dates.Format(forecast.MinimumDate, DateFormatStyle.DayMonth));
        ForecastLowColor = forecast.GoesNegative ? EntryPresenter.DangerColor : Palette.SecondaryText;
        ForecastNote = forecast.IsIncomplete ? _translator.Format("Forecast_Incomplete", forecast.UnknownCount) : null;
    }

    // The month-end review and an old backup (K14 at Home: only the backup item, once).
    private void LoadDataAttention(ZananceSettings settings, List<Account> accounts, List<LedgerEntry> entries, DateOnly today)
    {
        var firstData = accounts.Count == 0 ? (DateOnly?)null : accounts.Min(a => a.OpeningDate);
        var review = Core.Reports.PeriodReview.Due(settings.ReviewProgress, today, Calendar, _startDay, firstData);
        ReviewText = review is null ? null : _translator.Format("Home_Review", _dates.Format(PeriodMath.MonthRange(review.Year, review.Month, Calendar, _startDay).First, DateFormatStyle.MonthYear));
        var last = _backup.LastBackupAt is { } at ? DateOnly.FromDateTime(at.ToLocalTime().DateTime) : (DateOnly?)null;
        BackupText = entries.Count == 0 || (last is { } day && today.DayNumber - day.DayNumber <= Core.Reports.DataStatus.BackupDays)
            ? null
            : last is { } date ? _translator.Format("Issue_BackupOld", _dates.Format(date, DateFormatStyle.Short)) : _translator["Issue_NoBackup"];
    }

    [RelayCommand]
    private Task OpenReviewAsync() => Shell.Current.GoToAsync(AppShell.ReviewRoute);

    [RelayCommand]
    private Task OpenBackupAsync() => Shell.Current.GoToAsync(AppShell.BackupRoute);

    [RelayCommand]
    private Task OpenForecastAsync() => Shell.Current.GoToAsync(AppShell.ForecastRoute);

    // Quantities per asset type, never added across types or into money (ZEX-AS05); at most three, then "+N".
    private void LoadHoldings(HomeReadSnapshot snapshot, DateOnly today)
    {
        var types = snapshot.AssetTypes.Where(t => !t.IsArchived).ToDictionary(t => t.Id);
        RequireCurrentPublication();
        if (types.Count == 0)
        {
            HoldingsText = null;
            return;
        }

        var events = new AssetEventIndex(snapshot.AssetEvents);
        RequireCurrentPublication();
        var held = types.Values
            .Select(t => (Type: t, Quantity: HoldingsLedger.Quantity(events.For(t.Id), t.Id, today)))
            .Where(h => h.Quantity > 0)
            .Select(h => $"{h.Type.Name} {_holdingText.Quantity(h.Quantity, h.Type)}")
            .ToList();
        HoldingsText = held.Count == 0
            ? null
            : _translator.Format("Home_Holdings", string.Join(" · ", held.Take(3)) + (held.Count > 3 ? $" · +{held.Count - 3}" : string.Empty));
    }

    [RelayCommand]
    private Task OpenHoldingsAsync() => Shell.Current.GoToAsync(AppShell.HoldingsRoute);

    // Pinned goals with progress and the estimate of the plan, only when valid (ZEX-GO06, GO07). Paused goals leave Home.
    private void LoadGoals(HomeReadSnapshot snapshot, DateOnly today)
    {
        var view = Goals.GoalHomeProjection.Project(snapshot, _goalPresenter, _translator, today);
        RequireCurrentPublication();
        PinnedGoals.Clear();
        foreach (var row in view.Pinned) PinnedGoals.Add(row);
        HasPinnedGoals = PinnedGoals.Count > 0;
        GoalAttentionText = view.Attention;
    }

    [RelayCommand]
    private Task OpenGoalsAsync() => Shell.Current.GoToAsync(AppShell.GoalsRoute);

    [RelayCommand]
    private Task OpenGoalAsync(Goals.GoalRow row) => Shell.Current.GoToAsync(AppShell.GoalDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private Task OpenReportsAsync() => Shell.Current.GoToAsync(AppShell.ReportsRoute);

    private async Task LoadPlansAsync(Dictionary<Guid, Account> accounts, CategoryLookup categories, List<Schedule> allSchedules, List<OccurrenceState> states,
        DateOnly today, Core.Commerce.PlanWorkPolicy work)
    {
        var schedules = PlanActions.InForce(allSchedules);
        var text = new PlanText(_translator, _dates, _localization);

        var currency = _homeCurrency;
        var computation = await Task.Run(() => new
        {
            Due = schedules.SelectMany(s => Occurrences.OpenUpTo(s, states, today, s.ActiveFrom ?? s.Rule.Start))
                .Where(o => work.AllowsOccurrence(o, today)).ToList(),
            Upcoming = schedules.SelectMany(s => Occurrences.Between(s, states, today.AddDays(-400), today.AddDays(14), today))
                .Where(o => o.IsOpen && work.AllowsOccurrence(o, today)).OrderBy(o => o.DueDate).Take(3).ToList(),
            Contracts = Core.Reminders.ContractReminderPlanner.Upcoming(allSchedules, today),
            Next30 = Core.Reports.KpiCatalog.Commitments(accounts.Values, schedules, states, today)
                .FirstOrDefault(c => string.Equals(c.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)),
        });
        RequireCurrentPublication();
        var due = computation.Due;
        DueCount = due.Count;
        DueText = DueCount > 0 ? _translator.Format("Home_Due", DueCount) : null;
        var dueLook = due.Any(o => o.Status == OccurrenceView.Overdue) ? PlanLook.Danger : PlanLook.Future;
        (DueTileText, DueTileBackground, DueTileLine) = dueLook;

        var contracts = computation.Contracts;
        _contractPlanId = contracts.Count > 0 ? contracts[0].Schedule.Id : null;
        ContractText = contracts.Count == 0 ? null
            : _translator.Format(contracts[0].Kind == Core.Reminders.ContractDateKind.Cancellation ? "Home_CancelBy" : "Home_ReviewOn",
                contracts[0].Schedule.Name, _dates.Format(contracts[0].Date, DateFormatStyle.Long))
              + (contracts.Count > 1 ? " " + _translator.Format("Home_MoreContracts", contracts.Count - 1) : string.Empty);

        Upcoming.Clear();
        foreach (var occurrence in computation.Upcoming)
        {
            var schedule = occurrence.Schedule;
            var color = schedule.Kind == EntryKind.Transfer ? EntryPresenter.NeutralColor : categories.Color(schedule.CategoryId);
            var overdue = occurrence.Status == OccurrenceView.Overdue;
            var look = overdue ? PlanLook.Danger : PlanLook.Future;
            var days = occurrence.DueDate.DayNumber - today.DayNumber;
            Upcoming.Add(new PlanRow(
                schedule.Id,
                occurrence.OriginalDate,
                schedule.Name,
                accounts.TryGetValue(schedule.AccountId, out var planAccount) ? planAccount.Name : string.Empty,
                text.Amount(occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : _reportCurrency),
                schedule.Kind == EntryKind.Income ? EntryPresenter.IncomeColor : schedule.Kind == EntryKind.Expense ? EntryPresenter.ExpenseColor : EntryPresenter.NeutralColor,
                schedule.Kind == EntryKind.Transfer ? FluentIcons.Common.Symbol.ArrowSwap : Icons.Parse(schedule.Icon, categories.Icon(schedule.CategoryId)),
                color,
                color.WithAlpha(0.12f),
                overdue ? PlanText.OverdueDays(_translator, -days)
                    : days == 0 ? text.Status(OccurrenceView.Due)
                    : days == 1 ? _translator["Plan_Tomorrow"]
                    : _translator.Format("Plan_InDays", days),
                look.Text,
                look.Background) { BadgeStroke = look.Line }
                .WithDate(text, occurrence.DueDate, overdue, schedule.Kind));
        }

        HasUpcoming = Upcoming.Count > 0;

        // K04: what the next 30 days cost, known, estimated and unknown apart – the same number as the reports.
        var culture = _localization.CurrentCulture;
        var next30 = computation.Next30;
        Next30Text = next30 is null ? null : _translator.Format("Home_Next30", string.Join(" · ", new[]
        {
            MoneyText.Format(next30.Fixed, next30.CurrencyCode, culture),
            next30.Estimated > 0 ? "≈ " + MoneyText.Format(next30.Estimated, next30.CurrencyCode, culture) : null,
            next30.UnknownCount > 0 ? _translator.Format("Report_UnknownCount", next30.UnknownCount) : null,
        }.Where(s => s is not null)));
    }

    // Expense by top-level category in the Home currency (or the only currency in use). Small slices are combined. The
    // chart shows gross expense like the reports, so Home and reports show the same numbers (ZEX-S0104).
    private async Task BuildSlicesAsync(List<Account> accounts, List<LedgerEntry> entries, CategoryLookup categories, DateOnly from, DateOnly to, System.Globalization.CultureInfo culture)
    {
        Slices.Clear();
        SliceBrushes.Clear();
        ChartNote = null;

        Guid? TopLevel(Guid? id) => categories.Get(id)?.ParentId ?? id;
        var byCategory = await Task.Run(() => LedgerCalculator.ExpenseByCategory(accounts, entries, new LedgerFilter(from, to), TopLevel));
        RequireCurrentPublication();
        var currencies = byCategory.Select(c => c.CurrencyCode).Distinct().ToList();
        var currency = currencies.Contains(_homeCurrency) || currencies.Count == 0 ? _homeCurrency : currencies[0];
        if (currencies.Count > 1)
        {
            ChartNote = _translator.Format("Home_ChartCurrency", currency);
        }

        var positive = byCategory.Where(c => c.CurrencyCode == currency && c.GrossExpense > 0).OrderByDescending(c => c.GrossExpense).ToList();
        var total = positive.Sum(c => c.GrossExpense);
        SliceTotalText = total > 0 ? MoneyText.Format(total, currency, culture) : null;
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
            AddSlice(ids, categories.Name(slice.CategoryId), slice.GrossExpense, total, currency, categories.Color(slice.CategoryId), culture);
        }

        if (rest.Count > 0)
        {
            var ids = rest.SelectMany(r => categories.All.Where(c => c.Id == r.CategoryId || c.ParentId == r.CategoryId)).Select(c => c.Id).ToList();
            AddSlice(ids, _translator["Home_OtherCategories"], rest.Sum(r => r.GrossExpense), total, currency, Palette.Muted, culture);
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
            (double)MoneyText.ToDecimal(net, currencyInfo),
            MoneyText.Format(net, currency, culture),
            ((double)net / total).ToString("P0", culture),
            color,
            brush));
        SliceBrushes.Add(brush);
    }

    private (DateOnly From, DateOnly To) PeriodRange(DateOnly today)
    {
        var (year, month) = PeriodMath.MonthOf(today, Calendar, _startDay);
        if (PeriodIndex == 1)
        {
            (year, month) = PeriodMath.Previous(year, month);
        }

        return PeriodMath.MonthRange(year, month, Calendar, _startDay);
    }

    private const string GettingStartedKey = "home.getting_started.hidden";

    private void LoadGettingStarted(int entryCount, int planCount, int budgetCount)
    {
        FirstEntryDone = entryCount > 0;
        PlanDone = planCount > 0;
        BudgetDone = budgetCount > 0;
        ShowGettingStarted = HasAccounts && !Preferences.Default.Get(GettingStartedKey, false) && !(FirstEntryDone && PlanDone && BudgetDone);
    }

    [RelayCommand]
    private void HideGettingStarted()
    {
        Preferences.Default.Set(GettingStartedKey, true);
        ShowGettingStarted = false;
    }

    [RelayCommand]
    private Task AddPlanAsync() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute);

    [RelayCommand]
    private Task AddBudgetAsync() => Shell.Current.GoToAsync(AppShell.BudgetRoute);

    [RelayCommand]
    private Task OpenAccountsAsync() => Shell.Current.GoToAsync(AppShell.AccountsRoute);

    [RelayCommand]
    private Task AddEntryAsync() => Shell.Current.GoToAsync(HasAccounts ? AppShell.EntryEditorRoute : AppShell.AccountEditorRoute);

    // Quick add on Home (UX-04): the editor opens with the kind already chosen, so an expense is amount, category, Save.
    [RelayCommand]
    private Task AddKindAsync(string kind) => HasAccounts
        ? Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["kind"] = kind })
        : Shell.Current.GoToAsync(AppShell.AccountEditorRoute);

    [RelayCommand]
    private Task UseTemplateAsync(QuickTemplate template) => TemplateNavigation.OpenAsync(template.Id);

    [RelayCommand]
    private Task OpenEntryAsync(EntryRow row) =>
        Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private Task OpenTransactionsAsync() => Shell.Current.GoToAsync("//transactions");

    // A receipt photo or PDF read on the device (D-31, D-33) opens a new expense with the values found; the file is
    // attached when the entry is saved, and nothing is stored before (D-37). Phones can take the photo right away (D-38).
    [RelayCommand]
    private async Task ReadReceiptAsync()
    {
        if (!HasAccounts)
        {
            await Shell.Current.GoToAsync(AppShell.AccountEditorRoute);
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try { await _receipts.PickAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task OpenUnreviewedAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true });

    [RelayCommand]
    private Task OpenReimbursementsAsync() => Shell.Current.GoToAsync(AppShell.ReimbursementsRoute);

    [RelayCommand]
    private Task OpenContractAsync() => _contractPlanId is { } id
        ? Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = id })
        : Task.CompletedTask;

    [RelayCommand]
    private Task OpenDueAsync() => Shell.Current.GoToAsync("//plans");

    [RelayCommand]
    private Task OpenRatesAsync() => Shell.Current.GoToAsync(AppShell.RatesRoute);

    [RelayCommand]
    private Task CustomizeAsync() => Shell.Current.GoToAsync(AppShell.HomeLayoutRoute);

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
