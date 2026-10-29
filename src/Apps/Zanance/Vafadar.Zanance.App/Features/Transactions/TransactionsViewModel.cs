using Vafadar.Zanance.App.Interaction;
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

    /// <summary>Returns what a screen reader says for the day header.</summary>
    public override string ToString() => $"{Header}, {NetText}";
}

/// <summary>An account choice of the account filter; <see cref="Id"/> is <see langword="null"/> for "all accounts".</summary>
public sealed record AccountFilterOption(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>The transaction list (UI-04): search, filter chips and entries grouped by day.</summary>
public sealed partial class TransactionsViewModel : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private readonly ZananceStore _store;
    private readonly IAppInteraction _interaction;
    private readonly Commerce.QuickResourceActions _quick;
    private readonly TransactionUndoViewModel _undoAction;
    private int _startDay = 1;
    private string? _originalFile;
    private TransactionsReadSnapshot? _published;
    private volatile bool _active;
    private int _publicationRevision;
    private int _queryRevision;
    private CancellationTokenSource _lifetime = new();

    /// <summary>Gets inline original-file read feedback without duplicate alerts from shared appearance/navigation observers.</summary>
    [ObservableProperty]
    public partial string? ReadFeedback { get; private set; }
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private List<LedgerEntry> _entries = [];
    private Dictionary<Guid, Account> _accounts = [];
    private CategoryLookup? _categories;
    // One data/display snapshot owns its row identities; filters do not repeatedly format the same money and captions.
    private SnapshotProjectionCache<LedgerEntry, Guid, EntryRow>? _rows;
    private System.Globalization.CultureInfo? _rowCulture;
    private System.Globalization.CultureInfo? _rowLanguage;
    private bool _rowDark;
    private IReadOnlyCollection<DisplayUnit> _rowUnits = [];

    private CancellationTokenSource? _searchDelay;
    // Existing filter batches suppress intermediate refreshes independently of the asynchronous snapshot cover.
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

    // The rest of a report's scope (ZEX-S0601): currency, accounts and confirmed only, so the list matches the number.
    private string? _currency;
    private IReadOnlyCollection<Guid>? _scopeAccounts;
    private bool _confirmedOnly;

    public TransactionsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, UndoService undo, IAppInteraction interaction)
    {
        _store = store; _interaction = interaction;
        _quick = new(store, translator, interaction);
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        Undo = undo;
        _undoAction = new TransactionUndoViewModel(undo, interaction, LoadAsync, () => _active && Loading.IsReady, translator);
        _bulk = new BulkTransactionsViewModel(store, translator, interaction, undo, async () => { await LoadAsync(); UpdateUndo(); }, () => _active && Loading.IsReady);
        _bulk.Changed += (_, _) => SynchronizeSelection();
        SearchText = string.Empty;
        PeriodNames = [translator["Period_ThisMonth"], translator["Period_LastMonth"], translator["Period_ThisYear"], translator["Period_All"]];
        KindNames = [translator["KindFilter_All"], translator["KindFilter_Expenses"], translator["KindFilter_Income"], translator["KindFilter_Transfers"]];
        AccountOptions = [];
    }

    public UndoService Undo { get; }
    /// <summary>Gets the actual original operation, never a deletion caption for an aggregate adjustment.</summary>
    public string UndoText => _translator[Undo.NoticeKind == UndoNoticeKind.AggregateAdjusted ? "Aggregate_UndoNotice" : "Tx_Deleted"];

    public IReadOnlyList<string> PeriodNames { get; }

    public IReadOnlyList<string> KindNames { get; }

    /// <summary>Gets the entries grouped by day; replaced as a whole, so the list lays out once per change of the filter.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<EntryDayGroup> Days { get; set; } = [];

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

    /// <summary>Gets or sets a value indicating whether the less used filters (account, category, saved filters, selection) are shown.</summary>
    [ObservableProperty]
    public partial bool IsFilterOpen { get; set; }

    [RelayCommand]
    private void ToggleFilter() => IsFilterOpen = !IsFilterOpen;

    [ObservableProperty]
    public partial IReadOnlyList<AccountFilterOption> CategoryOptions { get; set; } = [];

    [ObservableProperty]
    public partial AccountFilterOption? SelectedCategory { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowReviewFilter))]
    public partial bool UnreviewedOnly { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowReviewFilter))]
    public partial int UnreviewedCount { get; set; }

    /// <summary>Keeps an active review filter visible even after its final original row is confirmed.</summary>
    public bool ShowReviewFilter => UnreviewedOnly || UnreviewedCount > 0;

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

    /// <summary>Gets the scope of the report a drill-down came from, e.g. "October · EUR · Accounts in totals" (ZEX-S0601).</summary>
    [ObservableProperty]
    public partial string? ScopeNote { get; set; }

    [ObservableProperty]
    public partial string? SummaryText { get; set; }

    [ObservableProperty]
    public partial string? CustomPeriodText { get; set; }

    private bool _drillDown;

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
            // The routed page owns its scope; a retained report must not retarget it by mutating its caller arrays.
            _categoryIds = query.TryGetValue("categories", out var categories) && categories is IReadOnlyCollection<Guid> categoryIds ? categoryIds.ToArray() : null;
            CategoryFilterName = _categoryIds is null ? null : query.TryGetValue("categoryName", out var name) ? name?.ToString() : null;
            _inTotalsOnly = query.TryGetValue("inTotals", out var inTotals) && inTotals is true;
            _currency = query.TryGetValue("currency", out var currency) ? currency as string : null;
            _scopeAccounts = query.TryGetValue("accounts", out var scope) && scope is IReadOnlyCollection<Guid> accountIds ? accountIds.ToArray() : null;
            _confirmedOnly = query.TryGetValue("confirmedOnly", out var confirmed) && confirmed is true;
            ScopeNote = query.TryGetValue("scope", out var note) ? note as string : null;
            UnreviewedOnly = false;
            SearchText = query.TryGetValue("search", out var search) && search is string text ? text : string.Empty;

            // A drill-down shows exactly the tapped number: earlier account and category choices do not apply (AT-50).
            _drillDown = true;
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
        OnPropertyChanged(nameof(UndoText));
        ShowUndo = Undo.CanUndo;
        if (ShowUndo)
        {
            Task.Delay(Undo.Remaining).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => ShowUndo = Undo.CanUndo), TaskScheduler.Default);
        }
    }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() { RetireRead(); return LoadAsync(); }
    private string DisplayIdentity() => $"{_localization.CurrentLanguage.CultureName}|{_localization.CurrentCulture.Name}|{_localization.CurrentCalendar}|{_localization.CurrentDigits}|{_localization.FormattingCultureName}|{Palette.IsDark}";
    private void OnDisplayChanged(object? sender, EventArgs args) { RetireRead(); if (_active) _ = LoadAsync(); }
    private void RetireRead()
    {
        ++_publicationRevision; _published = null; _quick.CoverTransactionCenter(); _bulk.Retire();
        var retired = _lifetime; _lifetime = new(); Loading = new(); retired.Cancel(); retired.Dispose();
        _searchDelay?.Cancel();
    }

    /// <summary>Gets the initial/reload cover and retry state, separate from a bulk-write dialog.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Retires page-owned publication and native saved-query responses when the original page leaves.</summary>
    public void SetActive(bool active)
    {
        _localization.Changed -= OnDisplayChanged;
        if (active) _localization.Changed += OnDisplayChanged;
        if (_active == active) return;
        _active = active;
        if (active) return;
        ++_publicationRevision; _published = null; _quick.Retire(); _bulk.Retire();
        var retired = _lifetime; _lifetime = new(); Loading = new();
        retired.Cancel(); retired.Dispose(); _searchDelay?.Cancel();
    }

    /// <summary>Reads one snapshot and presents it before enabling filters or exposing result rows.</summary>
    public async Task LoadAsync()
    {
        if (!_active) return;
        var display = DisplayIdentity();
        var revision = _publicationRevision; var owner = Loading; var token = _lifetime.Token;
        void Current()
        {
            token.ThrowIfCancellationRequested();
            if (!_active || display != DisplayIdentity() || revision != _publicationRevision || !ReferenceEquals(owner, Loading))
                throw new OperationCanceledException("The original Transactions publication retired.", token);
        }
        try
        {
            await owner.RunAsync(() => ReadSnapshotAsync(Current, token), () => { Current(); PresentSnapshot(); Current(); });
        }
        catch (OperationCanceledException) when (!_active || revision != _publicationRevision || display != DisplayIdentity()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Appearance and retained navigation share one failing task; the page owns one inline error, not two native alerts.
            if (_active && revision == _publicationRevision && display == DisplayIdentity() && ReferenceEquals(owner, Loading))
                ReadFeedback = _quick.IsBusy && _quick.Accepted || _bulk.IsBusy && _bulk.Accepted
                    || _undoAction.IsExecuting && Undo.LastAccepted
                    ? _translator["Occurrence_SavedRefreshFailed"]
                    : CommercialFailureMessage.OrFallback(exception, _translator, "Error_Unexpected");
        }
    }

    private async Task ReadSnapshotAsync(Action current, CancellationToken token)
    {
        current(); ReadFeedback = null; current();
        // D-635: capture all original references and complete ledger fields before changing any visible filter.
        _published = null; _quick.CoverTransactionCenter();
        var snapshot = await _store.ReadTransactionsAsync(path =>
        { current(); _originalFile ??= path; return _originalFile == path; }, token);
        current();
        await _store.ValidateTransactionsAsync(snapshot, token);
        current();
        var accounts = snapshot.Resources.Accounts;
        var categories = new CategoryLookup(snapshot.Resources.Categories, _translator);
        snapshot.Resources.EnsureCurrent();
        current();
        _startDay = snapshot.Resources.Settings?.MonthStartDay ?? 1;
        _accounts = accounts.ToDictionary(a => a.Id);
        _categories = categories;
        _entries = snapshot.Entries;
        _published = snapshot;
        _quick.PublishTransactionCenter(snapshot.Resources);
        current();
        _rows = null; // Even unchanged ids must reflect fresh amounts, categories, review states and account names.
        _bulk.Load(_entries, [.. _categories.All], _categories.Name, snapshot);
        current();

        var selected = _pendingAccount ?? (_drillDown ? null : SelectedAccount?.Id);
        _pendingAccount = null;
        AccountOptions = [new AccountFilterOption(null, _translator["Accounts_AllAccounts"]), .. accounts.Select(a => new AccountFilterOption(a.Id, a.Name))];
        current(); snapshot.Resources.EnsureCurrent();
        SelectedAccount = AccountOptions.FirstOrDefault(o => o.Id == selected) ?? AccountOptions[0];
        current(); snapshot.Resources.EnsureCurrent();
        ShowAccountFilter = accounts.Count > 1;
        current(); snapshot.Resources.EnsureCurrent();

        // Category filter (TX-05); a main category includes its sub-categories.
        var selectedCategory = _drillDown ? null : SelectedCategory?.Id;
        _drillDown = false;
        current();
        CategoryOptions = [new AccountFilterOption(null, _translator["Tx_AllCategories"]), .. _categories.All
            .Where(c => !c.IsArchived && c.ParentId is null)
            .OrderBy(c => c.Kind).ThenBy(c => c.SortOrder)
            .Select(c => new AccountFilterOption(c.Id, CategoryLookup.NameOf(c, _translator)))];
        current();
        SelectedCategory = CategoryOptions.FirstOrDefault(o => o.Id == selectedCategory) ?? CategoryOptions[0];
        current(); snapshot.Resources.EnsureCurrent();
        HasNoAccounts = accounts.Count == 0;
        current(); snapshot.Resources.EnsureCurrent();
        HasNoEntriesAtAll = _entries.Count == 0;
        current(); snapshot.Resources.EnsureCurrent();
        UnreviewedCount = _entries.Count(e => e.Review == ReviewState.Unreviewed);
        current(); snapshot.Resources.EnsureCurrent();
        PublishSavedFilters(); current(); snapshot.Resources.EnsureCurrent();
    }

    partial void OnPeriodIndexChanged(int value)
    {
        // Choosing a period chip replaces a range that came from a report.
        if (value >= 0 && !_loading && !Loading.IsLoading)
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
        if (_loading || Loading.IsLoading || _categories is null)
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
        if (!_loading && !Loading.IsLoading) { ++_queryRevision; _bulk.Retire(); }
        // Debounce typing so that large lists stay responsive.
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        var token = _searchDelay.Token;
        Task.Delay(250, token).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(Refresh), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    private void Refresh()
    {
        if (!_loading && !Loading.IsLoading) { ++_queryRevision; _bulk.Retire(); }
        if (!_active || _published is null || _loading || !Loading.IsReady || _categories is null)
        {
            return;
        }

        PresentSnapshot();
    }

    // Publishing runs while the page remains covered; property changes cannot start a partial filter refresh.
    private void PresentSnapshot()
    {
        OnPropertyChanged(nameof(UndoText));
        var categories = _categories ?? throw new InvalidOperationException("The category snapshot must be read before presenting transactions.");
        var (from, to) = PeriodRange();
        var filter = new EntryFilter(from, to, (KindFilter)KindIndex, SelectedAccount?.Id, _categoryIds, UnreviewedOnly, SearchText, _inTotalsOnly, _currency, _scopeAccounts, _confirmedOnly);
        var culture = _localization.CurrentCulture;
        var units = DisplayUnits.All;
        if (_rows is null || !ReferenceEquals(_rowCulture, culture) || !ReferenceEquals(_rowLanguage, _translator.Culture)
            || _rowDark != Palette.IsDark || !_rowUnits.SequenceEqual(units))
        {
            var presenter = new EntryPresenter(_accounts, categories, _translator, culture);
            _rows = new(entry => entry.Id, presenter.Row);
            _rowCulture = culture;
            _rowLanguage = _translator.Culture;
            _rowDark = Palette.IsDark;
            _rowUnits = units;
        }

        var matching = EntrySearch.Apply(_entries, filter, id => categories.Name(id), _accounts).ToList();
        var filtered = _categoryIds is not null || _customPeriod is not null || KindIndex != 0 || !string.IsNullOrWhiteSpace(SearchText);
        SummaryText = filtered && matching.Count > 0
            ? TransactionSummary.Format(matching, _accounts, _translator, culture)
            : null;

        // One new list instead of clearing and adding day by day: each added group made the list lay out again, which was
        // slow for "All" with thousands of entries.
        var days = new List<EntryDayGroup>();
        foreach (var day in EntrySearch.ByDay(matching, _accounts))
        {
            var net = string.Join("  ", day.Net.Where(n => n.Value != 0).Select(n => MoneyText.Format(n.Value, n.Key, culture, showPlus: true)));
            days.Add(new EntryDayGroup(_dates.Format(day.Date, DateFormatStyle.Long), net, day.Entries.Select(e =>
            {
                var row = _rows.Get(e);
                row.Selection.IsSelected = _bulk.IsSelected(e.Id);
                return row;
            })));
        }

        // D-110: keep the actual native source only when every caption, boundary and ordered row object matches.
        // Selection remains observable on these same rows; fresh data/display contexts must publish new objects.
        if (!SnapshotGroupIdentity.Matches<EntryDayGroup, EntryRow>(Days, days,
            static group => group.Header, static group => group.NetText, static group => group))
        {
            Days = days;
        }
        IsEmpty = days.Count == 0;
    }

    private (DateOnly? From, DateOnly? To) PeriodRange()
    {
        if (_customPeriod is { } custom)
        {
            return (custom.From, custom.To);
        }

        var calendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var (year, month) = PeriodMath.MonthOf(today, calendar, _startDay);
        switch (PeriodIndex)
        {
            case 0:
                var current = PeriodMath.MonthRange(year, month, calendar, _startDay);
                return (current.First, current.Last);
            case 1:
                var (py, pm) = PeriodMath.Previous(year, month);
                var previous = PeriodMath.MonthRange(py, pm, calendar, _startDay);
                return (previous.First, previous.Last);
            case 2:
                var range = PeriodMath.YearRange(today, calendar);
                return (range.First, range.Last);
            default:
                return (null, null);
        }
    }

    [RelayCommand]
    private Task AddAsync() => Failures.GuardAsync(async () =>
    {
        if (!CanUsePublication || _published is not { } source) return;
        var query = _queryRevision;
        await _store.ValidateTransactionsAsync(source);
        if (!CanUsePublication || !ReferenceEquals(source, _published) || query != _queryRevision) return;
        await _interaction.NavigateAsync(HasNoAccounts ? AppShell.AccountEditorRoute : AppShell.EntryEditorRoute);
    });

    private bool CanUsePublication => _active && Loading.IsReady && _published is not null;

    [RelayCommand]
    private Task OpenAsync(EntryRow row) => Failures.GuardAsync(async () =>
    {
        if (!CanUsePublication || _published is not { } source || row is null
            || !Days.SelectMany(day => day).Any(original => ReferenceEquals(original, row))) return;
        var query = _queryRevision;
        await _store.ValidateTransactionsAsync(source);
        if (!CanUsePublication || !ReferenceEquals(source, _published) || query != _queryRevision) return;
        if (IsSelecting) { ToggleSelection(row); return; }
        await _interaction.NavigateAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
    });

    // Bulk operations (F2-TX-04): select several entries, then mark them reviewed, change their category, add a tag or
    // delete them with undo. Every change goes through the normal validation.
    private readonly BulkTransactionsViewModel _bulk;

    [ObservableProperty]
    public partial bool IsSelecting { get; set; }

    [ObservableProperty]
    public partial string? SelectionText { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [RelayCommand]
    private void StartSelecting() { if (CanUsePublication) _bulk.Start(); }

    [RelayCommand]
    private void StopSelecting() => _bulk.Stop();

    [RelayCommand]
    private void SelectAll() { if (CanUsePublication) _bulk.SelectAll(Days.SelectMany(d => d).Select(r => r.Id)); }

    private void ToggleSelection(EntryRow row) => _bulk.Toggle(row.Id);

    // Preserve the existing XAML bindings while the tested flow owns all selection and bulk-write decisions.
    private void SynchronizeSelection()
    {
        IsSelecting = _bulk.IsSelecting; HasSelection = _bulk.HasSelection; SelectionText = _bulk.SelectionText; IsBusy = _bulk.IsBusy;
        foreach (var row in Days.SelectMany(d => d)) { row.Selection.IsSelected = _bulk.IsSelected(row.Id); }
    }

    [RelayCommand]
    private Task BulkReviewedAsync() => _bulk.ReviewedCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task BulkCategoryAsync() => _bulk.CategoryCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task BulkTagAsync() => _bulk.TagCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task BulkDeleteAsync() => _bulk.DeleteCommand.ExecuteAsync(null);

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

    private Task LoadSavedFiltersAsync() => LoadAsync();

    private void PublishSavedFilters()
    {
        _savedFilters = [.. _quick.Review!.Filters.OrderBy(f => f.SortOrder).ThenBy(f => f.Name)];
        SavedFilters.Clear();
        foreach (var filter in _savedFilters)
        {
            SavedFilters.Add(filter);
        }

        HasSavedFilters = SavedFilters.Count > 0;
    }

    // Saved filters (REP-08): the current combination under a name, applied again with one tap; entries never change.
    [RelayCommand]
    private Task SavedFilterMenuAsync()
    {
        var query = _queryRevision; var source = _published;
        return _quick.FilterMenuAsync(new SavedFilter
    {
        Name = string.Empty,
        Period = Math.Max(0, PeriodIndex), From = _customPeriod?.From, To = _customPeriod?.To,
        Kind = KindIndex, AccountId = SelectedAccount?.Id,
        CategoryIds = _categoryIds is null ? [] : [.. _categoryIds], CategoryName = CategoryFilterName,
        UnreviewedOnly = UnreviewedOnly, InTotalsOnly = _inTotalsOnly,
        Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
    }, LoadSavedFiltersAsync, () => _active && Loading.IsReady && source is not null
        && ReferenceEquals(source, _published) && query == _queryRevision);
    }

    [RelayCommand]
    private Task ApplySavedFilterAsync(SavedFilter filter) => Failures.GuardAsync(async () =>
    {
        if (!_active || !Loading.IsReady || _published is null || !_savedFilters.Any(original => ReferenceEquals(original, filter))) return;
        var source = _published; var query = _queryRevision;
        await _store.ValidateTransactionsAsync(source);
        if (!CanUsePublication || !ReferenceEquals(source, _published) || query != _queryRevision) return;
        source.Resources.EnsureCurrent();
        _loading = true;
        try
        {
            // D-109: a named filter replaces the combination; unrelated report-only scope must not restrict it.
            ClearReportScopeCore();
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
            var ids = filter.CategoryIds.Count > 0 ? filter.CategoryIds.ToArray() : null;
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
    });

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
        ClearReportScopeCore();
        CategoryFilterName = null;
    }

    /// <summary>Removes transient report restrictions without changing the current named/category/date filters.</summary>
    private void ClearReportScopeCore()
    {
        _currency = null;
        _scopeAccounts = null;
        _confirmedOnly = false;
        ScopeNote = null;
    }

    /// <summary>Gets the guarded Undo command bound by the existing transaction notice.</summary>
    public IAsyncRelayCommand UndoDeleteCommand => _undoAction.UndoDeleteCommand;
}
