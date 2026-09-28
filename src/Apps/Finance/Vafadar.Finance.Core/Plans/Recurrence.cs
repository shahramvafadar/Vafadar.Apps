using System.Globalization;
using Vafadar.Finance.Core.Budgets;

namespace Vafadar.Finance.Core.Plans;

/// <summary>One scheduled date of a rule. <see cref="Number"/> counts generated occurrences from 1.</summary>
public readonly record struct ScheduledDate(int Number, DateOnly Date);

/// <summary>Computes the dates of a <see cref="RecurrenceRule"/> (REC-03..11).</summary>
public static class Recurrence
{
    // Guards against endless loops with rules that never produce a date in range (e.g. Skip on day 31 every 12 months
    // starting in a 30-day month would be valid forever, but a broken rule must not hang the app).
    private const int MaxSteps = 100_000;

    private static readonly PersianCalendar Persian = new();

    /// <summary>Returns a validation problem of the rule, or <see langword="null"/> when it is valid.</summary>
    public static string? Validate(RecurrenceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.Interval < 1)
        {
            return "IntervalMustBePositive";
        }

        if (rule.SecondDay is { } second && (rule.Frequency != Frequency.Monthly || second is < 1 or > 31 || rule.DayRule.IsWeekday()))
        {
            return "SecondDayInvalid";
        }

        return rule.End switch
        {
            EndKind.OnDate when rule.EndDate is not { } end || end < rule.Start => "EndBeforeStart",
            EndKind.AfterCount when rule.Count is not > 0 => "CountMustBePositive",
            _ => null,
        };
    }

    /// <summary>
    /// Returns the scheduled dates between <paramref name="from"/> and <paramref name="to"/> (inclusive), in order.
    /// Every date is computed from the anchor, never from the previous date, so a short month does not shift later
    /// occurrences (REC-09, AT-19).
    /// </summary>
    public static IEnumerable<ScheduledDate> Between(RecurrenceRule rule, DateOnly from, DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (Validate(rule) is not null || to < from)
        {
            yield break;
        }

        var number = 0;
        for (var step = 0; step < MaxSteps; step++)
        {
            var dates = Candidates(rule, step);
            if (dates.Count == 0)
            {
                // The anchor day does not exist in this month and the policy is Skip (AT-20).
                if (StepBeyond(rule, step, to))
                {
                    yield break;
                }

                continue;
            }

            foreach (var date in dates)
            {
                if (date > to || (rule.End == EndKind.OnDate && date > rule.EndDate))
                {
                    yield break;
                }

                number++;
                if (rule.End == EndKind.AfterCount && number > rule.Count)
                {
                    yield break;
                }

                if (date >= from)
                {
                    yield return new ScheduledDate(number, date);
                }
            }

            if (rule.Frequency == Frequency.Once)
            {
                yield break;
            }
        }
    }

    /// <summary>Returns the next <paramref name="count"/> dates on or after <paramref name="from"/> (plan preview, REC-04).</summary>
    public static IReadOnlyList<ScheduledDate> Next(RecurrenceRule rule, DateOnly from, int count) =>
        [.. Between(rule, from, DateOnly.MaxValue).Take(count)];

    /// <summary>Returns how many times per year the rule occurs, for monthly equivalents (BUD-09).</summary>
    public static decimal PerYear(RecurrenceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var interval = Math.Max(1, rule.Interval);
        return rule.Frequency switch
        {
            Frequency.Daily => 365.25m / interval,
            Frequency.Weekly => 365.25m / 7 / interval,
            Frequency.Monthly => (rule.SecondDay is null ? 12m : 24m) / interval,
            Frequency.Yearly => 1m / interval,
            _ => 0,
        };
    }

    // The dates of one step: one, or for a monthly plan with a second day up to two, in order and without duplicates
    // (both days can fall on the same last day of a short month). The first date of the plan is its start.
    private static List<DateOnly> Candidates(RecurrenceRule rule, int step)
    {
        var dates = new List<DateOnly>(2);
        if (Candidate(rule, step) is { } main)
        {
            dates.Add(main);
        }

        if (rule.Frequency == Frequency.Monthly && rule.SecondDay is { } second)
        {
            var (year, month) = MonthOf(rule.Start, rule.Calendar);
            var total = (year * 12) + (month - 1) + (step * rule.Interval);
            if (DayIn(rule, total / 12, (total % 12) + 1, second) is { } extra && extra >= rule.Start && !dates.Contains(extra))
            {
                dates.Add(extra);
            }
        }

        dates.Sort();
        return dates;
    }

    private static DateOnly? Candidate(RecurrenceRule rule, int step)
    {
        var n = step * rule.Interval;
        switch (rule.Frequency)
        {
            case Frequency.Once:
                return step == 0 ? rule.Start : null;
            case Frequency.Daily:
                return AddDays(rule.Start, n);
            case Frequency.Weekly:
                return AddDays(rule.Start, n * 7L);
            case Frequency.Monthly:
            {
                var (year, month) = MonthOf(rule.Start, rule.Calendar);
                var total = (year * 12) + (month - 1) + n;
                return DayIn(rule, total / 12, (total % 12) + 1);
            }

            default:
            {
                var (year, month) = MonthOf(rule.Start, rule.Calendar);
                return DayIn(rule, year + n, month);
            }
        }
    }

    private static DateOnly? AddDays(DateOnly start, long days) =>
        days > DateOnly.MaxValue.DayNumber - start.DayNumber ? null : start.AddDays((int)days);

    private static DateOnly? DayIn(RecurrenceRule rule, int year, int month, int? fixedDay = null)
    {
        if (!IsSupportedYear(year, rule.Calendar))
        {
            return null;
        }

        var daysInMonth = rule.Calendar == PeriodCalendar.Persian ? Persian.GetDaysInMonth(year, month) : DateTime.DaysInMonth(year, month);
        var anchorDay = fixedDay ?? DayOf(rule.Start, rule.Calendar);
        int day;
        if (rule.DayRule.IsWeekday() && fixedDay is null)
        {
            day = WeekdayIn(rule, year, month, daysInMonth, anchorDay);
        }
        else if (rule.DayRule == MonthDayRule.LastDayOfMonth && fixedDay is null)
        {
            day = daysInMonth;
        }
        else if (anchorDay <= daysInMonth)
        {
            day = anchorDay;
        }
        else if (rule.MissingDay == MissingDayPolicy.LastValidDay)
        {
            day = daysInMonth;
        }
        else
        {
            return null;
        }

        return rule.Calendar == PeriodCalendar.Persian
            ? DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0))
            : new DateOnly(year, month, day);
    }

    // The day of the month with the weekday of the start date: in the same week of the month, or the last one (REC-12).
    // Weekdays are the same in both calendars, so the month's first day gives the offset.
    private static int WeekdayIn(RecurrenceRule rule, int year, int month, int daysInMonth, int anchorDay)
    {
        var first = rule.Calendar == PeriodCalendar.Persian
            ? DateOnly.FromDateTime(Persian.ToDateTime(year, month, 1, 0, 0, 0, 0))
            : new DateOnly(year, month, 1);
        var weekday = rule.Start.DayOfWeek;
        var firstMatch = 1 + (((int)weekday - (int)first.DayOfWeek + 7) % 7);
        var week = MonthDayRules.WeekOf(anchorDay);
        if (rule.DayRule == MonthDayRule.NthWeekday && week <= 4)
        {
            return firstMatch + ((week - 1) * 7);
        }

        return firstMatch + (((daysInMonth - firstMatch) / 7) * 7);
    }

    // A skipped month still has a first day; once that is past the range end, no later step can be inside it.
    private static bool StepBeyond(RecurrenceRule rule, int step, DateOnly to)
    {
        if (rule.Frequency is not (Frequency.Monthly or Frequency.Yearly))
        {
            return true;
        }

        var (year, month) = MonthOf(rule.Start, rule.Calendar);
        var n = step * rule.Interval;
        if (rule.Frequency == Frequency.Monthly)
        {
            var total = (year * 12) + (month - 1) + n;
            (year, month) = (total / 12, (total % 12) + 1);
        }
        else
        {
            year += n;
        }

        if (!IsSupportedYear(year, rule.Calendar))
        {
            return true;
        }

        return PeriodMath.MonthRange(year, month, rule.Calendar).First > to;
    }

    private static bool IsSupportedYear(int year, PeriodCalendar calendar) =>
        calendar == PeriodCalendar.Persian ? year is >= 1 and <= 9377 : year is >= 1 and <= 9999;

    private static (int Year, int Month) MonthOf(DateOnly date, PeriodCalendar calendar) => PeriodMath.MonthOf(date, calendar);

    private static int DayOf(DateOnly date, PeriodCalendar calendar) =>
        calendar == PeriodCalendar.Persian ? Persian.GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue)) : date.Day;
}
