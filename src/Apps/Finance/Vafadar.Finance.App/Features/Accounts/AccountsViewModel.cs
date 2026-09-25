using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Accounts;

/// <summary>An account as shown in lists.</summary>
public sealed record AccountItem(Guid Id, string Name, string TypeName, Symbol Icon, string BalanceText, bool IsNegative, bool NotInTotals);

/// <summary>A total per currency.</summary>
public sealed record CurrencyTotal(string CurrencyCode, string Text);

public sealed partial class AccountsViewModel(FinanceStore store, Translator translator, ILocalizationService localization, TimeProvider time) : ViewModelBase
{
    public ObservableCollection<AccountItem> Active { get; } = [];

    public ObservableCollection<AccountItem> Archived { get; } = [];

    public ObservableCollection<CurrencyTotal> Totals { get; } = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasArchived { get; set; }

    public async Task LoadAsync()
    {
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var culture = localization.CurrentCulture;

        Active.Clear();
        Archived.Clear();
        Totals.Clear();

        foreach (var account in accounts)
        {
            var balance = LedgerCalculator.Balance(account, entries, today);
            var item = new AccountItem(
                account.Id,
                account.Name,
                translator[$"AccountType_{account.Type}"],
                Icons.Parse(account.Icon, Icons.For(account.Type)),
                MoneyText.Format(balance, account.CurrencyCode, culture),
                balance < 0,
                !account.IncludeInTotals);
            (account.IsArchived ? Archived : Active).Add(item);
        }

        foreach (var (currency, total) in LedgerCalculator.TotalBalances(accounts.Where(a => !a.IsArchived), entries, today))
        {
            Totals.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        IsEmpty = accounts.Count == 0;
        HasArchived = Archived.Count > 0;
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute);
}
