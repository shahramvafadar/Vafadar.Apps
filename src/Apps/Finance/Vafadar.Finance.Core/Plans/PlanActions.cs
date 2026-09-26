using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Plans;

/// <summary>Why a plan cannot post automatically (REC-19); the plan then needs manual confirmation.</summary>
public enum AutoPostBlocker
{
    /// <summary>Automatic posting is off.</summary>
    Disabled,

    /// <summary>Only fixed amounts post automatically.</summary>
    AmountNotFixed,

    /// <summary>An account is missing or archived.</summary>
    AccountUnavailable,

    /// <summary>A transfer between currencies has no destination amount.</summary>
    DestinationAmountMissing,

    /// <summary>The plan is paused or ended.</summary>
    NotActive,
}

/// <summary>Plan operations that change the plan itself (REC-15, REC-16).</summary>
public static class PlanActions
{
    /// <summary>
    /// Splits a plan so that changes apply from <paramref name="from"/> on ("this and future"): the existing plan keeps
    /// every earlier occurrence unchanged and ends before it; the returned copy owns <paramref name="from"/> and later
    /// with the same rule anchor, so dates and occurrence numbers continue exactly (AT-25).
    /// </summary>
    public static Schedule SplitFrom(Schedule schedule, DateOnly from)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var next = Copy(schedule);
        next.ActiveFrom = schedule.ActiveFrom is { } start && start > from ? start : from;
        next.PreviousScheduleId = schedule.Id;
        schedule.ActiveUntil = from.AddDays(-1);
        schedule.State = ScheduleState.Ended;
        schedule.AutoPost = false;
        return next;
    }

    /// <summary>Pauses a plan from <paramref name="from"/>; settled occurrences stay as they are (REC-16).</summary>
    public static void Pause(Schedule schedule, DateOnly from)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        schedule.State = ScheduleState.Paused;
        schedule.PausedFrom = from;
    }

    /// <summary>
    /// Resumes a paused plan from <paramref name="resumeFrom"/>. Occurrences during the pause stay out of the plan: the
    /// paused plan ends before the pause and a continuation owns <paramref name="resumeFrom"/> and later.
    /// </summary>
    /// <returns>The continuation plan, or <see langword="null"/> when the plan was not paused.</returns>
    public static Schedule? Resume(Schedule schedule, DateOnly resumeFrom)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (schedule.State != ScheduleState.Paused || schedule.PausedFrom is not { } pausedFrom)
        {
            return null;
        }

        var next = Copy(schedule);
        next.State = ScheduleState.Active;
        next.PausedFrom = null;
        next.ActiveFrom = resumeFrom;
        next.PreviousScheduleId = schedule.Id;
        if (next.AutoPost)
        {
            next.AutoPostFrom = resumeFrom;
        }

        schedule.State = ScheduleState.Ended;
        schedule.PausedFrom = null;
        schedule.ActiveUntil = pausedFrom.AddDays(-1);
        schedule.AutoPost = false;
        return next;
    }

    /// <summary>Ends a plan: no occurrences after <paramref name="lastDate"/>.</summary>
    public static void End(Schedule schedule, DateOnly lastDate)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        schedule.State = ScheduleState.Ended;
        schedule.ActiveUntil = schedule.ActiveUntil is { } until && until < lastDate ? until : lastDate;
        schedule.PausedFrom = null;
        schedule.AutoPost = false;
    }

    /// <summary>Returns why <paramref name="schedule"/> cannot post automatically, or <see langword="null"/> when it can.</summary>
    public static AutoPostBlocker? AutoPostBlockedBy(Schedule schedule, IReadOnlyDictionary<Guid, Account> accounts)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(accounts);
        if (!schedule.AutoPost)
        {
            return AutoPostBlocker.Disabled;
        }

        if (schedule.State != ScheduleState.Active)
        {
            return AutoPostBlocker.NotActive;
        }

        if (schedule.AmountMode != AmountMode.Fixed || schedule.Amount is not > 0)
        {
            return AutoPostBlocker.AmountNotFixed;
        }

        if (!accounts.TryGetValue(schedule.AccountId, out var account) || account.IsArchived)
        {
            return AutoPostBlocker.AccountUnavailable;
        }

        if (schedule.Kind == EntryKind.Transfer)
        {
            if (schedule.ToAccountId is not { } toId || !accounts.TryGetValue(toId, out var destination) || destination.IsArchived)
            {
                return AutoPostBlocker.AccountUnavailable;
            }

            if (!string.Equals(account.CurrencyCode, destination.CurrencyCode, StringComparison.OrdinalIgnoreCase) && schedule.ToAmount is not > 0)
            {
                return AutoPostBlocker.DestinationAmountMissing;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns entries that could settle <paramref name="occurrence"/> instead of creating a second one (REC-17): same
    /// kind and account, not yet linked to a plan, dated within <paramref name="days"/> days of the due date; closest
    /// amount and date first. These are suggestions only – the user decides.
    /// </summary>
    public static IReadOnlyList<LedgerEntry> LinkCandidates(Occurrence occurrence, IEnumerable<LedgerEntry> entries, int days = 10)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(entries);
        var schedule = occurrence.Schedule;
        return
        [
            .. entries
                .Where(e => e.Kind == schedule.Kind && e.AccountId == schedule.AccountId && e.ScheduleId is null
                            && Math.Abs(e.Date.DayNumber - occurrence.DueDate.DayNumber) <= days)
                .OrderBy(e => occurrence.Amount is { } amount ? Math.Abs(e.Amount - amount) : 0)
                .ThenBy(e => Math.Abs(e.Date.DayNumber - occurrence.DueDate.DayNumber)),
        ];
    }

    private static Schedule Copy(Schedule schedule) => new()
    {
        Name = schedule.Name,
        Kind = schedule.Kind,
        AccountId = schedule.AccountId,
        ToAccountId = schedule.ToAccountId,
        CategoryId = schedule.CategoryId,
        AmountMode = schedule.AmountMode,
        Amount = schedule.Amount,
        ToAmount = schedule.ToAmount,
        Note = schedule.Note,
        Icon = schedule.Icon,
        Rule = schedule.Rule.Clone(),
        ActiveFrom = schedule.ActiveFrom,
        ActiveUntil = schedule.ActiveUntil,
        State = schedule.State,
        PausedFrom = schedule.PausedFrom,
        AutoPost = schedule.AutoPost,
        AutoPostFrom = schedule.AutoPostFrom,
        ReminderEnabled = schedule.ReminderEnabled,
        ReminderDaysBefore = schedule.ReminderDaysBefore,
        ReminderTime = schedule.ReminderTime,
        ReminderOnDueDate = schedule.ReminderOnDueDate,
    };
}
