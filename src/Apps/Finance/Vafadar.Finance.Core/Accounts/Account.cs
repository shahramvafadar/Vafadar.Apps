using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Accounts;

/// <summary>The kind of money container an account represents (ACC-03).</summary>
public enum AccountType
{
    /// <summary>Cash in hand.</summary>
    Cash = 0,

    /// <summary>Current / checking bank account.</summary>
    Checking = 1,

    /// <summary>Savings account.</summary>
    Savings = 2,

    /// <summary>Simple credit card: purchases increase the debt, payments are transfers (ACC-04).</summary>
    CreditCard = 3,

    /// <summary>
    /// Money borrowed, e.g. a loan or money owed to a friend (F2-DEBT-01). The balance is negative; receiving the money
    /// and repaying the principal are transfers, never income or spending. Interest and fees are expenses.
    /// </summary>
    Loan = 4,

    /// <summary>Money lent to someone; the balance is what they still owe. Lending and getting it back are transfers.</summary>
    Lent = 5,

    /// <summary>
    /// A non-cash asset with a manual valuation, e.g. a car or a property (F2-ASSET-01). A new valuation is a balance
    /// adjustment, never income or spending; an estimated value is not cash.
    /// </summary>
    Asset = 6,
}

/// <summary>Helpers for account types.</summary>
public static class AccountTypes
{
    /// <summary>Returns whether the type tracks a debt or a receivable rather than money at hand (ACC-05).</summary>
    public static bool IsDebt(this AccountType type) => type is AccountType.Loan or AccountType.Lent;

    /// <summary>Returns whether the account is not money at hand: debts, receivables and valued assets (ACC-05).</summary>
    public static bool IsOutsideCash(this AccountType type) => type.IsDebt() || type == AccountType.Asset;
}

/// <summary>
/// A financial account in the ledger – not a login account (§5.1). Its balance is always in its own currency.
/// </summary>
public sealed class Account : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the lender or borrower of a loan or lent money (F2-DEBT-01).</summary>
    public string? Counterparty { get; set; }

    /// <summary>Gets or sets the user-given name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the account type.</summary>
    public AccountType Type { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code. Cannot change once entries exist (ACC-07).</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Gets or sets the balance at the start of <see cref="OpeningDate"/>, in minor units (may be negative).</summary>
    public long OpeningBalance { get; set; }

    /// <summary>Gets or sets the date the opening balance applies to (before that day's entries, FIN-04).</summary>
    public DateOnly OpeningDate { get; set; }

    /// <summary>Gets or sets a value indicating whether the opening balance was explicitly confirmed (ACC-09).</summary>
    public bool OpeningBalanceKnown { get; set; } = true;

    /// <summary>Gets or sets the icon key (Fluent icon name).</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets a value indicating whether the account is part of the main totals.</summary>
    public bool IncludeInTotals { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the account is archived (ACC-06).</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
