using Vafadar.Zanance.Core.Goals;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Evaluates new goal activity separately from complete original progress and retained financial history.</summary>
public sealed class GoalWorkPolicy
{
    private readonly HashSet<Guid>? _available;
    private readonly HashSet<Guid>? _usable;
    private readonly HashSet<Guid>? _contributing;
    private readonly HashSet<Guid>? _reminding;

    private readonly bool _canReopenBasic;
    private readonly bool _canReopenAdvanced;
    private readonly bool _canReopenReminders;

    private GoalWorkPolicy(HashSet<Guid>? available, HashSet<Guid>? usable, HashSet<Guid>? contributing,
        HashSet<Guid>? reminding, bool canReopenBasic, bool canReopenAdvanced, bool canReopenReminders)
    {
        _available = available; _usable = usable; _contributing = contributing; _reminding = reminding;
        _canReopenBasic = canReopenBasic; _canReopenAdvanced = canReopenAdvanced;
        _canReopenReminders = canReopenReminders;
    }

    /// <summary>The separately gated inactive registration, never a customer entitlement.</summary>
    public static GoalWorkPolicy Inactive { get; } = new(null, null, null, null, true, true, true);

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
        var available = new HashSet<Guid>(); var usable = new HashSet<Guid>();
        var contributing = new HashSet<Guid>(); var reminding = new HashSet<Guid>();
        foreach (var goal in originals)
        {
            // Pause still occupies capacity, but neither selecting it nor an upgrade resumes its activity.
            if (!Enum.IsDefined(goal.Type) || !availability.IsSelected(goal.Id)
                || !basic || (goal.Type != GoalType.AccountBalance && !advanced)) continue;
            available.Add(goal.Id);
            if (goal.State != GoalState.Active) continue;
            usable.Add(goal.Id);
            if (advanced) contributing.Add(goal.Id);
            if (advanced && reminders) reminding.Add(goal.Id);
        }
        // A historical goal joins a free chosen slot only through its writer; this is a view affordance, not a grant.
        var room = !availability.RequiresSelection && (availability.Maximum is null
            || originals.Count(goal => availability.IsSelected(goal.Id)) < availability.Maximum);
        return new(available, usable, contributing, reminding, basic && room, basic && advanced && room, basic && reminders && room);
    }

    /// <summary>Checks original selected availability without resuming a pause or replacing its state caption.</summary>
    public bool IsAvailableGoal(Guid id) => _available is null || _available.Contains(id);

    /// <summary>Checks a historical goal's explicit reopening affordance; the actual writer rechecks type and slots.</summary>
    public bool CanReopenGoal(GoalType type, ContributionPlan? plan = null) => Enum.IsDefined(type)
        && (type == GoalType.AccountBalance ? _canReopenBasic : _canReopenAdvanced)
        && (!GoalContributionPolicy.HasWork(plan) || _canReopenAdvanced)
        && (plan?.ReminderEnabled != true || _canReopenReminders);

    /// <summary>Checks new activity only; viewing original progress and correcting history never use this filter.</summary>
    public bool CanUseGoal(Guid id) => _usable is null || _usable.Contains(id);

    /// <summary>Checks paid contribution suggestions and earmarking separately from basic balance-goal activity.</summary>
    public bool CanContribute(Guid id) => _contributing is null || _contributing.Contains(id);

    /// <summary>Checks native contribution-reminder preparation; the planner still checks progress and saved opt-in.</summary>
    public bool CanRemind(Guid id) => _reminding is null || _reminding.Contains(id);
}
