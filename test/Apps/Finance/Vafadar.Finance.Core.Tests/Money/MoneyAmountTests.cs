using System.Globalization;
using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.Tests.Money;

[Trait("AT", "AT-47")]
public sealed class MoneyAmountTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de");

    [Theory]
    [InlineData("12.50", "en", "EUR", 1250)]
    [InlineData("12,50", "de", "EUR", 1250)]
    [InlineData("1,234.56", "en", "EUR", 123456)]
    [InlineData("1.234,56", "de", "EUR", 123456)]
    [InlineData("1,234", "en", "EUR", 123400)]
    [InlineData("1.234", "de", "EUR", 123400)]
    [InlineData("۱۲٫۵", "fa", "EUR", 1250)]
    [InlineData("١٢٣", "fa", "EUR", 12300)]
    [InlineData("1500", "en", "JPY", 1500)]
    [InlineData("1.250", "en", "KWD", 1250)]
    [InlineData("0.5", "en", "EUR", 50)]
    public void Parses_amounts_in_every_digit_system_and_culture(string text, string culture, string currency, long expected)
    {
        Assert.True(MoneyAmount.TryParse(text, Currencies.Get(currency), CultureInfo.GetCultureInfo(culture), out var minor));
        Assert.Equal(expected, minor);
    }

    [Theory]
    [InlineData("12.345", "en", "EUR")]   // more decimals than the currency has
    [InlineData("1,234", "de", "EUR")]    // German decimal comma with 3 decimals: ambiguous, not guessed
    [InlineData("12.5", "en", "JPY")]     // JPY has no decimals
    [InlineData("-5", "en", "EUR")]       // sign is chosen by the entry kind (FIN-01)
    [InlineData("1,23,4", "en", "EUR")]   // invalid grouping
    [InlineData("abc", "en", "EUR")]
    [InlineData("", "en", "EUR")]
    public void Rejects_ambiguous_or_invalid_input(string text, string culture, string currency)
    {
        Assert.False(MoneyAmount.TryParse(text, Currencies.Get(currency), CultureInfo.GetCultureInfo(culture), out _));
    }

    [Fact]
    public void Formats_with_the_currency_digits()
    {
        Assert.Equal("1,234.56", MoneyAmount.Format(123456, Currencies.Get("EUR"), English));
        Assert.Equal("1.500", MoneyAmount.Format(1500, Currencies.Get("JPY"), German));
        Assert.Equal("1.250", MoneyAmount.Format(1250, Currencies.Get("KWD"), English));
    }

    [Fact]
    public void Rounds_half_away_from_zero()
    {
        Assert.Equal(1235, MoneyAmount.ToMinor(12.345m, Currencies.Euro));
        Assert.Equal(0.5m, MoneyAmount.ToDecimal(50, Currencies.Euro));
    }
}
