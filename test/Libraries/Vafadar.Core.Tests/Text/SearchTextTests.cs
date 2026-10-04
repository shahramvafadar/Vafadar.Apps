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
}