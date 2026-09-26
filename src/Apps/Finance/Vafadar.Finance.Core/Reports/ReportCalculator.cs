using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Reports;

/// <summary>
/// How an account balance changed in a period (REP-03): the change is not the same as income minus expense, because
/// transfers, adjustments and the opening balance also move it. <see cref="OpeningBalanceAdded"/> is the account's
/// opening balance when its opening date lies inside the period.
/// </summary>
public sealed record AccountMovement(
    Guid AccountId,
    string CurrencyCode,
    long Opening,
    long OpeningBalanceAdded,
    long Income,
    long Refunds,
    long Expense,
    long IncomeReversals,
    long TransfersIn,
    long TransfersOut,
    long Adjustments,
    long Closing)
{
    /// <summary>Gets the balance change of the period.</summary>
    public long Change => Closing - Opening;
}

/// <summary>Income and expense of one month; <see cref="IsPartial"/> marks a month that is not over yet (REP-05).</summary>
public sealed record MonthTotals(int Year, int Month, DateOnly From, DateOnly To, long NetIncome, long NetExpense, bool IsPartial)
{
    /// <summary>Gets net income minus net expense.</summary>
    public long Result => NetIncome - NetExpense;
}

/// <summary>Planned and recorded amounts of one plan in a period.</summary>
public sealed record PlanActual(Schedule Schedule, long Planned, int PlannedCount, int UnknownCount, long Actual, int SettledCount);

/// <summary>Calculations of the report screens (UI-11). They reuse the ledger formulas, so every number matches (Q-05).</summary>
public static class ReportCalculator
{
    /// <summary>Returns the movement of each account in scope between <paramref name="from"/> and <paramref name="to"/>.</summary>
    public static IReadOnlyList<AccountMovement> AccountMovements(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly from, DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        var result = new List<AccountMovement>();

        foreach (var account in accounts)
        {
            long income = 0, refunds = 0, expense = 0, reversals = 0, transfersIn = 0, transfersOut = 0, adjustments = 0;
            foreach (var entry in entries)
            {
                if (entry.Date < from || entry.Date > to || entry.Date < account.OpeningDate)
                {
                    continue;
                }

                if (entry.AccountId == account.Id)
                {
                    switch (entry.Kind)
                    {
                        case EntryKind.Income: income += entry.Amount; break;
                        case EntryKind.Refund: refunds += entry.Amount; break;
                        case EntryKind.Expense: expense += entry.Amount; break;
                        case EntryKind.IncomeReversal: reversals += entry.Amount; break;
                        case EntryKind.Transfer: transfersOut += entry.Amount; break;
                        case EntryKind.Adjustment: adjustments += entry.EffectOn(account.Id); break;
                    }
                }

                if (entry.Kind == EntryKind.Transfer && entry.ToAccountId == account.Id)
                {
                    transfersIn += entry.ToAmount ?? entry.Amount;
                }
            }

            var opening = LedgerCalculator.Balance(account, entries, from.AddDays(-1));
            var closing = LedgerCalculator.Balance(account, entries, to);
            var openingAdded = account.OpeningDate >= from && account.OpeningDate <= to ? account.OpeningBalance : 0;
            result.Add(new AccountMovement(account.Id, account.CurrencyCode, opening, openingAdded, income, refunds, expense, reversals, transfersIn, transfersOut, adjustments, closing));
        }

        return result;
    }

    /// <summary>
    /// Returns income and expense of the <paramref name="count"/> months up to the month containing
    /// <paramref name="today"/>, oldest first, in one currency. The current month is flagged as partial (REP-05).
    /// </summary>
    public static IReadOnlyList<MonthTotals> MonthlyTrend(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        DateOnly today,
        int count,
        PeriodCalendar calendar,
        string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var accountList = accounts.ToList();
        var (year, month) = PeriodMath.MonthOf(today, calendar);
        var months = new List<(int Year, int Month)>();
        for (var i = 0; i < count; i++)
        {
            months.Insert(0, (year, month));
            (year, month) = PeriodMath.Previous(year, month);
        }

        return
        [
            .. months.Select(m =>
            {
                var (from, to) = PeriodMath.MonthRange(m.Year, m.Month, calendar);
                var totals = LedgerCalculator.Totals(accountList, entries, new LedgerFilter(from, to))
                    .FirstOrDefault(t => string.Equals(t.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
                return new MonthTotals(m.Year, m.Month, from, to, totals?.NetIncome ?? 0, totals?.NetExpense ?? 0, to >= today);
            }),
        ];
    }

    /// <summary>
    /// Compares planned amounts (occurrences due in the period, skipped ones excluded) with recorded amounts (the
    /// entries that settled those occurrences) for every plan with occurrences in the period.
    /// </summary>
    public static IReadOnlyList<PlanActual> PlanVsActual(
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        IEnumerable<LedgerEntry> entries,
        DateOnly from,
        DateOnly to,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedules);
        var stateList = states.ToList();
        var bySettlement = entries.Where(e => e.ScheduleId is not null).ToLookup(e => (e.ScheduleId!.Value, e.OccurrenceDate));
        var result = new List<PlanActual>();

        foreach (var schedule in schedules)
        {
            long planned = 0, actual = 0;
            int plannedCount = 0, unknown = 0, settled = 0;
            foreach (var occurrence in Occurrences.Between(schedule, stateList, from, to, today).Where(o => o.Status != OccurrenceView.Skipped))
            {
                plannedCount++;
                if (occurrence.Amount is { } amount)
                {
                    planned += amount;
                }
                else
                {
                    unknown++;
                }

                foreach (var entry in bySettlement[(schedule.Id, occurrence.OriginalDate)])
                {
                    actual += entry.Amount;
                    settled++;
                }
            }

            if (plannedCount > 0)
            {
                result.Add(new PlanActual(schedule, planned, plannedCount, unknown, actual, settled));
            }
        }

        return result;
    }
}
