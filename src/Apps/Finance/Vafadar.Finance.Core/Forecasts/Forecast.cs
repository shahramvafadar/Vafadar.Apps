using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Forecasts;

/// <summary>Why an item is part of the forecast.</summary>
public enum ForecastSource
{
    /// <summary>An open plan occurrence due in the horizon.</summary>
    Plan,

    /// <summary>An open occurrence whose due date has passed; assumed at the base date (FOR-04).</summary>
    OverduePlan,

    /// <summary>An entry already recorded with a date after the base date.</summary>
    FutureEntry,
}

/// <summary>One item that moves the forecast balance.</summary>
/// <param name="Date">When the item is assumed to happen.</param>
/// <param name="Name">Plan name or entry title.</param>
/// <param name="CurrencyCode">Currency of <paramref name="Effect"/>.</param>
/// <param name="Effect">Signed effect on the balance in scope; <see langword="null"/> when the amount is unknown.</param>
/// <param name="Source">Why the item is included.</param>
/// <param name="IsEstimate">Whether the amount is an estimate.</param>
/// <param name="ScheduleId">The plan, for plan items.</param>
/// <param name="OriginalDate">The occurrence, for plan items.</param>
/// <param name="IsExcluded">Whether the item is left out by a what-if scenario; it is listed but moves nothing.</param>
/// <param name="IsMoved">Whether a what-if scenario assumes another date for the item.</param>
public sealed record ForecastItem(
    DateOnly Date,
    string Name,
    string CurrencyCode,
    long? Effect,
    ForecastSource Source,
    bool IsEstimate,
    Guid? ScheduleId,
    DateOnly? OriginalDate,
    bool IsExcluded = false,
    bool IsMoved = false);

/// <summary>
/// A temporary what-if for the forecast view (FOR-04): plan occurrences left out or assumed on another date. It is
/// never saved and never changes plans or entries.
/// </summary>
public sealed class ForecastScenario
{
    private readonly HashSet<(Guid, DateOnly)> _excluded = [];
    private readonly Dictionary<(Guid, DateOnly), DateOnly> _moved = [];

    /// <summary>Gets a value indicating whether the scenario changes anything.</summary>
    public bool IsEmpty => _excluded.Count == 0 && _moved.Count == 0;

    /// <summary>Leaves an occurrence out, or includes it again.</summary>
    public void SetExcluded(Guid scheduleId, DateOnly originalDate, bool excluded)
    {
        if (excluded)
        {
            _excluded.Add((scheduleId, originalDate));
        }
        else
        {
            _excluded.Remove((scheduleId, originalDate));
        }
    }

    /// <summary>Assumes another date for an occurrence; <see langword="null"/> restores its due date.</summary>
    public void SetDate(Guid scheduleId, DateOnly originalDate, DateOnly? date)
    {
        if (date is { } d)
        {
            _moved[(scheduleId, originalDate)] = d;
        }
        else
        {
            _moved.Remove((scheduleId, originalDate));
        }
    }

    /// <summary>Removes every change.</summary>
    public void Clear()
    {
        _excluded.Clear();
        _moved.Clear();
    }

    internal bool IsExcluded(Guid scheduleId, DateOnly originalDate) => _excluded.Contains((scheduleId, originalDate));

    internal DateOnly? DateOf(Guid scheduleId, DateOnly originalDate) => _moved.TryGetValue((scheduleId, originalDate), out var date) ? date : null;
}

/// <summary>One day of the forecast path.</summary>
public readonly record struct ForecastPoint(DateOnly Date, long Balance);

/// <summary>The forecast of one currency (FOR-01..09).</summary>
public sealed record CurrencyForecast(
    string CurrencyCode,
    long StartBalance,
    long EndBalance,
    long Minimum,
    DateOnly MinimumDate,
    IReadOnlyList<ForecastPoint> Path,
    IReadOnlyList<ForecastItem> Items)
{
    /// <summary>Gets the number of items whose amount is unknown; the result is then incomplete (FOR-05).</summary>
    public int UnknownCount => Items.Count(i => i.Effect is null && !i.IsExcluded);

    /// <summary>Gets a value indicating whether the result is incomplete.</summary>
    public bool IsIncomplete => UnknownCount > 0;

    /// <summary>Gets a value indicating whether the balance is estimated to drop below zero within the horizon (AT-44).</summary>
    public bool GoesNegative => Minimum < 0;
}

/// <summary>
/// Estimated balance after the recorded plans (docs/02-domain-design.md §8). It never guesses unplanned spending
/// (FOR-06) and never subtracts budgets or monthly equivalents (FOR-07).
/// </summary>
public static class ForecastCalculator
{
    /// <summary>Computes the forecast per currency for the accounts in scope from <paramref name="baseDate"/> to <paramref name="horizon"/>.</summary>
    /// <param name="accounts">All accounts.</param>
    /// <param name="entries">All entries.</param>
    /// <param name="schedules">All plans.</param>
    /// <param name="states">All occurrence states.</param>
    /// <param name="baseDate">The base date (today).</param>
    /// <param name="horizon">The last day of the forecast.</param>
    /// <param name="accountIds">Accounts in scope; <see langword="null"/> = accounts included in totals.</param>
    /// <param name="scenario">A temporary what-if for plan occurrences (FOR-04).</param>
    public static IReadOnlyList<CurrencyForecast> Compute(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        DateOnly baseDate,
        DateOnly horizon,
        IReadOnlyCollection<Guid>? accountIds = null,
        ForecastScenario? scenario = null)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(schedules);
        var accountList = accounts.ToList();
        var scope = LedgerCalculator.InScope(accountList, accountIds).Where(a => !a.IsArchived || accountIds is not null).ToDictionary(a => a.Id);
        var stateList = states.ToList();
        var items = new List<ForecastItem>();

        // Entries already recorded for later dates are part of the ledger but not of today's balance.
        foreach (var entry in entries.Where(e => e.Date > baseDate && e.Date <= horizon))
        {
            foreach (var (currency, effect) in Effects(entry.Kind, entry.AccountId, entry.ToAccountId, entry.Amount, entry.ToAmount, scope, entry.Direction))
            {
                items.Add(new ForecastItem(entry.Date, entry.Title ?? string.Empty, currency, effect, ForecastSource.FutureEntry, false, entry.ScheduleId, entry.OccurrenceDate));
            }
        }

        // Open occurrences only: a settled one is already in the ledger and must not be counted again (FOR-03, AT-42).
        foreach (var schedule in schedules.Where(s => s.State == ScheduleState.Active))
        {
            var since = schedule.ActiveFrom ?? schedule.Rule.Start;
            foreach (var occurrence in Occurrences.Between(schedule, stateList, since, horizon, baseDate).Where(o => o.IsOpen))
            {
                var overdue = occurrence.DueDate < baseDate;
                var date = overdue ? baseDate : occurrence.DueDate;
                var excluded = scenario?.IsExcluded(schedule.Id, occurrence.OriginalDate) == true;
                var moved = scenario?.DateOf(schedule.Id, occurrence.OriginalDate);
                if (moved is { } assumed)
                {
                    // A date beyond the horizon takes the item out of this forecast but keeps it listed, so it can be
                    // moved back; one in the past means today.
                    excluded |= assumed > horizon;
                    date = assumed < baseDate ? baseDate : assumed;
                }

                // Partial payments are already in the ledger; only the outstanding rest is still to come (AT-66).
                var amount = occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount;
                var effects = Effects(schedule.Kind, schedule.AccountId, schedule.ToAccountId, amount ?? 0, schedule.ToAmount, scope, null);
                foreach (var (currency, effect) in effects)
                {
                    items.Add(new ForecastItem(
                        date,
                        schedule.Name,
                        currency,
                        amount is null ? null : effect,
                        overdue ? ForecastSource.OverduePlan : ForecastSource.Plan,
                        occurrence.AmountMode == AmountMode.Estimated,
                        schedule.Id,
                        occurrence.OriginalDate,
                        excluded,
                        moved is not null));
                }
            }
        }

        var currencies = scope.Values.Select(a => a.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.Ordinal);
        var result = new List<CurrencyForecast>();
        foreach (var currency in currencies)
        {
            var start = scope.Values.Where(a => Same(a.CurrencyCode, currency)).Sum(a => LedgerCalculator.Balance(a, entries, baseDate));
            var currencyItems = items.Where(i => Same(i.CurrencyCode, currency)).OrderBy(i => i.Date).ToList();
            var byDay = currencyItems.Where(i => i.Effect is not null && !i.IsExcluded).GroupBy(i => i.Date).ToDictionary(g => g.Key, g => g.Sum(i => i.Effect!.Value));

            var path = new List<ForecastPoint>();
            var balance = start;
            var minimum = long.MaxValue;
            var minimumDate = baseDate;
            for (var day = baseDate; day <= horizon; day = day.AddDays(1))
            {
                balance += byDay.GetValueOrDefault(day);
                path.Add(new ForecastPoint(day, balance));
                if (balance < minimum)
                {
                    minimum = balance;
                    minimumDate = day;
                }
            }

            result.Add(new CurrencyForecast(currency, start, balance, minimum, minimumDate, path, currencyItems));
        }

        return result;
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // Effect on the scope per currency. A transfer inside the scope moves nothing in total, unless the currencies differ.
    private static List<(string Currency, long Effect)> Effects(
        EntryKind kind,
        Guid accountId,
        Guid? toAccountId,
        long amount,
        long? toAmount,
        IReadOnlyDictionary<Guid, Account> scope,
        AdjustmentDirection? direction)
    {
        var effects = new List<(string, long)>();
        scope.TryGetValue(accountId, out var source);
        switch (kind)
        {
            case EntryKind.Income or EntryKind.Refund when source is not null:
                effects.Add((source.CurrencyCode, amount));
                break;
            case EntryKind.Expense or EntryKind.IncomeReversal when source is not null:
                effects.Add((source.CurrencyCode, -amount));
                break;
            case EntryKind.Adjustment when source is not null:
                effects.Add((source.CurrencyCode, direction == AdjustmentDirection.Decrease ? -amount : amount));
                break;
            case EntryKind.Transfer:
                Account? destination = null;
                if (toAccountId is { } to)
                {
                    scope.TryGetValue(to, out destination);
                }

                if (source is not null)
                {
                    effects.Add((source.CurrencyCode, -amount));
                }

                if (destination is not null)
                {
                    effects.Add((destination.CurrencyCode, toAmount ?? amount));
                }

                break;
        }

        // Opposite effects in the same currency (a transfer inside the scope) cancel out.
        return [.. effects.GroupBy(e => e.Item1, StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Sum(e => e.Item2))).Where(e => e.Item2 != 0 || effects.Count == 1)];
    }
}
