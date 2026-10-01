using System.Globalization;

namespace Vafadar.Zanance.Core.Money;

/// <summary>
/// Formats amounts for display (VIS-02, LOC-03).
/// </summary>
/// <remarks>
/// The result is wrapped in Unicode "left-to-right isolate" marks, so a signed amount with its currency code is never
/// reordered inside right-to-left text. Left-to-right marks inside the isolate keep the order on text renderers that
/// ignore isolates (e.g. Windows). The minus sign is U+2212, the plus sign is shown only when requested.
/// </remarks>
public static class MoneyText
{
    private const char LeftToRightIsolate = '⁦';
    private const char PopDirectionalIsolate = '⁩';
    private const char LeftToRightMark = '\u200E';
    private const char Minus = '−';

    /// <summary>Formats <paramref name="minor"/> units of <paramref name="currencyCode"/>, e.g. <c>−12.50 EUR</c>.</summary>
    /// <param name="minor">Amount in minor units.</param>
    /// <param name="currencyCode">ISO currency code.</param>
    /// <param name="culture">Formatting culture (digits, separators).</param>
    /// <param name="showPlus">Whether positive amounts get a leading "+" (income).</param>
    /// <param name="showCurrency">Whether the currency code (or the user's display unit, FX-07) is appended.</param>
    /// <param name="approximate">Whether the amount is an estimate; "≈" is placed inside the isolated amount so it stays in front in any text direction.</param>
    public static string Format(long minor, string currencyCode, CultureInfo culture, bool showPlus = false, bool showCurrency = true, bool approximate = false)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var currency = CurrencyOf(currencyCode);
        string number, label;
        if (DisplayUnits.TryGet(currency.Code, out var unit))
        {
            var value = ToDecimal(Math.Abs(minor), currency);
            number = value.ToString("N" + DecimalsOf(value, currency.MinorDigits + unit.Exponent), culture);
            label = unit.Name;
        }
        else
        {
            number = MoneyAmount.Format(Math.Abs(minor), currency, culture);
            label = currency.Code;
        }

        var sign = minor < 0 ? Minus.ToString() : showPlus && minor > 0 ? "+" : string.Empty;
        var prefix = approximate ? "≈ " : string.Empty;
        var text = showCurrency ? $"{prefix}{sign}{number} {label}" : prefix + sign + number;
        return $"{LeftToRightIsolate}{LeftToRightMark}{text}{LeftToRightMark}{PopDirectionalIsolate}";
    }

    /// <summary>
    /// Formats a value for an input field: no grouping, no sign, no currency (e.g. <c>1234.5</c> → "1234.50"). With a
    /// display unit the value is in that unit, unless <paramref name="useUnit"/> is <see langword="false"/>.
    /// </summary>
    public static string ForInput(long minor, string currencyCode, CultureInfo culture, bool useUnit = true)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var currency = CurrencyOf(currencyCode);
        if (useUnit && DisplayUnits.TryGet(currency.Code, out var unit))
        {
            var value = ToDecimal(Math.Abs(minor), currency);
            return value.ToString("F" + DecimalsOf(value, currency.MinorDigits + unit.Exponent), culture);
        }

        return MoneyAmount.ToDecimal(Math.Abs(minor), currency).ToString("F" + currency.MinorDigits, culture);
    }

    /// <summary>
    /// Parses a user-entered amount like <see cref="MoneyAmount.TryParse"/>; with a display unit the text is in that
    /// unit (FX-07). Up to two decimals are read first, so "1,250" stays a grouped thousand; only when that fails are
    /// as many decimals accepted as one minor unit needs (what <see cref="ForInput"/> writes). The result is always in
    /// minor units of the currency.
    /// </summary>
    public static bool TryParse(string? text, Currency currency, CultureInfo culture, out long minor)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (!DisplayUnits.TryGet(currency.Code, out var unit))
        {
            return MoneyAmount.TryParse(text, currency, culture, out minor);
        }

        // One minor unit of the currency is 10^-(digits + exponent) display units; a display amount with fewer
        // decimals is an exact multiple of it.
        var digits = currency.MinorDigits + unit.Exponent;
        var accepted = Math.Min(2, digits);
        minor = 0;
        if (!MoneyAmount.TryParse(text, new Currency(currency.Code, accepted), culture, out var parsed))
        {
            accepted = digits;
            if (!MoneyAmount.TryParse(text, new Currency(currency.Code, accepted), culture, out parsed))
            {
                return false;
            }
        }

        try
        {
            minor = checked(parsed * Pow10(digits - accepted));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    /// <summary>Parses a user-entered amount of <paramref name="currencyCode"/>; see <see cref="TryParse(string?, Currency, CultureInfo, out long)"/>.</summary>
    public static bool TryParse(string? text, string currencyCode, CultureInfo culture, out long minor) =>
        TryParse(text, CurrencyOf(currencyCode), culture, out minor);

    /// <summary>Returns the amount as shown to the user: in the display unit if there is one (charts, FX-07).</summary>
    public static decimal ToDecimal(long minor, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return DisplayUnits.TryGet(currency.Code, out var unit)
            ? minor / (decimal)Pow10(currency.MinorDigits + unit.Exponent)
            : MoneyAmount.ToDecimal(minor, currency);
    }

    /// <summary>Returns the label of amounts of a currency: the user's display unit name or the ISO code.</summary>
    public static string UnitName(string? currencyCode) =>
        DisplayUnits.TryGet(currencyCode, out var unit) ? unit.Name : currencyCode ?? string.Empty;

    private static Currency CurrencyOf(string currencyCode) => Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);

    // Only as many decimals as the value needs: 1,250 toman, but 1,250.5 toman for an odd number of rials.
    private static int DecimalsOf(decimal value, int max)
    {
        var decimals = 0;
        while (decimals < max && decimal.Truncate(value * Pow10(decimals)) != value * Pow10(decimals))
        {
            decimals++;
        }

        return decimals;
    }

    private static long Pow10(int exponent)
    {
        long result = 1;
        for (var i = 0; i < exponent; i++)
        {
            result *= 10;
        }

        return result;
    }
}
