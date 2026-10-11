using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reopens exactly the reviewed settlement, preserving manual money or the complete generated deletion journal.</summary>
    internal async Task<IReadOnlyList<LedgerEntry>> ReopenOccurrenceAsync(Occurrence reviewed, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var state = await db.OccurrenceStates.FirstOrDefaultAsync(s => s.ScheduleId == reviewed.Schedule.Id
            && s.OriginalDate == reviewed.OriginalDate, cancellationToken);
        if (state?.Status != OccurrenceStatus.Settled || state.EntryId is not { } id || reviewed.State?.EntryId != id)
            throw new InvalidOperationException("The reviewed settlement changed; review it again.");
        var entry = await db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null || entry.ScheduleId != state.ScheduleId || entry.OccurrenceDate != state.OriginalDate || entry.IsPartialPayment)
            throw new InvalidOperationException("The retained payment link changed; review it again.");

        if (entry.Source == EntrySource.Schedule)
        {
            // D-135: the established deletion pipeline owns groups, refund links, paid totals and the exact Undo journal.
            // It already reopens states atomically; never reacquire a second writer after its committed Changed event.
            var deleted = await DeleteEntriesUnderWriterAsync(db, write, [id], cancellationToken);
            if (deleted.Count == 0)
                throw new InvalidOperationException("The settlement belongs to a holding; correct its holding event instead.");
            return deleted;
        }

        // A manual/imported payment remains actual money. Only its checked relationship is released with the state.
        await db.Entries.Where(e => e.Id == id).ExecuteUpdateAsync(e => e
            .SetProperty(x => x.ScheduleId, (Guid?)null).SetProperty(x => x.OccurrenceDate, (DateOnly?)null), cancellationToken);
        state.Status = OccurrenceStatus.Open;
        state.EntryId = null;
        state.AutoPostSuppressed = true;
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        OnChanged();
        return [];
    }
}
