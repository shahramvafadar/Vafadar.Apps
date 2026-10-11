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

    /// <summary>Reads complete original goal inputs and current exact-file rights without writing or filtering history.</summary>
    public async Task<GoalWorkSnapshot> GetWorkSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var access = new CommercialFileAccess(commercialAccess, ProfileFilePath(db));
        access.DemandFeature(CommercialFeature.History);
        var stored = await StoredResourceSelection.ReadAsync(db, access, QuotaKind.Goals, cancellationToken);
        var originals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
        var plans = await db.ContributionPlans.AsNoTracking().ToListAsync(cancellationToken);
        var allocations = await db.GoalAllocations.AsNoTracking().ToListAsync(cancellationToken);
        var references = GoalWorkSnapshot.CaptureReferences(await db.Accounts.AsNoTracking().ToListAsync(cancellationToken),
            await db.AssetTypes.AsNoTracking().ToListAsync(cancellationToken));
        var context = access.Context;
        var work = context is null ? GoalWorkPolicy.Inactive : GoalWorkPolicy.Resolve(context,
            StoredResourceSelection.Scope(context), originals, stored?.ForNewWork() ?? access.GoalSelection);
        if (StoredResourceSelection.Capture(await StoredResourceSelection.ReadAsync(db, access, QuotaKind.Goals, cancellationToken)) != StoredResourceSelection.Capture(stored))
            throw new InvalidOperationException("The goal choice changed during notification preparation.");
        access.EnsureSnapshotCurrent();
        var path = ProfileFilePath(db);
        return new(Ordered(originals), plans, allocations, work)
        {
            DatabasePath = path, Access = access, SelectionRevision = stored?.Revision, SelectionStamp = StoredResourceSelection.Capture(stored),
            InputStamp = GoalWorkSnapshot.CaptureInputs(originals, plans, allocations),
            ReferenceStamp = references,
            ReminderScope = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(path))),
        };
    }

    /// <summary>Rejects another file, retired rights or a changed/ABA choice before and after native publication.</summary>
    public async Task ValidateWorkSnapshotAsync(GoalWorkSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await ValidateWorkSnapshotAsync(snapshot, db, cancellationToken);
    }

    /// <summary>Rechecks a reviewed form under the same SQLite writer that will commit its explicit action.</summary>
    internal static async Task ValidateWorkSnapshotAsync(GoalWorkSnapshot snapshot, ZananceDbContext db, CancellationToken cancellationToken)
    {
        if (snapshot.Access is null || !string.Equals(snapshot.DatabasePath,
            ProfileFilePath(db), StringComparison.Ordinal))
            throw new InvalidOperationException("The goal work snapshot belongs to another opened file.");
        snapshot.Access.DemandFeature(CommercialFeature.History);
        snapshot.Access.EnsureSnapshotCurrent();
        if (StoredResourceSelection.Capture(await StoredResourceSelection.ReadAsync(db, snapshot.Access, QuotaKind.Goals, cancellationToken)) != snapshot.SelectionStamp)
            throw new InvalidOperationException("The original goal choice changed during notification preparation.");
        var originals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
        var plans = await db.ContributionPlans.AsNoTracking().ToListAsync(cancellationToken);
        var allocations = await db.GoalAllocations.AsNoTracking().ToListAsync(cancellationToken);
        if (GoalWorkSnapshot.CaptureInputs(originals, plans, allocations) != snapshot.InputStamp)
            throw new InvalidOperationException("Goal notification inputs changed during preparation.");
        if (GoalWorkSnapshot.CaptureReferences(await db.Accounts.AsNoTracking().ToListAsync(cancellationToken),
            await db.AssetTypes.AsNoTracking().ToListAsync(cancellationToken)) != snapshot.ReferenceStamp)
            throw new InvalidOperationException("Goal account or holding units changed after the form was reviewed.");
        snapshot.Access.EnsureSnapshotCurrent();
    }

    /// <summary>Gets the actual factory-bound file without SQLite's connection-state-dependent alias resolution.</summary>
    private static string ProfileFilePath(ZananceDbContext db)
    {
        // Android's SQLite DataSource can resolve an app-private path differently while a transaction is open.
        // D-146 uses the same immutable connection setting for open writers and auto-closed reads;
        // another factory/profile still produces a different path and is rejected.
        return Path.GetFullPath(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
            db.Database.GetDbConnection().ConnectionString).DataSource);
    }

    /// <summary>Returns all goals: active first, then by priority and target date.</summary>
    public async Task<List<Goal>> GetGoalsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
        return Ordered(goals);
    }

    /// <summary>Keeps the established original state/priority/date/name ordering for every complete read.</summary>
    private static List<Goal> Ordered(IEnumerable<Goal> goals) =>
        [.. goals.OrderBy(g => StateOrder(g.State)).ThenBy(g => g.Priority).ThenBy(g => g.TargetDate ?? DateOnly.MaxValue).ThenBy(g => g.Name)];

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
    public async Task SaveGoalAsync(Goal goal, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: review is not null);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
        var activity = await CheckRetainedContributionActivationAsync(db, write, goal, cancellationToken);
        await PrepareGoalAsync(db, write, goal, cancellationToken, activity);

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Saves the complete goal editor draft and its contribution plan atomically; null removes the plan.</summary>
    public async Task SaveGoalWithContributionPlanAsync(Goal goal, ContributionPlan? plan, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
        var existing = await db.ContributionPlans.FirstOrDefaultAsync(p => p.GoalId == goal.Id, cancellationToken);
        var original = write.Enforced ? await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goal.Id, cancellationToken) : null;
        // D-123: reject the complete draft before pin/protection normalization or an old plan can change.
        var activity = CheckContributionSave(write, plan, existing, goal, original);
        await PrepareGoalAsync(db, write, goal, cancellationToken, activity);
        PrepareContributionPlan(db, goal.Id, plan, existing);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Prepares the established goal invariants and capacity checks inside the caller's writer.</summary>
    private static async Task PrepareGoalAsync(ZananceDbContext db, CommercialWriteTransaction write, Goal goal,
        CancellationToken cancellationToken, CommercialFeature? contributionActivity = null)
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
            if (counted && !wasCounted && !write.HasStoredGoalChoice && write.DemandCapacity(CommercialFeature.BasicGoals, QuotaKind.Goals) is { } maximum)
            {
                var current = others.Count(g => g.State is GoalState.Active or GoalState.Paused);
                write.DemandCount(CommercialFeature.BasicGoals, QuotaKind.Goals, maximum, current);
            }
        }
        // D-143: retain historical state/corrections, but join a new or reopened goal under the same writer.
        await write.PrepareGoalChangeAsync(db, goal, existing, cancellationToken);
        // D-145: a newly joined slot is visible inside this writer, before pin/protection normalization.
        // Retained corrections and disabling/removing a contribution need no selected new-work identity.
        if (contributionActivity is { } feature)
            write.DemandSelectedGoal(feature, others.Append(goal), goal.Id);
        if (write.Enforced && existing?.State == GoalState.Paused && goal.State == GoalState.Active)
            write.DemandSelectedGoal(CommercialFeature.BasicGoals, others.Append(existing), goal.Id);
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
    private static async Task<CommercialFeature?> CheckRetainedContributionActivationAsync(ZananceDbContext db, CommercialWriteTransaction write,
        Goal target, CancellationToken cancellationToken)
    {
        if (!write.Enforced || target.State is not (GoalState.Active or GoalState.Paused)) return null;
        var original = await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == target.Id, cancellationToken);
        if (original is null || original.State is GoalState.Active or GoalState.Paused) return null;
        var plan = await db.ContributionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.GoalId == target.Id, cancellationToken);
        return plan is null ? null : CheckContributionSave(write, plan, plan, target, original);
    }

    /// <summary>Records an allocation (positive) or a release (negative).</summary>
    public async Task AddAllocationAsync(GoalAllocation allocation, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (allocation.Amount == 0)
        {
            throw new ArgumentException("An allocation must not be zero.", nameof(allocation));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: review is not null);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
        write.DemandFeature(allocation.Amount > 0 ? CommercialFeature.AdvancedGoals : CommercialFeature.Corrections);
        if (allocation.Amount > 0 && write.Enforced)
        {
            var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
            write.DemandSelectedGoal(CommercialFeature.AdvancedGoals, goals, allocation.GoalId);
            var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(account => account.Id, cancellationToken);
            write.DemandSelectedAccounts(CommercialFeature.AdvancedGoals, accounts, [allocation.AccountId]);
        }
        db.GoalAllocations.Add(allocation);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes one allocation (e.g. a mistake); balances are not affected.</summary>
    public async Task DeleteAllocationAsync(Guid id, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: review is not null);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
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
    public async Task SaveContributionPlanAsync(Guid goalId, ContributionPlan? plan, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: review is not null);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
        var existing = await db.ContributionPlans.FirstOrDefaultAsync(p => p.GoalId == goalId, cancellationToken);
        var goal = write.Enforced ? await db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goalId, cancellationToken) : null;
        var activity = CheckContributionSave(write, plan, existing, goal, goal);
        if (activity is { } feature)
            write.DemandSelectedGoal(feature, await db.Goals.AsNoTracking().ToListAsync(cancellationToken), goalId);
        PrepareContributionPlan(db, goalId, plan, existing);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Checks new paid contribution work independently of retained corrections and empty date scaffolding.</summary>
    private static CommercialFeature? CheckContributionSave(CommercialWriteTransaction write, ContributionPlan? plan, ContributionPlan? existing,
        Goal? target, Goal? original)
    {
        if (!write.Enforced) return null;
        if (plan is null)
        {
            write.DemandFeature(CommercialFeature.DeleteData);
            return null;
        }
        if (!Enum.IsDefined(plan.Method)) throw new ArgumentException("A contribution requires a known method.", nameof(plan));
        var newWork = GoalContributionPolicy.StartsWork(plan, existing, target, original);
        write.DemandFeature(newWork ? CommercialFeature.AdvancedGoals : CommercialFeature.Corrections);
        // D-123: empty date rows support the basic goal without an amount. Reminders are a separate paid tool;
        // turning one off/removing history never consumes a new right or creates an earmark/ledger movement.
        var startsReminder = GoalContributionPolicy.StartsReminder(plan, existing, target, original);
        if (startsReminder)
            write.DemandFeature(CommercialFeature.ContributionReviewReminders);
        return newWork ? CommercialFeature.AdvancedGoals : startsReminder ? CommercialFeature.ContributionReviewReminders : null;
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
    public async Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default, GoalWorkSnapshot? review = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        if (review is not null) await ValidateWorkSnapshotAsync(review, db, cancellationToken);
        write.DemandFeature(CommercialFeature.DeleteData);
        await db.GoalAllocations.Where(a => a.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.ContributionPlans.Where(p => p.GoalId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Goals.Where(g => g.Id == id).ExecuteDeleteAsync(cancellationToken);
        write.PrepareGoalDeletion(id);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
