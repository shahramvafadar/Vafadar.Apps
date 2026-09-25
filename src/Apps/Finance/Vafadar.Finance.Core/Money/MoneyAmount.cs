using System.Globalization;
using Vafadar.Core.Text;

namespace Vafadar.Finance.Core.Money;

/// <summary>
/// Conversion between minor units (stored) and decimal amounts (entered and displayed) for a currency (FIN-05).
/// </summary>
public static class MoneyAmount
{
    /// <summary>Converts minor units to a decimal amount, e.g. 9237 EUR → 92.37.</summary>
    public static decimal ToDecimal(long minor, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return minor / (decimal)currency.MinorFactor;
    }

    /// <summary>Converts a decimal amount to minor units, rounding half away from zero.</summary>
    public static long ToMinor(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return decimal.ToInt64(decimal.Round(amount * currency.MinorFactor, 0, MidpointRounding.AwayFromZero));
    }

    /// <summary>Formats minor units with the currency's digits in the given culture, without currency symbol.</summary>
    public static string Format(long minor, Currency currency, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(culture);
        return ToDecimal(minor, currency).ToString("N" + currency.MinorDigits, culture);
    }

    /// <summary>
    /// Parses a positive user-entered amount (LOC-04, FIN-05).
    /// </summary>
    /// <remarks>
    /// Rules: Persian and Arabic-Indic digits are accepted; '.', ',' and '٫' are separators.
    /// With two kinds of separators the last one is the decimal separator. A single kind used more than once is a
    /// group separator. A single occurrence is the decimal separator, except when it is the culture's group separator
    /// followed by exactly three digits. Groups must have three digits. More decimals than the currency allows, signs
    /// and anything else are rejected – nothing is guessed or silently rounded.
    /// </remarks>
    public static bool TryParse(string? text, Currency currency, CultureInfo culture, out long minor)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(culture);
        minor = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = Digits.ToAscii(text).Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
        if (value.Length == 0 || value.Any(c => c is not (>= '0' and <= '9') and not '.' and not ','))
        {
            return false;
        }

        var dots = value.Count(c => c == '.');
        var commas = value.Count(c => c == ',');
        char? decimalSeparator;

        if (dots > 0 && commas > 0)
        {
            decimalSeparator = value.LastIndexOf('.') > value.LastIndexOf(',') ? '.' : ',';
            if (value.Count(c => c == decimalSeparator) > 1)
            {
                return false;
            }
        }
        else if (dots + commas == 0)
        {
            decimalSeparator = null;
        }
        else
        {
            var separator = dots > 0 ? '.' : ',';
            var count = dots + commas;
            var digitsAfter = value.Length - value.LastIndexOf(separator) - 1;
            // Arabic/Persian group separator '٬' counts as ','.
            var cultureGroup = Digits.ToAscii(culture.NumberFormat.NumberGroupSeparator) is { Length: 1 } g ? g[0] : '\0';
            var isGroup = count > 1 || (separator == cultureGroup && digitsAfter == 3 && currency.MinorDigits < 3);
            decimalSeparator = isGroup ? null : separator;
        }

        var groupSeparator = decimalSeparator switch { '.' => ',', ',' => '.', _ => dots > 0 ? '.' : ',' };
        var decimalIndex = decimalSeparator is { } d ? value.IndexOf(d) : -1;
        var integerPart = decimalIndex < 0 ? value : value[..decimalIndex];
        var fractionPart = decimalIndex < 0 ? string.Empty : value[(decimalIndex + 1)..];

        if (integerPart.Contains(groupSeparator))
        {
            var groups = integerPart.Split(groupSeparator);
            if (groups[0].Length is 0 or > 3 || groups.Skip(1).Any(group => group.Length != 3))
            {
                return false;
            }

            integerPart = string.Concat(groups);
        }

        if (integerPart.Length == 0 && fractionPart.Length == 0)
        {
            return false;
        }

        if (fractionPart.Length > currency.MinorDigits || fractionPart.Contains(groupSeparator))
        {
            return false;
        }

        var normalized = (integerPart.Length == 0 ? "0" : integerPart) + (fractionPart.Length > 0 ? "." + fractionPart : string.Empty);
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        minor = ToMinor(amount, currency);
        return true;
    }
}
