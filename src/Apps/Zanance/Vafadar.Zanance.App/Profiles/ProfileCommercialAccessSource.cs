using Vafadar.Core.Settings;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Composes verified financial access with original device activity for the actual file, without a profile-service dependency.</summary>
public sealed class ProfileCommercialAccessSource : ICommercialWriteAccessSource
{
    private readonly ICommercialWriteAccessSource _source;
    private readonly ISettingsStore _preferences;
    private readonly IProfileWorkAccessSource _device;
    private readonly string _mainPath;

    /// <summary>Retains the existing source and stable original main path; construction performs no data/provider read.</summary>
    public ProfileCommercialAccessSource(ICommercialWriteAccessSource source, ISettingsStore preferences,
        IProfileWorkAccessSource device, string mainPath)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(device); ArgumentException.ThrowIfNullOrWhiteSpace(mainPath);
        _source = source; _preferences = preferences; _device = device; _mainPath = Path.GetFullPath(mainPath);
    }

    /// <inheritdoc />
    public CommercialWriteAccess Capture(string databasePath)
    {
        var path = Path.GetFullPath(databasePath);
        var original = _source.Capture(path) ?? throw new InvalidOperationException("Missing verified financial access.");
        // Current test builds stay inactive without preference/provider reads. Shared membership does not inherit
        // a personal device choice, and an enabled financial scope is never manufactured for the legacy main id.
        if (!original.Enforced) return original;
        if (original.DatabasePath != path) throw new InvalidOperationException("The verified financial facts belong to another file.");
        if (original.Context!.Scope.Kind == EntitlementScopeKind.SharedSpace) return original;
        var device = _device.Capture(_mainPath) ?? throw new InvalidOperationException("Missing verified device access.");
        if (!device.Enforced) return original;
        if (original.Context.ProfileWork is not null || device.Context!.PersonalPlan != original.Context.PersonalPlan)
            throw new InvalidOperationException("Device and financial activity require matching independent verified facts.");
        var work = ProfileService.CaptureFileWork(_preferences, _mainPath, path, device);
        if (_source.Capture(path) != original || _device.Capture(_mainPath) != device)
            throw new InvalidOperationException("Verified access changed during original file activity capture.");
        var context = new CapabilityContext(original.Context.PersonalPlan, original.Context.Scope, original.Context.SharedAccess, work);
        return new(path, context, original.AccountSelection, original.PlanSelection, original.GoalSelection, original.BudgetSelection);
    }
}
