using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Data side of enhancement ZEX phase 3: purchases and sales as one group, history checks, delete and Undo.</summary>
public sealed class ZexPhase3DataTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 3);
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly HoldingStore _holdings;

    public ZexPhase3DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
        _holdings = _services.GetRequiredService<HoldingStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    public async Task G11_a_purchase_saves_quantity_payment_and_fee_together_and_undo_restores_all()
    {
        var (cash, gold, safe) = await SetUpAsync();
        var purchase = new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Purchase, Quantity = 10_000, BasisAmount = 1_000_00, Date = Day };
        var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Amount = 1_000_00, Date = Day };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Amount = 20_00, Date = Day, CategoryId = await _holdings.FeesCategoryAsync(Ct) };

        Assert.True((await _holdings.SaveEventAsync(purchase, [payment, fee], Ct)).Succeeded);

        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(980_00, LedgerCalculator.Balance(cash, entries, Day));
        Assert.All(entries, e => Assert.Equal(purchase.GroupId, e.GroupId));
        Assert.Equal(10_000, HoldingsLedger.Quantity(await _holdings.GetEventsAsync(cancellationToken: Ct), gold.Id, Day));

        var (removed, conflict) = await _holdings.DeleteEventAsync(purchase.Id, Ct);
        Assert.Null(conflict);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await _holdings.GetEventsAsync(cancellationToken: Ct));

        await _holdings.RestoreAsync(removed!, Ct);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
        Assert.Single(await _holdings.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_opening_holding_takes_no_money_and_a_sale_beyond_it_is_refused()
    {
        var (cash, gold, safe) = await SetUpAsync();
        Assert.True((await _holdings.SaveEventAsync(new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Opening, Quantity = 20_000, Date = Day.AddDays(-30) }, [], Ct)).Succeeded);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));

        var sale = new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Sale, Quantity = 30_000, ProceedsAmount = 3_000_00, Date = Day };
        var proceeds = new LedgerEntry { Kind = EntryKind.AssetSale, AccountId = cash.Id, Amount = 3_000_00, Date = Day };
        var result = await _holdings.SaveEventAsync(sale, [proceeds], Ct);

        Assert.False(result.Succeeded);
        Assert.Equal(20_000, result.Conflict!.Available);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_purchase_a_later_sale_depends_on_cannot_be_deleted()
    {
        var (cash, gold, safe) = await SetUpAsync();
        var purchase = new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Purchase, Quantity = 10_000, BasisAmount = 1_000_00, Date = Day.AddDays(-5) };
        await _holdings.SaveEventAsync(purchase, [new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Amount = 1_000_00, Date = purchase.Date }], Ct);
        await _holdings.SaveEventAsync(new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Outflow, Quantity = 4_000, Date = Day }, [], Ct);

        var (removed, conflict) = await _holdings.DeleteEventAsync(purchase.Id, Ct);

        Assert.Null(removed);
        Assert.NotNull(conflict);
        Assert.Equal(2, (await _holdings.GetEventsAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task The_dimension_of_a_type_with_events_cannot_change()
    {
        var (_, gold, safe) = await SetUpAsync();
        await _holdings.SaveEventAsync(new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Opening, Quantity = 1_000, Date = Day }, [], Ct);

        gold.Dimension = AssetDimension.Count;

        Assert.False(await _holdings.SaveTypeAsync(gold, Ct));
    }

    private async Task<(Account Cash, AssetType Gold, AssetLocation Safe)> SetUpAsync()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var cash = new Account { Name = "Cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningBalance = 2_000_00, OpeningDate = new DateOnly(2026, 1, 1) };
        await _store.SaveAccountAsync(cash, Ct);
        var gold = new AssetType { Name = "18k gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500 };
        await _holdings.SaveTypeAsync(gold, Ct);
        var safe = await _holdings.EnsureDefaultLocationAsync("Home safe", Ct);
        return (cash, gold, safe);
    }
}
