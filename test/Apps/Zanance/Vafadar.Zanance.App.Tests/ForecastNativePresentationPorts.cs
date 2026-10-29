global using Color = Microsoft.Maui.Graphics.Color;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Headless palette boundary only; source/consent checks never assert these placeholders as native styling.</summary>
internal static class Palette
{
    /// <summary>Returns an opaque placeholder for comparison rows outside native theme qualification.</summary>
    public static Color SecondaryText => Microsoft.Maui.Graphics.Colors.Transparent;
}

/// <summary>Headless presentation boundary; the actual native presenter remains separately compiled and qualified.</summary>
internal static class EntryPresenter
{
    /// <summary>Returns an opaque placeholder without claiming native color or accessibility acceptance.</summary>
    public static Color DangerColor => Microsoft.Maui.Graphics.Colors.Transparent;
}
