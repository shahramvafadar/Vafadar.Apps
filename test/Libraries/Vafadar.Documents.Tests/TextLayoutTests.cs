namespace Vafadar.Documents.Tests;

public sealed class TextLayoutTests
{
    [Fact]
    public void A_skewed_separate_price_block_aligns_using_the_source_angle()
    {
        var angle = Math.Atan2(25, 600) * 180 / Math.PI;
        LayoutWord[] words =
        [
            new(90, 110, 10, "NETTO") { LineId = 0, BlockId = 0, Angle = angle, Right = 80 },
            new(115, 135, 10, "MwSt") { LineId = 1, BlockId = 0, Angle = angle, Right = 80 },
            new(140, 160, 10, "SUMME") { LineId = 2, BlockId = 0, Angle = angle, Right = 80 },
            new(115, 135, 610, "20,92") { LineId = 0, BlockId = 1, Angle = angle, Right = 670 },
            new(140, 160, 610, "3,98") { LineId = 1, BlockId = 1, Angle = angle, Right = 670 },
            new(165, 185, 610, "24,90") { LineId = 2, BlockId = 1, Angle = angle, Right = 670 },
        ];

        var layout = TextLayout.Reconstruct(words.Reverse());

        Assert.False(layout.IsAmbiguous);
        Assert.Equal("NETTO 20,92\nMwSt 3,98\nSUMME 24,90", layout.Text);
    }

    [Fact]
    public void Staggered_legacy_columns_without_metadata_are_uncertain()
    {
        LayoutWord[] words =
        [
            new(90, 110, 10, "NETTO"), new(115, 135, 10, "MwSt"), new(140, 160, 10, "SUMME"),
            new(115, 135, 610, "20,92"), new(140, 160, 610, "3,98"), new(165, 185, 610, "24,90"),
        ];

        Assert.True(TextLayout.Reconstruct(words).IsAmbiguous);
    }

    [Fact]
    public void Full_boxes_join_a_split_decimal_but_not_a_quantity()
    {
        LayoutWord[] words =
        [
            new(100, 120, 10, "SUMME") { Right = 80, LineId = 1 },
            new(100, 120, 500, "2") { Right = 510, LineId = 1 },
            new(100, 120, 610, "24") { Right = 630, LineId = 1 },
            new(116, 120, 633, ",") { Right = 637, LineId = 1 },
            new(100, 120, 640, "90") { Right = 660, LineId = 1 },
        ];

        Assert.Equal("SUMME 2 24,90", TextLayout.Rows(words));
    }

    [Fact]
    public void Large_total_digits_and_small_label_keep_their_source_line()
    {
        LayoutWord[] words =
        [
            new(110, 120, 10, "TOTAL") { LineId = 0 }, new(90, 130, 500, "24.90") { LineId = 0 },
        ];

        Assert.Equal("TOTAL 24.90", TextLayout.Rows(words));
    }

    [Theory]
    [InlineData(",", "24", "90", "24,90")]
    [InlineData("٫", "۲۴", "۹۰", "۲۴٫۹۰")]
    [InlineData("،", "24", "90", "24،90")]
    public void Persian_labels_reverse_but_decimal_fragments_keep_numeric_order(string separator, string whole, string fraction, string expected)
    {
        LayoutWord[] words =
        [
            new(100, 120, 700, "جمع") { Right = 760, LineId = 0 },
            new(100, 120, 600, "کل") { Right = 640, LineId = 0 },
            new(100, 120, 10, whole) { Right = 30, LineId = 0 },
            new(116, 120, 33, separator) { Right = 37, LineId = 0 },
            new(100, 120, 40, fraction) { Right = 60, LineId = 0 },
        ];

        Assert.Equal("جمع کل " + expected, TextLayout.Rows(words));
    }

    [Fact]
    public void Pages_with_identical_coordinates_and_line_ids_never_merge() =>
        Assert.Equal("TOTAL\f24,90", TextLayout.Rows([
            new(0, 20, 10, "TOTAL") { Page = 0, LineId = 0 },
            new(0, 20, 10, "24,90") { Page = 1, LineId = 0 },
        ]));

    [Fact]
    public void A_small_decimal_separator_stays_between_its_digits()
    {
        LayoutWord[] words =
        [
            new(100, 120, 10, "SUMME"), new(100, 120, 610, "24"),
            new(116, 120, 633, ","), new(100, 120, 640, "90"),
        ];

        Assert.Equal("SUMME 24 , 90", TextLayout.Rows(words));
    }

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
