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

    /// <summary>Accepts <c>unreviewed=true</c> (from Home) and <c>account</c> (from an account).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
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

    partial void OnPeriodIndexChanged(int value) => Refresh();

    partial void OnKindIndexChanged(int value) => Refresh();

    partial void OnSelectedAccountChanged(AccountFilterOption? value) => Refresh();

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
        var filter = new EntryFilter(from, to, (KindFilter)KindIndex, SelectedAccount?.Id, null, UnreviewedOnly, SearchText);
        var culture = _localization.CurrentCulture;
        var presenter = new EntryPresenter(_accounts, _categories, _translator, culture);
        var matching = EntrySearch.Apply(_entries, filter, id => _categories.Name(id), _accounts);

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
        _loading = false;
        Refresh();
    }

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        await Undo.UndoAsync();
        await LoadAsync();
    }
}
