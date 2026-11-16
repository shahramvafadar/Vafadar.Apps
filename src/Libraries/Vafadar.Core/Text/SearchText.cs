using System.Globalization;
using System.Text;

namespace Vafadar.Core.Text;

/// <summary>
/// Normalizes text for matching (search, categorization rules), so the same word typed on different keyboards is found:
/// Persian and Arabic-Indic digits become ASCII, the Arabic forms of Yeh (ي, ى) and Kaf (ك) become the Persian ones
/// (ی, ک), and the half-space of Persian words (zero-width non-joiner, U+200C) counts as a space, so "میوه فروشی" finds
/// "میوه‌فروشی". Only for comparing – the stored and shown text never changes.
/// </summary>
/// <remarks>
/// Two forms exist. <see cref="Normalize"/> only removes keyboard differences and keeps case, accents and spelling;
/// apps that store patterns or check unique names rely on exactly that, so it never changes. <see cref="Fold"/> is the
/// broader key for finding text: it also ignores case, accents and the spelling variants of Arabic-script words.
/// </remarks>
public static class SearchText
{
    // Arabic-script letters of Fold, as code points because their glyphs are hard to tell apart in source code.
    private const char Alef = (char)0x0627;
    private const char AlefWasla = (char)0x0671;
    private const char TehMarbuta = (char)0x0629;
    private const char Heh = (char)0x0647;
    private const char HehWithYehAbove = (char)0x06C0;
    private const char ArabicKaf = (char)0x0643;
    private const char Keheh = (char)0x06A9;
    private const char AlefMaksura = (char)0x0649;
    private const char ArabicYeh = (char)0x064A;
    private const char FarsiYeh = (char)0x06CC;
    private const char Tatweel = (char)0x0640;

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

    /// <summary>
    /// Returns the key for finding <paramref name="value"/> regardless of case, accents, keyboard and spelling variants;
    /// an empty string for <see langword="null"/> or blank text. Compare keys with <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On top of <see cref="Normalize"/>: digits of every script become ASCII; presentation forms, ligatures and
    /// full-width characters become their base letters; letters are lower-cased; accents, Arabic vowel marks and the
    /// hamza or madda on a letter are removed (آ أ إ → ا, ؤ → و, ئ → ی); ة and ۀ become ه; the tatweel and invisible
    /// format characters (direction marks, joiners, soft hyphen) are dropped; whitespace is collapsed to single spaces.
    /// </para>
    /// <para>
    /// The key is for comparing only – it is not the text and must not be stored in its place or shown. It is too
    /// broad for deciding whether two names are the same name ("Müller" and "Muller" share a key). Letters that are
    /// separate sounds in Kurdish (ێ ۆ ڕ ڵ ە) are kept.
    /// </para>
    /// </remarks>
    public static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Before decomposing: U+06C0 decomposes to the Kurdish letter Ae (U+06D5) plus a hamza, not to Heh, but in
        // Persian it is a spelling of Heh ("خانۀ" = "خانه").
        var text = Decompose(Digits.ToAscii(value).Replace(HehWithYehAbove, Heh)).ToLowerInvariant();

        var builder = new StringBuilder(text.Length);
        var separatorPending = false;
        Span<char> utf16 = stackalloc char[2];
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune) || rune.Value == ZeroWidthNonJoiner)
            {
                // A separator is written only between two kept characters: this trims and collapses in one pass.
                separatorPending = builder.Length > 0;
                continue;
            }

            if (IsIgnored(rune))
            {
                continue;
            }

            if (separatorPending)
            {
                builder.Append(' ');
                separatorPending = false;
            }

            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.DecimalDigitNumber)
            {
                builder.Append((char)('0' + (int)Rune.GetNumericValue(rune)));
            }
            else if (!AppendMapped(builder, rune.Value))
            {
                builder.Append(utf16[..rune.EncodeToUtf16(utf16)]);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Separates accents and marks from their letters and replaces compatibility characters (Unicode NFKD). Text that
    /// is not valid UTF-16 – a lone surrogate from a broken paste – cannot be decomposed and is folded as it is.
    /// </summary>
    private static string Decompose(string text)
    {
        try
        {
            return text.Normalize(NormalizationForm.FormKD);
        }
        catch (ArgumentException)
        {
            return text;
        }
    }

    /// <summary>Returns whether <paramref name="rune"/> carries no meaning for finding text.</summary>
    private static bool IsIgnored(Rune rune) =>
        rune.Value == Tatweel
        || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format or UnicodeCategory.Control;

    /// <summary>
    /// Appends the replacement of a letter that Unicode does not decompose; returns <see langword="false"/> when
    /// <paramref name="codePoint"/> has none.
    /// </summary>
    private static bool AppendMapped(StringBuilder builder, int codePoint)
    {
        switch (codePoint)
        {
            case ArabicYeh or AlefMaksura:
                builder.Append(FarsiYeh);
                return true;
            case ArabicKaf:
                builder.Append(Keheh);
                return true;
            case TehMarbuta:
                builder.Append(Heh);
                return true;
            case AlefWasla:
                builder.Append(Alef);
                return true;
            case 'ß':
                builder.Append("ss");
                return true;
            case 'æ':
                builder.Append("ae");
                return true;
            case 'œ':
                builder.Append("oe");
                return true;
            case 'ø':
                builder.Append('o');
                return true;
            case 'đ':
                builder.Append('d');
                return true;
            case 'ł':
                builder.Append('l');
                return true;
            case 'ı':
                builder.Append('i');
                return true;
            case 'ς':
                builder.Append('σ');
                return true;
            default:
                return false;
        }
    }
}