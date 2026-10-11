using System.Globalization;
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>An explicit financial definition choice, independent of period row ids and paid rights.</summary>
public sealed record BudgetSelection
{
    /// <summary>Copies canonical scoped definitions; subsequent caller mutation cannot change cached access.</summary>
    public BudgetSelection(QuotaScope scope, IEnumerable<BudgetDefinitionKey> definitions)
    {
        QuotaUsage.ValidateScope(QuotaKind.BudgetDefinitions, scope);
        ArgumentNullException.ThrowIfNull(definitions);
        var keys = definitions.Distinct().ToArray();
        if (keys.Any(key => key is null || key.Scope != scope))
            throw new ArgumentException("A budget choice belongs to one financial scope.", nameof(definitions));
        Scope = scope;
        Value = string.Join('\n', keys.Select(Encode).Order(StringComparer.Ordinal));
        if (Value.Length > 8447) throw new ArgumentException("The budget choice is too large.", nameof(definitions));
    }

    /// <summary>Gets the exact profile or shared-space boundary.</summary>
    public QuotaScope Scope { get; }
    /// <summary>Gets the canonical definition value, containing no period dates, purchase or membership facts.</summary>
    public string Value { get; }
    /// <summary>Returns an independent immutable list of the selected definitions.</summary>
    public IReadOnlyList<BudgetDefinitionKey> Definitions => Array.AsReadOnly(Value.Length == 0 ? []
        : Value.Split('\n').Select(value => Decode(Scope, value)).ToArray());

    /// <summary>Reads only the exact canonical representation; malformed choices never become implicit selections.</summary>
    public static BudgetSelection Parse(QuotaScope scope, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        // Match the existing scoped-choice column bound before splitting it; this does not count financial rows.
        if (value.Length > 8447) throw new ArgumentException("The budget choice is too large.", nameof(value));
        var selection = new BudgetSelection(scope, value.Length == 0 ? [] : value.Split('\n').Select(v => Decode(scope, v)));
        if (selection.Value != value) throw new ArgumentException("The budget choice is not canonical.", nameof(value));
        return selection;
    }

    private static string Encode(BudgetDefinitionKey key) => string.Concat(key.CurrencyCode, "|",
        ((int)key.Period).ToString(CultureInfo.InvariantCulture), "|", key.AccountSet);

    private static BudgetDefinitionKey Decode(QuotaScope scope, string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 3) throw new ArgumentException("The budget definition is invalid.", nameof(value));
        var period = (BudgetPeriod)int.Parse(parts[1], CultureInfo.InvariantCulture);
        return new(scope, parts[0], period, parts[2].Length == 0 ? []
            : parts[2].Split(';').Select(id => Guid.ParseExact(id, "N")));
    }
}

/// <summary>The separate chosen-definition projection; original budgets and all historical numbers remain intact.</summary>
public sealed class BudgetAvailability
{
    /// <summary>Copies the evaluated definition subset and its independent capacity/review state.</summary>
    internal BudgetAvailability(IEnumerable<BudgetDefinitionKey> selected, int? maximum, bool requiresSelection)
    {
        SelectedDefinitions = Array.AsReadOnly(selected.ToArray());
        Maximum = maximum; RequiresSelection = requiresSelection;
    }
    /// <summary>Gets usable definition identities, without granting a method or rollover capability.</summary>
    public IReadOnlyList<BudgetDefinitionKey> SelectedDefinitions { get; }
    /// <summary>Gets the available definition capacity; null means unlimited.</summary>
    public int? Maximum { get; }
    /// <summary>Gets whether a missing, stale or excessive choice requires an explicit review.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Checks canonical identity only; the work policy checks the actual row's paid tools separately.</summary>
    public bool IsSelected(BudgetDefinitionKey definition) => SelectedDefinitions.Contains(definition);
}

/// <summary>Resolves current-period definition choices without selecting the first budget or rewriting history.</summary>
public static class BudgetSelectionPolicy
{
    /// <summary>Evaluates all original rows against the local financial date and explicit month-start preference.</summary>
    public static BudgetAvailability Resolve(CapabilityContext context, QuotaScope scope, IEnumerable<Budget> budgets,
        DateOnly today, int monthStartDay, BudgetSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(budgets);
        var expected = new QuotaScope(context.Scope.Kind == EntitlementScopeKind.PersonalProfile
            ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace, context.Scope.Id);
        if (scope != expected || selection is not null && selection.Scope != scope)
            throw new ArgumentException("Budget availability belongs to one verified financial scope.", nameof(scope));
        var current = budgets.Where(budget => IsCurrent(budget, today, monthStartDay))
            .Select(budget => BudgetDefinitionKey.From(scope, budget)).ToHashSet();
        var maximum = QuotaPolicy.Get(QuotaKind.BudgetDefinitions, context).Maximum;
        var chosen = selection?.Definitions.ToHashSet();
        var needsChoice = maximum is > 0 && (chosen is null ? current.Count > maximum
            : chosen.Count > maximum || !chosen.IsSubsetOf(current));
        var selected = maximum is null ? current : maximum == 0 || needsChoice ? [] : chosen ?? current;
        return new(selected.OrderBy(key => key.CurrencyCode, StringComparer.Ordinal).ThenBy(key => key.Period)
            .ThenBy(key => key.AccountSet, StringComparer.Ordinal), maximum, needsChoice);
    }

    /// <summary>Uses the existing financial calendar/range algorithm, including month-start and weekly dates.</summary>
    public static bool IsCurrent(Budget budget, DateOnly today, int monthStartDay)
    {
        var (first, last) = BudgetPeriods.Range(budget, monthStartDay);
        return today >= first && today <= last;
    }
}
