using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Transactions;

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
    private readonly ZananceStore _store;
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
    private List<SavedFilter> _savedFilters = [];

    /// <summary>Gets the saved filters for one-tap use (REP-08).</summary>
    public ObservableCollection<SavedFilter> SavedFilters { get; } = [];

    [ObservableProperty]
    public partial bool HasSavedFilters { get; set; }
    private bool _inTotalsOnly;

    public TransactionsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, UndoService undo)
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
            SearchText = query.TryGetValue("search", out var search) && search is string text ? text : string.Empty;
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
            await LoadSavedFiltersAsync();
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
            Days.Add(new EntryDayGroup(_dates.Format(day.Date, DateFormatStyle.Long), net, day.Entries.Select(e =>
            {
                var row = presenter.Row(e);
                row.Selection.IsSelected = _selected.Contains(e.Id);
                return row;
            })));
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
    private Task OpenAsync(EntryRow row)
    {
        if (IsSelecting)
        {
            ToggleSelection(row);
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
    }

    // Bulk operations (F2-TX-04): select several entries, then mark them reviewed, change their category, add a tag or
    // delete them with undo. Every change goes through the normal validation.
    private readonly HashSet<Guid> _selected = [];

    [ObservableProperty]
    public partial bool IsSelecting { get; set; }

    [ObservableProperty]
    public partial string? SelectionText { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [RelayCommand]
    private void StartSelecting()
    {
        IsSelecting = true;
        UpdateSelectionText();
    }

    [RelayCommand]
    private void StopSelecting()
    {
        IsSelecting = false;
        _selected.Clear();
        foreach (var row in Days.SelectMany(d => d))
        {
            row.Selection.IsSelected = false;
        }

        UpdateSelectionText();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var row in Days.SelectMany(d => d))
        {
            _selected.Add(row.Id);
            row.Selection.IsSelected = true;
        }

        UpdateSelectionText();
    }

    private void ToggleSelection(EntryRow row)
    {
        row.Selection.IsSelected = _selected.Add(row.Id) || !_selected.Remove(row.Id);
        UpdateSelectionText();
    }

    private void UpdateSelectionText()
    {
        HasSelection = _selected.Count > 0;
        SelectionText = _translator.Format("Bulk_Selected", _selected.Count);
    }

    private List<LedgerEntry> SelectedEntries() => [.. _entries.Where(e => _selected.Contains(e.Id))];

    private async Task SaveBulkAsync(List<LedgerEntry> changed)
    {
        if (changed.Count == 0)
        {
            return;
        }

        var result = await _store.SaveEntriesAsync(changed, []);
        if (!result.Succeeded)
        {
            await Shell.Current.DisplayAlertAsync(_translator["Bulk_Title"], string.Join(Environment.NewLine, result.Errors.Distinct().Select(e => _translator[$"LedgerError_{e}"])), _translator["Common_Ok"]);
            return;
        }

        StopSelecting();
        await LoadAsync();
    }

    [RelayCommand]
    private Task BulkReviewedAsync()
    {
        var changed = SelectedEntries().Where(e => e.Review == ReviewState.Unreviewed).ToList();
        changed.ForEach(e => e.Review = ReviewState.Confirmed);
        return SaveBulkAsync(changed);
    }

    [RelayCommand]
    private async Task BulkCategoryAsync()
    {
        var entries = SelectedEntries().Where(e => e.Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Refund or EntryKind.IncomeReversal).ToList();
        var kinds = entries.Select(e => e.Kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense).Distinct().ToList();
        if (_categories is null || kinds.Count != 1)
        {
            await Shell.Current.DisplayAlertAsync(_translator["Bulk_Title"], _translator["Bulk_CategoryMixed"], _translator["Common_Ok"]);
            return;
        }

        var options = _categories.All.Where(c => c.Kind == kinds[0] && !c.IsArchived).OrderBy(c => c.ParentId is null ? c.SortOrder : _categories.Get(c.ParentId)?.SortOrder ?? 0).ThenBy(c => c.ParentId is null ? 0 : 1)
            .Select(c => (c.Id, Name: _categories.Name(c.Id))).ToList();
        var choice = await Shell.Current.DisplayActionSheetAsync(_translator["Bulk_ChooseCategory"], _translator["Common_Cancel"], null, [.. options.Select(o => o.Name)]);
        var picked = options.FindIndex(o => o.Name == choice);
        if (choice is null || picked < 0)
        {
            return;
        }

        entries.ForEach(e => e.CategoryId = options[picked].Id);
        await SaveBulkAsync(entries);
    }

    [RelayCommand]
    private async Task BulkTagAsync()
    {
        var tag = EntryTags.Parse(await Shell.Current.DisplayPromptAsync(_translator["Bulk_AddTag"], _translator["Bulk_AddTagMessage"], _translator["Common_Ok"], _translator["Common_Cancel"], maxLength: EntryTags.MaxLength)).FirstOrDefault();
        if (tag is null)
        {
            return;
        }

        var entries = SelectedEntries();
        entries.ForEach(e => e.Tags = EntryTags.Normalize([.. e.Tags, tag]));
        await SaveBulkAsync(entries);
    }

    [RelayCommand]
    private async Task BulkDeleteAsync()
    {
        var ids = _selected.ToList();
        if (ids.Count == 0 || !await Shell.Current.DisplayAlertAsync(_translator["Bulk_Title"], _translator.Format("Bulk_DeleteMessage", ids.Count), _translator["Common_Delete"], _translator["Common_Cancel"]))
        {
            return;
        }

        var deleted = new List<LedgerEntry>();
        foreach (var id in ids.Where(id => deleted.All(d => d.Id != id)))
        {
            deleted.AddRange(await _store.DeleteEntryAsync(id));
        }

        if (deleted.Count > 0)
        {
            Undo.Offer(deleted);
        }

        StopSelecting();
        await LoadAsync();
        UpdateUndo();
    }

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

    private async Task LoadSavedFiltersAsync()
    {
        _savedFilters = await _store.GetSavedFiltersAsync();
        SavedFilters.Clear();
        foreach (var filter in _savedFilters)
        {
            SavedFilters.Add(filter);
        }

        HasSavedFilters = SavedFilters.Count > 0;
    }

    // Saved filters (REP-08): the current combination under a name, applied again with one tap; entries never change.
    [RelayCommand]
    private async Task SavedFilterMenuAsync()
    {
        var save = _translator["Filter_SaveCurrent"];
        var remove = _translator["Filter_Remove"];
        string[] actions = _savedFilters.Count > 0 ? [save, remove] : [save];
        var choice = await Shell.Current.DisplayActionSheetAsync(_translator["Filter_Title"], _translator["Common_Cancel"], null, actions);
        if (choice == save)
        {
            await SaveCurrentFilterAsync();
        }
        else if (choice == remove)
        {
            var name = await Shell.Current.DisplayActionSheetAsync(remove, _translator["Common_Cancel"], null, [.. _savedFilters.Select(f => f.Name)]);
            if (_savedFilters.FirstOrDefault(f => f.Name == name) is { } filter)
            {
                await _store.DeleteSavedFilterAsync(filter.Id);
                await LoadSavedFiltersAsync();
            }
        }
    }

    private async Task SaveCurrentFilterAsync()
    {
        var name = (await Shell.Current.DisplayPromptAsync(_translator["Filter_SaveCurrent"], _translator["Filter_NameMessage"], _translator["Common_Save"], _translator["Common_Cancel"], maxLength: SavedFilter.MaxNameLength))?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (_savedFilters.Any(f => string.Equals(f.Name, name, StringComparison.CurrentCultureIgnoreCase))
            && !await Shell.Current.DisplayAlertAsync(_translator["Filter_SaveCurrent"], _translator.Format("Filter_Replace", name), _translator["Common_Save"], _translator["Common_Cancel"]))
        {
            return;
        }

        await _store.SaveSavedFilterAsync(new SavedFilter
        {
            Name = name,
            Period = Math.Max(0, PeriodIndex),
            From = _customPeriod?.From,
            To = _customPeriod?.To,
            Kind = KindIndex,
            AccountId = SelectedAccount?.Id,
            CategoryIds = _categoryIds is null ? [] : [.. _categoryIds],
            CategoryName = CategoryFilterName,
            UnreviewedOnly = UnreviewedOnly,
            InTotalsOnly = _inTotalsOnly,
            Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
        });
        await LoadSavedFiltersAsync();
    }

    [RelayCommand]
    private void ApplySavedFilter(SavedFilter filter)
    {
        _loading = true;
        try
        {
            if (filter.HasCustomRange)
            {
                _customPeriod = (filter.From!.Value, filter.To!.Value);
                CustomPeriodText = $"{_dates.Format(filter.From.Value, DateFormatStyle.Short)} – {_dates.Format(filter.To.Value, DateFormatStyle.Short)}";
                PeriodIndex = -1;
            }
            else
            {
                _customPeriod = null;
                CustomPeriodText = null;
                PeriodIndex = Math.Clamp(filter.Period, 0, PeriodNames.Count - 1);
            }

            KindIndex = Math.Clamp(filter.Kind, 0, KindNames.Count - 1);
            SelectedAccount = AccountOptions.FirstOrDefault(o => o.Id == filter.AccountId) ?? AccountOptions.FirstOrDefault();

            // A category from the picker selects it again; a drill-down keeps its categories and label. A category that
            // no longer exists is shown as such, so the filter is never active without a visible chip to clear it.
            var ids = filter.CategoryIds.Count > 0 ? filter.CategoryIds : null;
            var picked = filter.CategoryName is null && ids is not null ? CategoryOptions.FirstOrDefault(o => o.Id is { } id && ids.Contains(id)) : null;
            SelectedCategory = picked ?? CategoryOptions.FirstOrDefault();
            _categoryIds = ids;
            CategoryFilterName = picked is not null ? null : filter.CategoryName ?? (ids is null ? null : _translator["Filter_CategoryMissing"]);
            _inTotalsOnly = filter.InTotalsOnly;
            UnreviewedOnly = filter.UnreviewedOnly;
            SearchText = filter.Search ?? string.Empty;
        }
        finally
        {
            _loading = false;
        }

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
