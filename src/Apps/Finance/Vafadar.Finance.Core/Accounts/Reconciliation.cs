using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Accounts;

/// <summary>The comparison of an observed balance with the recorded one (ACC-08).</summary>
/// <param name="Recorded">Recorded balance at the end of the date.</param>
/// <param name="Observed">Balance the user sees, e.g. in the banking app.</param>
/// <param name="UnreviewedCount">Unreviewed entries of the account up to the date – check these first.</param>
/// <param name="PossibleDuplicates">Entries sharing date, kind and amount with another entry of the account.</param>
public sealed record ReconciliationResult(long Recorded, long Observed, int UnreviewedCount, int PossibleDuplicates)
{
    /// <summary>Gets observed minus recorded; zero means the account matches.</summary>
    public long Difference => Observed - Recorded;
}

/// <summary>
/// Manual reconciliation (ACC-08): compare, suggest what to review, and only then record an adjustment with a reason.
/// An adjustment changes the balance but never income, expense or budgets (AT-11).
/// </summary>
public static class Reconciliation
{
    /// <summary>Compares <paramref name="observed"/> with the recorded balance of <paramref name="account"/> on <paramref name="date"/>.</summary>
    public static ReconciliationResult Analyze(Account account, IReadOnlyCollection<LedgerEntry> entries, long observed, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(entries);
        var own = entries.Where(e => (e.AccountId == account.Id || e.ToAccountId == account.Id) && e.Date <= date && e.Date >= account.OpeningDate).ToList();
        var duplicates = own.GroupBy(e => (e.Date, e.Kind, e.Amount)).Where(g => g.Count() > 1).Sum(g => g.Count());
        return new ReconciliationResult(
            LedgerCalculator.Balance(account, entries, date),
            observed,
            own.Count(e => e.Review == ReviewState.Unreviewed),
            duplicates);
    }

    /// <summary>Creates the adjustment that makes the recorded balance equal the observed one, or <see langword="null"/> when they match.</summary>
    public static LedgerEntry? CreateAdjustment(Account account, ReconciliationResult result, DateOnly date, string reason)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(result);
        if (result.Difference == 0)
        {
            return null;
        }

        return new LedgerEntry
        {
            Kind = EntryKind.Adjustment,
            AccountId = account.Id,
            Date = date,
            Amount = Math.Abs(result.Difference),
            Direction = result.Difference > 0 ? AdjustmentDirection.Increase : AdjustmentDirection.Decrease,
            Note = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Review = ReviewState.Confirmed,
            Source = EntrySource.Manual,
        };
    }
}
