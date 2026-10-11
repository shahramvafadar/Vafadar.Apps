using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>ZEX-S0902, AT34: a backup restores everything of enhancement ZEX on a fresh install, and its preview counts it.</summary>
public sealed class ZexBackupRoundTripTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 1);
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task AT34_holdings_goals_snapshots_and_profile_settings_survive_a_restore_on_a_fresh_install()
    {
        await using var original = Install("a");
        var store = original.GetRequiredService<ZananceStore>();
        var holdings = original.GetRequiredService<HoldingStore>();
        var goals = original.GetRequiredService<GoalStore>();
        var cash = new Account { Name = "Cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningBalance = 2_000_00, OpeningDate = new DateOnly(2026, 1, 1), LastReconciledOn = Day };
        await store.SaveAccountAsync(cash, Ct);
        await store.EnsureDefaultCategoriesAsync(Ct);

        var gold = new AssetType { Name = "18k gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500 };
        await holdings.SaveTypeAsync(gold, Ct);
        var safe = await holdings.EnsureDefaultLocationAsync("Home safe", Ct);
        var purchase = new AssetEvent { AssetTypeId = gold.Id, LocationId = safe.Id, Kind = AssetEventKind.Purchase, Quantity = 10_000, BasisAmount = 1_000_00, Date = Day };
        await holdings.SaveEventAsync(purchase, [new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Amount = 1_000_00, Date = Day }], Ct);
        await holdings.SaveValuationAsync(new AssetValuation { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Day, PricePerUnitMilli = 10_000_000 }, Ct);

        var quantity = new Goal { Name = "50 g", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = gold.Id, TargetAmount = 50_000, HomePin = 1 };
        await goals.SaveGoalAsync(quantity, Ct);
        await goals.SaveContributionPlanAsync(quantity.Id, new ContributionPlan { Method = ContributionMethod.FixedAmount, Amount = 2_000, AssumedPricePerUnitMilli = 6_000_000 }, Ct);
        await store.SaveForecastSnapshotAsync(new ForecastSnapshot
        {
            Name = "Before", BaseDate = Day, Horizon = Day.AddDays(1), CurrencyCode = "EUR", AccountIds = cash.Id.ToString(), Path = "100000,90000", Minimum = 90_000, MinimumDate = Day.AddDays(1),
        }, Ct);
        var settings = await store.GetSettingsAsync(Ct);
        settings.DisplayUnits = "IRR:Toman:1";
        settings.HomeLayout = "Goals,Budget";
        settings.EssentialEstimate = 20_00;
        settings.ReviewReminderEnabled = true;
        await store.SaveSettingsAsync(settings, Ct);

        var package = await original.GetRequiredService<IBackupService>().CreatePackageAsync("correct horse", Ct);
        await using var fresh = Install("b");
        var inspected = await fresh.GetRequiredService<IBackupService>().InspectPackageAsync(package, "correct horse", Ct);
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, "correct horse", Ct);

        // The preview lists what the backup holds (S0409, S0803).
        Assert.Equal(("1", "1", "1"), (inspected.Summary![ZananceBackupSummary.Goals], inspected.Summary[ZananceBackupSummary.Holdings], inspected.Summary[ZananceBackupSummary.Snapshots]));

        var restoredStore = fresh.GetRequiredService<ZananceStore>();
        var restoredHoldings = fresh.GetRequiredService<HoldingStore>();
        var restoredGoals = fresh.GetRequiredService<GoalStore>();
        var accounts = await restoredStore.GetAccountsAsync(cancellationToken: Ct);
        var entries = await restoredStore.GetEntriesAsync(cancellationToken: Ct);
        var events = await restoredHoldings.GetEventsAsync(cancellationToken: Ct);
        Assert.Equal(1_000_00, LedgerCalculator.Balance(accounts.Single(), entries, Day));
        Assert.Equal(Day, accounts.Single().LastReconciledOn);
        Assert.Equal(10_000, HoldingsLedger.Quantity(events, gold.Id, Day));
        Assert.Equal(purchase.GroupId, entries.Single().GroupId);
        Assert.Equal(10_000_000, Assert.Single(await restoredHoldings.GetValuationsAsync(cancellationToken: Ct)).PricePerUnitMilli);

        var goal = Assert.Single(await restoredGoals.GetGoalsAsync(Ct));
        Assert.Equal((GoalType.HoldingQuantity, gold.Id, 1), (goal.Type, goal.AssetTypeId!.Value, goal.HomePin!.Value));
        Assert.Equal(6_000_000, Assert.Single(await restoredGoals.GetContributionPlansAsync(Ct)).AssumedPricePerUnitMilli);
        Assert.Equal([100_000, 90_000], Assert.Single(await restoredStore.GetForecastSnapshotsAsync(Ct)).Points().Select(p => p.Balance));

        var restoredSettings = await restoredStore.GetSettingsAsync(Ct);
        Assert.True(restoredSettings.ReviewReminderEnabled);
        Assert.Equal(("IRR:Toman:1", "Goals,Budget", 20_00L), (restoredSettings.DisplayUnits, restoredSettings.HomeLayout, restoredSettings.EssentialEstimate!.Value));
    }

    private ServiceProvider Install(string name)
    {
        Directory.CreateDirectory(_directory.Combine(name));
        var services = new ServiceCollection()
            .AddZananceData(_directory.Combine(Path.Combine(name, "zanance.db")))
            .AddVafadarBackup()
            .AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0, 0), name, "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>()
            .BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        return services;
    }
}
