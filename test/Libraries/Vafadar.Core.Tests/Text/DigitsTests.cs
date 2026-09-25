using Vafadar.Core.Text;

namespace Vafadar.Core.Tests.Text;

public sealed class DigitsTests
{
    [Theory]
    [InlineData("۱۲۳٫۵۰", "123.50")]
    [InlineData("١٬٢٣٤", "1,234")]
    [InlineData("12.5", "12.5")]
    [InlineData("مبلغ ۴۵", "مبلغ 45")]
    public void Converts_all_digit_systems_to_ascii(string input, string expected)
    {
        Assert.Equal(expected, Digits.ToAscii(input));
    }

    [Fact]
    public void Ascii_input_is_returned_unchanged()
    {
        const string input = "abc 123";

        Assert.Same(input, Digits.ToAscii(input));
    }

    [Fact]
    public void Converts_ascii_digits_to_persian()
    {
        Assert.Equal("۱۴۰۵/۰۷/۰۳", Digits.ToPersian("1405/07/03"));
    }
}
