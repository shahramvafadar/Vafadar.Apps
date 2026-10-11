using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Projects new plan work without changing original schedules, historical money or calculation inputs.</summary>
public sealed class PlanWorkPolicy
{
    private readonly HashSet<Guid>? _generating;
    private readonly HashSet<Guid>? _reminding;
    private readonly bool _contracts;

    private PlanWorkPolicy(HashSet<Guid>? generating, HashSet<Guid>? reminding, bool contracts)
    {
        _generating = generating; _reminding = reminding; _contracts = contracts;
    }

    /// <summary>The separately gated inactive registration; it supplies no customer entitlement.</summary>
    public static PlanWorkPolicy Inactive { get; } = new(null, null, true);

    /// <summary>Evaluates original states and the exact scoped choice; a choice never grants a feature or membership.</summary>
    public static PlanWorkPolicy Resolve(CapabilityContext context, QuotaScope scope,
        IEnumerable<Schedule> schedules, ResourceSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        var originals = schedules.ToDictionary(plan => plan.Id);
        var continuations = originals.Values.Where(plan => plan.PreviousScheduleId is not null)
            .ToLookup(plan => plan.PreviousScheduleId!.Value);
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, scope, context,
            originals.Values.Select(plan => QuotaItem.From(scope, plan)), selection);
        var basic = PlanPolicy.Check(CommercialFeature.BasicPlans, context) == FeaturePermission.Allowed;
        var reminders = PlanPolicy.Check(CommercialFeature.PlanReminders, context) == FeaturePermission.Allowed;
        var advanced = PlanPolicy.Check(CommercialFeature.AdvancedPlans, context) == FeaturePermission.Allowed;
        var generating = new HashSet<Guid>(); var reminding = new HashSet<Guid>();
        foreach (var original in originals.Values)
        {
            var owner = original;
            var visited = new HashSet<Guid>();
            // A dated ended slice belongs to its actual unique continuation, whose active slot the user chooses.
            // Ambiguous branches/cycles never invent a selected owner. All original slices remain available to history.
            while (owner.State == ScheduleState.Ended && visited.Add(owner.Id))
            {
                var children = continuations[owner.Id].Take(2).ToArray();
                if (children.Length != 1 || owner.ActiveUntil is not { } until
                    || children[0].ActiveFrom is not { } from || from <= until) break;
                owner = children[0];
            }
            if (!availability.IsSelected(owner.Id)) continue;
            if (reminders && (advanced || !RequiresAdvancedRule(original))) reminding.Add(original.Id);
            if (basic && owner.State is ScheduleState.Active or ScheduleState.Paused && (advanced || !RequiresAdvancedRule(original)))
                generating.Add(original.Id);
        }
        return new(generating, reminding, advanced);
    }

    /// <summary>Checks future generation only; callers still apply the original rule, slice, pause and occurrence state.</summary>
    public bool CanGenerate(Guid scheduleId) => _generating is null || _generating.Contains(scheduleId);

    /// <summary>Keeps complete historical/overdue occurrences while filtering only open future work.</summary>
    public bool AllowsOccurrence(Occurrence occurrence, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return !occurrence.IsOpen || occurrence.DueDate < today || CanGenerate(occurrence.Schedule.Id);
    }

    /// <summary>Checks new reminder delivery separately from retained history, including paid contract automation.</summary>
    public bool CanRemind(Guid scheduleId, bool contract = false) => (!contract || _contracts)
        && (_reminding is null || _reminding.Contains(scheduleId));

    /// <summary>Identifies paid recurrence operations, independent of retained contract fields or the automatic-post choice.</summary>
    public static bool RequiresAdvancedRule(Schedule plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return (plan.Rule.Frequency is Frequency.Monthly or Frequency.Yearly && plan.Rule.DayRule.IsWeekday())
            || (plan.Rule.Frequency == Frequency.Monthly && plan.Rule.SecondDay is not null)
            || plan.Rule.WeekendShift != WeekendShift.None;
    }
}
