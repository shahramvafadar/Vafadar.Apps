using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Data;

/// <summary>
/// Data access for savings goals and their allocations (F2-GOAL-01..05). Allocations are earmarks only: nothing here
/// writes a ledger entry or changes an account balance (F2-GOAL-03).
/// </summary>
public sealed class GoalStore(IDbContextFactory<ZananceDbContext> contextFactory)
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
        var others = await db.Goals.AsNoTracking().Where(g => g.Id != goal.Id).ToListAsync(cancellationToken);
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

        if (await db.Goals.AnyAsync(g => g.Id == goal.Id, cancellationToken))
        {
            db.Goals.Update(goal);
        }
        else
        {
            db.Goals.Add(goal);
        }

        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
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
        db.GoalAllocations.Add(allocation);
        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes one allocation (e.g. a mistake); balances are not affected.</summary>
    public async Task DeleteAllocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.GoalAllocations.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
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
        var existing = await db.ContributionPlans.FirstOrDefaultAsync(p => p.GoalId == goalId, cancellationToken);
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
        }

        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
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
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.GoalAllocations.Where(a => a.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.ContributionPlans.Where(p => p.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Goals.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
