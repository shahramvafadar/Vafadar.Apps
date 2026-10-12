using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads original financial inputs in a deferred SQLite reader; no writer slot, money mutation or eligibility filtering is used.</summary>
    public async Task<ReminderFinancialSnapshot> GetReminderFinancialSnapshotAsync(PlanWorkSnapshot plans,
        GoalWorkSnapshot goals, DateOnly day, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plans); ArgumentNullException.ThrowIfNull(goals);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
        if (PlanWorkSnapshot.CaptureInputs(plans.Schedules, plans.States) != plans.InputStamp
            || GoalWorkSnapshot.CaptureInputs(goals.Goals, goals.ContributionPlans, goals.Allocations) != goals.InputStamp)
            throw new InvalidOperationException("A captured original reminder rule/state packet was modified.");
        await PlanStore.ValidateWorkSnapshotAsync(plans, db, cancellationToken);
        await GoalStore.ValidateWorkSnapshotAsync(goals, db, cancellationToken);
        var financials = goals.ContributionPlans.Any(plan => plan.ReminderEnabled);
        var quantities = financials && goals.Goals.Any(goal => goal.Type == GoalType.HoldingQuantity);
        var result = await ReadReminderFinancialsAsync(db, day, financials, quantities, cancellationToken);
        plans.Access!.EnsureSnapshotCurrent(); goals.Access!.EnsureSnapshotCurrent();
        await reader.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>Rechecks all original plan, goal, financial and preference inputs in one coherent SQLite reader before/after native publication.</summary>
    public async Task ValidateReminderPreparationAsync(ReminderFinancialSnapshot financials, PlanWorkSnapshot plans,
        GoalWorkSnapshot goals, string originalSettingsPacket, DateOnly? firstData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(financials); ArgumentNullException.ThrowIfNull(plans); ArgumentNullException.ThrowIfNull(goals);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
        if (financials.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The financial reminder packet belongs to another original file.");
        if (PlanWorkSnapshot.CaptureInputs(plans.Schedules, plans.States) != plans.InputStamp
            || GoalWorkSnapshot.CaptureInputs(goals.Goals, goals.ContributionPlans, goals.Allocations) != goals.InputStamp)
            throw new InvalidOperationException("A captured original reminder rule/state packet was modified.");
        await PlanStore.ValidateWorkSnapshotAsync(plans, db, cancellationToken);
        await GoalStore.ValidateWorkSnapshotAsync(goals, db, cancellationToken);
        if (ReminderFinancialSnapshot.Capture(financials.Accounts, financials.Entries, financials.Events, financials.Types) != financials.InputStamp)
            throw new InvalidOperationException("The captured original financial reminder packet was modified.");
        var current = await ReadReminderFinancialsAsync(db, financials.Day, financials.IncludesGoalFinancials,
            financials.IncludesQuantityFinancials, cancellationToken);
        if (current.InputStamp != financials.InputStamp)
            throw new InvalidOperationException("Original financial reminder inputs changed during preparation.");
        await ValidateReviewReminderInputsAsync(plans, originalSettingsPacket, firstData, db, cancellationToken);
        plans.Access!.EnsureSnapshotCurrent(); goals.Access!.EnsureSnapshotCurrent();
        await reader.CommitAsync(cancellationToken);
    }

    // Only date-bounded original ledger/quantity inputs that the established progress calculator consumes are read.
    // With no contribution reminder, the accounts still govern original plan currency and review first-data date.
    private static async Task<ReminderFinancialSnapshot> ReadReminderFinancialsAsync(ZananceDbContext db, DateOnly day,
        bool financials, bool quantities, CancellationToken cancellationToken) => new(
            DatabaseFileIdentity.ConfiguredPath(db), day, financials, quantities,
            await db.Accounts.AsNoTracking().ToListAsync(cancellationToken),
            financials ? await db.Entries.AsNoTracking().Where(entry => entry.Date <= day).ToListAsync(cancellationToken) : [],
            quantities ? await db.AssetEvents.AsNoTracking().Where(item => item.Date <= day).ToListAsync(cancellationToken) : [],
            quantities ? await db.AssetTypes.AsNoTracking().ToListAsync(cancellationToken) : []);
}
