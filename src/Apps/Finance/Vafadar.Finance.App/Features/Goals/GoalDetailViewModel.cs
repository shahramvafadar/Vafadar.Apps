using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Goals;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Goals;

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
    FinanceStore finance,
    GoalStore goals,
    GoalPresenter presenter,
    Translator translator,
    IDateFormatter dates,
    ILocalizationService localization,
    TimeProvider time) : ViewModelBase, IQueryAttributable
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

    [ObservableProperty]
    public partial string? CompleteText { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        _goal = await goals.GetGoalAsync(_id);
        NotFound = _goal is null;
        if (_goal is not { } goal)
        {
            return;
        }

        var today = Today;
        var accounts = await finance.GetAccountsAsync();
        var balances = GoalPresenter.Balances(accounts, await finance.GetEntriesAsync(), today);
        var all = await goals.GetGoalsAsync();
        var allocations = await goals.GetAllocationsAsync();
        var status = GoalCalculator.Evaluate(all, allocations, balances, today).FirstOrDefault(s => s.Goal.Id == goal.Id);
        Summary = presenter.Row(goal, status, today);
        IsActive = goal.State == GoalState.Active;
        CompleteText = IsActive ? translator["Goal_Complete"] : translator["Goal_Reactivate"];
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
                dates.Format(allocation.Date, DateFormatStyle.Short),
                MoneyText.Format(allocation.Amount, goal.CurrencyCode, localization.CurrentCulture, showPlus: true),
                release ? Color.FromArgb("#5F6368") : Color.FromArgb("#1B5E20")));
        }

        // Only accounts in the goal's currency can hold its money; free money is shown to avoid double earmarking.
        var earmarks = GoalCalculator.Accounts(all, allocations, balances).ToDictionary(e => e.AccountId);
        Accounts =
        [
            .. accounts
                .Where(a => !a.IsArchived && string.Equals(a.CurrencyCode, goal.CurrencyCode, StringComparison.OrdinalIgnoreCase))
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

    [RelayCommand]
    private async Task AllocateAsync()
    {
        if (_goal is null || SelectedAccount is null || IsBusy)
        {
            return;
        }

        if (!MoneyAmount.TryParse(AmountText, Currencies.Get(_goal.CurrencyCode), localization.CurrentCulture, out var amount) || amount <= 0)
        {
            AmountError = translator["Amount_Invalid"];
            return;
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

    // Completing a goal releases its earmark for the funding view; its history stays (F2-GOAL-03).
    [RelayCommand]
    private async Task ToggleCompleteAsync()
    {
        if (_goal is null)
        {
            return;
        }

        _goal.State = _goal.State == GoalState.Active ? GoalState.Completed : GoalState.Active;
        await goals.SaveGoalAsync(_goal);
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