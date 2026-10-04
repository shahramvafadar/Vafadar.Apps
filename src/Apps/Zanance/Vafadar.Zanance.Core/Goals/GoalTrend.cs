using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Goals;

/// <summary>Whether a pace could be observed (ZEX-P15).</summary>
public enum TrendStatus
{
    /// <summary>A positive pace and the date it reaches the target.</summary>
    Ok = 0,

    /// <summary>Fewer than three complete periods: "Not enough history yet (2 of 3 months)".</summary>
    NotEnoughHistory = 1,

    /// <summary>The pace is zero or negative: "No date at your current pace".</summary>
    NoPace = 2,

    /// <summary>The target is reached already.</summary>
    Reached = 3,
}

/// <summary>The net contribution of one complete financial month; a one-off is more than 3 × the median of the others.</summary>
public sealed record TrendPeriod(DateOnly From, DateOnly To, long Contribution, bool IsOneOff);

/// <summary>
/// The goal's observed pace (ZEX-S0702, design §9.2): the median net contribution of up to six complete financial
/// months, and the date that pace reaches the target. An estimate, never a promise; the user's own plan stays a
/// separate line (ZEX-GO15).
/// </summary>
/// <param name="Status">Whether there is a pace and a date.</param>
/// <param name="Periods">The complete months used, oldest first.</param>
/// <param name="Pace">The median net contribution per month (C_trend), or <see langword="null"/> without enough history.</param>
/// <param name="Eta">The month end at which the pace reaches the target.</param>
/// <param name="SoFar">The net contribution of the running month, shown apart.</param>
public sealed record GoalTrend(TrendStatus Status, IReadOnlyList<TrendPeriod> Periods, long? Pace, DateOnly? Eta, long SoFar)
{
    /// <summary>Gets the number of complete months found (for "2 of 3 months").</summary>
    public int CompletePeriods => Periods.Count;
}

/// <summary>Computes the observed pace of a goal of any type (design §9.2).</summary>
public static class GoalTrendService
{
    /// <summary>Complete periods needed for a pace.</summary>
    public const int MinimumPeriods = 3;

    /// <summary>Complete periods used at most.</summary>
    public const int MaximumPeriods = 6;

    /// <summary>
    /// Returns the pace of <paramref name="goal"/>. Net contribution per month: a balance goal – the change of the account
    /// balance without its opening balance and adjustments; money set aside – the net earmarks; a quantity goal – the net
    /// quantity change without moves between locations (prices never count).
    /// </summary>
    public static GoalTrend Compute(
        Goal goal,
        long remaining,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<GoalAllocation> allocations,
        IReadOnlyCollection<AssetEvent> events,
        DateOnly today,
        PeriodCalendar calendar,
        int startDay = 1)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(events);
        var (start, contribution) = Source(goal, accounts, entries, allocations, events);

        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (currentFrom, _) = PeriodMath.MonthRange(current.Year, current.Month, calendar, startDay);
        var soFar = contribution(currentFrom, today);
        var periods = new List<TrendPeriod>();
        var (year, month) = PeriodMath.Previous(current.Year, current.Month);
        for (var i = 0; i < MaximumPeriods && start is { } first; i++)
        {
            var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
            if (from < first)
            {
                break;
            }

            periods.Insert(0, new TrendPeriod(from, to, contribution(from, to), false));
            (year, month) = PeriodMath.Previous(year, month);
        }

        // A period far above the others is named "one-off", so the user sees why the pace is what it is.
        periods = [.. periods.Select((p, i) =>
        {
            var others = periods.Where((_, j) => j != i).Select(o => o.Contribution).ToList();
            var median = KpiCatalog.Median(others);
            return p with { IsOneOff = others.Count > 0 && median > 0 && p.Contribution > 3 * median };
        })];

        if (remaining <= 0)
        {
            return new GoalTrend(TrendStatus.Reached, periods, null, null, soFar);
        }

        if (periods.Count < MinimumPeriods)
        {
            return new GoalTrend(TrendStatus.NotEnoughHistory, periods, null, null, soFar);
        }

        var pace = KpiCatalog.Median([.. periods.Select(p => p.Contribution)]);
        if (pace <= 0)
        {
            return new GoalTrend(TrendStatus.NoPace, periods, pace, null, soFar);
        }

        // The n-th month end from the running month on: ceil(R / C) contributions.
        var needed = (int)Math.Min(1200, (remaining + pace - 1) / pace);
        var (etaYear, etaMonth) = (current.Year, current.Month);
        for (var i = 1; i < needed; i++)
        {
            (etaYear, etaMonth) = PeriodMath.Next(etaYear, etaMonth);
        }

        return new GoalTrend(TrendStatus.Ok, periods, pace, PeriodMath.MonthRange(etaYear, etaMonth, calendar, startDay).Last, soFar);
    }

    // Where the data of the goal starts and how much was contributed between two days (both inclusive).
    private static (DateOnly? Start, Func<DateOnly, DateOnly, long> Contribution) Source(
        Goal goal,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<GoalAllocation> allocations,
        IReadOnlyCollection<AssetEvent> events)
    {
        switch (goal.Type)
        {
            case GoalType.AccountBalance when goal.AccountId is { } accountId && accounts.FirstOrDefault(a => a.Id == accountId) is { } account:
                var own = entries.Where(e => (e.AccountId == accountId || e.ToAccountId == accountId) && e.Kind != EntryKind.Adjustment && !LedgerCalculator.IsBeforeOpening(e, account)).ToList();
                return (account.OpeningDate, (from, to) => own.Where(e => e.Date >= from && e.Date <= to).Sum(e => e.EffectOn(accountId)));
            case GoalType.HoldingQuantity when goal.AssetTypeId is { } typeId:
                var ofType = events.Where(e => e.AssetTypeId == typeId).ToList();
                long Effect(AssetEvent e) => goal.LocationId is { } location ? (e.Kind == AssetEventKind.LocationTransfer ? 0 : e.EffectAt(location)) : e.TotalEffect;
                return (ofType.Count == 0 ? null : ofType.Min(e => e.Date), (from, to) => ofType.Where(e => e.Date >= from && e.Date <= to).Sum(Effect));
            case GoalType.Earmark:
                var mine = allocations.Where(a => a.GoalId == goal.Id).ToList();
                return (mine.Count == 0 ? null : mine.Min(a => a.Date), (from, to) => mine.Where(a => a.Date >= from && a.Date <= to).Sum(a => a.Amount));
            default:
                return (null, (_, _) => 0);
        }
    }
}
