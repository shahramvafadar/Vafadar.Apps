using System.Globalization;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>AT-412: rejected raw quantities never overflow while converting to stored base units.</summary>
[Trait("AT", "AT-412")]
public sealed class QuantityRangeTests
{
    [Theory]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Piece)]
    public void Parseable_decimal_outside_base_unit_range_is_a_refusal_not_an_exception(QuantityUnit unit)
    {
        var type = new AssetType { Name = "Fictitious quantity", PriceCurrencyCode = "EUR", Dimension = unit == QuantityUnit.Piece ? AssetDimension.Count : AssetDimension.Mass };
        var quantity = -1L; var accepted = true;
        var error = Record.Exception(() => accepted = Quantities.TryParse("79228162514264337593543950335", unit, type, CultureInfo.GetCultureInfo("en"), out quantity));
        Assert.Null(error); Assert.False(accepted); Assert.Equal(0, quantity);
    }
}
