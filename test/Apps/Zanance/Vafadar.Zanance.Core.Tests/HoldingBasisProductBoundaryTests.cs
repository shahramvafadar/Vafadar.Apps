using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Valid large holding outflows retain exact weighted basis without an overflowing intermediate product.</summary>
public sealed class HoldingBasisProductBoundaryTests
{
    /// <summary>A complete or partial supported outflow must not require the undivided basis product to fit decimal.</summary>
    [Theory]
    [InlineData(AssetEventKind.Outflow, long.MaxValue)]
    [InlineData(AssetEventKind.Outflow, long.MaxValue - 1)]
    [InlineData(AssetEventKind.Correction, long.MaxValue)]
    [InlineData(AssetEventKind.Correction, long.MaxValue - 1)]
    [InlineData(AssetEventKind.Sale, long.MaxValue)]
    [InlineData(AssetEventKind.Sale, long.MaxValue - 1)]
    public void Large_valid_outflows_keep_the_exact_remaining_basis(AssetEventKind kind, long part)
    {
        var events = History(long.MaxValue, long.MaxValue, part, kind);
        var source = events.Select(Snapshot).ToArray();
        Assert.Null(HoldingsLedger.FindConflict(events));

        var result = HoldingsLedger.Basis(events, events[0].AssetTypeId);

        Assert.Equal(long.MaxValue - part, result.Quantity);
        Assert.Equal(long.MaxValue - part, result.Basis);
        if (kind == AssetEventKind.Sale)
        {
            var sale = Assert.Single(result.Sales);
            Assert.Same(events[1], sale.Sale);
            Assert.Equal(part, sale.RemovedBasis);
            Assert.Equal(123 - part, sale.Result);
        }
        else
        {
            Assert.Empty(result.Sales);
        }

        Assert.Equal(source[0], Snapshot(events[0]));
        Assert.Equal(source[1], Snapshot(events[1]));
    }

    /// <summary>Weighted basis keeps the established nearest-minor-unit, midpoint-away rule.</summary>
    [Theory]
    [InlineData(1, 2, 1, 1)]
    [InlineData(1, 3, 1, 0)]
    [InlineData(2, 3, 1, 1)]
    [InlineData(5, 2, 1, 3)]
    public void Outflow_rounding_keeps_the_original_minor_unit_policy(long basis, long whole, long part, long removed)
    {
        var events = History(whole, basis, part, AssetEventKind.Sale);
        var result = HoldingsLedger.Basis(events, events[0].AssetTypeId);
        Assert.Equal(basis - removed, result.Basis);
        Assert.Equal(removed, Assert.Single(result.Sales).RemovedBasis);
    }

    /// <summary>An unpriced opening remains unknown through the large partial outflow.</summary>
    [Fact]
    public void Unknown_basis_does_not_become_a_zero_or_a_computed_cost()
    {
        var events = History(long.MaxValue, null, long.MaxValue - 1, AssetEventKind.Sale);
        var result = HoldingsLedger.Basis(events, events[0].AssetTypeId);
        Assert.Equal(1, result.Quantity);
        Assert.Null(result.Basis);
        Assert.Null(Assert.Single(result.Sales).RemovedBasis);
        Assert.Null(result.Sales[0].Result);
    }

    /// <summary>A genuinely unsupported accumulated monetary basis still refuses instead of wrapping.</summary>
    [Fact]
    public void Actual_basis_accumulation_overflow_is_still_refused()
    {
        var events = History(1, long.MaxValue, 1, AssetEventKind.Purchase);
        events[1].BasisAmount = 1;
        Assert.Throws<OverflowException>(() => HoldingsLedger.Basis(events, events[0].AssetTypeId));
    }

    private static object Snapshot(AssetEvent e) =>
        (e.Id, e.AssetTypeId, e.LocationId, e.ToLocationId, e.Kind, e.Date, e.Quantity,
            e.BasisAmount, e.ProceedsAmount, e.IsIncrease, e.GroupId, e.Reason, e.Note, e.CreatedAt, e.UpdatedAt);

    private static AssetEvent[] History(long quantity, long? basis, long part, AssetEventKind kind)
    {
        var type = Guid.CreateVersion7();
        var place = Guid.CreateVersion7();
        return
        [
            new() { AssetTypeId = type, LocationId = place, Kind = AssetEventKind.Opening, Quantity = quantity, BasisAmount = basis, Date = new(2026, 11, 1) },
            new() { AssetTypeId = type, LocationId = place, Kind = kind, Quantity = part, ProceedsAmount = 123, IsIncrease = false, Date = new(2026, 11, 2) },
        ];
    }
}
