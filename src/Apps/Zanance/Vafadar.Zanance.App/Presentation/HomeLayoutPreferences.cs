using Vafadar.Zanance.Core.Dashboard;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps the order and visibility of the Home sections (§21.5) in the settings of the open profile (ZEX-P20), so each
/// profile has its own layout and it is part of its backups. A profile without a layout of its own takes the device
/// preference of earlier versions once.
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
        if (settings.HomeLayout is null)
        {
            settings.HomeLayout = Preferences.Default.Get(LegacyKey, string.Empty);
            store.SaveSettings(settings);
        }

        return HomeLayout.Parse(settings.HomeLayout);
    }

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