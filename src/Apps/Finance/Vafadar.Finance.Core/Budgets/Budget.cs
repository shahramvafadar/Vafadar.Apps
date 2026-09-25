using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Budgets;

/// <summary>The calendar a budget period follows (§13.1).</summary>
public enum PeriodCalendar
{
    /// <summary>Gregorian months.</summary>
    Gregorian = 0,

    /// <summary>Persian (Solar Hijri) months.</summary>
    Persian = 1,
}

/// <summary>
/// A monthly spending budget (BUD-01). Limits are in <see cref="CurrencyCode"/> and never relabelled (BUD-08).
/// </summary>
public sealed class Budget : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the year in <see cref="Calendar"/>.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month (1–12) in <see cref="Calendar"/>.</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the period calendar.</summary>
    public PeriodCalendar Calendar { get; set; }

    /// <summary>Gets or sets the budget currency.</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Gets or sets the overall limit in minor units; <see langword="null"/> = no overall limit.</summary>
    public long? TotalLimit { get; set; }

    /// <summary>Gets or sets the accounts in scope; empty = all accounts included in totals with the budget currency.</summary>
    public List<Guid> AccountIds { get; set; } = [];

    /// <summary>Gets the category limits.</summary>
    public List<BudgetCategoryLimit> CategoryLimits { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the 80 %/100 % alerts are enabled (BUD-06).</summary>
    public bool AlertsEnabled { get; set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A limit for one category within a budget (BUD-02).</summary>
public sealed class BudgetCategoryLimit
{
    /// <summary>Gets or sets the category.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Gets or sets the limit in minor units (0 = "do not spend", BUD-05).</summary>
    public long Limit { get; set; }
}
