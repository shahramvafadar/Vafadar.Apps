using Vafadar.Finance.Core.Dashboard;

namespace Vafadar.Finance.App.Presentation;

/// <summary>Keeps the order and visibility of the Home sections in the local preferences (§21.5).</summary>
internal static class HomeLayoutPreferences
{
    private const string Key = "home_layout";

    /// <summary>Returns the stored layout, or the default.</summary>
    public static HomeLayout Load() => HomeLayout.Parse(Preferences.Default.Get(Key, string.Empty));

    /// <summary>Stores a layout.</summary>
    public static void Save(HomeLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Preferences.Default.Set(Key, layout.ToString());
    }
}