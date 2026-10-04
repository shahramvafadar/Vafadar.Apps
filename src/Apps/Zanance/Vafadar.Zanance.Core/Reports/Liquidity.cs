using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>
/// The lowest projected balance of the usable accounts in one currency and the headroom above protected money
/// (ZEX-K02, 04 §3). An estimate under the current plans and assumptions – never "safe to spend".
/// </summary>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="Start">The usable balance at the end of today.</param>
/// <param name="Minimum">The lowest liquidity after today, day-to-day spending included when estimated.</param>
/// <param name="MinimumDate">The first day it occurs.</param>
/// <param name="Protected">Covered money set aside for goals marked "Protect this money".</param>
/// <param name="EssentialPerDay">The day-to-day estimate per day, or <see langword="null"/> when not set ("not included").</param>
/// <param name="EssentialToMinimum">Day-to-day spending assumed up to the minimum date.</param>
/// <param name="UnknownCount">Plan items without a known amount; the result is then incomplete (AT37).</param>
/// <param name="Items">The forecast items up to the minimum date, for the explanation and drill-down.</param>
/// <param name="ItemsAfter">The forecast items after the minimum date (e.g. the salary that comes after the lowest point).</param>
public sealed record LiquidityResult(
    string CurrencyCode,
    long Start,
    long Minimum,
    DateOnly MinimumDate,
    long Protected,
    decimal? EssentialPerDay,
    long EssentialToMinimum,
    int UnknownCount,
    IReadOnlyList<ForecastItem> Items,
    IReadOnlyList<ForecastItem> ItemsAfter)
{
    /// <summary>Gets the lowest liquidity minus protected money; negative means the plans would use protected money.</summary>
    public long Headroom => Minimum - Protected;

    /// <summary>Gets a value indicating whether unknown amounts make the result incomplete.</summary>
    public bool IsIncomplete => UnknownCount > 0;

    /// <summary>Gets a value indicating whether day-to-day spending is part of the result.</summary>
    public bool IncludesEssential => EssentialPerDay is not null;
}

/// <summary>Liquidity and headroom (ZEX-S0606) on top of the forecast of the usable accounts.</summary>
public static class LiquidityCalculator
{
    /// <summary>
    /// Returns liquidity and headroom per currency of <paramref name="forecasts"/> (computed with the usable scope):
    /// <c>Liquidity(t) = forecast(t) − E × days(today, t]</c>, minimum over the days after today, headroom = minimum −
    /// protected money. Entries dated today are already in the start balance and never counted twice.
    /// </summary>
    public static IReadOnlyList<LiquidityResult> Compute(
        IEnumerable<CurrencyForecast> forecasts,
        IReadOnlyDictionary<string, long> protectedMoney,
        DateOnly today,
        long? essentialEstimate = null,
        EstimatePeriod essentialPeriod = EstimatePeriod.Day,
        string? essentialCurrency = null)
    {
        ArgumentNullException.ThrowIfNull(forecasts);
        ArgumentNullException.ThrowIfNull(protectedMoney);
        var result = new List<LiquidityResult>();
        foreach (var forecast in forecasts)
        {
            decimal? perDay = essentialEstimate is { } estimate && estimate > 0 && string.Equals(essentialCurrency, forecast.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                ? PerDay(estimate, essentialPeriod)
                : null;
            long Liquidity(ForecastPoint point) => point.Balance - Essential(perDay, point.Date.DayNumber - today.DayNumber);

            var after = forecast.Path.Where(p => p.Date > today).ToList();
            var lowest = after.Count == 0 ? new ForecastPoint(today, forecast.StartBalance) : after.MinBy(Liquidity);
            var minimum = after.Count == 0 ? forecast.StartBalance : Liquidity(lowest);
            var items = forecast.Items.Where(i => !i.IsExcluded).ToList();
            result.Add(new LiquidityResult(
                forecast.CurrencyCode,
                forecast.StartBalance,
                minimum,
                lowest.Date,
                protectedMoney.GetValueOrDefault(forecast.CurrencyCode),
                perDay,
                Essential(perDay, lowest.Date.DayNumber - today.DayNumber),
                forecast.UnknownCount,
                [.. items.Where(i => i.Date <= lowest.Date)],
                [.. items.Where(i => i.Date > lowest.Date)]));
        }

        return result;
    }

    /// <summary>
    /// Returns covered money set aside for protected goals per currency (ZEX-P16, AT24): earmarks of active or paused
    /// goals of money set aside marked "Protect this money", in usable accounts, each account at most its positive
    /// balance, counted once. Balance goals reserve nothing and are never subtracted.
    /// </summary>
    public static IReadOnlyDictionary<string, long> ProtectedMoney(
        IEnumerable<Goal> goals,
        IEnumerable<GoalAllocation> allocations,
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(allocations);
        var protectedGoals = goals.Where(g => g.Protect && g.Type == GoalType.Earmark && g.State is GoalState.Active or GoalState.Paused).Select(g => g.Id).ToHashSet();
        var earmarks = allocations.Where(a => protectedGoals.Contains(a.GoalId)).GroupBy(a => a.AccountId).ToDictionary(g => g.Key, g => g.Sum(a => a.Amount));
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in accounts.Where(a => !a.IsArchived && a.UsableForPayments && earmarks.ContainsKey(a.Id)))
        {
            var covered = Math.Min(Math.Max(0, earmarks[account.Id]), Math.Max(0, LedgerCalculator.Balance(account, entries, today)));
            result[account.CurrencyCode] = result.GetValueOrDefault(account.CurrencyCode) + covered;
        }

        return result;
    }

    /// <summary>
    /// Suggests a day-to-day estimate per day (04 §3): the median daily consumption of the last three complete financial
    /// months on usable accounts of the currency, without entries that settled plans; <see langword="null"/> without
    /// three months of history. Zanance only suggests it – the user sets it.
    /// </summary>
    public static long? SuggestPerDay(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, string currencyCode, DateOnly today, PeriodCalendar calendar, int startDay = 1)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var usable = accounts.Where(a => !a.IsArchived && a.UsableForPayments && string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)).ToList();
        if (usable.Count == 0)
        {
            return null;
        }

        var unplanned = entries.Where(e => e.ScheduleId is null).ToList();
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (year, month) = PeriodMath.Previous(current.Year, current.Month);
        var daily = new List<long>();
        for (var i = 0; i < 3; i++)
        {
            var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
            if (usable.Min(a => a.OpeningDate) > from)
            {
                return null;
            }

            var net = LedgerCalculator.Totals(usable, unplanned, new LedgerFilter(from, to, [.. usable.Select(a => a.Id)])).Sum(t => t.NetExpense);
            daily.Add((long)Math.Round((decimal)net / (to.DayNumber - from.DayNumber + 1), MidpointRounding.AwayFromZero));
            (year, month) = PeriodMath.Previous(year, month);
        }

        return KpiCatalog.Median(daily);
    }

    /// <summary>Returns an estimate per day: a week is 7 days, a month 365 / 12 days.</summary>
    public static decimal PerDay(long amount, EstimatePeriod period) => period switch
    {
        EstimatePeriod.Week => amount / 7m,
        EstimatePeriod.Month => amount * 12m / 365m,
        _ => amount,
    };

    private static long Essential(decimal? perDay, int days) => perDay is { } value && days > 0 ? (long)Math.Round(value * days, MidpointRounding.AwayFromZero) : 0;
}
