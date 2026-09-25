using Vafadar.Finance.Core.Budgets;

namespace Vafadar.Finance.Core.Plans;

/// <summary>How often a plan repeats (REC-04).</summary>
public enum Frequency
{
    /// <summary>A single future payment (REC-02).</summary>
    Once = 0,

    /// <summary>Every N days.</summary>
    Daily = 1,

    /// <summary>Every N weeks (e.g. every 2 weeks – not "twice a month", REC-05).</summary>
    Weekly = 2,

    /// <summary>Every N months of the rule's calendar.</summary>
    Monthly = 3,

    /// <summary>Every N years of the rule's calendar.</summary>
    Yearly = 4,
}

/// <summary>Which day of the month a monthly or yearly rule uses (REC-08).</summary>
public enum MonthDayRule
{
    /// <summary>The day of the start date (the anchor).</summary>
    SpecificDay = 0,

    /// <summary>Always the last day of the month.</summary>
    LastDayOfMonth = 1,
}

/// <summary>What happens when the anchor day does not exist in a month, e.g. the 31st in April (REC-08, REC-10).</summary>
public enum MissingDayPolicy
{
    /// <summary>Use the last valid day of that month (default).</summary>
    LastValidDay = 0,

    /// <summary>No occurrence in that month.</summary>
    Skip = 1,
}

/// <summary>How a plan ends (REC-06).</summary>
public enum EndKind
{
    /// <summary>No end.</summary>
    Never = 0,

    /// <summary>No occurrence after <see cref="RecurrenceRule.EndDate"/>.</summary>
    OnDate = 1,

    /// <summary>After <see cref="RecurrenceRule.Count"/> scheduled occurrences; skipping one does not add another.</summary>
    AfterCount = 2,
}

/// <summary>
/// A recurrence rule (docs/02-domain-design.md §5). The calendar is fixed when the plan is created, so changing the
/// display calendar never moves a due date (REC-11, AT-23).
/// </summary>
public sealed class RecurrenceRule
{
    /// <summary>Gets or sets the frequency.</summary>
    public Frequency Frequency { get; set; }

    /// <summary>Gets or sets the interval N (≥ 1).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>Gets or sets the first due date; it is also the anchor every occurrence is computed from (REC-09).</summary>
    public DateOnly Start { get; set; }

    /// <summary>Gets or sets the calendar monthly and yearly steps are counted in.</summary>
    public PeriodCalendar Calendar { get; set; }

    /// <summary>Gets or sets the day rule of monthly and yearly plans.</summary>
    public MonthDayRule DayRule { get; set; }

    /// <summary>Gets or sets what happens when the anchor day does not exist.</summary>
    public MissingDayPolicy MissingDay { get; set; }

    /// <summary>Gets or sets the end kind.</summary>
    public EndKind End { get; set; }

    /// <summary>Gets or sets the last possible date for <see cref="EndKind.OnDate"/>.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the number of occurrences for <see cref="EndKind.AfterCount"/>.</summary>
    public int? Count { get; set; }

    /// <summary>Returns a copy.</summary>
    public RecurrenceRule Clone() => (RecurrenceRule)MemberwiseClone();
}
