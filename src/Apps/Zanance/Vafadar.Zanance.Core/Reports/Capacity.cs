using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>
/// How much could go to goals per financial month in one currency (02 §9.3): <c>I − S − NM − P − G</c>. A suggestion
/// limit, never an allocation; <see cref="Amount"/> is <see langword="null"/> with fewer than three complete months.
/// </summary>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="Income">I – median eligible income of the last three complete months.</param>
/// <param name="Spending">S – median consumption of the same months without entries that settled non-monthly plans.</param>
/// <param name="NonMonthly">NM – monthly shares of active non-monthly expense plans.</param>
/// <param name="LoanPrincipal">P – monthly loan principal of scheduled installments.</param>
/// <param name="OtherGoals">G – contribution plans already accepted for active goals.</param>
public sealed record CapacityResult(string CurrencyCode, long Income, long Spending, long NonMonthly, long LoanPrincipal, long OtherGoals, bool EnoughHistory)
{
    /// <summary>Gets the capacity per month, or <see langword="null"/> without enough history ("not enough history").</summary>
    public long? Amount => EnoughHistory ? Income - Spending - NonMonthly - LoanPrincipal - OtherGoals : null;
}

/// <summary>What a goal needs per month to reach its target on time.</summary>
public sealed record GoalNeed(Guid GoalId, GoalPriority Priority, DateOnly? TargetDate, long PerMonth);

/// <summary>A suggested monthly contribution for a goal; the suggestions never add up to more than the capacity (AT23).</summary>
public sealed record CapacitySuggestion(Guid GoalId, long Needed, long Suggested);

/// <summary>Capacity and the split of suggestions over goals (ZEX-S0604).</summary>
public static class CapacityCalculator
{
    /// <summary>Returns the capacity per month in <paramref name="currencyCode"/>.</summary>
    /// <param name="exceptGoalId">A goal whose own contribution plan is not subtracted (when it is the one being planned).</param>
    public static CapacityResult Compute(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<Schedule> schedules,
        IEnumerable<OccurrenceState> states,
        IEnumerable<Goal> goals,
        IEnumerable<ContributionPlan> contributionPlans,
        string currencyCode,
        DateOnly today,
        PeriodCalendar calendar,
        int startDay = 1,
        Guid? exceptGoalId = null)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(goals);
        ArgumentNullException.ThrowIfNull(contributionPlans);
        bool Same(string code) => string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase);
        var accountList = accounts.ToList();
        var byId = accountList.ToDictionary(a => a.Id);
        var usable = accountList.Where(a => !a.IsArchived && a.UsableForPayments && Same(a.CurrencyCode)).ToList();
        var inForce = PlanActions.InForce(schedules);
        var nonMonthly = inForce.Where(s => s.Kind == EntryKind.Expense && byId.TryGetValue(s.AccountId, out var a) && Same(a.CurrencyCode) && BudgetPlanning.MonthlyEquivalent(s) is not null).ToList();
        var nonMonthlyIds = nonMonthly.Select(s => s.Id).ToHashSet();

        // I and S of the last three complete financial months on the usable accounts.
        var incomes = new List<long>();
        var spending = new List<long>();
        var enough = usable.Count > 0;
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (year, month) = PeriodMath.Previous(current.Year, current.Month);
        var scope = usable.Select(a => a.Id).ToList();
        var regular = entries.Where(e => !(e.ScheduleId is { } plan && nonMonthlyIds.Contains(plan))).ToList();
        for (var i = 0; i < 3 && enough; i++)
        {
            var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
            enough = usable.Min(a => a.OpeningDate) <= from;
            var all = KpiCatalog.Surplus(usable, entries, new LedgerFilter(from, to, scope)).FirstOrDefault();
            var withoutNonMonthly = KpiCatalog.Surplus(usable, regular, new LedgerFilter(from, to, scope)).FirstOrDefault();
            incomes.Add(all?.EligibleIncome ?? 0);
            spending.Add(withoutNonMonthly?.Consumption ?? 0);
            (year, month) = PeriodMath.Previous(year, month);
        }

        var income = KpiCatalog.Median(incomes);
        var nm = nonMonthly.Sum(s => BudgetPlanning.MonthlyEquivalent(s)!.Value);

        // P: the principal part of installments of loans in this currency, per month.
        long principal = 0;
        foreach (var loan in accountList.Where(a => a.Type == AccountType.Loan && !a.IsArchived && Same(a.CurrencyCode)))
        {
            var outstanding = LoanCalculator.Outstanding(loan.Type, LedgerCalculator.Balance(loan, entries, today));
            foreach (var plan in inForce.Where(s => s.Kind == EntryKind.Transfer && s.ToAccountId == loan.Id && (s.ToAmount ?? s.Amount) is > 0))
            {
                var payment = plan.ToAmount ?? plan.Amount!.Value;
                var (_, part) = LoanCalculator.Split(outstanding, loan.InterestRate ?? 0, payment);
                principal += (long)Math.Round(part * Recurrence.PerYear(plan.Rule) / 12m, MidpointRounding.AwayFromZero);
            }
        }

        // G: what other active goals of this currency already plan per month.
        // Quantity goals plan grams or units, never money, so their plans are not part of G.
        var active = goals.Where(g => g.State == GoalState.Active && g.Type != GoalType.HoldingQuantity && Same(g.CurrencyCode) && g.Id != exceptGoalId).Select(g => g.Id).ToHashSet();
        long other = 0;
        foreach (var plan in contributionPlans.Where(p => active.Contains(p.GoalId)))
        {
            var perDate = plan.Method switch
            {
                ContributionMethod.FixedAmount or ContributionMethod.SpendingCut => plan.Amount ?? 0,
                ContributionMethod.ShareOfIncome => (long)Math.Round(income * (plan.Percent ?? 0) / 100m, MidpointRounding.AwayFromZero),
                _ => 0,
            };
            other += (long)Math.Round(perDate * Recurrence.PerYear(plan.Rule) / 12m, MidpointRounding.AwayFromZero);
        }

        return new CapacityResult(currencyCode, income, KpiCatalog.Median(spending), nm, principal, other, enough && incomes.Count == 3);
    }

    /// <summary>
    /// Returns the quantity money capacity buys at a price the user assumes (ZEX-S0703), in the holding type's base unit:
    /// 150 EUR at 60 EUR per g is 2.5 g (2,500 mg). Rounded down; never a valuation of the holding.
    /// </summary>
    public static long QuantityFor(long capacity, long assumedPricePerUnitMilli) =>
        capacity <= 0 || assumedPricePerUnitMilli <= 0 ? 0 : (long)((decimal)capacity * 1_000 * Holdings.Quantities.PerGramOrUnit / assumedPricePerUnitMilli);

    /// <summary>
    /// Returns what a goal needs per month: the remaining amount over the months left to its target date (rounded up),
    /// or the whole remaining amount without a date.
    /// </summary>
    public static GoalNeed Need(Goal goal, long remaining, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(goal);
        if (remaining <= 0)
        {
            return new GoalNeed(goal.Id, goal.Priority, goal.TargetDate, 0);
        }

        if (goal.TargetDate is not { } target || target <= today)
        {
            return new GoalNeed(goal.Id, goal.Priority, goal.TargetDate, remaining);
        }

        var months = Math.Max(1, ((target.Year - today.Year) * 12) + target.Month - today.Month + (target.Day >= today.Day ? 0 : -1));
        return new GoalNeed(goal.Id, goal.Priority, goal.TargetDate, (remaining + months - 1) / months);
    }

    /// <summary>
    /// Splits <paramref name="capacity"/> over the goals by priority, then by target date (earliest first, no date last);
    /// each goal gets at most what it needs and the sum never exceeds the capacity (AT23). Nothing is allocated.
    /// </summary>
    public static IReadOnlyList<CapacitySuggestion> Distribute(long capacity, IEnumerable<GoalNeed> needs)
    {
        ArgumentNullException.ThrowIfNull(needs);
        var left = Math.Max(0, capacity);
        var result = new List<CapacitySuggestion>();
        foreach (var need in needs.OrderBy(n => n.Priority).ThenBy(n => n.TargetDate ?? DateOnly.MaxValue))
        {
            var suggested = Math.Min(left, Math.Max(0, need.PerMonth));
            left -= suggested;
            result.Add(new CapacitySuggestion(need.GoalId, need.PerMonth, suggested));
        }

        return result;
    }
}
