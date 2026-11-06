using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Tests.Goals;

/// <summary>Negative recorded balances never become positive progress above a supported goal target.</summary>
public sealed class GoalOvershootBoundaryTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    [Theory]
    [InlineData(long.MinValue, 1, 0)]
    [InlineData(long.MinValue + 1, long.MaxValue, 0)]
    [InlineData(-2, long.MaxValue, 0)]
    [InlineData(-1, long.MaxValue, 0)]
    [InlineData(0, long.MaxValue, 0)]
    [InlineData(long.MaxValue, long.MaxValue, 0)]
    [InlineData(long.MaxValue, 1, long.MaxValue - 1)]
    [InlineData(2, 1, 1)]
    public void Original_preview_compares_before_subtracting(long current, long target, long expected)
    {
        var goal = new Goal { Name = "Fictitious balance target", Type = GoalType.AccountBalance,
            CurrencyCode = "EUR", TargetAmount = target };
        var before = JsonSerializer.Serialize(goal);

        var progress = GoalProgressService.Scenario(goal, current, 0, null, Today);

        Assert.Equal(expected, progress.Overshoot);
        Assert.Equal(Math.Max(0, target - Math.Max(0, current)), progress.Remaining);
        Assert.Equal(current >= target, progress.IsReached);
        Assert.Equal(before, JsonSerializer.Serialize(goal));
    }

    [Theory]
    [InlineData(long.MinValue, 1)]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MinValue + 1, long.MaxValue)]
    [InlineData(-2, long.MaxValue)]
    public void Original_balance_goal_keeps_debt_and_source_without_false_overshoot(long opening, long target)
    {
        var account = new Account { Name = "Fictitious negative checking", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningBalance = opening, OpeningDate = Today };
        var goal = new Goal { Name = "Fictitious balance target", Type = GoalType.AccountBalance,
            CurrencyCode = "EUR", AccountId = account.Id, TargetAmount = target };
        var before = JsonSerializer.Serialize(new { Account = account, Goal = goal });

        var progress = Assert.Single(GoalProgressService.Evaluate([goal], [], [account], [], [], Today));

        Assert.Equal(opening, progress.Current);
        Assert.Equal(GoalNotice.NegativeBalance, progress.Notice);
        Assert.Equal(0, progress.Progress);
        Assert.False(progress.IsReached);
        Assert.Equal(target, progress.Remaining);
        Assert.Null(progress.Required);
        Assert.Null(progress.Eta);
        Assert.Equal(0, progress.Overshoot);
        Assert.Equal(before, JsonSerializer.Serialize(new { Account = account, Goal = goal }));
    }
}
