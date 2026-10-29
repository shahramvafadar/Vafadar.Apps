using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>An account that can fund the goal.</summary>
public sealed record FundingAccount(Guid Id, string Name, long Unallocated, string UnallocatedText)
{
    /// <summary>Gets whether this original account accepts a new positive earmark; retained releases stay separate.</summary>
    public bool CanFund { get; init; } = true;

    public override string ToString() => Name;
}

/// <summary>One allocation or release in the history.</summary>
public sealed partial record AllocationRow(Guid Id, string Text, string DateText, string AmountText, bool IsRelease);

/// <summary>
/// One goal: funding, the next suggestion, per-account earmarks and the history. "Set aside" and "Release" only change
/// the earmark; moving money between accounts is a transfer entry and spending it is an expense (F2-GOAL-03).
/// </summary>
public sealed partial class GoalDetailViewModel(
    ZananceStore store,
    GoalStore goals,
    PlanStore plans,
    HoldingStore holdings,
    GoalPresenter presenter,
    Translator translator,
    IDateFormatter dates,
    ILocalizationService localization,
    TimeProvider time,
    IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
{
    private IAsyncRelayCommand? _reloadCommand;
    /// <summary>Gets the original Reload action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand ReloadCommand => _reloadCommand ??= DetailCommand(ReloadAsync, () => IsNotBusy && !_refreshing);
    private IAsyncRelayCommand? _saveAssumedPriceCommand;
    /// <summary>Gets the original SaveAssumedPrice action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand SaveAssumedPriceCommand => _saveAssumedPriceCommand ??= DetailCommand(SaveAssumedPriceAsync, () => CanSaveAssumedPrice);
    private IAsyncRelayCommand? _allocateCommand;
    /// <summary>Gets the original Allocate action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand AllocateCommand => _allocateCommand ??= DetailCommand(AllocateAsync, () => CanAllocate);
    private IAsyncRelayCommand? _editCommand;
    /// <summary>Gets the original Edit action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand EditCommand => _editCommand ??= DetailCommand(EditAsync, () => CanInteract);
    private IAsyncRelayCommand? _toggleCompleteCommand;
    /// <summary>Gets the original ToggleComplete action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand ToggleCompleteCommand => _toggleCompleteCommand ??= DetailCommand(ToggleCompleteAsync, () => CanToggleComplete);
    private IAsyncRelayCommand? _togglePauseCommand;
    /// <summary>Gets the original TogglePause action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand TogglePauseCommand => _togglePauseCommand ??= DetailCommand(TogglePauseAsync, () => CanTogglePause);
    private IAsyncRelayCommand? _restoreCommand;
    /// <summary>Gets the original Restore action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand RestoreCommand => _restoreCommand ??= DetailCommand(RestoreAsync, () => CanRestore);
    private IAsyncRelayCommand? _archiveCommand;
    /// <summary>Gets the original Archive action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand ArchiveCommand => _archiveCommand ??= DetailCommand(ArchiveAsync, () => CanInteract);
    private IAsyncRelayCommand? _deleteCommand;
    /// <summary>Gets the original Delete action owned by this detail lifetime.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand ??= DetailCommand(DeleteAsync, () => CanInteract);
    private IAsyncRelayCommand<AllocationRow>? _deleteAllocationCommand;
    /// <summary>Gets deletion of only the exact currently published original allocation row.</summary>
    public IAsyncRelayCommand<AllocationRow> DeleteAllocationCommand => _deleteAllocationCommand ??= AllocationCommand();

    private IAsyncRelayCommand DetailCommand(Func<Task> action, Func<bool> canExecute)
    {
        var identity = _identityVersion;
        bool Current() => !_retired && identity == _identityVersion;
        return new AsyncRelayCommand(() => Current() && canExecute() ? action() : Task.CompletedTask,
            () => Current() && canExecute());
    }
    private IAsyncRelayCommand<AllocationRow> AllocationCommand()
    {
        var identity = _identityVersion;
        bool Current(AllocationRow? row) => !_retired && identity == _identityVersion && CanInteract
            && row is not null && OwnsHistory(row);
        return new AsyncRelayCommand<AllocationRow>(row => Current(row) ? DeleteAllocationAsync(row!) : Task.CompletedTask, Current);
    }
    private void ResetCommands()
    {
        _reloadCommand = null; OnPropertyChanged(nameof(ReloadCommand));
        _saveAssumedPriceCommand = null; OnPropertyChanged(nameof(SaveAssumedPriceCommand));
        _allocateCommand = null; OnPropertyChanged(nameof(AllocateCommand));
        _editCommand = null; OnPropertyChanged(nameof(EditCommand));
        _toggleCompleteCommand = null; OnPropertyChanged(nameof(ToggleCompleteCommand));
        _togglePauseCommand = null; OnPropertyChanged(nameof(TogglePauseCommand));
        _restoreCommand = null; OnPropertyChanged(nameof(RestoreCommand));
        _archiveCommand = null; OnPropertyChanged(nameof(ArchiveCommand));
        _deleteCommand = null; OnPropertyChanged(nameof(DeleteCommand));
        _deleteAllocationCommand = null; OnPropertyChanged(nameof(DeleteAllocationCommand));
    }
    /// <summary>Retires pending reads and original commands without discarding the retained original money drafts.</summary>
    public void Retire()
    {
        _identityVersion++; _retired = true; _needsReload = true;
        var prior = _readLifetime; _readLifetime = new(); prior.Cancel(); prior.Dispose();
        IsLoading = true; ResetCommands(); OnPropertyChanged(nameof(CanView)); RefreshActions();
    }

    private Guid _id;
    private long _identityVersion;
    private Func<bool>? _writeCurrent;
    private bool _retired;
    private CancellationTokenSource _readLifetime = new();

    /// <summary>Gets whether this original detail is awaiting its complete financial packet.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanView))]
    public partial bool IsLoading { get; private set; } = true;
    /// <summary>Gets whether the local read cover must offer an explicit retry.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanView))]
    public partial bool HasReadFailed { get; private set; }
    /// <summary>Gets whether the complete original detail may be shown, independently of write busy state.</summary>
    public bool CanView => !_retired && !IsLoading && !HasReadFailed;

    private Dictionary<Guid, AllocationRow> _historyRows = [];
    private bool OwnsHistory(AllocationRow row) => _historyRows.TryGetValue(row.Id, out var current) && ReferenceEquals(current, row);
    private Goal? _goal;
    private GoalWorkPolicy _work = GoalWorkPolicy.Inactive;
    private bool _refreshing;
    private GoalWorkSnapshot? _review;
    private GoalWorkSnapshot? _originalSource;
    private GoalListSnapshot? _financialReview;
    private bool _needsReload;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private bool _publishingDrafts;
    private bool _amountNeedsReview, _priceNeedsReview;
    private readonly MoneyDraftInput _amountInput = new(), _priceInput = new();
    private long _moneyDraftVersion;
    private string _priceBaseline = string.Empty;

    /// <summary>Explains the original contribution parser after a display-only format change.</summary>
    public string? AmountFormatNote => OriginalFormatNote(_amountInput);
    /// <summary>Explains the original assumed-price parser after a display-only format change.</summary>
    public string? PriceFormatNote => OriginalFormatNote(_priceInput);
    /// <summary>Explains the original assumed price's display-unit factor independently of physical units.</summary>
    public string? PriceUnitNote => _goal is { } goal && presenter.TypeOf(goal) is { } type
        ? DisplayUnitNote.ForInput(translator, _priceInput, type.PriceCurrencyCode) : null;

    private string? OriginalFormatNote(MoneyDraftInput input) => input.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;

    // Captions follow the draft, never the other way round; no amount is converted by a display refresh.
    private void RefreshMoneyInputs()
    {
        if (_goal is not { } goal) return;
        var amountWasStale = _amountNeedsReview; var priceWasStale = _priceNeedsReview;
        _amountNeedsReview = _amountInput.NeedsReview(goal.CurrencyCode);
        UnitNote = DisplayUnitNote.ForInput(translator, _amountInput, goal.CurrencyCode);
        if (_amountNeedsReview) AmountError = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(goal.CurrencyCode));
        else if (amountWasStale) AmountError = null;
        if (presenter.TypeOf(goal) is { } type)
        {
            _priceNeedsReview = _priceInput.NeedsReview(type.PriceCurrencyCode);
            AssumedPriceLabel = translator.Format("Goal_AssumedPrice", _priceInput.UnitName(type.PriceCurrencyCode),
                translator[type.Dimension == Core.Holdings.AssetDimension.Mass ? "Holding_PerGram" : "Holding_Piece"]);
            if (_priceNeedsReview) PriceError = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(type.PriceCurrencyCode));
            else if (priceWasStale) PriceError = null;
        }
        OnPropertyChanged(nameof(AmountFormatNote)); OnPropertyChanged(nameof(PriceFormatNote)); OnPropertyChanged(nameof(PriceUnitNote));
    }

    /// <summary>Gets feedback for a retained price draft whose stored currency changed.</summary>
    [ObservableProperty]
    public partial string? PriceError { get; set; }

    partial void OnAmountTextChanged(string value)
    {
        if (_publishingDrafts) return;
        _amountInput.ChangeText(value, _goal?.CurrencyCode ?? "EUR", localization.CurrentCulture);
        _moneyDraftVersion++; AmountError = null; RefreshMoneyInputs(); RefreshActions();
    }

    partial void OnAssumedPriceTextChanged(string value)
    {
        if (_publishingDrafts) return;
        _priceInput.ChangeText(value, _goal is { } goal ? presenter.TypeOf(goal)?.PriceCurrencyCode ?? "EUR" : "EUR", localization.CurrentCulture);
        _moneyDraftVersion++; PriceError = null; RefreshMoneyInputs(); RefreshActions();
    }

    /// <summary>Gets post-commit reload feedback, distinct from a rejected write.</summary>
    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets whether the loaded original form can accept an explicit action.</summary>
    public bool CanInteract => _goal is not null && _goal.Id == _id && CanView && !_refreshing && !_needsReload && IsNotBusy;
    /// <summary>Gets whether the original form requires a successful read before another write.</summary>
    public bool NeedsReload => _needsReload;
    /// <summary>Gets positive earmark eligibility independently of the visible retained release panel.</summary>
    public bool CanContribute => _goal is { State: GoalState.Active, Type: GoalType.Earmark } goal && _work.CanContribute(goal.Id);
    /// <summary>Gets whether the current explicit direction/account can be saved; the writer rechecks current rights.</summary>
    public bool CanAllocate => CanInteract && !_amountNeedsReview && !_amountInput.NeedsReview(_goal!.CurrencyCode) && SelectedAccount is { } account
        && (DirectionIndex == 1 || DirectionIndex == 0 && CanContribute && account.CanFund);
    /// <summary>Gets pause/resume availability without resuming an unselected pause.</summary>
    public bool CanTogglePause => CanInteract && (_goal!.State == GoalState.Active
        || _goal.State == GoalState.Paused && _work.IsAvailableGoal(_goal.Id));
    /// <summary>Gets completion/reopening availability; retained completion remains a correction.</summary>
    public bool CanToggleComplete => CanInteract && (_goal!.State is GoalState.Active or GoalState.Paused || CanReopenOriginal());
    /// <summary>Gets explicit restoration availability for the original archived goal.</summary>
    public bool CanRestore => CanInteract && _goal!.State == GoalState.Archived && CanReopenOriginal();

    // Reopening the original saved contribution is new work too; a basic target must not hide its paid saved rule.
    private bool CanReopenOriginal() => _goal is { } goal
        && _work.CanReopenGoal(goal.Type, _review?.ContributionPlans.FirstOrDefault(plan => plan.GoalId == goal.Id));
    /// <summary>Gets whether the assumed-price field can start new work or correct a retained recorded price.</summary>
    [ObservableProperty]
    public partial bool CanEditAssumedPrice { get; set; }

    /// <summary>Gets whether an explicit assumed-price save is available in the loaded original form.</summary>
    public bool CanSaveAssumedPrice => CanInteract && CanEditAssumedPrice && !_priceNeedsReview
        && !_priceInput.NeedsReview(presenter.TypeOf(_goal!)?.PriceCurrencyCode ?? "EUR");

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(DirectionIndex) or nameof(SelectedAccount)) _moneyDraftVersion++;
        if (e.PropertyName is nameof(IsBusy) or nameof(IsLoading) or nameof(HasReadFailed) or nameof(DirectionIndex) or nameof(SelectedAccount) or nameof(CanEditAssumedPrice)) RefreshActions();
    }

    // Command enablement and native button bindings share the same loaded state; no control copies the policy.
    private void RefreshActions()
    {
        OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanContribute)); OnPropertyChanged(nameof(CanAllocate));
        OnPropertyChanged(nameof(CanTogglePause)); OnPropertyChanged(nameof(CanToggleComplete)); OnPropertyChanged(nameof(CanRestore));
        OnPropertyChanged(nameof(CanSaveAssumedPrice));
        OnPropertyChanged(nameof(NeedsReload)); ReloadCommand.NotifyCanExecuteChanged();
        SaveAssumedPriceCommand.NotifyCanExecuteChanged(); DeleteAllocationCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged(); ArchiveCommand.NotifyCanExecuteChanged(); DeleteCommand.NotifyCanExecuteChanged();
        AllocateCommand.NotifyCanExecuteChanged(); TogglePauseCommand.NotifyCanExecuteChanged();
        ToggleCompleteCommand.NotifyCanExecuteChanged(); RestoreCommand.NotifyCanExecuteChanged();
    }

    public SnapshotCollection<AllocationRow> History { get; } = [];

    public SnapshotCollection<string> PerAccount { get; } = [];

    [ObservableProperty]
    public partial GoalRow? Summary { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<FundingAccount> Accounts { get; set; } = [];

    [ObservableProperty]
    public partial FundingAccount? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial bool HasFundingAccounts { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> DirectionNames { get; set; } = [];

    [ObservableProperty]
    public partial int DirectionIndex { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    [ObservableProperty]
    public partial string? NoAccountsText { get; set; }
    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial string? CompleteText { get; set; }

    /// <summary>The screen reader hint of the complete button; none while it reactivates a completed goal.</summary>
    [ObservableProperty]
    public partial string? CompleteHint { get; set; }

    /// <summary>Gets a value indicating whether money can be set aside (active goals of money set aside).</summary>
    [ObservableProperty]
    public partial bool CanSetAside { get; set; }

    /// <summary>Gets the text of the pause button: "Pause" or "Resume" (ZEX-S0303).</summary>
    [ObservableProperty]
    public partial string? PauseText { get; set; }

    [ObservableProperty]
    public partial bool CanPause { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    /// <summary>Gets a value indicating whether the goal is money set aside (it has a history of earmarks).</summary>
    [ObservableProperty]
    public partial bool IsEarmark { get; set; }

    /// <summary>Gets the observed pace and its date (ZEX-S0702): "At your recent pace …", "No date …" or "Not enough history …".</summary>
    [ObservableProperty]
    public partial string? TrendText { get; set; }

    /// <summary>Gets the net contribution of the running month, shown apart from the pace.</summary>
    [ObservableProperty]
    public partial string? SoFarText { get; set; }

    /// <summary>Gets the months behind the pace (Advanced), with one-offs named.</summary>
    public SnapshotCollection<string> TrendPeriods { get; } = [];

    [ObservableProperty]
    public partial bool HasTrendPeriods { get; set; }

    /// <summary>Gets a value indicating whether the goal counts a holding quantity (ZEX-S0701).</summary>
    [ObservableProperty]
    public partial bool IsQuantity { get; set; }

    /// <summary>Gets or sets the price the user assumes per gram or unit (ZEX-S0703); only to turn money capacity into a quantity.</summary>
    [ObservableProperty]
    public partial string AssumedPriceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? AssumedPriceLabel { get; set; }

    /// <summary>Gets "At your price: 2.5 g per month from your capacity", a separate line from the quantity pace.</summary>
    [ObservableProperty]
    public partial string? CapacityQuantityText { get; set; }

    [ObservableProperty]
    public partial bool CanUseAssumedPrice { get; set; }

    /// <summary>Gets the essential coverage in the goal's currency (ZEX-K07, Advanced): how many months usable money lasts.</summary>
    [ObservableProperty]
    public partial string? CoverageText { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            if (_id != id)
            {
                _identityVersion++; _needsReload = true;
                var prior = _readLifetime; _readLifetime = new(); prior.Cancel(); prior.Dispose();
                _id = id; IsLoading = true; ResetCommands(); RefreshActions();
            }
        }
    }

    // Display and choice reloads retain explicit dirty inputs in their original currency/culture, never reinterpret money.
    Task Presentation.IThemeAware.RefreshThemeAsync() => _retired ? Task.CompletedTask : LoadAsync();

    public async Task LoadAsync()
    {
        var identity = _identityVersion; var token = _readLifetime.Token; _retired = false;
        try { await _loadGate.WaitAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        void RequireCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (identity != _identityVersion || _retired) throw new OperationCanceledException("Original goal detail retired.", token);
        }
        IsLoading = true; HasReadFailed = false; _refreshing = true; RefreshActions();
        var previousScope = _review?.ReminderScope; var previousGoal = _goal?.Id;
        var priceDirty = AssumedPriceText != _priceBaseline || _priceNeedsReview
            || _goal is { } original && presenter.TypeOf(original) is { } originalType && _priceInput.NeedsReview(originalType.PriceCurrencyCode);
        var priceDraft = _priceInput.Capture();
        try
        {
            _publishingDrafts = true;
            RequireCurrent(); await LoadCoreAsync(token, RequireCurrent); RequireCurrent();
            _needsReload = false; SaveError = null;
            _priceBaseline = AssumedPriceText;
            var sameContext = previousScope == _review?.ReminderScope && previousGoal == _goal?.Id;
            if (previousGoal is not null && !sameContext)
            {
                AmountText = string.Empty; AmountError = null; DirectionIndex = 0;
                _amountNeedsReview = false; _priceNeedsReview = false; PriceError = null;
            }
            if (!sameContext)
            {
                // A newly opened historical/readonly earmark offers its real available action and original funding account.
                // Subsequent display/choice reloads keep the user's explicit direction and selected account.
                DirectionIndex = !CanContribute && CanSetAside ? 1 : 0;
                SelectedAccount = DirectionIndex == 1
                    ? Accounts.FirstOrDefault(account => _review!.Allocations.Where(item => item.GoalId == _goal?.Id && item.AccountId == account.Id).Sum(item => item.Amount) > 0)
                        ?? Accounts.FirstOrDefault()
                    : Accounts.FirstOrDefault(account => account.CanFund && account.Id == SelectedAccount?.Id)
                        ?? Accounts.FirstOrDefault(account => account.CanFund) ?? Accounts.FirstOrDefault();
            }
            if (sameContext && priceDirty)
            {
                _priceInput.Restore(priceDraft); AssumedPriceText = priceDraft.Text;
            }
            else
            {
                _priceInput.Initialize(AssumedPriceText, _goal is { } goal ? presenter.TypeOf(goal)?.PriceCurrencyCode ?? "EUR" : "EUR", localization.CurrentCulture);
            }
            if (_goal is { } loaded)
            {
                if (AmountText.Length == 0) _amountInput.Initialize(AmountText, loaded.CurrencyCode, localization.CurrentCulture);
            }
            RefreshMoneyInputs();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || identity != _identityVersion || _retired) { }
        catch
        {
            HasReadFailed = true;
            // A partially published read must not enable old controls against a new file or changed financial units.
            _needsReload = true;
            SaveError = translator["Error_Unexpected"];
            throw;
        }
        finally
        {
            _publishingDrafts = false; _refreshing = false;
            if (identity == _identityVersion && !_retired) { IsLoading = false; RefreshActions(); }
            _loadGate.Release();
        }
    }

    /// <summary>Retries publication without resubmitting a previously committed action or discarding dirty inputs.</summary>
    private async Task ReloadAsync()
    {
        if (IsBusy || _refreshing) return;
        IsBusy = true;
        try { await LoadAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!HasReadFailed) await interaction.ShowFailureAsync(exception);
            else System.Diagnostics.Debug.WriteLine("Original goal detail retry failed: " + exception.GetType().FullName);
        }
        finally { IsBusy = false; }
    }

    private async Task LoadCoreAsync(CancellationToken token, Action requireCurrent)
    {
        // Keep the established dependency surface; the captured goal reader owns all financial inputs.
        ArgumentNullException.ThrowIfNull(store); ArgumentNullException.ThrowIfNull(plans); ArgumentNullException.ThrowIfNull(holdings);
        var detail = await goals.ReadDetailSnapshotAsync(token); requireCurrent();
        var source = detail.Finances; var snapshot = source.Goals;
        _originalSource ??= snapshot;
        if (!_originalSource.HasSameOriginalFile(snapshot))
            throw new InvalidOperationException("The original goal detail cannot follow another selected profile.");
        var goal = snapshot.Goals.FirstOrDefault(item => item.Id == _id);
        if (goal is null)
        {
            _goal = null; _review = snapshot; NotFound = true; Summary = null;
            History.ReplaceAll([]); _historyRows.Clear(); PerAccount.ReplaceAll([]); TrendPeriods.ReplaceAll([]); return;
        }

        var today = Today; var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var settings = detail.Settings; var accounts = source.Accounts; var entries = source.Entries;
        var all = snapshot.Goals; var allocations = snapshot.Allocations;
        var facts = await Task.Run(() =>
        {
            var balances = GoalPresenter.Balances(accounts, entries, today);
            var progress = GoalProgressService.Evaluate(all, allocations, accounts, entries, snapshot.ContributionPlans,
                today, source.AssetEvents, source.AssetTypes).FirstOrDefault(item => item.Goal.Id == goal.Id);
            var trend = GoalTrendService.Compute(goal, progress?.Remaining ?? 0, accounts, entries,
                allocations.Where(item => item.GoalId == goal.Id), source.AssetEvents, today, calendar, settings.MonthStartDay);
            var coverage = settings.Shows(Feature.GoalDetails)
                ? Core.Reports.KpiCatalog.Coverage(accounts, entries, detail.Categories, detail.Schedules,
                    goal.CurrencyCode, today, calendar, settings.MonthStartDay).Months : null;
            var type = source.AssetTypes.FirstOrDefault(item => item.Id == goal.AssetTypeId);
            var plan = snapshot.ContributionPlans.FirstOrDefault(item => item.GoalId == goal.Id);
            var capacity = goal.Type == GoalType.HoldingQuantity && settings.Shows(Feature.GoalDetails)
                && type is not null && plan?.AssumedPricePerUnitMilli is not null
                ? Core.Reports.CapacityCalculator.Compute(accounts, entries, detail.Schedules, detail.States,
                    all, snapshot.ContributionPlans, type.PriceCurrencyCode, today, calendar, settings.MonthStartDay, goal.Id) : null;
            var earmarks = GoalCalculator.Accounts(all, allocations, balances).ToDictionary(item => item.AccountId);
            return (Balances: balances, Progress: progress, Trend: trend, Coverage: coverage, Capacity: capacity, Earmarks: earmarks);
        }, token);
        await goals.ValidateListSnapshotAsync(source, token); requireCurrent();
        _goal = goal; _review = snapshot; _financialReview = source; _work = snapshot.Work; NotFound = false;
        UnitNote = DisplayUnitNote.For(translator, goal.CurrencyCode);
        Summary = presenter.DetailRow(source, goal, facts.Progress); requireCurrent();
        CoverageText = facts.Coverage is { } months
            ? translator.Format("Goal_Coverage", translator.Format("Report_Months", months.ToString("0.0", localization.CurrentCulture))) : null;
        FormatTrend(goal, settings, facts.Trend, facts.Capacity); requireCurrent();
        IsActive = goal.State == GoalState.Active;
        IsArchived = goal.State == GoalState.Archived;
        IsEarmark = goal.Type == GoalType.Earmark;
        CanSetAside = IsEarmark && (CanContribute || allocations.Where(a => a.GoalId == goal.Id).GroupBy(a => a.AccountId).Any(g => g.Sum(a => a.Amount) > 0));
        CanPause = goal.State is GoalState.Active or GoalState.Paused;
        PauseText = translator[goal.State == GoalState.Paused ? "Goal_Resume" : "Goal_Pause"];
        CompleteText = goal.State is GoalState.Active or GoalState.Paused ? translator["Goal_Complete"] : translator["Goal_Reactivate"];
        CompleteHint = goal.State is GoalState.Active or GoalState.Paused ? translator["Goal_Complete_A11yHint"] : null;
        DirectionNames = [translator["Goal_SetAside"], translator["Goal_Release"]];

        var names = accounts.ToDictionary(a => a.Id, a => a.Name);
        var own = allocations.Where(a => a.GoalId == goal.Id).OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt).ToList();
        var perAccount = own.GroupBy(item => item.AccountId).Select(group => new { group.Key, Amount = group.Sum(item => item.Amount) })
            .Where(group => group.Amount != 0)
            .Select(group => translator.Format("Goal_InAccount", presenter.Money(group.Amount, goal.CurrencyCode), names.GetValueOrDefault(group.Key, "?"))).ToArray();
        var history = own.Select(allocation => new AllocationRow(allocation.Id,
            translator.Format(allocation.Amount < 0 ? "Goal_ReleasedFrom" : "Goal_SetAsideIn", names.GetValueOrDefault(allocation.AccountId, "?")),
            dates.Format(allocation.Date, DateFormatStyle.Long),
            MoneyText.Format(allocation.Amount, goal.CurrencyCode, localization.CurrentCulture, showPlus: true), allocation.Amount < 0)).ToArray();
        PerAccount.ReplaceAll(perAccount); requireCurrent(); History.ReplaceAll(history); requireCurrent();
        _historyRows = history.ToDictionary(item => item.Id);

        // Only accounts in the goal's currency can hold its money; free money is shown to avoid double earmarking.
        var earmarks = facts.Earmarks;
        Accounts =
        [
            // Money is set aside only in money accounts – never in a loan, money lent or a valued asset (ZEX-S0307).
            .. accounts
                .Where(a => !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                    || own.Where(item => item.AccountId == a.Id).Sum(item => item.Amount) > 0)
                .Select(a =>
                {
                    var free = earmarks.TryGetValue(a.Id, out var e) ? e.Unallocated : Math.Max(0, facts.Balances.GetValueOrDefault(a.Id));
                    return new FundingAccount(a.Id, a.Name, free, translator.Format("Goal_Free", presenter.Money(free, goal.CurrencyCode)))
                    { CanFund = !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase) };
                }),
        ];
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == SelectedAccount?.Id)
            ?? Accounts.OrderByDescending(a => accounts.First(x => x.Id == a.Id).Type == AccountType.Savings).ThenByDescending(a => a.Unallocated).FirstOrDefault();
        HasFundingAccounts = Accounts.Count > 0;
        NoAccountsText = HasFundingAccounts ? null : translator.Format("Goal_NoAccounts", goal.CurrencyCode);
        requireCurrent(); source.EnsureCurrent();
    }

    // The observed pace (design §9.2) – a separate line from the user's own plan, which always stays available.
    private void FormatTrend(Goal goal, Core.Settings.ZananceSettings settings, GoalTrend trend, Core.Reports.CapacityResult? capacity)
    {
        TrendText = trend.Status switch
        {
            TrendStatus.Ok => translator.Format("Goal_TrendEta", presenter.Amount(goal, trend.Pace!.Value), trend.CompletePeriods, dates.Format(trend.Eta!.Value, DateFormatStyle.MonthYear)),
            TrendStatus.NoPace => translator["Goal_TrendNoPace"],
            TrendStatus.NotEnoughHistory => translator.Format("Goal_TrendHistory", trend.CompletePeriods, GoalTrendService.MinimumPeriods),
            _ => null,
        };
        SoFarText = trend.Status != TrendStatus.Reached && trend.SoFar != 0 ? translator.Format("Goal_TrendSoFar", presenter.Amount(goal, trend.SoFar)) : null;
        TrendPeriods.ReplaceAll(settings.Shows(Feature.GoalDetails) ? trend.Periods.Select(period =>
        {
            var text = $"{dates.Format(period.From, DateFormatStyle.MonthYear)}: {presenter.Amount(goal, period.Contribution)}";
            return period.IsOneOff ? text + " · " + translator["Goal_TrendOneOff"] : text;
        }).ToArray() : []);

        HasTrendPeriods = TrendPeriods.Count > 0;

        // Quantity goals: money capacity becomes a quantity only at a price the user types (ZEX-S0703).
        IsQuantity = goal.Type == GoalType.HoldingQuantity;
        CapacityQuantityText = null;
        CanUseAssumedPrice = IsQuantity && settings.Shows(Feature.GoalDetails);
        CanEditAssumedPrice = false;
        AssumedPriceText = string.Empty;
        if (!CanUseAssumedPrice || presenter.TypeOf(goal) is not { } type)
        {
            return;
        }

        AssumedPriceLabel = translator.Format("Goal_AssumedPrice", type.PriceCurrencyCode, translator[type.Dimension == Core.Holdings.AssetDimension.Mass ? "Holding_PerGram" : "Holding_Piece"]);
        var plan = _review!.ContributionPlans.FirstOrDefault(p => p.GoalId == goal.Id);
        CanEditAssumedPrice = _work.CanContribute(goal.Id) || plan?.AssumedPricePerUnitMilli is not null;
        if (plan?.AssumedPricePerUnitMilli is { } price)
        {
            AssumedPriceText = MoneyText.ForInput((long)Math.Round(price / 1_000m, MidpointRounding.AwayFromZero), type.PriceCurrencyCode, localization.CurrentCulture);
            CapacityQuantityText = capacity?.Amount is { } none && none <= 0 ? translator["Goal_NoCapacity"]
                : capacity?.Amount is { } money
                ? translator.Format("Goal_CapacityQuantity", presenter.Amount(goal, Core.Reports.CapacityCalculator.QuantityFor(money, price)), MoneyText.Format(money, type.PriceCurrencyCode, localization.CurrentCulture))
                : translator["Report_NotEnoughHistory"];
        }
    }

    // Stores the user's assumed price, never a holding valuation. A retained recorded price can still be corrected.
    private async Task SaveAssumedPriceAsync()
    {
        if (!CanSaveAssumedPrice || _goal is not { } goal || presenter.TypeOf(goal) is not { } type || _review is not { } review) return;
        RefreshMoneyInputs();
        if (_priceInput.NeedsReview(type.PriceCurrencyCode)) return;
        var text = AssumedPriceText; var version = _moneyDraftVersion;
        long? priceMilli = null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (!_priceInput.TryParse(out var price) || price <= 0 || price > long.MaxValue / 1_000)
            { PriceError = translator["Amount_Invalid"]; return; }
            priceMilli = price * 1_000;
        }
        await RunWriteAsync(async () =>
        {
            var original = review.ContributionPlans.FirstOrDefault(p => p.GoalId == goal.Id);
            // Keep the loaded review immutable when the writer rejects this explicit price draft.
            var plan = original is null ? new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.FixedAmount }
                : new ContributionPlan
                {
                    GoalId = original.GoalId, Method = original.Method, Amount = original.Amount, Percent = original.Percent,
                    Rule = original.Rule.Clone(), CategoryIds = [.. original.CategoryIds], ReminderEnabled = original.ReminderEnabled,
                    AssumedPricePerUnitMilli = original.AssumedPricePerUnitMilli,
                };
            // Keep thousandths and the existing upper bound; no currency conversion or holding-price write.
            plan.AssumedPricePerUnitMilli = priceMilli;
            await goals.SaveContributionPlanAsync(goal.Id, plan, review: review,
                isCurrent: () => _writeCurrent?.Invoke() != false && version == _moneyDraftVersion && ReferenceEquals(_review, review) && !_priceInput.NeedsReview(type.PriceCurrencyCode));
            _priceBaseline = text;
            return true;
        }, accepted: () => _priceBaseline = text);
    }

    private async Task AllocateAsync()
    {
        if (!CanInteract || _goal is not { } goal || SelectedAccount is not { } account || _review is not { } review) return;
        RefreshMoneyInputs();
        if (_amountInput.NeedsReview(goal.CurrencyCode)) return;
        if (!CanAllocate) { AmountError = translator["ActiveItems_ReadOnly"]; return; }
        if (!_amountInput.TryParse(out var amount) || amount <= 0)
        { AmountError = translator["Amount_Invalid"]; return; }
        var direction = DirectionIndex; var version = _moneyDraftVersion;
        await RunWriteAsync(async () =>
        {
            // The reviewed writer rejects any intervening allocation change, including another release.
            if (direction == 1)
            {
                var earmarked = review.Allocations.Where(a => a.GoalId == goal.Id && a.AccountId == account.Id).Sum(a => a.Amount);
                if (amount > earmarked)
                { AmountError = translator.Format("Goal_ReleaseTooMuch", presenter.Money(Math.Max(0, earmarked), goal.CurrencyCode)); return false; }
            }
            AmountError = null;
            await goals.AddAllocationAsync(new GoalAllocation
            {
                GoalId = goal.Id, AccountId = account.Id, Amount = direction == 1 ? -amount : amount, Date = Today,
            }, review: review, isCurrent: () => _writeCurrent?.Invoke() != false && version == _moneyDraftVersion && ReferenceEquals(_review, review)
                && !_amountInput.NeedsReview(goal.CurrencyCode));
            AmountText = string.Empty;
            return true;
        }, accepted: () =>
        {
            // An observer can fail after acceptance but before the normal clear above; readback must not rearm it.
            if (version == _moneyDraftVersion) AmountText = string.Empty;
        });
    }

    private async Task DeleteAllocationAsync(AllocationRow row)
    {
        if (!CanInteract || _review is not { } review || !OwnsHistory(row)) return;
        await RunWriteAsync(async () =>
        {
            if (!await interaction.ConfirmAsync(translator["Goal_DeleteAllocation"], $"{row.Text} {row.AmountText}", translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await goals.DeleteAllocationAsync(row.Id, review: review, isCurrent: _writeCurrent); return true;
        });
    }

    private async Task EditAsync()
    {
        if (!CanInteract || _review is not { } review || _financialReview is not { } source || _goal is not { } goal) return;
        var identity = _identityVersion; var token = _readLifetime.Token; IsBusy = true;
        try
        {
            await goals.ValidateListSnapshotAsync(source, token);
            if (identity != _identityVersion || _retired || _refreshing || _needsReload || !CanView || !ReferenceEquals(_review, review)) return;
            await interaction.NavigateAsync(AppRoutes.GoalEditorRoute, new Dictionary<string, object>
            { ["id"] = goal.Id, ["goalEditorOrigin"] = source.CreateEditorOrigin() });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (identity != _identityVersion || _retired) return;
            // Native command failures keep this original draft covered; they must not escape as an async UI exception.
            _needsReload = true; SaveError = translator["Error_Unexpected"]; HasReadFailed = true; RefreshActions();
            System.Diagnostics.Debug.WriteLine("Original goal detail Edit refused: " + error.GetType().FullName);
        }
        finally { IsBusy = false; }
    }

    // Completion is always explicit; reopening and resuming independently require current new-work affordances.
    private async Task ToggleCompleteAsync()
    {
        if (!CanToggleComplete || _goal is not { } goal || _review is not { } review) return;
        var completing = goal.State is GoalState.Active or GoalState.Paused;
        await RunWriteAsync(async () =>
        {
            if (completing && !await interaction.ConfirmAsync(translator["Goal_Complete"], translator[goal.Type == GoalType.Earmark ? "Goal_CompleteMessage" : "Goal_CompleteBalanceMessage"], translator["Goal_Complete"], translator["Common_Cancel"])) return false;
            await WriteStateAsync(goal, review, draft =>
            {
                draft.State = completing ? GoalState.Completed : GoalState.Active;
                draft.CompletedAt = completing ? time.GetUtcNow() : null; draft.PausedAt = null;
            });
            return true;
        });
    }

    private async Task TogglePauseAsync()
    {
        if (!CanTogglePause || _goal is not { } goal || _review is not { } review) return;
        var pausing = goal.State == GoalState.Active;
        await RunWriteAsync(async () =>
        {
            await WriteStateAsync(goal, review, draft =>
            { draft.State = pausing ? GoalState.Paused : GoalState.Active; draft.PausedAt = pausing ? time.GetUtcNow() : null; });
            return true;
        });
    }

    private async Task RestoreAsync()
    {
        if (!CanRestore || _goal is not { } goal || _review is not { } review) return;
        await RunWriteAsync(async () => { await WriteStateAsync(goal, review, draft => draft.State = GoalState.Active); return true; });
    }

    // D-306: lifecycle changes belong to a detached draft, never the original reviewed object. The same
    // complete review must remain unchanged through confirmation, writer validation and a rejected Save.
    private async Task WriteStateAsync(Goal goal, GoalWorkSnapshot review, Action<Goal> change)
    {
        var draft = goal.Clone();
        change(draft);
        await goals.SaveGoalAsync(draft, review: review, isCurrent: _writeCurrent);
    }

    private async Task ArchiveAsync()
    {
        if (!CanInteract || _goal is not { State: GoalState.Active } goal || _review is not { } review) return;
        await RunWriteAsync(async () => { await WriteStateAsync(goal, review, draft => draft.State = GoalState.Archived); return true; },
            () => interaction.NavigateAsync(".."));
    }

    private async Task DeleteAsync()
    {
        if (!CanInteract || _goal is not { } goal || _review is not { } review) return;
        await RunWriteAsync(async () =>
        {
            if (!await interaction.ConfirmAsync(translator["Goal_Delete"], translator["Goal_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await goals.DeleteGoalAsync(goal.Id, review: review, isCurrent: _writeCurrent); return true;
        }, () => interaction.NavigateAsync(".."));
    }

    // One busy guard spans confirmation, the reviewed writer, feedback and post-commit publication/navigation.
    private async Task RunWriteAsync(Func<Task<bool>> operation, Func<Task>? completed = null, Action? accepted = null)
    {
        if (!CanInteract) return;
        var identity = _identityVersion; var original = _review;
        bool Current() => !_retired && identity == _identityVersion && ReferenceEquals(original, _review) && _goal?.Id == _id;
        IsBusy = true; SaveError = null; var committed = false; _writeCurrent = Current;
        try
        {
            committed = await operation();
            if (committed && Current()) { if (completed is null) await LoadAsync(); else await completed(); }
        }
        catch (Vafadar.Zanance.Data.Commerce.CommittedWriteNotificationException<Guid>)
        {
            // These original money writers publish their accepted identity before observers can fail.
            // Keep the form covered until readback; a notification error must not resubmit an earmark.
            if (!Current()) return;
            accepted?.Invoke();
            _needsReload = true; SaveError = translator["Occurrence_SavedRefreshFailed"];
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Current()) return;
            if (committed) { _needsReload = true; SaveError = translator["Occurrence_SavedRefreshFailed"]; }
            else
            {
                SaveError = Presentation.CommercialFailureMessage.Describe(exception, translator, draftKept: true);
                await interaction.ShowFailureAsync(exception);
            }
        }
        finally { _writeCurrent = null; IsBusy = false; }
    }
}
