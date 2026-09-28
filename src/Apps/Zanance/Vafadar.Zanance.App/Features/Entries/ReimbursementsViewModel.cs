using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Money others still owe for reimbursable expenses (F2-TX-03).</summary>
public sealed partial class ReimbursementsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IDateFormatter dates) : ViewModelBase
{
    public ObservableCollection<EntryRow> Items { get; } = [];

    [ObservableProperty]
    public partial string? TotalText { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public async Task LoadAsync()
    {
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var presenter = new EntryPresenter(accounts, new CategoryLookup(await store.GetCategoriesAsync(), translator), translator, localization.CurrentCulture);
        var culture = localization.CurrentCulture;
        var open = EntryActions.OpenReimbursements(await store.GetEntriesAsync());

        Items.Clear();
        foreach (var (expense, amount) in open)
        {
            var currency = presenter.CurrencyOf(expense.AccountId);
            Items.Add(presenter.Row(expense) with
            {
                Subtitle = $"{expense.ReimbursedBy ?? translator["Entry_ReimbursedBySomeone"]} · {dates.Format(expense.Date, DateFormatStyle.Short)}",
                AmountText = MoneyText.Format(amount, currency, culture),
            });
        }

        TotalText = string.Join(" · ", open.GroupBy(o => presenter.CurrencyOf(o.Expense.AccountId)).Select(g => MoneyText.Format(g.Sum(o => o.Open), g.Key, culture)));
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private Task OpenAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });
}