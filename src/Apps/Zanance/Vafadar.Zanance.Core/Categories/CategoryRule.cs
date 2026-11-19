using Vafadar.Core.Domain;
using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Categories;

/// <summary>
/// A local categorization rule (F2-TX-04): when the payee or title of a new entry contains <see cref="Match"/>, the
/// category is suggested. Suggestions are always visible and can be changed; they never alter saved entries.
/// </summary>
public sealed class CategoryRule : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the text to look for in payee or title (case, digit style and Persian letter forms are ignored).</summary>
    public required string Match { get; set; }

    /// <summary>Gets or sets the suggested category.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Gets or sets whether the rule applies to expenses or income (the kind of its category).</summary>
    public CategoryKind Kind { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Applies categorization rules.</summary>
public static class CategoryRules
{
    /// <summary>Shortest useful match text.</summary>
    public const int MinLength = 2;

    /// <summary>
    /// Returns the rule for a payee and title: the longest matching text wins, so "Market Hall" beats "Market". Rules
    /// of archived categories are ignored.
    /// </summary>
    public static CategoryRule? Suggest(IEnumerable<CategoryRule> rules, IReadOnlyDictionary<Guid, Category> categories, CategoryKind kind, string? payee, string? title) =>
        Resolve(rules, categories, kind, payee, title).Rule;

    /// <summary>Distinguishes a tie between different targets from an unambiguous longest rule.</summary>
    public static (CategoryRule? Rule, bool Conflicting) Resolve(IEnumerable<CategoryRule> rules,
        IReadOnlyDictionary<Guid, Category> categories, CategoryKind kind, string? payee, string? title)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(categories);
        var haystack = Normalize($"{payee} {title}");
        if (haystack.Length == 0)
        {
            return (null, false);
        }

        CategoryRule? rule = null;
        var length = 0;
        var conflict = false;
        foreach (var candidate in rules)
        {
            if (candidate.Kind != kind || !categories.TryGetValue(candidate.CategoryId, out var category) ||
                category.IsArchived || category.Kind != kind) continue;
            var pattern = Normalize(candidate.Match);
            if (pattern.Length < MinLength || pattern.Length < length ||
                !haystack.Contains(pattern, StringComparison.CurrentCultureIgnoreCase)) continue;
            if (pattern.Length > length)
            {
                length = pattern.Length;
                rule = candidate;
                conflict = false;
            }
            else if (rule!.CategoryId != candidate.CategoryId)
                conflict = true;
        }
        return conflict ? (null, true) : (rule, false);
    }

    // The same normalization as the transaction search: digits, Persian letter forms and half-spaces (SearchText).
    private static string Normalize(string? text) => SearchText.Normalize(text);
}
