using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>
/// Income and consumption of a period in one currency (ZEX-K05, K06), with the movements that are neither: transfers
/// between accounts of the scope and capital movements (holding purchases and sales) are separate lines (ZEX-R1).
/// </summary>
public sealed record SurplusResult(
    string CurrencyCode,
    long Income,
    long IncomeReversals,
    long Expense,
    long Refunds,
    long Transfers,
    long CapitalPurchases,
    long CapitalSales)
{
    /// <summary>Gets income minus income reversals.</summary>
    public long EligibleIncome => Income - IncomeReversals;

    /// <summary>Gets expenses minus refunds (interest and fees are expenses and count once).</summary>
    public long Consumption => Expense - Refunds;

    /// <summary>Gets eligible income minus consumption (ZEX-K05); negative is a deficit.</summary>
    public long Surplus => EligibleIncome - Consumption;

    /// <summary>
    /// Gets the surplus rate in percent with one decimal (ZEX-K06), or <see langword="null"/> ("not available") when
    /// there is no eligible income – never 0 % or infinity (AT32).
    /// </summary>
    public decimal? RatePercent => EligibleIncome > 0 ? Math.Round(Surplus * 100m / EligibleIncome, 1, MidpointRounding.AwayFromZero) : null;
}

/// <summary>The current period and its comparison period of the same length (ZEX-AT30).</summary>
public sealed record ComparisonRange(DateOnly From, DateOnly To, DateOnly CompareFrom, DateOnly CompareTo, bool IsPartial);

/// <summary>How spending in one category changed (ZEX-K11).</summary>
/// <param name="CategoryId">The category (its top level), or <see langword="null"/> without category.</param>
/// <param name="Current">Net consumption in the current range.</param>
/// <param name="Comparison">Net consumption in the comparison range.</param>
public sealed record SpendingChange(Guid? CategoryId, long Current, long Comparison)
{
    /// <summary>Gets the change.</summary>
    public long Delta => Current - Comparison;

    /// <summary>Gets the change in percent with one decimal, or <see langword="null"/> when the comparison is not positive ("new").</summary>
    public decimal? DeltaPercent => Comparison > 0 ? Math.Round(Delta * 100m / Comparison, 1, MidpointRounding.AwayFromZero) : null;

    /// <summary>Gets a value indicating whether nothing was spent in the comparison range.</summary>
    public bool IsNew => Comparison == 0 && Current != 0;
}

/// <summary>An open payment in the commitments of the next days (ZEX-K04).</summary>
/// <param name="Name">The plan.</param>
/// <param name="CurrencyCode">The currency of the paying account.</param>
/// <param name="DueDate">The due date (overdue ones keep their date).</param>
/// <param name="Amount">What is still to pay, or <see langword="null"/> when unknown.</param>
/// <param name="IsEstimate">Whether the amount is an estimate.</param>
/// <param name="IsOverdue">Whether the due date has passed.</param>
/// <param name="ScheduleId">The plan id, for drill-down.</param>
public sealed record CommitmentItem(string Name, string CurrencyCode, DateOnly DueDate, long? Amount, bool IsEstimate, bool IsOverdue, Guid ScheduleId);

/// <summary>Open commitments of one currency: known, estimated (≈) and unknown amounts are never mixed (ZEX-K04).</summary>
public sealed record CommitmentTotal(string CurrencyCode, long Fixed, long Estimated, int UnknownCount, IReadOnlyList<CommitmentItem> Items);

/// <summary>One plan in the twelve-month view (ZEX-K08).</summary>
/// <param name="Schedule">The plan.</param>
/// <param name="Total">The expected amounts in the next twelve months.</param>
/// <param name="Group">Fixed, non-monthly or estimated.</param>
/// <param name="MonthlyShare">The monthly share of a non-monthly plan ("share, not money set aside").</param>
/// <param name="UnknownCount">Occurrences without a known amount.</param>
public sealed record YearlyPlan(Schedule Schedule, long Total, CommitmentGroup Group, long? MonthlyShare, int UnknownCount);

/// <summary>The group of a regular payment in the twelve-month view.</summary>
public enum CommitmentGroup
{
    /// <summary>A known amount that comes every month or more often.</summary>
    Fixed = 0,

    /// <summary>A known amount that comes less often than monthly, or in a non-monthly category.</summary>
    NonMonthly = 1,

    /// <summary>An estimated amount (≈).</summary>
    Estimated = 2,
}

/// <summary>The regular payments of the next twelve months in one currency (ZEX-K08).</summary>
public sealed record YearlyCommitments(string CurrencyCode, long Fixed, long NonMonthly, long Estimated, int UnknownCount, IReadOnlyList<YearlyPlan> Plans);

/// <summary>Installments of one currency in a period against eligible income (ZEX-K09).</summary>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="Installments">Scheduled installments due in the period.</param>
/// <param name="Interest">Their estimated interest part.</param>
/// <param name="Principal">Their estimated principal part.</param>
/// <param name="EligibleIncome">Eligible income of the period.</param>
/// <param name="NextDate">The next open installment, if any.</param>
/// <param name="NextAmount">Its amount, if known.</param>
/// <param name="LoansWithoutPlan">Loans with an installment but no plan ("add the payment day").</param>
public sealed record DebtBurden(string CurrencyCode, long Installments, long Interest, long Principal, long EligibleIncome, DateOnly? NextDate, long? NextAmount, int LoansWithoutPlan)
{
    /// <summary>Gets installments as a share of eligible income in percent (one decimal), or <see langword="null"/> without income.</summary>
    public decimal? RatioPercent => EligibleIncome > 0 ? Math.Round(Installments * 100m / EligibleIncome, 1, MidpointRounding.AwayFromZero) : null;
}

/// <summary>Where money owed to the user comes from (ZEX-K12).</summary>
public enum ReceivableKind
{
    /// <summary>An expense another person pays back.</summary>
    Reimbursement = 0,

    /// <summary>The balance of an account of money lent.</summary>
    Lent = 1,
}

/// <summary>One open receivable with its age (ZEX-K12).</summary>
/// <param name="Kind">Reimbursement or money lent.</param>
/// <param name="SourceId">The expense entry or the account, for drill-down.</param>
/// <param name="Name">Who owes it (or the entry title or account name).</param>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="Amount">The open amount.</param>
/// <param name="Since">The expense date or the last lending date.</param>
/// <param name="AgeDays">Days since then.</param>
/// <param name="DueDate">The optional due date.</param>
/// <param name="IsOverdue">Whether the due date has passed.</param>
public sealed record Receivable(ReceivableKind Kind, Guid SourceId, string Name, string CurrencyCode, long Amount, DateOnly Since, int AgeDays, DateOnly? DueDate, bool IsOverdue);

/// <summary>Open receivables of one currency with aging buckets 0–30, 31–90 and over 90 days (ZEX-K12).</summary>
public sealed record ReceivableTotal(string CurrencyCode, long Total, long UpTo30Days, long UpTo90Days, long Over90Days, int Over90Count, int OverdueCount, IReadOnlyList<Receivable> Items);

/// <summary>Why the essential coverage cannot be computed, or <see cref="Ok"/> (ZEX-K07).</summary>
public enum CoverageStatus
{
    /// <summary>A number of months is available.</summary>
    Ok = 0,

    /// <summary>Fewer than three complete months with essential spending.</summary>
    NotEnoughHistory = 1,

    /// <summary>Essential spending is zero: "not available", never infinite.</summary>
    NotAvailable = 2,

    /// <summary>The usable money is negative: 0.0 months.</summary>
    BalanceNegative = 3,
}

/// <summary>How many months the usable money covers essential costs (ZEX-K07).</summary>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="UsableMoney">Balance of the usable accounts.</param>
/// <param name="MonthlyEssential">Median essential consumption plus non-monthly shares.</param>
/// <param name="Months">Months with one decimal when <paramref name="Status"/> is <see cref="CoverageStatus.Ok"/> or negative.</param>
/// <param name="Status">Why there is no number.</param>
public sealed record EssentialCoverage(string CurrencyCode, long UsableMoney, long MonthlyEssential, decimal? Months, CoverageStatus Status);

/// <summary>
/// The KPI definitions of the catalog (docs/enhancements/2026-10-multi-unit-goals-insights/04-kpi-and-report-catalog.md):
/// one calculator per definition, used by Home, reports and the PDF alike, so a number is the same everywhere
/// (ZEX-AT36). Unknown is never zero, and a zero denominator gives "not available".
/// </summary>
public static class KpiCatalog
{
    /// <summary>Returns income, consumption, surplus and rate per currency, with transfers and capital movements apart (K05, K06).</summary>
    public static IReadOnlyList<SurplusResult> Surplus(IEnumerable<Account> accounts, IEnumerable<LedgerEntry> entries, LedgerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(filter);
        var accountList = accounts.ToList();
        var entryList = entries as IReadOnlyCollection<LedgerEntry> ?? [.. entries];
        var scope = LedgerCalculator.InScope(accountList, filter.AccountIds).ToDictionary(a => a.Id);
        var result = LedgerCalculator.Totals(accountList, entryList, filter)
            .ToDictionary(t => t.CurrencyCode, t => new long[] { t.Income, t.IncomeReversals, t.GrossExpense, t.Refunds, 0, 0, 0 }, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entryList)
        {
            if (entry.Date < filter.From || entry.Date > filter.To || (filter.ConfirmedOnly && entry.Review != ReviewState.Confirmed)
                || !scope.TryGetValue(entry.AccountId, out var account) || LedgerCalculator.IsBeforeOpening(entry, account))
            {
                continue;
            }

            var slot = entry.Kind switch
            {
                // A transfer counts once, from its source; it moves money but is never income or spending.
                EntryKind.Transfer => 4,
                EntryKind.AssetPurchase => 5,
                EntryKind.AssetSale => 6,
                _ => -1,
            };
            if (slot < 0)
            {
                continue;
            }

            if (!result.TryGetValue(account.CurrencyCode, out var values))
            {
                result[account.CurrencyCode] = values = new long[7];
            }

            values[slot] = checked(values[slot] + entry.Amount);
        }

        return
        [
            .. result.OrderBy(r => r.Key, StringComparer.Ordinal)
                .Select(r => new SurplusResult(r.Key, r.Value[0], r.Value[1], r.Value[2], r.Value[3], r.Value[4], r.Value[5], r.Value[6])),
        ];
    }

    /// <summary>
    /// Returns the range to report and the range it is compared with: a running period is compared day 1…d with day
    /// 1…d of the previous period, never with the whole of it (ZEX-AT30).
    /// </summary>
    public static ComparisonRange Compare(DateOnly from, DateOnly to, DateOnly previousFrom, DateOnly previousTo, DateOnly today)
    {
        if (today < from || today >= to)
        {
            return new ComparisonRange(from, to, previousFrom, previousTo, false);
        }

        var days = today.DayNumber - from.DayNumber;
        var compareTo = previousFrom.AddDays(days);
        return new ComparisonRange(from, today, previousFrom, compareTo > previousTo ? previousTo : compareTo, true);
    }

    /// <summary>Returns the change of net consumption per top-level category in one currency, largest change first (K11).</summary>
    public static IReadOnlyList<SpendingChange> SpendingChanges(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        ComparisonRange range,
        string currencyCode,
        Func<Guid?, Guid?> group,
        IReadOnlyCollection<Guid>? accountIds = null,
        bool confirmedOnly = false)
    {
        ArgumentNullException.ThrowIfNull(range);
        var accountList = accounts.ToList();
        // Entries without a category are kept under Guid.Empty (a dictionary key cannot be null).
        Dictionary<Guid, long> Net(DateOnly from, DateOnly to) =>
            LedgerCalculator.ExpenseByCategory(accountList, entries, new LedgerFilter(from, to, accountIds, confirmedOnly), group)
                .Where(c => string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(c => c.CategoryId ?? Guid.Empty, c => c.Net);
        var current = Net(range.From, range.To);
        var comparison = Net(range.CompareFrom, range.CompareTo);
        return
        [
            .. current.Keys.Union(comparison.Keys)
                .Select(id => new SpendingChange(id == Guid.Empty ? null : id, current.GetValueOrDefault(id), comparison.GetValueOrDefault(id)))
                .Where(c => c.Current != 0 || c.Comparison != 0)
                .OrderByDescending(c => Math.Abs(c.Delta))
                .ThenBy(c => c.CategoryId),
        ];
    }

    /// <summary>
    /// Returns the open outgoing payments due up to <paramref name="days"/> days from today, overdue ones included, per
    /// currency (K04). Only the outstanding part of a partly paid occurrence counts; a transfer between usable accounts
    /// moves no money out of the scope and is left out, a transfer to a card, a loan or a savings account kept aside is a
    /// payment.
    /// </summary>
    public static IReadOnlyList<CommitmentTotal> Commitments(IEnumerable<Account> accounts, IEnumerable<Schedule> schedules, IEnumerable<OccurrenceState> states, DateOnly today, int days = 30)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(schedules);
        var byId = accounts.ToDictionary(a => a.Id);
        var stateList = states.ToList();
        var horizon = today.AddDays(days);
        var items = new List<CommitmentItem>();
        foreach (var schedule in PlanActions.InForce(schedules))
        {
            if (!byId.TryGetValue(schedule.AccountId, out var account) || !IsOutgoing(schedule, byId))
            {
                continue;
            }

            var since = schedule.ActiveFrom ?? schedule.Rule.Start;
            foreach (var occurrence in Occurrences.Between(schedule, stateList, since, horizon, today).Where(o => o.IsOpen))
            {
                items.Add(new CommitmentItem(schedule.Name, account.CurrencyCode, occurrence.DueDate, occurrence.Outstanding,
                    occurrence.AmountMode == AmountMode.Estimated, occurrence.DueDate < today, schedule.Id));
            }
        }

        return
        [
            .. items.GroupBy(i => i.CurrencyCode, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new CommitmentTotal(
                    g.Key,
                    g.Where(i => i.Amount is not null && !i.IsEstimate).Sum(i => i.Amount!.Value),
                    g.Where(i => i.Amount is not null && i.IsEstimate).Sum(i => i.Amount!.Value),
                    g.Count(i => i.Amount is null),
                    [.. g.OrderBy(i => i.DueDate).ThenBy(i => i.Name, StringComparer.CurrentCulture)])),
        ];
    }

    /// <summary>
    /// Returns the regular expense payments expected in the next twelve months per currency, grouped into fixed,
    /// non-monthly and estimated amounts; unknown amounts are counted, never added (K08).
    /// </summary>
    public static IReadOnlyList<YearlyCommitments> Yearly(IEnumerable<Account> accounts, IEnumerable<Category> categories, IEnumerable<Schedule> schedules, IEnumerable<OccurrenceState> states, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(categories);
        var byId = accounts.ToDictionary(a => a.Id);
        var categoryById = categories.ToDictionary(c => c.Id);
        var stateList = states.ToList();
        var to = today.AddMonths(12).AddDays(-1);
        var plans = new List<(string Currency, YearlyPlan Plan)>();
        foreach (var schedule in PlanActions.InForce(schedules).Where(s => s.Kind == EntryKind.Expense))
        {
            if (!byId.TryGetValue(schedule.AccountId, out var account))
            {
                continue;
            }

            var occurrences = Occurrences.Between(schedule, stateList, today, to, today).Where(o => o.Status != OccurrenceView.Skipped).ToList();
            if (occurrences.Count == 0)
            {
                continue;
            }

            var spendingType = schedule.CategoryId is { } id && categoryById.TryGetValue(id, out var category)
                ? (category.ParentId is { } parent && categoryById.TryGetValue(parent, out var top) ? top.SpendingType : category.SpendingType)
                : SpendingType.Flexible;
            var group = schedule.AmountMode == AmountMode.Estimated ? CommitmentGroup.Estimated
                : spendingType == SpendingType.NonMonthly || Recurrence.PerYear(schedule.Rule) < 12 ? CommitmentGroup.NonMonthly
                : CommitmentGroup.Fixed;
            plans.Add((account.CurrencyCode, new YearlyPlan(
                schedule,
                occurrences.Where(o => o.Amount is not null).Sum(o => o.Amount!.Value),
                group,
                group == CommitmentGroup.NonMonthly ? BudgetPlanning.MonthlyEquivalent(schedule) : null,
                occurrences.Count(o => o.Amount is null))));
        }

        return
        [
            .. plans.GroupBy(p => p.Currency, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new YearlyCommitments(
                    g.Key,
                    g.Where(p => p.Plan.Group == CommitmentGroup.Fixed).Sum(p => p.Plan.Total),
                    g.Where(p => p.Plan.Group == CommitmentGroup.NonMonthly).Sum(p => p.Plan.Total),
                    g.Where(p => p.Plan.Group == CommitmentGroup.Estimated).Sum(p => p.Plan.Total),
                    g.Sum(p => p.Plan.UnknownCount),
                    [.. g.Select(p => p.Plan).OrderBy(p => p.Group).ThenByDescending(p => p.Total)])),
        ];
    }

    /// <summary>
    /// Returns the scheduled installments of loans (plans that transfer money to a loan account) due in the period
    /// against eligible income, per currency (K09). The split into interest and principal is an estimate from the
    /// loan's rate; principal is never consumption.
    /// </summary>
    public static IReadOnlyList<DebtBurden> Debt(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        DateOnly from,
        DateOnly to,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        var accountList = accounts.ToList();
        var stateList = states.ToList();
        var inForce = PlanActions.InForce(schedules);
        var income = Surplus(accountList, entries, new LedgerFilter(from, to)).ToDictionary(s => s.CurrencyCode, s => s.EligibleIncome, StringComparer.OrdinalIgnoreCase);
        var sums = new Dictionary<string, (long Total, long Interest, long Principal, DateOnly? Next, long? NextAmount, int Missing)>(StringComparer.OrdinalIgnoreCase);
        foreach (var loan in accountList.Where(a => a.Type == AccountType.Loan && !a.IsArchived))
        {
            var current = sums.GetValueOrDefault(loan.CurrencyCode);
            var plans = inForce.Where(s => s.Kind == EntryKind.Transfer && s.ToAccountId == loan.Id).ToList();
            if (plans.Count == 0)
            {
                current.Missing += loan.Installment is > 0 ? 1 : 0;
                sums[loan.CurrencyCode] = current;
                continue;
            }

            var outstanding = LoanCalculator.Outstanding(loan.Type, LedgerCalculator.Balance(loan, entries, from.AddDays(-1)));
            foreach (var plan in plans)
            {
                foreach (var occurrence in Occurrences.Between(plan, stateList, from, to, today).Where(o => o.Status != OccurrenceView.Skipped && o.Amount is not null))
                {
                    // The destination amount is what reaches the loan when the currencies differ.
                    var payment = plan.ToAmount ?? occurrence.Amount!.Value;
                    var (interest, principal) = LoanCalculator.Split(outstanding, loan.InterestRate ?? 0, payment);
                    outstanding = Math.Max(0, outstanding - principal);
                    current.Total += payment;
                    current.Interest += interest;
                    current.Principal += principal;
                }

                if (Occurrences.NextOpen(plan, stateList, today, today) is { } next && (current.Next is null || next.DueDate < current.Next))
                {
                    current.Next = next.DueDate;
                    current.NextAmount = next.Outstanding;
                }
            }

            sums[loan.CurrencyCode] = current;
        }

        return
        [
            .. sums.OrderBy(s => s.Key, StringComparer.Ordinal)
                .Select(s => new DebtBurden(s.Key, s.Value.Total, s.Value.Interest, s.Value.Principal, income.GetValueOrDefault(s.Key), s.Value.Next, s.Value.NextAmount, s.Value.Missing)),
        ];
    }

    /// <summary>
    /// Returns open receivables per currency: open reimbursements and positive balances of money lent, with their age
    /// and aging buckets (K12). They are never usable money until received.
    /// </summary>
    public static IReadOnlyList<ReceivableTotal> Receivables(IEnumerable<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        var accountList = accounts.ToList();
        var byId = accountList.ToDictionary(a => a.Id);
        var items = new List<Receivable>();
        foreach (var (expense, open) in EntryActions.OpenReimbursements(entries))
        {
            if (!byId.TryGetValue(expense.AccountId, out var account))
            {
                continue;
            }

            var name = expense.ReimbursedBy ?? expense.Title ?? expense.Payee ?? string.Empty;
            items.Add(new Receivable(ReceivableKind.Reimbursement, expense.Id, name, account.CurrencyCode, open, expense.Date, today.DayNumber - expense.Date.DayNumber,
                expense.ReimbursementDueDate, expense.ReimbursementDueDate < today));
        }

        foreach (var lent in accountList.Where(a => a.Type == AccountType.Lent && !a.IsArchived))
        {
            var balance = LedgerCalculator.Balance(lent, entries, today);
            if (balance <= 0)
            {
                continue;
            }

            // The age runs from the last time money was lent (a transfer into the account), else from its opening.
            var since = entries.Where(e => e.Date <= today && e.Date >= lent.OpeningDate && e.EffectOn(lent.Id) > 0).Select(e => (DateOnly?)e.Date).Max() ?? lent.OpeningDate;
            items.Add(new Receivable(ReceivableKind.Lent, lent.Id, lent.Counterparty ?? lent.Name, lent.CurrencyCode, balance, since, today.DayNumber - since.DayNumber,
                lent.DueDate, lent.DueDate < today));
        }

        return
        [
            .. items.GroupBy(i => i.CurrencyCode, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new ReceivableTotal(
                    g.Key,
                    g.Sum(i => i.Amount),
                    g.Where(i => i.AgeDays <= 30).Sum(i => i.Amount),
                    g.Where(i => i.AgeDays is > 30 and <= 90).Sum(i => i.Amount),
                    g.Where(i => i.AgeDays > 90).Sum(i => i.Amount),
                    g.Count(i => i.AgeDays > 90),
                    g.Count(i => i.IsOverdue),
                    [.. g.OrderByDescending(i => i.AgeDays)])),
        ];
    }

    /// <summary>
    /// Returns the essential coverage in one currency (K07): usable money divided by the median essential consumption
    /// of the last three complete financial months plus the monthly shares of essential non-monthly plans. Entries that
    /// settled such plans are replaced by their shares, so nothing counts twice.
    /// </summary>
    public static EssentialCoverage Coverage(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<Category> categories,
        IEnumerable<Schedule> schedules,
        string currencyCode,
        DateOnly today,
        PeriodCalendar calendar,
        int startDay = 1)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(categories);
        var accountList = accounts.ToList();
        var categoryById = categories.ToDictionary(c => c.Id);
        bool Essential(Guid? id) => id is { } value && categoryById.TryGetValue(value, out var category)
            && (category.IsEssential || (category.ParentId is { } parent && categoryById.TryGetValue(parent, out var top) && top.IsEssential));
        var nonMonthly = PlanActions.InForce(schedules).Where(s => s.Kind == EntryKind.Expense && Essential(s.CategoryId) && BudgetPlanning.MonthlyEquivalent(s) is not null).ToList();
        var nonMonthlyIds = nonMonthly.Select(s => s.Id).ToHashSet();
        var usable = accountList.Where(a => !a.IsArchived && a.UsableForPayments && string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)).ToList();
        var usableMoney = usable.Sum(a => LedgerCalculator.Balance(a, entries, today));
        var scope = accountList.Where(a => a.IncludeInTotals && string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)).Select(a => a.Id).ToList();

        var months = new List<long>();
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (year, month) = PeriodMath.Previous(current.Year, current.Month);
        var oldest = today;
        var relevant = entries.Where(e => !(e.ScheduleId is { } plan && nonMonthlyIds.Contains(plan)) && Essential(e.CategoryId)).ToList();
        for (var i = 0; i < 3; i++)
        {
            var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
            oldest = from;
            months.Add(LedgerCalculator.ExpenseByCategory(accountList, relevant, new LedgerFilter(from, to, scope), id => id).Sum(c => c.Net));
            (year, month) = PeriodMath.Previous(year, month);
        }

        var share = nonMonthly.Where(s => accountList.Any(a => a.Id == s.AccountId && string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)))
            .Sum(s => BudgetPlanning.MonthlyEquivalent(s)!.Value);
        // Three complete months of data are needed; inside them, no essential spending at all is "not available".
        if (!HasHistory(accountList, entries, scope, oldest))
        {
            return new EssentialCoverage(currencyCode, usableMoney, 0, null, CoverageStatus.NotEnoughHistory);
        }

        var monthly = Median(months) + share;
        if (monthly <= 0)
        {
            return new EssentialCoverage(currencyCode, usableMoney, monthly, null, CoverageStatus.NotAvailable);
        }

        return usableMoney < 0
            ? new EssentialCoverage(currencyCode, usableMoney, monthly, 0m, CoverageStatus.BalanceNegative)
            : new EssentialCoverage(currencyCode, usableMoney, monthly, Math.Round((decimal)usableMoney / monthly, 1, MidpointRounding.AwayFromZero), CoverageStatus.Ok);
    }

    /// <summary>Returns the median of the values (the mean of the two middle ones for an even count, rounded half away from zero).</summary>
    public static long Median(IReadOnlyCollection<long> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (long)Math.Round((sorted[middle - 1] + sorted[middle]) / 2m, MidpointRounding.AwayFromZero);
    }

    // Whether the scope has data from the start of the oldest of the three months on (an account opened or an entry
    // recorded by then), so "no essential spending" is a fact, not a lack of history.
    private static bool HasHistory(IReadOnlyCollection<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, IReadOnlyCollection<Guid> scope, DateOnly oldest) =>
        accounts.Any(a => scope.Contains(a.Id) && a.OpeningDate <= oldest) || entries.Any(e => scope.Contains(e.AccountId) && e.Date <= oldest);

    // A payment leaves the usable money: an expense or income reversal of any account, or a transfer from a usable
    // account to one that is not usable (a card, a loan, a savings account kept aside).
    private static bool IsOutgoing(Schedule schedule, IReadOnlyDictionary<Guid, Account> accounts) => schedule.Kind switch
    {
        EntryKind.Expense or EntryKind.IncomeReversal => true,
        EntryKind.Transfer => accounts.TryGetValue(schedule.AccountId, out var source) && Usable(source)
            && !(schedule.ToAccountId is { } to && accounts.TryGetValue(to, out var destination) && Usable(destination)),
        _ => false,
    };

    private static bool Usable(Account account) => !account.IsArchived && account.UsableForPayments;
}
