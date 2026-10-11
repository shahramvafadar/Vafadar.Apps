namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Original catalogue identities and device-scoped new-work availability; the empty id is the original main profile.</summary>
public sealed class LocalProfileAvailability
{
    /// <summary>Copies original identities and selected availability without exposing mutable storage.</summary>
    internal LocalProfileAvailability(IEnumerable<string> ids, IEnumerable<string> selected, int? maximum, bool needsChoice)
    { Ids = Array.AsReadOnly(ids.ToArray()); SelectedIds = Array.AsReadOnly(selected.ToArray()); Maximum = maximum; RequiresSelection = needsChoice; }
    /// <summary>Gets every original identity; viewing and retained corrections never depend on selection.</summary>
    public IReadOnlyList<string> Ids { get; }
    /// <summary>Gets original identities usable for new work; no paid feature is granted.</summary>
    public IReadOnlyList<string> SelectedIds { get; }
    /// <summary>Gets the device capacity, independent of accounts and shared membership.</summary>
    public int? Maximum { get; }
    /// <summary>Gets whether a missing or stale choice must be reviewed explicitly.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Checks an exact original id without manufacturing a Guid for the legacy main profile.</summary>
    public bool IsSelected(string id) => SelectedIds.Contains(id, StringComparer.Ordinal);
}

/// <summary>Applies the approved profile downgrade rule without rewriting catalogue identities or financial scopes.</summary>
public static class LocalProfileWorkPolicy
{
    /// <summary>Resolves original device resources; null means no explicit choice, while empty is an explicit choice of none.</summary>
    public static LocalProfileAvailability Resolve(QuotaScope device, CapabilityContext context,
        IEnumerable<string> originalIds, IEnumerable<string>? selectedIds = null)
    {
        QuotaUsage.ValidateScope(QuotaKind.LocalProfiles, device);
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(originalIds);
        if (context.Scope.Kind != EntitlementScopeKind.PersonalProfile)
            throw new ArgumentException("Shared membership cannot select personal device profiles.", nameof(context));
        var originals = originalIds.ToArray();
        foreach (var id in originals) ValidateId(id);
        if (originals.Count(id => id.Length == 0) != 1 || originals.Distinct(StringComparer.Ordinal).Count() != originals.Length)
            throw new ArgumentException("The catalogue requires exactly one original main profile and unique original ids.", nameof(originalIds));
        var choice = selectedIds?.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (choice is not null) foreach (var id in choice) ValidateId(id);
        var maximum = QuotaPolicy.Get(QuotaKind.LocalProfiles, context).Maximum;
        var needs = maximum is { } limit && (choice is null ? originals.Length > limit
            : choice.Length > limit || choice.Any(id => !originals.Contains(id, StringComparer.Ordinal)));
        return new(originals, maximum is null ? originals : needs ? [] : choice ?? originals, maximum, needs);
    }
    /// <summary>Accepts only the original main id or an actual catalogue Guid in its stored N spelling.</summary>
    public static void ValidateId(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (id.Length != 0 && !Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid local profile identity.", nameof(id));
    }
}
