using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Plans;

/// <summary>Status of an occurrence as shown to the user (REC-13).</summary>
public enum OccurrenceView
{
    /// <summary>Open, due after today.</summary>
    Future = 0,

    /// <summary>Open, due today.</summary>
    Due = 1,

    /// <summary>Open, due date passed.</summary>
    Overdue = 2,

    /// <summary>Settled by an entry.</summary>
    Settled = 3,

    /// <summary>Skipped.</summary>
    Skipped = 4,
}

/// <summary>An occurrence of a plan with its effective values.</summary>
/// <param name="Schedule">The plan.</param>
/// <param name="Number">Position in the series (1-based).</param>
/// <param name="OriginalDate">Date computed by the rule – the identity of the occurrence.</param>
/// <param name="DueDate">Effective due date (moved or original).</param>
/// <param name="Amount">Effective amount; <see langword="null"/> when unknown.</param>
/// <param name="Status">Derived status.</param>
/// <param name="State">The stored state row, if any.</param>
public sealed record Occurrence(
    Schedule Schedule,
    int Number,
    DateOnly OriginalDate,
    DateOnly DueDate,
    long? Amount,
    OccurrenceView Status,
    OccurrenceState? State)
{
    /// <summary>Gets a value indicating whether the occurrence still needs to be settled or skipped.</summary>
    public bool IsOpen => Status is OccurrenceView.Future or OccurrenceView.Due or OccurrenceView.Overdue;

    /// <summary>Gets the amount mode that applies: a changed amount of an estimated plan counts as known.</summary>
    public AmountMode AmountMode => State?.Amount is not null ? AmountMode.Fixed : Schedule.AmountMode;
}

/// <summary>Computes occurrences of plans and the entries that settle them (§6, §7).</summary>
public static class Occurrences
{
    // Moved due dates may lie far from their original date; look this far back for originals whose due date moved
    // into the requested range.
    private const int MoveWindowDays = 400;

    /// <summary>Returns the occurrences whose effective due date lies in [<paramref name="from"/>, <paramref name="to"/>].</summary>
    public static IReadOnlyList<Occurrence> Between(
        Schedule schedule,
        IEnumerable<OccurrenceState> states,
        DateOnly from,
        DateOnly to,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(states);

        var byDate = states.Where(s => s.ScheduleId == schedule.Id).ToDictionary(s => s.OriginalDate);
        var searchFrom = from.DayNumber - MoveWindowDays > DateOnly.MinValue.DayNumber ? from.AddDays(-MoveWindowDays) : DateOnly.MinValue;
        var searchTo = to.DayNumber + MoveWindowDays < DateOnly.MaxValue.DayNumber ? to.AddDays(MoveWindowDays) : DateOnly.MaxValue;

        var result = new List<Occurrence>();
        foreach (var scheduled in Recurrence.Between(schedule.Rule, searchFrom, searchTo))
        {
            if (!schedule.Owns(scheduled.Date))
            {
                continue;
            }

            byDate.TryGetValue(scheduled.Date, out var state);
            var occurrence = Create(schedule, scheduled, state, today);
            if (occurrence.DueDate >= from && occurrence.DueDate <= to)
            {
                result.Add(occurrence);
            }
        }

        return [.. result.OrderBy(o => o.DueDate).ThenBy(o => o.Number)];
    }

    /// <summary>Returns the open occurrences due on or before <paramref name="today"/>, oldest first.</summary>
    public static IReadOnlyList<Occurrence> OpenUpTo(Schedule schedule, IEnumerable<OccurrenceState> states, DateOnly today, DateOnly since) =>
        [.. Between(schedule, states, since, today, today).Where(o => o.IsOpen)];

    /// <summary>Returns the next open occurrence on or after <paramref name="from"/>, if any.</summary>
    public static Occurrence? NextOpen(Schedule schedule, IEnumerable<OccurrenceState> states, DateOnly from, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var list = states.ToList();
        for (var window = 62; window <= 62 * 64; window *= 4)
        {
            if (Between(schedule, list, from, from.AddDays(window), today).FirstOrDefault(o => o.IsOpen) is { } next)
            {
                return next;
            }
        }

        return null;
    }

    /// <summary>Creates the entry that settles <paramref name="occurrence"/> with the plan's values.</summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <param name="amount">The actual amount (minor units).</param>
    /// <param name="date">The actual payment date – may differ from the due date (FIN-07, AT-28).</param>
    /// <param name="review">Confirmed when the user settles it, unreviewed for automatic posting.</param>
    public static LedgerEntry CreateEntry(Occurrence occurrence, long amount, DateOnly date, ReviewState review)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        var schedule = occurrence.Schedule;
        return new LedgerEntry
        {
            Kind = schedule.Kind,
            Date = date,
            AccountId = schedule.AccountId,
            Amount = amount,
            ToAccountId = schedule.Kind == EntryKind.Transfer ? schedule.ToAccountId : null,
            ToAmount = schedule.Kind == EntryKind.Transfer ? schedule.ToAmount : null,
            CategoryId = schedule.Kind == EntryKind.Transfer ? null : schedule.CategoryId,
            Title = schedule.Name,
            Icon = schedule.Icon,
            Review = review,
            Source = EntrySource.Schedule,
            ScheduleId = schedule.Id,
            OccurrenceDate = occurrence.OriginalDate,
        };
    }

    private static Occurrence Create(Schedule schedule, ScheduledDate scheduled, OccurrenceState? state, DateOnly today)
    {
        var due = state?.DueDate ?? scheduled.Date;
        var amount = state?.Amount ?? (schedule.AmountMode == AmountMode.Unknown ? null : schedule.Amount);
        var status = state?.Status switch
        {
            OccurrenceStatus.Settled => OccurrenceView.Settled,
            OccurrenceStatus.Skipped => OccurrenceView.Skipped,
            _ when due < today => OccurrenceView.Overdue,
            _ when due == today => OccurrenceView.Due,
            _ => OccurrenceView.Future,
        };

        return new Occurrence(schedule, scheduled.Number, scheduled.Date, due, amount, status, state);
    }
}
