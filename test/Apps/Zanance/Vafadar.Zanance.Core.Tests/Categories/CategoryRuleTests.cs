using Vafadar.Zanance.Core.Categories;

namespace Vafadar.Zanance.Core.Tests.Categories;

public sealed class CategoryRuleTests
{
    private static readonly Category Food = new() { Name = "Food", Kind = CategoryKind.Expense };
    private static readonly Category Household = new() { Name = "Household", Kind = CategoryKind.Expense };
    private static readonly Category Salary = new() { Name = "Salary", Kind = CategoryKind.Income };

    private static IReadOnlyDictionary<Guid, Category> Categories => new[] { Food, Household, Salary }.ToDictionary(c => c.Id);

    [Fact]
    public void The_longest_matching_text_of_the_same_kind_wins_ignoring_case_and_digit_style()
    {
        CategoryRule[] rules =
        [
            new() { Match = "market", CategoryId = Food.Id, Kind = CategoryKind.Expense },
            new() { Match = "Market Hall", CategoryId = Household.Id, Kind = CategoryKind.Expense },
            new() { Match = "ACME", CategoryId = Salary.Id, Kind = CategoryKind.Income },
            new() { Match = "۲۴", CategoryId = Food.Id, Kind = CategoryKind.Expense },
        ];

        Assert.Equal(Food.Id, CategoryRules.Suggest(rules, Categories, CategoryKind.Expense, "MARKET 12", null)?.CategoryId);
        Assert.Equal(Household.Id, CategoryRules.Suggest(rules, Categories, CategoryKind.Expense, null, "market hall weekly")?.CategoryId);
        Assert.Equal(Salary.Id, CategoryRules.Suggest(rules, Categories, CategoryKind.Income, "Acme Ltd", null)?.CategoryId);
        Assert.Null(CategoryRules.Suggest(rules, Categories, CategoryKind.Expense, "Acme Ltd", null));
        Assert.Equal(Food.Id, CategoryRules.Suggest(rules, Categories, CategoryKind.Expense, "Kiosk 24", null)?.CategoryId);
        Assert.Null(CategoryRules.Suggest(rules, Categories, CategoryKind.Expense, " ", ""));
    }

    [Fact]
    public void Rules_of_archived_categories_and_too_short_texts_are_ignored()
    {
        var archived = new Category { Name = "Old", Kind = CategoryKind.Expense, IsArchived = true };
        var categories = new[] { Food, archived }.ToDictionary(c => c.Id);
        CategoryRule[] rules =
        [
            new() { Match = "shop", CategoryId = archived.Id, Kind = CategoryKind.Expense },
            new() { Match = "s", CategoryId = Food.Id, Kind = CategoryKind.Expense },
        ];

        Assert.Null(CategoryRules.Suggest(rules, categories, CategoryKind.Expense, "shop", null));
    }

    [Fact]
    public void Persian_letter_forms_and_half_spaces_do_not_hide_a_rule()
    {
        // Rules typed on an Arabic keyboard (Kaf, Yeh) or with a space; payees with Persian letters or a half-space (CR02-02).
        var cafe = new CategoryRule { Match = "كافي", CategoryId = Food.Id, Kind = CategoryKind.Expense };
        var grocer = new CategoryRule { Match = "میوه فروشی", CategoryId = Household.Id, Kind = CategoryKind.Expense };

        Assert.Equal(Food.Id, CategoryRules.Suggest([cafe], Categories, CategoryKind.Expense, "کافی نادری", null)?.CategoryId);
        Assert.Equal(Household.Id, CategoryRules.Suggest([grocer], Categories, CategoryKind.Expense, "میوه‌فروشی محله", null)?.CategoryId);
    }
}