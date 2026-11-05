using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Signed allocation histories keep supported pair nets without changing funding scope or priority.</summary>
public sealed class EarmarkNetReadTests
{
    private static readonly DateOnly Day = new(2026, 11, 1);

    /// <summary>Valid funded daily prefixes stay independent of storage order for active and paused goals.</summary>
    [Theory]
    [InlineData(0, 0, false), InlineData(0, 1, false), InlineData(0, 2, false), InlineData(0, 3, false), InlineData(0, 4, false), InlineData(0, 5, false)]
    [InlineData(1, 0, false), InlineData(1, 1, false), InlineData(1, 2, false), InlineData(1, 3, false), InlineData(1, 4, false), InlineData(1, 5, false)]
    [InlineData(2, 0, false), InlineData(2, 1, false), InlineData(2, 2, false), InlineData(2, 3, false), InlineData(2, 4, false), InlineData(2, 5, false)]
    [InlineData(3, 0, false), InlineData(3, 1, false), InlineData(3, 2, false), InlineData(3, 3, false), InlineData(3, 4, false), InlineData(3, 5, false)]
    [InlineData(0, 0, true), InlineData(0, 1, true), InlineData(0, 2, true), InlineData(0, 3, true), InlineData(0, 4, true), InlineData(0, 5, true)]
    [InlineData(1, 0, true), InlineData(1, 1, true), InlineData(1, 2, true), InlineData(1, 3, true), InlineData(1, 4, true), InlineData(1, 5, true)]
    [InlineData(2, 0, true), InlineData(2, 1, true), InlineData(2, 2, true), InlineData(2, 3, true), InlineData(2, 4, true), InlineData(2, 5, true)]
    [InlineData(3, 0, true), InlineData(3, 1, true), InlineData(3, 2, true), InlineData(3, 3, true), InlineData(3, 4, true), InlineData(3, 5, true)]
    public void Supported_funded_pair_net_is_storage_order_independent(int reader, int permutation, bool paused)
    {
        var account = Account(); var goal = Goal(); goal.State = paused ? GoalState.Paused : GoalState.Active;
        var original = Allocations(goal, account, [long.MaxValue, -long.MaxValue, long.MaxValue]);
        long prefix = 0;
        foreach (var row in original) { prefix = checked(prefix + row.Amount); Assert.True(prefix >= 0 && prefix <= account.OpeningBalance); }
        int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
        var source = orders[permutation].Select(i => original[i]).ToArray(); var before = State(source);
        Assert.Equal(long.MaxValue, Read(reader, goal, account, source));
        Assert.True(before.SequenceEqual(State(source)));
        for (var i = 0; i < source.Length; i++) Assert.Same(original[orders[permutation][i]], source[i]);
    }

    /// <summary>Unsupported signed pair nets still refuse before any nonnegative clamp.</summary>
    [Theory]
    [InlineData(0, false), InlineData(0, true), InlineData(1, false), InlineData(1, true)]
    [InlineData(2, false), InlineData(2, true), InlineData(3, false), InlineData(3, true)]
    public void Unsupported_pair_net_still_refuses(int reader, bool negative)
    {
        var account = Account(); var goal = Goal();
        var source = Allocations(goal, account, negative ? [-long.MaxValue, -2] : [long.MaxValue, 1]);
        var before = State(source); Assert.Throws<OverflowException>(() => Read(reader, goal, account, source));
        Assert.True(before.SequenceEqual(State(source)));
    }

    /// <summary>Already nonnegative per-goal totals cannot be widened to hide an unsupported account total.</summary>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void Unsupported_nonnegative_account_total_still_refuses(int reader)
    {
        var account = Account(); var first = Goal(); var second = Goal();
        var source = Allocations(first, account, [long.MaxValue]).Concat(Allocations(second, account, [1])).ToArray();
        var balances = new Dictionary<Guid, long> { [account.Id] = long.MaxValue }; var before = State(source);
        Assert.Throws<OverflowException>(() => reader switch
        {
            0 => (object)GoalCalculator.Evaluate([first, second], source, balances, Day),
            1 => GoalCalculator.Accounts([first, second], source, balances),
            _ => GoalProgressService.Evaluate([first, second], source, [account], [], [], Day)
        });
        Assert.True(before.SequenceEqual(State(source)));
    }

    /// <summary>Account/goal pair boundaries and the original active/paused selection remain independent.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Pair_and_lifecycle_scope_do_not_cross_offset(bool accounts)
    {
        var firstAccount = Account(); var secondAccount = Account(); secondAccount.OpeningBalance = 2;
        var first = Goal(); var second = Goal(); second.State = GoalState.Paused; second.TargetAmount = 2;
        var archived = Goal(); archived.State = GoalState.Archived;
        var source = Allocations(first, firstAccount, [long.MaxValue, long.MaxValue, -long.MaxValue])
            .Concat(Allocations(second, secondAccount, [2])).Concat(Allocations(archived, firstAccount, [long.MaxValue])).ToArray();
        source[1].Date = Day.AddDays(2); source[2].Date = Day.AddDays(1);
        var before = State(source); var balances = new Dictionary<Guid, long> { [firstAccount.Id] = long.MaxValue, [secondAccount.Id] = 2 };
        if (accounts)
        {
            var result = GoalCalculator.Accounts([first, second, archived], source, balances);
            Assert.Equal(2, result.Count); Assert.Equal(long.MaxValue, result.Single(r => r.AccountId == firstAccount.Id).Earmarked);
            Assert.Equal(2L, result.Single(r => r.AccountId == secondAccount.Id).Earmarked);
        }
        else
        {
            var result = GoalCalculator.Evaluate([first, second, archived], source, balances, Day);
            Assert.Equal(2, result.Count); Assert.Equal(long.MaxValue, result.Single(r => r.Goal.Id == first.Id).Funded);
            Assert.Equal(2L, result.Single(r => r.Goal.Id == second.Id).Funded);
        }
        Assert.True(before.SequenceEqual(State(source)));
    }

    /// <summary>A balance shortfall still cuts lower priority first after complete pair-net calculation.</summary>
    [Fact]
    public void Priority_shortfall_rule_remains_after_offsetting_history()
    {
        var account = Account(); var high = Goal(); high.Priority = GoalPriority.High;
        var low = Goal(); low.Priority = GoalPriority.Low;
        var source = Allocations(high, account, [long.MaxValue, 80, -long.MaxValue]).Concat(Allocations(low, account, [80])).ToArray();
        source[1].Date = Day.AddDays(2); source[2].Date = Day.AddDays(1); source[3].Date = Day.AddDays(3);
        var before = State(source); var result = GoalCalculator.Evaluate([low, high], source, new Dictionary<Guid, long> { [account.Id] = 100 }, Day.AddDays(3));
        Assert.Equal(80L, result.Single(r => r.Goal.Id == high.Id).Funded);
        Assert.Equal(20L, result.Single(r => r.Goal.Id == low.Id).Funded);
        Assert.True(before.SequenceEqual(State(source)));
    }

    /// <summary>The original ordinary negative-net clamp remains without weakening the range refusal.</summary>
    [Fact]
    public void Ordinary_negative_pair_keeps_the_original_zero_clamp()
    {
        var account = Account(); var goal = Goal(); var source = Allocations(goal, account, [-1]);
        var before = State(source); var balances = new Dictionary<Guid, long> { [account.Id] = long.MaxValue };
        Assert.Empty(GoalCalculator.Accounts([goal], source, balances));
        var status = Assert.Single(GoalCalculator.Evaluate([goal], source, balances, Day));
        Assert.Equal(0L, status.Allocated); Assert.Equal(0L, status.Funded); Assert.True(before.SequenceEqual(State(source)));
    }

    private static long Read(int reader, Goal goal, Account account, GoalAllocation[] source)
    {
        var balances = new Dictionary<Guid, long> { [account.Id] = account.OpeningBalance };
        return reader switch
        {
            0 => Assert.Single(GoalCalculator.Evaluate([goal], source, balances, Day.AddDays(3))).Funded,
            1 => Assert.Single(GoalCalculator.Accounts([goal], source, balances)).Earmarked,
            2 => Assert.Single(GoalProgressService.Evaluate([goal], source, [account], [], [], Day.AddDays(3))).Current,
            _ => GoalTrendService.Compute(goal, 0, [account], [], source, [], Day.AddDays(3), PeriodCalendar.Gregorian).SoFar
        };
    }

    private static Account Account() => new() { Name = "Fictitious funded source", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningDate = Day, OpeningBalance = long.MaxValue };
    private static Goal Goal() => new() { Name = "Fictitious earmark source", CurrencyCode = "EUR", Type = GoalType.Earmark, TargetAmount = long.MaxValue };
    private static GoalAllocation[] Allocations(Goal goal, Account account, long[] amounts) => amounts.Select((amount, i) => new GoalAllocation
    { GoalId = goal.Id, AccountId = account.Id, Amount = amount, Date = Day.AddDays(i), Note = "Fictitious in-memory history", CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch }).ToArray();
    private static object[] State(IEnumerable<GoalAllocation> source) => source.Select(a => (object)(a.Id, a.GoalId, a.AccountId, a.Amount, a.Date, a.Note, a.CreatedAt, a.UpdatedAt)).ToArray();
}
