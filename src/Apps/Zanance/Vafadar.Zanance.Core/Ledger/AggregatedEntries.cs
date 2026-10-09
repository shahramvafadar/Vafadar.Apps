namespace Vafadar.Zanance.Core.Ledger;

/// <summary>
/// An aggregated entry and the detailed entries that fall into its range: same account, kind and category, dated inside
/// the range (ZEX-P21, AT33). Counting both would count the money twice.
/// </summary>
public sealed record AggregateOverlap(LedgerEntry Aggregate, IReadOnlyList<LedgerEntry> Detailed)
{
    /// <summary>Gets the sum of the detailed entries.</summary>
    public long DetailedTotal => Detailed.Sum(e => e.Amount);

    /// <summary>Gets what the aggregate keeps after "Link and replace": the difference, never negative; 0 removes it.</summary>
    public long Remainder => Math.Max(0, Aggregate.Amount - DetailedTotal);
}

/// <summary>
/// Aggregated entries (ZEX-S0611): an entry may sum up several purchases or receipts of a date range ("Groceries,
/// September, 412 EUR"). It counts like any entry; when detailed entries for the same account, kind and category are
/// added inside its range, the user chooses <i>Link and replace</i> (the aggregate keeps only the difference) or
/// <i>Keep both</i> (counts twice). Nothing is changed without that choice.
/// </summary>
public static class AggregatedEntries
{
    /// <summary>
    /// Whether a standard income/expense aggregate can be reduced without guessing how to redistribute a foreign
    /// amount, reimbursement, related group or plan settlement. Refund relationships are checked by the data layer.
    /// </summary>
    public static bool CanLink(LedgerEntry aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.IsAggregated && aggregate.Kind is EntryKind.Income or EntryKind.Expense
            && (aggregate.AggregatedFrom ?? aggregate.Date) <= (aggregate.AggregatedTo ?? aggregate.Date)
            && aggregate.ScheduleId is null && aggregate.OccurrenceDate is null && aggregate.GroupId is null
            && aggregate.RefundOfId is null && aggregate.ReimbursableAmount is null && aggregate.OriginalAmount is null
            && aggregate.OriginalCurrencyCode is null;
    }

    /// <summary>Returns whether <paramref name="detailed"/> belongs into the range of <paramref name="aggregate"/>.</summary>
    public static bool Covers(LedgerEntry aggregate, LedgerEntry detailed)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(detailed);
        return aggregate.IsAggregated
            && !detailed.IsAggregated
            && aggregate.Id != detailed.Id
            && aggregate.AccountId == detailed.AccountId
            && aggregate.Kind == detailed.Kind
            && aggregate.CategoryId == detailed.CategoryId
            && detailed.Date >= (aggregate.AggregatedFrom ?? aggregate.Date)
            && detailed.Date <= (aggregate.AggregatedTo ?? aggregate.Date);
    }

    /// <summary>
    /// Returns the overlaps that saving or importing <paramref name="changed"/> creates with the other entries: a detailed
    /// entry inside an aggregate's range, or an aggregate whose range covers existing detailed entries.
    /// </summary>
    public static IReadOnlyList<AggregateOverlap> Find(IEnumerable<LedgerEntry> changed, IReadOnlyCollection<LedgerEntry> existing)
    {
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(existing);
        var changedList = changed.ToList();
        var all = existing.Where(e => changedList.All(c => c.Id != e.Id)).Concat(changedList).ToList();
        var result = new List<AggregateOverlap>();
        foreach (var aggregate in all.Where(e => e.IsAggregated))
        {
            var detailed = all.Where(e => Covers(aggregate, e)).ToList();

            // Only overlaps that the change brings in: the aggregate itself changed, or one of its detailed entries did.
            if (detailed.Count > 0 && (changedList.Any(c => c.Id == aggregate.Id) || detailed.Any(d => changedList.Any(c => c.Id == d.Id))))
            {
                result.Add(new AggregateOverlap(aggregate, detailed));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns overlaps caused by accepted new import rows. An existing remainder is reduced only by the new details,
    /// never by details from an earlier import. A newly imported aggregate can cover existing and new details (D-72).
    /// Repeated or already present ids count once; account, kind, category and inclusive dates still define coverage.
    /// </summary>
    public static IReadOnlyList<AggregateOverlap> FindForImport(IEnumerable<LedgerEntry> incoming, IReadOnlyCollection<LedgerEntry> existing)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(existing);
        var known = existing.Select(e => e.Id).ToHashSet();
        var added = incoming.Where(e => !known.Contains(e.Id)).DistinctBy(e => e.Id).ToList();
        var addedIds = added.Select(e => e.Id).ToHashSet();
        var all = existing.Concat(added).ToList();
        return [.. all.Where(e => e.IsAggregated).Select(aggregate => new AggregateOverlap(aggregate,
                [.. all.Where(detail => Covers(aggregate, detail) && (addedIds.Contains(aggregate.Id) || addedIds.Contains(detail.Id)))]))
            .Where(overlap => overlap.Detailed.Count > 0)];
    }

    /// <summary>
    /// Returns the aggregate after "Link and replace": a copy with the remaining difference, or <see langword="null"/>
    /// when the detailed entries cover all of it (the aggregate is removed). The original is not changed, so Undo can put
    /// it back.
    /// </summary>
    public static LedgerEntry? Replace(AggregateOverlap overlap)
    {
        ArgumentNullException.ThrowIfNull(overlap);
        if (overlap.Remainder == 0)
        {
            return null;
        }

        var remaining = overlap.Aggregate.Copy();
        remaining.Amount = overlap.Remainder;
        return remaining;
    }
}
