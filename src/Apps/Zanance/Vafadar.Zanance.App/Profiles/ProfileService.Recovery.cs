using System.Collections.ObjectModel;
using Vafadar.Core.Settings;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    /// <summary>Device catalogue fields retained when explicit lost-key recovery selects a new installation.</summary>
    public static IReadOnlyList<string> RecoveryCatalogueKeys { get; } = [CatalogueKey, ListKey, CurrentKey, MainNameKey];

    /// <summary>Captures only device profile metadata; financial data and security credentials remain outside this packet.</summary>
    public IReadOnlyDictionary<string, string?> CaptureRecoveryCatalogue()
    {
        lock (CatalogueGate)
            return new ReadOnlyDictionary<string, string?>(RecoveryCatalogueKeys.ToDictionary(key => key, preferences.Get, StringComparer.Ordinal));
    }

    /// <summary>Encodes a restored single-main catalogue without asserting a paid tier, device quota choice or verified account.</summary>
    public static string RecoveredMainCataloguePacket(string? name) => EncodeCatalogue(new CatalogueState(name, null), Guid.NewGuid());
}
