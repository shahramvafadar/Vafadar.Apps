namespace Vafadar.Zanance.App.Presentation;

internal sealed partial class CategoryLookup
{
    /// <summary>The stored colour of a category.</summary>
    public static Microsoft.Maui.Graphics.Color ParseColor(string? value) =>
        value is not null && Microsoft.Maui.Graphics.Color.TryParse(value, out var color) ? color : Microsoft.Maui.Graphics.Color.FromArgb("#78909C");
}
