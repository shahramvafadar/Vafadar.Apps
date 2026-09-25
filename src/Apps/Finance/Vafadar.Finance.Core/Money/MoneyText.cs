using System.Globalization;

namespace Vafadar.Finance.Core.Money;

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
    /// <param name="showCurrency">Whether the currency code is appended.</param>
    public static string Format(long minor, string currencyCode, CultureInfo culture, bool showPlus = false, bool showCurrency = true)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        var number = MoneyAmount.Format(Math.Abs(minor), currency, culture);
        var sign = minor < 0 ? Minus.ToString() : showPlus && minor > 0 ? "+" : string.Empty;
        var text = showCurrency ? $"{sign}{number} {currency.Code}" : sign + number;
        return $"{LeftToRightIsolate}{LeftToRightMark}{text}{LeftToRightMark}{PopDirectionalIsolate}";
    }

    /// <summary>Formats a value for an input field: no grouping, no sign, no currency (e.g. <c>1234.5</c> → "1234.50").</summary>
    public static string ForInput(long minor, string currencyCode, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        return MoneyAmount.ToDecimal(Math.Abs(minor), currency).ToString("F" + currency.MinorDigits, culture);
    }
}
