using System.Security.Cryptography;
using System.Text;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Reminders;

/// <summary>A reminder to schedule on the device.</summary>
/// <param name="Id">Stable id of the occurrence's reminder, the same on every run (REM-07).</param>
/// <param name="NotifyAt">Local time of the notification.</param>
/// <param name="Occurrence">The occurrence it reminds of.</param>
public sealed record PlannedReminder(int Id, DateTime NotifyAt, Occurrence Occurrence);

/// <summary>
/// Decides which reminders the device should hold (REM-01, REM-07..10). The result is rebuilt from the plans on every
/// start, resume and change, so settled, skipped or moved occurrences never keep an old reminder (REM-04, AT-36),
/// and the financial due date never depends on the notification time (REM-08).
/// </summary>
public static class ReminderPlanner
{
    /// <summary>How far ahead reminders are scheduled.</summary>
    public const int HorizonDays = 62;

    /// <summary>At most this many reminders are pending, far below platform limits (REM-10).</summary>
    public const int MaxPending = 30;

    /// <summary>Returns the reminders to schedule, soonest first.</summary>
    /// <param name="schedules">All plans.</param>
    /// <param name="states">All occurrence states.</param>
    /// <param name="now">The current local time.</param>
    public static IReadOnlyList<PlannedReminder> Plan(IEnumerable<Schedule> schedules, IEnumerable<OccurrenceState> states, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        var stateList = states.ToList();
        var today = DateOnly.FromDateTime(now);
        var reminders = new List<PlannedReminder>();

        foreach (var schedule in schedules.Where(s => s.State == ScheduleState.Active && s.ReminderEnabled))
        {
            var from = today;
            var to = today.AddDays(HorizonDays + Math.Max(0, schedule.ReminderDaysBefore));
            foreach (var occurrence in Occurrences.Between(schedule, stateList, from, to, today).Where(o => o.IsOpen))
            {
                var notifyAt = NotifyAt(occurrence.DueDate, schedule.ReminderDaysBefore, schedule.ReminderTime);

                // Only future reminders: missed ones are shown in the app, not as a burst of notifications (AT-33).
                if (notifyAt > now && notifyAt <= now.AddDays(HorizonDays))
                {
                    reminders.Add(new PlannedReminder(StableId(schedule.Id, occurrence.OriginalDate), notifyAt, occurrence));
                }
            }
        }

        return [.. reminders.OrderBy(r => r.NotifyAt).Take(MaxPending)];
    }

    /// <summary>
    /// Returns the local notification time. A time that does not exist on a daylight-saving day is handled by the
    /// platform, which fires at the next valid time (REM-07).
    /// </summary>
    public static DateTime NotifyAt(DateOnly dueDate, int daysBefore, TimeOnly time) =>
        dueDate.AddDays(-Math.Max(0, daysBefore)).ToDateTime(time, DateTimeKind.Local);

    /// <summary>Returns a positive id that is the same for the same occurrence on every device run.</summary>
    public static int StableId(Guid scheduleId, DateOnly originalDate)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes($"{scheduleId:N}:{originalDate.DayNumber}"), hash);
        return (BitConverter.ToInt32(hash) & int.MaxValue) | 1;
    }
}
