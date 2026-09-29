namespace Vafadar.Documents.Tests;

public sealed class TextLayoutTests
{
    [Fact]
    public void Words_on_one_line_form_a_row_in_reading_order()
    {
        LayoutWord[] words = [new(100, 120, 400, "4,10"), new(0, 20, 10, "Markt"), new(101, 119, 10, "SUMME")];

        Assert.Equal("Markt\nSUMME 4,10", TextLayout.Rows(words));
    }

    [Fact]
    public void A_persian_row_is_read_from_right_to_left() =>
        Assert.Equal("فروشگاه نمونه", TextLayout.Rows([new(0, 10, 50, "نمونه"), new(0, 10, 120, "فروشگاه")]));

    [Fact]
    public void No_words_give_no_text() => Assert.Null(TextLayout.Rows([new(0, 10, 0, "  ")]));

    [Theory]
    [InlineData(0, 4, new int[0])]
    [InlineData(3, 4, new[] { 0, 1, 2 })]
    [InlineData(10, 4, new[] { 0, 1, 2, 9 })]
    [InlineData(10, 1, new[] { 9 })]
    public void Long_documents_are_read_from_the_start_and_the_last_page(int pageCount, int maxPages, int[] expected) =>
        Assert.Equal(expected, TextLayout.PagesToRead(pageCount, maxPages));
}
