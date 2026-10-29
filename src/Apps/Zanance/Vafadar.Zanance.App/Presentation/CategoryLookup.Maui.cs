using System.Collections.Concurrent;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Native category color resolution stays outside application flow tests.</summary>
internal sealed partial class CategoryLookup
{
    private static readonly ConcurrentDictionary<(string Value, bool Dark), Color> DisplayColors = new();

    /// <summary>Gets the current native color for an original category.</summary>
    public Color Color(Guid? id) => DisplayColor(Get(id)?.Color);

    /// <summary>The colour to show: in the dark theme dark category colours are lightened so icons stay readable.</summary>
    public static Color DisplayColor(string? value)
    {
        // Every row of a long list asks for its category colour; there are only a few distinct colours, so each is parsed
        // (and lightened) once per theme instead of once per row.
        var dark = Palette.IsDark;
        return DisplayColors.GetOrAdd((value ?? string.Empty, dark), key =>
        {
            var color = ParseColor(key.Value.Length == 0 ? null : key.Value);
            return key.Dark && color.GetLuminosity() < 0.62f ? color.WithLuminosity(0.68f) : color;
        });
    }


}
