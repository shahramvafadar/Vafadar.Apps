using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Goals;

/// <summary>Why a goal shows a message instead of, or besides, its numbers (ZEX-S0303).</summary>
public enum GoalNotice
{
    /// <summary>Nothing to say.</summary>
    None = 0,

    /// <summary>The target date has passed and the goal is not reached: the remaining amount is needed now.</summary>
    Overdue = 1,

    /// <summary>The account of a balance goal is archived or gone: choose another account or complete the goal.</summary>
    AccountUnavailable = 2,

    /// <summary>The goal is paused.</summary>
    Paused = 3,

    /// <summary>The balance of a balance goal is negative: progress counts as 0 %.</summary>
    NegativeBalance = 4,

    /// <summary>The holding type of a quantity goal is archived or gone.</summary>
    HoldingUnavailable = 5,
}

/// <summary>
/// The numbers of one goal for every view (design §8.4, §9.1): current amount, remaining, progress, overshoot, the
/// derived states and the scenario of the user's plan. For all types with target T and current F:
/// remaining = max(0, T − F), progress = clamp(F / T, 0, 1), overshoot = max(0, F − T).
/// </summary>
/// <param name="Goal">The goal.</param>
/// <param name="Current">F: the account balance (balance goal), the covered earmarks (money set aside) or the held quantity (quantity goal).</param>
/// <param name="Unfunded">Earmarked money no balance covers (money set aside only).</param>
/// <param name="Opportunities">N: contribution dates from today to the target date, both inclusive.</param>
/// <param name="Required">Contribution per date that reaches the target in time; <see langword="null"/> without a target date.</param>
/// <param name="PlannedContribution">C: the amount of the user's plan per date, when known.</param>
/// <param name="Eta">The date the plan reaches the target (ceil(R / C)-th date); <see langword="null"/> when there is none.</param>
/// <param name="Notice">A message that belongs to the goal.</param>
public sealed record GoalProgress(
    Goal Goal,
    long Current,
    long Unfunded,
    int Opportunities,
    long? Required,
    long? PlannedContribution,
    DateOnly? Eta,
    GoalNotice Notice)
{
    /// <summary>Gets T, the target.</summary>
    public long Target => Goal.TargetAmount;

    /// <summary>Gets max(0, T − F).</summary>
    public long Remaining => Math.Max(0, Target - Math.Max(0, Current));

    /// <summary>Gets max(0, F − T): how far the goal is above its target.</summary>
    public long Overshoot => Math.Max(0, Current - Target);

    /// <summary>Gets clamp(F / T, 0, 1) for the bar; the real numbers are always shown next to it.</summary>
    public double Progress => Target <= 0 ? 0 : Math.Clamp((double)Current / Target, 0, 1);

    /// <summary>Gets a value indicating whether the target is reached now (derived, never stored, ZEX-P13).</summary>
    public bool IsReached => Target > 0 && Current >= Target;

    /// <summary>Gets a value indicating whether the target date passed without reaching the target (derived).</summary>
    public bool IsOverdue => Notice == GoalNotice.Overdue;
}

/// <summary>
/// Evaluates goals of every type (ZEX-S0301): money set aside through <see cref="GoalCalculator"/> (same funding and
/// priority rules as before, so existing goals keep their numbers), balance goals from the recorded account balance.
/// One money source never counts twice: balance goals reserve nothing and are never subtracted anywhere.
/// </summary>
public static class GoalProgressService
{
    /// <summary>Evaluates the goals that are not archived or completed (active and paused).</summary>
    /// <param name="goals">All goals.</param>
    /// <param name="allocations">All earmarks.</param>
    /// <param name="accounts">All accounts.</param>
    /// <param name="entries">All entries (for balances).</param>
    /// <param name="plans">The contribution plans of the goals.</param>
    /// <param name="today">The current date.</param>
    /// <param name="events">The holding events, for quantity goals (ZEX-S0701); without them a quantity goal holds nothing.</param>
    /// <param name="types">The holding types, to tell an archived type of a quantity goal.</param>
    public static IReadOnlyList<GoalProgress> Evaluate(
        IEnumerable<Goal> goals,
        IEnumerable<GoalAllocation> allocations,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<ContributionPlan> plans,
        DateOnly today,
        IReadOnlyCollection<AssetEvent>? events = null,
        IReadOnlyCollection<AssetType>? types = null)
    {
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(accounts);
        var balances = accounts.ToDictionary(a => a.Id, a => LedgerCalculator.Balance(a, entries, today));
        var goalList = goals.Where(g => g.State is GoalState.Active or GoalState.Paused).ToList();
        var earmarks = GoalCalculator.Evaluate(goalList.Where(g => g.Type == GoalType.Earmark), allocations, balances, today).ToDictionary(s => s.Goal.Id);
        var planByGoal = plans.GroupBy(p => p.GoalId).ToDictionary(g => g.Key, g => g.First());
        var byId = accounts.ToDictionary(a => a.Id);

        var result = new List<GoalProgress>();
        foreach (var goal in goalList)
        {
            long current = 0, unfunded = 0;
            var notice = GoalNotice.None;
            if (goal.Type == GoalType.AccountBalance)
            {
                if (goal.AccountId is { } accountId && byId.TryGetValue(accountId, out var account) && !account.IsArchived)
                {
                    current = balances[accountId];
                    notice = current < 0 ? GoalNotice.NegativeBalance : GoalNotice.None;
                }
                else
                {
                    notice = GoalNotice.AccountUnavailable;
                }
            }
            else if (goal.Type == GoalType.HoldingQuantity)
            {
                // The quantity held, of all locations or the chosen one; a price change is never progress (AT25, AT26).
                current = HeldQuantity(goal, events ?? [], today);
                if (goal.AssetTypeId is not { } typeId || (types is not null && types.FirstOrDefault(t => t.Id == typeId) is not { IsArchived: false }))
                {
                    notice = GoalNotice.HoldingUnavailable;
                }
            }
            else if (earmarks.TryGetValue(goal.Id, out var status))
            {
                current = status.Funded;
                unfunded = status.Unfunded;
            }

            result.Add(Scenario(goal, current, unfunded, planByGoal.GetValueOrDefault(goal.Id), today, notice));
        }

        return result;
    }

    /// <summary>
    /// Computes the scenario of one goal from its current amount (also for the live preview of the editor, ZEX-GO14):
    /// the contribution dates up to the target date, the required contribution and the ETA of the user's plan.
    /// </summary>
    public static GoalProgress Scenario(Goal goal, long current, long unfunded, ContributionPlan? plan, DateOnly today, GoalNotice notice = GoalNotice.None)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var rule = ContributionSchedule.RuleFor(goal, plan, today);
        var remaining = Math.Max(0, goal.TargetAmount - Math.Max(0, current));
        var opportunities = goal.TargetDate is { } date ? ContributionSchedule.Opportunities(rule, today, date) : 0;
        long? required = goal.TargetDate is null ? null : GoalCalculator.SuggestedContribution(remaining, opportunities);
        long? planned = plan is { Method: ContributionMethod.FixedAmount, Amount: > 0 } ? plan.Amount : null;
        var eta = planned is { } contribution ? ContributionSchedule.Eta(rule, today, remaining, contribution) : null;

        var reached = goal.TargetAmount > 0 && current >= goal.TargetAmount;
        if (notice == GoalNotice.None && goal.State == GoalState.Paused)
        {
            notice = GoalNotice.Paused;
        }

        if (notice is GoalNotice.None or GoalNotice.NegativeBalance && goal.TargetDate is { } due && due < today && !reached)
        {
            notice = GoalNotice.Overdue;
        }

        return new GoalProgress(goal, current, unfunded, opportunities, required, planned, eta, notice);
    }

    /// <summary>Returns the quantity a quantity goal counts: the holding type at all locations or at its one location.</summary>
    public static long HeldQuantity(Goal goal, IEnumerable<AssetEvent> events, DateOnly at)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(events);
        if (goal.AssetTypeId is not { } typeId)
        {
            return 0;
        }

        var ofType = events.Where(e => e.AssetTypeId == typeId).ToList();
        return goal.LocationId is { } location
            ? HoldingsLedger.Positions(ofType, at).Where(p => p.LocationId == location).Sum(p => p.Quantity)
            : HoldingsLedger.Quantity(ofType, typeId, at);
    }

    /// <summary>
    /// Returns whether another active balance goal already follows <paramref name="accountId"/> (ZEX-P12: at most one per
    /// account; money set aside on the same account is a different kind and allowed).
    /// </summary>
    public static bool HasOtherBalanceGoal(IEnumerable<Goal> goals, Guid accountId, Guid exceptGoalId) =>
        goals.Any(g => g.Id != exceptGoalId && g.Type == GoalType.AccountBalance && g.AccountId == accountId && g.State is GoalState.Active or GoalState.Paused);
}
