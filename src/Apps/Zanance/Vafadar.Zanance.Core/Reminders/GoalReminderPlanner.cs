using System.Security.Cryptography;
using System.Text;
using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Reminders;

/// <summary>A contribution-date reminder; it never records a contribution or a ledger entry.</summary>
/// <param name="Id">Stable notification id in the goal namespace.</param>
/// <param name="NotifyAt">Future local notification time.</param>
/// <param name="Goal">The active, available and unreached goal.</param>
/// <param name="Date">The contribution date from the saved recurrence rule.</param>
public sealed record GoalReminder(int Id, DateTime NotifyAt, Goal Goal, DateOnly Date);

/// <summary>Computes optional goal reminders from saved contribution rules and current progress (ZCR-LOC-02).</summary>
public static class GoalReminderPlanner
{
    /// <summary>Local delivery time on contribution dates; no background transfer or exact alarm is required.</summary>
    public static TimeOnly DeliveryTime => new(9, 0);

    /// <summary>Returns future reminders inside the shared horizon, with the shared pending-count bound.</summary>
    public static IReadOnlyList<GoalReminder> Plan(IEnumerable<GoalProgress> progress, IEnumerable<ContributionPlan> plans, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(plans);
        var byGoal = plans.Where(p => p.ReminderEnabled).GroupBy(p => p.GoalId).ToDictionary(g => g.Key, g => g.First());
        var today = DateOnly.FromDateTime(now);
        var until = now.AddDays(ReminderPlanner.HorizonDays);
        var result = new List<GoalReminder>();
        foreach (var item in progress.Where(IsEligible))
        {
            if (!byGoal.TryGetValue(item.Goal.Id, out var plan)) { continue; }
            foreach (var date in ContributionSchedule.Dates(plan.Rule, today, DateOnly.FromDateTime(until)))
            {
                var at = date.ToDateTime(DeliveryTime, DateTimeKind.Local);
                // Missed dates stay in the user's plan; resuming the app must never send a notification burst.
                if (at > now && at <= until) { result.Add(new GoalReminder(StableId(item.Goal.Id, date), at, item.Goal, date)); }
            }
        }
        return [.. result.OrderBy(r => r.NotifyAt).ThenBy(r => r.Goal.Id).Take(ReminderPlanner.MaxPending)];
    }

    /// <summary>Returns whether current progress permits reminders; paused/reached/unavailable goals stay silent.</summary>
    public static bool IsEligible(GoalProgress progress) => progress.Goal.State == GoalState.Active
        && progress.Target > 0 && !progress.IsReached
        && progress.Notice is not (GoalNotice.AccountUnavailable or GoalNotice.HoldingUnavailable);

    /// <summary>Returns a deterministic positive id derived from the separate goal namespace.</summary>
    public static int StableId(Guid goalId, DateOnly date)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"goal:{goalId:N}:{date.DayNumber}")), hash);
        return (BitConverter.ToInt32(hash) & int.MaxValue) | 1;
    }
}
