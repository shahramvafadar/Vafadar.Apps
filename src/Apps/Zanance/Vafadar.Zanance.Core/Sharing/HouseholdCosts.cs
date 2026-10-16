using System.Text.Json.Serialization;
using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Financial display contact, never an authentication, membership, invitation or purchase grant.</summary>
public sealed class HouseholdContact : Entity, IAuditableEntity
{
    /// <summary>Creates a new explicitly entered financial contact.</summary>
    public HouseholdContact() { }
    /// <summary>Restores an existing financial identity without granting service access.</summary>
    [JsonConstructor]
    public HouseholdContact(Guid id) : base(id) { }
    /// <summary>User-visible name, encrypted with the household financial content.</summary>
    public required string Name { get; set; }
    /// <summary>Optional historical service identity label; it is never consulted for authorization.</summary>
    public string? IdentityId { get; set; }
    /// <summary>Retains old allocations after a person leaves; archived contacts cannot receive new allocations.</summary>
    public bool IsArchived { get; set; }
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }
    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A household expense or refund, distinct from a cash account movement or an estimate.</summary>
public enum HouseholdCostKind
{
    /// <summary>Actual cost, shared through explicit minor-unit allocations.</summary>
    Expense = 1,
    /// <summary>Actual refund reducing an identified original household cost.</summary>
    Refund = 2,
}

/// <summary>Explains whether an actual shared-account ledger entry exists, without manufacturing a cash movement.</summary>
public enum HouseholdPaymentKind
{
    /// <summary>A member paid or received money privately; this cost creates no shared-account ledger entry.</summary>
    Private = 1,
    /// <summary>An explicitly linked existing shared-account expense/refund is the one actual cash movement.</summary>
    SharedAccount = 2,
}

/// <summary>One actual cost counted once for the household, never once per member or again through its linked ledger entry.</summary>
public sealed class HouseholdCost : Entity, IAuditableEntity
{
    /// <summary>Creates one explicit actual cost; opening its editor never persists it.</summary>
    public HouseholdCost() { }
    /// <summary>Restores the same cross-device id for idempotent financial publication.</summary>
    [JsonConstructor]
    public HouseholdCost(Guid id) : base(id) { }
    /// <summary>The actual expense/refund kind, independent of the payment source.</summary>
    public HouseholdCostKind Kind { get; set; }
    /// <summary>The user-confirmed financial date, never an operational sync timestamp.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Positive minor-unit magnitude; the kind determines its cost-allocation sign.</summary>
    public long Amount { get; set; }
    /// <summary>Exact ISO4217 unit, never inferred from device display culture.</summary>
    public required string CurrencyCode { get; set; }
    /// <summary>The member who privately paid/received money; null when the shared account is the payer/recipient.</summary>
    public Guid? PaidByContactId { get; set; }
    /// <summary>Private payment or an explicitly linked shared-account ledger movement.</summary>
    public HouseholdPaymentKind PaymentKind { get; set; }
    /// <summary>One existing matching ledger movement for SharedAccount; null for private payments.</summary>
    public Guid? EntryId { get; set; }
    /// <summary>Explicit original household expense for refunds; null for expenses.</summary>
    public Guid? RefundOfCostId { get; set; }
    /// <summary>Explicit user description, included only in encrypted household content.</summary>
    public required string Title { get; set; }
    /// <summary>Optional encrypted user note.</summary>
    public string? Note { get; set; }
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }
    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Exact personal share of one cost; it is not a category split, a debt posting or a second expense.</summary>
public sealed class HouseholdCostAllocation
{
    /// <summary>The single actual cost being divided.</summary>
    public Guid CostId { get; set; }
    /// <summary>The financial contact owing or receiving this share.</summary>
    public Guid ContactId { get; set; }
    /// <summary>Nonnegative exact minor units; all allocations must sum to the cost magnitude.</summary>
    public long Amount { get; set; }
}

/// <summary>A confirmed person-to-person repayment; records its actual facts without automatically posting money.</summary>
public sealed class HouseholdSettlement : Entity, IAuditableEntity
{
    /// <summary>Creates a draft repayment identity.</summary>
    public HouseholdSettlement() { }
    /// <summary>Restores the same immutable identity for cross-device idempotency.</summary>
    [JsonConstructor]
    public HouseholdSettlement(Guid id) : base(id) { }
    /// <summary>The financial contact who actually paid.</summary>
    public Guid FromContactId { get; set; }
    /// <summary>The distinct financial contact who actually received money.</summary>
    public Guid ToContactId { get; set; }
    /// <summary>The actual financial date supplied by the user.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Positive exact minor-unit magnitude.</summary>
    public long Amount { get; set; }
    /// <summary>Exact ISO4217 unit; currencies are never netted together or converted without explicit rates.</summary>
    public required string CurrencyCode { get; set; }
    /// <summary>Optional explicitly linked existing transfer; never creates another ledger movement.</summary>
    public Guid? EntryId { get; set; }
    /// <summary>For a linked cross-currency transfer, selects its actual destination rather than source amount.</summary>
    public bool UsesDestinationAmount { get; set; }
    /// <summary>Optional encrypted note, not an estimated repayment or automatic debt settlement.</summary>
    public string? Note { get; set; }
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }
    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
