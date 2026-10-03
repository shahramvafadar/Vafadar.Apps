using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Goals;

/// <summary>
/// The contribution dates of a goal (ZEX-S0305, ZEX-P14) on the recurrence engine of the plans: weekly, every two weeks
/// from a pay day, monthly on a day, Gregorian or Persian calendar, month-end rules. Goals without a plan of their own
/// keep their old frequency, mapped to an equivalent rule that starts today.
/// </summary>
public static class ContributionSchedule
{
    // An ETA further away than this many contributions is reported as "no date" instead of being computed.
    private const int MaxContributions = 1_200;

    /// <summary>Returns the rule of the goal's contribution dates.</summary>
    public static RecurrenceRule RuleFor(Goal goal, ContributionPlan? plan, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(goal);
        if (plan is not null)
        {
            return plan.Rule;
        }

        return goal.Frequency == ContributionFrequency.Weekly
            ? new RecurrenceRule { Frequency = Frequency.Weekly, Start = today }
            : new RecurrenceRule { Frequency = Frequency.Monthly, Start = today, MissingDay = MissingDayPolicy.LastValidDay };
    }

    /// <summary>Returns the contribution dates from <paramref name="from"/> to <paramref name="to"/> (both inclusive).</summary>
    public static IReadOnlyList<DateOnly> Dates(RecurrenceRule rule, DateOnly from, DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return [.. Recurrence.Between(rule, from, to).Select(d => d.Date)];
    }

    /// <summary>Returns N: the contribution dates from today to the target date, both inclusive (0 when the date passed).</summary>
    public static int Opportunities(RecurrenceRule rule, DateOnly today, DateOnly target) =>
        target < today ? 0 : Dates(rule, today, target).Count;

    /// <summary>
    /// Returns the date the remaining amount is reached when <paramref name="contribution"/> is set aside on every
    /// contribution date from today: the ceil(R / C)-th date (design §9.1). <see langword="null"/> when nothing remains,
    /// the contribution is not positive or the date lies more than 1,200 contributions away.
    /// </summary>
    public static DateOnly? Eta(RecurrenceRule rule, DateOnly today, long remaining, long contribution)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (remaining <= 0 || contribution <= 0)
        {
            return null;
        }

        var needed = (remaining + contribution - 1) / contribution;
        if (needed > MaxContributions)
        {
            return null;
        }

        var dates = Recurrence.Next(rule, today, (int)needed);
        return dates.Count == needed ? dates[^1].Date : null;
    }

    /// <summary>Returns ceil(R / C): how many contributions reach the remaining amount, or 0.</summary>
    public static long PeriodsNeeded(long remaining, long contribution) =>
        remaining <= 0 || contribution <= 0 ? 0 : (remaining + contribution - 1) / contribution;
}
