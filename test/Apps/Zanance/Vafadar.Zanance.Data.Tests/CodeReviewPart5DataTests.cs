using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Regression tests of the code review, part 5: the data layer.</summary>
public sealed class CodeReviewPart5DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly GoalStore _goals;

    public CodeReviewPart5DataTests()
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
    public async Task A_changed_assumed_price_of_an_existing_plan_is_kept()
    {
        // CR05-01: the goal page saves a new price into the existing plan; it was dropped on update.
        var goal = new Goal { Name = "50 g gold", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = Guid.NewGuid(), TargetAmount = 50_000 };
        await _goals.SaveGoalAsync(goal, Ct);
        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Method = ContributionMethod.FixedAmount, Amount = 2_000, AssumedPricePerUnitMilli = 6_000_000 }, Ct);

        var plan = Assert.Single(await _goals.GetContributionPlansAsync(Ct));
        plan.AssumedPricePerUnitMilli = 7_500_000;
        await _goals.SaveContributionPlanAsync(goal.Id, plan, Ct);
        Assert.Equal(7_500_000, Assert.Single(await _goals.GetContributionPlansAsync(Ct)).AssumedPricePerUnitMilli);

        plan.AssumedPricePerUnitMilli = null;
        await _goals.SaveContributionPlanAsync(goal.Id, plan, Ct);
        Assert.Null(Assert.Single(await _goals.GetContributionPlansAsync(Ct)).AssumedPricePerUnitMilli);
    }

    [Fact]
    public async Task Merging_categories_moves_them_in_spending_cuts_and_saved_filters()
    {
        // CR05-02: category lists of goal plans and saved filters followed no merge and kept the archived category.
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var expenses = (await _store.GetCategoriesAsync(Ct)).Where(c => c.Kind == CategoryKind.Expense).ToList();
        var (source, target, other) = (expenses[0], expenses[1], expenses[2]);
        var goal = new Goal { Name = "Trip", CurrencyCode = "EUR", TargetAmount = 100_000 };
        await _goals.SaveGoalAsync(goal, Ct);
        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Method = ContributionMethod.SpendingCut, Amount = 5_000, CategoryIds = [source.Id, other.Id] }, Ct);
        await _store.SaveSavedFilterAsync(new SavedFilter { Name = "Both", CategoryIds = [source.Id, target.Id] }, Ct);

        await _store.MergeCategoryAsync(source.Id, target.Id, Ct);

        Assert.Equal([target.Id, other.Id], Assert.Single(await _goals.GetContributionPlansAsync(Ct)).CategoryIds);
        Assert.Equal([target.Id], Assert.Single(await _store.GetSavedFiltersAsync(Ct)).CategoryIds);
    }

    [Fact]
    public async Task Default_categories_tell_listeners_only_when_some_were_added()
    {
        // CR05-03: a second call adds nothing and must not make every listener reload.
        var changes = 0;
        _store.Changed += (_, _) => changes++;

        await _store.EnsureDefaultCategoriesAsync(Ct);
        await _store.EnsureDefaultCategoriesAsync(Ct);

        Assert.Equal(1, changes);
    }
}