using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>
/// Data access for savings goals and their allocations (F2-GOAL-01..05). Allocations are earmarks only: nothing here
/// writes a ledger entry or changes an account balance (F2-GOAL-03).
/// </summary>
public sealed class GoalStore(IDbContextFactory<ZananceDbContext> contextFactory, ICommercialWriteAccessSource commercialAccess)
{
    /// <summary>Raised after goals or allocations were written.</summary>
    public event EventHandler? Changed;

    /// <summary>Returns all goals: active first, then by priority and target date.</summary>
    public async Task<List<Goal>> GetGoalsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
        return [.. goals.OrderBy(g => StateOrder(g.State)).ThenBy(g => g.Priority).ThenBy(g => g.TargetDate ?? DateOnly.MaxValue).ThenBy(g => g.Name)];
    }

    /// <summary>Returns one goal.</summary>
    public async Task<Goal?> GetGoalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    /// <summary>Returns the allocations, optionally of one goal, newest first.</summary>
    public async Task<List<GoalAllocation>> GetAllocationsAsync(Guid? goalId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.GoalAllocations.AsNoTracking();
        if (goalId is { } id)
        {
            query = query.Where(a => a.GoalId == id);
        }

        var list = await query.ToListAsync(cancellationToken);
        return [.. list.OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt)];
    }

    /// <summary>
    /// Inserts or updates a goal. A balance goal needs an account, and an account has at most one active or paused balance
    /// goal (ZEX-P12); at most two goals are pinned to Home – pinning a third takes the pin of the oldest (ZEX-GO06).
    /// </summary>
    /// <exception cref="InvalidOperationException">The account already has a balance goal.</exception>
    public async Task SaveGoalAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        await CheckRetainedContributionActivationAsync(db, write, goal, cancellationToken);
        await PrepareGoalAsync(db, write, goal, cancellationToken);

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Saves the complete goal editor draft and its contribution plan atomically; null removes the plan.</summary>
    public async Task SaveGoalWithContributionPlanAsync(Goal goal, ContributionPlan? plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        var existing = await db.ContributionPlans.FirstOrDefaultAsync(p => p.GoalId == goal.Id, cancellationToken);
        var original = write.Enforced ? await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goal.Id, cancellationToken) : null;
        // D-123: reject the complete draft before pin/protection normalization or an old plan can change.
        CheckContributionSave(write, plan, existing, goal, original);
        await PrepareGoalAsync(db, write, goal, cancellationToken);
        PrepareContributionPlan(db, goal.Id, plan, existing);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Prepares the established goal invariants and capacity checks inside the caller's writer.</summary>
    private static async Task PrepareGoalAsync(ZananceDbContext db, CommercialWriteTransaction write, Goal goal, CancellationToken cancellationToken)
    {
        var existing = await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goal.Id, cancellationToken);
        var others = await db.Goals.AsNoTracking().Where(g => g.Id != goal.Id).ToListAsync(cancellationToken);
        if (write.Enforced)
        {
            if (!Enum.IsDefined(goal.State) || !Enum.IsDefined(goal.Type))
                throw new ArgumentException("A goal requires a known type and state.", nameof(goal));
            var counted = goal.State is GoalState.Active or GoalState.Paused;
            var wasCounted = existing?.State is GoalState.Active or GoalState.Paused;
            var newUse = existing is null || (counted && !wasCounted);
            var addsAdvancedKind = goal.Type != GoalType.AccountBalance && (newUse || (counted && existing!.Type != goal.Type));
            write.DemandFeature(addsAdvancedKind ? CommercialFeature.AdvancedGoals
                : newUse ? CommercialFeature.BasicGoals : CommercialFeature.Corrections);
            // D-120: pause retains its slot. Reopening completed/archived goals consumes capacity before pin or
            // protection normalization can mutate the supplied draft or another goal's stored metadata.
            if (counted && !wasCounted && write.DemandCapacity(CommercialFeature.BasicGoals, QuotaKind.Goals) is { } maximum)
            {
                var current = others.Count(g => g.State is GoalState.Active or GoalState.Paused);
                write.DemandCount(CommercialFeature.BasicGoals, QuotaKind.Goals, maximum, current);
            }
        }
        if (goal.Type == GoalType.AccountBalance && goal.State is GoalState.Active or GoalState.Paused
            && (goal.AccountId is not { } accountId || GoalProgressService.HasOtherBalanceGoal(others, accountId, goal.Id)))
        {
            throw new InvalidOperationException("The account already has a balance goal.");
        }

        if (goal.Type != GoalType.Earmark)
        {
            // Only money set aside can be protected; a balance goal reserves nothing (ZEX-P16).
            goal.Protect = false;
        }

        if (goal.HomePin is not null)
        {
            var pinned = others.Where(g => g.HomePin is not null).OrderBy(g => g.HomePin).ToList();
            if (pinned.Count >= 2)
            {
                var oldest = pinned[0];
                oldest.HomePin = null;
                db.Goals.Update(oldest);
            }
        }

        if (existing is not null)
        {
            db.Goals.Update(goal);
        }
        else
        {
            db.Goals.Add(goal);
        }
    }

    /// <summary>Reopening a retained goal cannot silently reactivate its paid contribution work or reminders.</summary>
    private static async Task CheckRetainedContributionActivationAsync(ZananceDbContext db, CommercialWriteTransaction write,
        Goal target, CancellationToken cancellationToken)
    {
        if (!write.Enforced || target.State is not (GoalState.Active or GoalState.Paused)) return;
        var original = await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == target.Id, cancellationToken);
        if (original is null || original.State is GoalState.Active or GoalState.Paused) return;
        var plan = await db.ContributionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.GoalId == target.Id, cancellationToken);
        if (plan is not null) CheckContributionSave(write, plan, plan, target, original);
    }

    /// <summary>Records an allocation (positive) or a release (negative).</summary>
    public async Task AddAllocationAsync(GoalAllocation allocation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (allocation.Amount == 0)
        {
            throw new ArgumentException("An allocation must not be zero.", nameof(allocation));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        write.DemandFeature(allocation.Amount > 0 ? CommercialFeature.AdvancedGoals : CommercialFeature.Corrections);
        db.GoalAllocations.Add(allocation);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes one allocation (e.g. a mistake); balances are not affected.</summary>
    public async Task DeleteAllocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        write.DemandFeature(CommercialFeature.DeleteData);
        await db.GoalAllocations.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Returns the contribution plans of all goals (ZEX-GO08).</summary>
    public async Task<List<ContributionPlan>> GetContributionPlansAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContributionPlans.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>Saves the contribution plan of a goal, replacing an earlier one; <see langword="null"/> removes it.</summary>
    public async Task SaveContributionPlanAsync(Guid goalId, ContributionPlan? plan, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var existing = await db.ContributionPlans.FirstOrDefaultAsync(p => p.GoalId == goalId, cancellationToken);
        var goal = write.Enforced ? await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goalId, cancellationToken) : null;
        CheckContributionSave(write, plan, existing, goal, goal);
        PrepareContributionPlan(db, goalId, plan, existing);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Checks new paid contribution work independently of retained corrections and empty date scaffolding.</summary>
    private static void CheckContributionSave(CommercialWriteTransaction write, ContributionPlan? plan, ContributionPlan? existing,
        Goal? target, Goal? original)
    {
        if (!write.Enforced) return;
        if (plan is null)
        {
            write.DemandFeature(CommercialFeature.DeleteData);
            return;
        }
        if (!Enum.IsDefined(plan.Method)) throw new ArgumentException("A contribution requires a known method.", nameof(plan));
        static bool AdvancedDates(ContributionPlan p) => p.Rule.Frequency is not (Frequency.Weekly or Frequency.Monthly)
            || p.Rule.DayRule.IsWeekday() || p.Rule.SecondDay is not null || p.Rule.WeekendShift != WeekendShift.None;
        static bool Work(ContributionPlan? p) => p is not null && (p.Method != ContributionMethod.FixedAmount
            || p.Amount is not null || p.Percent is not null || p.CategoryIds.Count > 0
            || p.AssumedPricePerUnitMilli is not null || AdvancedDates(p));
        var counted = target?.State is GoalState.Active or GoalState.Paused;
        var reopening = counted && original is not null && original.State is not (GoalState.Active or GoalState.Paused);
        var newWork = Work(plan) && (!Work(existing) || reopening
            || (counted && existing is not null && (plan.Method != existing.Method || (AdvancedDates(plan) && !AdvancedDates(existing)))));
        write.DemandFeature(newWork ? CommercialFeature.AdvancedGoals : CommercialFeature.Corrections);
        // D-123: empty date rows support the basic goal without an amount. Reminders are a separate paid tool;
        // turning one off/removing history never consumes a new right or creates an earmark/ledger movement.
        if (plan.ReminderEnabled && (existing?.ReminderEnabled != true || reopening))
            write.DemandFeature(CommercialFeature.ContributionReviewReminders);
    }

    /// <summary>Copies the complete explicitly saved contribution draft without replacing an existing plan identity.</summary>
    private static void PrepareContributionPlan(ZananceDbContext db, Guid goalId, ContributionPlan? plan, ContributionPlan? existing)
    {
        if (plan is null)
        {
            if (existing is not null)
            {
                db.ContributionPlans.Remove(existing);
            }
        }
        else if (existing is null)
        {
            plan.GoalId = goalId;
            db.ContributionPlans.Add(plan);
        }
        else
        {
            existing.Method = plan.Method;
            existing.Amount = plan.Amount;
            existing.Percent = plan.Percent;
            existing.Rule = plan.Rule.Clone();
            existing.CategoryIds = [.. plan.CategoryIds];
            existing.ReminderEnabled = plan.ReminderEnabled;
            existing.AssumedPricePerUnitMilli = plan.AssumedPricePerUnitMilli;
        }
    }

    // Active goals first, then paused, completed and archived ones.
    private static int StateOrder(GoalState state) => state switch
    {
        GoalState.Active => 0,
        GoalState.Paused => 1,
        GoalState.Completed => 2,
        _ => 3,
    };

    /// <summary>Deletes a goal and its allocations; ledger entries are never touched.</summary>
    public async Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.DeleteData);
        await db.GoalAllocations.Where(a => a.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.ContributionPlans.Where(p => p.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Goals.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
