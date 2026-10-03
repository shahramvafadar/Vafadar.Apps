using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Goals;

/// <summary>The eligible income of a period and the entries it consists of (shown in the preview, ZEX-GO09).</summary>
/// <param name="CurrencyCode">The currency.</param>
/// <param name="Total">Income minus income reversals.</param>
/// <param name="Entries">The income entries and reversals that count.</param>
public sealed record EligibleIncomeResult(string CurrencyCode, long Total, IReadOnlyList<LedgerEntry> Entries);

/// <summary>
/// Eligible income for a "share of income" contribution plan (ZEX-GO09): income minus income reversals on the chosen
/// accounts in one currency. Transfers (also loan principal and money moved from savings), refunds and reimbursements,
/// adjustments and opening balances never count – they are not income. Categories the user excludes for this goal
/// (for example a bonus) are left out too.
/// </summary>
public static class EligibleIncome
{
    /// <summary>Returns the eligible income of the period.</summary>
    /// <param name="accounts">All accounts.</param>
    /// <param name="entries">All entries.</param>
    /// <param name="currencyCode">The goal currency; only accounts in it count.</param>
    /// <param name="from">First day.</param>
    /// <param name="to">Last day.</param>
    /// <param name="accountIds">The accounts that count; <see langword="null"/> = the accounts in totals.</param>
    /// <param name="excludedCategoryIds">Income categories left out for this goal.</param>
    public static EligibleIncomeResult Of(
        IEnumerable<Account> accounts,
        IEnumerable<LedgerEntry> entries,
        string currencyCode,
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? accountIds = null,
        IReadOnlyCollection<Guid>? excludedCategoryIds = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var scope = LedgerCalculator.InScope(accounts, accountIds)
            .Where(a => string.Equals(a.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(a => a.Id);
        var counted = entries
            .Where(e => e.Kind is EntryKind.Income or EntryKind.IncomeReversal
                && e.Date >= from && e.Date <= to
                && scope.TryGetValue(e.AccountId, out var account) && !LedgerCalculator.IsBeforeOpening(e, account)
                && (excludedCategoryIds is null || e.CategoryId is not { } category || !excludedCategoryIds.Contains(category)))
            .OrderBy(e => e.Date)
            .ToList();
        var total = counted.Sum(e => e.Kind == EntryKind.Income ? e.Amount : -e.Amount);
        return new EligibleIncomeResult(currencyCode, total, counted);
    }

    /// <summary>Returns the suggestion of a share-of-income plan: percent × eligible income, rounded to the minor unit, never negative.</summary>
    public static long Suggestion(long eligibleIncome, decimal percent) =>
        eligibleIncome <= 0 || percent <= 0 ? 0 : (long)Math.Round(eligibleIncome * percent / 100m, MidpointRounding.AwayFromZero);
}
