namespace Vafadar.Finance.Core.Ledger;

/// <summary>Creates derived entries: duplicates, refunds and transfer fees (TX-05, REF-01..04, FIN-02).</summary>
public static class EntryActions
{
    /// <summary>
    /// Copies an entry as a new, confirmed, manual entry on <paramref name="date"/>. Links to plans, imports, groups
    /// and refunded purchases are not copied, so the copy never settles an occurrence or over-refunds a purchase.
    /// </summary>
    public static LedgerEntry Duplicate(LedgerEntry source, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new LedgerEntry
        {
            Kind = source.Kind,
            Date = date,
            AccountId = source.AccountId,
            Amount = source.Amount,
            ToAccountId = source.ToAccountId,
            ToAmount = source.ToAmount,
            Direction = source.Direction,
            CategoryId = source.CategoryId,
            Title = source.Title,
            Payee = source.Payee,
            Note = source.Note,
            Icon = source.Icon,
            OriginalAmount = source.OriginalAmount,
            OriginalCurrencyCode = source.OriginalCurrencyCode,
            Review = ReviewState.Confirmed,
            Source = EntrySource.Manual,
        };
    }

    /// <summary>Returns the amount of <paramref name="purchase"/> that can still be refunded (REF-04).</summary>
    /// <param name="purchase">The expense.</param>
    /// <param name="entries">Entries that may contain refunds linked to the purchase.</param>
    /// <param name="exceptRefundId">A refund to leave out, e.g. the one being edited.</param>
    public static long Refundable(LedgerEntry purchase, IEnumerable<LedgerEntry> entries, Guid? exceptRefundId = null)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        ArgumentNullException.ThrowIfNull(entries);
        if (purchase.Kind != EntryKind.Expense)
        {
            return 0;
        }

        var refunded = entries.Where(e => e.Kind == EntryKind.Refund && e.RefundOfId == purchase.Id && e.Id != exceptRefundId).Sum(e => e.Amount);
        return Math.Max(0, purchase.Amount - refunded);
    }

    /// <summary>Creates a refund linked to <paramref name="purchase"/> in the purchase's category (REF-01/02).</summary>
    public static LedgerEntry CreateRefund(LedgerEntry purchase, long amount, Guid accountId, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        return new LedgerEntry
        {
            Kind = EntryKind.Refund,
            Date = date,
            AccountId = accountId,
            Amount = amount,
            CategoryId = purchase.CategoryId,
            Title = purchase.Title,
            Payee = purchase.Payee,
            Icon = purchase.Icon,
            RefundOfId = purchase.Id,
            Review = ReviewState.Confirmed,
            Source = EntrySource.Manual,
        };
    }

    /// <summary>
    /// Brings the fee of a transfer in line with the transfer: an expense of the source account on the same date,
    /// grouped with the transfer so both are shown, edited and deleted together (FIN-02).
    /// </summary>
    /// <param name="transfer">The transfer; gets a group id when it has none and a fee is added.</param>
    /// <param name="existingFee">The current fee entry, or <see langword="null"/>.</param>
    /// <param name="fee">The fee in minor units of the source account; 0 means no fee.</param>
    /// <param name="categoryId">Category of a new fee entry.</param>
    /// <returns>The fee entry to save, or <see langword="null"/> when there is no fee (delete <paramref name="existingFee"/>).</returns>
    public static LedgerEntry? SyncTransferFee(LedgerEntry transfer, LedgerEntry? existingFee, long fee, Guid? categoryId)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        if (fee <= 0)
        {
            return null;
        }

        transfer.GroupId ??= Guid.CreateVersion7();
        var entry = existingFee ?? new LedgerEntry { Kind = EntryKind.Expense, CategoryId = categoryId };
        entry.Date = transfer.Date;
        entry.AccountId = transfer.AccountId;
        entry.Amount = fee;
        entry.GroupId = transfer.GroupId;
        entry.Review = transfer.Review;
        entry.Source = transfer.Source;
        return entry;
    }

    /// <summary>Returns the fee entry grouped with <paramref name="transfer"/>, if any.</summary>
    public static LedgerEntry? FindTransferFee(LedgerEntry transfer, IEnumerable<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(entries);
        return transfer.GroupId is { } group
            ? entries.FirstOrDefault(e => e.GroupId == group && e.Id != transfer.Id && e.Kind == EntryKind.Expense)
            : null;
    }

    /// <summary>
    /// Returns whether an event can be split across categories (F2-TX-01): one income or expense without plan or refund
    /// link, or the parts of an earlier split. A transfer fee (grouped with its transfer) cannot be split.
    /// </summary>
    /// <param name="group">The entry and, when it belongs to a group, all entries of that group.</param>
    public static bool CanSplit(IReadOnlyCollection<LedgerEntry> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.Count > 0
            && group.All(e => e.Kind is EntryKind.Expense or EntryKind.Income && e.ScheduleId is null && e.RefundOfId is null)
            && (group.Count == 1 ? group.First().GroupId is null : IsSplit(group));
    }

    /// <summary>Returns whether a group of entries is a split purchase or income rather than a transfer with a fee.</summary>
    public static bool IsSplit(IReadOnlyCollection<LedgerEntry> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.Count >= 2 && group.All(e => e.Kind == group.First().Kind && e.Kind is EntryKind.Expense or EntryKind.Income);
    }

    /// <summary>
    /// Splits an event across categories (F2-TX-01): the parts share one group, date, account, title and payee and sum
    /// exactly to the event amount, so the balance changes by the same total and every category budget sees its share.
    /// The first part keeps the original entry id, so refunds and links to it stay valid.
    /// </summary>
    /// <param name="parts">The existing entries of the event: one entry, or all parts of an earlier split.</param>
    /// <param name="shares">Category and amount of each part, at least two, each greater than zero.</param>
    /// <returns>The entries to save and the ids of parts to delete.</returns>
    public static (IReadOnlyList<LedgerEntry> Save, IReadOnlyList<Guid> Delete) Split(IReadOnlyList<LedgerEntry> parts, IReadOnlyList<(Guid? CategoryId, long Amount)> shares)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(shares);
        if (!CanSplit(parts))
        {
            throw new ArgumentException("Only income or expense entries without plan or refund links can be split.", nameof(parts));
        }

        if (shares.Count < 2 || shares.Any(s => s.Amount <= 0))
        {
            throw new ArgumentException("A split needs at least two parts with positive amounts.", nameof(shares));
        }

        var total = parts.Sum(p => p.Amount);
        if (shares.Sum(s => s.Amount) != total)
        {
            throw new ArgumentException("The parts must add up exactly to the amount.", nameof(shares));
        }

        var first = parts[0];
        var group = first.GroupId ?? Guid.CreateVersion7();
        var save = new List<LedgerEntry>();
        for (var i = 0; i < shares.Count; i++)
        {
            var part = i < parts.Count ? parts[i] : new LedgerEntry
            {
                Kind = first.Kind,
                AccountId = first.AccountId,
                Date = first.Date,
                Title = first.Title,
                Payee = first.Payee,
                Note = first.Note,
                Icon = first.Icon,
                Review = first.Review,
                Source = first.Source,
            };
            part.GroupId = group;
            part.CategoryId = shares[i].CategoryId;
            part.Amount = shares[i].Amount;

            // A foreign amount belongs to the whole event and cannot be divided exactly; parts keep only the account amount.
            part.OriginalAmount = null;
            part.OriginalCurrencyCode = null;
            save.Add(part);
        }

        return (save, [.. parts.Skip(shares.Count).Select(p => p.Id)]);
    }

    /// <summary>Joins the parts of a split into one entry with the total amount, keeping the first part's id and category.</summary>
    public static (LedgerEntry Save, IReadOnlyList<Guid> Delete) Join(IReadOnlyList<LedgerEntry> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (!IsSplit(parts))
        {
            throw new ArgumentException("These entries are not a split.", nameof(parts));
        }

        var first = parts[0];
        first.Amount = parts.Sum(p => p.Amount);
        first.GroupId = null;
        return (first, [.. parts.Skip(1).Select(p => p.Id)]);
    }
}