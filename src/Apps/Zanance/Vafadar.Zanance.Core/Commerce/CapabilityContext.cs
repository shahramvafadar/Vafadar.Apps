namespace Vafadar.Zanance.Core.Commerce;

/// <summary>The financial data context, distinct from an online identity or a device.</summary>
public enum EntitlementScopeKind
{
    /// <summary>A personal local profile.</summary>
    PersonalProfile,
    /// <summary>One specifically identified shared space.</summary>
    SharedSpace,
}

/// <summary>Immutable financial scope identity, preventing a shared grant from leaking to personal profiles.</summary>
public sealed record EntitlementScope
{
    /// <summary>Creates a non-empty, explicitly typed scope.</summary>
    public EntitlementScope(EntitlementScopeKind kind, Guid id)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (id == Guid.Empty) throw new ArgumentException("A scope requires an identity.", nameof(id));
        Kind = kind;
        Id = id;
    }
    /// <summary>Gets the scope kind.</summary>
    public EntitlementScopeKind Kind { get; }
    /// <summary>Gets the exact profile or space identity.</summary>
    public Guid Id { get; }
}

/// <summary>
/// Membership facts supplied by the authorized shared-space layer. This is not an invitation, role or server
/// authorization check; policy eligibility cannot grant access to another person's data.
/// </summary>
public sealed record SharedSpaceAccess
{
    /// <summary>Creates facts tied to exactly one space.</summary>
    public SharedSpaceAccess(Guid spaceId, bool membershipActive, bool ownerHasActivePro)
    {
        if (spaceId == Guid.Empty) throw new ArgumentException("A membership requires a space.", nameof(spaceId));
        SpaceId = spaceId;
        MembershipActive = membershipActive;
        OwnerHasActivePro = ownerHasActivePro;
    }
    /// <summary>Gets the space to which the membership belongs.</summary>
    public Guid SpaceId { get; }
    /// <summary>Gets whether membership was actually accepted and remains active.</summary>
    public bool MembershipActive { get; }
    /// <summary>Gets whether the host currently supplies the required Pro right.</summary>
    public bool OwnerHasActivePro { get; }
}

/// <summary>Separates personal paid rights from a grant inside one shared space; it has no experience-mode input.</summary>
public sealed record CapabilityContext
{
    /// <summary>Creates a policy context; mismatched, inactive or missing memberships never unlock a space.</summary>
    public CapabilityContext(ProductPlan personalPlan, EntitlementScope scope, SharedSpaceAccess? sharedAccess = null)
    {
        if (!Enum.IsDefined(personalPlan)) throw new ArgumentOutOfRangeException(nameof(personalPlan));
        ArgumentNullException.ThrowIfNull(scope);
        PersonalPlan = personalPlan;
        Scope = scope;
        SharedAccess = sharedAccess;
    }
    /// <summary>Gets the independently resolved personal tier.</summary>
    public ProductPlan PersonalPlan { get; }
    /// <summary>Gets the financial target scope.</summary>
    public EntitlementScope Scope { get; }
    /// <summary>Gets the separately supplied membership facts.</summary>
    public SharedSpaceAccess? SharedAccess { get; }
    /// <summary>Returns whether accepted membership matches this exact target, independently of the host's purchase.</summary>
    public bool HasSharedMembership => Scope.Kind == EntitlementScopeKind.SharedSpace
        && SharedAccess is { MembershipActive: true } access && access.SpaceId == Scope.Id;
    /// <summary>Returns whether matching membership also has the host's active Pro right for new shared work.</summary>
    public bool HasSharedAccess => HasSharedMembership && SharedAccess!.OwnerHasActivePro;
    /// <summary>Returns the local tier inside this context, without extending guests' personal rights.</summary>
    public ProductPlan LocalPlan => HasSharedAccess ? ProductPlan.Plus : PersonalPlan;
}
