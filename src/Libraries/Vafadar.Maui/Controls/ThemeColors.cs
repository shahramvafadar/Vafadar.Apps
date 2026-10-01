namespace Vafadar.Maui.Controls;

/// <summary>Neutral colors of the shared controls in the light and the dark theme.</summary>
internal static class ThemeColors
{
    /// <summary>Gets a value indicating whether the app currently shows the dark theme.</summary>
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>Gets the outline of fields and unselected chips.</summary>
    public static Color Outline => IsDark ? Color.FromArgb("#3A4556") : Color.FromArgb("#B9C3D1");

    /// <summary>Gets the text color on the page background.</summary>
    public static Color Text => IsDark ? Color.FromArgb("#E9EEF5") : Color.FromArgb("#0F1B2D");

    /// <summary>Returns a readable text color on <paramref name="background"/>.</summary>
    public static Color OnColor(Color background)
    {
        ArgumentNullException.ThrowIfNull(background);
        var luminance = (0.2126 * background.Red) + (0.7152 * background.Green) + (0.0722 * background.Blue);
        return luminance > 0.55 ? Color.FromArgb("#0F1B2D") : Colors.White;
    }
}
