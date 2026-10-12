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
    private readonly bool _canCorrect;
    private readonly bool _canConfigureContribution;
    private readonly bool _canConfigureReminders;
    private WorkRestriction? NewBasic { get; init; }
    private WorkRestriction? NewAdvanced { get; init; }
    private WorkRestriction? NewReminder { get; init; }

    private GoalWorkPolicy(HashSet<Guid>? available, HashSet<Guid>? usable, HashSet<Guid>? contributing,
        HashSet<Guid>? reminding, bool canReopenBasic, bool canReopenAdvanced, bool canReopenReminders, bool canCorrect,
        bool canConfigureContribution, bool canConfigureReminders)
    {
        _available = available; _usable = usable; _contributing = contributing; _reminding = reminding;
        _canReopenBasic = canReopenBasic; _canReopenAdvanced = canReopenAdvanced;
        _canReopenReminders = canReopenReminders;
        _canCorrect = canCorrect;
        _canConfigureContribution = canConfigureContribution; _canConfigureReminders = canConfigureReminders;
    }

    /// <summary>The separately gated inactive registration, never a customer entitlement.</summary>
    public static GoalWorkPolicy Inactive { get; } = new(null, null, null, null, true, true, true, true, true, true);

    /// <summary>Resolves original goal states and the exact explicit choice; selection grants no paid tool.</summary>
    public static GoalWorkPolicy Resolve(CapabilityContext context, QuotaScope scope,
        IEnumerable<Goal> goals, ResourceSelection? selection = null, bool selectionNeedsReview = false)
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
        var needsSelection = availability.RequiresSelection || availability.Maximum is > 0 && selectionNeedsReview;
        var room = !needsSelection && (availability.Maximum is null
            || originals.Count(goal => availability.IsSelected(goal.Id)) < availability.Maximum);
        WorkRestriction? Restriction(CommercialFeature feature)
        {
            var permission = PlanPolicy.Check(feature, context);
            if (permission != FeaturePermission.Allowed) return new(feature, permission);
            if (needsSelection) return new(feature, FeaturePermission.Allowed, QuotaKind.Goals, RequiresSelection: true);
            if (!room) return new(feature, FeaturePermission.Allowed, QuotaKind.Goals, availability.Maximum, availability.SelectedCount, 1);
            return null;
        }
        return new(available, usable, contributing, reminding, basic && room, basic && advanced && room, basic && reminders && room,
            PlanPolicy.Check(CommercialFeature.Corrections, context) == FeaturePermission.Allowed, basic && advanced, basic && advanced && reminders)
        {
            NewBasic = Restriction(CommercialFeature.BasicGoals),
            NewAdvanced = Restriction(CommercialFeature.AdvancedGoals),
            NewReminder = Restriction(CommercialFeature.ContributionReviewReminders),
        };
    }

    /// <summary>Explains a new paired goal's captured restriction without mutating its draft or authorizing a write.</summary>
    public WorkRestriction? CreationRestriction(GoalType type, ContributionPlan? plan = null)
    {
        if (!Enum.IsDefined(type)) return new(CommercialFeature.BasicGoals, FeaturePermission.Allowed, InvalidConfiguration: true);
        if (type != GoalType.AccountBalance || GoalContributionPolicy.HasWork(plan))
            return NewAdvanced ?? (plan?.ReminderEnabled == true ? NewReminder : null);
        return NewBasic ?? (plan?.ReminderEnabled == true ? NewReminder : null);
    }

    /// <summary>Checks original selected availability without resuming a pause or replacing its state caption.</summary>
    public bool IsAvailableGoal(Guid id) => _available is null || _available.Contains(id);

    /// <summary>Checks retained metadata/contribution corrections independently of selected new activity.</summary>
    public bool CanCorrect => _canCorrect;

    /// <summary>Checks a new goal's paired paid contribution before its eventual atomic joining of a free slot.</summary>
    public bool CanCreateContribution(GoalType type) => CanReopenGoal(type) && _canReopenAdvanced;

    /// <summary>Checks the independent paid reminder for a new paired goal draft, never a native permission grant.</summary>
    public bool CanCreateReminder(GoalType type) => CanCreateContribution(type) && _canReopenReminders;

    /// <summary>Allows configuration of a selected active/paused goal without resuming or delivering its work.</summary>
    public bool CanConfigureContribution(Guid id) => IsAvailableGoal(id) && _canConfigureContribution;

    /// <summary>Checks a selected configuration's paid reminder separately from native permission and delivery.</summary>
    public bool CanConfigureReminder(Guid id) => IsAvailableGoal(id) && _canConfigureReminders;

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
