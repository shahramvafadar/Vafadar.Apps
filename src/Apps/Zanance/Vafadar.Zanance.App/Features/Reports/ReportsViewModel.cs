using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Backup;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Core.Commerce;
using System.Text.Json;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A category row of the expense table: gross, refunds and net (REP-02).</summary>
public sealed record CategoryReportRow(IReadOnlyCollection<Guid> CategoryIds, string Name, Color Color, string GrossText, string RefundsText, string NetText, Color NetColor, string DetailText);

/// <summary>An account with its movement lines.</summary>
public sealed record AccountReport(string Name, IReadOnlyList<AmountLine> Lines)
{
    /// <summary>Gets the original account currency for its independently labelled PDF scope.</summary>
    public string CurrencyCode { get; init; } = string.Empty;
}

/// <summary>One month of the trend chart; amounts are major units for the chart axis.</summary>
public sealed record TrendPoint(string Label, double Income, double Expense, string IncomeText, string ExpenseText, string ResultText, Color ResultColor);

/// <summary>Spending of one tag.</summary>
public sealed record TagReportRow(string Tag, string Name, string AmountText, string Details);

/// <summary>A plan with planned and recorded amounts.</summary>
public sealed record PlanReportRow(string Name, string PlannedText, string ActualText, string Details);

/// <summary>One item of the data status with the screen that fixes it (ZEX-K14).</summary>
public sealed record IssueRow(DataIssueKind Kind, string Text, string? ActionText, IReadOnlyList<Guid>? Ids);

/// <summary>
/// The reports hub (ZEX-UI13): report packages under one scope bar – period overview (R1), commitments (R2), goals
/// and capacity (R3), holdings and net worth (R4) and data status (R6). Every number uses the scope (period, accounts,
/// currency, confirmed only), names it, explains itself with "?" and lists the entries behind it with the same scope,
/// so the list shows the same total (ZEX-S0601, AT36). Each package ends with its data status, never a score.
/// </summary>
public sealed partial class ReportsViewModel : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private const int TrendMonths = 6;
    private readonly ReportWorkStore _reportStore;
    private readonly ReportExportSession _export;
    private readonly ReportWorkspace _workspace;
    private bool _active = true, _rootRetired;
    private Func<bool>? _rootOwner;
    private PeriodCalendar? _scopeCalendar;
    private bool OwnsPublication => !_detached && _active && !_rootRetired && (_rootOwner?.Invoke() ?? true);
    /// <summary>Gets current original source/frame command availability independently of stale visual rows.</summary>
    public bool CanRunCommands => OwnsPublication && !IsBusy && _workspace.CanInteract;
    /// <summary>Gets explicit current-page Retry availability, including a covered source failure.</summary>
    public bool CanReload => OwnsPublication && !IsBusy && !ReportLoading;
    /// <summary>Binds original root ownership through a weak native-root check.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); _workspace.BindRootOwner(owner); UpdateWorkspaceState(); }
    /// <summary>Retires original parent reads and row/native effects when the actual page or tab departs.</summary>
    public void SetActive(bool active)
    {
        if (_active == active) return; _active = active;
        _localization.Changed -= OnReportDisplayChanged; if (active && !_rootRetired) _localization.Changed += OnReportDisplayChanged;
        _workspace.SetActive(active); UpdateWorkspaceState();
    }
    /// <summary>Permanently retires the old root and source-bound child handoffs.</summary>
    internal void RetireRoot() { _rootRetired = true; SetActive(false); _workspace.RetireRoot(); UpdateWorkspaceState(); }
    private void OnReportDisplayChanged(object? sender, EventArgs args) { if (OwnsPublication) Reload(); }
    private void UpdateWorkspaceState()
    {
        if (_detached) return;
        ReportLoading = OwnsPublication && _workspace.Loading.IsLoading; ReportReady = OwnsPublication && _workspace.CanInteract;
        if (_workspace.Failure is { } failure) ReportFeedback = CommercialFailureMessage.OrFallback(failure, _translator, "Report_ReadFailed");
        OnPropertyChanged(nameof(CanRunCommands)); OnPropertyChanged(nameof(CanReload)); RefreshReportsCommand.NotifyCanExecuteChanged();
    }
    private bool RowCurrent<T>(IEnumerable<T> rows, T? row, int? package = null) where T : class => row is not null
        && (package is null || PackageIndex == package) && rows.Any(item => ReferenceEquals(item, row));
    private ReportReadReview? _review;
    private ReportInputs Inputs => _review?.Inputs ?? throw new InvalidOperationException("No report review is available.");
    /// <summary>Gets whether all original sources and the selected projection are ready for interaction.</summary>
    [ObservableProperty]
    public partial bool ReportReady { get; set; }
    /// <summary>Gets translated read/retry or selected-package access feedback without changing selection.</summary>
    [ObservableProperty]
    public partial string? ReportFeedback { get; set; }
    /// <summary>Gets paid analysis availability separately from Simple/Advanced presentation.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCommitments), nameof(ShowGoals), nameof(ShowWealth), nameof(ShowHistory))]
    public partial bool CanUseAdvancedReports { get; set; }
    /// <summary>Gets whether the covered original report read is still active.</summary>
    [ObservableProperty]
    public partial bool ReportLoading { get; set; }
    private bool _detached;


    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly GoalStore _goals;
    private readonly HoldingStore _holdings;
    private readonly IBackupService _backup;
    private readonly Goals.GoalPresenter _goalPresenter;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly Security.AppLockService _lock;
    private int _year;
    private int _month;
    private int _startDay = 1;
    private DateOnly _from;
    private DateOnly _to;
    private string _currency = Currencies.Euro.Code;
    private ZananceSettings _settings = new();
    private List<Account> _accounts = [];
    // A load is running; a scope change meanwhile asks for one more load after it (instead of being lost).
    private bool _loading;
    private bool _reloadRequested;

    // The page sets scope values itself (currency list, account, the packages of the PDF): those changes reload nothing.
    private bool _applying;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ReportsViewModel(
        ZananceStore store,
        PlanStore plans,
        GoalStore goals,
        HoldingStore holdings,
        IBackupService backup,
        Goals.GoalPresenter goalPresenter,
        Translator translator,
        IDateFormatter dates,
        ILocalizationService localization,
        TimeProvider time,
        Security.AppLockService appLock, ReportWorkStore reportStore)
    {
        _lock = appLock; _reportStore = reportStore; _export = new(reportStore);
        _store = store;
        _plans = plans;
        _goals = goals;
        _holdings = holdings;
        _backup = backup;
        _goalPresenter = goalPresenter;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time; _workspace = new(reportStore);
        _workspace.PropertyChanged += (_, _) => UpdateWorkspaceState();
        PropertyChanged += (_, args) => { if (args.PropertyName == nameof(IsBusy)) UpdateWorkspaceState(); };
        localization.Changed += OnReportDisplayChanged;
        PeriodKindNames = [translator["Report_Month"], translator["Report_Year"]];
        PackageNames = [translator["Report_R1"], translator["Report_R2"], translator["Report_R3"], translator["Report_R4"], translator["Report_R5"], translator["Report_R6"]];
        AccountScopeNames = [translator["Report_ScopeInTotals"], translator["Report_ScopeUsable"], translator["Report_ScopeOneAccount"]];
        PeriodText = string.Empty;
        ScopeText = string.Empty;
    }

    [ObservableProperty] public partial IReadOnlyList<string> PeriodKindNames { get; private set; } = [];

    [ObservableProperty] public partial IReadOnlyList<string> PackageNames { get; private set; } = [];

    [ObservableProperty] public partial IReadOnlyList<string> AccountScopeNames { get; private set; } = [];

    public ObservableCollection<IssueRow> Issues { get; } = [];

    [ObservableProperty]
    public partial int PeriodKind { get; set; }

    /// <summary>Gets or sets the package: 0 R1, 1 R2, 2 R3, 3 R4, 4 R5, 5 R6.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOverview), nameof(ShowCommitments), nameof(ShowGoals), nameof(ShowWealth), nameof(ShowHistory), nameof(ShowStatus), nameof(ShowEmpty), nameof(ShowPeriod))]
    public partial int PackageIndex { get; set; }

    [ObservableProperty]
    public partial string PeriodText { get; set; }

    /// <summary>Gets the scope the numbers are computed with, e.g. "October · EUR · Accounts in totals" (ZEX-S0601).</summary>
    [ObservableProperty]
    public partial string ScopeText { get; set; }

    [ObservableProperty]
    public partial bool IsScopeOpen { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> CurrencyNames { get; set; } = [];

    [ObservableProperty]
    public partial int CurrencyIndex { get; set; }

    [ObservableProperty]
    public partial bool HasCurrencyChoice { get; set; }

    [ObservableProperty]
    public partial int AccountScopeIndex { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountChoice> ScopeAccounts { get; set; } = [];

    [ObservableProperty]
    public partial AccountChoice? ScopeAccount { get; set; }

    [ObservableProperty]
    public partial bool IsOneAccount { get; set; }

    /// <summary>Gets or sets a value indicating whether unreviewed entries are left out (default: included, with their count).</summary>
    [ObservableProperty]
    public partial bool ConfirmedOnly { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    /// <summary>Gets the data status line of the package, e.g. "Data status: 3 unreviewed · USD rate from 12 Aug".</summary>
    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial bool HasIssues { get; set; }

    [ObservableProperty]
    public partial Symbol PreviousIcon { get; set; }

    [ObservableProperty]
    public partial Symbol NextIcon { get; set; }

    // Without any entry the period overview has nothing to show; one empty state replaces it. The other packages still
    // show plans, goals, balances and the data status.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmpty), nameof(ShowOverview))]
    public partial bool IsEmpty { get; set; }

    public bool ShowEmpty => IsEmpty && PackageIndex == 0;

    public bool ShowOverview => PackageIndex == 0 && !IsEmpty;

    public bool ShowCommitments => PackageIndex == 1 && CanUseAdvancedReports;

    public bool ShowGoals => PackageIndex == 2 && CanUseAdvancedReports;

    public bool ShowWealth => PackageIndex == 3 && CanUseAdvancedReports;

    /// <summary>Gets a value indicating whether the wealth change package R5 is shown (Advanced content, ZEX-E08).</summary>
    public bool ShowHistory => PackageIndex == 4 && CanUseAdvancedReports;

    public bool ShowStatus => PackageIndex == 5;

    /// <summary>Gets a value indicating whether the package has a period (commitments look ahead from today; wealth is today).</summary>
    public bool ShowPeriod => PackageIndex is 0 or 5;

    private DateOnly Today => _review?.Day ?? DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private CultureInfo Culture => _localization.CurrentCulture;

    private PeriodCalendar Calendar => Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);

    partial void OnPeriodKindChanged(int value) => Reload();

    partial void OnPackageIndexChanged(int value) => Reload();

    partial void OnCurrencyIndexChanged(int value) => Reload();

    partial void OnConfirmedOnlyChanged(bool value) => Reload();

    partial void OnAccountScopeIndexChanged(int value)
    {
        IsOneAccount = value == 2;
        Reload();
    }

    partial void OnScopeAccountChanged(AccountChoice? value) => Reload();

    private void Reload()
    {
        if (_applying || _detached || !OwnsPublication)
        {
            return;
        }

        if (_loading)
        {
            _reloadRequested = true;
            return;
        }

        _ = Presentation.Failures.GuardAsync(LoadAsync);
    }

    /// <summary>Query: <c>report</c> (index of the package to show).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("report", out var report) && report is int index)
        {
            PackageIndex = Math.Clamp(index, 0, PackageNames.Count - 1);
        }
    }

    [RelayCommand]
    private void ToggleScope() { if (CanRunCommands) IsScopeOpen = !IsScopeOpen; }

    [RelayCommand]
    private Task AddEntryAsync() => NavigateReportAsync(new(AppShell.EntryEditorRoute));

    /// <summary>Loads the shown package.</summary>
    /// <remarks>
    /// Loads run one after the other (appearing, a query and the scope switches can ask at once, as on Home, CR07-05), and a
    /// switch changed during a load is applied by one more load right after it.
    /// </remarks>
    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        if (!OwnsPublication) return;
        if (IsBusy) { _reloadRequested = true; return; }
        await _gate.WaitAsync();
        try
        {
            if (!OwnsPublication) return;
            _loading = true; ReportFeedback = null; StatusText = null;
            do
            {
                _reloadRequested = false;
                await _workspace.PublishAsync(async (review, token) =>
                { _review = review; await LoadCoreAsync(); token.ThrowIfCancellationRequested(); }, _ => { });
            }
            while (_reloadRequested && OwnsPublication);
        }
        finally
        {
            _loading = false; UpdateWorkspaceState();
            if (ReportReady && _lastAcceptedSuggestion is { } receipt)
                ReportFeedback = _translator.Format("Report_Accepted", Money(receipt.Amount, receipt.Currency), receipt.Name)
                    + (ReportFeedback is null ? string.Empty : "\n\n" + ReportFeedback);
            _gate.Release();
        }
    }

    private async Task LoadCoreAsync()
    {
        _settings = Inputs.Settings ?? new();
        // A deliberate financial-calendar/pay-cycle change keeps the original rebuilt-root current-period behavior.
        if (_scopeCalendar is not null && (_scopeCalendar != Calendar || _startDay != _settings.MonthStartDay)) _year = 0;
        _scopeCalendar = Calendar;
        PeriodKindNames = [_translator["Report_Month"], _translator["Report_Year"]];
        PackageNames = [_translator["Report_R1"], _translator["Report_R2"], _translator["Report_R3"], _translator["Report_R4"], _translator["Report_R5"], _translator["Report_R6"]];
        AccountScopeNames = [_translator["Report_ScopeInTotals"], _translator["Report_ScopeUsable"], _translator["Report_ScopeOneAccount"]];
        CanUseAdvancedReports = (_review ?? throw new InvalidOperationException("The original report is not loaded.")).Permission(CommercialFeature.AdvancedReports) == FeaturePermission.Allowed;
        _startDay = _settings.MonthStartDay;
        IsAdvanced = _settings.Shows(Feature.ReportDetails) && CanUseAdvancedReports;
        if (_year == 0)
        {
            (_year, _month) = PeriodMath.MonthOf(Today, Calendar, _startDay);
        }

        PreviousIcon = _localization.IsRightToLeft ? Symbol.ChevronRight : Symbol.ChevronLeft;
        NextIcon = _localization.IsRightToLeft ? Symbol.ChevronLeft : Symbol.ChevronRight;

        // Months follow the financial month (a pay cycle shows its exact range); years stay calendar years.
        (_from, _to) = PeriodKind == 0 ? PeriodMath.MonthRange(_year, _month, Calendar, _startDay) : (PeriodMath.MonthRange(_year, 1, Calendar).First, PeriodMath.MonthRange(_year, 12, Calendar).Last);
        PeriodText = PeriodKind != 0 ? _year.ToString(CultureInfo.InvariantCulture)
            : _startDay > 1 ? $"{_dates.Format(_from, DateFormatStyle.Short)} – {_dates.Format(_to, DateFormatStyle.Short)}"
            : _dates.Format(_from, DateFormatStyle.MonthYear);
        if (_to >= Today && _from <= Today)
        {
            // A running period is labelled as such, so it is not compared as if it were complete (REP-05).
            PeriodText += " · " + _translator["Report_SoFar"];
        }

        _accounts = await Task.FromResult(Inputs.Accounts);
        ApplyScope();
        var entries = await Task.FromResult(Inputs.Entries);
        IsEmpty = entries.Count == 0;

        if (PackageIndex is >= 1 and <= 4 && !CanUseAdvancedReports)
        {
            ReportFeedback = CommercialFailureMessage.Describe(new CommercialWriteRejectedException(CommercialFeature.AdvancedReports,
                _review.Permission(CommercialFeature.AdvancedReports)), _translator);
            return;
        }
        _review.Project(PackageIndex, _ => true);
        switch (PackageIndex)
        {
            case 0:
                await BuildOverviewAsync(entries);
                break;
            case 1:
                await BuildCommitmentsAsync(entries);
                break;
            case 2:
                await BuildGoalsAsync(entries);
                break;
            case 3:
                await BuildWealthAsync(entries);
                break;
            case 4:
                await BuildHistoryAsync(entries);
                break;
            default:
                await BuildStatusAsync(entries, full: true);
                break;
        }
    }

    /// <summary>Explicitly retries the original file and retains the selected package and scope.</summary>
    [RelayCommand(CanExecute = nameof(CanReload))]
    private Task RefreshReportsAsync() => LoadAsync();

    // LoadScope sets the currency and the account itself; that must not start another load.
    private void ApplyScope()
    {
        _applying = true;
        try
        {
            LoadScope();
        }
        finally
        {
            _applying = false;
        }
    }

    // The scope bar: currencies of the accounts in scope, the account scope and the text that names it.
    private void LoadScope()
    {
        ScopeAccounts = [.. _accounts.Where(a => !a.IsArchived).Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        if (ScopeAccount is null || ScopeAccounts.All(a => a.Id != ScopeAccount.Id))
        {
            ScopeAccount = ScopeAccounts.FirstOrDefault();
        }

        var currencies = ScopedAccounts().Select(a => a.CurrencyCode.ToUpperInvariant()).Distinct().Order(StringComparer.Ordinal).ToList();
        if (currencies.Count == 0)
        {
            currencies.Add(_settings.DefaultCurrencyCode);
        }

        if (!currencies.SequenceEqual(CurrencyNames))
        {
            var keep = CurrencyNames.ElementAtOrDefault(CurrencyIndex) ?? _settings.DefaultCurrencyCode;
            CurrencyNames = currencies;
            CurrencyIndex = Math.Max(0, currencies.IndexOf(keep.ToUpperInvariant()));
        }

        _currency = CurrencyNames.ElementAtOrDefault(CurrencyIndex) ?? _settings.DefaultCurrencyCode;
        HasCurrencyChoice = CurrencyNames.Count > 1;
        var accountsText = AccountScopeIndex switch
        {
            1 => _translator["Report_ScopeUsable"],
            2 => ScopeAccount?.Name ?? string.Empty,
            _ => _translator["Report_ScopeInTotals"],
        };
        var period = PackageIndex switch
        {
            1 => _translator["Report_Next30"],
            2 or 3 or 4 => _translator["Report_Today"],
            _ => PeriodText.Replace(" · " + _translator["Report_SoFar"], string.Empty, StringComparison.Ordinal),
        };
        // A formatted span shows the text as it is, so the digits follow the app's digit setting here (D-27).
        ScopeText = NativeDigits.Apply(string.Join(" · ", new[] { period, _currency, accountsText, ConfirmedOnly ? _translator["Report_ConfirmedOnly"] : null }.Where(s => !string.IsNullOrEmpty(s)))) ?? string.Empty;
    }

    // The accounts of the scope: in totals (default), usable for payments, or one account (ZEX-S0601).
    private IReadOnlyCollection<Guid>? ScopeIds() => AccountScopeIndex switch
    {
        1 => [.. _accounts.Where(a => !a.IsArchived && a.UsableForPayments).Select(a => a.Id)],
        2 when ScopeAccount is not null => [ScopeAccount.Id],
        _ => null,
    };

    private IEnumerable<Account> ScopedAccounts() => LedgerCalculator.InScope(_accounts, ScopeIds()).Where(a => !a.IsArchived);

    private LedgerFilter Filter(DateOnly from, DateOnly to) => new(from, to, ScopeIds(), ConfirmedOnly);

    private string Money(long minor, string? currency = null, bool showPlus = false) => MoneyText.Format(minor, currency ?? _currency, Culture, showPlus);

    // Opens the transactions with the same scope as the number, so the list shows the same total (AT36).
    private Task Drill(KindFilter kind, IReadOnlyCollection<Guid>? categories, string? name, DateOnly? from = null, DateOnly? to = null) =>
        NavigateReportAsync(CreateDrill(kind, categories, name, from, to));
    private ReportNavigation CreateDrill(KindFilter kind, IReadOnlyCollection<Guid>? categories, string? name, DateOnly? from = null, DateOnly? to = null)
    {
        var query = new Dictionary<string, object>
        {
            ["from"] = from ?? _from,
            ["to"] = to ?? _to,
            ["kind"] = kind,
            ["inTotals"] = AccountScopeIndex == 0,
            ["currency"] = _currency,
            ["confirmedOnly"] = ConfirmedOnly,
            ["scope"] = ScopeText,
        };
        if (ScopeIds() is { } ids)
        {
            query["accounts"] = ids;
        }

        if (categories is not null)
        {
            query["categories"] = categories;
            query["categoryName"] = name ?? string.Empty;
        }

        return new("//transactions", query);
    }

    // The data status of the package (K14): the full list in R6, a one-line footer elsewhere.
    private async Task BuildStatusAsync(IReadOnlyCollection<LedgerEntry> entries, bool full, int unknownAmounts = 0, IReadOnlyCollection<Core.Rates.RateInfo>? rates = null, IReadOnlyCollection<string>? missingRates = null, int holdingsWithoutPrice = 0)
    {
        if (full)
        {
            var events = await Task.FromResult(Inputs.AssetEvents);
            var values = Core.Holdings.AssetValuationService.Values(await Task.FromResult(Inputs.AssetTypes), events, await Task.FromResult(Inputs.AssetValuations), Today);
            holdingsWithoutPrice = values.Count(v => v.Quantity > 0 && v.Value is null);
            var forecasts = Core.Forecasts.ForecastCalculator.Compute(_accounts, entries, await Task.FromResult(Inputs.Schedules), await Task.FromResult(Inputs.OccurrenceStates), Today, Today.AddDays(30));
            unknownAmounts = forecasts.Sum(f => f.UnknownCount);
        }

        var lastBackup = _backup.LastBackupAt is { } at ? DateOnly.FromDateTime(at.ToLocalTime().DateTime) : (DateOnly?)null;
        var issues = DataStatus.Check(_accounts, entries, await Task.FromResult(Inputs.Categories), Filter(_from, _to), Today, lastBackup, unknownAmounts, rates, missingRates, holdingsWithoutPrice);
        Issues.Clear();
        foreach (var issue in issues)
        {
            Issues.Add(new IssueRow(issue.Kind, IssueText(issue), _translator[$"Issue_{issue.Kind}_Action"], issue.Ids));
        }

        HasIssues = Issues.Count > 0;
        StatusText = issues.Count == 0
            ? _translator["Report_NoIssues"]
            : _translator.Format("Report_DataStatus", string.Join(" · ", issues.Take(3).Select(IssueText)) + (issues.Count > 3 ? " · …" : string.Empty));
    }

    private string IssueText(DataIssue issue) => issue.Kind switch
    {
        DataIssueKind.BackupOld when issue.Date is { } date => _translator.Format("Issue_BackupOld", _dates.Format(date, DateFormatStyle.Short)),
        DataIssueKind.BackupOld => _translator["Issue_NoBackup"],
        DataIssueKind.OutdatedRates when issue.Date is { } date => _translator.Format("Issue_OutdatedRates", string.Join(", ", issue.Details ?? []), _dates.Format(date, DateFormatStyle.Short)),
        DataIssueKind.MissingRates or DataIssueKind.OutdatedRates => _translator.Format($"Issue_{issue.Kind}", string.Join(", ", issue.Details ?? []), string.Empty),
        DataIssueKind.NotReconciled or DataIssueKind.OpeningUnknown => _translator.Format($"Issue_{issue.Kind}", string.Join(", ", issue.Details ?? [])),
        _ => _translator.Format($"Issue_{issue.Kind}", issue.Count),
    };

    // Each item opens the list or screen that fixes it (K14).
    [RelayCommand]
    private Task FixIssueAsync(IssueRow row)
    {
        if (!CanRunCommands || !RowCurrent(Issues, row)) return Task.CompletedTask;
        var target = row.Kind switch
    {
        DataIssueKind.BackupOld => new ReportNavigation(AppShell.BackupRoute),
        DataIssueKind.Unreviewed => new ReportNavigation("//transactions", new Dictionary<string, object> { ["unreviewed"] = true }),
        DataIssueKind.UnknownAmounts => new ReportNavigation("//plans"),
        DataIssueKind.MissingRates or DataIssueKind.OutdatedRates => new ReportNavigation(AppShell.RatesRoute),
        DataIssueKind.HoldingsWithoutPrice => new ReportNavigation(AppShell.HoldingsRoute),
        DataIssueKind.OpeningUnknown or DataIssueKind.NotReconciled when row.Ids is [var id, ..] => new ReportNavigation(AppShell.AccountDetailRoute, new Dictionary<string, object> { ["id"] = id }),
        DataIssueKind.WithoutCategory => CreateDrill(KindFilter.All, null, null),
        _ => CreateDrill(KindFilter.All, null, null),
        };
        return NavigateReportAsync(target, () => RowCurrent(Issues, row));
    }

    [RelayCommand]
    private Task ExplainStatusAsync() => ExplainAsync("K14", string.Empty, [.. Issues.Select(i => new AmountLine(i.Text, string.Empty, false))]);

    [RelayCommand]
    private Task OpenReviewAsync() => NavigateReportAsync(new(AppShell.ReviewRoute));

    [RelayCommand]
    private Task PreviousAsync()
    {
        if (!CanRunCommands) return Task.CompletedTask;
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
        if (!CanRunCommands) return Task.CompletedTask;
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

    // Opens the explanation of a KPI with the number, the scope it was computed with and its parts (ZEX-UI14).
    private async Task NavigateReportAsync(ReportNavigation target, Func<bool>? rowCurrent = null)
    {
        if (!CanRunCommands || _workspace.CaptureCommand(ScopeStamp, rowCurrent) is not { } command) return;
        IsBusy = true;
        try { await command.ValidateAsync(); await NavigateFrozenAsync(target); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (command.IsCurrent) await _workspace.RefuseAsync(exception); }
        finally { IsBusy = false; if (_reloadRequested && OwnsPublication) await LoadAsync(); }
    }
    private async Task NavigateFrozenAsync(ReportNavigation target)
    {
        // The app keeps a source-bound incoming tab scope until the recipient leaves it normally.
        using var handoff = (Application.Current as App)?.HoldReportScopeNavigation();
        await Shell.Current.GoToAsync(target.Route, target.CopyQuery()); handoff?.Complete();
    }
    private async Task ExplainAsync(string id, string value, IReadOnlyList<AmountLine> lines, Func<ReportNavigation>? showEntries = null)
    {
        if (!CanRunCommands || _workspace.CaptureCommand(ScopeStamp) is not { } command) return;
        var target = showEntries?.Invoke(); var originalScope = ScopeText; var originalStatus = StatusText; var originalLines = lines.ToArray();
        IsBusy = true;
        try
        {
            await command.ValidateAsync(); var source = command.CaptureChildSource();
            var sheet = new KpiExplanation(id, value, originalScope, originalLines, originalStatus,
                target is null ? null : async () => { await source.ValidateAsync(); await NavigateFrozenAsync(target); },
                source.ValidateAsync, () => source.IsCurrent,
                () => !_rootRetired && (_rootOwner?.Invoke() ?? true) && ReferenceEquals(Shell.Current?.CurrentPage?.BindingContext, this));
            await Shell.Current.GoToAsync(AppShell.KpiSheetRoute, new Dictionary<string, object> { ["sheet"] = sheet });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (command.IsCurrent) await _workspace.RefuseAsync(exception); }
        finally { IsBusy = false; if (_reloadRequested && OwnsPublication) await LoadAsync(); }
    }

    // PDF of the scope (REP-07, ZEX-S0601): every package with its own scope line, clearly marked as not official.
    [RelayCommand]
    private async Task SharePdfAsync()
    {
        if (!CanRunCommands || _review is null || _workspace.CaptureCommand(ScopeStamp) is not { } command) return;
        IsBusy = true;
        try
        {
            var review = _review; var scope = ScopeStamp();
            var advanced = review.Permission(CommercialFeature.AdvancedReports) == FeaturePermission.Allowed
                && review.Permission(CommercialFeature.AdvancedPdf) == FeaturePermission.Allowed;
            await _export.ExportAsync(review, advanced, () =>
            { command.RequireCurrent(); if (scope != ScopeStamp() || !ReferenceEquals(review, _review)) throw new InvalidOperationException("The reviewed report scope changed."); },
                () => CreatePdfAsync(advanced), new NativeExport(this), command.Token);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { if (command.IsCurrent) await _workspace.RefuseAsync(ex); }
        finally { IsBusy = false; if (_reloadRequested && OwnsPublication) await LoadAsync(); }
    }

    private string ScopeStamp() => JsonSerializer.Serialize(new { PackageIndex, PeriodKind, _year, _month, AccountScopeIndex, from = _from, to = _to, currency = _currency, scope = ScopeText,
        theme = Application.Current?.RequestedTheme, userTheme = Application.Current?.UserAppTheme,
        account = ScopeAccount?.Id, CurrencyIndex, currencies = CurrencyNames, ConfirmedOnly, ChangeRangeIndex,
        language = _localization.CurrentLanguage.CultureName, culture = Culture.Name, calendar = _localization.CurrentCalendar,
        digits = _localization.CurrentDigits, region = _localization.CurrentRegion, week = _localization.FirstDayOfWeek });

    private sealed class NativeExport(ReportsViewModel owner) : IReportExportRuntime
    {
        public Task<bool> ConfirmAsync() => owner._lock.ConfirmAsync(owner._translator["Lock_ConfirmExport"]);
        public async Task<string> WriteAsync(byte[] document)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"zanance-report-{Guid.NewGuid():N}.pdf");
            try { await File.WriteAllBytesAsync(path, document); return path; }
            catch { File.Delete(path); throw; }
        }
        public Task ShareAsync(string path) => Share.Default.RequestAsync(new ShareFileRequest
            { Title = owner._translator["Report_SharePdf"], File = new ShareFile(path, "application/pdf") });
        public Task RemoveAsync(string path) { File.Delete(path); return Task.CompletedTask; }
    }

    /// <summary>Creates a detached PDF of the reviewed original scope without walking the live page.</summary>
    public Task<byte[]> CreatePdfAsync() => CreatePdfAsync(_review?.Permission(CommercialFeature.AdvancedReports) == FeaturePermission.Allowed
        && _review.Permission(CommercialFeature.AdvancedPdf) == FeaturePermission.Allowed);

    private async Task<byte[]> CreatePdfAsync(bool advanced)
    {
        var review = _review ?? throw new InvalidOperationException("Load the original report before export.");
        review.DemandPdf(advanced); var scope = ScopeStamp(); await _reportStore.ValidateAsync(review);
        var projection = new ReportsViewModel(_store, _plans, _goals, _holdings, _backup, _goalPresenter, _translator,
            _dates, _localization, _time, _lock, _reportStore) { _detached = true, _review = review,
            _settings = _settings, _accounts = Inputs.Accounts, _year = _year, _month = _month, _startDay = _startDay,
            _from = _from, _to = _to, _currency = _currency, PeriodKind = PeriodKind, AccountScopeIndex = AccountScopeIndex,
            ScopeAccount = ScopeAccount, CurrencyNames = CurrencyNames, CurrencyIndex = CurrencyIndex, ConfirmedOnly = ConfirmedOnly,
            ChangeRangeIndex = ChangeRangeIndex, PeriodText = PeriodText, CanUseAdvancedReports = advanced, IsAdvanced = IsAdvanced && advanced };
        var bytes = await projection.RenderPdfAsync(advanced); await _reportStore.ValidateAsync(review);
        if (scope != ScopeStamp()) throw new InvalidOperationException("The original report scope changed during rendering.");
        return bytes;
    }

    private async Task<byte[]> RenderPdfAsync(bool advanced)
    {
        // Only this detached projection walks packages; all sections share one reviewed original packet.
        await _gate.WaitAsync();
        var shown = PackageIndex;
        var tables = new List<Vafadar.Zanance.Reports.ReportTable>();
        _applying = true;
        try
        {
            var entries = await Task.FromResult(Inputs.Entries);

            // R1: the overview with categories, trend and tags.
            PackageIndex = 0;
            LoadScope();
            await BuildOverviewAsync(entries);
            var overviewScope = ScopeText;
            tables.Add(new(_translator["Report_R1"], [_translator["Report_Item"], _translator["Report_Amount"]],
                [.. OverviewLines.Concat(SeparateLines).Select(l => (IReadOnlyList<string>)[l.Label, l.Amount])], new HashSet<int> { 1 }, overviewScope));
            if (CategoryRows.Count > 0)
            {
                tables.Add(new(_translator["Report_GrossByCategory"], [_translator["Entry_Category"], _translator["Report_Gross"], _translator["Report_Refunds"], _translator["Report_Net"]],
                    [.. CategoryRows.Select(r => (IReadOnlyList<string>)[r.Name, r.GrossText, r.RefundsText, r.NetText])], new HashSet<int> { 1, 2, 3 }, overviewScope));
            }

            // Original period balance/movements are basic owned-data reporting, not paid wealth analysis.
            BuildAccounts(entries);
            foreach (var account in Accounts)
            {
                tables.Add(new(account.Name, [_translator["Report_Item"], _translator["Report_Amount"]],
                    [.. account.Lines.Select(l => (IReadOnlyList<string>)[l.Label, l.Amount])], new HashSet<int> { 1 }, overviewScope.Replace(" · " + _currency + " · ", " · " + account.CurrencyCode + " · ", StringComparison.Ordinal)));
            }

            if (advanced) { _review!.DemandPdf(true); BuildTrend(entries); }
            if (advanced && Trend.Count > 0)
            {
                tables.Add(new(_translator["Report_Trend"], [_translator["Report_Month"], _translator["KindFilter_Income"], _translator["KindFilter_Expenses"], _translator["Home_Result"]],
                    [.. Trend.Select(t => (IReadOnlyList<string>)[t.Label, t.IncomeText, t.ExpenseText, t.ResultText])], new HashSet<int> { 1, 2, 3 }, overviewScope));
            }

            if (advanced) BuildTags(entries);
            if (advanced && TagRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Tags"], [_translator["Entry_Tags"], _translator["Report_Net"]],
                    [.. TagRows.Select(t => (IReadOnlyList<string>)["#" + t.Tag, t.AmountText])], new HashSet<int> { 1 }, overviewScope + " · " + _translator["Report_TagsNote"]));
            }

            if (advanced)
            {
            _review!.DemandPdf(true);
            // R2: commitments and plans.
            PackageIndex = 1;
            LoadScope();
            await BuildCommitmentsAsync(entries);
            if (CommitmentRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Next30Title"], [_translator["Plan_Name"], _translator["Entry_Date"], _translator["Report_Amount"]],
                    [.. CommitmentRows.Select(c => (IReadOnlyList<string>)[c.Name, c.DateText, c.AmountText])], new HashSet<int> { 2 }, ScopeText));
            }

            if (PlanRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Plans"], [_translator["Plan_Name"], _translator["Report_Planned"], _translator["Report_Actual"]],
                    [.. PlanRows.Select(p => (IReadOnlyList<string>)[p.Name, p.PlannedText, p.ActualText])], new HashSet<int> { 1, 2 }, overviewScope));
            }

            // R4: net worth and the movement of every account.
            PackageIndex = 3;
            LoadScope();
            await BuildWealthAsync(entries);
            if (WealthLines.Count > 0)
            {
                tables.Add(new(_translator["Report_NetWorth"], [_translator["Report_Item"], _translator["Report_Amount"]],
                    [.. WealthLines.Select(l => (IReadOnlyList<string>)[l.Label, l.Amount])], new HashSet<int> { 1 }, ScopeText));
            }

            // R5: net worth at month ends and its change (Advanced data, printed when there is a history).
            PackageIndex = 4;
            LoadScope();
            await BuildHistoryAsync(entries);
            if (HasHistory)
            {
                tables.Add(new(_translator["Report_History"], [_translator["Report_Month"], _translator["Report_NetWorth"]],
                    [.. HistoryRows.Select(h => (IReadOnlyList<string>)[h.Label, h.ValueText])], new HashSet<int> { 1 }, ScopeText));
                tables.Add(new(ChangeTitle ?? _translator["Report_R5"], [_translator["Report_Item"], _translator["Report_Amount"]],
                    [.. ChangeLines.Select(l => (IReadOnlyList<string>)[l.Label, l.Amount])], new HashSet<int> { 1 }, ChangeNote ?? ScopeText));
            }

            }
            PackageIndex = 5;
            await BuildStatusAsync(entries, full: true);
            var document = new Vafadar.Zanance.Reports.ReportDocument(
                _translator.Format("Report_PdfTitle", _translator["App_Name"]),
                PeriodText,
                StatusText ?? string.Empty,
                _translator.Format("Report_PdfCreated", _dates.Format(Today, DateFormatStyle.Long)),
                _translator["Report_PdfDisclaimer"],
                _localization.IsRightToLeft,
                [],
                tables);
            return await Task.Run(() => Vafadar.Zanance.Reports.PdfReport.Write(document));
        }
        finally
        {
            PackageIndex = shown;
            _applying = false;
            _gate.Release();
        }
    }
}
