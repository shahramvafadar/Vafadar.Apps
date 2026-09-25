using System.Text;

namespace Vafadar.Core.Text;

/// <summary>
/// Converts Persian (۰–۹) and Arabic-Indic (٠–٩) digits and separators to their ASCII equivalents, so that numbers
/// typed on any keyboard can be parsed.
/// </summary>
public static class Digits
{
    /// <summary>Returns <paramref name="value"/> with all digits converted to ASCII.</summary>
    /// <remarks>The Arabic decimal separator (٫) becomes '.', the Arabic thousands separator (٬) becomes ','.</remarks>
    public static string ToAscii(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder? builder = null;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var mapped = c switch
            {
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                '٫' => '.',
                '٬' => ',',
                _ => c,
            };

            if (mapped != c)
            {
                builder ??= new StringBuilder(value, 0, i, value.Length);
            }

            builder?.Append(mapped);
        }

        return builder?.ToString() ?? value;
    }

    /// <summary>Returns <paramref name="value"/> with ASCII digits converted to Persian digits (for display).</summary>
    public static string ToPersian(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c;
            }
        });
    }
}
