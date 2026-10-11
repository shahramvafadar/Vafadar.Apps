using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>The result of an automatic posting run.</summary>
/// <param name="Posted">Number of entries recorded.</param>
/// <param name="NeedsReview">Open occurrences due up to today that the user still has to settle.</param>
public sealed record AutoPostResult(int Posted, int NeedsReview);

/// <summary>
/// Records due occurrences of plans with automatic posting as unreviewed entries (docs/02-domain-design.md §7). Runs on
/// start, resume and after a restore; it is idempotent – running it twice or concurrently cannot create a second entry
/// for an occurrence, because the database allows one settlement per occurrence (D-07, REC-21, AT-30). Correctness
/// never depends on background execution (REC-22).
/// </summary>
public sealed class AutoPostProcessor(IDbContextFactory<ZananceDbContext> contextFactory, ZananceStore store, PlanStore plans)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Runs <paramref name="work"/> while no posting run can start, e.g. a restore that replaces the database: a run that
    /// read the old data must not write into the restored one.
    /// </summary>
    public async Task RunExclusiveAsync(Func<Task> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await work();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Posts every eligible occurrence due on or before <paramref name="today"/>.</summary>
    public async Task<AutoPostResult> RunAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // An entry saved without its state (interrupted settle) must not look open, or it would be posted again.
            await plans.RepairSettlementsAsync(cancellationToken);
            var accounts = (await store.GetAccountsAsync(cancellationToken: cancellationToken)).ToDictionary(a => a.Id);
            var schedules = await plans.GetSchedulesAsync(cancellationToken);
            var states = await plans.GetStatesAsync(cancellationToken: cancellationToken);
            var posted = 0;
            var needsReview = 0;

            foreach (var schedule in schedules.Where(s => s.State == ScheduleState.Active))
            {
                var since = schedule.ActiveFrom ?? schedule.Rule.Start;
                var open = Occurrences.OpenUpTo(schedule, states, today, since);
                var canPost = PlanActions.AutoPostBlockedBy(schedule, accounts) is null;
                foreach (var occurrence in open)
                {
                    var eligible = canPost
                        && occurrence.State?.AutoPostSuppressed != true
                        && (schedule.AutoPostFrom is not { } autoFrom || occurrence.OriginalDate >= autoFrom)
                        && occurrence.Amount is > 0;

                    if (!eligible)
                    {
                        needsReview++;
                        continue;
                    }

                    try
                    {
                        // D-138: cached discovery never supplies the amount/date or authorizes the actual money write.
                        var result = await plans.TryPostAutomaticallyAsync(schedule.Id, occurrence.OriginalDate, today, cancellationToken);
                        if (result == AutomaticPostStatus.Posted) posted++;
                        else if (result == AutomaticPostStatus.NeedsReview) needsReview++;
                    }
                    catch (DbUpdateException)
                    {
                        // Another run settled it first – exactly what the unique index is for. Anything else is real.
                        if (!await IsSettledAsync(occurrence, cancellationToken))
                        {
                            throw;
                        }

                        // The entry exists; make sure its state says so too.
                        await plans.RepairSettlementsAsync(cancellationToken);
                    }
                }
            }

            return new AutoPostResult(posted, needsReview);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> IsSettledAsync(Occurrence occurrence, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Entries.AnyAsync(e => e.ScheduleId == occurrence.Schedule.Id && e.OccurrenceDate == occurrence.OriginalDate && !e.IsPartialPayment, cancellationToken);
    }
}
