namespace Vafadar.Zanance.Core.Commerce;

/// <summary>An explicit scoped choice of retained resources for new work, independent of purchase verification.</summary>
public sealed record ResourceSelection
{
    /// <summary>Copies and canonicalizes the user's identities; neither caller mutation nor ordering changes the choice.</summary>
    public ResourceSelection(QuotaKind kind, QuotaScope scope, IEnumerable<Guid> selectedIds)
    {
        QuotaUsage.ValidateScope(kind, scope);
        if (kind is not (QuotaKind.FinancialAccounts or QuotaKind.Goals or QuotaKind.RecurringPlans
            or QuotaKind.QuickTemplates or QuotaKind.SavedFilters or QuotaKind.LocalProfiles))
            throw new ArgumentException("This resource requires its separate definition or hosted-membership policy.", nameof(kind));
        ArgumentNullException.ThrowIfNull(selectedIds);
        var ids = selectedIds.Distinct().Order().ToArray();
        if (ids.Contains(Guid.Empty)) throw new ArgumentException("A selected resource requires an identity.", nameof(selectedIds));
        Kind = kind;
        Scope = scope;
        IdentitySet = string.Join(";", ids.Select(id => id.ToString("N")));
    }

    /// <summary>Gets the kind of resource, never a membership or a paid right.</summary>
    public QuotaKind Kind { get; }
    /// <summary>Gets the exact financial or device boundary of the choice.</summary>
    public QuotaScope Scope { get; }
    // Like budget definition identity, a canonical value keeps separately materialized cached snapshots equal.
    private string IdentitySet { get; }
    /// <summary>Returns an independent immutable identity list, without exposing the snapshot's storage.</summary>
    public IReadOnlyList<Guid> SelectedIds => Array.AsReadOnly(IdentitySet.Length == 0 ? []
        : IdentitySet.Split(';').Select(id => Guid.ParseExact(id, "N")).ToArray());
}

/// <summary>Immutable availability projection; archived/ended entities and their financial data are never modified.</summary>
public sealed class ResourceAvailability
{
    /// <summary>Creates the evaluated projection, retaining original pause/archive/end state where appropriate.</summary>
    internal ResourceAvailability(IEnumerable<QuotaItem> items, int? maximum, bool requiresSelection)
    {
        Items = Array.AsReadOnly(items.ToArray());
        Maximum = maximum;
        RequiresSelection = requiresSelection;
    }

    /// <summary>Gets every scoped retained identity, including read-only and historical items.</summary>
    public IReadOnlyList<QuotaItem> Items { get; }
    /// <summary>Gets the current capacity; an upgrade can restore unlimited new work without rewriting entities.</summary>
    public int? Maximum { get; }
    /// <summary>Gets whether a missing, stale or over-capacity choice needs explicit review before new work.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Gets the selected count; paused plans/goals continue consuming their chosen slots.</summary>
    public int SelectedCount => Items.Count(item => item.State is QuotaItemState.Active or QuotaItemState.Paused);
    /// <summary>Checks the selected identity only; separate feature, ownership and pause rules still apply.</summary>
    public bool IsSelected(Guid id) => Items.Any(item => item.Id == id
        && item.State is QuotaItemState.Active or QuotaItemState.Paused);
}

/// <summary>Applies the approved downgrade choice without guessing active items, deleting data or granting capabilities.</summary>
public static class ResourceSelectionPolicy
{
    /// <summary>Projects original entity states against the actual scope, tier and optional explicit choice.</summary>
    public static ResourceAvailability Resolve(QuotaKind kind, QuotaScope scope, CapabilityContext context,
        IEnumerable<QuotaItem> items, ResourceSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(items);
        // Construction validates supported kinds even when no explicit selection has been supplied yet.
        _ = new ResourceSelection(kind, scope, []);
        if (scope.Kind is QuotaScopeKind.PersonalProfile or QuotaScopeKind.SharedSpace)
        {
            var expected = context.Scope.Kind == EntitlementScopeKind.PersonalProfile
                ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace;
            if (scope.Kind != expected || scope.Id != context.Scope.Id)
                throw new ArgumentException("Availability and capability boundaries do not match.", nameof(scope));
        }
        else if (context.Scope.Kind != EntitlementScopeKind.PersonalProfile)
            throw new ArgumentException("Shared membership cannot select personal device resources.", nameof(context));
        if (selection is not null && (selection.Kind != kind || selection.Scope != scope))
            throw new ArgumentException("The explicit choice belongs to another resource boundary.", nameof(selection));

        var originals = new Dictionary<Guid, QuotaItem>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Kind != kind || item.Scope != scope) continue;
            if (item.State == QuotaItemState.ReadOnly)
                throw new ArgumentException("Use original entity states, not an earlier tier's derived read-only projection.", nameof(items));
            if (originals.TryGetValue(item.Id, out var prior) && prior.State != item.State)
                throw new ArgumentException("Conflicting original state for the same scoped resource.", nameof(items));
            originals[item.Id] = item;
        }
        var eligible = originals.Values.Where(item => item.State is QuotaItemState.Active or QuotaItemState.Paused)
            .Select(item => item.Id).ToHashSet();
        var maximum = QuotaPolicy.Get(kind, context).Maximum;
        var selected = selection?.SelectedIds.ToHashSet();
        var needsChoice = maximum is > 0 && (selected is null ? eligible.Count > maximum
            : selected.Count > maximum || !selected.IsSubsetOf(eligible));
        // Unlimited access makes retained limited-tier choices irrelevant. Zero access cannot be fixed by a choice.
        var enabled = maximum is null ? eligible
            : maximum == 0 || needsChoice ? [] : selected ?? eligible;
        return new(originals.Values.OrderBy(item => item.Id).Select(item => item.State is QuotaItemState.Active or QuotaItemState.Paused
            && !enabled.Contains(item.Id) ? new QuotaItem(kind, scope, item.Id, QuotaItemState.ReadOnly) : item), maximum, needsChoice);
    }
}
