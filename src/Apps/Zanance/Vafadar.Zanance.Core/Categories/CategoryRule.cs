using Vafadar.Core.Domain;
using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Categories;

/// <summary>
/// A local categorization rule (F2-TX-04): when the payee or title of a new entry contains <see cref="Match"/>, the
/// category is suggested. Suggestions are always visible and can be changed; they never alter saved entries.
/// </summary>
public sealed class CategoryRule : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the text to look for in payee or title (case and digit style are ignored).</summary>
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
    public static CategoryRule? Suggest(IEnumerable<CategoryRule> rules, IReadOnlyDictionary<Guid, Category> categories, CategoryKind kind, string? payee, string? title)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(categories);
        var haystack = Normalize($"{payee} {title}");
        if (haystack.Length == 0)
        {
            return null;
        }

        return rules
            .Where(r => r.Kind == kind && categories.TryGetValue(r.CategoryId, out var category) && !category.IsArchived)
            .Select(r => (Rule: r, Text: Normalize(r.Match)))
            .Where(x => x.Text.Length >= MinLength && haystack.Contains(x.Text, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(x => x.Text.Length)
            .Select(x => x.Rule)
            .FirstOrDefault();
    }

    private static string Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : Digits.ToAscii(text.Trim());
}