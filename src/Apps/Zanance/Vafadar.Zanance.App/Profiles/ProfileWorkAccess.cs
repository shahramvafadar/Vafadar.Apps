using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Cached verified personal/device facts for the actual local catalogue; never obtained from a backup.</summary>
public interface IProfileWorkAccessSource
{
    /// <summary>Captures already verified facts for the exact original main database path, without I/O or a provider request.</summary>
    ProfileWorkAccess Capture(string cataloguePath);
}

/// <summary>Immutable device quota facts, separate from the currently open financial profile and paid activation.</summary>
public sealed record ProfileWorkAccess
{
    private ProfileWorkAccess() { }
    /// <summary>Creates verified facts bound to the actual catalogue path and device identity.</summary>
    public ProfileWorkAccess(string cataloguePath, QuotaScope deviceScope, CapabilityContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cataloguePath); ArgumentNullException.ThrowIfNull(context);
        QuotaUsage.ValidateScope(QuotaKind.LocalProfiles, deviceScope);
        if (context.Scope.Kind != EntitlementScopeKind.PersonalProfile) throw new ArgumentException("Shared facts cannot grant personal device work.", nameof(context));
        CataloguePath = Path.GetFullPath(cataloguePath); DeviceScope = deviceScope; Context = context;
    }
    /// <summary>Gets current inactive test registration; this is not a permanent customer grant.</summary>
    public static ProfileWorkAccess Inactive { get; } = new();
    /// <summary>Gets the exact enabled catalogue boundary.</summary>
    public string? CataloguePath { get; }
    /// <summary>Gets the verified device counting identity.</summary>
    public QuotaScope? DeviceScope { get; }
    /// <summary>Gets verified personal facts; absent while enforcement is inactive.</summary>
    public CapabilityContext? Context { get; }
    /// <summary>Gets whether an approved provider enabled the device policy.</summary>
    public bool Enforced => Context is not null;
}

/// <summary>Current production registration; profile limits stay inactive before owner approval of ENT-05.</summary>
internal sealed class InactiveProfileWorkAccessSource : IProfileWorkAccessSource
{
    /// <inheritdoc />
    public ProfileWorkAccess Capture(string cataloguePath) => ProfileWorkAccess.Inactive;
}
