using Vafadar.Core.Domain;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Plans;

/// <summary>Whether a plan's amount is known (REC-03). Unknown is not zero.</summary>
public enum AmountMode
{
    /// <summary>The amount is fixed; only fixed plans may post automatically (REC-19).</summary>
    Fixed = 0,

    /// <summary>The amount is an estimate and is confirmed per occurrence.</summary>
    Estimated = 1,

    /// <summary>The amount is not known yet; forecasts are marked incomplete (FOR-05).</summary>
    Unknown = 2,
}

/// <summary>State of a plan as a whole – independent of the state of its occurrences (REC-13, REC-16).</summary>
public enum ScheduleState
{
    /// <summary>Occurrences are due as scheduled.</summary>
    Active = 0,

    /// <summary>Temporarily stopped from <see cref="Schedule.PausedFrom"/>; settled history is kept.</summary>
    Paused = 1,

    /// <summary>Ended; no occurrences after <see cref="Schedule.ActiveUntil"/>.</summary>
    Ended = 2,
}

/// <summary>
/// A planned income, expense or transfer (REC-01). A plan is not a ledger entry: it never changes a balance until one
/// of its occurrences is settled (AT-16). A plan owns the slice [<see cref="ActiveFrom"/>, <see cref="ActiveUntil"/>]
/// of its rule; "this and future" edits split the series into two plans sharing the same anchor (REC-15).
/// </summary>
public sealed class Schedule : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name shown in lists and used as the title of settled entries.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the kind: <see cref="EntryKind.Income"/>, <see cref="EntryKind.Expense"/> or <see cref="EntryKind.Transfer"/>.</summary>
    public EntryKind Kind { get; set; } = EntryKind.Expense;

    /// <summary>Gets or sets the account (for transfers: the source).</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the destination of a transfer.</summary>
    public Guid? ToAccountId { get; set; }

    /// <summary>Gets or sets the category of income and expense plans.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets whether the amount is fixed, estimated or unknown.</summary>
    public AmountMode AmountMode { get; set; }

    /// <summary>Gets or sets the amount in minor units of the account currency; <see langword="null"/> when unknown.</summary>
    public long? Amount { get; set; }

    /// <summary>Gets or sets the destination amount of a transfer between currencies.</summary>
    public long? ToAmount { get; set; }

    /// <summary>Gets or sets a note.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets an icon key; <see langword="null"/> uses the category icon.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the recurrence rule.</summary>
    public RecurrenceRule Rule { get; set; } = new();

    /// <summary>Gets or sets the first original date this plan owns; <see langword="null"/> = from the rule start.</summary>
    public DateOnly? ActiveFrom { get; set; }

    /// <summary>Gets or sets the last original date this plan owns; <see langword="null"/> = until the rule ends.</summary>
    public DateOnly? ActiveUntil { get; set; }

    /// <summary>Gets or sets the plan state.</summary>
    public ScheduleState State { get; set; }

    /// <summary>Gets or sets the first original date that is paused.</summary>
    public DateOnly? PausedFrom { get; set; }

    /// <summary>Gets or sets the plan this one continues after a "this and future" edit or a resume.</summary>
    public Guid? PreviousScheduleId { get; set; }

    /// <summary>Gets or sets a value indicating whether due occurrences are recorded automatically (REC-19, REC-20).</summary>
    public bool AutoPost { get; set; }

    /// <summary>
    /// Gets or sets the first date automatic posting applies to. Occurrences before it are never posted
    /// automatically, so enabling auto-post never creates a batch of old entries (REC-07).
    /// </summary>
    public DateOnly? AutoPostFrom { get; set; }

    /// <summary>Gets or sets a value indicating whether a reminder is scheduled (REM-01).</summary>
    public bool ReminderEnabled { get; set; }

    /// <summary>Gets or sets how many days before the due date the reminder fires.</summary>
    public int ReminderDaysBefore { get; set; } = 3;

    /// <summary>Gets or sets the local time of the reminder.</summary>
    public TimeOnly ReminderTime { get; set; } = new(9, 0);

    /// <summary>Gets or sets the contract partner, e.g. the provider of a subscription (F2-CON-01).</summary>
    public string? ContractProvider { get; set; }

    /// <summary>Gets or sets the contract or customer number.</summary>
    public string? ContractReference { get; set; }

    /// <summary>Gets or sets the end of the current contract term.</summary>
    public DateOnly? ContractEnd { get; set; }

    /// <summary>Gets or sets a value indicating whether the contract renews automatically at <see cref="ContractEnd"/>.</summary>
    public bool ContractRenews { get; set; }

    /// <summary>
    /// Gets or sets the last day to cancel. It is not a payment due date, and the app only reminds; it never marks a
    /// contract as cancelled by itself (F2-CON-01, F2-CON-03).
    /// </summary>
    public DateOnly? CancellationDeadline { get; set; }

    /// <summary>Gets or sets a date to review the contract, e.g. to compare prices.</summary>
    public DateOnly? ReviewDate { get; set; }

    /// <summary>Gets a value indicating whether any contract detail is set.</summary>
    public bool HasContract => ContractProvider is not null || ContractReference is not null || ContractEnd is not null
        || CancellationDeadline is not null || ReviewDate is not null;

    /// <summary>
    /// Gets or sets a value indicating whether a second reminder fires on the due date itself, at the same time
    /// (REM-01, Advanced mode). It has no effect when the first reminder is already on the due date.
    /// </summary>
    public bool ReminderOnDueDate { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Returns whether <paramref name="originalDate"/> belongs to this plan's slice of the rule.</summary>
    public bool Owns(DateOnly originalDate) =>
        (ActiveFrom is not { } from || originalDate >= from)
        && (ActiveUntil is not { } until || originalDate <= until)
        && !(State == ScheduleState.Paused && PausedFrom is { } paused && originalDate >= paused);
}

/// <summary>Stored status of an occurrence. Future, due and overdue are derived and never stored (§6).</summary>
public enum OccurrenceStatus
{
    /// <summary>Not settled yet (possibly with a moved due date or a changed amount).</summary>
    Open = 0,

    /// <summary>Settled by an entry.</summary>
    Settled = 1,

    /// <summary>Skipped by the user; does not add an occurrence at the end (REC-06, AT-27).</summary>
    Skipped = 2,
}

/// <summary>
/// What happened to one occurrence of a plan. Rows exist only for occurrences the user or the app changed; all other
/// occurrences are open with the plan's values. Unique per (plan, original date) (D-06).
/// </summary>
public sealed class OccurrenceState : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the plan.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the date computed by the rule; identifies the occurrence.</summary>
    public DateOnly OriginalDate { get; set; }

    /// <summary>Gets or sets the stored status.</summary>
    public OccurrenceStatus Status { get; set; }

    /// <summary>Gets or sets a moved due date for this occurrence only (REC-14, AT-26).</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Gets or sets a changed amount for this occurrence only.</summary>
    public long? Amount { get; set; }

    /// <summary>Gets or sets the settling entry.</summary>
    public Guid? EntryId { get; set; }

    /// <summary>Gets or sets the sum of partial payments recorded for this occurrence (F2-TX-02).</summary>
    public long PaidAmount { get; set; }

    /// <summary>Gets or sets a note.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic posting must leave this occurrence alone, e.g. after the user
    /// undid or deleted an automatic entry (REC-18, AT-31).
    /// </summary>
    public bool AutoPostSuppressed { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
