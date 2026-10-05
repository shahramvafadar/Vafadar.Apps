using System.Collections.ObjectModel;
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

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>An account that can fund the goal.</summary>
public sealed record FundingAccount(Guid Id, string Name, long Unallocated, string UnallocatedText)
{
    public override string ToString() => Name;
}

/// <summary>One allocation or release in the history.</summary>
public sealed record AllocationRow(Guid Id, string Text, string DateText, string AmountText, Color AmountColor);

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
    TimeProvider time) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _id;
    private Goal? _goal;

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

    // Loading would replace a typed assumed price with the saved one; both typed amounts stay.
    async Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        var (price, amount) = (AssumedPriceText, AmountText);
        await LoadAsync();
        (AssumedPriceText, AmountText) = (price, amount);
    }

    public async Task LoadAsync()
    {
        _goal = await goals.GetGoalAsync(_id);
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
        var all = await goals.GetGoalsAsync();
        var allocations = await goals.GetAllocationsAsync();
        var progress = (await presenter.EvaluateAsync(goals, accounts, entries, today, all)).FirstOrDefault(p => p.Goal.Id == goal.Id);
        Summary = presenter.Row(goal, progress);
        await LoadTrendAsync(goal, progress?.Remaining ?? 0, settings, accounts, entries);
        IsActive = goal.State == GoalState.Active;
        IsArchived = goal.State == GoalState.Archived;
        IsEarmark = goal.Type == GoalType.Earmark;
        CanSetAside = IsActive && IsEarmark;
        CanPause = goal.State is GoalState.Active or GoalState.Paused;
        PauseText = translator[goal.State == GoalState.Paused ? "Goal_Resume" : "Goal_Pause"];
        CompleteText = goal.State is GoalState.Active or GoalState.Paused ? translator["Goal_Complete"] : translator["Goal_Reactivate"];
        DirectionNames = [translator["Goal_SetAside"], translator["Goal_Release"]];

        var names = accounts.ToDictionary(a => a.Id, a => a.Name);
        var own = allocations.Where(a => a.GoalId == goal.Id).ToList();
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
                release ? Palette.SecondaryText : Palette.IncomeText));
        }

        // Only accounts in the goal's currency can hold its money; free money is shown to avoid double earmarking.
        var earmarks = GoalCalculator.Accounts(all, allocations, balances).ToDictionary(e => e.AccountId);
        Accounts =
        [
            // Money is set aside only in money accounts – never in a loan, money lent or a valued asset (ZEX-S0307).
            .. accounts
                .Where(a => !a.IsArchived && !a.Type.IsOutsideCash() && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                .Select(a =>
                {
                    var free = earmarks.TryGetValue(a.Id, out var e) ? e.Unallocated : Math.Max(0, balances.GetValueOrDefault(a.Id));
                    return new FundingAccount(a.Id, a.Name, free, translator.Format("Goal_Free", presenter.Money(free, goal.CurrencyCode)));
                }),
        ];
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == SelectedAccount?.Id)
            ?? Accounts.OrderByDescending(a => accounts.First(x => x.Id == a.Id).Type == AccountType.Savings).ThenByDescending(a => a.Unallocated).FirstOrDefault();
        HasFundingAccounts = Accounts.Count > 0;
        NoAccountsText = HasFundingAccounts ? null : translator.Format("Goal_NoAccounts", goal.CurrencyCode);
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
        if (!CanUseAssumedPrice || presenter.TypeOf(goal) is not { } type)
        {
            return;
        }

        AssumedPriceLabel = translator.Format("Goal_AssumedPrice", type.PriceCurrencyCode, translator[type.Dimension == Core.Holdings.AssetDimension.Mass ? "Holding_PerGram" : "Holding_Piece"]);
        var plan = (await goals.GetContributionPlansAsync()).FirstOrDefault(p => p.GoalId == goal.Id);
        if (plan?.AssumedPricePerUnitMilli is { } price)
        {
            AssumedPriceText = MoneyText.ForInput((long)Math.Round(price / 1_000m, MidpointRounding.AwayFromZero), type.PriceCurrencyCode, localization.CurrentCulture);
            var capacity = Core.Reports.CapacityCalculator.Compute(accounts, entries, await plans.GetSchedulesAsync(), await plans.GetStatesAsync(),
                await goals.GetGoalsAsync(), await goals.GetContributionPlansAsync(), type.PriceCurrencyCode, Today, calendar, settings.MonthStartDay, goal.Id);
            CapacityQuantityText = capacity.Amount is { } none && none <= 0 ? translator["Goal_NoCapacity"]
                : capacity.Amount is { } money
                ? translator.Format("Goal_CapacityQuantity", presenter.Amount(goal, Core.Reports.CapacityCalculator.QuantityFor(money, price)), MoneyText.Format(money, type.PriceCurrencyCode, localization.CurrentCulture))
                : translator["Report_NotEnoughHistory"];
        }
    }

    // Stores the assumed price with the goal's plan; it is never a valuation of the holding.
    [RelayCommand]
    private async Task SaveAssumedPriceAsync()
    {
        if (_goal is not { } goal || presenter.TypeOf(goal) is not { } type)
        {
            return;
        }

        var plan = (await goals.GetContributionPlansAsync()).FirstOrDefault(p => p.GoalId == goal.Id) ?? new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.FixedAmount };
        // Kept in thousandths; a price too large for that is not stored (as CR08-06).
        plan.AssumedPricePerUnitMilli = MoneyText.TryParse(AssumedPriceText, type.PriceCurrencyCode, localization.CurrentCulture, out var price) && price > 0 && price <= long.MaxValue / 1_000 ? price * 1_000 : null;
        await goals.SaveContributionPlanAsync(goal.Id, plan);
        await LoadAsync();
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

    [RelayCommand]
    private async Task AllocateAsync()
    {
        if (_goal is null || SelectedAccount is null || IsBusy)
        {
            return;
        }

        if (!MoneyText.TryParse(AmountText, Currencies.Get(_goal.CurrencyCode), localization.CurrentCulture, out var amount) || amount <= 0)
        {
            AmountError = translator["Amount_Invalid"];
            return;
        }

        // A release can only give back what is set aside for this goal in that account (ZEX-S0307).
        if (DirectionIndex == 1)
        {
            var earmarked = (await goals.GetAllocationsAsync(_goal.Id)).Where(a => a.AccountId == SelectedAccount.Id).Sum(a => a.Amount);
            if (amount > earmarked)
            {
                AmountError = translator.Format("Goal_ReleaseTooMuch", presenter.Money(Math.Max(0, earmarked), _goal.CurrencyCode));
                return;
            }
        }

        AmountError = null;
        IsBusy = true;
        try
        {
            await goals.AddAllocationAsync(new GoalAllocation
            {
                GoalId = _goal.Id,
                AccountId = SelectedAccount.Id,
                Amount = DirectionIndex == 1 ? -amount : amount,
                Date = Today,
            });
            AmountText = string.Empty;
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAllocationAsync(AllocationRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Goal_DeleteAllocation"], $"{row.Text} {row.AmountText}", translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await goals.DeleteAllocationAsync(row.Id);
            await LoadAsync();
        }
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.GoalEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    // Completing a goal releases its earmark for the funding view; its history stays (F2-GOAL-03, ZEX-GO13). Completing
    // is the user's decision ("I reached it / I bought it"), never set because a balance once touched the target.
    [RelayCommand]
    private async Task ToggleCompleteAsync()
    {
        if (_goal is null)
        {
            return;
        }

        var completing = _goal.State is GoalState.Active or GoalState.Paused;
        if (completing && !await Shell.Current.DisplayAlertAsync(translator["Goal_Complete"], translator[_goal.Type == GoalType.Earmark ? "Goal_CompleteMessage" : "Goal_CompleteBalanceMessage"], translator["Goal_Complete"], translator["Common_Cancel"]))
        {
            return;
        }

        _goal.State = completing ? GoalState.Completed : GoalState.Active;
        _goal.CompletedAt = completing ? time.GetUtcNow() : null;
        _goal.PausedAt = null;
        await SaveStateAsync();
    }

    // A paused goal keeps its data and money set aside, but leaves Home and the suggestions until resumed (ZEX-P13).
    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        if (_goal is null)
        {
            return;
        }

        var pausing = _goal.State == GoalState.Active;
        _goal.State = pausing ? GoalState.Paused : GoalState.Active;
        _goal.PausedAt = pausing ? time.GetUtcNow() : null;
        await SaveStateAsync();
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (_goal is null)
        {
            return;
        }

        _goal.State = GoalState.Active;
        await SaveStateAsync();
    }

    // Another balance goal may follow the account by now (ZEX-P12): then the state stays and the user is told.
    private async Task SaveStateAsync()
    {
        try
        {
            await goals.SaveGoalAsync(_goal!);
        }
        catch (InvalidOperationException)
        {
            await Shell.Current.DisplayAlertAsync(translator["Goal_Details"], translator["Goal_AccountHasGoal"], translator["Common_Ok"]);
            _goal = await goals.GetGoalAsync(_id);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (_goal is null)
        {
            return;
        }

        _goal.State = GoalState.Archived;
        await goals.SaveGoalAsync(_goal);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_goal is not null && await Shell.Current.DisplayAlertAsync(translator["Goal_Delete"], translator["Goal_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await goals.DeleteGoalAsync(_goal.Id);
            await Shell.Current.GoToAsync("..");
        }
    }
}
