using Vafadar.Localization.Formatting;

namespace Vafadar.Localization.Tests.Formatting;

public sealed class NativeDigitsTests
{
    [Theory]
    [InlineData("1,250.50 EUR", "۱٬۲۵۰٫۵۰ EUR")]
    [InlineData("1405/07/06", "۱۴۰۵/۰۷/۰۶")]
    [InlineData("3 days overdue", "۳ days overdue")]
    [InlineData("Total, 12.", "Total, ۱۲.")]
    [InlineData("costs 2.50.", "costs ۲٫۵۰.")]
    [InlineData("Version 0.1.0.1", "Version ۰.۱.۰.۱")]
    [InlineData("1.10.2026", "۱.۱۰.۲۰۲۶")]
    [InlineData("no digits", "no digits")]
    [InlineData("", "")]
    public void Latin_digits_and_the_separators_between_them_become_Persian(string text, string expected) =>
        Assert.Equal(expected, NativeDigits.ToPersian(text));

    [Fact]
    public void Apply_changes_nothing_while_it_is_off()
    {
        NativeDigits.IsEnabled = false;
        Assert.Equal("42", NativeDigits.Apply("42"));
        Assert.Null(NativeDigits.Apply(null));
    }
}
