using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Earmarked money per currency.</summary>
public sealed record EarmarkLine(string Text);

/// <summary>
/// Savings goals (F2-GOAL-01..05): progress from money actually covered by account balances, the next suggested
/// contribution and any shortfall. Earmarking never moves money (BUD-11).
/// </summary>
public sealed partial class GoalsViewModel(
    ZananceStore store,
    GoalStore goals,
    GoalPresenter presenter,
    Translator translator,
    TimeProvider time,
    IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
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

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        var balances = GoalPresenter.Balances(accounts, entries, today);
        var snapshot = await goals.GetWorkSnapshotAsync();
        var all = snapshot.Goals;
        var allocations = snapshot.Allocations;
        var progress = (await presenter.EvaluateAsync(snapshot, accounts, entries, today)).ToDictionary(p => p.Goal.Id);
        await goals.ValidateWorkSnapshotAsync(snapshot);

        // Active and paused goals above, completed and archived ones below (ZEX-S0303).
        Active.Clear();
        Finished.Clear();
        foreach (var goal in all)
        {
            var row = presenter.Row(goal, progress.GetValueOrDefault(goal.Id), snapshot.Work);
            (goal.State is GoalState.Active or GoalState.Paused ? Active : Finished).Add(row);
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
    private Task AddAsync() => interaction.NavigateAsync(AppRoutes.GoalEditorRoute);

    [RelayCommand]
    private Task OpenAsync(GoalRow row) => interaction.NavigateAsync(AppRoutes.GoalDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
}
