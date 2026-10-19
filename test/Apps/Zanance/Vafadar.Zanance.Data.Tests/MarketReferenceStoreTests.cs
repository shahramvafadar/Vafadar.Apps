using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Rates;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual migrated financial writer, retained source history and portable restore; no live provider or native effects.</summary>
[Trait("AT", "AT-299")]
public sealed class MarketReferenceStoreTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private ServiceProvider Install(string name)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(clock)
            .AddZananceData(directory.Combine(Path.Combine(name, "market.db")))
            .AddVafadarBackup()
            .AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0), name, "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>().BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>(); return services;
    }
    private MarketQuote Quote(MarketUnit unit = MarketUnit.Currency, decimal value = 1.23456789m)
    {
        var symbol = new MarketSymbol(unit == MarketUnit.Currency ? "EURUSD" : "GOLD", "Fictitious qualified reference",
            unit == MarketUnit.Currency ? "EUR" : "XAU", unit == MarketUnit.Currency ? "USD" : "EUR", unit,
            unit is MarketUnit.FineGram or MarketUnit.FineTroyOunce ? Metal.Gold : null);
        var catalog = new MarketCatalog(1, "fictitious-writer", "fixture-v1", "Fictitious source",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
        var now = clock.GetUtcNow();
        return new(1, new(catalog.Provider, catalog.Revision, symbol.Id, new(2026, 10, 17)), catalog, symbol,
            new("fixture-observation", new(2026, 10, 16), now.AddHours(-1), value), now, now.AddMinutes(1), now.AddHours(1));
    }
    private static AssetType Gold() => new() { Name = "Original 18k gold", Kind = AssetKind.PreciousMetal,
        Dimension = AssetDimension.Mass, Metal = Metal.Gold, PurityPer10000 = 7500, PriceCurrencyCode = "EUR" };
    private static async Task<AssetType> SeedGold(ServiceProvider services)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var type = Gold(); db.AssetTypes.Add(type); await db.SaveChangesAsync(Ct); return type;
    }
    public void Dispose() { SqliteTestPools.Clear(directory); directory.Dispose(); }

    [Fact]
    public async Task Calculated_Ecb_decimal_precision_is_not_shortened_by_sqlite_or_portable_restore()
    {
        clock.SetUtcNow(new(2026, 10, 19, 13, 0, 0, TimeSpan.Zero)); var now = clock.GetUtcNow();
        var symbol = EcbReference.Catalog.Symbols.Single(s => s.Id == "USD-EUR"); var digest = new string('b', 64);
        var expected = 1m / 1.1253m;
        var quote = new MarketQuote(2, new("ecb", EcbReference.Revision, symbol.Id, new(2026, 10, 19)),
            EcbReference.Catalog with { Symbols = [symbol] }, symbol,
            new(EcbReference.QuoteId(new(2026, 10, 16), symbol, digest), new(2026, 10, 16), null, expected)
            { Daily = new(1, digest, now, null, 1.1253m, 1m, EcbReference.Division) }, now, now.AddMinutes(5), now.AddDays(7));
        await using var source = Install("ecb-full-decimal"); var store = source.GetRequiredService<MarketReferenceStore>();
        var receipt = await store.ApplyAsync(await store.ReviewRateAsync(quote, Ct), false, () => { }, Ct);
        Assert.Equal(expected, Assert.Single(await source.GetRequiredService<ZananceStore>().GetRatesAsync(Ct)).Rate);
        var package = await source.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
        await using var restored = Install("ecb-full-decimal-restored"); await restored.GetRequiredService<IBackupService>().RestorePackageAsync(package, null, Ct);
        Assert.Equal(expected, Assert.Single(await restored.GetRequiredService<ZananceStore>().GetRatesAsync(Ct)).Rate);
        var history = Assert.Single(await restored.GetRequiredService<MarketReferenceStore>().HistoryAsync(ct: Ct));
        Assert.Equal(receipt.QuoteJson, history.Document.QuoteJson);
        Assert.Equal(expected, JsonSerializer.Deserialize(history.Document.QuoteJson, MarketJson.Default.MarketQuote)!.Observation.Value);
    }

    [Fact]
    public async Task Daily_Ecb_provenance_survives_original_writer_companion_packet_and_cold_history_without_rewriting_finance()
    {
        clock.SetUtcNow(new(2026, 10, 19, 13, 0, 0, TimeSpan.Zero));
        var now = clock.GetUtcNow(); var symbol = EcbReference.Catalog.Symbols.Single(s => s.Id == "USD-CHF");
        var digest = new string('a', 64);
        var quote = new MarketQuote(2, new("ecb", EcbReference.Revision, symbol.Id, new(2026, 10, 19)),
            EcbReference.Catalog with { Symbols = [symbol] }, symbol,
            new(EcbReference.QuoteId(new(2026, 10, 16), symbol, digest), new(2026, 10, 16), null, 0.72m)
            { Daily = new(1, digest, now, now.AddMinutes(-5), 1.25m, 0.9m, EcbReference.Division) }, now, now.AddMinutes(5), now.AddDays(7));
        string original;
        await using (var services = Install("ecb-cold"))
        {
            var store = services.GetRequiredService<MarketReferenceStore>();
            var review = await store.ReviewRateAsync(quote, Ct);
            Assert.Empty(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct));
            var receipt = await store.ApplyAsync(review, false, () => { }, Ct); original = receipt.QuoteJson;
            var document = MarketReferenceDocument.From(receipt);
            var wire = JsonSerializer.Serialize(new MarketSourceUpload(1, Guid.NewGuid(), [document]), MarketSourceJson.Default.MarketSourceUpload);
            var transferred = JsonSerializer.Deserialize(wire, MarketSourceJson.Default.MarketSourceUpload)!;
            Assert.Equal(document, Assert.Single(transferred.Documents)); MarketReferenceDocuments.Validate(transferred.Documents[0], now);
            var row = Assert.Single(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct));
            Assert.Equal(0.72m, row.Rate); Assert.True(row.IsEstimate); Assert.Equal(quote.Request.OnOrBefore, row.Date);
            var package = await services.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
            await using var restored = Install("ecb-portable");
            await restored.GetRequiredService<IBackupService>().RestorePackageAsync(package, null, Ct);
            var portable = Assert.Single(await restored.GetRequiredService<MarketReferenceStore>().HistoryAsync(ct: Ct));
            Assert.Equal(document, portable.Document); Assert.True(portable.MatchesCurrent);
        }
        clock.Advance(TimeSpan.FromDays(8));
        await using var cold = Install("ecb-cold");
        var history = Assert.Single(await cold.GetRequiredService<MarketReferenceStore>().HistoryAsync(ct: Ct));
        Assert.Equal(original, history.Document.QuoteJson); MarketReferenceDocuments.Validate(history.Document, clock.GetUtcNow());
        var decoded = JsonSerializer.Deserialize(original, MarketJson.Default.MarketQuote)!;
        Assert.Equal(quote.Observation, decoded.Observation);
        Assert.Empty(await cold.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Exact_rate_and_source_commit_once_and_original_completion_survives_cache_expiry()
    {
        await using var services = Install("rate"); var store = services.GetRequiredService<MarketReferenceStore>(); var quote = Quote();
        var review = await store.ReviewRateAsync(quote, Ct);
        Assert.False(review.ReplacesExisting);
        var saved = await store.ApplyAsync(review, false, () => { }, Ct);
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(saved.Id, (await store.ApplyAsync(review, false, () => { }, Ct)).Id);
        var history = Assert.Single(await store.CurrentAsync(MarketReferenceTarget.ExchangeRate, saved.TargetId, Ct));
        Assert.Equal(saved.QuoteJson, history.QuoteJson);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.False(db.Database.HasPendingModelChanges());
        var row = Assert.Single(await db.ExchangeRates.ToListAsync(Ct));
        Assert.Equal(quote.Observation.Value, row.Rate); Assert.True(row.IsEstimate); Assert.Equal(quote.Request.OnOrBefore, row.Date);
        Assert.Single(await db.MarketReferenceReceipts.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }
    [Fact]
    public async Task Saved_source_pages_keep_deleted_targets_and_original_ids_without_recreating_finance()
    {
        await using var services = Install("history-pages"); var store = services.GetRequiredService<MarketReferenceStore>();
        var originals = new List<Guid>();
        for (var i = 0; i < 21; i++)
        {
            var quote = Quote(value: 1m + i / 100m);
            originals.Add((await store.ApplyAsync(await store.ReviewRateAsync(quote, Ct), false, () => { }, Ct)).Id);
        }
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.ExchangeRates.RemoveRange(await db.ExchangeRates.ToArrayAsync(Ct)); await db.SaveChangesAsync(Ct); }
        clock.Advance(TimeSpan.FromDays(2));
        var first = await store.HistoryAsync(ct: Ct); var second = await store.HistoryAsync(20, ct: Ct);
        Assert.Equal(20, first.Count); Assert.Single(second); Assert.All(first.Concat(second), item => Assert.False(item.MatchesCurrent));
        Assert.Equal(originals.Order(), first.Concat(second).Select(row => row.Document.Id).Order());
        Assert.Empty(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.HistoryAsync(count: 21, ct: Ct));
    }
    [Fact]
    public async Task Old_review_cannot_overwrite_a_new_same_date_manual_rate()
    {
        await using var services = Install("concurrent"); var store = services.GetRequiredService<MarketReferenceStore>(); var quote = Quote();
        var review = await store.ReviewRateAsync(quote, Ct);
        await services.GetRequiredService<ZananceStore>().SaveRateAsync(new() { Date = review.Mapping.Date,
            FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Rate = 9.876m }, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(review, false, () => { }, Ct));
        Assert.Equal(9.876m, Assert.Single(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct)).Rate);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.MarketReferenceReceipts.ToListAsync(Ct));
    }
    [Fact]
    public async Task Newly_stale_reference_requires_explicit_acknowledgment_and_native_cover_prevents_writes()
    {
        await using var services = Install("stale"); var store = services.GetRequiredService<MarketReferenceStore>();
        var review = await store.ReviewRateAsync(Quote(), Ct); clock.Advance(TimeSpan.FromMinutes(2));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(review, false, () => { }, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(review, true, () => throw new InvalidOperationException("Fixture covered"), Ct));
        Assert.Empty(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct));
        var saved = await store.ApplyAsync(review, true, () => { }, Ct);
        Assert.Equal(review.Id, saved.Id);
    }
    [Fact]
    public async Task Expired_uncommitted_reference_cannot_create_financial_history()
    {
        await using var services = Install("expired"); var store = services.GetRequiredService<MarketReferenceStore>();
        var review = await store.ReviewRateAsync(Quote(), Ct); clock.Advance(TimeSpan.FromHours(2));
        await Assert.ThrowsAsync<ArgumentException>(() => store.ApplyAsync(review, true, () => { }, Ct));
        Assert.Empty(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct));
    }
    [Fact]
    public async Task Manual_change_removes_current_attribution_without_destroying_original_source_history()
    {
        await using var services = Install("manual"); var store = services.GetRequiredService<MarketReferenceStore>();
        var saved = await store.ApplyAsync(await store.ReviewRateAsync(Quote(), Ct), false, () => { }, Ct);
        var rate = Assert.Single(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct)); rate.Rate = 2;
        await services.GetRequiredService<ZananceStore>().SaveRateAsync(rate, Ct);
        Assert.Empty(await store.CurrentAsync(MarketReferenceTarget.ExchangeRate, saved.TargetId, Ct));
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(saved.QuoteJson, Assert.Single(await db.MarketReferenceReceipts.ToListAsync(Ct)).QuoteJson);
    }
    [Fact]
    public async Task Fine_metal_price_keeps_quantity_cost_and_original_note_separate()
    {
        await using var services = Install("gold"); var type = await SeedGold(services); var store = services.GetRequiredService<MarketReferenceStore>();
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            db.AssetValuations.Add(new() { AssetTypeId = type.Id, Date = new(2026, 10, 17), CurrencyCode = "EUR", PricePerUnitMilli = 123,
                Note = "Original manual note", Source = ValuationSource.Purchase }); await db.SaveChangesAsync(Ct);
        }
        var review = await store.ReviewPriceAsync(Quote(MarketUnit.FineTroyOunce, 3110.34768m), type.Id, false, Ct);
        Assert.True(review.ReplacesExisting); Assert.Equal(7_500_000, review.Mapping.PricePerUnitMilli);
        var saved = await store.ApplyAsync(review, false, () => { }, Ct);
        Assert.Single(await store.CurrentAsync(MarketReferenceTarget.AssetPrice, saved.TargetId, Ct));
        await using var after = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var price = Assert.Single(await after.AssetValuations.ToListAsync(Ct));
        Assert.Equal(7_500_000, price.PricePerUnitMilli); Assert.Equal("Original manual note", price.Note); Assert.Equal(ValuationSource.Manual, price.Source);
        Assert.Empty(await after.AssetEvents.ToListAsync(Ct)); Assert.Empty(await after.Entries.ToListAsync(Ct));
    }
    [Fact]
    public async Task Changed_asset_definition_refuses_original_review_and_no_replacement_price_is_written()
    {
        await using var services = Install("definition"); var type = await SeedGold(services); var store = services.GetRequiredService<MarketReferenceStore>();
        var review = await store.ReviewPriceAsync(Quote(MarketUnit.FineGram, 100), type.Id, false, Ct);
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { var changed = await db.AssetTypes.SingleAsync(Ct); changed.PurityPer10000 = 9999; await db.SaveChangesAsync(Ct); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(review, false, () => { }, Ct));
        await using var after = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await after.AssetValuations.ToListAsync(Ct)); Assert.Empty(await after.MarketReferenceReceipts.ToListAsync(Ct));
    }
    [Theory]
    [InlineData(MarketUnit.FineGram, false, AssetDimension.Count, Metal.Gold, 7500)]
    [InlineData(MarketUnit.FineGram, false, AssetDimension.Mass, Metal.Silver, 7500)]
    [InlineData(MarketUnit.FineGram, false, AssetDimension.Mass, Metal.Gold, null)]
    [InlineData(MarketUnit.Gram, false, AssetDimension.Mass, Metal.Gold, 7500)]
    [InlineData(MarketUnit.Piece, false, AssetDimension.Count, Metal.Gold, 7500)]
    [InlineData(MarketUnit.Piece, true, AssetDimension.Mass, Metal.Gold, 7500)]
    public void Unsupported_metal_or_item_mapping_never_guesses_a_retail_price(MarketUnit unit, bool exact, AssetDimension dimension, Metal metal, int? purity)
    {
        var type = Gold(); type.Dimension = dimension; type.Metal = metal; type.PurityPer10000 = purity;
        Assert.Throws<ArgumentException>(() => MarketApplication.PricePerUnitMilli(Quote(unit, 100), type, exact, clock.GetUtcNow()));
    }
    [Fact]
    public async Task Actual_SQLite_refuses_immutable_or_oversized_source_mutation()
    {
        await using var services = Install("bounds"); var store = services.GetRequiredService<MarketReferenceStore>();
        var saved = await store.ApplyAsync(await store.ReviewRateAsync(Quote(), Ct), false, () => { }, Ct);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlAsync($"UPDATE MarketReferenceReceipts SET QuoteJson='{{}}' WHERE Id={saved.Id}", Ct));
        var oversized = new string('x', 32769); var id = Guid.CreateVersion7();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlAsync($"INSERT INTO MarketReferenceReceipts SELECT {id},Target,TargetId,TargetFingerprint,QuoteDigest,{oversized},MappingJson,AcceptedAt,CreatedAt,UpdatedAt FROM MarketReferenceReceipts", Ct));
        Assert.Equal(saved.QuoteJson, Assert.Single(await db.MarketReferenceReceipts.AsNoTracking().ToListAsync(Ct)).QuoteJson);
    }
    [Fact]
    public async Task Portable_restore_preserves_original_source_and_exact_financial_binding_after_cache_expiry()
    {
        await using var original = Install("portable-original"); var store = original.GetRequiredService<MarketReferenceStore>();
        var saved = await store.ApplyAsync(await store.ReviewRateAsync(Quote(), Ct), false, () => { }, Ct);
        await using (var db = await original.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var digest = MarketReferenceDocuments.Digest(MarketReferenceDocument.From(saved), clock.GetUtcNow());
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO SyncMarketSourcesSent (DocumentId,Digest,Acknowledged,Epoch,MembershipVersion)
                VALUES ({saved.Id},{digest},1,0,0)
                """, Ct);
        }
        var package = await original.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
        await using var fresh = Install("portable-fresh"); await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, null, Ct);
        clock.Advance(TimeSpan.FromDays(10));
        var restored = Assert.Single(await fresh.GetRequiredService<MarketReferenceStore>().CurrentAsync(MarketReferenceTarget.ExchangeRate, saved.TargetId, Ct));
        Assert.Equal(saved.Id, restored.Id); Assert.Equal(saved.QuoteJson, restored.QuoteJson); Assert.Equal(saved.MappingJson, restored.MappingJson);
        Assert.Equal(saved.TargetFingerprint, restored.TargetFingerprint);
        await using var target = await fresh.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(0, await target.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM SyncMarketSourcesSent").SingleAsync(Ct));
        await using var source = await original.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(1, await source.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM SyncMarketSourcesSent").SingleAsync(Ct));
    }

    [Fact]
    public async Task Cover_between_target_mutation_and_commit_rolls_back_both_value_and_source()
    {
        await using var services = Install("cover-before-commit"); var store = services.GetRequiredService<MarketReferenceStore>();
        var review = await store.ReviewRateAsync(Quote(), Ct); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(review, false,
            () => { if (++checks == 3) throw new InvalidOperationException("Fixture retired native scope"); }, Ct));
        Assert.Equal(3, checks);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.ExchangeRates.ToListAsync(Ct)); Assert.Empty(await db.MarketReferenceReceipts.ToListAsync(Ct));
    }
    [Fact]
    public async Task Post_commit_observer_failure_retains_original_result_and_retry_never_duplicates_value_or_receipt()
    {
        await using var services = Install("observer"); var store = services.GetRequiredService<MarketReferenceStore>();
        var review = await store.ReviewRateAsync(Quote(), Ct);
        EventHandler reject = (_, _) => throw new InvalidOperationException("Fixture observer refused"); store.Changed += reject;
        var error = await Assert.ThrowsAsync<CommittedWriteNotificationException<MarketReferenceReceipt>>(() => store.ApplyAsync(review, false, () => { }, Ct));
        Assert.Equal(review.Id, error.Result.Id); store.Changed -= reject;
        Assert.Equal(error.Result.Id, (await store.ApplyAsync(review, false, () => { }, Ct)).Id);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Single(await db.ExchangeRates.ToListAsync(Ct)); Assert.Single(await db.MarketReferenceReceipts.ToListAsync(Ct));
    }
    [Fact]
    public async Task Matching_digest_alone_cannot_attribute_a_forged_effective_mapping_to_a_current_manual_value()
    {
        await using var services = Install("forged-mapping"); var store = services.GetRequiredService<MarketReferenceStore>(); var quote = Quote();
        var rate = new ExchangeRate { Date = quote.Request.OnOrBefore, FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Rate = 9m, IsEstimate = true };
        await services.GetRequiredService<ZananceStore>().SaveRateAsync(rate, Ct);
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            db.MarketReferenceReceipts.Add(new() { Target = MarketReferenceTarget.ExchangeRate, TargetId = rate.Id,
                TargetFingerprint = MarketApplication.Fingerprint(rate), QuoteDigest = MarketReference.Digest(quote, clock.GetUtcNow()),
                QuoteJson = JsonSerializer.Serialize(quote, MarketJson.Default.MarketQuote), AcceptedAt = clock.GetUtcNow(),
                MappingJson = JsonSerializer.Serialize(new MarketAppliedMapping(1, MarketReferenceTarget.ExchangeRate, rate.Date,
                    "USD", "EUR", 9m, null, null, false), MarketMappingJson.Default.MarketAppliedMapping) });
            await db.SaveChangesAsync(Ct);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CurrentAsync(MarketReferenceTarget.ExchangeRate, rate.Id, Ct));
        Assert.Equal(9m, Assert.Single(await services.GetRequiredService<ZananceStore>().GetRatesAsync(Ct)).Rate);
    }
}
