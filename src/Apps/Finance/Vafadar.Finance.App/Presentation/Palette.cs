namespace Vafadar.Finance.App.Presentation;

/// <summary>
/// The semantic colors of the app in a light and a dark variant (UX-08, VIS-01). XAML uses them as dynamic resources, so
/// switching the theme recolors open pages; code reads the current value through the properties.
/// </summary>
internal static class Palette
{
    private static readonly Dictionary<string, (string Light, string Dark)> Colors = new()
    {
        ["Primary"] = ("#2E7D32", "#81C784"),
        ["OnPrimary"] = ("#FFFFFF", "#0D1F0E"),
        ["IncomeText"] = ("#1B5E20", "#81C784"),
        ["IncomeBackground"] = ("#E8F5E9", "#1B3A1E"),
        ["ExpenseText"] = ("#B71C1C", "#EF9A9A"),
        ["ExpenseBackground"] = ("#FFEBEE", "#3B1D1D"),
        ["TransferText"] = ("#37474F", "#B0BEC5"),
        ["TransferBackground"] = ("#ECEFF1", "#263238"),
        ["WarningText"] = ("#8D5B00", "#FFCC80"),
        ["WarningBackground"] = ("#FFF4E0", "#3A2C12"),
        ["PageBackground"] = ("#F6F7F6", "#121412"),
        ["CardBackground"] = ("#FFFFFF", "#1D201D"),
        ["SecondaryText"] = ("#5F6368", "#A8AEB4"),
        ["DividerColor"] = ("#E3E6E3", "#2E332E"),
        ["AmountText"] = ("#1F1F1F", "#EDEDED"),
        ["NearLimit"] = ("#F9A825", "#FFD54F"),
        ["Muted"] = ("#9E9E9E", "#757575"),
    };

    /// <summary>Gets the semantic color keys.</summary>
    public static IEnumerable<string> Keys => Colors.Keys;

    /// <summary>Gets a value indicating whether the dark variant is active.</summary>
    public static bool IsDark { get; private set; }

    public static Color Primary => Get("Primary");

    public static Color IncomeText => Get("IncomeText");

    public static Color IncomeBackground => Get("IncomeBackground");

    public static Color ExpenseText => Get("ExpenseText");

    public static Color ExpenseBackground => Get("ExpenseBackground");

    public static Color TransferText => Get("TransferText");

    public static Color TransferBackground => Get("TransferBackground");

    public static Color WarningText => Get("WarningText");

    public static Color WarningBackground => Get("WarningBackground");

    public static Color PageBackground => Get("PageBackground");

    public static Color SecondaryText => Get("SecondaryText");

    public static Color AmountText => Get("AmountText");

    public static Color NearLimit => Get("NearLimit");

    public static Color Muted => Get("Muted");

    /// <summary>Writes the variant into <paramref name="resources"/>; dynamic resources update at once.</summary>
    public static void Apply(ResourceDictionary resources, bool dark)
    {
        ArgumentNullException.ThrowIfNull(resources);
        IsDark = dark;
        foreach (var key in Colors.Keys)
        {
            resources[key] = Get(key);
        }
    }

    /// <summary>Returns the current value of a semantic color.</summary>
    public static Color Get(string key) => Color.FromArgb(IsDark ? Colors[key].Dark : Colors[key].Light);
}