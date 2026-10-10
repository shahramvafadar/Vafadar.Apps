using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Contains the parsed positive minor units or the explanation for the actual affected input.</summary>
/// <param name="Amount">The entered positive amount, or null for an omitted optional override or invalid input.</param>
/// <param name="ErrorKey">The translated field requirement, or null when the input can continue.</param>
public sealed record OccurrenceAmountResult(long? Amount, string? ErrorKey);

/// <summary>Preserves the existing distinction between a required payment and an optional occurrence override.</summary>
public static class OccurrenceAmountValidation
{
    /// <summary>Uses the existing regional parser; an empty override keeps the plan amount and never becomes zero.</summary>
    public static OccurrenceAmountResult Validate(string text, string currency, CultureInfo culture, bool required)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!required && string.IsNullOrWhiteSpace(text)) { return new(null, null); }
        return MoneyText.TryParse(text, Currencies.Get(currency), culture, out var amount) && amount > 0
            ? new(amount, null) : new(null, required ? "Plan_AmountMustBePositive" : "Amount_Invalid");
    }
}

/// <summary>Identifies the input belonging to the actual attempted occurrence action.</summary>
public enum OccurrenceInput
{
    /// <summary>The actual amount for completion or partial payment.</summary>
    Payment,
    /// <summary>The optional replacement amount for this occurrence only.</summary>
    Override,
}
