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

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A category row of the expense table: gross, refunds and net (REP-02).</summary>
public sealed record CategoryReportRow(IReadOnlyCollection<Guid> CategoryIds, string Name, Color Color, string GrossText, string RefundsText, string NetText, Color NetColor, string DetailText);

/// <summary>A labelled amount, e.g. one line of an account movement.</summary>
public sealed record AmountLine(string Label, string Amount, bool IsTotal, bool IsNegative = false, bool IsPositive = false);

/// <summary>An account with its movement lines.</summary>
public sealed record AccountReport(string Name, IReadOnlyList<AmountLine> Lines);

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
public sealed partial class ReportsViewModel : ViewModelBase, IQueryAttributable
{
    private const int TrendMonths = 6;

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
    private bool _loading;

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
        Security.AppLockService appLock)
    {
        _lock = appLock;
        _store = store;
        _plans = plans;
        _goals = goals;
        _holdings = holdings;
        _backup = backup;
        _goalPresenter = goalPresenter;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        PeriodKindNames = [translator["Report_Month"], translator["Report_Year"]];
        PackageNames = [translator["Report_R1"], translator["Report_R2"], translator["Report_R3"], translator["Report_R4"], translator["Report_R6"]];
        AccountScopeNames = [translator["Report_ScopeInTotals"], translator["Report_ScopeUsable"], translator["Report_ScopeOneAccount"]];
        PeriodText = string.Empty;
        ScopeText = string.Empty;
    }

    public IReadOnlyList<string> PeriodKindNames { get; }

    public IReadOnlyList<string> PackageNames { get; }

    public IReadOnlyList<string> AccountScopeNames { get; }

    public ObservableCollection<IssueRow> Issues { get; } = [];

    [ObservableProperty]
    public partial int PeriodKind { get; set; }

    /// <summary>Gets or sets the package: 0 R1, 1 R2, 2 R3, 3 R4, 4 R6.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOverview), nameof(ShowCommitments), nameof(ShowGoals), nameof(ShowWealth), nameof(ShowStatus), nameof(ShowEmpty), nameof(ShowPeriod))]
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

    public bool ShowCommitments => PackageIndex == 1;

    public bool ShowGoals => PackageIndex == 2;

    public bool ShowWealth => PackageIndex == 3;

    public bool ShowStatus => PackageIndex == 4;

    /// <summary>Gets a value indicating whether the package has a period (commitments look ahead from today; wealth is today).</summary>
    public bool ShowPeriod => PackageIndex is 0 or 4;

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private CultureInfo Culture => _localization.CurrentCulture;

    private PeriodCalendar Calendar => _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;

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
        if (!_loading)
        {
            _ = Presentation.Failures.GuardAsync(LoadAsync);
        }
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
    private void ToggleScope() => IsScopeOpen = !IsScopeOpen;

    [RelayCommand]
    private Task AddEntryAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute);

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _settings = await _store.GetSettingsAsync();
            _startDay = _settings.MonthStartDay;
            IsAdvanced = _settings.Mode == ExperienceMode.Advanced;
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

            _accounts = await _store.GetAccountsAsync();
            LoadScope();
            var entries = await _store.GetEntriesAsync();
            IsEmpty = entries.Count == 0;

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
                default:
                    await BuildStatusAsync(entries, full: true);
                    break;
            }
        }
        finally
        {
            _loading = false;
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
            2 or 3 => _translator["Report_Today"],
            _ => PeriodText.Replace(" · " + _translator["Report_SoFar"], string.Empty, StringComparison.Ordinal),
        };
        ScopeText = string.Join(" · ", new[] { period, _currency, accountsText, ConfirmedOnly ? _translator["Report_ConfirmedOnly"] : null }.Where(s => !string.IsNullOrEmpty(s)));
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
    private Task Drill(KindFilter kind, IReadOnlyCollection<Guid>? categories, string? name, DateOnly? from = null, DateOnly? to = null)
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

        return Shell.Current.GoToAsync("//transactions", query);
    }

    // The data status of the package (K14): the full list in R6, a one-line footer elsewhere.
    private async Task BuildStatusAsync(IReadOnlyCollection<LedgerEntry> entries, bool full, int unknownAmounts = 0, IReadOnlyCollection<Core.Rates.RateInfo>? rates = null, IReadOnlyCollection<string>? missingRates = null, int holdingsWithoutPrice = 0)
    {
        if (full)
        {
            var events = await _holdings.GetEventsAsync();
            var values = Core.Holdings.AssetValuationService.Values(await _holdings.GetTypesAsync(), events, await _holdings.GetValuationsAsync(), Today);
            holdingsWithoutPrice = values.Count(v => v.Quantity > 0 && v.Value is null);
            var forecasts = Core.Forecasts.ForecastCalculator.Compute(_accounts, entries, await _plans.GetSchedulesAsync(), await _plans.GetStatesAsync(), Today, Today.AddDays(30));
            unknownAmounts = forecasts.Sum(f => f.UnknownCount);
        }

        var lastBackup = _backup.LastBackupAt is { } at ? DateOnly.FromDateTime(at.ToLocalTime().DateTime) : (DateOnly?)null;
        var issues = DataStatus.Check(_accounts, entries, await _store.GetCategoriesAsync(), Filter(_from, _to), Today, lastBackup, unknownAmounts, rates, missingRates, holdingsWithoutPrice);
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
    private Task FixIssueAsync(IssueRow row) => row.Kind switch
    {
        DataIssueKind.BackupOld => Shell.Current.GoToAsync(AppShell.BackupRoute),
        DataIssueKind.Unreviewed => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["unreviewed"] = true }),
        DataIssueKind.UnknownAmounts => Shell.Current.GoToAsync("//plans"),
        DataIssueKind.MissingRates or DataIssueKind.OutdatedRates => Shell.Current.GoToAsync(AppShell.RatesRoute),
        DataIssueKind.HoldingsWithoutPrice => Shell.Current.GoToAsync(AppShell.HoldingsRoute),
        DataIssueKind.OpeningUnknown or DataIssueKind.NotReconciled when row.Ids is [var id, ..] => Shell.Current.GoToAsync(AppShell.AccountDetailRoute, new Dictionary<string, object> { ["id"] = id }),
        DataIssueKind.WithoutCategory => Drill(KindFilter.All, null, null),
        _ => Drill(KindFilter.All, null, null),
    };

    [RelayCommand]
    private Task OpenReviewAsync() => Shell.Current.GoToAsync(AppShell.ReviewRoute);

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

    // Opens the explanation of a KPI with the number, the scope it was computed with and its parts (ZEX-UI14).
    private Task ExplainAsync(string id, string value, IReadOnlyList<AmountLine> lines, Func<Task>? showEntries = null) =>
        Shell.Current.GoToAsync(AppShell.KpiSheetRoute, new Dictionary<string, object>
        {
            ["sheet"] = new KpiExplanation(id, value, ScopeText, lines, StatusText, showEntries),
        });

    // PDF of the scope (REP-07, ZEX-S0601): every package with its own scope line, clearly marked as not official.
    [RelayCommand]
    private async Task SharePdfAsync()
    {
        if (IsBusy || !await _lock.ConfirmAsync(_translator["Lock_ConfirmExport"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var pdf = await CreatePdfAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, string.Create(CultureInfo.InvariantCulture, $"zanance-report-{_from:yyyy-MM}.pdf"));
            await File.WriteAllBytesAsync(path, pdf);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = _translator["Report_SharePdf"], File = new ShareFile(path, "application/pdf") });
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            await Failures.ShowAsync(ex);
        }
        finally
        {
            IsBusy = false;
            await LoadAsync();
        }
    }

    /// <summary>Creates the PDF of the current scope with all packages; the screen must be reloaded afterwards.</summary>
    public async Task<byte[]> CreatePdfAsync()
    {
        var shown = PackageIndex;
        var entries = await _store.GetEntriesAsync();
        var tables = new List<Vafadar.Zanance.Reports.ReportTable>();
        _loading = true;
        try
        {
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

            BuildTrend(entries);
            if (Trend.Count > 0)
            {
                tables.Add(new(_translator["Report_Trend"], [_translator["Report_Month"], _translator["KindFilter_Income"], _translator["KindFilter_Expenses"], _translator["Home_Result"]],
                    [.. Trend.Select(t => (IReadOnlyList<string>)[t.Label, t.IncomeText, t.ExpenseText, t.ResultText])], new HashSet<int> { 1, 2, 3 }, overviewScope));
            }

            BuildTags(entries);
            if (TagRows.Count > 0)
            {
                tables.Add(new(_translator["Report_Tags"], [_translator["Entry_Tags"], _translator["Report_Net"]],
                    [.. TagRows.Select(t => (IReadOnlyList<string>)["#" + t.Tag, t.AmountText])], new HashSet<int> { 1 }, overviewScope + " · " + _translator["Report_TagsNote"]));
            }

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

            foreach (var account in Accounts)
            {
                tables.Add(new(account.Name, [_translator["Report_Item"], _translator["Report_Amount"]],
                    [.. account.Lines.Select(l => (IReadOnlyList<string>)[l.Label, l.Amount])], new HashSet<int> { 1 }, overviewScope));
            }

            PackageIndex = 4;
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
            _loading = false;
        }
    }
}
