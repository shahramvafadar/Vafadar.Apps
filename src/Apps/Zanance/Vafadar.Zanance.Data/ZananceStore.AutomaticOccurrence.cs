using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Rebuilds automatic money from actual plan/state/accounts under the same writer as the ledger Save.</summary>
    internal async Task<AutomaticPostStatus> PostAutomaticOccurrenceAsync(Guid scheduleId, DateOnly originalDate,
        DateOnly today, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var plan = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (plan is null || plan.State != ScheduleState.Active || !plan.Owns(originalDate)
            || !Recurrence.Between(plan.Rule, originalDate, originalDate).Any()) return AutomaticPostStatus.NoLongerDue;
        var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId
            && s.OriginalDate == originalDate, cancellationToken);
        if (state?.Status is OccurrenceStatus.Settled or OccurrenceStatus.Skipped
            || await db.Entries.AnyAsync(e => e.ScheduleId == scheduleId && e.OccurrenceDate == originalDate
                && !e.IsPartialPayment, cancellationToken)) return AutomaticPostStatus.NoLongerDue;
        state ??= new OccurrenceState { ScheduleId = scheduleId, OriginalDate = originalDate };
        var due = state.DueDate ?? plan.Rule.ApplyWeekend(originalDate);
        var actual = Occurrences.Between(plan, [state], due, due, today).Single(o => o.OriginalDate == originalDate);
        if (!actual.IsOpen || actual.DueDate > today) return AutomaticPostStatus.NoLongerDue;
        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        // D-138: the discovery snapshot may predate a manual payment, an override or a changed plan/account.
        // Even a legacy state with an incorrect paid cache must not post another full amount over real partial money.
        var hasPartial = await db.Entries.AnyAsync(e => e.ScheduleId == scheduleId && e.OccurrenceDate == originalDate
            && e.IsPartialPayment, cancellationToken);
        if (PlanActions.AutoPostBlockedBy(plan, accounts) is not null || state.AutoPostSuppressed || hasPartial
            || (plan.AutoPostFrom is { } enabledFrom && originalDate < enabledFrom) || actual.Amount is not > 0)
            return AutomaticPostStatus.NeedsReview;
        try
        {
            var schedules = write.Enforced ? await db.Schedules.AsNoTracking().ToListAsync(cancellationToken) : [];
            write.DemandSelectedPlan(CommercialFeature.BasicPlans, schedules, scheduleId);
            write.DemandFeature(CommercialFeature.AdvancedPlans);
            var entry = Occurrences.CreateEntry(actual, actual.Amount.Value, actual.DueDate, ReviewState.Unreviewed);
            state.Status = OccurrenceStatus.Settled; state.EntryId = entry.Id;
            var result = await SaveEntriesUnderWriterAsync(db, write, [entry], [], null, new HashSet<Guid>(), cancellationToken, state);
            return result.Succeeded ? AutomaticPostStatus.Posted : AutomaticPostStatus.NeedsReview;
        }
        catch (CommercialWriteRejectedException)
        {
            // Missing selection/paid capabilities never turn retained automatic plans into new financial work.
            // The transaction rolls back even if a later ledger check rejected the source/destination choice.
            return AutomaticPostStatus.NeedsReview;
        }
    }
}
