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
}
