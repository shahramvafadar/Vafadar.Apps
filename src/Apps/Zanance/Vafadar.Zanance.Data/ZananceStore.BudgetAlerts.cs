using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Actual-file budget alert preparation; choosing activity never changes financial calculations.</summary>
public sealed partial class ZananceStore
{
    /// <summary>Reads every eligible current period with complete original spending and carry, without saving preferences or money.</summary>
    public async Task<BudgetAlertSnapshot> GetBudgetAlertSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var choices = new BudgetChoiceStore(contextFactory, commercialAccess, time);
        var review = await choices.ReadAsync(cancellationToken);
        var current = review.Budgets.Where(budget => budget.AlertsEnabled && budget.TotalLimit is not null
            && BudgetSelectionPolicy.IsCurrent(budget, review.Today, review.MonthStartDay) && review.Work.CanUse(budget.Id)).ToArray();
        var chains = current.ToDictionary(budget => budget.Id, budget => BudgetCarryChain(budget, review.Budgets));
        var ranges = current.Concat(chains.Values.SelectMany(chain => chain)).Select(budget => BudgetPeriods.Range(budget, review.MonthStartDay)).ToArray();
        DateOnly? from = ranges.Length == 0 ? null : ranges.Min(range => range.First);
        DateOnly? to = ranges.Length == 0 ? null : ranges.Max(range => range.Last);
        var inputs = await ReadBudgetAlertInputsAsync(from, to, cancellationToken);
        var candidates = current.Select(budget =>
        {
            var range = BudgetPeriods.Range(budget, review.MonthStartDay);
            var carry = CalculateBudgetCarry(budget, chains[budget.Id], review.MonthStartDay, inputs.Accounts, inputs.Entries, inputs.Categories);
            return new BudgetAlertCandidate(budget.Id, budget.CurrencyCode, new(checked(budget.TotalLimit!.Value + carry.Total),
                FlexCalculator.SpentAgainstLimit(budget, inputs.Accounts, inputs.Entries, inputs.Categories, range.First, range.Last)));
        }).ToArray();
        var snapshot = new BudgetAlertSnapshot(review, candidates, from, to, inputs.Stamp, inputs.ShowDetails);
        await ValidateBudgetAlertSnapshotAsync(snapshot, cancellationToken);
        return snapshot;
    }

    /// <summary>Rechecks file, rights, choices, periods, financial inputs and privacy before and after a native alert effect.</summary>
    public async Task ValidateBudgetAlertSnapshotAsync(BudgetAlertSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var choices = new BudgetChoiceStore(contextFactory, commercialAccess, time);
        await choices.ValidateAsync(snapshot.Choice, cancellationToken);
        var inputs = await ReadBudgetAlertInputsAsync(snapshot.From, snapshot.To, cancellationToken);
        if (inputs.Stamp != snapshot.InputStamp || inputs.ShowDetails != snapshot.ShowDetails)
            throw new InvalidOperationException("Budget alert financial or privacy inputs changed; reload them.");
        await choices.ValidateAsync(snapshot.Choice, cancellationToken);
    }

    // One context owns each bounded read. SQLite async I/O is synchronous; materialize the ledger off the UI thread (D-75).
    private async Task<AlertInputs> ReadBudgetAlertInputsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            var accounts = await db.Accounts.AsNoTracking().OrderBy(account => account.Id).ToListAsync(cancellationToken);
            var categories = await db.Categories.AsNoTracking().OrderBy(category => category.Id).ToListAsync(cancellationToken);
            var entries = from is null || to is null ? [] : await db.Entries.AsNoTracking()
                .Where(entry => entry.Date >= from && entry.Date <= to).OrderBy(entry => entry.Id).ToListAsync(cancellationToken);
            var details = await db.Settings.Select(settings => (bool?)settings.NotificationsShowDetails).FirstOrDefaultAsync(cancellationToken) ?? false;
            var stamp = new StringBuilder();
            // Explicit invariant packets work with Release/AOT and include every input used by the original calculators.
            foreach (var account in accounts) stamp.AppendLine(FormattableString.Invariant(
                $"a|{account.Id:N}|{account.CurrencyCode}|{account.Type}|{account.OpeningDate:yyyy-MM-dd}|{account.IncludeInTotals}|{account.IsArchived}|{account.UpdatedAt:O}"));
            foreach (var category in categories) stamp.AppendLine(FormattableString.Invariant(
                $"c|{category.Id:N}|{category.ParentId:N}|{category.SpendingType}|{category.UpdatedAt:O}"));
            foreach (var entry in entries) stamp.AppendLine(FormattableString.Invariant(
                $"e|{entry.Id:N}|{entry.Date:yyyy-MM-dd}|{entry.Kind}|{entry.AccountId:N}|{entry.Amount}|{entry.CategoryId:N}|{entry.Review}|{entry.CreatedAt:O}|{entry.UpdatedAt:O}"));
            return new AlertInputs(accounts, entries, categories, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(stamp.ToString()))), details);
        }, cancellationToken);
    }

    /// <summary>Original bounded calculator inputs, retained only for the current preparation.</summary>
    private sealed record AlertInputs(List<Account> Accounts, List<LedgerEntry> Entries, List<Category> Categories, string Stamp, bool ShowDetails);

    // Canonical account sets must never mix carries from another definition. Reject duplicate periods instead of guessing a row.
    internal static List<Budget> BudgetCarryChain(Budget budget, IEnumerable<Budget> originals)
    {
        if (budget.Rollover == BudgetRollover.None) return [];
        var identity = BudgetDefinitionKey.CanonicalIdentity(budget.CurrencyCode, budget.Period, budget.AccountIds);
        var sameKind = originals.Where(original => BudgetDefinitionKey.CanonicalIdentity(original.CurrencyCode, original.Period, original.AccountIds) == identity
            && (budget.Period != BudgetPeriod.Month || original.Calendar == budget.Calendar)).ToArray();
        var chain = new List<Budget>();
        if (budget.Period == BudgetPeriod.Month)
        {
            var byMonth = sameKind.ToDictionary(original => (original.Year, original.Month));
            var key = PeriodMath.Previous(budget.Year, budget.Month);
            while (chain.Count < BudgetRolloverCalculator.MaxMonths && byMonth.TryGetValue(key, out var previous))
            { chain.Insert(0, previous); key = PeriodMath.Previous(key.Year, key.Month); }
        }
        else
        {
            var byStart = sameKind.ToDictionary(original => original.PeriodStart);
            var start = BudgetPeriods.Step(budget.Period, budget.PeriodStart, -1);
            while (chain.Count < BudgetRolloverCalculator.MaxMonths && byStart.TryGetValue(start, out var previous))
            { chain.Insert(0, previous); start = BudgetPeriods.Step(budget.Period, start, -1); }
        }
        return chain;
    }

    // Shared by the budget page and alert batch: tiers only decide new activity, never alter original carry/spending.
    internal static BudgetCarry CalculateBudgetCarry(Budget budget, List<Budget> chain, int startDay,
        List<Account> accounts, List<LedgerEntry> entries, List<Category> categories)
    {
        if (chain.Count == 0) return BudgetCarry.None;
        var months = chain.Select(original =>
        {
            var (start, end) = BudgetPeriods.Range(original, startDay);
            var scope = original.AccountIds.Count > 0 ? original.AccountIds : null;
            return new BudgetMonth(original.Rollover, original.TotalLimit,
                original.Method == BudgetMethod.Flex
                    ? FlexCalculator.FlexibleSpent(accounts, entries, categories, start, end, original.CurrencyCode, scope)
                    : BudgetCalculator.NetExpense(accounts, entries, start, end, original.CurrencyCode, scope),
                original.CategoryLimits.ToDictionary(limit => limit.CategoryId, limit => limit.Limit),
                original.CategoryLimits.ToDictionary(limit => limit.CategoryId, limit => BudgetCalculator.NetExpense(accounts, entries,
                    start, end, original.CurrencyCode, scope, [limit.CategoryId], categories)));
        }).ToList();
        return BudgetRolloverCalculator.CarryInto(months, budget.Rollover);
    }
}
