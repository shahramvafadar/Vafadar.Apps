using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>
/// Data access for plans and their occurrences (docs/02-domain-design.md §5–§7). Settling, linking and undoing keep the
/// entry and the occurrence state consistent in one transaction.
/// </summary>
public sealed class PlanStore(IDbContextFactory<ZananceDbContext> contextFactory, ZananceStore store,
    ICommercialWriteAccessSource commercialAccess)
{
    /// <summary>Raised after plans or occurrence states were written.</summary>
    public event EventHandler? Changed;

    /// <summary>Returns all plans, active first.</summary>
    public async Task<List<Schedule>> GetSchedulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Schedules.AsNoTracking().OrderBy(s => s.State).ThenBy(s => s.Name).ToListAsync(cancellationToken);
    }

    /// <summary>Returns one plan.</summary>
    public async Task<Schedule?> GetScheduleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>Returns the stored occurrence states, optionally of one plan.</summary>
    public async Task<List<OccurrenceState>> GetStatesAsync(Guid? scheduleId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.OccurrenceStates.AsNoTracking();
        if (scheduleId is { } id)
        {
            query = query.Where(s => s.ScheduleId == id);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Inserts or updates plans in one transaction.</summary>
    public async Task SaveSchedulesAsync(IEnumerable<Schedule> schedules, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var batch = schedules.ToList();
        await CheckCommercialSaveAsync(db, write, batch, null, cancellationToken);
        foreach (var schedule in batch)
        {
            if (await db.Schedules.AnyAsync(s => s.Id == schedule.Id, cancellationToken))
            {
                db.Schedules.Update(schedule);
            }
            else
            {
                db.Schedules.Add(schedule);
            }
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Inserts or updates one plan.</summary>
    public Task SaveScheduleAsync(Schedule schedule, CancellationToken cancellationToken = default) =>
        SaveSchedulesAsync([schedule], cancellationToken);

    /// <summary>
    /// Saves a "this and future" split (<see cref="PlanActions.SplitFrom"/>) or a resume (<see cref="PlanActions.Resume"/>):
    /// occurrences from the split date on – their
    /// states and settling entries – move to the new plan, everything earlier stays with the old one.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> (nothing saved) when an occurrence recorded on or after the split date – settled, skipped,
    /// moved, overridden or partly paid – is not a date of the new rule: moving it would orphan the record, and the new
    /// plan would post that period again.
    /// </returns>
    public async Task<bool> SaveSplitAsync(Schedule previous, Schedule next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        var from = next.ActiveFrom ?? next.Rule.Start;

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var recorded = (await db.OccurrenceStates.Where(s => s.ScheduleId == previous.Id && s.OriginalDate >= from).Select(s => s.OriginalDate).ToListAsync(cancellationToken))
            .Concat(await db.Entries.Where(e => e.ScheduleId == previous.Id && e.OccurrenceDate >= from).Select(e => e.OccurrenceDate!.Value).ToListAsync(cancellationToken))
            .Distinct().ToList();
        if (recorded.Count > 0)
        {
            var newDates = Recurrence.Between(next.Rule, from, recorded.Max()).Select(d => d.Date).ToHashSet();
            if (!recorded.All(newDates.Contains))
            {
                return false;
            }
        }

        await CheckCommercialSaveAsync(db, write, [previous, next], previous.Id, cancellationToken);
        db.Schedules.Update(previous);
        db.Schedules.Add(next);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);

        await db.OccurrenceStates.Where(s => s.ScheduleId == previous.Id && s.OriginalDate >= from)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ScheduleId, next.Id), cancellationToken);
        await db.Entries.Where(e => e.ScheduleId == previous.Id && e.OccurrenceDate >= from)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.ScheduleId, next.Id), cancellationToken);
        write.EnsureCurrent();
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
        return true;
    }

    /// <summary>Checks a whole save against transaction-local identities, states and net capacity before writes.</summary>
    private static async Task CheckCommercialSaveAsync(ZananceDbContext db, CommercialWriteTransaction write,
        IReadOnlyList<Schedule> batch, Guid? continuationOf, CancellationToken cancellationToken)
    {
        if (!write.Enforced) return;
        if (batch.Any(s => s is null || !Enum.IsDefined(s.State)) || batch.Select(s => s.Id).Distinct().Count() != batch.Count)
            throw new ArgumentException("A plan batch requires distinct ids and known states.", nameof(batch));
        var ids = batch.Select(s => s.Id).ToArray();
        var existing = await db.Schedules.AsNoTracking().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        Schedule? continued = null;
        if (continuationOf is { } prior)
        {
            if (batch.Count != 2 || !existing.TryGetValue(prior, out continued)
                || batch[0].Id != prior || batch[0].State != ScheduleState.Ended
                || existing.ContainsKey(batch[1].Id) || batch[1].PreviousScheduleId != prior)
                throw new ArgumentException("A split requires an existing ended slice and a distinct linked continuation.", nameof(batch));
        }
        foreach (var schedule in batch)
        {
            existing.TryGetValue(schedule.Id, out var original);
            // D-120: a verified split/resume transfers the old slot, not a second plan. Compare its advanced tools
            // with the stored predecessor, never the already-mutated Ended object supplied by the UI.
            if (original is null && continued?.State is ScheduleState.Active or ScheduleState.Paused
                && schedule.PreviousScheduleId == continued.Id) original = continued;
            var counted = schedule.State is ScheduleState.Active or ScheduleState.Paused;
            var newUse = original is null || (counted && original.State == ScheduleState.Ended);
            var addsAdvanced = (newUse || counted) && AddsAdvancedTools(schedule, newUse ? null : original);
            write.DemandFeature(addsAdvanced ? CommercialFeature.AdvancedPlans
                : newUse ? CommercialFeature.BasicPlans : CommercialFeature.Corrections);
        }
        if (batch.Count == 0) { write.DemandFeature(CommercialFeature.Corrections); return; }
        var oldSlots = existing.Values.Count(s => s.State is ScheduleState.Active or ScheduleState.Paused);
        var newSlots = batch.Count(s => s.State is ScheduleState.Active or ScheduleState.Paused);
        // Ending an old slice and creating its continuation is one slot, regardless of input order. Corrections
        // above quota remain possible; a batch that grows capacity must fit in its entirety or save nothing.
        if (newSlots <= oldSlots) return;
        if (write.DemandCapacity(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans) is not { } maximum) return;
        var current = await db.Schedules.CountAsync(s => s.State == ScheduleState.Active || s.State == ScheduleState.Paused, cancellationToken);
        write.DemandCount(CommercialFeature.BasicPlans, QuotaKind.RecurringPlans, maximum, current, newSlots - oldSlots);
    }

    /// <summary>Detects newly enabled paid tools while retained tool corrections remain available after expiry.</summary>
    private static bool AddsAdvancedTools(Schedule target, Schedule? original)
    {
        static bool Weekday(Schedule s) => s.Rule.Frequency is Frequency.Monthly or Frequency.Yearly && s.Rule.DayRule.IsWeekday();
        static bool SecondDay(Schedule s) => s.Rule.Frequency == Frequency.Monthly && s.Rule.SecondDay is not null;
        static bool Shift(Schedule s) => s.Rule.WeekendShift != WeekendShift.None;
        static bool Contract(Schedule s) => s.HasContract || s.ContractReference is not null || s.ContractRenews;
        return (target.AutoPost && original?.AutoPost != true)
            || (Weekday(target) && (original is null || !Weekday(original)))
            || (SecondDay(target) && (original is null || !SecondDay(original)))
            || (Shift(target) && (original is null || !Shift(original)))
            || (Contract(target) && (original is null || !Contract(original)));
    }

    /// <summary>Returns whether any occurrence of the plan was settled or skipped.</summary>
    public async Task<bool> HasHistoryAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OccurrenceStates.AnyAsync(s => s.ScheduleId == scheduleId && s.Status != OccurrenceStatus.Open, cancellationToken)
            || await db.Entries.AnyAsync(e => e.ScheduleId == scheduleId, cancellationToken);
    }

    /// <summary>Deletes a plan without history; plans with settled occurrences can only be ended.</summary>
    /// <returns><see langword="false"/> when the plan has history.</returns>
    public async Task<bool> DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        if (await HasHistoryAsync(scheduleId, cancellationToken))
        {
            return false;
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Checked again inside the transaction: an entry settled meanwhile keeps the plan.
        if (await db.Entries.AnyAsync(e => e.ScheduleId == scheduleId, cancellationToken)
            || await db.OccurrenceStates.AnyAsync(s => s.ScheduleId == scheduleId && s.Status != OccurrenceStatus.Open, cancellationToken))
        {
            return false;
        }

        await db.OccurrenceStates.Where(s => s.ScheduleId == scheduleId).ExecuteDeleteAsync(cancellationToken);
        await db.Schedules.Where(s => s.Id == scheduleId).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
        return true;
    }

    /// <summary>
    /// Settles an occurrence with a new entry (confirm with the actual amount and date). The entry is validated like any
    /// other; the unique index guarantees one settlement per occurrence (D-07).
    /// </summary>
    public async Task<SaveResult> SettleAsync(Occurrence occurrence, LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(entry);
        entry.ScheduleId = occurrence.Schedule.Id;
        entry.OccurrenceDate = occurrence.OriginalDate;

        var result = await store.SaveEntryAsync(entry, cancellationToken);
        if (result.Succeeded)
        {
            // The entry and the state are two writes; if the second one is lost (app killed, disk error), the next
            // RepairSettlementsAsync restores it from the entry, so the occurrence can never stay open next to its entry.
            await UpdateStateAsync(occurrence, state =>
            {
                state.Status = OccurrenceStatus.Settled;
                state.EntryId = entry.Id;
            }, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Makes every occurrence that has a settling entry (not a partial payment) settled with that entry. Idempotent; run
    /// before automatic posting. Reopening an occurrence deletes or unlinks its entry first, so it is never undone here.
    /// </summary>
    /// <returns>The number of states repaired.</returns>
    public async Task<int> RepairSettlementsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settling = await db.Entries.AsNoTracking()
            .Where(e => e.ScheduleId != null && e.OccurrenceDate != null && !e.IsPartialPayment)
            .Select(e => new { e.Id, ScheduleId = e.ScheduleId!.Value, Date = e.OccurrenceDate!.Value })
            .ToListAsync(cancellationToken);
        if (settling.Count == 0)
        {
            return 0;
        }

        var states = (await db.OccurrenceStates.ToListAsync(cancellationToken)).ToDictionary(s => (s.ScheduleId, s.OriginalDate));
        var repaired = 0;
        foreach (var e in settling)
        {
            if (!states.TryGetValue((e.ScheduleId, e.Date), out var state))
            {
                state = new OccurrenceState { ScheduleId = e.ScheduleId, OriginalDate = e.Date };
                db.OccurrenceStates.Add(state);
                states[(e.ScheduleId, e.Date)] = state;
            }

            if (state.Status != OccurrenceStatus.Settled || state.EntryId != e.Id)
            {
                state.Status = OccurrenceStatus.Settled;
                state.EntryId = e.Id;
                repaired++;
            }
        }

        if (repaired > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            OnChanged();
        }

        return repaired;
    }

    /// <summary>
    /// Records a partial payment of an occurrence (F2-TX-02). The occurrence stays open with the outstanding rest; a
    /// payment that reaches or exceeds the rest settles it instead, so the final payment is an ordinary settlement and an
    /// overpayment is visible as a larger actual amount.
    /// </summary>
    public async Task<SaveResult> PayPartAsync(Occurrence occurrence, LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(entry);
        if (occurrence.Outstanding is { } outstanding && entry.Amount >= outstanding)
        {
            entry.IsPartialPayment = false;
            return await SettleAsync(occurrence, entry, cancellationToken);
        }

        entry.ScheduleId = occurrence.Schedule.Id;
        entry.OccurrenceDate = occurrence.OriginalDate;
        entry.IsPartialPayment = true;
        var result = await store.SaveEntryAsync(entry, cancellationToken);
        if (result.Succeeded)
        {
            OnChanged();
        }

        return result;
    }

    /// <summary>Returns the partial payments of an occurrence, oldest first.</summary>
    public async Task<List<LedgerEntry>> GetPartialPaymentsAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var list = await db.Entries.AsNoTracking()
            .Where(e => e.ScheduleId == occurrence.Schedule.Id && e.OccurrenceDate == occurrence.OriginalDate && e.IsPartialPayment)
            .ToListAsync(cancellationToken);
        return [.. list.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)];
    }

    /// <summary>Settles an occurrence with an existing entry instead of creating a second one (REC-17, AT-29).</summary>
    public async Task LinkAsync(Occurrence occurrence, Guid entryId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(
                e => e.SetProperty(x => x.ScheduleId, occurrence.Schedule.Id).SetProperty(x => x.OccurrenceDate, occurrence.OriginalDate),
                cancellationToken);
        }

        await UpdateStateAsync(occurrence, state =>
        {
            state.Status = OccurrenceStatus.Settled;
            state.EntryId = entryId;
        }, cancellationToken);
    }

    /// <summary>
    /// Reopens a settled occurrence. An entry created by the plan is deleted; a linked entry that existed before is kept
    /// and only unlinked. Automatic posting will not recreate it (REC-18, AT-31).
    /// </summary>
    /// <returns>The deleted entries (for undo).</returns>
    public async Task<IReadOnlyList<LedgerEntry>> UnsettleAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        IReadOnlyList<LedgerEntry> deleted = [];
        Func<ZananceDbContext, Task>? unlink = null;
        if (occurrence.State?.EntryId is { } entryId && await store.GetEntryAsync(entryId, cancellationToken) is { } entry)
        {
            if (entry.Source == EntrySource.Schedule)
            {
                deleted = await store.DeleteEntryAsync(entryId, cancellationToken);
            }
            else
            {
                // An entry the user recorded stays; it only loses its link, together with the reopened occurrence.
                unlink = db => db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(
                    e => e.SetProperty(x => x.ScheduleId, (Guid?)null).SetProperty(x => x.OccurrenceDate, (DateOnly?)null),
                    cancellationToken);
            }
        }

        await UpdateStateAsync(occurrence, state =>
        {
            state.Status = OccurrenceStatus.Open;
            state.EntryId = null;
            state.AutoPostSuppressed = true;
        }, cancellationToken, unlink);
        return deleted;
    }

    /// <summary>Skips an occurrence (REC-14); the series does not get an extra occurrence (AT-27).</summary>
    public Task SkipAsync(Occurrence occurrence, CancellationToken cancellationToken = default) =>
        UpdateStateAsync(occurrence, state => state.Status = OccurrenceStatus.Skipped, cancellationToken);

    /// <summary>Undoes a skip.</summary>
    public Task UnskipAsync(Occurrence occurrence, CancellationToken cancellationToken = default) =>
        UpdateStateAsync(occurrence, state => state.Status = OccurrenceStatus.Open, cancellationToken);

    /// <summary>Changes the due date, amount or note of this occurrence only (AT-26).</summary>
    public Task ChangeOccurrenceAsync(Occurrence occurrence, DateOnly? dueDate, long? amount, string? note, CancellationToken cancellationToken = default) =>
        UpdateStateAsync(occurrence, state =>
        {
            state.DueDate = dueDate == occurrence.OriginalDate ? null : dueDate;
            state.Amount = amount;
            state.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }, cancellationToken);

    // alsoInTransaction runs in the same transaction as the state change: both are saved or neither.
    private async Task UpdateStateAsync(Occurrence occurrence, Action<OccurrenceState> change, CancellationToken cancellationToken, Func<ZananceDbContext, Task>? alsoInTransaction = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (alsoInTransaction is not null)
        {
            await alsoInTransaction(db);
        }

        var state = await db.OccurrenceStates.FirstOrDefaultAsync(
            s => s.ScheduleId == occurrence.Schedule.Id && s.OriginalDate == occurrence.OriginalDate, cancellationToken);
        if (state is null)
        {
            state = new OccurrenceState { ScheduleId = occurrence.Schedule.Id, OriginalDate = occurrence.OriginalDate };
            db.OccurrenceStates.Add(state);
        }

        change(state);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
