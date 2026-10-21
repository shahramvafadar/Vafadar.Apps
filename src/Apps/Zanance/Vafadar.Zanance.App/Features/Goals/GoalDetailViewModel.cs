using System.Collections.ObjectModel;
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
    private Guid _id;
    private Goal? _goal;
    private GoalWorkPolicy _work = GoalWorkPolicy.Inactive;
    private bool _refreshing;
    private GoalWorkSnapshot? _review;
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
    public bool CanInteract => _goal is not null && !_refreshing && !_needsReload && IsNotBusy;
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
        if (e.PropertyName is nameof(IsBusy) or nameof(DirectionIndex) or nameof(SelectedAccount) or nameof(CanEditAssumedPrice)) RefreshActions();
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

    public ObservableCollection<AllocationRow> History { get; } = [];

    public ObservableCollection<string> PerAccount { get; } = [];

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
    public ObservableCollection<string> TrendPeriods { get; } = [];

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
            _id = id;
        }
    }

    // Display and choice reloads retain explicit dirty inputs in their original currency/culture, never reinterpret money.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        await _loadGate.WaitAsync();
        _refreshing = true; RefreshActions();
        var previousScope = _review?.ReminderScope; var previousGoal = _goal?.Id;
        var priceDirty = AssumedPriceText != _priceBaseline || _priceNeedsReview
            || _goal is { } original && presenter.TypeOf(original) is { } originalType && _priceInput.NeedsReview(originalType.PriceCurrencyCode);
        var priceDraft = _priceInput.Capture();
        try
        {
            _publishingDrafts = true;
            await LoadCoreAsync();
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
        catch
        {
            // A partially published read must not enable old controls against a new file or changed financial units.
            _needsReload = true;
            SaveError = translator["Error_Unexpected"];
            throw;
        }
        finally { _publishingDrafts = false; _refreshing = false; RefreshActions(); _loadGate.Release(); }
    }

    /// <summary>Retries publication without resubmitting a previously committed action or discarding dirty inputs.</summary>
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ReloadAsync()
    {
        if (IsBusy || _refreshing) return;
        IsBusy = true;
        try { await LoadAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { await interaction.ShowFailureAsync(exception); }
        finally { IsBusy = false; }
    }

    private async Task LoadCoreAsync()
    {
        var snapshot = await goals.GetWorkSnapshotAsync();
        _goal = snapshot.Goals.FirstOrDefault(goal => goal.Id == _id);
        _review = snapshot;
        NotFound = _goal is null;
        if (_goal is not { } goal)
        {
            return;
        }

        UnitNote = DisplayUnitNote.For(translator, goal.CurrencyCode);

        // The ledger is read once per load; coverage, trend, capacity and progress all work on the same data.
        var settings = await store.GetSettingsAsync();
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        await LoadCoverageAsync(goal, settings, accounts, entries);

        var today = Today;
        var balances = GoalPresenter.Balances(accounts, entries, today);
        var all = snapshot.Goals;
        var allocations = snapshot.Allocations;
        var progress = (await presenter.EvaluateAsync(snapshot, accounts, entries, today)).FirstOrDefault(p => p.Goal.Id == goal.Id);
        await goals.ValidateWorkSnapshotAsync(snapshot);
        _work = snapshot.Work;
        _review = snapshot;
        Summary = presenter.Row(goal, progress, _work);
        await LoadTrendAsync(goal, progress?.Remaining ?? 0, settings, accounts, entries);
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
        PerAccount.Clear();
        foreach (var group in own.GroupBy(a => a.AccountId))
        {
            var sum = group.Sum(a => a.Amount);
            if (sum != 0)
            {
                PerAccount.Add(translator.Format("Goal_InAccount", presenter.Money(sum, goal.CurrencyCode), names.GetValueOrDefault(group.Key, "?")));
            }
        }

        History.Clear();
        foreach (var allocation in own)
        {
            var release = allocation.Amount < 0;
            History.Add(new AllocationRow(
                allocation.Id,
                translator.Format(release ? "Goal_ReleasedFrom" : "Goal_SetAsideIn", names.GetValueOrDefault(allocation.AccountId, "?")),
                dates.Format(allocation.Date, DateFormatStyle.Long),
                MoneyText.Format(allocation.Amount, goal.CurrencyCode, localization.CurrentCulture, showPlus: true),
                release));
        }

        // Only accounts in the goal's currency can hold its money; free money is shown to avoid double earmarking.
        var earmarks = GoalCalculator.Accounts(all, allocations, balances).ToDictionary(e => e.AccountId);
        Accounts =
        [
            // Money is set aside only in money accounts – never in a loan, money lent or a valued asset (ZEX-S0307).
            .. accounts
                .Where(a => !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                    || own.Where(item => item.AccountId == a.Id).Sum(item => item.Amount) > 0)
                .Select(a =>
                {
                    var free = earmarks.TryGetValue(a.Id, out var e) ? e.Unallocated : Math.Max(0, balances.GetValueOrDefault(a.Id));
                    return new FundingAccount(a.Id, a.Name, free, translator.Format("Goal_Free", presenter.Money(free, goal.CurrencyCode)))
                    { CanFund = !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase) };
                }),
        ];
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == SelectedAccount?.Id)
            ?? Accounts.OrderByDescending(a => accounts.First(x => x.Id == a.Id).Type == AccountType.Savings).ThenByDescending(a => a.Unallocated).FirstOrDefault();
        HasFundingAccounts = Accounts.Count > 0;
        NoAccountsText = HasFundingAccounts ? null : translator.Format("Goal_NoAccounts", goal.CurrencyCode);
        await goals.ValidateWorkSnapshotAsync(snapshot);
    }

    // The observed pace (design §9.2) – a separate line from the user's own plan, which always stays available.
    private async Task LoadTrendAsync(Goal goal, long remaining, Core.Settings.ZananceSettings settings, List<Account> accounts, List<Core.Ledger.LedgerEntry> entries)
    {
        var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var trend = GoalTrendService.Compute(goal, remaining, accounts, entries, await goals.GetAllocationsAsync(goal.Id), await holdings.GetEventsAsync(), Today, calendar, settings.MonthStartDay);
        TrendText = trend.Status switch
        {
            TrendStatus.Ok => translator.Format("Goal_TrendEta", presenter.Amount(goal, trend.Pace!.Value), trend.CompletePeriods, dates.Format(trend.Eta!.Value, DateFormatStyle.MonthYear)),
            TrendStatus.NoPace => translator["Goal_TrendNoPace"],
            TrendStatus.NotEnoughHistory => translator.Format("Goal_TrendHistory", trend.CompletePeriods, GoalTrendService.MinimumPeriods),
            _ => null,
        };
        SoFarText = trend.Status != TrendStatus.Reached && trend.SoFar != 0 ? translator.Format("Goal_TrendSoFar", presenter.Amount(goal, trend.SoFar)) : null;
        TrendPeriods.Clear();
        if (settings.Shows(Feature.GoalDetails))
        {
            foreach (var period in trend.Periods)
            {
                var text = $"{dates.Format(period.From, DateFormatStyle.MonthYear)}: {presenter.Amount(goal, period.Contribution)}";
                TrendPeriods.Add(period.IsOneOff ? text + " · " + translator["Goal_TrendOneOff"] : text);
            }
        }

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
            var capacity = Core.Reports.CapacityCalculator.Compute(accounts, entries, await plans.GetSchedulesAsync(), await plans.GetStatesAsync(),
                _review!.Goals, _review.ContributionPlans, type.PriceCurrencyCode, Today, calendar, settings.MonthStartDay, goal.Id);
            CapacityQuantityText = capacity.Amount is { } none && none <= 0 ? translator["Goal_NoCapacity"]
                : capacity.Amount is { } money
                ? translator.Format("Goal_CapacityQuantity", presenter.Amount(goal, Core.Reports.CapacityCalculator.QuantityFor(money, price)), MoneyText.Format(money, type.PriceCurrencyCode, localization.CurrentCulture))
                : translator["Report_NotEnoughHistory"];
        }
    }

    // Stores the user's assumed price, never a holding valuation. A retained recorded price can still be corrected.
    [RelayCommand(CanExecute = nameof(CanSaveAssumedPrice))]
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
                isCurrent: () => version == _moneyDraftVersion && ReferenceEquals(_review, review) && !_priceInput.NeedsReview(type.PriceCurrencyCode));
            _priceBaseline = text;
            return true;
        }, accepted: () => _priceBaseline = text);
    }

    // For an emergency fund the question is how long the money would last; shown in Advanced (ZEX-S0612).
    private async Task LoadCoverageAsync(Goal goal, Core.Settings.ZananceSettings settings, List<Account> accounts, List<Core.Ledger.LedgerEntry> entries)
    {
        CoverageText = null;
        if (!settings.Shows(Feature.GoalDetails))
        {
            return;
        }

        var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var coverage = Core.Reports.KpiCatalog.Coverage(accounts, entries, await store.GetCategoriesAsync(), await plans.GetSchedulesAsync(), goal.CurrencyCode, Today, calendar, settings.MonthStartDay);
        CoverageText = coverage.Months is { } months
            ? translator.Format("Goal_Coverage", translator.Format("Report_Months", months.ToString("0.0", localization.CurrentCulture)))
            : null;
    }

    [RelayCommand(CanExecute = nameof(CanAllocate))]
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
            }, review: review, isCurrent: () => version == _moneyDraftVersion && ReferenceEquals(_review, review)
                && !_amountInput.NeedsReview(goal.CurrencyCode));
            AmountText = string.Empty;
            return true;
        }, accepted: () =>
        {
            // An observer can fail after acceptance but before the normal clear above; readback must not rearm it.
            if (version == _moneyDraftVersion) AmountText = string.Empty;
        });
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task DeleteAllocationAsync(AllocationRow row)
    {
        if (!CanInteract || _review is not { } review || !History.Contains(row)) return;
        await RunWriteAsync(async () =>
        {
            if (!await interaction.ConfirmAsync(translator["Goal_DeleteAllocation"], $"{row.Text} {row.AmountText}", translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await goals.DeleteAllocationAsync(row.Id, review: review); return true;
        });
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private Task EditAsync() => interaction.NavigateAsync(AppRoutes.GoalEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    // Completion is always explicit; reopening and resuming independently require current new-work affordances.
    [RelayCommand(CanExecute = nameof(CanToggleComplete))]
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

    [RelayCommand(CanExecute = nameof(CanTogglePause))]
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

    [RelayCommand(CanExecute = nameof(CanRestore))]
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
        await goals.SaveGoalAsync(draft, review: review);
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task ArchiveAsync()
    {
        if (!CanInteract || _goal is not { State: GoalState.Active } goal || _review is not { } review) return;
        await RunWriteAsync(async () => { await WriteStateAsync(goal, review, draft => draft.State = GoalState.Archived); return true; },
            () => interaction.NavigateAsync(".."));
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task DeleteAsync()
    {
        if (!CanInteract || _goal is not { } goal || _review is not { } review) return;
        await RunWriteAsync(async () =>
        {
            if (!await interaction.ConfirmAsync(translator["Goal_Delete"], translator["Goal_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"])) return false;
            await goals.DeleteGoalAsync(goal.Id, review: review); return true;
        }, () => interaction.NavigateAsync(".."));
    }

    // One busy guard spans confirmation, the reviewed writer, feedback and post-commit publication/navigation.
    private async Task RunWriteAsync(Func<Task<bool>> operation, Func<Task>? completed = null, Action? accepted = null)
    {
        if (!CanInteract) return;
        IsBusy = true; SaveError = null; var committed = false;
        try
        {
            committed = await operation();
            if (committed) { if (completed is null) await LoadAsync(); else await completed(); }
        }
        catch (Vafadar.Zanance.Data.Commerce.CommittedWriteNotificationException<Guid>)
        {
            // These original money writers publish their accepted identity before observers can fail.
            // Keep the form covered until readback; a notification error must not resubmit an earmark.
            accepted?.Invoke();
            _needsReload = true; SaveError = translator["Occurrence_SavedRefreshFailed"];
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (committed) { _needsReload = true; SaveError = translator["Occurrence_SavedRefreshFailed"]; }
            else
            {
                SaveError = Presentation.CommercialFailureMessage.Describe(exception, translator, draftKept: true);
                await interaction.ShowFailureAsync(exception);
            }
        }
        finally { IsBusy = false; }
    }
}
