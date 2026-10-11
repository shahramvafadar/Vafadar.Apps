using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>An original holding type or location displayed in a quantity-goal picker.</summary>
public sealed record HoldingChoice(Guid? Id, string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>An original category identity displayed for a spending-cut draft.</summary>
public sealed record CutCategory(Guid Id, string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>Explicit goal/contribution drafts, complete original rules and actual-file reviewed writes; no ledger posting.</summary>
public sealed partial class GoalEditorViewModel : ViewModelBase, IThemeAware, IUnsavedChanges
{
    private static readonly (Frequency Frequency, int Interval)[] Schedules = [(Frequency.Monthly, 1), (Frequency.Weekly, 2), (Frequency.Weekly, 1)];
    private readonly ZananceStore _store;
    private readonly GoalStore _goals;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly HoldingStore _holdings;
    private readonly Holdings.HoldingText _holdingText;
    private readonly IAppInteraction _interaction;
    private readonly IGoalEditorNotifications _notifications;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly Goal _newGoal = new() { Name = string.Empty, CurrencyCode = "EUR" };
    private List<AssetType> _types = [];
    private List<AssetEvent> _events = [];
    private List<Category> _categories = [];
    private List<Account> _accounts = [];
    private List<LedgerEntry> _entries = [];
    private Goal? _existing;
    private ContributionPlan? _existingPlan;
    private GoalWorkSnapshot? _review;
    private GoalWorkPolicy _work = GoalWorkPolicy.Inactive;
    private ZananceSettings? _settings;
    private RecurrenceRule? _savedRule;
    private PeriodCalendar _calendarAtOpen;
    private Guid? _id;
    private string? _openedScope;
    private long _currentEarmarked;
    private bool _loading = true, _hasQuery, _needsReload, _committed, _pendingReview;
    private Draft? _baseline;
    private CultureInfo _targetCulture, _contributionCulture, _percentCulture;
    private string _targetUnit = string.Empty, _contributionUnit = string.Empty;
    private bool _targetNeedsReview, _contributionNeedsReview;
    private string? _appliedCut;

    /// <summary>Creates the covered form without reading a database or requesting native permission.</summary>
    public GoalEditorViewModel(ZananceStore store, GoalStore goals, Translator translator, ILocalizationService localization,
        IDateFormatter dates, TimeProvider time, HoldingStore holdings, Holdings.HoldingText holdingText,
        IAppInteraction interaction, IGoalEditorNotifications notifications)
    {
        _store = store; _goals = goals; _translator = translator; _localization = localization; _dates = dates;
        _time = time; _holdings = holdings; _holdingText = holdingText; _interaction = interaction; _notifications = notifications;
        _targetCulture = _contributionCulture = _percentCulture = localization.CurrentCulture;
        Title = translator["Goal_NewTitle"]; Name = AmountText = Note = string.Empty; CurrencyCode = "EUR";
        TargetDate = Today.AddYears(1); FirstDate = Today; PriorityIndex = (int)GoalPriority.Normal;
        PropertyChanged += (_, args) => { if (args.PropertyName == nameof(IsBusy)) { UpdatePreview(); RefreshCapabilities(); } };
        RefreshDisplay();
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial IReadOnlyList<string> TypeNames { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> ScheduleNames { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> MethodNames { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> PriorityNames { get; set; } = [];

    /// <summary>Gets the money accounts a balance goal can follow.</summary>
    public ObservableCollection<AccountChoice> Accounts { get; } = [];

    /// <summary>Gets the expense categories a spending cut can use.</summary>
    public ObservableCollection<CutCategory> CutCategories { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>Gets or sets the goal type: 0 = balance on one account, 1 = money set aside, 2 = quantity of a holding.</summary>
    [ObservableProperty]
    public partial int TypeIndex { get; set; }

    [ObservableProperty]
    public partial bool CanChangeType { get; set; } = true;

    [ObservableProperty]
    public partial bool IsBalanceGoal { get; set; } = true;

    /// <summary>Gets a value indicating whether the goal counts the quantity of a holding, e.g. 50 g of gold (ZEX-S0701).</summary>
    [ObservableProperty]
    public partial bool IsQuantityGoal { get; set; }

    /// <summary>Gets a value indicating whether the goal is in money (balance or money set aside).</summary>
    [ObservableProperty]
    public partial bool IsMoneyGoal { get; set; } = true;

    /// <summary>Gets a value indicating whether the currency can be chosen: money set aside only.</summary>
    [ObservableProperty]
    public partial bool ShowCurrencyChoice { get; set; }

    /// <summary>Gets a value indicating whether the plan's method can be chosen (Advanced money goals).</summary>
    [ObservableProperty]
    public partial bool ShowMethods { get; set; }

    public ObservableCollection<HoldingChoice> HoldingTypes { get; } = [];

    public ObservableCollection<HoldingChoice> HoldingLocations { get; } = [];

    [ObservableProperty]
    public partial HoldingChoice? HoldingType { get; set; }

    [ObservableProperty]
    public partial HoldingChoice? HoldingLocation { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> QuantityUnitNames { get; set; } = [];

    [ObservableProperty]
    public partial int QuantityUnitIndex { get; set; }

    [ObservableProperty]
    public partial bool HasQuantityUnitChoice { get; set; }

    /// <summary>Gets a value indicating whether the type can be chosen: new goals in Advanced (Simple creates balance goals).</summary>
    [ObservableProperty]
    public partial bool ShowTypeChooser { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial AccountChoice? Account { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial bool CurrencyLocked { get; set; }

    [ObservableProperty]
    public partial bool HasTargetDate { get; set; }

    [ObservableProperty]
    public partial DateOnly TargetDate { get; set; }

    [ObservableProperty]
    public partial int ScheduleIndex { get; set; }

    /// <summary>Gets or sets the first contribution date, e.g. the next pay day.</summary>
    [ObservableProperty]
    public partial DateOnly FirstDate { get; set; }

    /// <summary>Gets or sets optional notifications on contribution dates, off by default.</summary>
    [ObservableProperty]
    public partial bool ReminderEnabled { get; set; }

    /// <summary>Gets an explanation when this device cannot deliver the selected reminder.</summary>
    [ObservableProperty]
    public partial string? ReminderNote { get; set; }

    [ObservableProperty]
    public partial int MethodIndex { get; set; }

    [ObservableProperty]
    public partial string ContributionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PercentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CutCategory? CutCategory { get; set; }

    [ObservableProperty]
    public partial bool IsFixedMethod { get; set; } = true;

    [ObservableProperty]
    public partial bool IsShareMethod { get; set; }

    [ObservableProperty]
    public partial bool IsCutMethod { get; set; }

    [ObservableProperty]
    public partial bool ShowOnHome { get; set; }

    [ObservableProperty]
    public partial bool Protect { get; set; }

    [ObservableProperty]
    public partial int PriorityIndex { get; set; }

    [ObservableProperty]
    public partial string? IconKey { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; }

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets the preview lines: progress, the user's plan and the amount needed by the date (ZEX-GO14).</summary>
    [ObservableProperty]
    public partial string? PreviewProgress { get; set; }

    [ObservableProperty]
    public partial string? PreviewPlan { get; set; }

    [ObservableProperty]
    public partial string? PreviewRequired { get; set; }

    [ObservableProperty]
    public partial string? PreviewMethod { get; set; }

    /// <summary>Gets a value indicating whether "Apply to budget" is offered (a spending cut with a valid amount).</summary>
    [ObservableProperty]
    public partial bool CanApplyCut { get; set; }


    /// <summary>Gets whether the complete initial publication succeeded.</summary>
    [ObservableProperty]
    public partial bool IsReady { get; set; }
    /// <summary>Gets a field-local error for an explicit contribution amount/unit.</summary>
    [ObservableProperty]
    public partial string? ContributionError { get; set; }
    /// <summary>Gets a field-local percentage error.</summary>
    [ObservableProperty]
    public partial string? PercentError { get; set; }
    /// <summary>Gets a field-local recurrence error.</summary>
    [ObservableProperty]
    public partial string? RuleError { get; set; }
    /// <summary>Gets the original named rule, anchor and ending, including uncommon saved choices.</summary>
    [ObservableProperty]
    public partial string? ScheduleSummary { get; set; }
    /// <summary>Gets the explicit short-month behavior of the saved/entered monthly rule.</summary>
    [ObservableProperty]
    public partial string? ShortMonthNote { get; set; }
    /// <summary>Gets the actual next contribution dates in the rule calendar, without saving anything.</summary>
    public ObservableCollection<string> UpcomingDates { get; } = [];
    /// <summary>Gets the saved multiple/missing-category identity count, never a replacement selection.</summary>
    [ObservableProperty]
    public partial string? RetainedCategoriesText { get; set; }
    /// <summary>Gets the current saved values after an explicitly requested reload retained dirty edits.</summary>
    [ObservableProperty]
    public partial string? ReloadedSavedSummary { get; set; }

    /// <summary>Gets whether the body must remain covered while loading or after a failed publication.</summary>
    public bool IsCovered => !IsReady;
    /// <summary>Gets whether the covered body is actively loading.</summary>
    public bool IsLoading => _loading;
    /// <summary>Gets whether an explicit reload is required; it never resubmits a previous action.</summary>
    public bool CanReload => !IsBusy && !_loading && !_committed;
    /// <summary>Gets whether this form may close or confirm discard.</summary>
    public bool CanLeave => !IsBusy && !_loading;
    /// <summary>Gets whether published retained corrections may be edited.</summary>
    public bool CanInteract => IsReady && !IsBusy && !_loading && !_committed && _work.CanCorrect;
    /// <summary>Gets whether a reloaded draft needs the explicit review action before financial configuration writes.</summary>
    public bool RequiresReview => _pendingReview;
    /// <summary>Gets the original method caption even when Simple mode hides the method chooser.</summary>
    public string CurrentMethodText => MethodNames.ElementAtOrDefault(MethodIndex) ?? string.Empty;
    /// <summary>Gets whether a new paid contribution may be configured; paused configuration never resumes activity.</summary>
    private bool NewContribution => _existing is { } goal
        ? goal.State is GoalState.Active or GoalState.Paused && _work.CanConfigureContribution(goal.Id)
        : _work.CanCreateContribution(CurrentType);
    private bool NewReminder => _existing is { } goal
        ? goal.State is GoalState.Active or GoalState.Paused && _work.CanConfigureReminder(goal.Id)
        : _work.CanCreateReminder(CurrentType);
    private GoalType CurrentType => IsQuantityGoal ? GoalType.HoldingQuantity : IsBalanceGoal ? GoalType.AccountBalance : GoalType.Earmark;
    /// <summary>Gets whether retained contribution values or an unavailable dirty value may be corrected/cleared.</summary>
    public bool CanEditContribution => CanInteract && (NewContribution || GoalContributionPolicy.HasWork(_existingPlan)
        || ContributionText.Length > 0 || PercentText.Length > 0);
    /// <summary>Gets whether methods may be chosen/corrected without erasing an unavailable dirty selection.</summary>
    public bool CanChooseMethod => CanInteract && (NewContribution || GoalContributionPolicy.HasWork(_existingPlan)
        || MethodIndex != (int)(_existingPlan?.Method ?? ContributionMethod.FixedAmount));
    /// <summary>Gets whether a retained opt-in can be disabled or a permitted new one can be enabled.</summary>
    public bool CanEditReminder => CanInteract && (NewReminder || _existingPlan?.ReminderEnabled == true || ReminderEnabled);
    /// <summary>Gets independent work feedback; original lifecycle/rules/values remain unchanged.</summary>
    public string? WorkText => !IsReady || (NewContribution && _work.CanCorrect) ? null : _translator["Goal_ContributionUnavailable"];
    /// <summary>Gets the complete close/cancel caption, including a successfully committed form.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    /// <summary>Gets whether the complete draft has current new-work/correction rights and a usable review.</summary>
    public bool CanSave => CanInteract && !_needsReload && !_pendingReview && !_targetNeedsReview && !_contributionNeedsReview
        && (_existing is not null || _work.CanReopenGoal(CurrentType)) && DraftWorkAllowed();

    private void RefreshCapabilities()
    {
        foreach (var property in new[] { nameof(IsCovered), nameof(IsLoading), nameof(CanReload), nameof(CanLeave), nameof(CanInteract),
            nameof(RequiresReview), nameof(CanEditContribution), nameof(CanChooseMethod), nameof(CanEditReminder), nameof(WorkText),
            nameof(CloseText), nameof(CanSave), nameof(CurrentMethodText) }) OnPropertyChanged(property);
        SaveCommand.NotifyCanExecuteChanged(); ApplyCutCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged(); AcceptReviewCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Publishes the requested original goal or the new draft; repeated identical native queries retain input.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var id = query.TryGetValue("id", out var value) && value is Guid identity ? identity : (Guid?)null;
        if (_hasQuery)
        {
            // Shell also publishes an empty query while attaching a native modal. It is not a new draft identity.
            if (!query.ContainsKey("id")) return;
            if (_id != id) throw new InvalidOperationException("A retained goal editor cannot be rebound to another identity.");
            return;
        }
        _hasQuery = true; _id = id; await LoadAsync(false);
    }

    /// <summary>Starts publication only when navigation supplied no query; construction never starts a read.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (!_hasQuery) await ApplyQueryAsync(new Dictionary<string, object>());
        else { await _loadGate.WaitAsync(); _loadGate.Release(); }
    }

    private async Task LoadAsync(bool keepDraft)
    {
        await _loadGate.WaitAsync();
        var draft = keepDraft ? CaptureDraft() : null; var baseline = _baseline; var prior = _review;
        var contexts = (_targetCulture, _contributionCulture, _percentCulture, _targetUnit, _contributionUnit);
        _loading = true; IsReady = false; SaveError = null; RefreshCapabilities();
        try
        {
            var review = await _goals.GetWorkSnapshotAsync();
            if (_openedScope is not null && _openedScope != review.ReminderScope)
                throw new InvalidOperationException("This retained goal draft belongs to another opened profile file.");
            var goal = _id is { } id ? review.Goals.FirstOrDefault(item => item.Id == id) : null;
            if (_id is not null && goal is null) throw new InvalidOperationException("The original goal no longer exists.");
            var plan = goal is null ? null : review.ContributionPlans.FirstOrDefault(item => item.GoalId == goal.Id);
            var settings = await _store.GetSettingsAsync(); var accounts = await _store.GetAccountsAsync();
            var entries = await _store.GetEntriesAsync(); var types = await _holdings.GetTypesAsync();
            var events = await _holdings.GetEventsAsync(); var locations = await _holdings.GetLocationsAsync();
            var categories = await _store.GetCategoriesAsync();
            await _goals.ValidateWorkSnapshotAsync(review);

            // No await follows until the complete original references, captions and baseline are published.
            _openedScope ??= review.ReminderScope; _review = review; _work = review.Work; _existing = goal; _existingPlan = plan;
            _settings = settings; _accounts = accounts; _entries = entries; _types = types; _events = events; _categories = categories;
            if (prior is null) _calendarAtOpen = Calendars.ToPeriod(_localization.CurrentCalendar);
            _savedRule = goal is null ? null : ContributionSchedule.RuleFor(goal, plan, Today).Clone();
            _currentEarmarked = goal is null ? 0 : Math.Max(0, review.Allocations.Where(item => item.GoalId == goal.Id).Sum(item => item.Amount));
            var usedCategories = plan?.CategoryIds ?? [];
            Accounts.Clear();
            foreach (var account in accounts.Where(item => (!item.IsArchived && item.Type is AccountType.Cash or AccountType.Checking or AccountType.Savings)
                || item.Id == goal?.AccountId || item.Id == draft?.Account))
                Accounts.Add(new(account.Id, account.Name, account.CurrencyCode));
            HoldingTypes.Clear();
            foreach (var type in types.Where(item => !item.IsArchived || item.Id == goal?.AssetTypeId || item.Id == draft?.Holding))
                HoldingTypes.Add(new(type.Id, type.Name));
            HoldingLocations.Clear(); HoldingLocations.Add(new(null, _translator["Goal_AllLocations"]));
            foreach (var location in locations.Where(item => !item.IsArchived || item.Id == goal?.LocationId || item.Id == draft?.Location))
                HoldingLocations.Add(new(location.Id, location.Name));
            var lookup = new CategoryLookup(categories, _translator); CutCategories.Clear();
            foreach (var category in lookup.All.Where(item => (item.Kind == CategoryKind.Expense && item.ParentId is null && !item.IsArchived)
                || usedCategories.Contains(item.Id) || item.Id == draft?.Category).OrderBy(item => item.SortOrder))
                CutCategories.Add(new(category.Id, lookup.Name(category.Id)));

            IsAdvanced = settings.Shows(Feature.GoalOptions); CanChangeType = goal is null; ShowTypeChooser = CanChangeType && IsAdvanced;
            TypeIndex = goal?.Type switch { GoalType.Earmark => 1, GoalType.HoldingQuantity => 2, _ => 0 };
            HoldingType = HoldingTypes.FirstOrDefault(item => item.Id == goal?.AssetTypeId) ?? (goal is null ? HoldingTypes.FirstOrDefault() : null);
            HoldingLocation = HoldingLocations.FirstOrDefault(item => item.Id == goal?.LocationId) ?? HoldingLocations[0];
            QuantityUnitIndex = 0;
            Account = goal is not null ? Accounts.FirstOrDefault(item => item.Id == goal.AccountId)
                : Accounts.FirstOrDefault(item => item.Id == settings.DefaultAccountId && item.CurrencyCode == settings.DefaultCurrencyCode)
                    ?? Accounts.FirstOrDefault(item => accounts.First(original => original.Id == item.Id).Type == AccountType.Savings) ?? Accounts.FirstOrDefault();
            CurrencyCode = goal?.CurrencyCode ?? (TypeIndex == 0 ? Account?.CurrencyCode : null) ?? settings.DefaultCurrencyCode;
            UpdateType(); UpdateUnits(false);
            Name = goal?.Name ?? string.Empty; AmountText = goal is null ? string.Empty : FormatTarget(goal.TargetAmount);
            HasTargetDate = goal?.TargetDate is not null; TargetDate = goal?.TargetDate ?? Today.AddYears(1);
            ScheduleIndex = _savedRule is null ? 0 : Array.IndexOf(Schedules, (_savedRule.Frequency, _savedRule.Interval));
            if (ScheduleIndex < 0) ScheduleIndex = 3;
            FirstDate = _savedRule?.Start ?? Today; ReminderEnabled = plan?.ReminderEnabled ?? false;
            MethodIndex = (int)(plan?.Method ?? ContributionMethod.FixedAmount);
            ContributionText = plan?.Amount is { } amount ? FormatTarget(amount) : string.Empty;
            PercentText = plan?.Percent?.ToString("0.##", _localization.CurrentCulture) ?? string.Empty;
            CutCategory = CutCategories.FirstOrDefault(item => usedCategories.Contains(item.Id));
            ShowOnHome = goal?.HomePin is not null; Protect = goal?.Protect ?? false;
            PriorityIndex = (int)(goal?.Priority ?? GoalPriority.Normal); IconKey = goal?.Icon; Note = goal?.Note ?? string.Empty;
            CurrencyLocked = goal is not null && review.Allocations.Any(item => item.GoalId == goal.Id);
            _baseline = CaptureDraft();
            _targetCulture = _contributionCulture = _percentCulture = _localization.CurrentCulture;
            _targetUnit = _contributionUnit = UnitKey();
            if (draft is not null && baseline is not null)
            {
                var merged = MergeDraft(draft, baseline, _baseline); RestoreDraft(merged);
                var unitChanged = draft.Currency != baseline.Currency || draft.Unit != baseline.Unit || draft.Holding != baseline.Holding;
                if (draft.Amount != baseline.Amount || unitChanged) { _targetCulture = contexts._targetCulture; _targetUnit = contexts._targetUnit; }
                if (draft.Contribution != baseline.Contribution || unitChanged) { _contributionCulture = contexts._contributionCulture; _contributionUnit = contexts._contributionUnit; }
                if (draft.Percent != baseline.Percent) _percentCulture = contexts._percentCulture;
                _pendingReview = draft != baseline && prior is not null && !prior.HasSameOriginalInputs(review);
                ReloadedSavedSummary = _pendingReview ? SavedSummary(goal, plan) : null;
            }
            else { _pendingReview = false; ReloadedSavedSummary = null; }
            _needsReload = false; IsReady = true;
        }
        catch { _needsReload = true; SaveError = _translator["Error_Unexpected"]; throw; }
        finally { _loading = false; UpdateInputUnits(); RefreshDisplay(); _loadGate.Release(); }
    }

    /// <summary>Refreshes only live display captions; language/theme changes retain every explicit draft and parse context.</summary>
    public void RefreshDisplay()
    {
        var publishing = _loading; _loading = true;
        try
        {
            Title = _translator[_existing is null ? "Goal_NewTitle" : "Goal_EditTitle"];
            TypeNames = [_translator["GoalType_AccountBalance"], _translator["GoalType_Earmark"], _translator["GoalType_HoldingQuantity"]];
            ScheduleNames = [_translator["Goal_Monthly"], _translator["Goal_EveryTwoWeeks"], _translator["Goal_Weekly"]];
            if (_savedRule is not null && Array.IndexOf(Schedules, (_savedRule.Frequency, _savedRule.Interval)) < 0)
                ScheduleNames = [.. ScheduleNames, _translator["Goal_SavedSchedule"]];
            MethodNames = [_translator["Contribution_Fixed"], _translator["Contribution_ShareOfIncome"], _translator["Contribution_SpendingCut"]];
            PriorityNames = [_translator["GoalPriority_High"], _translator["GoalPriority_Normal"], _translator["GoalPriority_Low"]];
            var location = HoldingLocation?.Id;
            if (HoldingLocations.Count > 0) { HoldingLocations[0] = new(null, _translator["Goal_AllLocations"]); HoldingLocation = HoldingLocations.FirstOrDefault(item => item.Id == location); }
            var category = CutCategory?.Id; var lookup = new CategoryLookup(_categories, _translator);
            for (var i = 0; i < CutCategories.Count; i++) CutCategories[i] = new(CutCategories[i].Id, lookup.Name(CutCategories[i].Id));
            CutCategory = CutCategories.FirstOrDefault(item => item.Id == category);
            UpdateUnits(false); UnitNote = IsQuantityGoal ? null : DisplayUnitNote.For(_translator, CurrencyCode);
            RetainedCategoriesText = _existingPlan is { CategoryIds.Count: > 1 } saved
                ? _translator.Format("Goal_SavedCategories", saved.CategoryIds.Count) : null;
            RefreshReminderNote();
        }
        finally { _loading = publishing; }
        if (!publishing) UpdatePreview();
        RefreshCapabilities();
    }

    Task IThemeAware.RefreshThemeAsync() { RefreshDisplay(); return Task.CompletedTask; }

    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    {
        if (!CanReload) return; IsBusy = true;
        try { await LoadAsync(true); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void AcceptReview()
    {
        if (!CanInteract || !_pendingReview) return;
        _pendingReview = false; ReloadedSavedSummary = null; RefreshCapabilities();
    }

    private string SavedSummary(Goal? goal, ContributionPlan? plan)
    {
        var value = goal is null ? string.Empty : goal.Type == GoalType.HoldingQuantity
            && _types.FirstOrDefault(type => type.Id == goal.AssetTypeId) is { } type
                ? _holdingText.Quantity(goal.TargetAmount, type)
                : MoneyText.Format(goal.TargetAmount, goal.CurrencyCode, _localization.CurrentCulture);
        return _translator.Format("Goal_ReloadedSaved", goal?.Name ?? _translator["Goal_NewTitle"], value,
            plan is null ? string.Empty : new PlanText(_translator, _dates, _localization).EditorSummary(plan.Rule), goal?.Note ?? string.Empty);
    }

    partial void OnTypeIndexChanged(int value) { UpdateType(); UpdateUnits(false); ChangedDraft(); }
    partial void OnAccountChanged(AccountChoice? value)
    {
        if (!_loading && IsBalanceGoal && value is not null) CurrencyCode = value.CurrencyCode;
        ChangedDraft();
    }
    partial void OnCurrencyCodeChanged(string value) { UnitNote = IsQuantityGoal ? null : DisplayUnitNote.For(_translator, value); ChangedDraft(); }
    partial void OnMethodIndexChanged(int value)
    {
        IsFixedMethod = value == (int)ContributionMethod.FixedAmount; IsShareMethod = value == (int)ContributionMethod.ShareOfIncome;
        IsCutMethod = value == (int)ContributionMethod.SpendingCut; ChangedDraft();
    }
    partial void OnAmountTextChanged(string value)
    {
        if (!_loading) { _targetCulture = _localization.CurrentCulture; _targetUnit = UnitKey(); _targetNeedsReview = false; AmountError = null; }
        ChangedDraft();
    }
    partial void OnContributionTextChanged(string value)
    {
        if (!_loading) { _contributionCulture = _localization.CurrentCulture; _contributionUnit = UnitKey(); _contributionNeedsReview = false; ContributionError = null; }
        ChangedDraft();
    }
    partial void OnPercentTextChanged(string value) { if (!_loading) { _percentCulture = _localization.CurrentCulture; PercentError = null; } ChangedDraft(); }
    partial void OnHoldingTypeChanged(HoldingChoice? value)
    {
        if (!_loading)
        {
            UpdateUnits(true);
            if (IsQuantityGoal && SelectedType is { } type) CurrencyCode = type.PriceCurrencyCode;
        }
        ChangedDraft();
    }
    partial void OnHoldingLocationChanged(HoldingChoice? value) => ChangedDraft();
    partial void OnQuantityUnitIndexChanged(int value) => ChangedDraft();
    partial void OnHasTargetDateChanged(bool value) => ChangedDraft();
    partial void OnTargetDateChanged(DateOnly value) => ChangedDraft();
    partial void OnScheduleIndexChanged(int value) => ChangedDraft();
    partial void OnFirstDateChanged(DateOnly value) => ChangedDraft();
    partial void OnCutCategoryChanged(CutCategory? value) => ChangedDraft();

    // A loaded/retranslated opt-in never prompts. One busy guard spans an explicit native choice and its late result.
    async partial void OnReminderEnabledChanged(bool value)
    {
        if (_loading) return; ChangedDraft(); RefreshReminderNote();
        if (!value || !CanInteract || !NewReminder || _existing is { State: not GoalState.Active }) return;
        IsBusy = true;
        try
        {
            if (_notifications.IsSupported)
            {
                var enabled = await _notifications.EnableAsync();
                if (_review is { } review) await _goals.ValidateWorkSnapshotAsync(review);
                ReminderNote = enabled ? null : _translator["Reminder_PermissionDenied"];
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _needsReload = true; SaveError = _translator["Error_Unexpected"]; await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }

    private void RefreshReminderNote() => ReminderNote = !ReminderEnabled ? null
        : !_notifications.IsSupported ? _translator["Reminder_NotOnThisDevice"]
        : !NewReminder || _existing is { State: not GoalState.Active } ? _translator["Goal_ReminderUnavailable"] : ReminderNote;

    private void ChangedDraft()
    {
        if (_loading) return; UpdateInputUnits(); UpdatePreview(); RefreshCapabilities();
    }

    private AssetType? SelectedType => HoldingType?.Id is { } id ? _types.FirstOrDefault(type => type.Id == id) : null;
    private QuantityUnit SelectedUnit => SelectedType is { } type
        ? Quantities.UnitsOf(type.Dimension)[Math.Clamp(QuantityUnitIndex, 0, Quantities.UnitsOf(type.Dimension).Count - 1)] : QuantityUnit.Gram;
    private void UpdateUnits(bool reset)
    {
        if (SelectedType is not { } type) { QuantityUnitNames = []; HasQuantityUnitChoice = false; return; }
        var units = Quantities.UnitsOf(type.Dimension);
        QuantityUnitNames = [.. units.Select(unit => unit == QuantityUnit.Piece ? _holdingText.CountName(type)
            : _translator[unit == QuantityUnit.Kilogram ? "Unit_Kilogram" : "Unit_Gram"])];
        HasQuantityUnitChoice = IsQuantityGoal && units.Count > 1;
        if (reset) QuantityUnitIndex = 0;
    }
    private void UpdateType()
    {
        IsBalanceGoal = TypeIndex == 0; IsQuantityGoal = TypeIndex == 2; IsMoneyGoal = !IsQuantityGoal;
        ShowCurrencyChoice = TypeIndex == 1; HasQuantityUnitChoice = IsQuantityGoal && QuantityUnitNames.Count > 1;
        ShowMethods = IsAdvanced && IsMoneyGoal;
        if (!_loading && IsBalanceGoal && Account is not null) CurrencyCode = Account.CurrencyCode;
        if (!_loading && IsQuantityGoal && _existing is null)
        { MethodIndex = (int)ContributionMethod.FixedAmount; if (SelectedType is { } type) CurrencyCode = type.PriceCurrencyCode; }
    }

    private string UnitKey() => IsQuantityGoal
        ? $"quantity|{HoldingType?.Id}|{SelectedType?.Dimension}|{SelectedType?.Divisible}|{SelectedType?.CountUnitName}|{SelectedUnit}"
        : "money|" + CurrencyCode;
    private void UpdateInputUnits()
    {
        if (_loading || !IsReady) return;
        var unit = UnitKey(); _targetNeedsReview = AmountText.Length > 0 && _targetUnit != unit;
        _contributionNeedsReview = ContributionText.Length > 0 && _contributionUnit != unit;
        if (_targetNeedsReview) AmountError = _translator["Goal_DraftUnitChanged"];
        if (_contributionNeedsReview) ContributionError = _translator["Goal_DraftUnitChanged"];
    }
    private bool TryAmount(string text, CultureInfo culture, out long value)
    {
        value = 0;
        return IsQuantityGoal ? SelectedType is { } type && Quantities.TryParse(text, SelectedUnit, type, culture, out value) && value > 0
            : Currencies.TryGet(CurrencyCode, out var currency) && MoneyText.TryParse(text, currency, culture, out value) && value > 0;
    }
    private string FormatTarget(long value) => IsQuantityGoal ? Quantities.ForInput(value, SelectedUnit, _localization.CurrentCulture)
        : MoneyText.ForInput(value, CurrencyCode, _localization.CurrentCulture);
    private string Show(long value) => IsQuantityGoal && SelectedType is { } type ? _holdingText.Quantity(value, type)
        : MoneyText.Format(value, CurrencyCode, _localization.CurrentCulture);

    private RecurrenceRule BuildRule()
    {
        var rule = _savedRule?.Clone() ?? new RecurrenceRule { Calendar = _calendarAtOpen };
        if (ScheduleIndex < Schedules.Length)
        {
            var (frequency, interval) = Schedules[Math.Clamp(ScheduleIndex, 0, Schedules.Length - 1)];
            if (frequency != rule.Frequency && frequency == Frequency.Weekly)
            { rule.DayRule = MonthDayRule.SpecificDay; rule.SecondDay = null; }
            rule.Calendar = frequency == Frequency.Monthly
                ? _savedRule is { Frequency: Frequency.Monthly or Frequency.Yearly } saved ? saved.Calendar : _calendarAtOpen
                : PeriodCalendar.Gregorian;
            rule.Frequency = frequency; rule.Interval = interval;
        }
        rule.Start = FirstDate; return rule;
    }

    private Goal BuildGoal(long target)
    {
        var goal = (_existing ?? _newGoal).Clone(); goal.Name = Name.Trim(); goal.Type = CurrentType;
        goal.AccountId = IsBalanceGoal ? Account?.Id : null; goal.AssetTypeId = IsQuantityGoal ? HoldingType?.Id : null;
        goal.LocationId = IsQuantityGoal ? HoldingLocation?.Id : null; goal.TargetAmount = target; goal.CurrencyCode = CurrencyCode;
        goal.TargetDate = HasTargetDate ? TargetDate : null;
        if (ScheduleIndex < Schedules.Length) goal.Frequency = Schedules[ScheduleIndex].Frequency == Frequency.Monthly ? ContributionFrequency.Monthly : ContributionFrequency.Weekly;
        goal.Priority = (GoalPriority)Math.Clamp(PriorityIndex, 0, 2); goal.Icon = IconKey;
        goal.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(); goal.Protect = IsQuantityGoal ? false : goal.Type == GoalType.Earmark && Protect;
        goal.HomePin = ShowOnHome ? goal.HomePin ?? (_review?.Goals.Max(item => item.HomePin) ?? 0) + 1 : null;
        return goal;
    }

    // Untouched hidden values, complete categories and uncommon rules remain the original saved plan.
    private ContributionPlan? BuildPlan()
    {
        var draft = CaptureDraft();
        if (_existing is not null && _existingPlan is null && _baseline is { } baseline && !PlanFieldsChanged(draft, baseline)) return null;
        var original = _existingPlan;
        var plan = original is null ? new ContributionPlan() : new ContributionPlan
        {
            GoalId = original.GoalId, Method = original.Method, Amount = original.Amount, Percent = original.Percent,
            CategoryIds = [.. original.CategoryIds], AssumedPricePerUnitMilli = original.AssumedPricePerUnitMilli,
            CreatedAt = original.CreatedAt, UpdatedAt = original.UpdatedAt,
        };
        var method = (ContributionMethod)Math.Clamp(MethodIndex, 0, 2);
        var methodChanged = original is not null && original.Method != method;
        if (methodChanged) { plan.Amount = null; plan.Percent = null; plan.CategoryIds = []; }
        plan.Method = method; plan.ReminderEnabled = ReminderEnabled; plan.Rule = BuildRule();
        if (method is ContributionMethod.FixedAmount or ContributionMethod.SpendingCut
            && (original is null || methodChanged || ContributionText != _baseline?.Contribution))
            plan.Amount = TryAmount(ContributionText, _contributionCulture, out var amount) ? amount : null;
        if (method == ContributionMethod.ShareOfIncome && (original is null || methodChanged || PercentText != _baseline?.Percent))
            plan.Percent = TryPercent(out var percent) ? percent : null;
        if (method == ContributionMethod.SpendingCut && (original is null || methodChanged || CutCategory?.Id != _baseline?.Category))
            plan.CategoryIds = CutCategory is { } category ? [category.Id] : [];
        return plan;
    }

    private bool TryPercent(out decimal percent) => decimal.TryParse(Vafadar.Core.Text.Digits.ToAscii(PercentText.Trim().TrimEnd('%')),
        NumberStyles.Number, _percentCulture, out percent) && percent is > 0 and <= 100;

    private bool DraftWorkAllowed()
    {
        if (!IsReady) return false;
        var plan = BuildPlan(); var goal = BuildGoal(1);
        return (!GoalContributionPolicy.StartsWork(plan, _existingPlan, goal, _existing) || NewContribution)
            && (!GoalContributionPolicy.StartsReminder(plan, _existingPlan, goal, _existing) || NewReminder);
    }

    private void UpdatePreview()
    {
        if (_loading || !IsReady) return;
        PreviewProgress = PreviewPlan = PreviewRequired = PreviewMethod = null; CanApplyCut = false;
        var rule = BuildRule(); var text = new PlanText(_translator, _dates, _localization);
        ScheduleSummary = text.EditorSummary(rule); ShortMonthNote = text.ShortMonthHint(rule); UpcomingDates.Clear();
        foreach (var date in text.UpcomingDates(rule, Today)) UpcomingDates.Add(date);
        if (_targetNeedsReview || _contributionNeedsReview || !TryAmount(AmountText, _targetCulture, out var target) || Recurrence.Validate(rule) is not null) return;
        var culture = _localization.CurrentCulture; var goal = BuildGoal(target); var plan = BuildPlan();
        var current = IsQuantityGoal ? GoalProgressService.HeldQuantity(goal, _events, Today)
            : IsBalanceGoal ? _accounts.FirstOrDefault(item => item.Id == Account?.Id) is { } account ? LedgerCalculator.Balance(account, _entries, Today) : 0
            : _currentEarmarked;
        var progress = GoalProgressService.Scenario(goal, current, 0, plan, Today);
        PreviewProgress = _translator.Format("Goal_PreviewNow", Show(Math.Max(0, current)), Math.Round(progress.Progress * 100).ToString("0", culture), Show(progress.Remaining));
        if (progress.Eta is { } eta && progress.PlannedContribution is { } contribution)
            PreviewPlan = _translator.Format("Goal_PreviewPlan", Show(contribution), text.Rule(rule), ContributionSchedule.PeriodsNeeded(progress.Remaining, contribution), _dates.Format(eta, DateFormatStyle.Short));
        if (progress.Required is { } required && goal.TargetDate is { } due)
            PreviewRequired = progress.Opportunities == 0 ? _translator.Format("Goal_PreviewNeededNow", Show(progress.Remaining))
                : _translator.Format("Goal_PreviewRequired", _dates.Format(due, DateFormatStyle.Short), Show(required), progress.Opportunities);
        if (IsShareMethod && plan?.Percent is { } percent && _settings is { } settings)
        {
            var calendar = Calendars.ToPeriod(_localization.CurrentCalendar); var (y, m) = PeriodMath.MonthOf(Today, calendar, settings.MonthStartDay);
            var (year, month) = PeriodMath.Previous(y, m); var (from, to) = PeriodMath.MonthRange(year, month, calendar, settings.MonthStartDay);
            var income = EligibleIncome.Of(_accounts, _entries, CurrencyCode, from, to);
            PreviewMethod = _translator.Format("Goal_PreviewShare", Show(income.Total), income.Entries.Count, percent.ToString("0.##", culture), Show(EligibleIncome.Suggestion(income.Total, percent)));
        }
        if (IsCutMethod && !IsQuantityGoal && plan is { Amount: > 0 } && CutCategory is { } category)
        {
            PreviewMethod = _translator.Format("Goal_PreviewCut", category.Name, Show(plan.Amount.Value));
            CanApplyCut = CanInteract && !_needsReload && !_pendingReview && NewContribution
                && (_existing is null || _existing.State == GoalState.Active) && _categories.Any(item => item.Id == category.Id && item.Kind == CategoryKind.Expense && !item.IsArchived)
                && CutFingerprint(plan.Amount.Value, category.Id) != _appliedCut;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave || _review is not { } review) return;
        SaveError = null; NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Goal_NameRequired"] : null;
        AmountError = TryAmount(AmountText, _targetCulture, out var amount) ? null : _translator[IsQuantityGoal ? "AssetEvent_QuantityInvalid" : "Amount_Invalid"];
        ContributionError = ContributionText.Trim().Length > 0 && !IsShareMethod && !TryAmount(ContributionText, _contributionCulture, out _)
            ? _translator[IsQuantityGoal ? "AssetEvent_QuantityInvalid" : "Amount_Invalid"] : null;
        PercentError = IsShareMethod && PercentText.Trim().Length > 0 && !TryPercent(out _) ? _translator["Goal_PercentInvalid"] : null;
        RuleError = Recurrence.Validate(BuildRule()) is null ? null : _translator["Plan_RuleInvalid"];
        if (IsBalanceGoal && Account is null) SaveError = _translator["Goal_ChooseAccount"];
        if (IsQuantityGoal && SelectedType is null) SaveError = _translator["Goal_ChooseHolding"];
        var goal = BuildGoal(amount);
        if (IsBalanceGoal && Account is { } account && GoalProgressService.HasOtherBalanceGoal(review.Goals, account.Id, goal.Id))
            SaveError = _translator["Goal_AccountHasGoal"];
        if (NameError is not null || AmountError is not null || ContributionError is not null || PercentError is not null || RuleError is not null || SaveError is not null) return;
        IsBusy = true;
        try
        {
            var plan = BuildPlan();
            if (plan?.AssumedPricePerUnitMilli is not null && _existing is { } old
                && (old.AssetTypeId != goal.AssetTypeId || old.CurrencyCode != goal.CurrencyCode))
            {
                if (!await _interaction.ConfirmAsync(_translator["Goal_EditTitle"], _translator["Goal_ResetPriceForHolding"],
                    _translator["Common_Save"], _translator["Common_Cancel"])) return;
                plan.AssumedPricePerUnitMilli = null;
            }
            try { await _goals.SaveGoalWithContributionPlanAsync(goal, plan, review: review); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            { _needsReload = true; SaveError = _translator["Goal_SaveFailed"]; await _interaction.ShowFailureAsync(exception); return; }
            // A committed draft is retired before navigation; a failed return can never duplicate or resubmit it.
            _existing = goal; _baseline = CaptureDraft(); _committed = true; _pendingReview = false;
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            { SaveError = _translator["Goal_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { SaveError = _translator["Goal_SaveFailed"]; await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; RefreshCapabilities(); }
    }

    private string CutFingerprint(long amount, Guid category) => DraftFingerprint.Create(_openedScope, _existing?.Id, amount, category, CurrencyCode);

    [RelayCommand(CanExecute = nameof(CanApplyCut))]
    private async Task ApplyCutAsync()
    {
        if (!CanInteract || !CanApplyCut || _review is not { } review || CutCategory is not { } category
            || !TryAmount(ContributionText, _contributionCulture, out var cut)) return;
        var submitted = CutFingerprint(cut, category.Id); var type = CurrentType; var currency = CurrencyCode;
        IsBusy = true;
        try
        {
            var budget = await _store.ReviewGoalBudgetCutAsync(review, _existing?.Id, type, category.Id, currency);
            if (budget is null || budget.OriginalLimit < 0)
            { await _interaction.AlertAsync(_translator["Contribution_SpendingCut"], _translator.Format("Goal_CutNoLimit", category.Name), _translator["Common_Ok"]); return; }
            var name = new CategoryLookup([budget.Category], _translator).Name(budget.Category.Id);
            var newLimit = Math.Max(0, budget.OriginalLimit - cut);
            if (!await _interaction.ConfirmAsync(_translator["Goal_ApplyCut"], _translator.Format("Goal_ApplyCutMessage", name,
                MoneyText.Format(budget.OriginalLimit, budget.CurrencyCode, _localization.CurrentCulture), MoneyText.Format(newLimit, budget.CurrencyCode, _localization.CurrentCulture)),
                _translator["Goal_ApplyCut"], _translator["Common_Cancel"])) return;
            await _store.ApplyGoalBudgetCutAsync(budget, cut); _appliedCut = submitted;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _needsReload = true; SaveError = _translator["Goal_SaveFailed"]; await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; UpdatePreview(); RefreshCapabilities(); }
    }

    [RelayCommand(CanExecute = nameof(CanLeave))]
    private async Task CancelAsync()
    {
        if (!CanLeave) return; IsBusy = true;
        try
        {
            if (IsDirty && !await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"],
                _translator["Common_Discard"], _translator["Common_KeepEditing"])) return;
            await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }

    /// <inheritdoc />
    public bool IsDirty => !_committed && _baseline is { } baseline && CaptureDraft() != baseline;

    private sealed record Draft(int Type, Guid? Holding, Guid? Location, int Unit, Guid? Account, string Name, string Amount,
        string Currency, bool HasDate, DateOnly Date, int Schedule, DateOnly First, bool Reminder, int Method, string Contribution,
        string Percent, Guid? Category, bool Home, bool Protect, int Priority, string? Icon, string Note);
    private Draft CaptureDraft() => new(TypeIndex, HoldingType?.Id, HoldingLocation?.Id, QuantityUnitIndex, Account?.Id, Name, AmountText,
        CurrencyCode, HasTargetDate, TargetDate, ScheduleIndex, FirstDate, ReminderEnabled, MethodIndex, ContributionText, PercentText,
        CutCategory?.Id, ShowOnHome, Protect, PriorityIndex, IconKey, Note);
    private static bool PlanFieldsChanged(Draft draft, Draft baseline) => draft.Schedule != baseline.Schedule || draft.First != baseline.First
        || draft.Reminder != baseline.Reminder || draft.Method != baseline.Method || draft.Contribution != baseline.Contribution
        || draft.Percent != baseline.Percent || draft.Category != baseline.Category;
    private static Draft MergeDraft(Draft draft, Draft baseline, Draft current)
    {
        static T Keep<T>(T value, T original, T latest) => EqualityComparer<T>.Default.Equals(value, original) ? latest : value;
        return new(Keep(draft.Type, baseline.Type, current.Type), Keep(draft.Holding, baseline.Holding, current.Holding),
            Keep(draft.Location, baseline.Location, current.Location), Keep(draft.Unit, baseline.Unit, current.Unit), Keep(draft.Account, baseline.Account, current.Account),
            Keep(draft.Name, baseline.Name, current.Name), Keep(draft.Amount, baseline.Amount, current.Amount), Keep(draft.Currency, baseline.Currency, current.Currency),
            Keep(draft.HasDate, baseline.HasDate, current.HasDate), Keep(draft.Date, baseline.Date, current.Date), Keep(draft.Schedule, baseline.Schedule, current.Schedule),
            Keep(draft.First, baseline.First, current.First), Keep(draft.Reminder, baseline.Reminder, current.Reminder), Keep(draft.Method, baseline.Method, current.Method),
            Keep(draft.Contribution, baseline.Contribution, current.Contribution), Keep(draft.Percent, baseline.Percent, current.Percent), Keep(draft.Category, baseline.Category, current.Category),
            Keep(draft.Home, baseline.Home, current.Home), Keep(draft.Protect, baseline.Protect, current.Protect), Keep(draft.Priority, baseline.Priority, current.Priority),
            Keep(draft.Icon, baseline.Icon, current.Icon), Keep(draft.Note, baseline.Note, current.Note));
    }
    private void RestoreDraft(Draft draft)
    {
        TypeIndex = draft.Type; HoldingType = HoldingTypes.FirstOrDefault(item => item.Id == draft.Holding);
        HoldingLocation = HoldingLocations.FirstOrDefault(item => item.Id == draft.Location); QuantityUnitIndex = draft.Unit;
        Account = Accounts.FirstOrDefault(item => item.Id == draft.Account); Name = draft.Name; AmountText = draft.Amount;
        CurrencyCode = draft.Currency; HasTargetDate = draft.HasDate; TargetDate = draft.Date; ScheduleIndex = draft.Schedule;
        FirstDate = draft.First; ReminderEnabled = draft.Reminder; MethodIndex = draft.Method; ContributionText = draft.Contribution;
        PercentText = draft.Percent; CutCategory = CutCategories.FirstOrDefault(item => item.Id == draft.Category);
        ShowOnHome = draft.Home; Protect = draft.Protect; PriorityIndex = draft.Priority; IconKey = draft.Icon; Note = draft.Note;
        UpdateType(); UpdateUnits(false);
    }
}
