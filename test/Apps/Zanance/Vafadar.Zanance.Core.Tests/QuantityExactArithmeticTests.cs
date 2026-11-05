using System.Globalization;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Submitted quantities and derived masses retain exact stored-unit boundaries.</summary>
public sealed class QuantityExactArithmeticTests
{
    /// <summary>Decimal parsing must not erase a submitted nonzero fraction below one base unit.</summary>
    [Theory]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Piece)]
    public void Rounded_overprecision_is_not_an_exact_quantity(QuantityUnit unit)
    {
        Assert.False(Quantities.TryParse("1.000000000000000000000000000001", unit, Type(unit), CultureInfo.GetCultureInfo("en"), out var quantity, out var outside));
        Assert.Equal(0, quantity);
        Assert.False(outside);
    }

    /// <summary>An exact value just above the largest stored quantity retains the range error.</summary>
    [Theory]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Piece)]
    public void A_rounded_tail_above_the_limit_is_still_outside(QuantityUnit unit)
    {
        var maximum = unit == QuantityUnit.Kilogram ? "9223372036854.775807" : "9223372036854775.807";
        Assert.False(Quantities.TryParse(maximum + "000000000000000000000000000001", unit, Type(unit), CultureInfo.GetCultureInfo("en"), out var quantity, out var outside));
        Assert.Equal(0, quantity);
        Assert.True(outside);
    }

    /// <summary>Redundant fractional zeros do not change the exact submitted quantity.</summary>
    [Theory]
    [InlineData(QuantityUnit.Gram)]
    [InlineData(QuantityUnit.Kilogram)]
    [InlineData(QuantityUnit.Piece)]
    public void Exact_trailing_zeroes_remain_supported(QuantityUnit unit)
    {
        Assert.True(Quantities.TryParse("1.000000000000000000000000000000", unit, Type(unit), CultureInfo.GetCultureInfo("en"), out var quantity, out var outside));
        Assert.Equal(Quantities.Factor(unit), quantity);
        Assert.False(outside);
    }

    /// <summary>A supported final mass does not require its undivided product to fit long.</summary>
    [Theory]
    [InlineData(long.MaxValue, 1000, long.MaxValue)]
    [InlineData(long.MaxValue, 2, 18446744073709551L)]
    [InlineData(long.MinValue, 1000, long.MinValue)]
    [InlineData(3001, 333, 999)]
    public void Mass_divides_the_exact_product(long quantity, long weight, long expected)
    {
        var type = Type(QuantityUnit.Piece);
        type.UnitWeightMg = weight;
        Assert.Equal(expected, Quantities.MassMg(quantity, type));
        Assert.Equal(weight, type.UnitWeightMg);
    }

    /// <summary>Purity keeps the original truncation toward zero after an exact wide product.</summary>
    [Theory]
    [InlineData(long.MaxValue, 5000, 4611686018427387903L)]
    [InlineData(13333, 7500, 9999)]
    public void Fine_mass_divides_the_exact_product(long quantity, int purity, long expected)
    {
        var type = Type(QuantityUnit.Gram);
        type.PurityPer10000 = purity;
        Assert.Equal(expected, Quantities.FineMassMg(quantity, type));
        Assert.Equal(purity, type.PurityPer10000);
    }

    /// <summary>Large signed purity results use independently calculated integer-division expectations.</summary>
    [Theory]
    [InlineData(long.MaxValue, 9222449699651090329L)]
    [InlineData(long.MinValue, -9222449699651090330L)]
    public void Large_fine_mass_keeps_the_exact_quotient(long quantity, long expected) =>
        Fine_mass_divides_the_exact_product(quantity, 9999, expected);

    /// <summary>Unknown weight or purity remains unknown rather than invented or evaluated.</summary>
    [Fact]
    public void Missing_weight_and_purity_remain_unknown()
    {
        var type = Type(QuantityUnit.Piece);
        type.PurityPer10000 = 9999;
        Assert.Null(Quantities.FineMassMg(long.MaxValue, type));
        type.UnitWeightMg = long.MaxValue;
        type.PurityPer10000 = null;
        Assert.Null(Quantities.FineMassMg(long.MaxValue, type));
    }

    /// <summary>Only a representable completed mass is accepted; an oversized result still refuses.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrepresentable_final_mass_still_refuses(bool negative)
    {
        var type = Type(QuantityUnit.Piece);
        type.UnitWeightMg = 1001;
        Assert.Throws<OverflowException>(() => Quantities.MassMg(negative ? long.MinValue : long.MaxValue, type));
    }

    private static AssetType Type(QuantityUnit unit) => new()
    {
        Name = "Exact quantity boundary", PriceCurrencyCode = "EUR", Divisible = true,
        Dimension = unit == QuantityUnit.Piece ? AssetDimension.Count : AssetDimension.Mass,
    };
}
