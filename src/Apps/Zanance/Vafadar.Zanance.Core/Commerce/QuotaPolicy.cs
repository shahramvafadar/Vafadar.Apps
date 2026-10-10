namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Separately scoped resource counts, never transaction or history limits.</summary>
public enum QuotaKind
{
    /// <summary>Usable financial accounts in a profile or space.</summary>
    FinancialAccounts,
    /// <summary>Active budget definitions, not rows for historical periods.</summary>
    BudgetDefinitions,
    /// <summary>Active or paused goals.</summary>
    Goals,
    /// <summary>Active or paused plans.</summary>
    RecurringPlans,
    /// <summary>Quick-entry templates.</summary>
    QuickTemplates,
    /// <summary>Saved filters.</summary>
    SavedFilters,
    /// <summary>Usable local profiles on a device.</summary>
    LocalProfiles,
    /// <summary>Spaces hosted by one online identity.</summary>
    HostedSpaces,
    /// <summary>Owner, members and unexpired pending invitations in one space.</summary>
    SharedMembers,
}

/// <summary>A maximum count; null means unlimited, zero means no new resources of this kind.</summary>
public sealed record Quota
{
    /// <summary>Creates a nonnegative or unlimited maximum.</summary>
    public Quota(int? maximum)
    {
        if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        Maximum = maximum;
    }
    /// <summary>Gets the maximum, or null for unlimited.</summary>
    public int? Maximum { get; }
    /// <summary>Checks an addition without integer overflow; does not mutate or delete over-quota data.</summary>
    public bool CanAdd(int current, int requested = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(current);
        ArgumentOutOfRangeException.ThrowIfNegative(requested);
        return requested == 0 || Maximum is null || (long)current + requested <= Maximum;
    }
}

/// <summary>The final proposed counts only; no app service applies them until separately approved enforcement.</summary>
public static class QuotaPolicy
{
    /// <summary>Returns the quota in the appropriate personal or specifically authorized shared context.</summary>
    public static Quota Get(QuotaKind kind, CapabilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var shared = context.Scope.Kind == EntitlementScopeKind.SharedSpace;
        if (shared && !context.HasSharedAccess) return new(0);
        if (kind == QuotaKind.SharedMembers) return new(shared ? 6 : 0);
        if (kind == QuotaKind.HostedSpaces) return new(!shared && context.PersonalPlan == ProductPlan.Pro ? 1 : 0);
        if (kind == QuotaKind.LocalProfiles) return shared ? new(0) : new(context.PersonalPlan == ProductPlan.Free ? 1 : null);
        if (context.LocalPlan >= ProductPlan.Plus) return new(null);
        return kind switch
        {
            QuotaKind.FinancialAccounts => new(3),
            QuotaKind.BudgetDefinitions or QuotaKind.Goals or QuotaKind.SavedFilters => new(1),
            QuotaKind.RecurringPlans => new(5),
            QuotaKind.QuickTemplates => new(3),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}
