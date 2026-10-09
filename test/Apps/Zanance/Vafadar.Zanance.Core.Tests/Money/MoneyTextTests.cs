using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

public sealed class MoneyTextTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    [Fact]
    public void Amounts_are_isolated_left_to_right_with_a_real_minus_sign()
    {
        var text = MoneyText.Format(-1_250, "EUR", English);

        Assert.StartsWith("⁦‎", text, StringComparison.Ordinal);
        Assert.EndsWith("‎⁩", text, StringComparison.Ordinal);
        Assert.Equal("−12.50 EUR", Strip(text));
    }

    [Theory]
    [InlineData(1_250, true, "+12.50 EUR")]
    [InlineData(1_250, false, "12.50 EUR")]
    [InlineData(0, true, "0.00 EUR")]
    public void Plus_sign_only_when_requested_and_never_for_zero(long minor, bool showPlus, string expected) =>
        Assert.Equal(expected, Strip(MoneyText.Format(minor, "EUR", English, showPlus)));

    [Fact]
    public void Currency_minor_digits_are_respected()
    {
        Assert.Equal("1,250 JPY", Strip(MoneyText.Format(1_250, "JPY", English)));
        Assert.Equal("1.250 KWD", Strip(MoneyText.Format(1_250, "KWD", English)));
    }

    [Fact]
    public void Approximate_sign_stays_inside_the_isolated_amount() =>
        Assert.Equal("\u2066\u200E≈ \u22123.00\u00A0EUR\u200E\u2069", MoneyText.Format(-300, "EUR", English, approximate: true));

    [Fact]
    public void Input_text_has_no_grouping_sign_or_currency() =>
        Assert.Equal("1234.50", MoneyText.ForInput(-123_450, "EUR", English));

    /// <summary>The full signed storage range retains currency digits and the real minus sign.</summary>
    [Theory, Trait("AT", "AT-93")]
    [InlineData("EUR", "en", "−92,233,720,368,547,758.08 EUR")]
    [InlineData("EUR", "de", "−92.233.720.368.547.758,08 EUR")]
    [InlineData("JPY", "en", "−9,223,372,036,854,775,808 JPY")]
    [InlineData("KWD", "en", "−9,223,372,036,854,775.808 KWD")]
    public void Minimum_signed_minor_value_keeps_its_full_magnitude_sign_and_currency(string currency, string culture, string expected) =>
        Assert.Equal(expected, Strip(MoneyText.Format(long.MinValue, currency, CultureInfo.GetCultureInfo(culture), showPlus: true)));

    /// <summary>Absolute input formatting stays exact while the existing positive parser rejects out-of-range values.</summary>
    [Theory, Trait("AT", "AT-93")]
    [InlineData("EUR", "en", "92233720368547758.08")]
    [InlineData("EUR", "de", "92233720368547758,08")]
    [InlineData("JPY", "en", "9223372036854775808")]
    [InlineData("KWD", "en", "9223372036854775.808")]
    public void Minimum_signed_input_magnitude_formats_exactly_without_expanding_the_positive_parser(string currency, string culture, string expected)
    {
        var formatting = CultureInfo.GetCultureInfo(culture);
        var input = MoneyText.ForInput(long.MinValue, currency, formatting);
        Assert.Equal(expected, input);
        Assert.False(MoneyText.TryParse(input, currency, formatting, out var parsed));
        Assert.Equal(0, parsed);
    }

    /// <summary>Boundary magnitudes preserve the approximation marker and bidi packet.</summary>
    [Fact, Trait("AT", "AT-93")]
    public void Minimum_signed_value_preserves_approximation_and_direction_marks_without_currency() =>
        Assert.Equal("\u2066\u200E≈ −92,233,720,368,547,758.08\u200E\u2069",
            MoneyText.Format(long.MinValue, "EUR", English, showCurrency: false, approximate: true));

    private static string Strip(string text) => text.Trim('⁦', '⁩', '‎');
}
