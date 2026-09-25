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
}

/// <summary>
/// A financial account in the ledger – not a login account (§5.1). Its balance is always in its own currency.
/// </summary>
public sealed class Account : Entity, IAuditableEntity
{
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
