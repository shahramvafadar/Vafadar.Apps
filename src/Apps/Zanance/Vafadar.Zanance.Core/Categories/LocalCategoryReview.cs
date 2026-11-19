using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Categories;

/// <summary>The reason shown with a candidate or an explicit absence of a safe match.</summary>
public enum CategoryReviewReason
{
    /// <summary>A fixed label signal has one eligible category.</summary>
    Signal,
    /// <summary>The available label and taxonomy provide no eligible match.</summary>
    NoMatch,
    /// <summary>The strongest existing rules point to different categories.</summary>
    ConflictingRules,
    /// <summary>Local evidence points to more than one possible category.</summary>
    ConflictingSignals,
}

/// <summary>A local draft candidate and one reason. No ID means no proposed category.</summary>
public sealed record LocalCategoryReview(Guid? CategoryId, CategoryReviewReason Reason)
{
    // These whole words are fixed taxonomy signals rather than customer-specific examples or merchant lists.
    private static readonly (CategoryKind Kind, string Key, HashSet<string> Words)[] Signals =
    [
        Make(CategoryKind.Expense, "Food", "supermarket groceries supermarkt سوپرمارکت خواربار supermercado supermarche supermercato"),
        Make(CategoryKind.Expense, "Health", "pharmacy apotheke داروخانه farmacia pharmacie"),
        Make(CategoryKind.Expense, "Transport", "railway train bahn تاکسی taxi tren treno ferrovia"),
        Make(CategoryKind.Expense, "Insurance", "insurance versicherung بیمه seguro assurance assicurazione"),
        Make(CategoryKind.Expense, "Housing", "rent miete اجاره alquiler loyer affitto"),
        Make(CategoryKind.Expense, "Education", "tuition studiengebuhr شهریه matricula scolarite"),
        Make(CategoryKind.Income, "Salary", "salary payroll gehalt حقوق nomina salaire stipendio"),
        Make(CategoryKind.Income, "Interest", "interest zinsen بهره intereses interets interessi"),
    ];

    /// <summary>Evaluates only payment label/payee words after the explicit rules have been considered.</summary>
    public static LocalCategoryReview Evaluate(IEnumerable<Category> categories, CategoryKind kind, string? payee,
        string? title, bool conflictingRules = false)
    {
        if (conflictingRules) return new(null, CategoryReviewReason.ConflictingRules);
        // A long label could contain conflicting evidence beyond a prefix. Decline instead of truncating it.
        if (payee?.Length > 512 || title?.Length > 512) return new(null, CategoryReviewReason.NoMatch);
        var words = Words(payee).Concat(Words(title)).ToHashSet(StringComparer.Ordinal);
        var keys = Signals.Where(signal => signal.Kind == kind && signal.Words.Overlaps(words))
            .Select(signal => signal.Key).ToArray();
        if (keys.Length > 1) return new(null, CategoryReviewReason.ConflictingSignals);
        if (keys.Length == 0) return new(null, CategoryReviewReason.NoMatch);
        var available = categories.Where(category => category.Kind == kind && !category.IsArchived &&
            category.SystemKey == keys[0]).Take(2).ToArray();
        return available.Length == 1 ? new(available[0].Id, CategoryReviewReason.Signal)
            : new(null, available.Length > 1 ? CategoryReviewReason.ConflictingSignals : CategoryReviewReason.NoMatch);
    }

    private static (CategoryKind, string, HashSet<string>) Make(CategoryKind kind, string key, string words) =>
        (kind, key, words.Split(' ').ToHashSet(StringComparer.Ordinal));

    private static IEnumerable<string> Words(string? input)
    {
        if (input is null) yield break;
        var folded = SearchText.Fold(input);
        for (var start = 0; start < folded.Length;)
        {
            while (start < folded.Length && !char.IsLetterOrDigit(folded[start])) start++;
            var end = start;
            while (end < folded.Length && char.IsLetterOrDigit(folded[end])) end++;
            if (end > start) yield return folded[start..end];
            start = end;
        }
    }
}
