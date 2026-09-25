using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>The kind of a recorded financial event (D-05).</summary>
public enum EntryKind
{
    /// <summary>Money received.</summary>
    Income = 0,

    /// <summary>Money spent.</summary>
    Expense = 1,

    /// <summary>Money moved between two of the user's accounts – neither income nor expense (FIN-02).</summary>
    Transfer = 2,

    /// <summary>A purchase refund: reduces the expense of its category and increases the receiving account (REF-02).</summary>
    Refund = 3,

    /// <summary>Paying back income received: reduces income (REF-05).</summary>
    IncomeReversal = 4,

    /// <summary>A balance correction after reconciliation – outside income and expense (ACC-08).</summary>
    Adjustment = 5,
}

/// <summary>Whether the user has confirmed an entry (FIN-11).</summary>
public enum ReviewState
{
    /// <summary>Entered or confirmed by the user.</summary>
    Confirmed = 0,

    /// <summary>Recorded automatically and not yet reviewed.</summary>
    Unreviewed = 1,
}

/// <summary>Where an entry came from.</summary>
public enum EntrySource
{
    /// <summary>Entered by hand.</summary>
    Manual = 0,

    /// <summary>Created from a plan occurrence.</summary>
    Schedule = 1,

    /// <summary>Imported from a file.</summary>
    Import = 2,
}

/// <summary>Direction of an adjustment.</summary>
public enum AdjustmentDirection
{
    /// <summary>Increases the balance.</summary>
    Increase = 0,

    /// <summary>Decreases the balance.</summary>
    Decrease = 1,
}

/// <summary>
/// One recorded financial event. <see cref="Amount"/> is always a positive magnitude in the currency of
/// <see cref="AccountId"/>; <see cref="Kind"/> defines its effect (see docs/02-domain-design.md §3).
/// </summary>
public sealed class LedgerEntry : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the kind.</summary>
    public EntryKind Kind { get; set; }

    /// <summary>Gets or sets the effective date of the money movement (FIN-08).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the account (for transfers: the source).</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the amount in minor units of the account currency (&gt; 0).</summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets the destination account of a transfer.</summary>
    public Guid? ToAccountId { get; set; }

    /// <summary>Gets or sets the amount credited to the destination, in its currency (FX-03).</summary>
    public long? ToAmount { get; set; }

    /// <summary>Gets or sets the direction of an adjustment.</summary>
    public AdjustmentDirection? Direction { get; set; }

    /// <summary>Gets or sets the category (income/expense/refund/reversal only).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the short title; when empty the category name is shown (TX-02).</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the payee or payer.</summary>
    public string? Payee { get; set; }

    /// <summary>Gets or sets a free, possibly multi-line note (TX-03).</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets an item-specific icon; <see langword="null"/> uses the category icon (VIS-03).</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the review state.</summary>
    public ReviewState Review { get; set; }

    /// <summary>Gets or sets the origin.</summary>
    public EntrySource Source { get; set; }

    /// <summary>Gets or sets the original purchase amount in a foreign currency (FX-01).</summary>
    public long? OriginalAmount { get; set; }

    /// <summary>Gets or sets the currency of <see cref="OriginalAmount"/>.</summary>
    public string? OriginalCurrencyCode { get; set; }

    /// <summary>Gets or sets the purchase this refund belongs to; <see langword="null"/> = no original record (REF-04).</summary>
    public Guid? RefundOfId { get; set; }

    /// <summary>Gets or sets an id shared by related entries, e.g. a transfer and its fee.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Gets or sets the plan this entry settles.</summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>Gets or sets the original date of the settled occurrence (unique together with <see cref="ScheduleId"/>).</summary>
    public DateOnly? OccurrenceDate { get; set; }

    /// <summary>Gets or sets the import batch that created the entry.</summary>
    public Guid? ImportBatchId { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Returns the signed effect of this entry on the balance of <paramref name="accountId"/>, in its currency.</summary>
    public long EffectOn(Guid accountId)
    {
        long effect = 0;
        if (AccountId == accountId)
        {
            effect += Kind switch
            {
                EntryKind.Income or EntryKind.Refund => Amount,
                EntryKind.Expense or EntryKind.IncomeReversal or EntryKind.Transfer => -Amount,
                EntryKind.Adjustment => Direction == AdjustmentDirection.Decrease ? -Amount : Amount,
                _ => 0,
            };
        }

        if (Kind == EntryKind.Transfer && ToAccountId == accountId)
        {
            effect += ToAmount ?? Amount;
        }

        return effect;
    }
}
