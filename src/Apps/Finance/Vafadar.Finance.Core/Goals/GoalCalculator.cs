namespace Vafadar.Finance.Core.Goals;

/// <summary>Progress of one goal.</summary>
/// <param name="Goal">The goal.</param>
/// <param name="Allocated">Money earmarked for the goal (allocations minus releases).</param>
/// <param name="Funded">The part of <paramref name="Allocated"/> that the account balances actually cover.</param>
/// <param name="Remaining">Target minus funded money; never negative.</param>
/// <param name="Opportunities">Contributions left until the target date, including the current period.</param>
/// <param name="SuggestedContribution">Next contribution that reaches the target in time; <see langword="null"/> without a target date.</param>
public sealed record GoalStatus(Goal Goal, long Allocated, long Funded, long Remaining, int Opportunities, long? SuggestedContribution)
{
    /// <summary>Gets the earmarked money that no account balance covers (shown as "Unfunded", F2-GOAL-02).</summary>
    public long Unfunded => Allocated - Funded;

    /// <summary>Gets the funded share of the target between 0 and 1.</summary>
    public double Progress => Goal.TargetAmount <= 0 ? 0 : Math.Clamp((double)Funded / Goal.TargetAmount, 0, 1);

    /// <summary>Gets a value indicating whether the funded money reaches the target.</summary>
    public bool IsReached => Goal.TargetAmount > 0 && Funded >= Goal.TargetAmount;

    /// <summary>Gets a value indicating whether the target date has passed without reaching the target.</summary>
    public bool IsOverdue(DateOnly today) => Goal.TargetDate is { } date && date < today && !IsReached;
}

/// <summary>Earmarked and free money of one account.</summary>
/// <param name="AccountId">The account.</param>
/// <param name="Balance">Recorded balance.</param>
/// <param name="Earmarked">Money allocated to active goals.</param>
public sealed record AccountEarmark(Guid AccountId, long Balance, long Earmarked)
{
    /// <summary>Gets the money not earmarked for any goal; never negative.</summary>
    public long Unallocated => Math.Max(0, Balance - Earmarked);

    /// <summary>Gets how much the earmarked money exceeds the balance (F2-GOAL-05).</summary>
    public long Shortfall => Math.Max(0, Earmarked - Math.Max(0, Balance));
}

/// <summary>
/// Goal funding (F2-GOAL-02..05, BUD-11/12, AT-65). The same money never counts for two goals: when the allocations of
/// an account exceed its balance, the shortfall is taken from the goals with the lowest priority first, so the sum of
/// funded money never exceeds what the account holds.
/// </summary>
public static class GoalCalculator
{
    /// <summary>Evaluates the active goals.</summary>
    /// <param name="goals">All goals; only active ones are evaluated.</param>
    /// <param name="allocations">All allocations.</param>
    /// <param name="balances">Recorded balance per account.</param>
    /// <param name="today">The current date.</param>
    public static IReadOnlyList<GoalStatus> Evaluate(
        IEnumerable<Goal> goals,
        IEnumerable<GoalAllocation> allocations,
        IReadOnlyDictionary<Guid, long> balances,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(balances);
        var active = goals.Where(g => g.State == GoalState.Active).ToList();
        var funded = Funding(active, allocations.ToList(), balances);

        return
        [
            .. active.Select(goal =>
            {
                var allocated = funded.Where(f => f.Key.GoalId == goal.Id).Sum(f => f.Value.Allocated);
                var covered = funded.Where(f => f.Key.GoalId == goal.Id).Sum(f => f.Value.Funded);
                var remaining = Math.Max(0, goal.TargetAmount - covered);
                var opportunities = goal.TargetDate is { } date ? Opportunities(goal.Frequency, today, date) : 0;
                long? suggested = goal.TargetDate is null ? null : SuggestedContribution(remaining, opportunities);
                return new GoalStatus(goal, allocated, covered, remaining, opportunities, suggested);
            }),
        ];
    }

    /// <summary>Returns the earmarked and free money of every account with allocations to active goals.</summary>
    public static IReadOnlyList<AccountEarmark> Accounts(IEnumerable<Goal> goals, IEnumerable<GoalAllocation> allocations, IReadOnlyDictionary<Guid, long> balances)
    {
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(balances);
        var active = goals.Where(g => g.State == GoalState.Active).Select(g => g.Id).ToHashSet();
        return
        [
            .. allocations
                .Where(a => active.Contains(a.GoalId))
                .GroupBy(a => (a.AccountId, a.GoalId))
                .Select(g => (g.Key.AccountId, Amount: Math.Max(0, g.Sum(a => a.Amount))))
                .GroupBy(x => x.AccountId)
                .Select(g => new AccountEarmark(g.Key, balances.GetValueOrDefault(g.Key), g.Sum(x => x.Amount)))
                .Where(e => e.Earmarked > 0),
        ];
    }

    /// <summary>
    /// Returns the contributions left until <paramref name="target"/>, counting the current period: for a monthly goal
    /// due in three months that is 3 or 4 depending on the day. A past target date leaves none.
    /// </summary>
    public static int Opportunities(ContributionFrequency frequency, DateOnly today, DateOnly target)
    {
        if (target < today)
        {
            return 0;
        }

        if (frequency == ContributionFrequency.Weekly)
        {
            return ((target.DayNumber - today.DayNumber) / 7) + 1;
        }

        var count = 0;
        while (today.AddMonths(count) <= target)
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Splits the remaining amount evenly over the opportunities left, rounded up to the minor unit (F2-GOAL-04). With
    /// no opportunity left the whole remaining amount is due now. The monthly average of a yearly cost is not used.
    /// </summary>
    public static long SuggestedContribution(long remaining, int opportunities)
    {
        if (remaining <= 0)
        {
            return 0;
        }

        return opportunities <= 0 ? remaining : (remaining + opportunities - 1) / opportunities;
    }

    // Allocated and covered money per (account, goal). Within one account, a shortfall is taken from low priority first,
    // then from the goal needed latest, so urgent and important goals keep their funding.
    private static Dictionary<(Guid AccountId, Guid GoalId), (long Allocated, long Funded)> Funding(
        List<Goal> goals, List<GoalAllocation> allocations, IReadOnlyDictionary<Guid, long> balances)
    {
        var byId = goals.ToDictionary(g => g.Id);
        var result = new Dictionary<(Guid, Guid), (long, long)>();
        foreach (var account in allocations.Where(a => byId.ContainsKey(a.GoalId)).GroupBy(a => a.AccountId))
        {
            var perGoal = account
                .GroupBy(a => a.GoalId)
                .Select(g => (Goal: byId[g.Key], Allocated: Math.Max(0, g.Sum(a => a.Amount))))
                .ToList();
            var available = Math.Max(0, balances.GetValueOrDefault(account.Key));
            var shortfall = Math.Max(0, perGoal.Sum(g => g.Allocated) - available);

            foreach (var (goal, allocated) in perGoal
                         .OrderByDescending(g => g.Goal.Priority)
                         .ThenByDescending(g => g.Goal.TargetDate ?? DateOnly.MaxValue)
                         .ThenBy(g => g.Goal.Name, StringComparer.Ordinal))
            {
                var cut = Math.Min(shortfall, allocated);
                shortfall -= cut;
                result[(account.Key, goal.Id)] = (allocated, allocated - cut);
            }
        }

        return result;
    }
}
