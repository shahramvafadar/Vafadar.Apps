using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

// The display units are process-wide; only this class uses the rial, so other tests never see the toman.
public sealed class DisplayUnitTests : IDisposable
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly Currency Rial = Currencies.Get("IRR");

    public DisplayUnitTests() => DisplayUnits.Set([new DisplayUnit("IRR", "Toman", 1)]);

    public void Dispose() => DisplayUnits.Set([]);

    // Without isolates and marks, and with a plain space instead of the non-breaking one.
    private static string Plain(string text) => new string([.. text.Where(c => c is not ('‎' or '⁦' or '⁩'))]).Replace(' ', ' ');

    [Fact]
    public void Rial_amounts_are_shown_and_entered_in_toman_while_the_stored_amount_stays_in_rial()
    {
        // 12,500,000 rial = 1,250,000 toman; stored with the rial's two minor digits.
        const long minor = 12_500_000_00;
        Assert.Equal("1,250,000 Toman", Plain(MoneyText.Format(minor, "IRR", English)));
        Assert.Equal("−1,250,000 Toman", Plain(MoneyText.Format(-minor, "IRR", English)));
        Assert.Equal("1250000", MoneyText.ForInput(minor, "IRR", English));
        Assert.Equal("12500000.00", MoneyText.ForInput(minor, "IRR", English, useUnit: false));

        Assert.True(MoneyText.TryParse("1,250,000", Rial, English, out var parsed));
        Assert.Equal(minor, parsed);
        Assert.Equal(1_250_000m, MoneyText.ToDecimal(minor, Rial));
        Assert.Equal("Toman", MoneyText.UnitName("IRR"));
    }

    [Fact]
    public void Odd_rials_show_the_toman_fraction_and_persian_input_is_accepted()
    {
        // 12,505 rial = 1,250.5 toman.
        Assert.Equal("1,250.5 Toman", Plain(MoneyText.Format(12_505_00, "IRR", English)));
        Assert.True(MoneyText.TryParse("۱۲۵۰٫۵", Rial, CultureInfo.GetCultureInfo("fa-IR"), out var parsed));
        Assert.Equal(12_505_00, parsed);

        // A third toman decimal is a whole number of rial minor units; a fourth is rejected rather than rounded.
        Assert.True(MoneyText.TryParse("1.255", Rial, English, out var fine));
        Assert.Equal(12_55, fine);
        Assert.False(MoneyText.TryParse("1.2555", Rial, English, out _));
        Assert.True(MoneyText.TryParse("1,250", Rial, English, out var grouped));
        Assert.Equal(12_500_00, grouped);
    }

    [Fact]
    public void A_value_written_for_input_is_read_back_unchanged_with_a_large_factor()
    {
        // One unit of a million rial: 12,345,000 rial is shown as 12.345 and must save unchanged.
        DisplayUnits.Set([new DisplayUnit("IRR", "Million", 6)]);
        foreach (var culture in new[] { English, CultureInfo.GetCultureInfo("de-DE") })
        {
            var text = MoneyText.ForInput(12_345_000_00, "IRR", culture);
            Assert.True(MoneyText.TryParse(text, Rial, culture, out var parsed), text);
            Assert.Equal(12_345_000_00, parsed);
        }
    }

    /// <summary>Display-unit conversion retains the exact boundary fraction and ISO input alternative.</summary>
    [Fact, Trait("AT", "AT-93")]
    public void Minimum_signed_value_keeps_exact_toman_fraction_and_currency_input_magnitude()
    {
        Assert.Equal("−9,223,372,036,854,775.808 Toman", Plain(MoneyText.Format(long.MinValue, "IRR", English)));
        Assert.Equal("9223372036854775.808", MoneyText.ForInput(long.MinValue, "IRR", English));
        Assert.Equal("92233720368547758.08", MoneyText.ForInput(long.MinValue, "IRR", English, useUnit: false));
    }

    /// <summary>An unsigned boundary magnitude cannot silently exceed positive stored minor units.</summary>
    [Fact, Trait("AT", "AT-93")]
    public void Minimum_signed_toman_input_magnitude_is_not_silently_rounded_or_accepted_as_positive()
    {
        var input = MoneyText.ForInput(long.MinValue, "IRR", CultureInfo.GetCultureInfo("de"));
        Assert.Equal("9223372036854775,808", input);
        Assert.False(MoneyText.TryParse(input, "IRR", CultureInfo.GetCultureInfo("de"), out var minor));
        Assert.Equal(0, minor);
    }

    [Fact]
    public void Other_currencies_and_undefined_units_are_unchanged()
    {
        Assert.Equal("12.50 EUR", Plain(MoneyText.Format(1_250, "EUR", English)));
        Assert.True(MoneyText.TryParse("12.50", Currencies.Euro, English, out var euro));
        Assert.Equal(1_250, euro);
        Assert.Equal("EUR", MoneyText.UnitName("EUR"));

        DisplayUnits.Set([]);
        Assert.Equal("125,000.00 IRR", Plain(MoneyText.Format(12_500_000, "IRR", English)));
    }

    [Fact]
    public void Only_explicit_valid_units_are_kept()
    {
        DisplayUnits.Set([new DisplayUnit("IRR", " ", 1), new DisplayUnit("XXX", "Unit", 1), new DisplayUnit("IRR", "Toman", 0)]);
        Assert.Empty(DisplayUnits.All);

        DisplayUnits.Set([new DisplayUnit("irr", " Toman ", 1)]);
        var unit = Assert.Single(DisplayUnits.All);
        Assert.Equal(("IRR", "Toman", 10L), (unit.CurrencyCode, unit.Name, unit.Factor));
    }
}
