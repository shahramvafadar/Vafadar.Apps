using System.Globalization;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>AT-412: exact stored limits and ordinary invalid drafts preserve their distinct original meanings.</summary>
[Trait("AT", "AT-412")]
public sealed class QuantityBoundaryTests
{
    [Theory]
    [InlineData(QuantityUnit.Gram, true, "9223372036854775.807", long.MaxValue)]
    [InlineData(QuantityUnit.Gram, true, "9223372036854775.808", 0L)]
    [InlineData(QuantityUnit.Kilogram, true, "9223372036854.775807", long.MaxValue)]
    [InlineData(QuantityUnit.Kilogram, true, "9223372036854.775808", 0L)]
    [InlineData(QuantityUnit.Piece, true, "9223372036854775.807", long.MaxValue)]
    [InlineData(QuantityUnit.Piece, true, "9223372036854775.808", 0L)]
    [InlineData(QuantityUnit.Piece, false, "9223372036854775", 9223372036854775000L)]
    [InlineData(QuantityUnit.Piece, false, "9223372036854776", 0L)]
    public void Exact_stored_unit_boundary_remains_accepted_and_only_the_next_value_is_outside(QuantityUnit unit, bool divisible, string raw, long expected)
    {
        var type = new AssetType { Name = "Original bounded quantity", PriceCurrencyCode = "EUR", Divisible = divisible,
            Dimension = unit == QuantityUnit.Piece ? AssetDimension.Count : AssetDimension.Mass };
        var accepted = Quantities.TryParse(raw, unit, type, CultureInfo.GetCultureInfo("en"), out var quantity, out var outside);
        Assert.Equal(expected > 0, accepted); Assert.Equal(expected == 0, outside); Assert.Equal(expected, quantity);
    }

    [Theory]
    [InlineData(QuantityUnit.Gram, "0.0001", 0L)]
    [InlineData(QuantityUnit.Piece, "1.5", 1500L)]
    [InlineData(QuantityUnit.Piece, "-1", 0L)]
    public void Invalid_precision_fractional_whole_type_and_sign_are_not_range_errors(QuantityUnit unit, string raw, long converted)
    {
        var type = new AssetType { Name = "Original whole quantity", PriceCurrencyCode = "EUR",
            Dimension = unit == QuantityUnit.Piece ? AssetDimension.Count : AssetDimension.Mass };
        Assert.False(Quantities.TryParse(raw, unit, type, CultureInfo.GetCultureInfo("en"), out var quantity, out var outside));
        Assert.False(outside); Assert.Equal(converted, quantity);
    }
}
