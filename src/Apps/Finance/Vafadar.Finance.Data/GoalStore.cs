using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Goals;

namespace Vafadar.Finance.Data;

/// <summary>
/// Data access for savings goals and their allocations (F2-GOAL-01..05). Allocations are earmarks only: nothing here
/// writes a ledger entry or changes an account balance (F2-GOAL-03).
/// </summary>
public sealed class GoalStore(IDbContextFactory<FinanceDbContext> contextFactory)
{
    /// <summary>Raised after goals or allocations were written.</summary>
    public event EventHandler? Changed;

    /// <summary>Returns all goals: active first, then by priority and target date.</summary>
    public async Task<List<Goal>> GetGoalsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
        return [.. goals.OrderBy(g => g.State).ThenBy(g => g.Priority).ThenBy(g => g.TargetDate ?? DateOnly.MaxValue).ThenBy(g => g.Name)];
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

    /// <summary>Inserts or updates a goal.</summary>
    public async Task SaveGoalAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
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

    /// <summary>Deletes a goal and its allocations; ledger entries are never touched.</summary>
    public async Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.GoalAllocations.Where(a => a.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Goals.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}