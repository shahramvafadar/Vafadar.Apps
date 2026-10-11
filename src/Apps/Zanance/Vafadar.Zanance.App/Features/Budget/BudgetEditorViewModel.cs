using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using BudgetModel = Vafadar.Zanance.Core.Budgets.Budget;

namespace Vafadar.Zanance.App.Features.Budget;

/// <summary>An original category limit; its raw input retains the culture in which the user entered it.</summary>
public sealed partial class LimitInput(Guid categoryId, string name, Symbol icon) : ObservableObject
{
    /// <summary>Gets the original category identity, including hidden or unavailable references.</summary>
    public Guid CategoryId { get; } = categoryId;
    /// <summary>Gets the complete live translated original caption.</summary>
    [ObservableProperty] public partial string Name { get; set; } = name;
    /// <summary>Gets the original category glyph.</summary>
    public Symbol Icon { get; } = icon;
    /// <summary>Gets or sets the explicit raw amount draft; blank removes this category limit.</summary>
    [ObservableProperty] public partial string Text { get; set; } = string.Empty;
    /// <summary>Gets complete field-local validation feedback.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets the suggested amount, never applied without an explicit action.</summary>
    public long? Suggested { get; set; }
    /// <summary>Gets the current translated average explanation.</summary>
    [ObservableProperty] public partial string? SuggestionText { get; set; }
    internal CultureInfo InputCulture { get; set; } = CultureInfo.InvariantCulture;
}

/// <summary>An original account reference, retained even when archived or unavailable.</summary>
public sealed partial class ScopeAccount(Guid id, string name) : ObservableObject
{
    /// <summary>Gets the original financial identity.</summary>
    public Guid Id { get; } = id;
    /// <summary>Gets its complete current caption.</summary>
    [ObservableProperty] public partial string Name { get; set; } = name;
    /// <summary>Gets or sets the explicit account-scope draft.</summary>
    [ObservableProperty] public partial bool IsIncluded { get; set; } = true;
}

/// <summary>Covered original budget drafts, retained edits and paired actual-file writes; no ledger posting.</summary>
public sealed partial class BudgetEditorViewModel : ViewModelBase, IUnsavedChanges, IThemeAware
{
    private readonly ZananceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IAppInteraction _interaction;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly Guid _newId = Guid.CreateVersion7();
    private BudgetEditorReview? _review;
    private BudgetModel? _budget;
    private CategoryLookup? _lookup;
    private BudgetWorkPolicy _work = BudgetWorkPolicy.Inactive;
    private int _year, _month;
    private PeriodCalendar _calendar;
    private BudgetPeriod _period;
    private DateOnly _periodStart;
    private string _currency = "EUR";
    private string? _openedScope;
    private Guid? _id;
    private IReadOnlyDictionary<string, object>? _openingQuery;
    private bool _hasQuery, _idSpecified, _loading = true, _publishing, _committed, _needsReload, _pendingReview;
    private Draft? _baseline;
    private CultureInfo _totalCulture;
    private IReadOnlyList<LedgerEntry> _entries = [];
    private IReadOnlyList<(DateOnly First, DateOnly Last)> _pastPeriods = [];
    private BudgetSuggestion _suggestion = BudgetSuggestion.None;

    /// <summary>Creates an entirely covered form without starting a read or writing defaults.</summary>
    public BudgetEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction)
    {
        _store = store; _translator = translator; _localization = localization; _interaction = interaction;
        _totalCulture = localization.CurrentCulture;
        PropertyChanged += (_, args) => { if (args.PropertyName == nameof(IsBusy)) RefreshCapabilities(); };
        RefreshDisplay();
    }
    /// <summary>Gets every original or retained draft category limit.</summary>
    public ObservableCollection<LimitInput> Limits { get; } = [];
    /// <summary>Gets current eligible and complete retained original account references.</summary>
    public ObservableCollection<ScopeAccount> ScopeAccounts { get; } = [];
    [ObservableProperty] public partial string TotalText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool AlertsEnabled { get; set; } = true;
    [ObservableProperty] public partial string? CurrencyCode { get; set; }
    [ObservableProperty] public partial string? Error { get; set; }
    [ObservableProperty] public partial string? TotalError { get; set; }
    [ObservableProperty] public partial bool IsAdvanced { get; set; }
    [ObservableProperty] public partial string? HiddenLimitsText { get; set; }
    [ObservableProperty] public partial string? TotalSuggestionText { get; set; }
    [ObservableProperty] public partial bool HasCategorySuggestions { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> RolloverNames { get; set; } = [];
    [ObservableProperty] public partial int RolloverIndex { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> MethodNames { get; set; } = [];
    [ObservableProperty] public partial int MethodIndex { get; set; }
    [ObservableProperty] public partial bool IsWeekly { get; set; }
    /// <summary>Gets whether the complete initial/reloaded form publication succeeded.</summary>
    [ObservableProperty] public partial bool IsReady { get; set; }
    /// <summary>Gets the current saved values when a reload retained explicit dirty input.</summary>
    [ObservableProperty] public partial string? ReloadedSavedSummary { get; set; }
    /// <summary>Gets whether the saved original method is Flex.</summary>
    public bool IsFlex => MethodIndex == (int)BudgetMethod.Flex;
    /// <summary>Gets the existing Simple/Advanced method visibility.</summary>
    public bool ShowMethods => IsAdvanced && !IsWeekly;
    /// <summary>Gets the existing category-limit visibility; retained hidden limits remain in the draft.</summary>
    public bool ShowCategoryLimits => IsAdvanced && !IsFlex;
    /// <summary>Gets whether optional fill suggestions are visible.</summary>
    public bool ShowFillSuggestions => ShowCategoryLimits && HasCategorySuggestions;
    /// <summary>Gets whether a load or failed publication must cover the entire form.</summary>
    public bool IsCovered => !IsReady;
    /// <summary>Gets whether the covered form is actively loading.</summary>
    public bool IsLoading => _loading;
    /// <summary>Gets whether this form has an explicit changed-saved-value review waiting.</summary>
    public bool RequiresReview => _pendingReview;
    /// <summary>Gets whether original corrections may be edited.</summary>
    public bool CanInteract => IsReady && !_loading && !IsBusy && !_committed && _work.CanCorrect;
    /// <summary>Gets whether the draft can be closed without interrupting publication or a write.</summary>
    public bool CanLeave => !_loading && !IsBusy;
    /// <summary>Gets whether an explicit reload can be requested; it never resubmits a Save.</summary>
    public bool CanReload => !_loading && !IsBusy && !_committed;
    /// <summary>Gets separate new-tool and retained-method correction affordances.</summary>
    public bool CanChooseMethod => CanInteract && (_work.CanConfigure(BudgetMethod.Envelopes, BudgetRollover.None, _period)
        || _budget?.Method is BudgetMethod.Envelopes or BudgetMethod.Flex || MethodIndex != (int)(_budget?.Method ?? BudgetMethod.Limits));
    /// <summary>Gets separate new-tool and retained-rollover correction affordances.</summary>
    public bool CanChooseRollover => CanInteract && (_work.CanConfigure(BudgetMethod.Limits, BudgetRollover.Surplus, _period)
        || _budget is { Rollover: not BudgetRollover.None } || RolloverIndex != (int)(_budget?.Rollover ?? BudgetRollover.None));
    /// <summary>Gets current feature feedback independently of preserved limits and historical amounts.</summary>
    public string? WorkText => IsReady && (_budget is null ? !DraftAllowed() : !_work.CanUse(_budget.Id)) ? _translator["Budget_WorkUnavailable"] : null;
    /// <summary>Gets the complete close caption after a successful write, including failed return navigation.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    /// <summary>Gets whether a complete current review can save this configuration; the writer rechecks capacity.</summary>
    public bool CanSave => CanInteract && !_needsReload && !_pendingReview && DraftAllowed();
    /// <inheritdoc />
    public bool IsDirty => !_committed && _baseline is not null && Fingerprint(CaptureDraft()) != Fingerprint(_baseline);

    /// <summary>Retains one explicit identity; native empty/repeated modal-attachment queries never erase a draft.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (_hasQuery)
        {
            if (query.Count == 0) return;
            if (query.Any(pair => pair.Key is "id" or "scope" or "year" or "month" or "calendar" or "period" or "start" or "currency"
                && (_openingQuery is null || !_openingQuery.TryGetValue(pair.Key, out var original) || !Equals(original, pair.Value))))
                throw new InvalidOperationException("A retained budget draft cannot be rebound to another identity.");
            return;
        }
        _openingQuery = new Dictionary<string, object>(query);
        _hasQuery = true; _idSpecified = query.ContainsKey("id"); _id = query.TryGetValue("id", out var identity) && identity is Guid key ? key : null;
        _year = query.TryGetValue("year", out var year) && year is int y ? y : 0;
        _month = query.TryGetValue("month", out var month) && month is int m ? m : 0;
        _calendar = query.TryGetValue("calendar", out var calendar) && calendar is PeriodCalendar c ? c : PeriodCalendar.Gregorian;
        _period = query.TryGetValue("period", out var period) && period is BudgetPeriod p ? p : BudgetPeriod.Month;
        _periodStart = query.TryGetValue("start", out var start) && start is DateOnly date ? date : default;
        _currency = query.TryGetValue("currency", out var currency) && currency is string code ? code : "EUR";
        _openedScope = query.TryGetValue("scope", out var origin) && origin is string original ? original : null;
        await LoadAsync(false);
    }
    /// <summary>Starts publication only when navigation supplied no identity; construction remains read-free.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (!_hasQuery) await ApplyQueryAsync(new Dictionary<string, object>());
        else { await _loadGate.WaitAsync(); _loadGate.Release(); }
    }
    private async Task LoadAsync(bool keepDraft)
    {
        await _loadGate.WaitAsync();
        var draft = keepDraft ? CaptureDraft() : null; var baseline = _baseline; var prior = _review;
        _loading = true; IsReady = false; Error = null; RefreshCapabilities();
        try
        {
            var review = await _store.GetBudgetEditorReviewAsync();
            if (_openedScope is not null && _openedScope != review.Choices.ReminderScope)
                throw new InvalidOperationException("This budget draft belongs to another profile file.");
            var settings = review.Settings ?? new ZananceSettings();
            if (_year == 0 && _period == BudgetPeriod.Month && _id is null)
            {
                _calendar = settings.BudgetCalendar;
                (_year, _month) = PeriodMath.MonthOf(review.Choices.Today, _calendar, settings.MonthStartDay);
            }
            var original = _id is { } id ? review.Choices.Items.SingleOrDefault(item => item.Id == id)?.ToBudget()
                : _idSpecified ? null : FindPeriod(review.Choices)?.ToBudget();
            if (_id is not null && original is null) throw new InvalidOperationException("The original budget no longer exists.");
            if (prior is not null && original is not null && (_budget?.CurrencyCode != original.CurrencyCode
                || _budget.Period != original.Period || _budget.Year != original.Year || _budget.Month != original.Month
                || _budget.Calendar != original.Calendar || _budget.PeriodStart != original.PeriodStart))
                throw new InvalidOperationException("The saved budget identity changed; reopen the original period.");
            var target = original ?? Configuration();
            var periods = BudgetPeriods.Before(target.Period, target.Year, target.Month, target.PeriodStart,
                target.Calendar, settings.MonthStartDay, BudgetSuggestions.PeriodsFor(target.Period));
            var entries = await _store.GetEntriesAsync(periods[^1].First, periods[0].Last);
            await _store.ValidateBudgetEditorReviewAsync(review);

            // Publish all complete references and defaults synchronously before enabling any native field.
            _publishing = true; _openedScope ??= review.Choices.ReminderScope; _review = review; _work = review.Choices.Work;
            _budget = original; if (original is not null) _id = original.Id;
            _year = target.Year; _month = target.Month; _calendar = target.Calendar; _period = target.Period; _periodStart = target.PeriodStart;
            _currency = target.CurrencyCode; _entries = entries; _pastPeriods = periods;
            _lookup = new CategoryLookup(review.Categories, _translator); IsAdvanced = settings.Shows(Feature.BudgetOptions);
            IsWeekly = _period != BudgetPeriod.Month; CurrencyCode = _currency;
            _totalCulture = _localization.CurrentCulture;
            TotalText = original?.TotalLimit is { } total ? MoneyText.ForInput(total, _currency, _totalCulture) : string.Empty;
            AlertsEnabled = original?.AlertsEnabled ?? true; RolloverIndex = (int)(original?.Rollover ?? BudgetRollover.None);
            MethodIndex = (int)(original?.Method ?? BudgetMethod.Limits);
            ScopeAccounts.Clear();
            var savedAccounts = original?.AccountIds ?? [];
            var accountIds = review.Accounts.Where(item => !item.IsArchived && item.IncludeInTotals && item.CurrencyCode == _currency)
                .Select(item => item.Id).Concat(savedAccounts).Concat(draft?.Accounts.Keys.AsEnumerable() ?? []).Distinct().ToArray();
            foreach (var accountId in accountIds)
            {
                var row = new ScopeAccount(accountId, AccountName(accountId)) { IsIncluded = savedAccounts.Count == 0 || savedAccounts.Contains(accountId) };
                row.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(row.IsIncluded) && !_publishing && ScopeAccounts.Contains(row)) DraftChanged(); };
                ScopeAccounts.Add(row);
            }
            Limits.Clear();
            var categoryIds = review.Categories.Where(item => item.Kind == CategoryKind.Expense && item.ParentId is null && !item.IsArchived)
                .OrderBy(item => item.SortOrder).Select(item => item.Id).Concat(original?.CategoryLimits.Select(item => item.CategoryId) ?? [])
                .Concat(draft?.Limits.Keys.AsEnumerable() ?? []).Distinct().ToArray();
            foreach (var categoryId in categoryIds)
            {
                var row = new LimitInput(categoryId, CategoryName(categoryId), _lookup.Icon(categoryId)) { InputCulture = _localization.CurrentCulture };
                row.Text = original?.CategoryLimits.SingleOrDefault(item => item.CategoryId == categoryId) is { } limit
                    ? MoneyText.ForInput(limit.Limit, _currency, row.InputCulture) : string.Empty;
                row.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(row.Text) && !_publishing && Limits.Contains(row))
                    { row.InputCulture = _localization.CurrentCulture; row.Error = null; DraftChanged(); } };
                Limits.Add(row);
            }
            _baseline = CaptureDraft();
            if (draft is not null && baseline is not null) MergeDraft(draft, baseline);
            _needsReload = false; _pendingReview = prior is not null && baseline is not null && !prior.MatchesInputs(review) && IsDirty;
            ReloadedSavedSummary = _pendingReview ? SavedSummary(original) : null;
            RefreshDisplay(); IsReady = true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = _translator["Budget_LoadFailed"]; await _interaction.ShowFailureAsync(exception); }
        finally { _publishing = false; _loading = false; RefreshCapabilities(); _loadGate.Release(); }
    }
    private string SavedSummary(BudgetModel? original) => _translator.Format("Budget_ReloadedSaved",
        original?.TotalLimit is { } value ? MoneyText.Format(value, _currency, _localization.CurrentCulture) : _translator["Budget_NoLimit"],
        original is not { AccountIds.Count: > 0 } ? _translator["ActiveItems_BudgetAllAccounts"] : string.Join(", ", original.AccountIds.Select(AccountName)),
        _translator[original?.Method switch { BudgetMethod.Flex => "Budget_MethodFlex", BudgetMethod.Envelopes => "Budget_MethodEnvelopes", _ => "Budget_MethodLimits" }]
            + " · " + _translator[original?.Rollover switch { BudgetRollover.Surplus => "Rollover_Surplus", BudgetRollover.SurplusAndDeficit => "Rollover_Both", _ => "Rollover_None" }],
        string.Join(Environment.NewLine, original?.CategoryLimits.Select(limit => CategoryName(limit.CategoryId) + ": "
            + MoneyText.Format(limit.Limit, _currency, _localization.CurrentCulture)) ?? []));

    private BudgetChoiceItem? FindPeriod(BudgetChoiceSnapshot choices)
    {
        var rows = choices.Items.Where(item => item.CurrencyCode == _currency && item.Period == _period);
        if (_period == BudgetPeriod.Month) rows = rows.Where(item => item.Year == _year && item.Month == _month && item.Calendar == _calendar);
        else rows = rows.Where(item => _periodStart >= item.PeriodStart && _periodStart < item.PeriodStart.AddDays(BudgetPeriods.Days(_period)));
        return rows.SingleOrDefault();
    }
    private BudgetModel Configuration() => new(_budget?.Id ?? _newId)
    {
        // Weekly navigation retains month coordinates, but new stored weeks keep their canonical zero coordinates.
        Year = _budget?.Year ?? (_period == BudgetPeriod.Month ? _year : 0),
        Month = _budget?.Month ?? (_period == BudgetPeriod.Month ? _month : 0), Calendar = _calendar,
        Period = _period, PeriodStart = _periodStart, CurrencyCode = _currency,
        Method = (BudgetMethod)MethodIndex, Rollover = (BudgetRollover)RolloverIndex,
        AlertsEnabled = AlertsEnabled, AccountIds = AccountScope(),
        CreatedAt = _budget?.CreatedAt ?? default, UpdatedAt = _budget?.UpdatedAt ?? default,
    };
    private List<Guid> AccountScope()
    {
        if (_budget is not null && _baseline is not null
            && ScopeAccounts.All(row => _baseline.Accounts.TryGetValue(row.Id, out var selected) && selected == row.IsIncluded))
            return [.. _budget.AccountIds];
        if ((_budget?.AccountIds.Count ?? 0) == 0 && ScopeAccounts.All(row => row.IsIncluded)) return [];
        return [.. ScopeAccounts.Where(row => row.IsIncluded).Select(row => row.Id)];
    }
    private bool DraftAllowed()
    {
        if (_review is null || !Enum.IsDefined((BudgetMethod)MethodIndex) || !Enum.IsDefined((BudgetRollover)RolloverIndex)) return false;
        var target = Configuration();
        // D-153: sharing a selected definition or replacing its last current footprint does not need an extra slot.
        // Future periods remain independent of today's capacity; the paired writer rechecks the exact net change.
        return _review.Choices.CanSave(target);
    }
    private string CategoryName(Guid id) => _lookup?.Get(id) is { } category ? CategoryLookup.NameOf(category, _translator)
        : _translator.Format("Budget_MissingReference", id.ToString("N"));
    private string AccountName(Guid id) => _review?.Accounts.FirstOrDefault(item => item.Id == id) is { } account ? account.Name
        : _translator.Format("Budget_MissingReference", id.ToString("N"));
    /// <summary>Refreshes native-facing captions without changing indexes, raw amounts, scopes or input cultures.</summary>
    public void RefreshDisplay()
    {
        var publishing = _publishing; _publishing = true;
        try
        {
            RolloverNames = [_translator["Rollover_None"], _translator["Rollover_Surplus"], _translator["Rollover_Both"]];
            MethodNames = [_translator["Budget_MethodLimits"], _translator["Budget_MethodEnvelopes"], _translator["Budget_MethodFlex"]];
            var notes = new List<string>();
            if (!IsAdvanced && _budget?.CategoryLimits.Count is > 0) notes.Add(_translator.Format("Budget_HiddenLimits", _budget.CategoryLimits.Count));
            if (!IsAdvanced && RolloverIndex != 0) notes.Add(_translator[$"Rollover_Active_{(BudgetRollover)RolloverIndex}"]);
            if (!IsAdvanced && MethodIndex != 0) notes.Add(_translator.Format("Budget_MethodActive", MethodNames.ElementAtOrDefault(MethodIndex) ?? string.Empty));
            HiddenLimitsText = notes.Count == 0 ? null : string.Join(Environment.NewLine, notes);
            foreach (var row in Limits) row.Name = CategoryName(row.CategoryId);
            foreach (var row in ScopeAccounts) row.Name = AccountName(row.Id);
            if (_pendingReview) ReloadedSavedSummary = SavedSummary(_budget);
            CalculateSuggestions(); RefreshNativeColors();
        }
        finally { _publishing = publishing; }
        RefreshCapabilities();
    }
    Task IThemeAware.RefreshThemeAsync() { RefreshDisplay(); return Task.CompletedTask; }
    partial void RefreshNativeColors();
    private void CalculateSuggestions()
    {
        if (_review is null || _pastPeriods.Count == 0) return;
        _suggestion = BudgetSuggestions.Suggest(_review.Accounts, _entries, _review.Categories, _pastPeriods,
            _currency, AccountScope(), flexibleOnly: IsFlex);
        var total = _suggestion.Total; var key = (_period, total?.Months) switch
        { (BudgetPeriod.Month, 1) => "Budget_SuggestionOne", (BudgetPeriod.Month, _) => "Budget_Suggestion",
            (_, 1) => "Budget_SuggestionOnePeriod", (BudgetPeriod.Week, _) => "Budget_SuggestionWeeks", _ => "Budget_SuggestionFortnights" };
        TotalSuggestionText = total is null ? null : _translator.Format(key, MoneyText.Format(total.Suggested, _currency, _localization.CurrentCulture), total.Months);
        foreach (var row in Limits)
        {
            row.Suggested = _suggestion.Categories.TryGetValue(row.CategoryId, out var suggestion) ? suggestion.Suggested : null;
            row.SuggestionText = suggestion is null ? null : _translator.Format("Budget_CategoryAverage", MoneyText.Format(suggestion.Average, _currency, _localization.CurrentCulture));
        }
        HasCategorySuggestions = Limits.Any(row => row.Suggested is not null);
    }
    private void DraftChanged()
    {
        if (_publishing) return;
        CalculateSuggestions(); RefreshCapabilities();
    }
    partial void OnTotalTextChanged(string value) { if (!_publishing) { _totalCulture = _localization.CurrentCulture; TotalError = null; } DraftChanged(); }
    partial void OnAlertsEnabledChanged(bool value) => DraftChanged();
    partial void OnRolloverIndexChanged(int value) => DraftChanged();
    partial void OnMethodIndexChanged(int value) => DraftChanged();
    partial void OnIsAdvancedChanged(bool value) => RefreshCapabilities();
    partial void OnIsWeeklyChanged(bool value) => RefreshCapabilities();
    partial void OnHasCategorySuggestionsChanged(bool value) => OnPropertyChanged(nameof(ShowFillSuggestions));
    private void RefreshCapabilities()
    {
        foreach (var property in new[] { nameof(IsFlex), nameof(ShowMethods), nameof(ShowCategoryLimits), nameof(ShowFillSuggestions), nameof(IsCovered),
            nameof(IsLoading), nameof(RequiresReview), nameof(CanInteract), nameof(CanLeave), nameof(CanReload), nameof(CanChooseMethod),
            nameof(CanChooseRollover), nameof(WorkText), nameof(CloseText), nameof(CanSave), nameof(IsDirty) }) OnPropertyChanged(property);
        SaveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
        AcceptReviewCommand.NotifyCanExecuteChanged(); UseTotalSuggestionCommand.NotifyCanExecuteChanged(); FillSuggestionsCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync() { if (CanReload) { IsBusy = true; try { await LoadAsync(true); } finally { IsBusy = false; } } }
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void AcceptReview() { if (!CanInteract) return; _pendingReview = false; ReloadedSavedSummary = null; RefreshCapabilities(); }
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void UseTotalSuggestion() { if (CanInteract && _suggestion.Total is { } total) TotalText = MoneyText.ForInput(total.Suggested, _currency, _localization.CurrentCulture); }
    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void FillSuggestions()
    { if (CanInteract) foreach (var row in Limits.Where(row => row.Suggested is not null && string.IsNullOrWhiteSpace(row.Text)))
        row.Text = MoneyText.ForInput(row.Suggested!.Value, _currency, _localization.CurrentCulture); }
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave || _review is null) return;
        IsBusy = true; Error = TotalError = null;
        try
        {
            var currency = Currencies.Get(_currency); long? total = null; var valid = true;
            if (!string.IsNullOrWhiteSpace(TotalText))
            {
                if (!MoneyText.TryParse(TotalText, currency, _totalCulture, out var value) || value < 0) { TotalError = _translator["Amount_Invalid"]; valid = false; }
                else total = value;
            }
            var limits = new List<BudgetCategoryLimit>();
            foreach (var row in Limits)
            {
                row.Error = null; if (string.IsNullOrWhiteSpace(row.Text)) continue;
                if (!MoneyText.TryParse(row.Text, currency, row.InputCulture, out var amount) || amount < 0) { row.Error = _translator["Amount_Invalid"]; valid = false; }
                else limits.Add(new() { CategoryId = row.CategoryId, Limit = amount });
            }
            if (!valid) return;
            if (total is null && limits.Count == 0) { Error = _translator["Budget_NothingToSave"]; return; }
            if (ScopeAccounts.Count > 0 && ScopeAccounts.All(row => !row.IsIncluded)) { Error = _translator["Budget_NoAccountChosen"]; return; }
            var candidate = Configuration(); candidate.TotalLimit = total; candidate.CategoryLimits = limits;
            await _store.SaveBudgetAsync(candidate, review: _review);
            // Retire the draft before returning. A failed native return cannot repeat a committed financial configuration.
            _budget = candidate; _committed = true; _baseline = CaptureDraft(); RefreshCapabilities();
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            { Error = _translator["Budget_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _needsReload = true; Error = _translator["Budget_SaveFailed"]; await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; RefreshCapabilities(); }
    }
    [RelayCommand(CanExecute = nameof(CanLeave))]
    private async Task CancelAsync()
    {
        if (!CanLeave) return; IsBusy = true;
        try
        {
            if (!IsDirty || await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"],
                _translator["Common_Discard"], _translator["Common_KeepEditing"])) await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }
    private sealed record AmountDraft(string Text, CultureInfo Culture);
    private sealed record Draft(string Total, CultureInfo Culture, bool Alerts, int Method, int Rollover,
        Dictionary<Guid, AmountDraft> Limits, Dictionary<Guid, bool> Accounts);
    private Draft CaptureDraft() => new(TotalText, _totalCulture, AlertsEnabled, MethodIndex, RolloverIndex,
        Limits.ToDictionary(row => row.CategoryId, row => new AmountDraft(row.Text, row.InputCulture)), ScopeAccounts.ToDictionary(row => row.Id, row => row.IsIncluded));
    private static string Fingerprint(Draft draft) => DraftFingerprint.Create(draft.Total, draft.Alerts, draft.Method, draft.Rollover,
        string.Join(';', draft.Limits.OrderBy(row => row.Key).Select(row => row.Key.ToString("N") + ":" + row.Value.Text)),
        string.Join(';', draft.Accounts.OrderBy(row => row.Key).Select(row => row.Key.ToString("N") + ":" + row.Value)));
    private void MergeDraft(Draft draft, Draft baseline)
    {
        if (draft.Total != baseline.Total) { TotalText = draft.Total; _totalCulture = draft.Culture; }
        if (draft.Alerts != baseline.Alerts) AlertsEnabled = draft.Alerts;
        if (draft.Method != baseline.Method) MethodIndex = draft.Method;
        if (draft.Rollover != baseline.Rollover) RolloverIndex = draft.Rollover;
        foreach (var row in Limits)
            if (draft.Limits.TryGetValue(row.CategoryId, out var edited) && (!baseline.Limits.TryGetValue(row.CategoryId, out var old) || edited.Text != old.Text))
            { row.Text = edited.Text; row.InputCulture = edited.Culture; }
        foreach (var row in ScopeAccounts)
            if (draft.Accounts.TryGetValue(row.Id, out var edited) && (!baseline.Accounts.TryGetValue(row.Id, out var old) || edited != old)) row.IsIncluded = edited;
    }
}
