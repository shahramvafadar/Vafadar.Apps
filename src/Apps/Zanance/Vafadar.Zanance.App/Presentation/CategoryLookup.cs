using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Categories;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Display names, icons and colours of categories for one screen load.</summary>
internal sealed class CategoryLookup
{
    private readonly Dictionary<Guid, Category> _byId;
    private readonly Translator _translator;

    public CategoryLookup(IEnumerable<Category> categories, Translator translator)
    {
        _translator = translator;
        _byId = categories.ToDictionary(c => c.Id);
    }

    public IReadOnlyCollection<Category> All => _byId.Values;

    public Category? Get(Guid? id) => id is { } value && _byId.TryGetValue(value, out var category) ? category : null;

    /// <summary>The user's name, or the translated default name of a starter category.</summary>
    public static string NameOf(Category category, Translator translator) =>
        !string.IsNullOrWhiteSpace(category.Name) ? category.Name
        : category.SystemKey is { } key ? translator[$"Category_{key}"]
        : string.Empty;

    public string Name(Guid? id) => Get(id) is { } category ? NameOf(category, _translator) : _translator["Category_Uncategorized"];

    public Symbol Icon(Guid? id) => Icons.Parse(Get(id)?.Icon, Symbol.QuestionCircle);

    public Color Color(Guid? id) => DisplayColor(Get(id)?.Color);

    /// <summary>The "Uncategorized" fallback of a kind (TX-01).</summary>
    public Guid? Uncategorized(CategoryKind kind) =>
        _byId.Values.FirstOrDefault(c => c.Kind == kind && c.SystemKey == DefaultCategories.Uncategorized)?.Id;

    /// <summary>The category used for transfer fees.</summary>
    public Guid? Fees() =>
        _byId.Values.FirstOrDefault(c => c.Kind == CategoryKind.Expense && c.SystemKey == DefaultCategories.Fees)?.Id;

    /// <summary>The colour to show: in the dark theme dark category colours are lightened so icons stay readable.</summary>
    public static Color DisplayColor(string? value)
    {
        var color = ParseColor(value);
        return Palette.IsDark && color.GetLuminosity() < 0.62f ? color.WithLuminosity(0.68f) : color;
    }

    /// <summary>The stored colour of a category.</summary>
    public static Color ParseColor(string? value) =>
        value is not null && Microsoft.Maui.Graphics.Color.TryParse(value, out var color) ? color : Microsoft.Maui.Graphics.Color.FromArgb("#78909C");
}
