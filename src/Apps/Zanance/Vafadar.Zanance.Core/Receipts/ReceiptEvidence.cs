using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Receipts;

/// <summary>Semantic evidence for a purchase total, independent of OCR character confidence.</summary>
public enum ReceiptAmountStatus
{
    /// <summary>No purchase-total evidence was read.</summary>
    NotFound,
    /// <summary>A unique, complete purchase total was read.</summary>
    Found,
    /// <summary>Conflicting, damaged, signed or uncertain evidence needs a user's decision.</summary>
    Review,
}

/// <summary>A complete numeric candidate with the source and explicit unit attached to it.</summary>
/// <param name="Amount">The number in the receipt's unit.</param>
/// <param name="Source">The source row, for review only; never persisted automatically.</param>
/// <param name="CurrencyCode">The explicit ISO currency, when unambiguous.</param>
/// <param name="MajorUnitFactor">Major ISO units per receipt unit (toman = ten rials).</param>
/// <param name="UnitLabel">The printed unit, including ambiguous symbols such as $.</param>
public sealed record ReceiptAmountCandidate(decimal Amount, string Source, string? CurrencyCode = null,
    int MajorUnitFactor = 1, string? UnitLabel = null)
{
    /// <summary>
    /// Resolves an explicit compatible unit, or an unlabelled ISO amount, for the selected account. Never converts
    /// currencies, interprets an ambiguous symbol or silently rounds beyond the currency's precision.
    /// </summary>
    public bool TryMinor(string? accountCurrency, out long minor)
    {
        minor = 0;
        if (!Currencies.TryGet(accountCurrency ?? string.Empty, out var currency)
            || (CurrencyCode is not null && !string.Equals(CurrencyCode, currency.Code, StringComparison.OrdinalIgnoreCase))
            || (UnitLabel is not null && CurrencyCode is null) || Amount < 0 || MajorUnitFactor is not (1 or 10)
            || Amount > long.MaxValue / (decimal)MajorUnitFactor / currency.MinorFactor)
        {
            return false;
        }

        var value = Amount * MajorUnitFactor * currency.MinorFactor;
        if (value > long.MaxValue || value != decimal.Truncate(value))
        {
            return false;
        }

        minor = decimal.ToInt64(value);
        return true;
    }
}
