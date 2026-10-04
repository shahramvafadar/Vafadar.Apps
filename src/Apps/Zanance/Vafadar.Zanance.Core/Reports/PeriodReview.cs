using System.Globalization;
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>The steps of the period-end review (ZEX-S0610); each opens an existing screen. New values are appended.</summary>
public enum ReviewStep
{
    /// <summary>Review unreviewed entries.</summary>
    Unreviewed = 0,

    /// <summary>Settle or skip due and overdue plan payments.</summary>
    Plans = 1,

    /// <summary>Compare account balances with the bank.</summary>
    Reconcile = 2,

    /// <summary>Look at the goals.</summary>
    Goals = 3,

    /// <summary>Make a backup.</summary>
    Backup = 4,
}

/// <summary>The state of the review of one financial month: which month, which steps are done, and whether it is finished.</summary>
public sealed record PeriodReviewState(int Year, int Month, IReadOnlySet<ReviewStep> Done, bool Finished)
{
    /// <summary>Gets the key of the month, e.g. "2026-09".</summary>
    public string Key => PeriodReview.Key(Year, Month);
}

/// <summary>
/// The period-end review (ZEX-S0610): a short checklist of existing actions for the financial month that just ended.
/// The progress is kept in the profile's settings as "2026-09" (finished) or "2026-09:Unreviewed,Plans" (steps done),
/// so it survives a restart and travels with backups. Nothing is computed twice – every step opens its own screen.
/// </summary>
public static class PeriodReview
{
    /// <summary>Returns the key of a financial month.</summary>
    public static string Key(int year, int month) => string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}");

    /// <summary>
    /// Returns the review that is due: the financial month before the one containing <paramref name="today"/>, unless it is
    /// finished; <see langword="null"/> when there is nothing to review (no data before this month).
    /// </summary>
    public static PeriodReviewState? Due(string? progress, DateOnly today, PeriodCalendar calendar, int startDay, DateOnly? firstData)
    {
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (year, month) = PeriodMath.Previous(current.Year, current.Month);
        if (firstData is not { } first || first > PeriodMath.MonthRange(year, month, calendar, startDay).Last)
        {
            return null;
        }

        var state = Parse(progress, year, month);
        return state.Finished ? null : state;
    }

    /// <summary>Returns the state of the review of a month from the stored progress.</summary>
    public static PeriodReviewState Parse(string? progress, int year, int month)
    {
        var key = Key(year, month);
        if (string.IsNullOrEmpty(progress) || !progress.StartsWith(key, StringComparison.Ordinal))
        {
            return new PeriodReviewState(year, month, new HashSet<ReviewStep>(), false);
        }

        if (progress.Length == key.Length)
        {
            return new PeriodReviewState(year, month, new HashSet<ReviewStep>(Enum.GetValues<ReviewStep>()), true);
        }

        var done = progress[(key.Length + 1)..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.TryParse<ReviewStep>(s, out var step) ? step : (ReviewStep?)null)
            .Where(s => s is not null)
            .Select(s => s!.Value)
            .ToHashSet();
        return new PeriodReviewState(year, month, done, false);
    }

    /// <summary>Returns the progress text with one step done or undone.</summary>
    public static string Toggle(PeriodReviewState state, ReviewStep step, bool done)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new HashSet<ReviewStep>(state.Done);
        if (done)
        {
            steps.Add(step);
        }
        else
        {
            steps.Remove(step);
        }

        return steps.Count == 0 ? state.Key + ":" : state.Key + ":" + string.Join(",", steps.Order());
    }

    /// <summary>Returns the progress text of a finished review.</summary>
    public static string Finish(PeriodReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Key;
    }
}
