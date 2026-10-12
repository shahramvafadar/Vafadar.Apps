namespace Vafadar.Zanance.Core.Commerce;

/// <summary>A captured explanation of a blocked affordance; it is never a grant or an alternative to writer validation.</summary>
/// <param name="Feature">Original operation whose policy produced the restriction.</param>
/// <param name="Permission">Original missing feature right, or Allowed for capacity/selection.</param>
/// <param name="Quota">Original resource boundary, when capacity or selection is involved.</param>
/// <param name="Maximum">Captured maximum, never a price or transaction amount.</param>
/// <param name="Current">Captured selected count used by the original policy.</param>
/// <param name="Requested">Net prospective addition, when capacity is involved.</param>
/// <param name="RequiresSelection">Whether the original choice requires explicit review.</param>
/// <param name="InvalidConfiguration">Whether invalid draft enums require ordinary field feedback.</param>
public sealed record WorkRestriction(CommercialFeature Feature, FeaturePermission Permission,
    QuotaKind? Quota = null, int? Maximum = null, int? Current = null, int? Requested = null,
    bool RequiresSelection = false, bool InvalidConfiguration = false);
