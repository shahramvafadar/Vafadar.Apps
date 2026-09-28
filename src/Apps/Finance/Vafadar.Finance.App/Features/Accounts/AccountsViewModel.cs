using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Accounts;

/// <summary>An account as shown in lists.</summary>
public sealed record AccountItem(Guid Id, string Name, string TypeName, Symbol Icon, string BalanceText, bool IsNegative, bool NotInTotals, bool OpeningUnknown = false);

/// <summary>A total per currency.</summary>
public sealed record CurrencyTotal(string CurrencyCode, string Text);

public sealed partial class AccountsViewModel(FinanceStore store, Translator translator, ILocalizationService localization, TimeProvider time) : ViewModelBase
{
    public ObservableCollection<AccountItem> Active { get; } = [];

    public ObservableCollection<AccountItem> Archived { get; } = [];

    public ObservableCollection<CurrencyTotal> Totals { get; } = [];

    // Loans and money lent are listed apart from the money at hand (ACC-05, F2-DEBT-01).
    public ObservableCollection<AccountItem> Debts { get; } = [];

    [ObservableProperty]
    public partial string? DebtSummary { get; set; }

    [ObservableProperty]
    public partial bool HasDebts { get; set; }

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
        Debts.Clear();
        var owed = new Dictionary<string, long>();
        var lent = new Dictionary<string, long>();

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
                !account.IncludeInTotals,
                !account.OpeningBalanceKnown);
            if (account.IsArchived)
            {
                Archived.Add(item);
            }
            else if (account.Type.IsDebt())
            {
                Debts.Add(item);
                var target = balance < 0 ? owed : lent;
                target[account.CurrencyCode] = target.GetValueOrDefault(account.CurrencyCode) + Math.Abs(balance);
            }
            else
            {
                Active.Add(item);
            }
        }

        foreach (var (currency, total) in LedgerCalculator.TotalBalances(accounts.Where(a => !a.IsArchived), entries, today))
        {
            Totals.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        var parts = new List<string>();
        if (owed.Count > 0)
        {
            parts.Add(translator.Format("Accounts_IOwe", string.Join(" · ", owed.Where(o => o.Value != 0).Select(o => MoneyText.Format(o.Value, o.Key, culture)))));
        }

        if (lent.Values.Any(v => v != 0))
        {
            parts.Add(translator.Format("Accounts_OwedToMe", string.Join(" · ", lent.Where(o => o.Value != 0).Select(o => MoneyText.Format(o.Value, o.Key, culture)))));
        }

        DebtSummary = parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null;
        HasDebts = Debts.Count > 0;
        IsEmpty = accounts.Count == 0;
        HasArchived = Archived.Count > 0;
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute);
}
