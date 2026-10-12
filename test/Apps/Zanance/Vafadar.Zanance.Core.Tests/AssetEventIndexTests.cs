using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>The original quantity calculator remains the oracle for indexed unchanged event snapshots.</summary>
public sealed class AssetEventIndexTests
{
    private static readonly DateOnly Day = new(2026, 10, 12);

    [Theory, Trait("AT", "AT-177")]
    [InlineData(AssetEventKind.Opening), InlineData(AssetEventKind.Purchase), InlineData(AssetEventKind.Sale)]
    [InlineData(AssetEventKind.LocationTransfer), InlineData(AssetEventKind.GiftReceived)]
    [InlineData(AssetEventKind.Outflow), InlineData(AssetEventKind.Correction)]
    public void Every_original_kind_keeps_date_boundaries_and_type_separation(AssetEventKind kind)
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        AssetEvent Event(Guid type, DateOnly date, long quantity, bool increase) => new()
        {
            AssetTypeId = type, Date = date, Quantity = quantity, Kind = kind, IsIncrease = increase,
            LocationId = Guid.NewGuid(), ToLocationId = Guid.NewGuid(), BasisAmount = 12, ProceedsAmount = 17,
        };
        AssetEvent[] source = [Event(first, Day.AddDays(-1), 10, true), Event(second, Day, 2000, true),
            Event(first, Day, 2, false), Event(first, Day.AddDays(1), 9, true)];
        var index = new AssetEventIndex(source);
        foreach (var type in new[] { first, second, Guid.NewGuid() })
            foreach (var date in new[] { Day.AddDays(-2), Day.AddDays(-1), Day, Day.AddDays(1) })
                Assert.Equal(HoldingsLedger.Quantity(source, type, date), HoldingsLedger.Quantity(index.For(type), type, date));
        Assert.Equal(new[] { source[0], source[2], source[3] }, index.For(first));
        Assert.Same(source[1], Assert.Single(index.For(second)));
    }

    [Fact, Trait("AT", "AT-177")]
    public void Snapshot_is_enumerated_once_across_repeated_asset_queries()
    {
        var item = new AssetEvent { AssetTypeId = Guid.NewGuid() }; var reads = 0;
        IEnumerable<AssetEvent> Once() { Assert.Equal(1, ++reads); yield return item; }
        var index = new AssetEventIndex(Once());
        Assert.Same(item, Assert.Single(index.For(item.AssetTypeId)));
        Assert.Same(item, Assert.Single(index.For(item.AssetTypeId)));
        Assert.Empty(index.For(Guid.NewGuid())); Assert.Equal(1, reads);
    }

    [Fact, Trait("AT", "AT-177")]
    public void Original_checked_intermediate_overflow_is_not_hidden_by_grouping_or_later_outflow()
    {
        var type = Guid.NewGuid();
        AssetEvent[] source = [new() { AssetTypeId = type, Date = Day, Kind = AssetEventKind.Opening, Quantity = long.MaxValue },
            new() { AssetTypeId = type, Date = Day, Kind = AssetEventKind.Purchase, Quantity = 1 },
            new() { AssetTypeId = type, Date = Day, Kind = AssetEventKind.Outflow, Quantity = 1 }];
        var index = new AssetEventIndex(source);
        Assert.Throws<OverflowException>(() => HoldingsLedger.Quantity(source, type, Day));
        Assert.Throws<OverflowException>(() => HoldingsLedger.Quantity(index.For(type), type, Day));
        Assert.Equal(0, HoldingsLedger.Quantity(index.For(type), type, Day.AddDays(-1)));
    }

    [Fact, Trait("AT", "AT-177")]
    public void A_same_location_transfer_remains_one_neutral_event_and_preserves_metadata()
    {
        var location = Guid.NewGuid(); var item = new AssetEvent
        { AssetTypeId = Guid.NewGuid(), LocationId = location, ToLocationId = location, Kind = AssetEventKind.LocationTransfer,
          Date = Day, Quantity = 9000, Note = "Fictitious transfer", GroupId = Guid.NewGuid() };
        var before = System.Text.Json.JsonSerializer.Serialize(item);
        var index = new AssetEventIndex([item]);
        Assert.Same(item, Assert.Single(index.For(item.AssetTypeId)));
        Assert.Equal(0, HoldingsLedger.Quantity(index.For(item.AssetTypeId), item.AssetTypeId, Day));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(item));
    }
}
