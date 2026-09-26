using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Transactions;

/// <summary>The entries of one day with its net result.</summary>
public sealed class EntryDayGroup(string header, string netText, IEnumerable<EntryRow> rows) : List<EntryRow>(rows)
{
    public string Header { get; } = header;

    public string NetText { get; } = netText;
}

/// <summary>An account choice of the account filter; <see cref="Id"/> is <see langword="null"/> for "all accounts".</summary>
public sealed record AccountFilterOption(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>The transaction list (UI-04): search, filter chips and entries grouped by day.</summary>
public sealed partial class TransactionsViewModel : ViewModelBase, IQueryAttributable
{
    private readonly FinanceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private List<LedgerEntry> _entries = [];
    private Dictionary<Guid, Account> _accounts = [];
    private CategoryLookup? _categories;
    private CancellationTokenSource? _searchDelay;
    private bool _loading;
    private Guid? _pendingAccount;
    private IReadOnlyCollection<Guid>? _categoryIds;
    private (DateOnly From, DateOnly To)? _customPeriod;
    private bool _inTotalsOnly;

    public TransactionsViewModel(FinanceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, UndoService undo)
    {
        _store = store;
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        Undo = undo;
        SearchText = string.Empty;
        PeriodNames = [translator["Period_ThisMonth"], translator["Period_LastMonth"], translator["Period_ThisYear"], translator["Period_All"]];
        KindNames = [translator["KindFilter_All"], translator["KindFilter_Expenses"], translator["KindFilter_Income"], translator["KindFilter_Transfers"]];
        AccountOptions = [];
    }

    public UndoService Undo { get; }

    public IReadOnlyList<string> PeriodNames { get; }

    public IReadOnlyList<string> KindNames { get; }

    public ObservableCollection<EntryDayGroup> Days { get; } = [];

    [ObservableProperty]
    public partial int PeriodIndex { get; set; }

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountFilterOption> AccountOptions { get; set; }

    [ObservableProperty]
    public partial AccountFilterOption? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial bool ShowAccountFilter { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountFilterOption> CategoryOptions { get; set; } = [];

    [ObservableProperty]
    public partial AccountFilterOption? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial bool UnreviewedOnly { get; set; }

    [ObservableProperty]
    public partial int UnreviewedCount { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasNoEntriesAtAll { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    [ObservableProperty]
    public partial bool ShowUndo { get; set; }

    [ObservableProperty]
    public partial string? CategoryFilterName { get; set; }

    [ObservableProperty]
    public partial string? SummaryText { get; set; }

    [ObservableProperty]
    public partial string? CustomPeriodText { get; set; }

    /// <summary>
    /// Accepts <c>unreviewed=true</c>, <c>account</c>, and a drill-down from a number (AT-50): <c>period</c> (chip index),
    /// <c>kind</c> (<see cref="KindFilter"/>), <c>categories</c> (ids), <c>categoryName</c> and <c>inTotals</c>.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.ContainsKey("period") || query.ContainsKey("kind") || query.ContainsKey("categories") || query.ContainsKey("from"))
        {
            _loading = true;
            PeriodIndex = query.TryGetValue("period", out var period) && period is int p ? p : 0;
            if (query.TryGetValue("from", out var from) && from is DateOnly start && query.TryGetValue("to", out var to) && to is DateOnly end)
            {
                _customPeriod = (start, end);
                CustomPeriodText = $"{_dates.Format(start, DateFormatStyle.Short)} – {_dates.Format(end, DateFormatStyle.Short)}";
                PeriodIndex = -1;
            }
            else
            {
                _customPeriod = null;
                CustomPeriodText = null;
            }

            KindIndex = query.TryGetValue("kind", out var kind) && kind is KindFilter k ? (int)k : 0;
            _categoryIds = query.TryGetValue("categories", out var categories) ? categories as IReadOnlyCollection<Guid> : null;
            CategoryFilterName = _categoryIds is null ? null : query.TryGetValue("categoryName", out var name) ? name?.ToString() : null;
            _inTotalsOnly = query.TryGetValue("inTotals", out var inTotals) && inTotals is true;
            UnreviewedOnly = false;
            SearchText = string.Empty;
            _loading = false;
        }

        if (query.TryGetValue("unreviewed", out var unreviewed) && unreviewed is "true" or true)
        {
            UnreviewedOnly = true;
            PeriodIndex = 3;
        }

        if (query.TryGetValue("account", out var account) && Guid.TryParse(account?.ToString(), out var accountId))
        {
            _pendingAccount = accountId;
        }

        query.Clear();
    }

    /// <summary>Shows the undo banner while the undo offer is valid.</summary>
    public void UpdateUndo()
    {
        ShowUndo = Undo.CanUndo;
        if (ShowUndo)
        {
            Task.Delay(Undo.Remaining).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => ShowUndo = Undo.CanUndo), TaskScheduler.Default);
        }
    }

    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var accounts = await _store.GetAccountsAsync();
            _accounts = accounts.ToDictionary(a => a.Id);
            _categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            _entries = await _store.GetEntriesAsync();

            var selected = _pendingAccount ?? SelectedAccount?.Id;
            _pendingAccount = null;
            AccountOptions = [new AccountFilterOption(null, _translator["Accounts_AllAccounts"]), .. accounts.Select(a => new AccountFilterOption(a.Id, a.Name))];
            SelectedAccount = AccountOptions.FirstOrDefault(o => o.Id == selected) ?? AccountOptions[0];
            ShowAccountFilter = accounts.Count > 1;

            // Category filter (TX-05); a main category includes its sub-categories.
            var selectedCategory = SelectedCategory?.Id;
            CategoryOptions = [new AccountFilterOption(null, _translator["Tx_AllCategories"]), .. _categories.All
                .Where(c => !c.IsArchived && c.ParentId is null)
                .OrderBy(c => c.Kind).ThenBy(c => c.SortOrder)
                .Select(c => new AccountFilterOption(c.Id, CategoryLookup.NameOf(c, _translator)))];
            SelectedCategory = CategoryOptions.FirstOrDefault(o => o.Id == selectedCategory) ?? CategoryOptions[0];
            HasNoAccounts = accounts.Count == 0;
            HasNoEntriesAtAll = _entries.Count == 0;
            UnreviewedCount = _entries.Count(e => e.Review == ReviewState.Unreviewed);
        }
        finally
        {
            _loading = false;
        }

        Refresh();
    }

    partial void OnPeriodIndexChanged(int value)
    {
        // Choosing a period chip replaces a range that came from a report.
        if (value >= 0 && !_loading)
        {
            _customPeriod = null;
            CustomPeriodText = null;
        }

        Refresh();
    }

    partial void OnKindIndexChanged(int value) => Refresh();

    partial void OnSelectedAccountChanged(AccountFilterOption? value) => Refresh();

    partial void OnSelectedCategoryChanged(AccountFilterOption? value)
    {
        if (_loading || _categories is null)
        {
            return;
        }

        _categoryIds = value?.Id is { } id ? [.. _categories.All.Where(c => c.Id == id || c.ParentId == id).Select(c => c.Id)] : null;
        CategoryFilterName = null;
        Refresh();
    }

    partial void OnUnreviewedOnlyChanged(bool value) => Refresh();

    partial void OnSearchTextChanged(string value)
    {
        // Debounce typing so that large lists stay responsive.
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        var token = _searchDelay.Token;
        Task.Delay(250, token).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(Refresh), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    private void Refresh()
    {
        if (_loading || _categories is null)
        {
            return;
        }

        var (from, to) = PeriodRange();
        var filter = new EntryFilter(from, to, (KindFilter)KindIndex, SelectedAccount?.Id, _categoryIds, UnreviewedOnly, SearchText, _inTotalsOnly);
        var culture = _localization.CurrentCulture;
        var presenter = new EntryPresenter(_accounts, _categories, _translator, culture);
        var matching = EntrySearch.Apply(_entries, filter, id => _categories.Name(id), _accounts).ToList();
        var filtered = _categoryIds is not null || _customPeriod is not null || KindIndex != 0 || !string.IsNullOrWhiteSpace(SearchText);
        SummaryText = filtered && matching.Count > 0
            ? _translator.Format("Tx_FilterTotal", matching.Count, string.Join("  ", EntrySearch.NetByCurrency(matching, _accounts).Select(n => MoneyText.Format(n.Value, n.Key, culture, showPlus: true))))
            : null;

        Days.Clear();
        foreach (var day in EntrySearch.ByDay(matching, _accounts))
        {
            var net = string.Join("  ", day.Net.Where(n => n.Value != 0).Select(n => MoneyText.Format(n.Value, n.Key, culture, showPlus: true)));
            Days.Add(new EntryDayGroup(_dates.Format(day.Date, DateFormatStyle.Long), net, day.Entries.Select(presenter.Row)));
        }

        IsEmpty = Days.Count == 0;
    }

    private (DateOnly? From, DateOnly? To) PeriodRange()
    {
        if (_customPeriod is { } custom)
        {
            return (custom.From, custom.To);
        }

        var calendar = _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var (year, month) = PeriodMath.MonthOf(today, calendar);
        switch (PeriodIndex)
        {
            case 0:
                var current = PeriodMath.MonthRange(year, month, calendar);
                return (current.First, current.Last);
            case 1:
                var (py, pm) = PeriodMath.Previous(year, month);
                var previous = PeriodMath.MonthRange(py, pm, calendar);
                return (previous.First, previous.Last);
            case 2:
                var range = PeriodMath.YearRange(today, calendar);
                return (range.First, range.Last);
            default:
                return (null, null);
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(HasNoAccounts ? AppShell.AccountEditorRoute : AppShell.EntryEditorRoute);

    [RelayCommand]
    private Task OpenAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private void ClearFilters()
    {
        _loading = true;
        PeriodIndex = 3;
        KindIndex = 0;
        UnreviewedOnly = false;
        SearchText = string.Empty;
        SelectedAccount = AccountOptions.FirstOrDefault();
        ClearCategoryFilterCore();
        _loading = false;
        Refresh();
    }

    [RelayCommand]
    private void ClearCategoryFilter()
    {
        ClearCategoryFilterCore();
        Refresh();
    }

    private void ClearCategoryFilterCore()
    {
        if (_customPeriod is not null)
        {
            _customPeriod = null;
            CustomPeriodText = null;
            PeriodIndex = 0;
        }

        var loading = _loading;
        _loading = true;
        SelectedCategory = CategoryOptions.FirstOrDefault();
        _loading = loading;
        _categoryIds = null;
        _inTotalsOnly = false;
        CategoryFilterName = null;
    }

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        await Undo.UndoAsync();
        await LoadAsync();
    }
}
