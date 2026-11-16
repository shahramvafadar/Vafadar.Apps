using Vafadar.Core.Text;

namespace Vafadar.Core.Tests.Text;

public sealed class SearchTextTests
{
    [Theory]
    [InlineData("  كيك ۱۲  ", "کیک 12")]
    [InlineData("مصطفى", "مصطفی")]
    [InlineData("میوه‌فروشی", "میوه فروشی")]
    [InlineData("Market", "Market")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Digits_letter_forms_and_half_spaces_are_normalized_for_matching(string? input, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(input));

    // Normalize is what stored patterns and unique-name checks compare with: it must keep what Fold removes.
    [Theory]
    [InlineData("Café", "Café")]
    [InlineData("آب", "آب")]
    [InlineData("سؤال", "سؤال")]
    [InlineData("a  b", "a  b")]
    public void Normalize_keeps_case_accents_spelling_and_inner_spaces(string input, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(input));

    [Theory]
    [InlineData("Market HALL", "market hall")]
    [InlineData("Café Müller", "cafe muller")]
    [InlineData("Straße", "strasse")]
    [InlineData("STRASSE", "strasse")]
    [InlineData("Ærø Łódź", "aero lodz")]
    [InlineData("İstanbul", "istanbul")]
    [InlineData("ISTANBUL", "istanbul")]
    [InlineData("ıstanbul", "istanbul")]
    [InlineData("ΟΔΌΣ", "οδοσ")]
    [InlineData("οδός", "οδοσ")]
    public void Fold_ignores_case_and_accents(string input, string expected) =>
        Assert.Equal(expected, SearchText.Fold(input));

    [Fact]
    public void Fold_treats_composed_and_decomposed_letters_alike()
    {
        var composed = "caf" + (char)0x00E9;
        var decomposed = "cafe" + (char)0x0301;

        Assert.Equal("cafe", SearchText.Fold(composed));
        Assert.Equal("cafe", SearchText.Fold(decomposed));
    }

    [Theory]
    [InlineData("  كيك ۱۲  ", "کیک 12")]
    [InlineData("مصطفى", "مصطفی")]
    [InlineData("آب", "اب")]
    [InlineData("أحمد", "احمد")]
    [InlineData("إبراهيم", "ابراهیم")]
    [InlineData("سؤال", "سوال")]
    [InlineData("رئیس", "رییس")]
    [InlineData("رییس", "رییس")]
    [InlineData("خانۀ", "خانه")]
    [InlineData("فاطمة", "فاطمه")]
    [InlineData("مُحَمَّد", "محمد")]
    [InlineData("سـلام", "سلام")]
    public void Fold_ignores_spelling_variants_of_arabic_script_words(string input, string expected) =>
        Assert.Equal(expected, SearchText.Fold(input));

    [Fact]
    public void Fold_reads_heh_with_a_separate_hamza_as_heh()
    {
        var hehThenHamza = "خانه" + (char)0x0654;

        Assert.Equal("خانه", SearchText.Fold(hehThenHamza));
    }

    [Fact]
    public void Fold_reads_the_alef_wasla_as_alef()
    {
        var alefWasla = ((char)0x0671).ToString();

        Assert.Equal("ا", SearchText.Fold(alefWasla));
    }

    [Theory]
    [InlineData("هەولێر")]
    [InlineData("کۆڕ")]
    [InlineData("گوڵ")]
    public void Fold_keeps_letters_that_are_separate_sounds_in_kurdish(string word) =>
        Assert.Equal(word, SearchText.Fold(word));

    [Theory]
    [InlineData("میوه‌فروشی", "میوه فروشی")]
    [InlineData("میوه‌ فروشی", "میوه فروشی")]
    [InlineData("  a \t b  ", "a b")]
    [InlineData("a\r\nb", "a b")]
    public void Fold_reads_half_spaces_as_spaces_and_collapses_whitespace(string input, string expected) =>
        Assert.Equal(expected, SearchText.Fold(input));

    [Fact]
    public void Fold_collapses_non_breaking_spaces()
    {
        var input = "a" + (char)0x00A0 + (char)0x00A0 + "b";

        Assert.Equal("a b", SearchText.Fold(input));
    }

    [Theory]
    [InlineData(0x200E)] // left-to-right mark
    [InlineData(0x200F)] // right-to-left mark
    [InlineData(0x200D)] // zero-width joiner
    [InlineData(0x200B)] // zero-width space
    [InlineData(0x00AD)] // soft hyphen
    [InlineData(0xFEFF)] // byte order mark
    [InlineData(0x2068)] // first strong isolate
    [InlineData(0x2069)] // pop directional isolate
    [InlineData(0x0007)] // a control character
    public void Fold_drops_invisible_characters(int codePoint)
    {
        var invisible = (char)codePoint;

        Assert.Equal("سلام", SearchText.Fold(invisible + "سل" + invisible + "ام" + invisible));
    }

    [Theory]
    [InlineData(0xFEFB, "لا")] // lam-alef ligature, isolated presentation form
    [InlineData(0xFB01, "fi")] // Latin ligature fi
    [InlineData(0xFF21, "a")] // full-width A
    [InlineData(0xFE8D, "ا")] // alef, isolated presentation form
    public void Fold_reads_presentation_forms_as_their_letters(int codePoint, string expected) =>
        Assert.Equal(expected, SearchText.Fold(((char)codePoint).ToString()));

    [Theory]
    [InlineData("۱۲۳", "123")]
    [InlineData("١٢٣", "123")]
    [InlineData("१२३", "123")]
    [InlineData("１２３", "123")]
    [InlineData("۱۲٫۵", "12.5")]
    public void Fold_converts_the_digits_of_every_script(string input, string expected) =>
        Assert.Equal(expected, SearchText.Fold(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fold_returns_an_empty_key_for_blank_text(string? input) =>
        Assert.Equal(string.Empty, SearchText.Fold(input));

    [Fact]
    public void Fold_returns_an_empty_key_for_text_of_marks_only()
    {
        var marksOnly = new string([(char)0x064E, (char)0x0651, (char)0x200F]);

        Assert.Equal(string.Empty, SearchText.Fold(marksOnly));
    }

    [Fact]
    public void Fold_accepts_text_that_is_not_valid_utf16()
    {
        var loneSurrogate = "Caf" + (char)0xD800 + "É";

        var key = SearchText.Fold(loneSurrogate);

        Assert.StartsWith("caf", key, StringComparison.Ordinal);
        Assert.EndsWith("é", key, StringComparison.Ordinal);
    }

    [Fact]
    public void Fold_keeps_characters_outside_the_basic_plane()
    {
        var emoji = char.ConvertFromUtf32(0x1F600);

        Assert.Equal("a" + emoji + "b", SearchText.Fold("A" + emoji + "B"));
    }

    [Theory]
    [InlineData("Café Müller  Straße")]
    [InlineData("  كيك ۱۲  ")]
    [InlineData("خانۀ مُحَمَّد")]
    [InlineData("میوه‌فروشی")]
    [InlineData("ΟΔΌΣ Ærø")]
    public void Fold_of_a_key_is_the_same_key(string input)
    {
        var key = SearchText.Fold(input);

        Assert.Equal(key, SearchText.Fold(key));
    }

    [Theory]
    [InlineData("Rostam", "ROSTAM")]
    [InlineData("رئیس", "رييس")]
    [InlineData("سهراب", "سُهراب")]
    [InlineData("زال", "زالـ")]
    [InlineData("کی‌کاووس", "كي كاووس")]
    public void Fold_gives_differently_typed_names_the_same_key(string first, string second) =>
        Assert.Equal(SearchText.Fold(first), SearchText.Fold(second));
}
