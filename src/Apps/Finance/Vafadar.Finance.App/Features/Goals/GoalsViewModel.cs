using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.Core.Goals;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Goals;

/// <summary>Earmarked money per currency.</summary>
public sealed record EarmarkLine(string Text);

/// <summary>
/// Savings goals (F2-GOAL-01..05): progress from money actually covered by account balances, the next suggested
/// contribution and any shortfall. Earmarking never moves money (BUD-11).
/// </summary>
public sealed partial class GoalsViewModel(
    FinanceStore finance,
    GoalStore goals,
    GoalPresenter presenter,
    Translator translator,
    TimeProvider time) : ViewModelBase
{
    public ObservableCollection<GoalRow> Active { get; } = [];

    public ObservableCollection<GoalRow> Finished { get; } = [];

    public ObservableCollection<EarmarkLine> Earmarks { get; } = [];

    [ObservableProperty]
    public partial bool HasActive { get; set; }

    [ObservableProperty]
    public partial bool HasFinished { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public async Task LoadAsync()
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var accounts = await finance.GetAccountsAsync();
        var balances = GoalPresenter.Balances(accounts, await finance.GetEntriesAsync(), today);
        var all = await goals.GetGoalsAsync();
        var allocations = await goals.GetAllocationsAsync();
        var status = GoalCalculator.Evaluate(all, allocations, balances, today).ToDictionary(s => s.Goal.Id);

        Active.Clear();
        Finished.Clear();
        foreach (var goal in all)
        {
            var row = presenter.Row(goal, status.GetValueOrDefault(goal.Id), today);
            (goal.State == GoalState.Active ? Active : Finished).Add(row);
        }

        // Earmarked and still free money per currency, so the user sees what is not yet assigned (BUD-12).
        Earmarks.Clear();
        var byCurrency = accounts.ToDictionary(a => a.Id, a => a.CurrencyCode);
        foreach (var group in GoalCalculator.Accounts(all, allocations, balances).GroupBy(e => byCurrency.GetValueOrDefault(e.AccountId, "EUR")))
        {
            var text = translator.Format("Goals_Earmarked", presenter.Money(group.Sum(e => e.Earmarked), group.Key), presenter.Money(group.Sum(e => e.Unallocated), group.Key));
            var shortfall = group.Sum(e => e.Shortfall);
            Earmarks.Add(new EarmarkLine(shortfall > 0 ? text + " · " + translator.Format("Goals_Shortfall", presenter.Money(shortfall, group.Key)) : text));
        }

        HasActive = Active.Count > 0;
        HasFinished = Finished.Count > 0;
        IsEmpty = all.Count == 0;
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.GoalEditorRoute);

    [RelayCommand]
    private Task OpenAsync(GoalRow row) => Shell.Current.GoToAsync(AppShell.GoalDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
}