using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Contains independent period and bill problems without modifying the settlement draft.</summary>
/// <param name="PeriodErrorKey">The reversed-period explanation, or null for an inclusive valid range.</param>
/// <param name="AmountErrorKey">The nonnegative bill requirement, or null for a valid regional amount.</param>
/// <param name="Actual">The original parsed minor-unit bill, including zero, or null for invalid input.</param>
public sealed record SettlementValidationResult(string? PeriodErrorKey, string? AmountErrorKey, long? Actual)
{
    /// <summary>Gets whether the entered period and nonnegative actual bill can be compared with advances.</summary>
    public bool HasErrors => PeriodErrorKey is not null || AmountErrorKey is not null;
}

/// <summary>Validates the existing inclusive settlement period and regional money input before ledger calculation.</summary>
public static class SettlementDraftValidation
{
    /// <summary>Collects both field problems; zero is a valid bill that can refund every remaining advance.</summary>
    public static SettlementValidationResult Validate(DateOnly from, DateOnly to, string text, string currency, CultureInfo culture,
        MoneyInputContext? original = null)
    {
        ArgumentNullException.ThrowIfNull(culture);
        long actual = 0;
        var valid = (original is null ? MoneyText.TryParse(text, Currencies.Get(currency), culture, out actual)
            : original.MatchesCurrent(currency) && original.TryParse(text, out actual)) && actual >= 0;
        return new SettlementValidationResult(from > to ? "Settlement_PeriodInvalid" : null,
            valid ? null : "Settlement_AmountInvalid", valid ? actual : null);
    }
}
