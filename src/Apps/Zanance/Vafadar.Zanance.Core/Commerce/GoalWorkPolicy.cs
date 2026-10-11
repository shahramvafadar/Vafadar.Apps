using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Evaluates new goal activity separately from complete original progress and retained financial history.</summary>
public sealed class GoalWorkPolicy
{
    private readonly HashSet<Guid>? _usable;
    private readonly HashSet<Guid>? _contributing;
    private readonly HashSet<Guid>? _reminding;

    private GoalWorkPolicy(HashSet<Guid>? usable, HashSet<Guid>? contributing, HashSet<Guid>? reminding)
    {
        _usable = usable; _contributing = contributing; _reminding = reminding;
    }

    /// <summary>The separately gated inactive registration, never a customer entitlement.</summary>
    public static GoalWorkPolicy Inactive { get; } = new(null, null, null);

    /// <summary>Resolves original goal states and the exact explicit choice; selection grants no paid tool.</summary>
    public static GoalWorkPolicy Resolve(CapabilityContext context, QuotaScope scope,
        IEnumerable<Goal> goals, ResourceSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(goals);
        var originals = goals.ToArray();
        var availability = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, scope, context,
            originals.Select(goal => QuotaItem.From(scope, goal)), selection);
        var basic = PlanPolicy.Check(CommercialFeature.BasicGoals, context) == FeaturePermission.Allowed;
        var advanced = PlanPolicy.Check(CommercialFeature.AdvancedGoals, context) == FeaturePermission.Allowed;
        var reminders = PlanPolicy.Check(CommercialFeature.ContributionReviewReminders, context) == FeaturePermission.Allowed;
        var usable = new HashSet<Guid>(); var contributing = new HashSet<Guid>(); var reminding = new HashSet<Guid>();
        foreach (var goal in originals)
        {
            // Pause still occupies capacity, but neither selecting it nor an upgrade resumes its activity.
            if (goal.State != GoalState.Active || !Enum.IsDefined(goal.Type) || !availability.IsSelected(goal.Id)
                || !basic || (goal.Type != GoalType.AccountBalance && !advanced)) continue;
            usable.Add(goal.Id);
            if (advanced) contributing.Add(goal.Id);
            if (advanced && reminders) reminding.Add(goal.Id);
        }
        return new(usable, contributing, reminding);
    }

    /// <summary>Checks new activity only; viewing original progress and correcting history never use this filter.</summary>
    public bool CanUseGoal(Guid id) => _usable is null || _usable.Contains(id);

    /// <summary>Checks paid contribution suggestions and earmarking separately from basic balance-goal activity.</summary>
    public bool CanContribute(Guid id) => _contributing is null || _contributing.Contains(id);

    /// <summary>Checks native contribution-reminder preparation; the planner still checks progress and saved opt-in.</summary>
    public bool CanRemind(Guid id) => _reminding is null || _reminding.Contains(id);
}
