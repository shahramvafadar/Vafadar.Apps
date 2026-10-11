namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Separates chosen quick-template draft activity and new saved definitions from retained query/history rights.</summary>
public sealed class QuickResourceWorkPolicy
{
    private readonly HashSet<Guid>? _templates;
    private readonly HashSet<Guid>? _filters;
    private QuickResourceWorkPolicy(HashSet<Guid>? templates, HashSet<Guid>? filters, bool createTemplate,
        bool createFilter, bool correct, bool delete, bool history)
    {
        _templates = templates; _filters = filters; CanCreateTemplate = createTemplate; CanCreateFilter = createFilter;
        CanCorrect = correct; CanDelete = delete; _history = history;
    }
    private readonly bool _history;
    /// <summary>Gets the separately gated unrestricted test registration, never a permanent paid grant.</summary>
    public static QuickResourceWorkPolicy Inactive { get; } = new(null, null, true, true, true, true, true);
    /// <summary>Gets whether a fresh template can occupy an available chosen slot.</summary>
    public bool CanCreateTemplate { get; }
    /// <summary>Gets whether a fresh saved query can occupy an available chosen slot.</summary>
    public bool CanCreateFilter { get; }
    /// <summary>Gets retained correction rights, independent of selected slots.</summary>
    public bool CanCorrect { get; }
    /// <summary>Gets deletion rights, independent of creation or selection.</summary>
    public bool CanDelete { get; }
    /// <summary>Checks template draft activity only; separate original account and ledger rights still apply before Save.</summary>
    public bool CanApplyTemplate(Guid id) => _templates is null || _templates.Contains(id);
    /// <summary>Allows retained saved-query history without filtering money or granting new resource/transaction rights.</summary>
    public bool CanViewFilter(Guid id) => _history && (_filters is null || _filters.Contains(id));

    /// <summary>Projects complete original identities and optional scoped choices without modifying retained objects.</summary>
    public static QuickResourceWorkPolicy Resolve(CapabilityContext context, QuotaScope scope,
        IEnumerable<Guid> templates, IEnumerable<Guid> filters, ResourceSelection? templateChoice = null,
        ResourceSelection? filterChoice = null, bool templateNeedsReview = false, bool filterNeedsReview = false)
    {
        var templateIds = templates.ToArray(); var filterIds = filters.ToArray();
        var templateAvailability = ResourceSelectionPolicy.Resolve(QuotaKind.QuickTemplates, scope, context,
            templateIds.Select(id => new QuotaItem(QuotaKind.QuickTemplates, scope, id, QuotaItemState.Active)), templateChoice);
        var filterAvailability = ResourceSelectionPolicy.Resolve(QuotaKind.SavedFilters, scope, context,
            filterIds.Select(id => new QuotaItem(QuotaKind.SavedFilters, scope, id, QuotaItemState.Active)), filterChoice);
        var templateRight = PlanPolicy.Check(CommercialFeature.QuickTemplates, context) == FeaturePermission.Allowed;
        var filterRight = PlanPolicy.Check(CommercialFeature.SavedFilters, context) == FeaturePermission.Allowed;
        static bool Room(ResourceAvailability availability, bool needsReview) => !needsReview && !availability.RequiresSelection
            && (availability.Maximum is null || availability.SelectedCount < availability.Maximum);
        return new(templateIds.Where(id => templateRight && !templateNeedsReview && templateAvailability.IsSelected(id)).ToHashSet(),
            filterIds.ToHashSet(), templateRight && Room(templateAvailability, templateNeedsReview),
            filterRight && Room(filterAvailability, filterNeedsReview),
            PlanPolicy.Check(CommercialFeature.Corrections, context) == FeaturePermission.Allowed,
            PlanPolicy.Check(CommercialFeature.DeleteData, context) == FeaturePermission.Allowed,
            PlanPolicy.Check(CommercialFeature.History, context) == FeaturePermission.Allowed);
    }
}
