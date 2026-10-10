using System.Globalization;
using Vafadar.Zanance.App.Features.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks the real occurrence input rules without converting an omitted override into a payment.</summary>
public sealed class OccurrenceAmountValidationTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("0", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("not money", true)]
    [InlineData("9223372036854775808", false)]
    [Trait("AT", "AT-108")]
    public void Unpayable_input_reports_the_requirement_of_the_attempted_action(string text, bool required)
    {
        var result = OccurrenceAmountValidation.Validate(text, "EUR", CultureInfo.InvariantCulture, required);
        Assert.Null(result.Amount);
        Assert.Equal(required ? "Plan_AmountMustBePositive" : "Amount_Invalid", result.ErrorKey);
    }

    [Theory, InlineData(""), InlineData(" ")]
    [Trait("AT", "AT-108")]
    public void Omitted_optional_override_keeps_the_plan_amount_instead_of_posting_zero(string text)
    {
        var result = OccurrenceAmountValidation.Validate(text, "EUR", CultureInfo.InvariantCulture, false);
        Assert.Null(result.Amount); Assert.Null(result.ErrorKey);
    }

    [Theory]
    [InlineData("en-US", "12.34", "EUR", 1234L, true)]
    [InlineData("de-DE", "12,34", "EUR", 1234L, false)]
    [InlineData("de-DE", "۱۲,۳۴", "EUR", 1234L, true)]
    [InlineData("en-US", "١٢.٣٤", "EUR", 1234L, false)]
    [InlineData("en-US", "1200", "JPY", 1200L, true)]
    [Trait("AT", "AT-108")]
    public void Regional_positive_input_retains_currency_minor_units_and_digit_parsing(string culture, string text, string currency, long expected, bool required)
    {
        var result = OccurrenceAmountValidation.Validate(text, currency, CultureInfo.GetCultureInfo(culture), required);
        Assert.Null(result.ErrorKey); Assert.Equal(expected, result.Amount);
    }

    [Fact, Trait("AT", "AT-108")]
    public void Corrected_override_clears_its_own_error_and_does_not_change_an_invalid_payment_result()
    {
        var payment = OccurrenceAmountValidation.Validate("-1", "EUR", CultureInfo.InvariantCulture, true);
        var invalidOverride = OccurrenceAmountValidation.Validate("0", "EUR", CultureInfo.InvariantCulture, false);
        var correctedOverride = OccurrenceAmountValidation.Validate("", "EUR", CultureInfo.InvariantCulture, false);
        Assert.Equal("Plan_AmountMustBePositive", payment.ErrorKey); Assert.Null(payment.Amount);
        Assert.Equal("Amount_Invalid", invalidOverride.ErrorKey);
        Assert.Null(correctedOverride.ErrorKey); Assert.Null(correctedOverride.Amount);
    }
}
