global using Color = Microsoft.Maui.Graphics.Color;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Headless palette boundary only; source/consent checks never assert these placeholders as native styling.</summary>
internal static class Palette
{
    /// <summary>Returns an opaque placeholder for comparison rows outside native theme qualification.</summary>
    /// <summary>Returns a headless warning placeholder.</summary>
    public static Color WarningText => Microsoft.Maui.Graphics.Colors.Transparent;
    /// <summary>Returns a headless income placeholder.</summary>
    public static Color IncomeText => Microsoft.Maui.Graphics.Colors.Transparent;
    /// <summary>Returns a headless expense placeholder.</summary>
    public static Color ExpenseText => Microsoft.Maui.Graphics.Colors.Transparent;
    public static Color SecondaryText => Microsoft.Maui.Graphics.Colors.Transparent;
}

/// <summary>Headless presentation boundary; the actual native presenter remains separately compiled and qualified.</summary>
internal static class EntryPresenter
{
    /// <summary>Returns an opaque placeholder without claiming native color or accessibility acceptance.</summary>
    /// <summary>Returns a headless original expense placeholder.</summary>
    public static Color ExpenseColor => Microsoft.Maui.Graphics.Colors.Transparent;
    public static Color DangerColor => Microsoft.Maui.Graphics.Colors.Transparent;
}

/// <summary>Awaits the original read in headless checks; native notice presentation is separately qualified.</summary>
internal static class Failures
{
    /// <summary>Propagates read failures to the boundary check instead of displaying native notices.</summary>
    public static Task GuardAsync(Func<Task> action) => action();
}
