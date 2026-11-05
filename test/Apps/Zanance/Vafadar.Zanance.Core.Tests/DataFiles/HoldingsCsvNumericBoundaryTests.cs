using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

/// <summary>Untrusted holdings quantities and prices must remain exact and report row errors without overflow.</summary>
public sealed class HoldingsCsvNumericBoundaryTests
{
    [Theory]
    [InlineData("79228162514264337593543950335", 0L)]
    [InlineData("9223372036854775.807", long.MaxValue)]
    [InlineData("9223372036854775.808", 0L)]
    [InlineData("1.00000000000000000000000000001", 0L)]
    [InlineData("9223372036854775.80700000000001", 0L)]
    [InlineData("0001.0000000000000000000000000000", 1000L)]
    [InlineData(".001", 1L)]
    [InlineData("1.", 1000L)]
    [InlineData("0.0001", 0L)]
    [InlineData("1e2", 0L)]
    [InlineData("+1", 0L)]
    public void Event_quantity_is_exact_or_a_row_error(string text, long expected)
    {
        var type = new AssetType { Name = "Boundary asset", PriceCurrencyCode = "EUR" };
        var location = new AssetLocation { Name = "Boundary location" };
        var row = Row("event", type.Id); Set(row, "location", location.Id.ToString());
        Set(row, "kind", "Opening"); Set(row, "quantity", text);
        var content = HoldingsCsv.Read([HoldingsCsv.Columns, row], [type], [location]);
        if (expected == 0) { Assert.Empty(content.Events); Assert.Equal("Quantity", Assert.Single(content.Errors).Error); }
        else { Assert.Empty(content.Errors); Assert.Equal(expected, Assert.Single(content.Events).Quantity); }
        Assert.Empty(content.Valuations);
    }

    [Theory]
    [InlineData("EUR", "79228162514264337593543950335", 0L)]
    [InlineData("JPY", "79228162514264337593543950335", 0L)]
    [InlineData("KWD", "79228162514264337593543950335", 0L)]
    [InlineData("EUR", "92233720368547.75807", long.MaxValue)]
    [InlineData("EUR", "92233720368547.75808", 0L)]
    [InlineData("JPY", "9223372036854775.807", long.MaxValue)]
    [InlineData("KWD", "9223372036854.775807", long.MaxValue)]
    [InlineData("EUR", "1.00000000000000000000000000001", 0L)]
    [InlineData("EUR", "0.00001000000000000000000000001", 0L)]
    [InlineData("KWD", "0.00000100000000000000000000001", 0L)]
    [InlineData("EUR", "0001.2300000000000000000000000000", 123000L)]
    [InlineData("JPY", ".001", 1L)]
    [InlineData("KWD", ".000001", 1L)]
    public void Price_is_exact_in_its_original_currency_or_a_row_error(string currency, string text, long expected)
    {
        var type = new AssetType { Name = "Boundary asset", PriceCurrencyCode = currency };
        var row = Row("valuation", type.Id); Set(row, "currency", currency); Set(row, "price_per_unit", text);
        var content = HoldingsCsv.Read([HoldingsCsv.Columns, row], [type], []);
        if (expected == 0) { Assert.Empty(content.Valuations); Assert.Equal("Amount", Assert.Single(content.Errors).Error); }
        else { Assert.Empty(content.Errors); Assert.Equal(expected, Assert.Single(content.Valuations).PricePerUnitMilli); }
        Assert.Empty(content.Events);
    }

    private static string[] Row(string kind, Guid type)
    {
        var row = Enumerable.Repeat(string.Empty, HoldingsCsv.Columns.Count).ToArray();
        Set(row, "record", kind); Set(row, "id", Guid.NewGuid().ToString()); Set(row, "asset_type", type.ToString());
        Set(row, "date", "2026-11-05"); Set(row, "format_version", HoldingsCsv.FormatVersion);
        return row;
    }

    private static void Set(string[] row, string column, string value) => row[HoldingsCsv.Columns.ToList().IndexOf(column)] = value;
}
