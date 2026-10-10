using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>A structured rejection before a commercial write; presentation translates it in the later ENT-04 slice.</summary>
public sealed class CommercialWriteRejectedException : InvalidOperationException
{
    /// <summary>Creates a permission rejection without financial data or user-facing provider details.</summary>
    public CommercialWriteRejectedException(CommercialFeature feature, FeaturePermission permission)
        : base("The commercial operation requires another right.")
    {
        Feature = feature;
        Permission = permission;
    }

    /// <summary>Creates a quota rejection with the observed transaction-local count and requested addition.</summary>
    public CommercialWriteRejectedException(CommercialFeature feature, QuotaKind quota, int maximum, int current, int requested)
        : base("The commercial operation exceeds the resource quota.")
    {
        Feature = feature;
        Permission = FeaturePermission.Allowed;
        Quota = quota;
        Maximum = maximum;
        Current = current;
        Requested = requested;
    }

    /// <summary>Creates an explicit resource-selection rejection, independent of payment and count failures.</summary>
    public CommercialWriteRejectedException(CommercialFeature feature, QuotaKind quota, Guid resourceId, bool requiresSelection)
        : base("The retained resource is not selected for new work.")
    {
        Feature = feature;
        Permission = FeaturePermission.Allowed;
        Quota = quota;
        ResourceId = resourceId;
        RequiresSelection = requiresSelection;
    }

    /// <summary>Gets the rejected retained identity, absent for feature/count failures.</summary>
    public Guid? ResourceId { get; }
    /// <summary>Gets whether the current scoped choice needs explicit review before any new work.</summary>
    public bool RequiresSelection { get; }

    /// <summary>Gets the operation requested.</summary>
    public CommercialFeature Feature { get; }
    /// <summary>Gets the missing commercial right, or Allowed for a count rejection.</summary>
    public FeaturePermission Permission { get; }
    /// <summary>Gets the rejected quota kind, absent for a permission rejection.</summary>
    public QuotaKind? Quota { get; }
    /// <summary>Gets the quota maximum, absent for a permission rejection.</summary>
    public int? Maximum { get; }
    /// <summary>Gets the actual transaction-local count, absent for a permission rejection.</summary>
    public int? Current { get; }
    /// <summary>Gets the attempted addition, absent for a permission rejection.</summary>
    public int? Requested { get; }
}
