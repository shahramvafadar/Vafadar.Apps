using System.Globalization;
using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.Tests.Money;

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
    public void Input_text_has_no_grouping_sign_or_currency() =>
        Assert.Equal("1234.50", MoneyText.ForInput(-123_450, "EUR", English));

    private static string Strip(string text) => text.Trim('⁦', '⁩', '‎');
}
