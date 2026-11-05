using System.Globalization;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Reads invariant wire decimals without accepting a rounded replacement for the submitted magnitude.</summary>
public static class SyncDecimal
{
    /// <summary>Preserves the existing floating-point text syntax while requiring an exactly representable decimal.</summary>
    public static bool TryParseExact(string? text, out decimal value)
    {
        value = default;
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return false;
        // Parse first so signs, whitespace and exponent grammar remain owned by the existing contract.
        // Comparing normalized digits avoids both decimal arithmetic rounding and giant integer powers.
        if (!TryMagnitude(text!, out var digits, out var power, out var negative) ||
            !TryMagnitude(parsed.ToString(CultureInfo.InvariantCulture), out var storedDigits, out var storedPower, out var storedNegative) ||
            digits != storedDigits || power != storedPower || negative != storedNegative) return false;
        value = parsed;
        return true;
    }

    private static bool TryMagnitude(string text, out string digits, out long power, out bool negative)
    {
        text = text.Trim();
        negative = text.StartsWith('-');
        if (text.StartsWith('-') || text.StartsWith('+')) text = text[1..];
        var exponentAt = text.IndexOfAny(['e', 'E']);
        var mantissa = exponentAt < 0 ? text : text[..exponentAt];
        var point = mantissa.IndexOf('.');
        var fraction = point < 0 ? 0 : mantissa.Length - point - 1;
        digits = mantissa.Replace(".", "", StringComparison.Ordinal).TrimStart('0');
        power = 0;
        if (digits.Length == 0) { negative = false; return true; }
        long exponent = 0;
        if (exponentAt >= 0 && !long.TryParse(text.AsSpan(exponentAt + 1), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out exponent)) return false;
        var significant = digits.TrimEnd('0');
        try { power = checked(exponent - fraction + digits.Length - significant.Length); }
        catch (OverflowException) { return false; }
        digits = significant;
        return true;
    }
}
