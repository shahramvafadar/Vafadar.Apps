using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>An account as shown in lists.</summary>
public sealed record AccountItem(Guid Id, string Name, string TypeName, Symbol Icon, string BalanceText, bool IsNegative, bool NotInTotals, bool OpeningUnknown = false)
{
    /// <summary>Gets the account type, which picks the tile colours: teal for savings, red for loans, sky for money lent (D-27).</summary>
    public AccountType Type { get; init; }

    /// <summary>Gets a value indicating whether this is the default account of new entries (ZEX-S0202).</summary>
    public bool IsDefault { get; init; }

    /// <summary>Gets the icon colour.</summary>
    public Color TileText => Look.Text;

    /// <summary>Gets the tile background.</summary>
    public Color TileBackground => Look.Background;

    /// <summary>Gets the tile outline.</summary>
    public Color TileStroke => Look.Line;

    private (Color Text, Color Background, Color Line) Look => Type switch
    {
        AccountType.Savings => (Palette.SavingText, Palette.SavingBackground, Palette.SavingLine),
        AccountType.Loan => PlanLook.Danger,
        AccountType.Lent => (Palette.RefundText, Palette.RefundBackground, Palette.RefundLine),
        _ => (Palette.SecondaryText, Palette.Get("SurfaceMuted"), Palette.DividerColor),
    };
}

/// <summary>A total per currency.</summary>
public sealed record CurrencyTotal(string CurrencyCode, string Text);

/// <summary>One group of the account list with its totals per currency, e.g. "Money · 2,000.00 EUR · 4,000.00 USD" (ZEX-S0202).</summary>
public sealed record AccountListGroup(AccountGroup Group, string Title, string? TotalsText, IReadOnlyList<AccountItem> Items);

public sealed partial class AccountsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, TimeProvider time, Vafadar.Localization.Formatting.IDateFormatter dates, HoldingStore holdings, Holdings.HoldingText holdingText) : ViewModelBase, Presentation.IThemeAware
{
    public ObservableCollection<AccountItem> Archived { get; } = [];

    public ObservableCollection<CurrencyTotal> Totals { get; } = [];

    /// <summary>Gets the groups in a fixed order: money, cards, owed to me, debts, valued assets (ZEX-S0202).</summary>
    public ObservableCollection<AccountListGroup> Groups { get; } = [];

    /// <summary>Gets the converted total of the accounts in totals (Advanced, only with a valuation currency).</summary>
    [ObservableProperty]
    public partial string? ConvertedText { get; set; }

    /// <summary>Gets the holdings per type, e.g. "18k gold 50.000 g · Coins 3 coins"; never summed (ZEX-AS05).</summary>
    [ObservableProperty]
    public partial string? HoldingsText { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasArchived { get; set; }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        var settings = await store.GetSettingsAsync();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var culture = localization.CurrentCulture;

        Archived.Clear();
        Totals.Clear();
        Groups.Clear();
        var grouped = new Dictionary<AccountGroup, List<(AccountItem Item, string Currency, long Balance)>>();

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
                !account.OpeningBalanceKnown) { Type = account.Type, IsDefault = account.Id == settings.DefaultAccountId };
            if (account.IsArchived)
            {
                Archived.Add(item);
                continue;
            }

            var group = account.Type.GroupOf();
            if (!grouped.TryGetValue(group, out var list))
            {
                grouped[group] = list = [];
            }

            list.Add((item, account.CurrencyCode, balance));
        }

        // Each group in exactly one place, with its own totals per currency; groups never mix (ZEX-MC12).
        foreach (var group in Enum.GetValues<AccountGroup>().Where(grouped.ContainsKey))
        {
            var rows = grouped[group];
            var totals = rows.GroupBy(r => r.Currency, StringComparer.OrdinalIgnoreCase)
                .Select(g => MoneyText.Format(g.Sum(r => r.Balance), g.Key, culture));
            Groups.Add(new AccountListGroup(group, translator[$"AccountGroup_{group}"], string.Join(" · ", totals), [.. rows.Select(r => r.Item)]));
        }

        var inTotals = LedgerCalculator.TotalBalances(accounts, entries, today);
        foreach (var (currency, total) in inTotals)
        {
            Totals.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        // Advanced: one converted line under the native totals, never instead of them (ZEX-MC08).
        ConvertedText = null;
        if (settings.Shows(Feature.ConvertedTotals) && settings.ValuationCurrencyEnabled && inTotals.Count > 1)
        {
            var combined = new Core.Rates.RateTable(await store.GetRatesAsync()).Combine(inTotals, settings.ReportCurrencyCode, today, new Core.Rates.RateFreshness(settings.RateFreshnessDays));
            ConvertedText = combined.IsComplete
                ? translator.Format(combined.IsOutdated ? "Home_CombinedOutdated" : "Home_Combined", MoneyText.Format(combined.Total!.Value, settings.ReportCurrencyCode, culture),
                    combined.OldestRateDate is { } date ? dates.Format(date, Vafadar.Localization.Formatting.DateFormatStyle.Short) : "-")
                : translator.Format("Home_CombinedIncomplete", string.Join(", ", combined.MissingCurrencies));
        }

        var types = (await holdings.GetTypesAsync()).Where(t => !t.IsArchived).ToList();
        var events = types.Count == 0 ? [] : await holdings.GetEventsAsync();
        var held = types.Select(t => (Type: t, Quantity: Core.Holdings.HoldingsLedger.Quantity(events, t.Id, today))).Where(h => h.Quantity > 0)
            .Select(h => $"{h.Type.Name} {holdingText.Quantity(h.Quantity, h.Type)}").ToList();
        HoldingsText = held.Count == 0 ? null : string.Join(" · ", held);

        IsEmpty = accounts.Count == 0;
        HasArchived = Archived.Count > 0;
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute);

    /// <summary>Opens the dedicated debt/receivable form without creating an account until confirmation.</summary>
    [RelayCommand]
    private Task AddDebtAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["debt"] = true });

    [RelayCommand]
    private Task OpenHoldingsAsync() => Shell.Current.GoToAsync(AppShell.HoldingsRoute);
}
