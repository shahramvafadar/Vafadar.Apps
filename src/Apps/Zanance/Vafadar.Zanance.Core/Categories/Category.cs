using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Categories;

/// <summary>Whether a category classifies income or expenses.</summary>
public enum CategoryKind
{
    /// <summary>Expense category.</summary>
    Expense = 0,

    /// <summary>Income category.</summary>
    Income = 1,
}

/// <summary>How an expense category is budgeted with the flex method (§10.3, D-28).</summary>
public enum SpendingType
{
    /// <summary>Day-to-day spending that shares one flexible limit (food, leisure, shopping).</summary>
    Flexible = 0,

    /// <summary>The same kind of bill every month (rent, energy, phone); expected from the plans.</summary>
    Fixed = 1,

    /// <summary>Bills that come a few times a year (insurance); a monthly share is set aside.</summary>
    NonMonthly = 2,
}

/// <summary>
/// A category of income or expense with at most one parent (CAT-01..03).
/// Default categories have a <see cref="SystemKey"/> and a translated name until the user renames them.
/// </summary>
public sealed class Category : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the kind.</summary>
    public CategoryKind Kind { get; set; }

    /// <summary>Gets or sets the user-given name; <see langword="null"/> means "use the translated default name".</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the key of a default category (e.g. <c>Food</c>); translated as <c>Category_Food</c>.</summary>
    public string? SystemKey { get; set; }

    /// <summary>Gets or sets the parent category (one level only).</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the colour as <c>#RRGGBB</c>.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the icon key.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets how the category is budgeted with the flex method; a sub-category follows its parent.</summary>
    public SpendingType SpendingType { get; set; }

    /// <summary>Gets or sets a value indicating whether the category is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
