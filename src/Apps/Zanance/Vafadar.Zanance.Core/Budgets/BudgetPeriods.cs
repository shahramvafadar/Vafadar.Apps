namespace Vafadar.Zanance.Core.Budgets;

/// <summary>Weekly and two-week budget periods next to the (financial) months (§10.3).</summary>
public static class BudgetPeriods
{
    /// <summary>Returns the number of days of a week period; months have no fixed length.</summary>
    public static int Days(BudgetPeriod period) => period switch
    {
        BudgetPeriod.Week => 7,
        BudgetPeriod.TwoWeeks => 14,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Months have no fixed length."),
    };

    /// <summary>Returns the first and last day of a budget.</summary>
    /// <param name="startDay">The day financial months start on (months only).</param>
    public static (DateOnly First, DateOnly Last) Range(Budget budget, int startDay)
    {
        ArgumentNullException.ThrowIfNull(budget);
        return budget.Period == BudgetPeriod.Month
            ? PeriodMath.MonthRange(budget.Year, budget.Month, budget.Calendar, startDay)
            : Range(budget.Period, budget.PeriodStart);
    }

    /// <summary>Returns the first and last day of a week period starting on <paramref name="start"/>.</summary>
    public static (DateOnly First, DateOnly Last) Range(BudgetPeriod period, DateOnly start) => (start, start.AddDays(Days(period) - 1));

    /// <summary>
    /// Returns the first day of the period that contains <paramref name="date"/>: the week begins on
    /// <paramref name="firstDayOfWeek"/>; two-week periods repeat every 14 days from <paramref name="fortnightStart"/>
    /// (or from the start of that week when none is set).
    /// </summary>
    public static DateOnly StartOf(BudgetPeriod period, DateOnly date, DayOfWeek firstDayOfWeek, DateOnly? fortnightStart = null)
    {
        var weekStart = date.AddDays(-(((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7));
        switch (period)
        {
            case BudgetPeriod.Week:
                return weekStart;
            case BudgetPeriod.TwoWeeks:
                var anchor = fortnightStart ?? weekStart;
                var offset = date.DayNumber - anchor.DayNumber;
                return anchor.AddDays((int)Math.Floor(offset / 14d) * 14);
            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, "Months are found with PeriodMath.MonthOf.");
        }
    }

    /// <summary>Returns the start of the period before or after the one starting on <paramref name="start"/>.</summary>
    public static DateOnly Step(BudgetPeriod period, DateOnly start, int count) => start.AddDays(Days(period) * count);

    /// <summary>
    /// Returns the last <paramref name="count"/> complete periods before (year, month) or before the week period starting
    /// on <paramref name="start"/>, newest first.
    /// </summary>
    public static IReadOnlyList<(DateOnly First, DateOnly Last)> Before(
        BudgetPeriod period,
        int year,
        int month,
        DateOnly start,
        PeriodCalendar calendar,
        int startDay,
        int count)
    {
        var periods = new List<(DateOnly First, DateOnly Last)>(count);
        var (y, m) = (year, month);
        for (var i = 1; i <= count; i++)
        {
            if (period == BudgetPeriod.Month)
            {
                (y, m) = PeriodMath.Previous(y, m);
                periods.Add(PeriodMath.MonthRange(y, m, calendar, startDay));
            }
            else
            {
                periods.Add(Range(period, Step(period, start, -i)));
            }
        }

        return periods;
    }
}
