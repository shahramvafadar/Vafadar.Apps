using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Saves new occurrence money and its state through the established ledger pipeline in one actual writer.</summary>
    internal async Task<SaveResult> SaveOccurrencePaymentAsync(Occurrence reviewed, LedgerEntry entry, bool partial,
        CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var plan = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == reviewed.Schedule.Id, cancellationToken);
        if (plan is null || !plan.Owns(reviewed.OriginalDate)
            || !Recurrence.Between(plan.Rule, reviewed.OriginalDate, reviewed.OriginalDate).Any()
            || entry.Kind != plan.Kind || entry.AccountId != plan.AccountId
            || (entry.Kind == EntryKind.Transfer && entry.ToAccountId != plan.ToAccountId)
            || await db.Entries.AnyAsync(e => e.Id == entry.Id, cancellationToken))
            throw new InvalidOperationException("The payment or occurrence changed; review it again.");

        var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == plan.Id
            && s.OriginalDate == reviewed.OriginalDate, cancellationToken);
        // Preserve the duplicate-settlement contract used by concurrent automatic posting, without a second money write.
        if (state?.Status == OccurrenceStatus.Settled || await db.Entries.AnyAsync(e => e.ScheduleId == plan.Id
            && e.OccurrenceDate == reviewed.OriginalDate && !e.IsPartialPayment, cancellationToken))
            throw new DbUpdateException("The occurrence already has a settlement.");
        if (state?.Status == OccurrenceStatus.Skipped)
            throw new InvalidOperationException("The occurrence was skipped; review it again.");

        state ??= new OccurrenceState { ScheduleId = plan.Id, OriginalDate = reviewed.OriginalDate };
        // Read actual money rather than a stale form or derived cache. Reuse Core's effective amount and due-date rules.
        var payments = await db.Entries.AsNoTracking().Where(e => e.ScheduleId == plan.Id
            && e.OccurrenceDate == reviewed.OriginalDate && e.IsPartialPayment).ToListAsync(cancellationToken);
        var paid = payments.Sum(e => (decimal)e.Amount);
        if (paid > long.MaxValue) throw new InvalidOperationException("The partial payment total is outside the supported range.");
        state.PaidAmount = (long)paid;
        var due = state.DueDate ?? plan.Rule.ApplyWeekend(reviewed.OriginalDate);
        var actual = Occurrences.Between(plan, [state], due, due, DateOnly.FromDateTime(time.GetLocalNow().DateTime))
            .Single(o => o.OriginalDate == reviewed.OriginalDate);
        entry.ScheduleId = plan.Id;
        entry.OccurrenceDate = reviewed.OriginalDate;
        entry.IsPartialPayment = partial && !(actual.Outstanding is { } rest && entry.Amount >= rest);
        if (!entry.IsPartialPayment)
        {
            state.Status = OccurrenceStatus.Settled;
            state.EntryId = entry.Id;
        }
        // A partial save creates/reuses its state in UpdatePaidAmountsAsync. A full/final save publishes after that step.
        return await SaveEntriesUnderWriterAsync(db, write, [entry], [], null, new HashSet<Guid>(), cancellationToken,
            entry.IsPartialPayment ? null : state);
    }
}
