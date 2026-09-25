using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Rates;

/// <summary>
/// A manually entered exchange rate: 1 <see cref="FromCurrencyCode"/> = <see cref="Rate"/> <see cref="ToCurrencyCode"/>
/// on <see cref="Date"/> (FX-02). Used only to value totals in the report currency; recorded amounts never change.
/// </summary>
public sealed class ExchangeRate : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the date the rate applies to.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the source currency.</summary>
    public required string FromCurrencyCode { get; set; }

    /// <summary>Gets or sets the target currency.</summary>
    public required string ToCurrencyCode { get; set; }

    /// <summary>Gets or sets the rate.</summary>
    public decimal Rate { get; set; }

    /// <summary>Gets or sets a value indicating whether the rate is an estimate rather than an actual exchange.</summary>
    public bool IsEstimate { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}
