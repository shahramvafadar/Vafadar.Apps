using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>End-of-day holding reads use actual chronology without hiding unsupported chronological prefixes.</summary>
public sealed class HoldingChronologicalQuantityTests
{
    private static readonly DateOnly Day = new(2026, 11, 1);

    /// <summary>Every permutation of a valid full-range history has the same supported final quantity.</summary>
    [Theory]
    [InlineData(0, false), InlineData(1, false), InlineData(2, false)]
    [InlineData(3, false), InlineData(4, false), InlineData(5, false)]
    [InlineData(0, true), InlineData(1, true), InlineData(2, true)]
    [InlineData(3, true), InlineData(4, true), InlineData(5, true)]
    public void Valid_permutations_keep_the_supported_quantity(int permutation, bool positions)
    {
        var original = History();
        int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
        var source = orders[permutation].Select(i => original[i]).ToArray();
        Assert.Null(HoldingsLedger.FindConflict(source));
        var quantity = positions ? Assert.Single(HoldingsLedger.Positions(source, Day.AddDays(2))).Quantity
            : HoldingsLedger.Quantity(source, original[0].AssetTypeId, Day.AddDays(2));
        Assert.Equal(long.MaxValue, quantity);
        for (var i = 0; i < source.Length; i++) Assert.Same(original[orders[permutation][i]], source[i]);
    }

    /// <summary>A later outflow cannot rescue a genuinely overflowing earlier day, even if input order hides it.</summary>
    [Theory]
    [InlineData(false), InlineData(true)]
    public void A_chronologically_unsupported_prefix_still_refuses(bool positions)
    {
        var events = History();
        events[1].Date = Day.AddDays(2); events[1].Quantity = 1;
        events[2].Date = Day.AddDays(1); events[2].Quantity = 1;
        Assert.Throws<OverflowException>(() => HoldingsLedger.FindConflict(events));
        Assert.Throws<OverflowException>(() => positions
            ? (object)HoldingsLedger.Positions(events, Day.AddDays(2))
            : HoldingsLedger.Quantity(events, events[0].AssetTypeId, Day.AddDays(2)));
    }

    /// <summary>Temporal ordering does not enumerate a possibly single-use source twice.</summary>
    [Theory]
    [InlineData(false), InlineData(true)]
    public void Original_source_is_enumerated_once(bool positions)
    {
        var events = History(); var reads = 0;
        IEnumerable<AssetEvent> Once()
        {
            Assert.Equal(1, ++reads);
            yield return events[0]; yield return events[2]; yield return events[1];
        }
        var quantity = positions ? Assert.Single(HoldingsLedger.Positions(Once(), Day.AddDays(2))).Quantity
            : HoldingsLedger.Quantity(Once(), events[0].AssetTypeId, Day.AddDays(2));
        Assert.Equal(long.MaxValue, quantity); Assert.Equal(1, reads);
    }

    /// <summary>Reordering calculation never reorders the established first-seen position keys.</summary>
    [Fact]
    public void Position_key_order_remains_the_original_first_seen_order()
    {
        var first = new AssetEvent { AssetTypeId = Guid.CreateVersion7(), LocationId = Guid.CreateVersion7(), Kind = AssetEventKind.Opening, Quantity = 1, Date = Day.AddDays(1) };
        var second = new AssetEvent { AssetTypeId = Guid.CreateVersion7(), LocationId = Guid.CreateVersion7(), Kind = AssetEventKind.Opening, Quantity = 2, Date = Day };
        var result = HoldingsLedger.Positions([first, second], Day.AddDays(1));
        Assert.Equal(2, result.Count);
        Assert.Equal((first.AssetTypeId, first.LocationId, 1L), (result[0].AssetTypeId, result[0].LocationId, result[0].Quantity));
        Assert.Equal((second.AssetTypeId, second.LocationId, 2L), (result[1].AssetTypeId, result[1].LocationId, result[1].Quantity));
    }

    private static AssetEvent[] History()
    {
        var type = Guid.CreateVersion7(); var place = Guid.CreateVersion7();
        return
        [
            new() { AssetTypeId = type, LocationId = place, Kind = AssetEventKind.Opening, Quantity = long.MaxValue, Date = Day },
            new() { AssetTypeId = type, LocationId = place, Kind = AssetEventKind.Outflow, Quantity = long.MaxValue, Date = Day.AddDays(1) },
            new() { AssetTypeId = type, LocationId = place, Kind = AssetEventKind.GiftReceived, Quantity = long.MaxValue, Date = Day.AddDays(2) },
        ];
    }
}
