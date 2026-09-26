using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Reminders;

/// <summary>Which contract date a reminder is about.</summary>
public enum ContractDateKind
{
    /// <summary>The last day to cancel.</summary>
    Cancellation,

    /// <summary>A date to review the contract.</summary>
    Review,
}

/// <summary>A reminder about a contract date of a plan (F2-CON-01).</summary>
/// <param name="Id">Stable notification id.</param>
/// <param name="NotifyAt">Local time of the notification.</param>
/// <param name="Schedule">The plan.</param>
/// <param name="Kind">Which date.</param>
/// <param name="Date">The contract date itself.</param>
public sealed record ContractReminder(int Id, DateTime NotifyAt, Schedule Schedule, ContractDateKind Kind, DateOnly Date);

/// <summary>
/// Reminders for cancellation deadlines and review dates. A cancellation deadline is reminded well ahead and again on the
/// day; the app never marks a contract as cancelled because a reminder was seen or tapped (F2-CON-03).
/// </summary>
public static class ContractReminderPlanner
{
    /// <summary>How many days before a cancellation deadline the first reminder fires.</summary>
    public const int CancellationLeadDays = 14;

    /// <summary>Returns the future contract reminders within the reminder horizon, soonest first.</summary>
    public static IReadOnlyList<ContractReminder> Plan(IEnumerable<Schedule> schedules, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        var result = new List<ContractReminder>();
        foreach (var schedule in schedules.Where(s => s.State != ScheduleState.Ended))
        {
            if (schedule.CancellationDeadline is { } deadline)
            {
                Add(schedule, ContractDateKind.Cancellation, deadline, deadline.AddDays(-CancellationLeadDays), 0x0800_0000);
                Add(schedule, ContractDateKind.Cancellation, deadline, deadline, 0x0400_0000);
            }

            if (schedule.ReviewDate is { } review)
            {
                Add(schedule, ContractDateKind.Review, review, review, 0x0200_0000);
            }
        }

        return [.. result.OrderBy(r => r.NotifyAt)];

        void Add(Schedule schedule, ContractDateKind kind, DateOnly date, DateOnly day, int mask)
        {
            var at = day.ToDateTime(schedule.ReminderTime, DateTimeKind.Local);
            if (at > now && at <= now.AddDays(ReminderPlanner.HorizonDays))
            {
                result.Add(new ContractReminder((ReminderPlanner.StableId(schedule.Id, date) ^ mask) | 1, at, schedule, kind, date));
            }
        }
    }

    /// <summary>Returns contract dates from today up to <paramref name="days"/> ahead, for the in-app attention list.</summary>
    public static IReadOnlyList<(Schedule Schedule, ContractDateKind Kind, DateOnly Date)> Upcoming(IEnumerable<Schedule> schedules, DateOnly today, int days = 30)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        var until = today.AddDays(days);
        return
        [
            .. schedules
                .Where(s => s.State != ScheduleState.Ended)
                .SelectMany(s => new (Schedule, ContractDateKind, DateOnly?)[] { (s, ContractDateKind.Cancellation, s.CancellationDeadline), (s, ContractDateKind.Review, s.ReviewDate) })
                .Where(x => x.Item3 is { } d && d >= today && d <= until)
                .Select(x => (x.Item1, x.Item2, x.Item3!.Value))
                .OrderBy(x => x.Item3),
        ];
    }
}