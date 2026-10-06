namespace Vafadar.Maui.Controls;

/// <summary>Neutral colors of the shared controls in the light and the dark theme.</summary>
internal static class ThemeColors
{
    /// <summary>Gets a value indicating whether the app currently shows the dark theme.</summary>
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>Gets the outline of fields and unselected chips.</summary>
    public static Color Outline => IsDark ? Color.FromArgb("#3D4A62") : Color.FromArgb("#B9C3D1");

    /// <summary>Gets the text color on the page background.</summary>
    public static Color Text => IsDark ? Color.FromArgb("#E9EEF5") : Color.FromArgb("#0F1B2D");

    /// <summary>Gets the color of secondary text (the same as the apps' SecondaryText).</summary>
    public static Color SecondaryText => IsDark ? Color.FromArgb("#AEB8C6") : Color.FromArgb("#4A5568");

    /// <summary>Gets the color of a value that is not valid (the same as the apps' ExpenseText).</summary>
    public static Color Danger => IsDark ? Color.FromArgb("#FF8E82") : Color.FromArgb("#C0392B");

    /// <summary>Returns a readable text color on <paramref name="background"/>.</summary>
    public static Color OnColor(Color background)
    {
        ArgumentNullException.ThrowIfNull(background);
        var luminance = (0.2126 * background.Red) + (0.7152 * background.Green) + (0.0722 * background.Blue);
        return luminance > 0.55 ? Color.FromArgb("#0F1B2D") : Colors.White;
    }
}
