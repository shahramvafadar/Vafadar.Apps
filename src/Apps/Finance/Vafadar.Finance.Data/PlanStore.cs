using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Data;

/// <summary>
/// Data access for plans and their occurrences (docs/02-domain-design.md §5–§7). Settling, linking and undoing keep the
/// entry and the occurrence state consistent in one transaction.
/// </summary>
public sealed class PlanStore(IDbContextFactory<FinanceDbContext> contextFactory, FinanceStore finance)
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
        foreach (var schedule in schedules)
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

        await db.SaveChangesAsync(cancellationToken);

        OnChanged();
    }

    /// <summary>Inserts or updates one plan.</summary>
    public Task SaveScheduleAsync(Schedule schedule, CancellationToken cancellationToken = default) =>
        SaveSchedulesAsync([schedule], cancellationToken);

    /// <summary>
    /// Saves a "this and future" split (<see cref="PlanActions.SplitFrom"/>): occurrences from the split date on – their
    /// states and settling entries – move to the new plan, everything earlier stays with the old one.
    /// </summary>
    public async Task SaveSplitAsync(Schedule previous, Schedule next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        var from = next.ActiveFrom ?? next.Rule.Start;

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Schedules.Update(previous);
        db.Schedules.Add(next);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();

        await db.OccurrenceStates.Where(s => s.ScheduleId == previous.Id && s.OriginalDate >= from)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ScheduleId, next.Id), cancellationToken);
        await db.Entries.Where(e => e.ScheduleId == previous.Id && e.OccurrenceDate >= from)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.ScheduleId, next.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        OnChanged();
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
        await db.OccurrenceStates.Where(s => s.ScheduleId == scheduleId).ExecuteDeleteAsync(cancellationToken);
        await db.Schedules.Where(s => s.Id == scheduleId).ExecuteDeleteAsync(cancellationToken);
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

        var result = await finance.SaveEntryAsync(entry, cancellationToken);
        if (result.Succeeded)
        {
            await UpdateStateAsync(occurrence, state =>
            {
                state.Status = OccurrenceStatus.Settled;
                state.EntryId = entry.Id;
            }, cancellationToken);
        }

        return result;
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
        if (occurrence.State?.EntryId is { } entryId && await finance.GetEntryAsync(entryId, cancellationToken) is { } entry)
        {
            if (entry.Source == EntrySource.Schedule)
            {
                deleted = await finance.DeleteEntryAsync(entryId, cancellationToken);
            }
            else
            {
                await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
                await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(
                    e => e.SetProperty(x => x.ScheduleId, (Guid?)null).SetProperty(x => x.OccurrenceDate, (DateOnly?)null),
                    cancellationToken);
            }
        }

        await UpdateStateAsync(occurrence, state =>
        {
            state.Status = OccurrenceStatus.Open;
            state.EntryId = null;
            state.AutoPostSuppressed = true;
        }, cancellationToken);
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

    private async Task UpdateStateAsync(Occurrence occurrence, Action<OccurrenceState> change, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var state = await db.OccurrenceStates.FirstOrDefaultAsync(
            s => s.ScheduleId == occurrence.Schedule.Id && s.OriginalDate == occurrence.OriginalDate, cancellationToken);
        if (state is null)
        {
            state = new OccurrenceState { ScheduleId = occurrence.Schedule.Id, OriginalDate = occurrence.OriginalDate };
            db.OccurrenceStates.Add(state);
        }

        change(state);
        await db.SaveChangesAsync(cancellationToken);
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
