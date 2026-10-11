using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Data side of enhancement ZEX phase 2: balance goals, Home pins and contribution plans.</summary>
public sealed class ZexPhase2DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly GoalStore _goals;

    public ZexPhase2DataTests()
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
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task An_account_has_at_most_one_active_balance_goal()
    {
        var account = await AccountAsync();
        await _goals.SaveGoalAsync(BalanceGoal(account, "Emergency fund"), Ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _goals.SaveGoalAsync(BalanceGoal(account, "Second"), Ct));

        // Money set aside on the same account is a different kind and allowed.
        await _goals.SaveGoalAsync(new Goal { Name = "Trip", CurrencyCode = "EUR", TargetAmount = 500_00 }, Ct);
        Assert.Equal(2, (await _goals.GetGoalsAsync(Ct)).Count);
    }

    [Fact]
    public async Task A_balance_goal_cannot_be_protected()
    {
        var account = await AccountAsync();
        var goal = BalanceGoal(account, "Emergency fund");
        goal.Protect = true;

        await _goals.SaveGoalAsync(goal, Ct);

        Assert.False((await _goals.GetGoalAsync(goal.Id, Ct))!.Protect);
    }

    [Fact]
    public async Task Pinning_a_third_goal_unpins_the_first()
    {
        var goals = new[] { "A", "B", "C" }.Select(n => new Goal { Name = n, CurrencyCode = "EUR", TargetAmount = 100 }).ToList();
        for (var i = 0; i < goals.Count; i++)
        {
            goals[i].HomePin = i + 1;
            await _goals.SaveGoalAsync(goals[i], Ct);
        }

        var pinned = (await _goals.GetGoalsAsync(Ct)).Where(g => g.HomePin is not null).Select(g => g.Name).Order();
        Assert.Equal(["B", "C"], pinned);
    }

    [Fact]
    public async Task A_contribution_plan_is_kept_replaced_and_removed_with_its_goal()
    {
        var goal = new Goal { Name = "Trip", CurrencyCode = "EUR", TargetAmount = 3_000_00 };
        await _goals.SaveGoalAsync(goal, Ct);
        var rule = new RecurrenceRule { Frequency = Frequency.Weekly, Interval = 2, Start = new DateOnly(2026, 10, 3) };

        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Method = ContributionMethod.FixedAmount, Amount = 150_00, Rule = rule }, Ct);
        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan { Method = ContributionMethod.ShareOfIncome, Percent = 10m, Rule = rule }, Ct);

        var plan = Assert.Single(await _goals.GetContributionPlansAsync(Ct));
        Assert.Equal((ContributionMethod.ShareOfIncome, 10m, 2), (plan.Method, plan.Percent!.Value, plan.Rule.Interval));

        await _goals.DeleteGoalAsync(goal.Id, Ct);
        Assert.Empty(await _goals.GetContributionPlansAsync(Ct));
    }

    private async Task<Account> AccountAsync()
    {
        var account = new Account { Name = "Savings", Type = AccountType.Savings, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }

    private static Goal BalanceGoal(Account account, string name) =>
        new() { Name = name, CurrencyCode = account.CurrencyCode, TargetAmount = 5_000_00, Type = GoalType.AccountBalance, AccountId = account.Id };
}
