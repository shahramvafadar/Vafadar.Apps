using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Categories;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>A reason an entry cannot be saved. The UI translates each value as <c>LedgerError_{Name}</c>.</summary>
public enum LedgerError
{
    /// <summary>Amount must be greater than zero (FIN-06).</summary>
    AmountMustBePositive,

    /// <summary>The account does not exist.</summary>
    UnknownAccount,

    /// <summary>New entries cannot be booked on an archived account (ACC-06).</summary>
    AccountArchived,

    /// <summary>A transfer needs a destination account.</summary>
    DestinationRequired,

    /// <summary>A transfer cannot go to the same account (FIN-02).</summary>
    SameAccountTransfer,

    /// <summary>A transfer between currencies needs the destination amount (FX-03).</summary>
    DestinationAmountRequired,

    /// <summary>The category does not fit the entry kind.</summary>
    CategoryKindMismatch,

    /// <summary>An adjustment needs a direction.</summary>
    AdjustmentDirectionRequired,

    /// <summary>Linked refunds would exceed the purchase amount (REF-04).</summary>
    RefundExceedsPurchase,

    /// <summary>A refund can only refer to an expense.</summary>
    RefundOriginalNotExpense,

    /// <summary>The foreign amount needs a valid, different currency.</summary>
    InvalidOriginalCurrency,
}

/// <summary>
/// Validates an entry against the domain invariants (docs/02-domain-design.md §10) before it is saved.
/// </summary>
public static class LedgerValidator
{
    /// <summary>Returns all problems of <paramref name="entry"/>; empty means valid.</summary>
    /// <param name="entry">The entry to check.</param>
    /// <param name="accounts">All accounts by id.</param>
    /// <param name="categories">All categories by id.</param>
    /// <param name="refundOriginal">The purchase a refund refers to, if any.</param>
    /// <param name="otherRefundsOfOriginal">Sum of other refunds already linked to that purchase (minor units).</param>
    /// <param name="isNew">Whether the entry is new (archived accounts only block new entries).</param>
    public static IReadOnlyList<LedgerError> Validate(
        LedgerEntry entry,
        IReadOnlyDictionary<Guid, Account> accounts,
        IReadOnlyDictionary<Guid, Category> categories,
        LedgerEntry? refundOriginal = null,
        long otherRefundsOfOriginal = 0,
        bool isNew = true)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(categories);

        var errors = new List<LedgerError>();

        if (entry.Amount <= 0)
        {
            errors.Add(LedgerError.AmountMustBePositive);
        }

        if (!accounts.TryGetValue(entry.AccountId, out var account))
        {
            errors.Add(LedgerError.UnknownAccount);
            return errors;
        }

        if (isNew && account.IsArchived)
        {
            errors.Add(LedgerError.AccountArchived);
        }

        switch (entry.Kind)
        {
            case EntryKind.Transfer:
                ValidateTransfer(entry, accounts, account, isNew, errors);
                break;

            case EntryKind.Adjustment when entry.Direction is null:
                errors.Add(LedgerError.AdjustmentDirectionRequired);
                break;
        }

        if (entry.CategoryId is { } categoryId && categories.TryGetValue(categoryId, out var category))
        {
            var expected = entry.Kind switch
            {
                EntryKind.Income or EntryKind.IncomeReversal => CategoryKind.Income,
                EntryKind.Expense or EntryKind.Refund => CategoryKind.Expense,
                _ => (CategoryKind?)null,
            };

            if (expected != category.Kind)
            {
                errors.Add(LedgerError.CategoryKindMismatch);
            }
        }

        if (entry.Kind == EntryKind.Refund && refundOriginal is not null)
        {
            if (refundOriginal.Kind != EntryKind.Expense)
            {
                errors.Add(LedgerError.RefundOriginalNotExpense);
            }
            else if (otherRefundsOfOriginal + entry.Amount > refundOriginal.Amount)
            {
                errors.Add(LedgerError.RefundExceedsPurchase);
            }
        }

        if (entry.OriginalAmount is not null || entry.OriginalCurrencyCode is not null)
        {
            if (entry.OriginalAmount is not > 0
                || !Money.Currencies.TryGet(entry.OriginalCurrencyCode, out _)
                || string.Equals(entry.OriginalCurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(LedgerError.InvalidOriginalCurrency);
            }
        }

        return errors;
    }

    private static void ValidateTransfer(LedgerEntry entry, IReadOnlyDictionary<Guid, Account> accounts, Account source, bool isNew, List<LedgerError> errors)
    {
        if (entry.ToAccountId is not { } toId || !accounts.TryGetValue(toId, out var destination))
        {
            errors.Add(LedgerError.DestinationRequired);
            return;
        }

        if (toId == entry.AccountId)
        {
            errors.Add(LedgerError.SameAccountTransfer);
        }

        if (isNew && destination.IsArchived && !errors.Contains(LedgerError.AccountArchived))
        {
            errors.Add(LedgerError.AccountArchived);
        }

        if (!string.Equals(source.CurrencyCode, destination.CurrencyCode, StringComparison.OrdinalIgnoreCase) && entry.ToAmount is not > 0)
        {
            errors.Add(LedgerError.DestinationAmountRequired);
        }
    }
}
