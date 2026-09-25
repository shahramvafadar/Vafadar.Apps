namespace Vafadar.Finance.Core.Categories;

/// <summary>
/// The editable starter set of categories (CAT-01). Names are translated through <c>Category_{Key}</c> resources.
/// </summary>
public static class DefaultCategories
{
    /// <summary>Key of the "uncategorized" fallback categories (TX-01).</summary>
    public const string Uncategorized = "Uncategorized";

    /// <summary>Gets the default categories as (kind, key, icon, colour).</summary>
    public static IReadOnlyList<(CategoryKind Kind, string Key, string Icon, string Color)> All { get; } =
    [
        (CategoryKind.Income, "Salary", "Money", "#2E7D32"),
        (CategoryKind.Income, "SideIncome", "BriefcaseMedical", "#388E3C"),
        (CategoryKind.Income, "GiftReceived", "Gift", "#43A047"),
        (CategoryKind.Income, "Interest", "ArrowTrendingLines", "#558B2F"),
        (CategoryKind.Income, Uncategorized, "QuestionCircle", "#78909C"),
        (CategoryKind.Expense, "Housing", "Home", "#5D4037"),
        (CategoryKind.Expense, "Food", "Food", "#E65100"),
        (CategoryKind.Expense, "Energy", "Flash", "#F9A825"),
        (CategoryKind.Expense, "Communication", "Phone", "#0277BD"),
        (CategoryKind.Expense, "Transport", "VehicleCar", "#283593"),
        (CategoryKind.Expense, "Health", "Heart", "#C62828"),
        (CategoryKind.Expense, "Insurance", "Shield", "#4527A0"),
        (CategoryKind.Expense, "Education", "HatGraduation", "#00695C"),
        (CategoryKind.Expense, "Family", "People", "#AD1457"),
        (CategoryKind.Expense, "Leisure", "Games", "#6A1B9A"),
        (CategoryKind.Expense, "Subscriptions", "ArrowRepeatAll", "#00838F"),
        (CategoryKind.Expense, "Other", "MoreHorizontal", "#546E7A"),
        (CategoryKind.Expense, Uncategorized, "QuestionCircle", "#78909C"),
    ];
}
