using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Ledger;

/// <summary>
/// A named combination of transaction list filters (§21.5, REP-08). A relative period (this month, last month, this
/// year, all) moves with the calendar; a custom range is kept as dates. Applying a filter never changes entries.
/// </summary>
public sealed class SavedFilter : Entity, IAuditableEntity
{
    /// <summary>The longest name.</summary>
    public const int MaxNameLength = 60;

    /// <summary>Gets or sets the user-given name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the period chip (0 this month, 1 last month, 2 this year, 3 all); ignored with a custom range.</summary>
    public int Period { get; set; }

    /// <summary>Gets or sets the first day of a custom range.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Gets or sets the last day of a custom range.</summary>
    public DateOnly? To { get; set; }

    /// <summary>Gets or sets the kind chip (0 all, 1 expenses, 2 income, 3 transfers).</summary>
    public int Kind { get; set; }

    /// <summary>Gets or sets the account, or <see langword="null"/> for all accounts.</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the categories (a main category with its sub-categories, or those of a report drill-down).</summary>
    public List<Guid> CategoryIds { get; set; } = [];

    /// <summary>Gets or sets the label of the categories, e.g. the drill-down it came from.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Gets or sets a value indicating whether only accounts included in totals count, as in a report drill-down.</summary>
    public bool InTotalsOnly { get; set; }

    /// <summary>Gets or sets a value indicating whether only unreviewed entries are shown.</summary>
    public bool UnreviewedOnly { get; set; }

    /// <summary>Gets or sets the search text (words, #tags, amounts).</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets a value indicating whether the filter uses a custom date range.</summary>
    public bool HasCustomRange => From is not null && To is not null;
}
