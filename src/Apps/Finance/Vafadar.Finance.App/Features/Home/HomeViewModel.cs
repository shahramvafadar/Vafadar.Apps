using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Home;

/// <summary>Home dashboard (UI-02). Numbers use one filter set and are labelled as recorded, not bank, balances (FIN-13).</summary>
public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly FinanceStore _store;
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;

    public HomeViewModel(FinanceStore store, Translator translator, IDateFormatter dates, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _translator = translator;
        _dates = dates;
        _localization = localization;
        _time = time;
        Today = string.Empty;
    }

    [ObservableProperty]
    public partial string Today { get; set; }

    public ObservableCollection<CurrencyTotal> Balances { get; } = [];

    public ObservableCollection<AccountItem> Accounts { get; } = [];

    [ObservableProperty]
    public partial bool HasAccounts { get; set; }

    public async Task LoadAsync()
    {
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        Today = _translator.Format("Home_Today", _dates.Format(today, DateFormatStyle.Long));

        var accounts = (await _store.GetAccountsAsync(includeArchived: false)).ToList();
        var entries = await _store.GetEntriesAsync();
        var culture = _localization.CurrentCulture;

        Balances.Clear();
        foreach (var (currency, total) in LedgerCalculator.TotalBalances(accounts, entries, today))
        {
            Balances.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        Accounts.Clear();
        foreach (var account in accounts)
        {
            var balance = LedgerCalculator.Balance(account, entries, today);
            Accounts.Add(new AccountItem(account.Id, account.Name, _translator[$"AccountType_{account.Type}"],
                Icons.Parse(account.Icon, Icons.For(account.Type)), MoneyText.Format(balance, account.CurrencyCode, culture), balance < 0, !account.IncludeInTotals));
        }

        HasAccounts = Accounts.Count > 0;
    }

    [RelayCommand]
    private Task OpenAccountsAsync() => Shell.Current.GoToAsync(AppShell.AccountsRoute);
}
