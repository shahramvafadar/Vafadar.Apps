using System.Collections.ObjectModel;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    /// <summary>Captures every original storage path without opening financial data or consulting commercial quotas.</summary>
    /// <remarks>Startup prepares inactive profiles too. Its integrity capture must not depend on commercial
    /// availability or a database-backed policy source before that database has been prepared.</remarks>
    public ProfileStorageCapture CaptureStorage()
    {
        lock (CatalogueGate)
        {
            var main = Path.GetFullPath(MainPath());
            var packet = preferences.Get(CatalogueKey); var list = preferences.Get(ListKey);
            var name = preferences.Get(MainNameKey); var selected = preferences.Get(CurrentKey);
            var rows = CatalogueProfiles(ReadCatalogue(preferences));
            var originalPaths = rows.ToDictionary(row => row.Id, row => Path.GetFullPath(PathOf(main, row.Id)), StringComparer.Ordinal);
            var current = StartupDatabasePath(main, preferences);
            void Validate()
            {
                lock (CatalogueGate)
                {
                    if (main != Path.GetFullPath(MainPath()) || packet != preferences.Get(CatalogueKey)
                        || list != preferences.Get(ListKey) || name != preferences.Get(MainNameKey)
                        || selected != preferences.Get(CurrentKey))
                        throw new InvalidOperationException("Captured storage catalogue changed during preparation.");
                }
            }
            Validate();
            return new(new ReadOnlyDictionary<string, string>(originalPaths), Path.GetFullPath(current), Validate);
        }
    }
}

/// <summary>Immutable complete storage map and current original, with an actual no-write catalogue recheck.</summary>
/// <param name="OriginalPaths">Main and every retained local profile, including inactive commercial choices.</param>
/// <param name="CurrentOriginalPath">Selected original before any protected generation publication.</param>
/// <param name="Validate">Rechecks actual device catalogue facts without opening financial data.</param>
public sealed record ProfileStorageCapture(IReadOnlyDictionary<string, string> OriginalPaths, string CurrentOriginalPath, Action Validate);
