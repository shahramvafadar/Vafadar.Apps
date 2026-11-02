using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Plans;

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

    /// <summary>Gets the sum of partial payments so far (F2-TX-02).</summary>
    public long Paid => State?.PaidAmount ?? 0;

    /// <summary>
    /// Gets what is still to pay: the amount minus partial payments, never negative; <see langword="null"/> when the
    /// amount is unknown. An open occurrence reflects only this outstanding amount (AT-66).
    /// </summary>
    public long? Outstanding => Amount is { } amount ? Math.Max(0, amount - Paid) : null;

    /// <summary>Gets the amount mode that applies: a changed amount of an estimated plan counts as known.</summary>
    public AmountMode AmountMode => State?.Amount is not null ? AmountMode.Fixed : Schedule.AmountMode;
}

/// <summary>Computes occurrences of plans and the entries that settle them (§6, §7).</summary>
public static class Occurrences
{
    // Retain padding for implicit weekend/holiday shifts. Explicit overrides outside
    // this generated window are resolved by original identity below (D-787).
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

        // REC-14 does not limit how far a user can move one due date. Resolve only
        // explicit overrides missed by the bounded generation window, without scanning
        // all earlier/future rule dates or adding an already generated identity twice.
        foreach (var state in byDate.Values)
        {
            if ((state.OriginalDate < searchFrom || state.OriginalDate > searchTo)
                && state.DueDate is { } due && due >= from && due <= to
                && Find(schedule, [state], state.OriginalDate, today) is { } moved)
            {
                result.Add(moved);
            }
        }

        return [.. result.OrderBy(o => o.DueDate).ThenBy(o => o.Number)];
    }

    /// <summary>Resolves one original rule/slice identity, including an effective date moved beyond the range window.</summary>
    public static Occurrence? Find(Schedule schedule, IEnumerable<OccurrenceState> states, DateOnly original, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(states);
        if (!schedule.Owns(original)) return null;
        var scheduled = Recurrence.Between(schedule.Rule, original, original).FirstOrDefault();
        return scheduled.Number == 0 ? null : Create(schedule, scheduled,
            states.FirstOrDefault(s => s.ScheduleId == schedule.Id && s.OriginalDate == original), today);
    }

    /// <summary>
    /// Gets the inclusive lower bound for complete due work. An explicit occurrence
    /// moved before the series starts must remain visible as due or overdue (REC-14).
    /// </summary>
    public static DateOnly DueRangeStart(Schedule schedule, IEnumerable<OccurrenceState> states)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(states);
        var first = schedule.ActiveFrom ?? schedule.Rule.Start;
        foreach (var state in states)
        {
            if (state.ScheduleId == schedule.Id && state.DueDate is { } due && due < first
                && Find(schedule, [state], state.OriginalDate, due) is not null)
            {
                first = due;
            }
        }

        return first;
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
            // A valid final-calendar occurrence must not fail while extending the
            // search horizon. Once MaxValue is searched, no later window exists (D-791).
            var end = window >= DateOnly.MaxValue.DayNumber - from.DayNumber
                ? DateOnly.MaxValue : from.AddDays(window);
            if (Between(schedule, list, from, end, today).FirstOrDefault(o => o.IsOpen) is { } next)
            {
                return next;
            }

            if (end == DateOnly.MaxValue) break;
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
        // A moved occurrence keeps its date; otherwise the weekend rule may shift it (F2-CON-05). Its identity stays the
        // rule date, so reminders, settlements and states still find it.
        var due = state?.DueDate ?? schedule.Rule.ApplyWeekend(scheduled.Date);
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
