namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The semantic colors of the app in a light and a dark variant (UX-08, VIS-01). XAML uses them as dynamic resources, so
/// switching the theme recolors open pages; code reads the current value through the properties.
/// </summary>
internal static class Palette
{
    // Neutral ground; colour only where it carries a meaning, and each colour always means the same (D-27):
    // blue = action, green = money in, red = problem or debt, amber = near a limit, violet = the future (plans),
    // teal = savings and goals, slate = transfers, sky = refunds, money owed to the user and review. Expenses stay neutral.
    private static readonly Dictionary<string, (string Light, string Dark)> Colors = new()
    {
        ["Primary"] = ("#1D56C9", "#79A6FF"),
        ["OnPrimary"] = ("#FFFFFF", "#0A1830"),
        ["PrimarySoft"] = ("#EAF1FD", "#17264A"),
        ["PrimaryLine"] = ("#C7D8F7", "#29437C"),
        ["IncomeText"] = ("#16794A", "#5FCB95"),
        ["IncomeBackground"] = ("#E8F5EE", "#10281D"),
        ["IncomeLine"] = ("#BEE3CE", "#1E4A35"),
        ["ExpenseText"] = ("#C0392B", "#FF8E82"),
        ["ExpenseBackground"] = ("#FDEDEB", "#331719"),
        ["DangerLine"] = ("#F4C6C0", "#5B2B2A"),
        ["TransferText"] = ("#4A5878", "#AAB7CF"),
        ["TransferBackground"] = ("#EEF1F6", "#1C2330"),
        ["TransferLine"] = ("#D5DBE6", "#323D51"),
        ["WarningText"] = ("#945700", "#F2BF60"),
        ["WarningBackground"] = ("#FFF5E1", "#2D2310"),
        ["WarningLine"] = ("#F1D49A", "#564117"),
        ["PlanText"] = ("#6A4FC4", "#B6A5FF"),
        ["PlanBackground"] = ("#F2EEFC", "#221C3D"),
        ["PlanLine"] = ("#D9CFF6", "#3B3066"),
        ["SavingText"] = ("#0E7880", "#5ED0D7"),
        ["SavingBackground"] = ("#E4F4F5", "#0E292C"),
        ["SavingLine"] = ("#B8E1E4", "#1C4B50"),
        ["RefundText"] = ("#1B72A2", "#70C2EE"),
        ["RefundBackground"] = ("#E6F3FA", "#0F2533"),
        ["RefundLine"] = ("#BCDDEF", "#1E4561"),
        ["PageBackground"] = ("#F4F6F9", "#0D1219"),
        ["CardBackground"] = ("#FFFFFF", "#151B24"),
        ["SurfaceMuted"] = ("#F7F9FC", "#1B222D"),
        ["SecondaryText"] = ("#4A5568", "#AEB8C6"),
        ["DividerColor"] = ("#E3E8EF", "#252E3A"),
        ["StrokeStrong"] = ("#CFD7E2", "#333D4B"),
        ["SnackbarBackground"] = ("#0F1B2D", "#E9EEF5"),
        ["SnackbarText"] = ("#FFFFFF", "#0F1B2D"),
        ["SnackbarAction"] = ("#9DBDFF", "#1D56C9"),
        ["AmountText"] = ("#0F1B2D", "#E9EEF5"),
        ["NearLimit"] = ("#E3A21A", "#D99A1A"),
        ["Muted"] = ("#697586", "#8C97A8"),
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

    public static Color PrimarySoft => Get("PrimarySoft");

    public static Color DangerLine => Get("DangerLine");

    public static Color PlanText => Get("PlanText");

    public static Color PlanLine => Get("PlanLine");

    public static Color IncomeLine => Get("IncomeLine");

    public static Color WarningLine => Get("WarningLine");

    public static Color TransferLine => Get("TransferLine");

    public static Color RefundLine => Get("RefundLine");

    public static Color SavingLine => Get("SavingLine");

    public static Color PlanBackground => Get("PlanBackground");

    public static Color SavingText => Get("SavingText");

    public static Color SavingBackground => Get("SavingBackground");

    public static Color RefundText => Get("RefundText");

    public static Color RefundBackground => Get("RefundBackground");

    public static Color CardBackground => Get("CardBackground");

    public static Color DividerColor => Get("DividerColor");

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
