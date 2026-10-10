using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>A detached, in-memory handoff of the transaction fields supported by a new plan.</summary>
public sealed record EntryPlanDraft
{
    /// <summary>Gets the expense, income or transfer kind.</summary>
    public required EntryKind Kind { get; init; }
    /// <summary>Gets the source account identity.</summary>
    public required Guid AccountId { get; init; }
    /// <summary>Gets the ISO currency in which Amount was parsed; changing accounts cannot reinterpret it.</summary>
    public required string CurrencyCode { get; init; }
    /// <summary>Gets the positive parsed source minor-unit amount.</summary>
    public required long Amount { get; init; }
    /// <summary>Gets the selected first due date, without shifting an unpaid draft into another month.</summary>
    public required DateOnly Date { get; init; }
    /// <summary>Gets the entered title or its displayed category fallback.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the original category for expense or income.</summary>
    public Guid? CategoryId { get; init; }
    /// <summary>Gets the note supported by the plan editor.</summary>
    public string? Note { get; init; }
    /// <summary>Gets the transfer destination identity.</summary>
    public Guid? ToAccountId { get; init; }
    /// <summary>Gets the destination ISO currency where a transfer is copied.</summary>
    public string? ToCurrencyCode { get; init; }
    /// <summary>Gets the parsed destination minor-unit amount of a cross-currency transfer.</summary>
    public long? ToAmount { get; init; }

    /// <summary>Creates a new unsaved monthly schedule anchored in the named rule calendar, without posting money.</summary>
    public Schedule CreateSchedule(PeriodCalendar calendar)
    {
        if (Kind is not (EntryKind.Expense or EntryKind.Income or EntryKind.Transfer)
            || AccountId == Guid.Empty || Amount <= 0 || Date == default || string.IsNullOrWhiteSpace(Name)
            || string.IsNullOrWhiteSpace(CurrencyCode)
            || (Kind == EntryKind.Transfer && (ToAccountId is null || ToAccountId == Guid.Empty || ToAccountId == AccountId
                || string.IsNullOrWhiteSpace(ToCurrencyCode)
                || (CurrencyCode != ToCurrencyCode && ToAmount is null or <= 0))))
        { throw new InvalidOperationException("The entry plan draft must contain validated supported fields."); }

        // D-111: opening Repeat is a detached draft. Auto-post and reminders remain off until explicitly chosen.
        return new Schedule
        {
            Name = Name, Kind = Kind, AccountId = AccountId, Amount = Amount, AmountMode = AmountMode.Fixed,
            CategoryId = Kind == EntryKind.Transfer ? null : CategoryId,
            ToAccountId = Kind == EntryKind.Transfer ? ToAccountId : null,
            ToAmount = Kind == EntryKind.Transfer ? ToAmount : null, Note = Note,
            Rule = new RecurrenceRule { Start = Date, Calendar = calendar, Frequency = Frequency.Monthly },
        };
    }
}
