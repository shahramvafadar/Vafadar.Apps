using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>
/// A quick template for recurring manual entries such as "Coffee" or "Fuel" (TX-04). Applying it only fills a new
/// entry form; it never creates an entry by itself and has nothing to do with plans or their occurrences.
/// </summary>
public sealed class EntryTemplate : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name shown on the template button.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the entry kind (income, expense or transfer).</summary>
    public EntryKind Kind { get; set; }

    /// <summary>Gets or sets the account.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the destination account of a transfer.</summary>
    public Guid? ToAccountId { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the usual amount in minor units; <see langword="null"/> asks for it every time.</summary>
    public long? Amount { get; set; }

    /// <summary>Gets or sets the title of the new entry.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the payee of the new entry.</summary>
    public string? Payee { get; set; }

    /// <summary>Gets or sets the icon of the new entry.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the position among the templates.</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Creates a template from an entry. Date, note, review state, plan and refund links are not copied, so a template
    /// can never settle an occurrence or refund a purchase again.
    /// </summary>
    public static EntryTemplate From(LedgerEntry entry, string name, bool keepAmount)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (entry.Kind is not (EntryKind.Income or EntryKind.Expense or EntryKind.Transfer))
        {
            throw new ArgumentException("Only income, expense and transfer entries can become templates.", nameof(entry));
        }

        return new EntryTemplate
        {
            Name = name.Trim(),
            Kind = entry.Kind,
            AccountId = entry.AccountId,
            ToAccountId = entry.Kind == EntryKind.Transfer ? entry.ToAccountId : null,
            CategoryId = entry.Kind == EntryKind.Transfer ? null : entry.CategoryId,
            Amount = keepAmount && entry.Amount > 0 ? entry.Amount : null,
            Title = entry.Title,
            Payee = entry.Payee,
            Icon = entry.Icon,
        };
    }

    /// <summary>Creates the unsaved entry a template fills in, dated <paramref name="date"/>.</summary>
    public LedgerEntry CreateEntry(DateOnly date) => new()
    {
        Kind = Kind,
        Date = date,
        AccountId = AccountId,
        ToAccountId = ToAccountId,
        CategoryId = CategoryId,
        Amount = Amount ?? 0,
        Title = Title,
        Payee = Payee,
        Icon = Icon,
    };
}