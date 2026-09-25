using Microsoft.EntityFrameworkCore;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Data;

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
public sealed class AutoPostProcessor(IDbContextFactory<FinanceDbContext> contextFactory, FinanceStore finance, PlanStore plans)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Posts every eligible occurrence due on or before <paramref name="today"/>.</summary>
    public async Task<AutoPostResult> RunAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = (await finance.GetAccountsAsync(cancellationToken: cancellationToken)).ToDictionary(a => a.Id);
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

                    var entry = Occurrences.CreateEntry(occurrence, occurrence.Amount!.Value, occurrence.DueDate, ReviewState.Unreviewed);
                    try
                    {
                        if ((await plans.SettleAsync(occurrence, entry, cancellationToken)).Succeeded)
                        {
                            posted++;
                        }
                        else
                        {
                            needsReview++;
                        }
                    }
                    catch (DbUpdateException)
                    {
                        // Another run settled it first – exactly what the unique index is for. Anything else is real.
                        if (!await IsSettledAsync(occurrence, cancellationToken))
                        {
                            throw;
                        }
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
        return await db.Entries.AnyAsync(e => e.ScheduleId == occurrence.Schedule.Id && e.OccurrenceDate == occurrence.OriginalDate, cancellationToken);
    }
}
