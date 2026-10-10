using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-124: holding capabilities, retained corrections and atomic quantity/payment/fee/derived-price writes.</summary>
[Trait("AT", "AT-124")]
public sealed class HoldingWritePolicyTests : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 10);
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private ServiceProvider Provider(string path, AccessSource source, bool retireAfterSave = false)
    {
        var services = new ServiceCollection().AddSingleton<ICommercialWriteAccessSource>(source)
            .AddSingleton<TimeProvider>(new FixedTime()).AddZananceData(path);
        if (retireAfterSave)
        {
            // The production factory builds its own options. Decorate that actual factory's native connections,
            // rather than registering an unrelated EF factory/options callback which the stores never consume.
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(provider => new RetiringConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!), source));
        }
        var provider = services.BuildServiceProvider(); provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider); return provider;
    }

    private async Task<Fixture> FixtureAsync(bool retireAfterSave = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, retireAfterSave);
        var store = provider.GetRequiredService<ZananceStore>(); var holdings = provider.GetRequiredService<HoldingStore>();
        await store.EnsureDefaultCategoriesAsync(Ct);
        var cash = new Account { Name = "Cash", CurrencyCode = "EUR", Type = AccountType.Cash, OpeningBalance = 100000, OpeningDate = Day.AddMonths(-1) };
        var asset = new Account { Name = "Legacy asset", CurrencyCode = "EUR", Type = AccountType.Asset, OpeningBalance = 50000, OpeningDate = Day.AddMonths(-1) };
        await store.SaveAccountAsync(cash, Ct); await store.SaveAccountAsync(asset, Ct);
        var type = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR", Dimension = AssetDimension.Mass,
            Metal = Metal.Gold, PurityPer10000 = 7500, Note = "Complete original metadata" };
        await holdings.SaveTypeAsync(type, Ct); var location = await holdings.EnsureDefaultLocationAsync("Safe", Ct);
        var opening = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening,
            Date = Day.AddDays(-20), Quantity = 10000, BasisAmount = 5000, Note = "Original history" };
        await holdings.SaveEventAsync(opening, [], Ct);
        var valuation = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = Day.AddDays(-20),
            PricePerUnitMilli = 500000, Note = "Original manual price" };
        await holdings.SaveValuationAsync(valuation, Ct);
        return new(path, provider, source, store, holdings, cash, asset, type, location, opening, valuation);
    }

    private sealed record Fixture(string Path, ServiceProvider Provider, AccessSource Source, ZananceStore Store,
        HoldingStore Holdings, Account Cash, Account Asset, AssetType Type, AssetLocation Location,
        AssetEvent Opening, AssetValuation Valuation)
    {
        public AssetEvent Event(AssetEventKind kind = AssetEventKind.Purchase) => new()
        { AssetTypeId = Type.Id, LocationId = Location.Id, Kind = kind, Date = Day, Quantity = 5000, BasisAmount = kind == AssetEventKind.Purchase ? 2500 : null };
        public LedgerEntry Payment(AssetEventKind kind = AssetEventKind.Purchase) => new()
        { AccountId = Cash.Id, Date = Day, Amount = 2500, Kind = kind == AssetEventKind.Sale ? EntryKind.AssetSale : EntryKind.AssetPurchase,
            Title = "Stored financial identity", Note = "Complete note", Tags = ["retained"] };
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(plan, shared, member, host));
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \""+table+"\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteConnection.ClearAllPools(); _directory.Dispose();
    }

    [Theory]
    [InlineData("type")]
    [InlineData("location")]
    [InlineData("price")]
    [InlineData("opening")]
    [InlineData("purchase")]
    [InlineData("sale")]
    [InlineData("move")]
    [InlineData("gift")]
    [InlineData("outflow")]
    [InlineData("convert")]
    public async Task Free_rejects_new_holding_work_before_any_stored_rows_or_events_change(string operation)
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var events = 0; f.Holdings.Changed += (_, _) => events++;
        var exception = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => NewWorkAsync(f, operation));
        Assert.Equal(CommercialFeature.ManageHoldings, exception.Feature); Assert.Equal(FeaturePermission.RequiresPlus, exception.Permission);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    private static async Task NewWorkAsync(Fixture f, string operation)
    {
        switch (operation)
        {
            case "type": await f.Holdings.SaveTypeAsync(new() { Name = "New type", PriceCurrencyCode = "EUR" }, Ct); break;
            case "location": await f.Holdings.SaveLocationAsync(new() { Name = "New place" }, Ct); break;
            case "price": await f.Holdings.SaveValuationAsync(new() { AssetTypeId = f.Type.Id, CurrencyCode = "EUR", Date = Day, PricePerUnitMilli = 123456 }, Ct); break;
            case "convert": await f.Holdings.ConvertAccountAsync(f.Asset.Id, 50000, Day, "Place", Ct); break;
            default:
                var kind = operation switch { "opening" => AssetEventKind.Opening, "purchase" => AssetEventKind.Purchase,
                    "sale" => AssetEventKind.Sale, "move" => AssetEventKind.LocationTransfer, "gift" => AssetEventKind.GiftReceived,
                    "outflow" => AssetEventKind.Outflow, _ => throw new ArgumentOutOfRangeException(nameof(operation)) };
                var item = f.Event(kind); if (kind == AssetEventKind.LocationTransfer) item.ToLocationId = Guid.NewGuid();
                await f.Holdings.SaveEventAsync(item, kind is AssetEventKind.Purchase or AssetEventKind.Sale ? [f.Payment(kind)] : [], true, Ct);
                Assert.Null(item.GroupId); break;
        }
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Paid_or_exact_shared_scope_saves_purchase_payment_fee_and_derived_price_once(ProductPlan plan, bool shared)
    {
        var f = await FixtureAsync(); f.Enable(plan, shared); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = f.Cash.Id, CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Date = Day, Amount = 100 };
        var events = 0; f.Holdings.Changed += (_, _) => events++;
        Assert.True((await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded);
        var stored = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, stored.Count);
        Assert.All(stored, entry => Assert.Equal(item.GroupId, entry.GroupId)); Assert.Equal(1, events);
        var price = Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Source == ValuationSource.Purchase);
        Assert.Equal((Day, "EUR", 500000L), (price.Date, price.CurrencyCode, price.PricePerUnitMilli));
        Assert.Equal(5000, HoldingsLedger.Positions([item], Day).Single().Quantity);
        Assert.Equal(97400, LedgerCalculator.Balance(f.Cash, stored, Day));
        var totals = Assert.Single(LedgerCalculator.Totals([f.Cash], stored, new(Day, Day)));
        Assert.Equal((0L, 100L), (totals.NetIncome, totals.NetExpense));
    }

    [Fact]
    public async Task Inactive_registration_keeps_new_holdings_and_atomic_purchase_price_available()
    {
        var f = await FixtureAsync(); await NewWorkAsync(f, "type"); await NewWorkAsync(f, "price");
        var item = f.Event(); Assert.True((await f.Holdings.SaveEventAsync(item, [f.Payment()], true, Ct)).Succeeded);
        Assert.Equal(2, (await f.Holdings.GetTypesAsync(Ct)).Count);
        Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Source == ValuationSource.Purchase);
    }

    [Fact]
    public async Task A_purchase_source_label_does_not_authorize_a_new_standalone_price_in_Free()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Holdings.SaveValuationAsync(new()
        { AssetTypeId = f.Type.Id, Date = Day, CurrencyCode = "EUR", PricePerUnitMilli = 123456, Source = ValuationSource.Purchase }, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Atomic_purchase_price_updates_the_latest_existing_same_date_price_and_preserves_older_metadata()
    {
        var f = await FixtureAsync();
        var older = new AssetValuation { AssetTypeId = f.Type.Id, Date = Day, CurrencyCode = "EUR", Source = ValuationSource.Purchase,
            PricePerUnitMilli = 123456, CreatedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero), Note = "Older complete metadata" };
        var latest = new AssetValuation { AssetTypeId = f.Type.Id, Date = Day, CurrencyCode = "EUR", Source = ValuationSource.Purchase,
            PricePerUnitMilli = 234567, CreatedAt = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero), Note = "Latest complete metadata" };
        await f.Holdings.SaveValuationAsync(older, Ct); await f.Holdings.SaveValuationAsync(latest, Ct);
        var before = JsonSerializer.Serialize(older); f.Enable(ProductPlan.Plus);
        var item = f.Event(); item.BasisAmount = 3000;
        Assert.True((await f.Holdings.SaveEventAsync(item, [f.Payment()], true, Ct)).Succeeded);
        var prices = await f.Holdings.GetValuationsAsync(cancellationToken: Ct);
        Assert.Equal(before, JsonSerializer.Serialize(prices.Single(v => v.Id == older.Id)));
        var corrected = prices.Single(v => v.Id == latest.Id);
        Assert.Equal((600000L, latest.Note, latest.CreatedAt), (corrected.PricePerUnitMilli, corrected.Note, corrected.CreatedAt));
        Assert.Equal(2, prices.Count(v => v.Source == ValuationSource.Purchase));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Free_and_expired_host_retain_metadata_price_and_purchase_corrections(bool expiredHost)
    {
        var f = await FixtureAsync(); var purchase = f.Event(); var payment = f.Payment();
        await f.Holdings.SaveEventAsync(purchase, [payment], true, Ct);
        var originalPrice = JsonSerializer.Serialize(f.Valuation); var typeCreated = f.Type.CreatedAt;
        f.Enable(shared: expiredHost, host: !expiredHost);
        f.Type.Name = "Corrected name"; f.Type.Note = "Corrected complete note"; await f.Holdings.SaveTypeAsync(f.Type, Ct);
        f.Location.Name = "Corrected location"; await f.Holdings.SaveLocationAsync(f.Location, Ct);
        f.Valuation.PricePerUnitMilli = 123456; await f.Holdings.SaveValuationAsync(f.Valuation, Ct);
        purchase.Date = Day.AddDays(1); purchase.Quantity = 4000; purchase.BasisAmount = 4000;
        payment.Date = purchase.Date; payment.Amount = 4000;
        Assert.True((await f.Holdings.SaveEventAsync(purchase, [payment], true, Ct)).Succeeded);
        Assert.Equal(typeCreated, Assert.Single(await f.Holdings.GetTypesAsync(Ct)).CreatedAt);
        Assert.Equal(2, (await f.Holdings.GetValuationsAsync(cancellationToken: Ct)).Count(v => v.Source == ValuationSource.Purchase));
        var stored = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal((4000L, "Complete note", "retained"), (stored.Amount, stored.Note, Assert.Single(stored.Tags)));
        Assert.NotEqual(originalPrice, JsonSerializer.Serialize(f.Valuation));
    }

    [Fact]
    public async Task Free_can_record_reasoned_quantity_correction_without_money_or_new_price()
    {
        var f = await FixtureAsync(); f.Enable(); var item = f.Event(AssetEventKind.Correction);
        item.Quantity = 1000; item.IsIncrease = true; item.Reason = "Recounted stored holding";
        Assert.True((await f.Holdings.SaveEventAsync(item, [], true, Ct)).Succeeded);
        Assert.Equal(11000, HoldingsLedger.Quantity(await f.Holdings.GetEventsAsync(cancellationToken: Ct), f.Type.Id, Day));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("no-reason")]
    [InlineData("money")]
    [InlineData("group")]
    [InlineData("basis")]
    [InlineData("proceeds")]
    [InlineData("unknown-type")]
    public async Task New_quantity_correction_cannot_hide_new_paid_work(string variation)
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var item = f.Event(AssetEventKind.Correction); item.IsIncrease = true; item.Reason = "Recount";
        switch (variation)
        {
            case "no-reason": item.Reason = " "; break;
            case "group": item.GroupId = Guid.NewGuid(); break;
            case "basis": item.BasisAmount = 1; break;
            case "proceeds": item.ProceedsAmount = 1; break;
            case "unknown-type": item.AssetTypeId = Guid.NewGuid(); break;
        }
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Holdings.SaveEventAsync(item, variation == "money" ? [f.Payment()] : [], true, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("type")]
    [InlineData("location")]
    [InlineData("price")]
    [InlineData("purchase")]
    [InlineData("convert")]
    [InlineData("default")]
    [InlineData("delete-event")]
    [InlineData("delete-price")]
    [InlineData("undo")]
    [InlineData("import")]
    public async Task Personal_Pro_never_substitutes_for_shared_membership_at_write_boundaries(string operation)
    {
        var f = await FixtureAsync(); f.Enable(ProductPlan.Pro, shared: true, member: false);
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Holdings.Changed += (_, _) => events++;
        var exception = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            switch (operation)
            {
                case "default": await f.Holdings.EnsureDefaultLocationAsync("Place", Ct); break;
                case "delete-event": await f.Holdings.DeleteEventAsync(f.Opening.Id, Ct); break;
                case "delete-price": await f.Holdings.DeleteValuationAsync(f.Valuation.Id, Ct); break;
                case "undo": await f.Holdings.RestoreAsync(new([f.Opening], []), Ct); break;
                case "import": await f.Holdings.ImportAsync(new([], [], [], [], []), Ct); break;
                default: await NewWorkAsync(f, operation); break;
            }
        });
        Assert.Equal(FeaturePermission.RequiresMembership, exception.Permission);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Free_default_location_scaffolding_does_not_enable_new_holdings()
    {
        var f = await FixtureAsync(); f.Location.IsArchived = true; await f.Holdings.SaveLocationAsync(f.Location, Ct);
        f.Enable(); var first = await f.Holdings.EnsureDefaultLocationAsync("New default", Ct);
        var second = await f.Holdings.EnsureDefaultLocationAsync("Not another default", Ct);
        Assert.Equal(first.Id, second.Id); Assert.Equal(2, (await f.Holdings.GetLocationsAsync(Ct)).Count);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => NewWorkAsync(f, "type"));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Free_import_preserves_data_once_without_granting_new_management_rights()
    {
        var f = await FixtureAsync(); f.Enable();
        var type = new AssetType { Name = "Imported", PriceCurrencyCode = "USD", Dimension = AssetDimension.Count };
        var place = new AssetLocation { Name = "Imported place" };
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = Day, Kind = AssetEventKind.Opening, Quantity = 1000 };
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "USD", Date = Day, PricePerUnitMilli = 123000 };
        var content = new HoldingsCsvContent([type], [place], [item], [price], []);
        Assert.Equal((4, 0, null), await f.Holdings.ImportAsync(content, Ct));
        var before = await SnapshotAsync(f.Provider);
        Assert.Equal((0, 4, null), await f.Holdings.ImportAsync(content, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Holdings.SaveValuationAsync(new()
        { AssetTypeId = type.Id, CurrencyCode = "USD", Date = Day.AddDays(1), PricePerUnitMilli = 234000 }, Ct));
    }

    [Fact]
    public async Task Independent_imports_review_actual_ids_inside_the_writer_and_import_the_same_content_once()
    {
        var f = await FixtureAsync(); var second = Provider(f.Path, f.Source).GetRequiredService<HoldingStore>(); f.Enable();
        var type = new AssetType { Name = "Imported once", PriceCurrencyCode = "EUR" };
        var location = new AssetLocation { Name = "Imported once place" };
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening, Date = Day, Quantity = 1000 };
        var price = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = Day, PricePerUnitMilli = 100000 };
        var content = new HoldingsCsvContent([type], [location], [item], [price], []);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var results = await Task.WhenAll(Task.Run(() => f.Holdings.ImportAsync(content, Ct), Ct), Task.Run(() => second.ImportAsync(content, Ct), Ct));
        Assert.Single(results, r => r.Imported == 4 && r.Skipped == 0 && r.Conflict is null);
        Assert.Single(results, r => r.Imported == 0 && r.Skipped == 4 && r.Conflict is null);
        Assert.Single(await f.Holdings.GetTypesAsync(Ct), t => t.Id == type.Id);
        Assert.Single(await f.Holdings.GetEventsAsync(cancellationToken: Ct), e => e.Id == item.Id);
        Assert.Single(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Id == price.Id);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Free_or_expired_host_can_delete_and_undo_the_complete_original_group(bool expiredHost)
    {
        var f = await FixtureAsync(); var item = f.Event(); await f.Holdings.SaveEventAsync(item, [f.Payment()], true, Ct);
        var before = await SnapshotAsync(f.Provider); f.Enable(shared: expiredHost, host: !expiredHost);
        var removed = await f.Holdings.DeleteEventAsync(item.Id, Ct); Assert.NotNull(removed.Removed); Assert.Null(removed.Conflict);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await f.Holdings.RestoreAsync(removed.Removed!, Ct); Assert.Equal(before, await SnapshotAsync(f.Provider));
        await f.Holdings.DeleteValuationAsync(f.Valuation.Id, Ct);
        Assert.DoesNotContain(await f.Holdings.GetValuationsAsync(cancellationToken: Ct), v => v.Id == f.Valuation.Id);
    }

    [Fact]
    public async Task Derived_price_failure_rolls_back_purchase_payment_fee_and_changed_then_retry_succeeds()
    {
        var f = await FixtureAsync(); f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider);
        await SqlAsync(f, "CREATE TRIGGER RejectDerivedPrice BEFORE INSERT ON AssetValuations BEGIN SELECT RAISE(ABORT, 'Owned price failure'); END;");
        var item = f.Event(); var payment = f.Payment(); var fee = new LedgerEntry
        { Kind = EntryKind.Expense, AccountId = f.Cash.Id, CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Date = Day, Amount = 100 };
        var events = 0; f.Holdings.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER RejectDerivedPrice;");
        Assert.True((await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct)).Succeeded); Assert.Equal(1, events);
    }

    [Fact]
    public async Task Failed_existing_purchase_price_correction_preserves_every_original_row_and_fee()
    {
        var f = await FixtureAsync(); var item = f.Event(); var payment = f.Payment();
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = f.Cash.Id, CategoryId = await f.Holdings.FeesCategoryAsync(Ct), Date = Day, Amount = 100 };
        await f.Holdings.SaveEventAsync(item, [payment, fee], true, Ct); f.Enable();
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Holdings.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectPriceUpdate BEFORE UPDATE ON AssetValuations BEGIN SELECT RAISE(ABORT, 'Owned update failure'); END;");
        item.BasisAmount = 3000; payment.Amount = 3000;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Holdings.SaveEventAsync(item, [payment], true, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Access_retired_after_sql_save_rolls_back_complete_group_and_derived_price()
    {
        var f = await FixtureAsync(retireAfterSave: true); f.Enable(ProductPlan.Plus);
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Holdings.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RetireAccessAfterPrice AFTER INSERT ON AssetValuations BEGIN SELECT RetireHoldingAccess(); END;");
        f.Source.RetireAfterSave = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Holdings.SaveEventAsync(f.Event(), [f.Payment()], true, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events); Assert.Equal(1, f.Source.RetiredWrites);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_sales_validate_quantity_inside_the_same_sqlite_writer(bool enabled)
    {
        var f = await FixtureAsync(); var second = Provider(f.Path, f.Source).GetRequiredService<HoldingStore>();
        if (enabled) f.Enable(ProductPlan.Plus);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var events = 0; f.Holdings.Changed += (_, _) => Interlocked.Increment(ref events); second.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<HoldingSaveResult> Sell(HoldingStore holdings)
        { var item = f.Event(AssetEventKind.Sale); item.Quantity = 7000; return await holdings.SaveEventAsync(item, [f.Payment(AssetEventKind.Sale)], true, Ct); }
        var results = await Task.WhenAll(Task.Run(() => Sell(f.Holdings), Ct), Task.Run(() => Sell(second), Ct));
        // FindConflict reports the available quantity before the whole date, not after the first same-day sale.
        Assert.Single(results, r => r.Succeeded); Assert.Equal(10000, Assert.Single(results, r => !r.Succeeded).Conflict!.Available);
        Assert.Equal(1, events); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(3000, HoldingsLedger.Quantity(await f.Holdings.GetEventsAsync(cancellationToken: Ct), f.Type.Id, Day));
    }

    [Fact]
    public async Task Failed_conversion_keeps_legacy_account_and_all_prior_data()
    {
        var f = await FixtureAsync(); f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider);
        await SqlAsync(f, "CREATE TRIGGER RejectConversionPrice BEFORE INSERT ON AssetValuations BEGIN SELECT RAISE(ABORT, 'Owned conversion failure'); END;");
        var events = 0; f.Holdings.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Holdings.ConvertAccountAsync(f.Asset.Id, 50000, Day, "New place", Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER RejectConversionPrice;");
        Assert.NotNull(await f.Holdings.ConvertAccountAsync(f.Asset.Id, 50000, Day, "New place", Ct));
        Assert.True((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single(a => a.Id == f.Asset.Id).IsArchived);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Cached exact-scope facts plus a rendezvous before actual writer acquisition, never fake holdings.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous;
        public int Captures;
        public bool RetireAfterSave;
        public int RetiredWrites;
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(System.IO.Path.GetFullPath(path), databasePath);
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>Decorates actual SQLite connections for a trigger-driven cached-access change after a price INSERT.</summary>
    private sealed class RetiringConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireHoldingAccess", () =>
            {
                if (source.RetireAfterSave)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free));
                    source.RetireAfterSave = false; source.RetiredWrites++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable local time keeps full audit metadata comparable across delete and Undo.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
