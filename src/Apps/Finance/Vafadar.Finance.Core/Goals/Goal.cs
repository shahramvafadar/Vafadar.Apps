using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Goals;

/// <summary>Order in which goals keep their funding when an account balance falls (F2-GOAL-05).</summary>
public enum GoalPriority
{
    /// <summary>Loses funding last.</summary>
    High,

    /// <summary>The default.</summary>
    Normal,

    /// <summary>Loses funding first.</summary>
    Low,
}

/// <summary>How often the user wants to set money aside; it drives the suggested contribution (F2-GOAL-04).</summary>
public enum ContributionFrequency
{
    /// <summary>Once per month.</summary>
    Monthly,

    /// <summary>Once per week.</summary>
    Weekly,
}

/// <summary>Life cycle of a goal.</summary>
public enum GoalState
{
    /// <summary>Money is being set aside.</summary>
    Active,

    /// <summary>The goal was reached or spent; its history stays.</summary>
    Completed,

    /// <summary>Hidden; its history stays.</summary>
    Archived,
}

/// <summary>
/// A savings goal such as an emergency fund, a trip or a yearly insurance (F2-GOAL-01). Money is earmarked for it with
/// <see cref="GoalAllocation"/>s; earmarking is neither a transfer nor an expense (F2-GOAL-03, BUD-11).
/// </summary>
public sealed class Goal : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the target amount in minor units of <see cref="CurrencyCode"/>.</summary>
    public long TargetAmount { get; set; }

    /// <summary>Gets or sets the currency; only accounts in this currency can fund the goal.</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Gets or sets the date the money is needed; <see langword="null"/> for an open-ended goal.</summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>Gets or sets the icon key.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the priority.</summary>
    public GoalPriority Priority { get; set; } = GoalPriority.Normal;

    /// <summary>Gets or sets how often money is set aside.</summary>
    public ContributionFrequency Frequency { get; set; }

    /// <summary>Gets or sets the state.</summary>
    public GoalState State { get; set; }

    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Money earmarked for a goal in one account (positive) or released again (negative). It never changes an account
/// balance: moving money to a savings account is a transfer entry, spending it is an expense entry (F2-GOAL-03).
/// </summary>
public sealed class GoalAllocation : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the goal.</summary>
    public Guid GoalId { get; set; }

    /// <summary>Gets or sets the account whose money is earmarked.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the amount in minor units; negative releases an earlier allocation.</summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets the date of the allocation.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets an optional note.</summary>
    public string? Note { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
