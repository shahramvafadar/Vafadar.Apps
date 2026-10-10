using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>
/// Supplies a synchronous, already verified access snapshot for the actual database opened by a write.
/// Implementations must use cached facts, never network or database work while a SQLite transaction is held.
/// This port neither verifies purchases nor authorizes shared-space roles (D-118).
/// </summary>
public interface ICommercialWriteAccessSource
{
    /// <summary>Captures access for the exact opened file, not the possibly changed current-profile preference.</summary>
    CommercialWriteAccess Capture(string databasePath);
}

/// <summary>Immutable write context, explicitly bound to the opened file when enforcement is approved and enabled.</summary>
public sealed record CommercialWriteAccess
{
    private CommercialWriteAccess() { }

    /// <summary>Creates enabled facts for one actual database; it is the provider's responsibility to verify them.</summary>
    public CommercialWriteAccess(string databasePath, CapabilityContext context, ResourceSelection? accountSelection = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(context);
        DatabasePath = Path.GetFullPath(databasePath);
        Context = context;
        if (accountSelection is not null)
        {
            var scope = new QuotaScope(context.Scope.Kind == EntitlementScopeKind.PersonalProfile
                ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace, context.Scope.Id);
            if (accountSelection.Kind != QuotaKind.FinancialAccounts || accountSelection.Scope != scope)
                throw new ArgumentException("The account choice belongs to another financial boundary.", nameof(accountSelection));
        }
        AccountSelection = accountSelection;
    }

    /// <summary>Gets the separately gated inactive policy; this grants no permanent customer entitlement.</summary>
    public static CommercialWriteAccess Inactive { get; } = new();
    /// <summary>Gets whether an approved source supplied enabled policy facts.</summary>
    public bool Enforced => Context is not null;
    /// <summary>Gets the exact enabled file boundary, absent for inactive enforcement.</summary>
    public string? DatabasePath { get; }
    /// <summary>Gets the verified-input context, absent for inactive enforcement.</summary>
    public CapabilityContext? Context { get; }
    /// <summary>Gets the explicit scoped account choice; absence never invents a choice above capacity.</summary>
    public ResourceSelection? AccountSelection { get; }
}

/// <summary>
/// The current release registration: commercial enforcement is not activated before ENT-05/BIL and owner approval.
/// It supplies no fake Plus right, sandbox grant or portable setting. Future activation replaces this provider.
/// </summary>
internal sealed class InactiveCommercialWriteAccessSource : ICommercialWriteAccessSource
{
    /// <inheritdoc />
    public CommercialWriteAccess Capture(string databasePath) => CommercialWriteAccess.Inactive;
}
