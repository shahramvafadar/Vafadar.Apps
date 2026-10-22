using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Budget;

/// <summary>One original category's financial status, before any native row or localized text is published.</summary>
public sealed record BudgetCategoryProjection(Guid CategoryId, BudgetStatus Status, long OwnLimit, long Carry);

/// <summary>Complete financial computation for one captured budget period; no control or mutable UI state is accessed.</summary>
public sealed record BudgetFinancialProjection(BudgetCarry Carry, BudgetStatus? Total,
    IReadOnlyList<BudgetCategoryProjection> Categories, FlexSummary? Flex, EnvelopeSummary? Envelope,
    bool EnvelopeOutsideCurrentPeriod, int Unreviewed, PlannedExpense Planned, long Equivalent)
{
    /// <summary>Runs the actual financial algorithms on a worker, keeping original today/filter/period inputs fixed.</summary>
    public static Task<BudgetFinancialProjection> PrepareAsync(BudgetReadSnapshot snapshot,
        Core.Budgets.Budget? budget, DateOnly from, DateOnly to, string currency, DateOnly today,
        bool confirmedOnly, CancellationToken cancellationToken = default) => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var accounts = snapshot.Review.Accounts;
            var categories = snapshot.Review.Categories;
            var entries = snapshot.Entries.Where(entry => entry.Date >= from && entry.Date <= to).ToList();
            var carry = budget is null ? BudgetCarry.None : snapshot.CarryFor(budget);
            var monthly = budget?.Period == BudgetPeriod.Month;
            var flex = monthly && budget?.Method == BudgetMethod.Flex
                ? FlexCalculator.Summarize((budget.TotalLimit ?? 0) + carry.Total, accounts, entries,
                    snapshot.Schedules, snapshot.States, categories, from, to, currency, today,
                    budget.AccountIds.Count > 0 ? budget.AccountIds : null, confirmedOnly) : null;
            BudgetStatus? total = null;
            var lines = new List<BudgetCategoryProjection>();
            if (budget is not null)
            {
                var scope = budget.AccountIds.Count > 0 ? budget.AccountIds : null;
                if (budget.TotalLimit is { } limit)
                    total = flex?.Flexible ?? new BudgetStatus(limit + carry.Total,
                        BudgetCalculator.NetExpense(accounts, entries, from, to, currency, scope, confirmedOnly: confirmedOnly));
                if (flex is null)
                    foreach (var line in budget.CategoryLimits)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var carried = carry.For(line.CategoryId);
                        lines.Add(new(line.CategoryId, new(line.Limit + carried,
                            BudgetCalculator.NetExpense(accounts, entries, from, to, currency, scope,
                                [line.CategoryId], categories, confirmedOnly)), line.Limit, carried));
                    }
            }
            EnvelopeSummary? envelope = null;
            var outside = monthly && budget?.Method == BudgetMethod.Envelopes && (today < from || today > to);
            if (monthly && budget?.Method == BudgetMethod.Envelopes && !outside)
            {
                var cash = LedgerCalculator.InScope(accounts, budget.AccountIds.Count > 0 ? budget.AccountIds : null)
                    .Where(account => !account.IsArchived && !account.Type.IsOutsideCash()
                        && string.Equals(account.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase));
                // Envelope cash uses all history to today, independently of the visible period and review filter.
                var balances = cash.ToDictionary(account => account.Id,
                    account => LedgerCalculator.Balance(account, snapshot.Entries, today));
                var earmarked = Core.Goals.GoalCalculator.Accounts(snapshot.Goals, snapshot.Allocations, balances)
                    .Sum(item => Math.Clamp(item.Earmarked, 0, Math.Max(0, item.Balance)));
                envelope = EnvelopeCalculator.Summarize(balances.Values.Sum(), earmarked, lines.Select(line => line.Status));
            }
            var planned = BudgetPlanning.PlannedInPeriod(snapshot.Schedules, snapshot.States,
                accounts.ToDictionary(account => account.Id), from, to, currency, today);
            var equivalent = snapshot.Schedules.Where(schedule => schedule.State == Core.Plans.ScheduleState.Active
                    && schedule.Kind == EntryKind.Expense && accounts.Any(account => account.Id == schedule.AccountId && account.CurrencyCode == currency))
                .Select(BudgetPlanning.MonthlyEquivalent).Sum(value => value ?? 0);
            cancellationToken.ThrowIfCancellationRequested();
            return new BudgetFinancialProjection(carry, total, lines.AsReadOnly(), flex, envelope, outside,
                entries.Count(entry => entry.Review == ReviewState.Unreviewed && entry.Kind is EntryKind.Expense or EntryKind.Refund), planned, equivalent);
        }, cancellationToken);
}
