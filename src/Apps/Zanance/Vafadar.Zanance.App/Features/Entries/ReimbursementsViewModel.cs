using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Money others still owe for reimbursable expenses (F2-TX-03).</summary>
public sealed partial class ReimbursementsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates) : ViewModelBase, Presentation.IThemeAware
{
    private EntryRow[] _rows = [];
    private string? _total;

    /// <summary>Gets the complete result, published with one collection notification.</summary>
    public SnapshotCollection<EntryRow> Items { get; } = [];

    /// <summary>Gets the initial/reload cover and retry state.</summary>
    public SnapshotLoadState Loading { get; } = new();

    /// <summary>Gets the complete outstanding totals, separately for each currency.</summary>
    [ObservableProperty]
    public partial string? TotalText { get; set; }

    /// <summary>Gets whether a successfully published snapshot has no outstanding expenses.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    /// <summary>Shares a pending read and exposes only a fully prepared result.</summary>
    public Task LoadAsync() => Loading.RunAsync(ReadSnapshotAsync, PresentSnapshot);

    /// <summary>Reads and calculates off the presentation thread, then prepares the current translated rows.</summary>
    private async Task ReadSnapshotAsync()
    {
        // D-352: even asynchronous SQLite materialization can occupy its caller; keep the complete read on a worker.
        var snapshot = await Task.Run(async () =>
        {
            var accounts = await store.GetAccountsAsync();
            var categories = await store.GetCategoriesAsync();
            var open = EntryActions.OpenReimbursements(await store.GetEntriesAsync());
            return (Accounts: accounts, Categories: categories, Open: open);
        });
        var culture = localization.CurrentCulture;
        var presenter = new EntryPresenter(snapshot.Accounts.ToDictionary(a => a.Id),
            new CategoryLookup(snapshot.Categories, translator), translator, culture);
        var rows = snapshot.Open.Select(item => presenter.Row(item.Expense) with
        {
            Subtitle = $"{item.Expense.ReimbursedBy ?? translator["Entry_ReimbursedBySomeone"]} · {dates.Format(item.Expense.Date, DateFormatStyle.Long)}",
            AmountText = MoneyText.Format(item.Open, presenter.CurrencyOf(item.Expense.AccountId), culture),
        }).ToArray();
        // Calculate every currency total before replacing any visible state; overflow keeps the prior result covered.
        var total = string.Join(" · ", snapshot.Open.GroupBy(o => presenter.CurrencyOf(o.Expense.AccountId))
            .Select(g => MoneyText.Format(g.Sum(o => o.Open), g.Key, culture)));
        _rows = rows;
        _total = total;
    }

    /// <summary>Publishes the complete rows and totals before removing the loading cover.</summary>
    private void PresentSnapshot()
    {
        Items.ReplaceAll(_rows);
        TotalText = _total;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private Task OpenAsync(EntryRow row)
    {
        // Refuse retained native targets while loading or after a new snapshot has retired their row object.
        if (!Loading.IsReady || !Items.Any(current => ReferenceEquals(current, row))) return Task.CompletedTask;
        return Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
    }
}
