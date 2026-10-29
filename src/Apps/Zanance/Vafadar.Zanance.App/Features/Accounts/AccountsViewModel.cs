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
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>An account as shown in lists.</summary>
public sealed partial record AccountItem(Guid Id, string Name, string TypeName, Symbol Icon, string BalanceText, bool IsNegative, bool NotInTotals, bool OpeningUnknown = false)
{
    /// <summary>Gets the account type, which picks the tile colours: teal for savings, red for loans, sky for money lent (D-27).</summary>
    public AccountType Type { get; init; }

    /// <summary>Gets a value indicating whether this is the default account of new entries (ZEX-S0202).</summary>
    public bool IsDefault { get; init; }


}

/// <summary>A total per currency.</summary>
public sealed record CurrencyTotal(string CurrencyCode, string Text);

/// <summary>One group of the account list with its totals per currency, e.g. "Money · 2,000.00 EUR · 4,000.00 USD" (ZEX-S0202).</summary>
public sealed record AccountListGroup(AccountGroup Group, string Title, string? TotalsText, IReadOnlyList<AccountItem> Items);

/// <summary>Publishes the complete original account overview only under its current original-file page cover.</summary>
public sealed partial class AccountsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, TimeProvider time, Vafadar.Localization.Formatting.IDateFormatter dates, Holdings.HoldingText holdingText, IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
{
    private CancellationTokenSource _lifetime = new();
    private int _revision;
    private AccountOverviewSnapshot? _displayed;

    /// <summary>Gets archived original rows, published by one native reset while covered.</summary>
    public SnapshotCollection<AccountItem> Archived { get; } = [];

    /// <summary>Gets original included-account totals separately for each currency.</summary>
    public SnapshotCollection<CurrencyTotal> Totals { get; } = [];

    /// <summary>Gets the groups in a fixed order: money, cards, owed to me, debts, valued assets (ZEX-S0202).</summary>
    public SnapshotCollection<AccountListGroup> Groups { get; } = [];

    /// <summary>Gets the converted total of the accounts in totals (Advanced, only with a valuation currency).</summary>
    [ObservableProperty]
    public partial string? ConvertedText { get; set; }

    /// <summary>Gets the holdings per type, e.g. "18k gold 50.000 g · Coins 3 coins"; never summed (ZEX-AS05).</summary>
    [ObservableProperty]
    public partial string? HoldingsText { get; set; }

    /// <summary>Gets whether the complete successfully read original source has no account.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>Gets whether the same complete original source has archived accounts.</summary>
    [ObservableProperty]
    public partial bool HasArchived { get; set; }

    /// <summary>Gets the initial, refresh and retry cover for this original page lifetime.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    async Task Presentation.IThemeAware.RefreshThemeAsync() { Retire(); await LoadAsync(); }

    /// <summary>Reads and projects complete original-file facts off UI, then publishes all rows under one cover.</summary>
    public async Task LoadAsync()
    {
        var loading = Loading; var revision = _revision; var token = _lifetime.Token;
        Prepared? prepared = null;
        var display = DisplayIdentity();
        void RequireCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (revision != _revision || !ReferenceEquals(loading, Loading) || display != DisplayIdentity())
                throw new OperationCanceledException("Original account overview page or display retired.", token);
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                var culture = localization.CurrentCulture;
                string? file = null;
                for (var attempt = 0; ; attempt++)
                {
                    RequireCurrent();
                    var snapshot = await store.ReadAccountOverviewAsync(token);
                    file ??= snapshot.DatabasePath;
                    if (!string.Equals(file, snapshot.DatabasePath, StringComparison.Ordinal))
                        throw new InvalidOperationException("Original account overview cannot follow another profile.");
                    try
                    {
                        var result = await Task.Run(() => Prepare(snapshot, today, culture, token), token);
                        await store.ValidateAccountOverviewAsync(snapshot, token);
                        RequireCurrent(); snapshot.EnsureCurrent(); prepared = result; return;
                    }
                    catch (AccountOverviewSupersededException) when (attempt < 2) { RequireCurrent(); }
                }
            }, () =>
            {
                RequireCurrent(); var result = prepared ?? throw new InvalidOperationException("Complete account overview is missing.");
                void Publish(Action change)
                {
                    RequireCurrent(); result.Source.EnsureCurrent(); change();
                    RequireCurrent(); result.Source.EnsureCurrent();
                }
                Publish(() => Archived.ReplaceAll(result.Archived)); Publish(() => Totals.ReplaceAll(result.Totals));
                Publish(() => Groups.ReplaceAll(result.Groups)); Publish(() => ConvertedText = result.Converted);
                Publish(() => HoldingsText = result.Holdings); Publish(() => IsEmpty = result.Empty);
                Publish(() => HasArchived = result.Archived.Length > 0); _displayed = result.Source;
            });
        }
        catch (Exception ex) when (token.IsCancellationRequested && ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"Retired original account overview: {ex}");
        }
    }

    /// <summary>Retires pending reads and old native targets before a retained page departs or changes theme.</summary>
    public void Retire()
    {
        _revision++; _displayed = null;
        var old = _lifetime; _lifetime = new(); Loading = new(); old.Cancel(); old.Dispose();
    }

    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    private Prepared Prepare(AccountOverviewSnapshot snapshot, DateOnly today, System.Globalization.CultureInfo culture, CancellationToken token)
    {
        var input = snapshot.Inputs; var accounts = input.Accounts; var entries = input.Entries;
        var settings = input.Settings ?? new ZananceSettings();
        var archived = new List<AccountItem>(); var totalsRows = new List<CurrencyTotal>(); var groups = new List<AccountListGroup>();
        var grouped = new Dictionary<AccountGroup, List<(AccountItem Item, string Currency, long Balance)>>();

        foreach (var account in accounts)
        {
            token.ThrowIfCancellationRequested();
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
                archived.Add(item);
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
            groups.Add(new AccountListGroup(group, translator[$"AccountGroup_{group}"], string.Join(" · ", totals), [.. rows.Select(r => r.Item)]));
        }

        var inTotals = LedgerCalculator.TotalBalances(accounts, entries, today);
        foreach (var (currency, total) in inTotals)
        {
            totalsRows.Add(new CurrencyTotal(currency, MoneyText.Format(total, currency, culture)));
        }

        // Advanced: one converted line under the native totals, never instead of them (ZEX-MC08).
        string? convertedText = null;
        if (settings.Shows(Feature.ConvertedTotals) && settings.ValuationCurrencyEnabled && inTotals.Count > 1)
        {
            var combined = new Core.Rates.RateTable(input.Rates).Combine(inTotals, settings.ReportCurrencyCode, today, new Core.Rates.RateFreshness(settings.RateFreshnessDays));
            convertedText = combined.IsComplete
                ? translator.Format(combined.IsOutdated ? "Home_CombinedOutdated" : "Home_Combined", MoneyText.Format(combined.Total!.Value, settings.ReportCurrencyCode, culture),
                    combined.OldestRateDate is { } date ? dates.Format(date, Vafadar.Localization.Formatting.DateFormatStyle.Short) : "-")
                : translator.Format("Home_CombinedIncomplete", string.Join(", ", combined.MissingCurrencies));
        }

        var types = input.Types.Where(t => !t.IsArchived).ToList();
        var events = input.Events;
        var held = types.Select(t => (Type: t, Quantity: Core.Holdings.HoldingsLedger.Quantity(events, t.Id, today))).Where(h => h.Quantity > 0)
            .Select(h => $"{h.Type.Name} {holdingText.Quantity(h.Quantity, h.Type)}").ToList();
        var holdingsText = held.Count == 0 ? null : string.Join(" · ", held);
        token.ThrowIfCancellationRequested();
        return new(snapshot, archived.ToArray(), totalsRows.ToArray(), groups.ToArray(), convertedText, holdingsText, accounts.Count == 0);
    }

    /// <summary>Owns every fully prepared native row/value privately until one covered synchronous publication.</summary>
    private sealed record Prepared(AccountOverviewSnapshot Source, AccountItem[] Archived, CurrencyTotal[] Totals,
        AccountListGroup[] Groups, string? Converted, string? Holdings, bool Empty);

    [RelayCommand]
    private Task AddAsync() => NavigateAsync(AppRoutes.AccountEditorRoute, null);

    /// <summary>Opens the dedicated debt/receivable form without creating an account until confirmation.</summary>
    [RelayCommand]
    private Task AddDebtAsync() => NavigateAsync(AppRoutes.AccountEditorRoute, new Dictionary<string, object> { ["debt"] = true });

    [RelayCommand]
    private Task OpenHoldingsAsync() => NavigateAsync(AppRoutes.HoldingsRoute, null);

    /// <summary>Opens only an actual current published row, never a retained same-ID row from a retired snapshot.</summary>
    public Task OpenAsync(AccountItem row) => Groups.SelectMany(group => group.Items).Concat(Archived)
        .Any(current => ReferenceEquals(current, row))
        ? NavigateAsync(AppRoutes.AccountDetailRoute, new Dictionary<string, object> { ["id"] = row.Id }) : Task.CompletedTask;

    private async Task NavigateAsync(string route, IDictionary<string, object>? parameters)
    {
        if (!Loading.IsReady || _displayed is not { } snapshot) return;
        var loading = Loading; var token = _lifetime.Token; var revision = _revision; var display = DisplayIdentity();
        bool Current() => !token.IsCancellationRequested && revision == _revision && ReferenceEquals(loading, Loading)
            && loading.IsReady && ReferenceEquals(snapshot, _displayed) && display == DisplayIdentity();
        try
        {
            await store.ValidateAccountOverviewAccessAsync(snapshot, token);
            if (Current())
            {
                if (route == AppRoutes.AccountEditorRoute)
                    parameters = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>()) { ["accountEditorOrigin"] = snapshot.EditorOrigin };
                await interaction.NavigateAsync(route, parameters);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (!Current()) return;
            try { await loading.RunAsync(() => Task.FromException(ex), () => { }); }
            catch (Exception failure) when (ReferenceEquals(failure, ex)) { }
            await interaction.ShowFailureAsync(ex);
        }
    }
}
