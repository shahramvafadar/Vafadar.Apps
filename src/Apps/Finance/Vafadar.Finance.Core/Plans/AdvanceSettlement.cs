using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Plans;

/// <summary>The result of settling advance payments against the actual bill.</summary>
/// <param name="Advances">The advance payments of the period.</param>
/// <param name="Paid">What the advances paid, minus refunds already received for them.</param>
/// <param name="Actual">The actual bill.</param>
public sealed record SettlementResult(IReadOnlyList<LedgerEntry> Advances, long Paid, long Actual)
{
    /// <summary>Gets the extra payment (positive) or the money back (negative).</summary>
    public long Difference => Actual - Paid;
}

/// <summary>
/// Final settlement of advance payments, e.g. monthly utility advances and a yearly bill (F2-CON-04). The bill is not
/// counted again on top of the advances: only the extra payment becomes an expense, and money back becomes refunds of
/// the advances, so spending ends up exactly at the actual bill.
/// </summary>
public static class AdvanceSettlement
{
    /// <summary>Collects the advances of a plan within a period and compares them with the actual bill.</summary>
    public static SettlementResult Compute(Schedule plan, IReadOnlyCollection<LedgerEntry> entries, DateOnly from, DateOnly to, long actual)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(entries);
        var advances = entries
            .Where(e => e.ScheduleId == plan.Id && e.Kind == EntryKind.Expense && e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date)
            .ToList();
        var paid = advances.Sum(a => EntryActions.Refundable(a, entries));
        return new SettlementResult(advances, paid, actual);
    }

    /// <summary>
    /// Creates the entries of the settlement: one expense for an extra payment, or refunds spread over the advances
    /// (newest first, each within what is still refundable) for money back. No entry when the bill matches exactly.
    /// </summary>
    public static IReadOnlyList<LedgerEntry> CreateEntries(Schedule plan, SettlementResult result, IReadOnlyCollection<LedgerEntry> entries, DateOnly date, string title)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(entries);
        if (result.Difference > 0)
        {
            return
            [
                new LedgerEntry
                {
                    Kind = EntryKind.Expense,
                    Date = date,
                    AccountId = plan.AccountId,
                    Amount = result.Difference,
                    CategoryId = plan.CategoryId,
                    Title = title,
                    Icon = plan.Icon,
                    Review = ReviewState.Confirmed,
                    Source = EntrySource.Manual,
                },
            ];
        }

        var back = -result.Difference;
        var refunds = new List<LedgerEntry>();
        foreach (var advance in result.Advances.Reverse())
        {
            if (back <= 0)
            {
                break;
            }

            var part = Math.Min(back, EntryActions.Refundable(advance, entries));
            if (part <= 0)
            {
                continue;
            }

            var refund = EntryActions.CreateRefund(advance, part, advance.AccountId, date);
            refund.Title = title;
            refunds.Add(refund);
            back -= part;
        }

        if (back > 0)
        {
            throw new InvalidOperationException("The money back is larger than the advances of the period.");
        }

        return refunds;
    }
}
