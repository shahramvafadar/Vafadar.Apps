using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Separates current new budget work from original financial calculations and retained corrections.</summary>
public sealed class BudgetWorkPolicy
{
    private readonly HashSet<Guid>? _usable;
    private readonly bool _basic;
    private readonly bool _advanced;
    private readonly bool _room;
    private BudgetWorkPolicy(HashSet<Guid>? usable, bool correct, bool basic, bool advanced, bool room)
    {
        _usable = usable; CanCorrect = correct; _basic = basic; _advanced = advanced; _room = room;
    }
    /// <summary>Gets the separately gated current inactive registration, never a permanent customer grant.</summary>
    public static BudgetWorkPolicy Inactive { get; } = new(null, true, true, true, true);
    /// <summary>Gets retained correction rights independently of active definition selection and paid methods.</summary>
    public bool CanCorrect { get; }
    /// <summary>Checks only new current-period activity. Never use this filter for original totals or carry history.</summary>
    public bool CanUse(Guid id) => _usable is null || _usable.Contains(id);
    /// <summary>Checks a new definition affordance; the actual writer checks the resulting net capacity again.</summary>
    public bool CanCreate(BudgetMethod method, BudgetRollover rollover, BudgetPeriod period) => _room && _basic
        && Enum.IsDefined(method) && Enum.IsDefined(rollover) && Enum.IsDefined(period)
        && (!RequiresAdvanced(method, rollover, period) || _advanced);
    /// <summary>Checks the separate paid tools; a selected definition alone never grants them.</summary>
    public static bool RequiresAdvanced(BudgetMethod method, BudgetRollover rollover, BudgetPeriod period) =>
        method != BudgetMethod.Limits || rollover != BudgetRollover.None || period != BudgetPeriod.Month;
    /// <summary>Projects selected current rows without modifying original dates, methods, accounts or limits.</summary>
    public static BudgetWorkPolicy Resolve(CapabilityContext context, QuotaScope scope, IEnumerable<Budget> budgets,
        DateOnly today, int monthStartDay, BudgetSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(budgets);
        var originals = budgets.ToArray();
        var availability = BudgetSelectionPolicy.Resolve(context, scope, originals, today, monthStartDay, selection);
        var basic = PlanPolicy.Check(CommercialFeature.BasicBudgets, context) == FeaturePermission.Allowed;
        var advanced = PlanPolicy.Check(CommercialFeature.AdvancedBudgets, context) == FeaturePermission.Allowed;
        var usable = originals.Where(budget => basic && BudgetSelectionPolicy.IsCurrent(budget, today, monthStartDay)
            && Enum.IsDefined(budget.Method) && Enum.IsDefined(budget.Rollover) && Enum.IsDefined(budget.Period)
            && availability.IsSelected(BudgetDefinitionKey.From(scope, budget))
            && (!RequiresAdvanced(budget.Method, budget.Rollover, budget.Period) || advanced)).Select(b => b.Id).ToHashSet();
        var room = !availability.RequiresSelection && (availability.Maximum is null
            || availability.SelectedDefinitions.Count < availability.Maximum);
        return new(usable, PlanPolicy.Check(CommercialFeature.Corrections, context) == FeaturePermission.Allowed,
            basic, advanced, room);
    }
}
