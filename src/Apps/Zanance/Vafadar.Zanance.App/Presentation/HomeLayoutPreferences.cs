using Vafadar.Zanance.Core.Dashboard;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps the order and visibility of the Home sections (§21.5) in the settings of the open profile (ZEX-P20), so each
/// profile has its own layout and it is part of its backups. A profile without a layout of its own takes the device
/// preference of earlier versions until the user explicitly saves a profile layout.
/// </summary>
internal static class HomeLayoutPreferences
{
    // The device preference of versions before ZEX-P20; read once per profile, never written any more.
    private const string LegacyKey = "home_layout";

    /// <summary>Returns the layout of the open profile, or the default.</summary>
    public static HomeLayout Load(ZananceStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var settings = store.GetSettings();
        // D-155: opening Home is read-only. Keep the legacy visual fallback until an explicit layout Save,
        // rather than creating preferences or changing a settings timestamp during every new profile's first visit.
        return FromSnapshot(settings.HomeLayout);
    }

    /// <summary>Resolves already captured profile settings without reopening a keyed database on the window thread.</summary>
    public static HomeLayout FromSnapshot(string? profileLayout) =>
        HomeLayout.Parse(profileLayout ?? Preferences.Default.Get(LegacyKey, string.Empty));

    /// <summary>Restores the default layout of the open profile, e.g. with "Delete all data".</summary>
    public static async Task ClearAsync(ZananceStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var settings = await store.GetSettingsAsync();
        settings.HomeLayout = string.Empty;
        await store.SaveSettingsAsync(settings);
    }

    /// <summary>Stores a layout in the open profile.</summary>
    public static async Task SaveAsync(ZananceStore store, HomeLayout layout)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(layout);
        var settings = await store.GetSettingsAsync();
        settings.HomeLayout = layout.ToString();
        await store.SaveSettingsAsync(settings);
    }
}
