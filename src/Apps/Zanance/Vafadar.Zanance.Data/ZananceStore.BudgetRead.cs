using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>One original-file budget read, including complete rollover and envelope history.</summary>
public sealed record BudgetReadSnapshot(BudgetEditorReview Review, List<LedgerEntry> Entries,
    List<Schedule> Schedules, List<OccurrenceState> States, List<Goal> Goals, List<GoalAllocation> Allocations)
{
    /// <summary>Calculates the unchanged carry from this same original snapshot, including unreviewed history.</summary>
    public BudgetCarry CarryFor(Budget budget) => ZananceStore.CalculateBudgetCarry(budget,
        ZananceStore.BudgetCarryChain(budget, Review.Choices.Items.Select(item => item.ToBudget())),
        Review.Settings?.MonthStartDay ?? 1, Review.Accounts.ToList(), Entries, Review.Categories.ToList());
}

public sealed partial class ZananceStore
{
    /// <summary>Reads all budget financial inputs on a worker with one captured lease and deferred SQLite transaction.</summary>
    public async Task<BudgetReadSnapshot> GetBudgetReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Capture the original file before scheduling; maintenance must not redirect an already requested read.
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            // SQLite async materialization/key derivation is synchronous; never perform it on the window thread.
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var review = await GetBudgetEditorReviewAsync(db, cancellationToken);
            var entries = await ReadCompleteHomeLedgerAsync(db, cancellationToken);
            var schedules = await db.Schedules.AsNoTracking().OrderBy(item => item.State).ThenBy(item => item.Name).ToListAsync(cancellationToken);
            var states = await db.OccurrenceStates.AsNoTracking().ToListAsync(cancellationToken);
            var goals = await db.Goals.AsNoTracking().ToListAsync(cancellationToken);
            // Match the original goal reader's stable history order, including same-date timestamp ties.
            var allocations = (await db.GoalAllocations.AsNoTracking().ToListAsync(cancellationToken))
                .OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ToList();
            await ValidateBudgetEditorReviewAsync(review, db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new BudgetReadSnapshot(review, entries, schedules, states, goals, allocations);
        }, cancellationToken);
    }
}
