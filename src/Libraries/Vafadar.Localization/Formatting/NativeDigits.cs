using System.Text;

namespace Vafadar.Localization.Formatting;

/// <summary>
/// Shows numbers with Persian digits (۰–۹) instead of Latin ones. Formatting and storage always use Latin digits;
/// this only changes the text a user reads, and input keeps accepting both. The inverse for parsing is
/// <see cref="Vafadar.Core.Text.Digits.ToAscii(string)"/>; unlike <see cref="Vafadar.Core.Text.Digits.ToPersian(string)"/> this
/// also converts the separators between digits.
/// </summary>
public static class NativeDigits
{
    private const char PersianZero = '۰';
    private const char ArabicThousandsSeparator = '٬';
    private const char ArabicDecimalSeparator = '٫';

    /// <summary>Gets or sets a value indicating whether displayed text uses Persian digits.</summary>
    /// <remarks>Set by the app from the user's choice and the current language; read by the UI when it shows text.</remarks>
    public static bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether explicit regional separators must remain independent of the digit shapes.</summary>
    public static bool PreserveSeparators { get; set; }

    /// <summary>Returns <paramref name="text"/> with Persian digits when <see cref="IsEnabled"/> is set.</summary>
    public static string? Apply(string? text) => !IsEnabled || text is null ? text
        : PreserveSeparators ? Vafadar.Core.Text.Digits.ToPersian(text) : ToPersian(text);

    /// <summary>
    /// Replaces the Latin digits of <paramref name="text"/> with Persian digits, and a comma or dot between two digits
    /// with the Persian thousands or decimal separator (۱٬۲۵۰٫۵۰). Other characters, e.g. the slashes of a date, stay.
    /// </summary>
    public static string? ToPersian(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Any(char.IsAsciiDigit))
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsAsciiDigit(c))
            {
                result.Append((char)(PersianZero + (c - '0')));
            }
            else if (c is ',' or '.' && i > 0 && i < text.Length - 1 && char.IsAsciiDigit(text[i - 1]) && char.IsAsciiDigit(text[i + 1])
                     && (c == ',' || !HasSeveralDots(text, i)))
            {
                result.Append(c == ',' ? ArabicThousandsSeparator : ArabicDecimalSeparator);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    // A number has one decimal separator; dots in a run like 0.1.0.1 (a version) or 1.2.2026 (a date) are not decimals
    // and stay dots.
    private static bool HasSeveralDots(string text, int index)
    {
        var start = index;
        while (start > 0 && (char.IsAsciiDigit(text[start - 1]) || text[start - 1] is '.' or ','))
        {
            start--;
        }

        var end = index;
        while (end < text.Length - 1 && (char.IsAsciiDigit(text[end + 1]) || text[end + 1] is '.' or ','))
        {
            end++;
        }

        // Only dots between two digits count, so a full stop after a number ("costs 2.50.") stays a full stop.
        var dots = 0;
        for (var i = start + 1; i < end; i++)
        {
            if (text[i] == '.' && char.IsAsciiDigit(text[i - 1]) && char.IsAsciiDigit(text[i + 1]))
            {
                dots++;
            }
        }

        return dots > 1;
    }
}
