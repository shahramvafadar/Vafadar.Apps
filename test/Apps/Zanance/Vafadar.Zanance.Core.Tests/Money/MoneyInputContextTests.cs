using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

/// <summary>Checks explicit monetary interpretation without changing process-wide units.</summary>
public sealed class MoneyInputContextTests
{
    /// <summary>Explicit units use the original exact scale and round-trip stored minor units.</summary>
    [Theory]
    [InlineData("en-US", 1, 12345L)]
    [InlineData("de-DE", 2, 123456L)]
    [InlineData("fa-IR", 6, 987654321L)]
    [InlineData("fr-FR", 3, 123456789L)]
    public void Explicit_input_roundtrips_original_unit(string cultureName, int exponent, long value)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName); var unit = new DisplayUnit("EUR", "Original unit", exponent);
        var text = MoneyText.ForInput(value, "EUR", culture, unit);
        Assert.True(MoneyText.TryParse(text, Currencies.Euro, culture, unit, out var parsed)); Assert.Equal(value, parsed);
    }

    /// <summary>The captured culture cannot be mutated by its former caller.</summary>
    [Fact]
    public void Context_clones_mutable_culture()
    {
        var culture = new CultureInfo("en-US"); var context = MoneyInputContext.Capture("USD", culture);
        culture.NumberFormat.NumberDecimalSeparator = ","; culture.NumberFormat.NumberGroupSeparator = ".";
        Assert.True(context.Culture.IsReadOnly); Assert.True(context.TryParse("1,234", out var parsed)); Assert.Equal(123400, parsed);
        Assert.False(context.UsesFormatOf(culture));
    }

    /// <summary>Neither an unrelated currency unit nor unsupported scaling can enter the explicit parser.</summary>
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("EUR", 0)]
    [InlineData("EUR", 7)]
    public void Explicit_parser_refuses_invalid_unit(string code, int exponent)
    {
        Assert.Throws<ArgumentException>(() => MoneyText.TryParse("12", Currencies.Euro, CultureInfo.InvariantCulture,
            new(code, "Original unit", exponent), out _));
    }

    /// <summary>Scaling overflow is a failed input, never wrapped monetary storage.</summary>
    [Fact]
    public void Explicit_parser_refuses_overflow() => Assert.False(MoneyText.TryParse("92233720368547758", Currencies.Euro,
        CultureInfo.InvariantCulture, new("EUR", "Million euros", 6), out _));
}
