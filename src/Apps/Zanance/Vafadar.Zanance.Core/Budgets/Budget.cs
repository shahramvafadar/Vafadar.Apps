using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Budgets;

/// <summary>The calendar a budget period follows (§13.1).</summary>
public enum PeriodCalendar
{
    /// <summary>Gregorian months.</summary>
    Gregorian = 0,

    /// <summary>Persian (Solar Hijri) months.</summary>
    Persian = 1,
}

/// <summary>What happens with the rest of the previous month (§10.3, Phase 2A).</summary>
public enum BudgetRollover
{
    /// <summary>Every month starts with its own limit.</summary>
    None,

    /// <summary>Money not spent last month is added to this month.</summary>
    Surplus,

    /// <summary>Unspent money is added and overspending is taken from this month.</summary>
    SurplusAndDeficit,
}

/// <summary>How the limits of a budget are read (§10.3, BUD-11/12).</summary>
public enum BudgetMethod
{
    /// <summary>Limits are spending caps (Phase 1).</summary>
    Limits = 0,

    /// <summary>
    /// Category limits are envelopes: money assigned to them from the balances at hand. The budget shows what is not
    /// assigned yet; nothing is moved or spent by assigning (BUD-11).
    /// </summary>
    Envelopes = 1,

    /// <summary>
    /// Flex budgeting (D-28): fixed bills are expected from the plans, non-monthly bills get a monthly share, and the
    /// overall limit is one number for everything flexible. Category limits are not used.
    /// </summary>
    Flex = 2,
}

/// <summary>The length of a budget period (§10.3).</summary>
public enum BudgetPeriod
{
    /// <summary>A (financial) month, identified by <see cref="Budget.Year"/> and <see cref="Budget.Month"/>.</summary>
    Month = 0,

    /// <summary>Seven days from <see cref="Budget.PeriodStart"/>.</summary>
    Week = 1,

    /// <summary>Fourteen days from <see cref="Budget.PeriodStart"/> (a two-week pay cycle).</summary>
    TwoWeeks = 2,
}

/// <summary>
/// A spending budget of a month, a week or two weeks (BUD-01, §10.3). Limits are in <see cref="CurrencyCode"/> and
/// never relabelled (BUD-08).
/// </summary>
public sealed class Budget : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the period length; months are the default and the only choice in Simple mode.</summary>
    public BudgetPeriod Period { get; set; }

    /// <summary>Gets or sets the first day of a week or two-week period; unused (default) for months.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Gets or sets the year in <see cref="Calendar"/> (0 for weeks).</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month (1–12) in <see cref="Calendar"/> (0 for weeks).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the period calendar.</summary>
    public PeriodCalendar Calendar { get; set; }

    /// <summary>Gets or sets the budget currency.</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the overall limit in minor units; <see langword="null"/> = no overall limit. With the flex method it is
    /// the limit of the flexible spending only.
    /// </summary>
    public long? TotalLimit { get; set; }

    /// <summary>Gets or sets the accounts in scope; empty = all accounts included in totals with the budget currency.</summary>
    public List<Guid> AccountIds { get; set; } = [];

    /// <summary>Gets the category limits.</summary>
    public List<BudgetCategoryLimit> CategoryLimits { get; set; } = [];

    /// <summary>Gets or sets how the rest of the previous month is carried into this month (§10.3).</summary>
    public BudgetRollover Rollover { get; set; }

    /// <summary>Gets or sets how the limits are read; envelopes are optional and never forced on anyone (BUD-12).</summary>
    public BudgetMethod Method { get; set; }

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
