using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Accounts;

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

/// <summary>The group an account is listed in (ZEX-S0202): each group has its own totals, never mixed with another.</summary>
public enum AccountGroup
{
    /// <summary>Cash, checking and savings.</summary>
    Money = 0,

    /// <summary>Credit cards.</summary>
    CreditCards = 1,

    /// <summary>Money lent: what others owe the user.</summary>
    Receivables = 2,

    /// <summary>Loans: what the user owes.</summary>
    Debts = 3,

    /// <summary>Valued assets such as a car or a property.</summary>
    ValuedAssets = 4,
}

/// <summary>Helpers for account types.</summary>
public static class AccountTypes
{
    /// <summary>Returns the group an account of this type is listed in.</summary>
    public static AccountGroup GroupOf(this AccountType type) => type switch
    {
        AccountType.CreditCard => AccountGroup.CreditCards,
        AccountType.Lent => AccountGroup.Receivables,
        AccountType.Loan => AccountGroup.Debts,
        AccountType.Asset => AccountGroup.ValuedAssets,
        _ => AccountGroup.Money,
    };

    /// <summary>Returns whether the type tracks a debt or a receivable rather than money at hand (ACC-05).</summary>
    public static bool IsDebt(this AccountType type) => type is AccountType.Loan or AccountType.Lent;

    /// <summary>Returns whether the account is not money at hand: debts, receivables and valued assets (ACC-05).</summary>
    public static bool IsOutsideCash(this AccountType type) => type.IsDebt() || type == AccountType.Asset;

    /// <summary>Returns whether new accounts of this type are usable for payments: cash, checking and savings (ZEX-P17).</summary>
    public static bool IsUsableByDefault(this AccountType type) => type is AccountType.Cash or AccountType.Checking or AccountType.Savings;

    /// <summary>Returns whether the type is a money account that can be the default account of new entries (ZEX-MC06).</summary>
    public static bool CanBeDefault(this AccountType type) => type is AccountType.Cash or AccountType.Checking or AccountType.Savings or AccountType.CreditCard;
}

/// <summary>
/// A financial account in the ledger – not a login account (§5.1). Its balance is always in its own currency.
/// </summary>
public sealed class Account : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the lender or borrower of a loan or lent money (F2-DEBT-01).</summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// Gets or sets the nominal annual interest rate in percent of a loan or money lent, used only for the repayment
    /// estimate and the split of installments (F2-DEBT-02); <see langword="null"/> when unknown.
    /// </summary>
    public decimal? InterestRate { get; set; }

    /// <summary>Gets or sets the agreed monthly installment in minor units (F2-DEBT-02); <see langword="null"/> when unknown.</summary>
    public long? Installment { get; set; }
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

    /// <summary>
    /// Gets or sets a value indicating whether the money of this account can pay bills (ZEX-P17): the forecast minimum and
    /// the liquidity numbers use only such accounts. Defaults by type (<see cref="AccountTypes.IsUsableByDefault"/>);
    /// never inferred from language, currency or country.
    /// </summary>
    public bool UsableForPayments { get; set; } = true;

    /// <summary>Gets or sets the optional country of the account (ISO 3166 alpha-2, ZEX-P18); information only, no logic depends on it.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Gets or sets a value indicating whether the account is archived (ACC-06).</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the display order.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
