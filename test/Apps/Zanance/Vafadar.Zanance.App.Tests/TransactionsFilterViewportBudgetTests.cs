using Vafadar.Zanance.App.Features.Transactions;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-50: the observed native safe-area/selection/Undo packet retains original transaction-result space.</summary>
[Trait("AT", "AT-50")]
public sealed class TransactionsFilterViewportBudgetTests
{
    [Theory]
    [InlineData(0, 164)]
    [InlineData(24, 140)]
    [InlineData(40, 124)]
    [InlineData(80, 84)]
    public void Actual_header_inset_does_not_consume_reserved_result_space(double topInset, double expectedFilters)
    {
        // Real failing Android packet: root720, header66, bulk221+16margin, Undo97+12margin.
        var filters = TransactionsFilterViewportBudget.Calculate(720, topInset + 66, 346, 0);
        Assert.Equal(expectedFilters, filters);
        Assert.Equal(144, 720 - topInset - 66 - 346 - filters);
    }

    [Fact]
    public void Explicit_bottom_padding_is_not_counted_as_filter_or_result_space() =>
        Assert.Equal(128, TransactionsFilterViewportBudget.Calculate(720, 90, 346, 12));

    [Fact]
    public void A_physically_short_window_retains_the_original_minimum_scrollable_filter_target() =>
        Assert.Equal(44, TransactionsFilterViewportBudget.Calculate(200, 66, 346, 0));
}
