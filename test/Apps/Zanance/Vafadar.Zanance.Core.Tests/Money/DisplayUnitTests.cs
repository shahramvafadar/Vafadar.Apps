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

        // More than two toman decimals are rejected rather than rounded.
        Assert.False(MoneyText.TryParse("1.255", Rial, English, out _));
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
