using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Data side of enhancement ZEX phase 5: quantity goals and read-only forecast snapshots.</summary>
public sealed class ZexPhase5DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly GoalStore _goals;

    public ZexPhase5DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
        _goals = _services.GetRequiredService<GoalStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    public async Task S0803_a_saved_snapshot_cannot_be_changed_and_keeps_its_path()
    {
        var snapshot = new ForecastSnapshot
        {
            Name = "Before the move", BaseDate = new DateOnly(2026, 10, 1), Horizon = new DateOnly(2026, 10, 3), CurrencyCode = "EUR",
            AccountIds = Guid.NewGuid().ToString(), Path = "1000,900,850", Minimum = 850, MinimumDate = new DateOnly(2026, 10, 3),
        };
        await _store.SaveForecastSnapshotAsync(snapshot, Ct);

        var saved = Assert.Single(await _store.GetForecastSnapshotsAsync(Ct));
        Assert.Equal([1_000, 900, 850], saved.Points().Select(p => p.Balance));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveForecastSnapshotAsync(snapshot, Ct));

        await _store.DeleteForecastSnapshotAsync(saved.Id, Ct);
        Assert.Empty(await _store.GetForecastSnapshotsAsync(Ct));
    }

    [Fact]
    public async Task S0701_a_quantity_goal_and_the_assumed_price_of_its_plan_are_kept()
    {
        var type = Guid.NewGuid();
        var goal = new Goal { Name = "50 g gold", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = type, TargetAmount = 50_000 };
        await _goals.SaveGoalAsync(goal, Ct);
        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Method = ContributionMethod.FixedAmount, Amount = 2_000, AssumedPricePerUnitMilli = 6_000_000 }, Ct);

        var saved = (await _goals.GetGoalsAsync(Ct)).Single();
        var plan = Assert.Single(await _goals.GetContributionPlansAsync(Ct));

        Assert.Equal((GoalType.HoldingQuantity, type, 50_000L), (saved.Type, saved.AssetTypeId!.Value, saved.TargetAmount));
        Assert.Equal(6_000_000, plan.AssumedPricePerUnitMilli);
    }
}
