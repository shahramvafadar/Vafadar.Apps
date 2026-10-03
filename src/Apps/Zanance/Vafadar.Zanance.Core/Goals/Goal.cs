using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Goals;

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

/// <summary>
/// Life cycle of a goal (ZEX-P13). Only these states are stored; *reached* and *overdue* are derived from the numbers, so
/// a withdrawal after reaching a goal shows the real state again. New values are appended, never renumbered.
/// </summary>
public enum GoalState
{
    /// <summary>Money is being set aside.</summary>
    Active,

    /// <summary>The goal was reached or spent; its history stays.</summary>
    Completed,

    /// <summary>Hidden; its history stays.</summary>
    Archived,

    /// <summary>On hold: kept with its data, left out of Home and of suggestions until resumed.</summary>
    Paused,
}

/// <summary>What a goal measures (ZEX-D07). New values are appended, never renumbered.</summary>
public enum GoalType
{
    /// <summary>Money set aside: earmarks in accounts of the goal currency (<see cref="GoalAllocation"/>); reserves money.</summary>
    Earmark = 0,

    /// <summary>The recorded balance of one money account; observes, reserves nothing.</summary>
    AccountBalance = 1,
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

    /// <summary>Gets or sets what the goal measures (ZEX-D07); existing goals are <see cref="GoalType.Earmark"/>.</summary>
    public GoalType Type { get; set; }

    /// <summary>Gets or sets the account a <see cref="GoalType.AccountBalance"/> goal follows; <see langword="null"/> otherwise.</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the position on Home (1 or 2), or <see langword="null"/> when the goal is not shown there (ZEX-GO06).</summary>
    public int? HomePin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the money set aside counts as protected, so the liquidity headroom keeps it
    /// out (ZEX-P16). Only <see cref="GoalType.Earmark"/> goals can be protected; a balance goal reserves nothing.
    /// </summary>
    public bool Protect { get; set; }

    /// <summary>Gets or sets when the goal was paused; <see langword="null"/> unless <see cref="GoalState.Paused"/>.</summary>
    public DateTimeOffset? PausedAt { get; set; }

    /// <summary>Gets or sets when the user marked the goal completed; the history stays.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>How a contribution plan suggests the amount (ZEX-GO08). New values are appended, never renumbered.</summary>
public enum ContributionMethod
{
    /// <summary>A fixed amount on each contribution date.</summary>
    FixedAmount = 0,

    /// <summary>A share of the eligible income of the last closed period.</summary>
    ShareOfIncome = 1,

    /// <summary>Spending less in some categories each financial month; budgets change only on explicit confirmation.</summary>
    SpendingCut = 2,
}

/// <summary>
/// The user's plan to reach a goal (ZEX-GO08..GO11): a method, an amount or percentage and a contribution schedule on
/// the recurrence engine (weekly, every two weeks, monthly, Gregorian or Persian calendar). It is a plan for the user –
/// Zanance moves no money and changes no budget without confirmation. One plan per goal.
/// </summary>
public sealed class ContributionPlan : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the goal.</summary>
    public Guid GoalId { get; set; }

    /// <summary>Gets or sets the method.</summary>
    public ContributionMethod Method { get; set; }

    /// <summary>Gets or sets the amount per date (fixed amount) or per financial month (spending cut), in minor units.</summary>
    public long? Amount { get; set; }

    /// <summary>Gets or sets the share of eligible income in percent (share of income).</summary>
    public decimal? Percent { get; set; }

    /// <summary>Gets or sets the contribution dates.</summary>
    public Plans.RecurrenceRule Rule { get; set; } = new();

    /// <summary>Gets or sets the categories of a spending cut.</summary>
    public List<Guid> CategoryIds { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether a reminder is shown on each contribution date.</summary>
    public bool ReminderEnabled { get; set; }

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
