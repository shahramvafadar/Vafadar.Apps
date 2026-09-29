using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Plans;

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

    /// <summary>
    /// The same weekday and week of the month as the start date, e.g. the second Monday (REC-12). A start in the fifth
    /// week means the last such weekday, as not every month has a fifth one.
    /// </summary>
    NthWeekday = 2,

    /// <summary>The last weekday of the month that the start date falls on, e.g. the last Friday (REC-12).</summary>
    LastWeekday = 3,
}

/// <summary>Helpers for weekday rules.</summary>
public static class MonthDayRules
{
    /// <summary>Returns the week of the month (1–5) of a day of the month: days 1–7 are the first week.</summary>
    public static int WeekOf(int dayOfMonth) => ((dayOfMonth - 1) / 7) + 1;

    /// <summary>Returns whether the rule picks a weekday rather than a day number.</summary>
    public static bool IsWeekday(this MonthDayRule rule) => rule is MonthDayRule.NthWeekday or MonthDayRule.LastWeekday;
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

/// <summary>What happens with a due date on a weekend day (F2-CON-05).</summary>
public enum WeekendShift
{
    /// <summary>The due date stays.</summary>
    None,

    /// <summary>The last working day before the weekend.</summary>
    Before,

    /// <summary>The first working day after the weekend.</summary>
    After,
}

/// <summary>
/// A recurrence rule (docs/02-domain-design.md §5). The calendar is fixed when the plan is created, so changing the
/// display calendar never moves a due date (REC-11, AT-23).
/// </summary>
public sealed class RecurrenceRule
{
    /// <summary>
    /// Gets or sets what happens when a due date falls on a weekend day (F2-CON-05), and with
    /// <see cref="HolidayRegion"/> also on a public holiday. Bank business days are not claimed.
    /// </summary>
    public WeekendShift WeekendShift { get; set; }

    /// <summary>
    /// Gets or sets the region (ISO 3166, e.g. <c>DE</c> or <c>IR</c>) whose public holidays count like weekend days
    /// (<see cref="PublicHolidays"/>, D-29); <see langword="null"/> = weekends only. Fixed when the plan is saved.
    /// </summary>
    public string? HolidayRegion { get; set; }

    /// <summary>Gets or sets the weekend days as a bit mask (bit n = <see cref="DayOfWeek"/> n), fixed when the plan is saved.</summary>
    public int WeekendDays { get; set; }

    /// <summary>Returns the due date of an occurrence scheduled on <paramref name="date"/> after the weekend and holiday rule.</summary>
    public DateOnly ApplyWeekend(DateOnly date)
    {
        var holidays = PublicHolidays.IsSupported(HolidayRegion);
        if (WeekendShift == WeekendShift.None || (WeekendDays == 0 && !holidays) || (WeekendDays & 0x7F) == 0x7F)
        {
            return date;
        }

        // Moves over weekend days and holidays together, e.g. from a Friday holiday over the weekend to Monday. The
        // limit only guards against a calendar without working days.
        var step = WeekendShift == WeekendShift.Before ? -1 : 1;
        for (var i = 0; i < 31 && ((WeekendDays & (1 << (int)date.DayOfWeek)) != 0 || (holidays && PublicHolidays.IsHoliday(HolidayRegion, date))); i++)
        {
            date = date.AddDays(step);
        }

        return date;
    }

    /// <summary>Returns the bit mask for weekend days.</summary>
    public static int MaskOf(IEnumerable<DayOfWeek> days) => days.Aggregate(0, (mask, day) => mask | (1 << (int)day));

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

    /// <summary>
    /// Gets or sets a second day of the month for monthly plans, e.g. salary on the 1st and the 15th (Phase 2A). Each day
    /// is its own occurrence; a day missing in a month follows <see cref="MissingDay"/>.
    /// </summary>
    public int? SecondDay { get; set; }

    /// <summary>Gets or sets the end kind.</summary>
    public EndKind End { get; set; }

    /// <summary>Gets or sets the last possible date for <see cref="EndKind.OnDate"/>.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the number of occurrences for <see cref="EndKind.AfterCount"/>.</summary>
    public int? Count { get; set; }

    /// <summary>Returns a copy.</summary>
    public RecurrenceRule Clone() => (RecurrenceRule)MemberwiseClone();
}
