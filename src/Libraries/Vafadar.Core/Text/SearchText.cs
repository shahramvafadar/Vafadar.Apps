using System.Text;

namespace Vafadar.Core.Text;

/// <summary>
/// Normalizes text for matching (search, categorization rules), so the same word typed on different keyboards is found:
/// Persian and Arabic-Indic digits become ASCII, the Arabic forms of Yeh (ي, ى) and Kaf (ك) become the Persian ones
/// (ی, ک), and the half-space of Persian words (zero-width non-joiner, U+200C) counts as a space, so "میوه فروشی" finds
/// "میوه‌فروشی". Only for comparing – the stored and shown text never changes.
/// </summary>
public static class SearchText
{
    private const char ZeroWidthNonJoiner = '\u200C';

    /// <summary>Returns the trimmed, normalized form of <paramref name="value"/>; an empty string for <see langword="null"/>.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var ascii = Digits.ToAscii(value.Trim());
        var builder = new StringBuilder(ascii.Length);
        foreach (var c in ascii)
        {
            switch (c)
            {
                case 'ي' or 'ى':
                    builder.Append('ی');
                    break;
                case 'ك':
                    builder.Append('ک');
                    break;
                case ZeroWidthNonJoiner:
                    builder.Append(' ');
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }
}